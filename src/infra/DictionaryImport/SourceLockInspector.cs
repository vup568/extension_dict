namespace Infrastructure.DictionaryImport;

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Application.DictionaryImport;

/// <summary>Offline, read-only preflight. Hashes actual bytes; it neither fetches artifacts nor opens a DB.</summary>
public static partial class SourceLockInspector
{
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 32
    };

    private const long MaxExpandedLimit = 8L * 1024 * 1024 * 1024;
    private const long MetadataLimit = 32L * 1024 * 1024;

    public static async Task<SourceLockInspection> VerifyAsync(string lockPath, CancellationToken ct = default)
    {
        var blockers = new List<ImportBlocker>();
        DictionarySourceLock? sourceLock = null;
        var lockHash = "";
        var fingerprint = "";
        try
        {
            var fullPath = Path.GetFullPath(lockPath);
            var root = Path.GetDirectoryName(fullPath)!;
            var info = new FileInfo(fullPath);
            if (!info.Exists || info.Length is < 2 or > 1048576 || HasReparsePoint(fullPath, root))
                return Refuse("source_lock_unavailable", "The bounded source lock must be an available regular local file.");

            await using var input = OpenRead(fullPath);
            var bytes = new byte[checked((int)input.Length)];
            await input.ReadExactlyAsync(bytes, ct);
            lockHash = Hash(bytes);
            sourceLock = JsonSerializer.Deserialize<DictionarySourceLock>(bytes, JsonOptions);
            if (sourceLock is null || sourceLock.SchemaVersion != 1 || sourceLock.Sources is null)
                return Refuse("source_lock_invalid", "Source lock schema version 1 and sources are required.");

            // EARS[Unwanted]: FR-006/007/008 require exact replay pins and explicit review evidence before canonical writes.
            if (!Pinned(sourceLock.TransformationId) || !Sha(sourceLock.TransformationSha256))
                Add("transformation_unpinned", "Transformation identity and SHA256 are required.");
            var mappingDeclared = !string.IsNullOrWhiteSpace(sourceLock.MappingPath)
                || !string.IsNullOrWhiteSpace(sourceLock.MappingSha256)
                || !string.IsNullOrWhiteSpace(sourceLock.MappingApprovalReference);
            if (sourceLock.Scope != "ja-en" || mappingDeclared)
            {
                if (!Sha(sourceLock.MappingSha256) || !Pinned(sourceLock.MappingApprovalReference))
                    Add("mapping_unapproved", "Reviewed mapping hash and approval reference are required.");
                await CheckFile(sourceLock.MappingPath, sourceLock.MappingSha256, 256L * 1024 * 1024, "mapping", null);
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pin in sourceLock.Sources)
            {
                ct.ThrowIfCancellationRequested();
                if (pin is null || !SourceIdPattern().IsMatch(pin.SourceId ?? ""))
                {
                    Add("source_identity_invalid", "Source identifiers must be bounded lowercase identifiers.");
                    continue;
                }
                var id = pin.SourceId!;
                if (!ids.Add(id)) Add("source_duplicate", "Source identifiers must be unique.", id);
                if (pin.Kind is not ("jmdict" or "kanjidic2" or "vietnamese" or "overrides") || pin.SourceId != pin.Kind)
                    Add("source_kind_invalid", "The source kind is unsupported.", id);
                if (!Pinned(pin.Version) || !Pinned(pin.Publisher) || !Pinned(pin.Attribution)
                    || !Uri.TryCreate(pin.ArtifactUrl, UriKind.Absolute, out var url)
                    || url.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(url.UserInfo)
                    || !Pinned(pin.ArtifactUrl))
                    Add("source_metadata_unpinned", "Exact source version, HTTPS URL, publisher and attribution are required.", id);
                if (pin.LicenseStatus != "approved" || !Pinned(pin.LicenseApprovalReference))
                    Add("license_unapproved", "Canonical use requires a separate approved component license decision.", id);
                if (pin.ExpectedRecords < 1)
                    Add("source_count_unpinned", "The measured source record count is required.", id);
                if (pin.SchemaEntities is null || pin.SchemaEntities.Count > 1024
                    || pin.SchemaEntities.Any(e => !EntityNamePattern().IsMatch(e.Key) || e.Value is null || e.Value.Length > 512 || e.Value.Contains('&') || e.Value.Contains('<')))
                    Add("schema_entities_invalid", "Pinned entity labels must be bounded non-recursive literal text.", id);

                await CheckFile(pin.SchemaPath, pin.SchemaSha256, MetadataLimit, "schema", id);
                await CheckFile(pin.LicensePath, pin.LicenseSha256, MetadataLimit, "license", id);
                if (pin.ArtifactBytes < 1 || pin.ExpandedBytes < 1 || pin.MaxExpandedBytes < pin.ExpandedBytes
                    || pin.MaxExpandedBytes > MaxExpandedLimit || pin.Compression is not ("none" or "gzip")
                    || !Sha(pin.ArtifactSha256) || !Sha(pin.ExpandedSha256))
                {
                    Add("artifact_bounds_invalid", "Exact compressed/expanded hashes, lengths and safe expansion bounds are required.", id);
                    continue;
                }
                var artifactPath = Resolve(root, pin.ArtifactPath);
                if (artifactPath is null)
                {
                    Add("artifact_unavailable", "Artifact must be a regular local file within the lock directory.", id);
                    continue;
                }
                try
                {
                    await using var compressed = OpenRead(artifactPath);
                    if (compressed.Length != pin.ArtifactBytes || !Equal(await HashStream(compressed, pin.ArtifactBytes, ct), pin.ArtifactSha256))
                    {
                        Add("artifact_hash_mismatch", "Artifact bytes differ from the approved pin.", id);
                        continue;
                    }
                    compressed.Position = 0;
                    if (pin.Compression == "gzip")
                    {
                        await using var expanded = new GZipStream(compressed, CompressionMode.Decompress, leaveOpen: true);
                        var result = await HashAndCount(expanded, pin.MaxExpandedBytes, ct);
                        if (result.Bytes != pin.ExpandedBytes || !Equal(result.Hash, pin.ExpandedSha256))
                            Add("expanded_hash_mismatch", "Expanded artifact bytes differ from the approved pin.", id);
                    }
                    else if (pin.ArtifactBytes != pin.ExpandedBytes || !Equal(pin.ArtifactSha256, pin.ExpandedSha256))
                        Add("expanded_hash_mismatch", "Uncompressed source and expanded pins must be identical.", id);
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException)
                {
                    Add("artifact_unreadable", "Artifact could not be read within its expansion bounds.", id);
                }
            }

            // EARS[State]: An explicit English-only scope never claims that Vietnamese data was imported.
            var requiredKinds = sourceLock.Scope switch
            {
                "ja-en" => new[] { "jmdict", "kanjidic2" },
                "ja-vi" => new[] { "jmdict", "kanjidic2", "vietnamese", "overrides" },
                _ => Array.Empty<string>()
            };
            if (requiredKinds.Length == 0)
                Add("source_scope_invalid", "The source scope is unsupported.");
            foreach (var kind in requiredKinds)
                if (!sourceLock.Sources.Any(pin => pin?.Kind == kind))
                    Add("source_group_missing", $"The {kind} source group is required.");
            if (sourceLock.Scope == "ja-en" && sourceLock.Sources.Any(pin => pin is not null && pin.Kind is not ("jmdict" or "kanjidic2")))
                Add("source_scope_mismatch", "The ja-en scope allows only JMdict and KANJIDIC2.");

            fingerprint = Hash(Encoding.UTF8.GetBytes($"{lockHash}\n{sourceLock.TransformationSha256}\n{sourceLock.MappingSha256}\n"));
            var resolvedLock = sourceLock with
            {
                MappingPath = Resolve(root, sourceLock.MappingPath) ?? sourceLock.MappingPath,
                Sources = sourceLock.Sources.Where(pin => pin is not null).Select(pin => pin with
                {
                    ArtifactPath = Resolve(root, pin.ArtifactPath) ?? pin.ArtifactPath,
                    SchemaPath = Resolve(root, pin.SchemaPath) ?? pin.SchemaPath,
                    LicensePath = Resolve(root, pin.LicensePath) ?? pin.LicensePath,
                    ArtifactSha256 = pin.ArtifactSha256?.ToLowerInvariant() ?? "",
                    ExpandedSha256 = pin.ExpandedSha256?.ToLowerInvariant() ?? "",
                    SchemaSha256 = pin.SchemaSha256?.ToLowerInvariant() ?? "",
                    LicenseSha256 = pin.LicenseSha256?.ToLowerInvariant() ?? ""
                }).ToArray()
            };
            return new(resolvedLock, lockHash, fingerprint, blockers);

            async Task CheckFile(string path, string hash, long maximum, string category, string? id)
            {
                var resolved = Resolve(root, path);
                if (!Sha(hash) || resolved is null)
                {
                    Add($"{category}_unavailable", $"A local {category} file and exact SHA256 are required.", id);
                    return;
                }
                try
                {
                    await using var file = OpenRead(resolved);
                    if (file.Length == 0 || file.Length > maximum || !Equal(await HashStream(file, maximum, ct), hash))
                        Add($"{category}_hash_mismatch", $"The {category} file does not match its bounded SHA256 pin.", id);
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                {
                    Add($"{category}_unreadable", $"The {category} file could not be read safely.", id);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException)
        {
            Add("source_lock_invalid", "Source lock could not be read as bounded schema version 1 JSON.");
        }
        return new(sourceLock, lockHash, fingerprint, blockers);

        void Add(string code, string message, string? source = null) => blockers.Add(new(code, message, source));
        SourceLockInspection Refuse(string code, string message) => new(null, lockHash, fingerprint, [new(code, message)]);
    }

    private static bool Pinned(string? value) => !string.IsNullOrWhiteSpace(value)
        && value.Length <= 4096 && !value.Contains("latest", StringComparison.OrdinalIgnoreCase)
        && !value.Contains("pending", StringComparison.OrdinalIgnoreCase)
        && !value.Contains("placeholder", StringComparison.OrdinalIgnoreCase)
        && !value.Contains("TODO", StringComparison.OrdinalIgnoreCase);

    private static bool Sha(string? value) => value is not null && ShaPattern().IsMatch(value) && value.Any(c => c != '0');
    private static bool Equal(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    private static string Hash(byte[] value) => Convert.ToHexStringLower(SHA256.HashData(value));
    private static FileStream OpenRead(string path) => new(path, FileMode.Open, FileAccess.Read, FileShare.Read,
        65536, FileOptions.Asynchronous | FileOptions.SequentialScan);

    /// <summary>Inputs are cache-relative; traversal and reparse points cannot escape the selected cache.</summary>
    private static string? Resolve(string root, string? relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)) return null;
        var path = Path.GetFullPath(Path.Combine(root, relative));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, PathComparison)
            || !File.Exists(path) || HasReparsePoint(path, root)) return null;
        return path;
    }

    private static bool HasReparsePoint(string path, string root)
    {
        for (var current = path; current is not null; current = Path.GetDirectoryName(current))
        {
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return true;
            if (string.Equals(current, root, PathComparison)) break;
        }
        return false;
    }

    private static StringComparison PathComparison => OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private static async Task<string> HashStream(Stream stream, long maximum, CancellationToken ct) => (await HashAndCount(stream, maximum, ct)).Hash;

    private static async Task<(string Hash, long Bytes)> HashAndCount(Stream stream, long maximum, CancellationToken ct)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[65536];
        long length = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            length = checked(length + read);
            if (length > maximum) throw new InvalidDataException("Bounded input exceeded.");
            hash.AppendData(buffer, 0, read);
        }
        return (Convert.ToHexStringLower(hash.GetHashAndReset()), length);
    }

    [GeneratedRegex("^[a-fA-F0-9]{64}$", RegexOptions.CultureInvariant)] private static partial Regex ShaPattern();
    [GeneratedRegex("^[a-z0-9_-]{1,64}$", RegexOptions.CultureInvariant)] private static partial Regex SourceIdPattern();
    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_-]{0,63}$", RegexOptions.CultureInvariant)] private static partial Regex EntityNamePattern();
}
