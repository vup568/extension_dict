using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Application.DictionaryImport;
using Domain.Entities;
using Infrastructure.DictionaryImport.Parsing;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Infrastructure.Persistence.Migrations;
using Npgsql;

namespace Infrastructure.DictionaryImport;

/// <summary>Fixture-verified local import mechanics. This class never publishes or activates a database.</summary>
public static class DictionaryImportStore
{
    private const long WriterLock = 0x4A5044494D50;
    public const int DefaultBatchSize = 500;

    public static async Task PrepareAsync(string connectionString, string confirmedTarget, string runId,
        SourceLockInspection inputs, CancellationToken ct = default)
    {
        RequireInputs(inputs, runId);
        var target = IsolatedImportTarget.Check(connectionString, confirmedTarget);
        await VerifyAllFilesAsync(inputs.Lock!, ct);
        var admin = new NpgsqlConnectionStringBuilder(target.ConnectionString) { Database = "postgres", Pooling = false };
        await using var connection = new NpgsqlConnection(admin.ConnectionString);
        await connection.OpenAsync(ct);
        await using (var exists = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_database WHERE datname=@name)", connection))
        {
            exists.Parameters.AddWithValue("name", confirmedTarget);
            if ((bool)(await exists.ExecuteScalarAsync(ct))!) throw new InvalidOperationException("IMPORT_TARGET_ALREADY_EXISTS");
        }
        // EARS[Event] FR-009: CREATE has no reuse/reset branch; a racing existing database is refused by PostgreSQL.
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{confirmedTarget}\" TEMPLATE template0", connection))
            await create.ExecuteNonQueryAsync(ct);
        await using var context = Context(target.ConnectionString);
        await context.Database.MigrateAsync(ct);
        var oid = await DatabaseOidAsync(context, ct);
        context.DictionaryImportRuns.Add(new DictionaryImportRun
        {
            RunId = runId, TargetDatabase = confirmedTarget, DatabaseOid = oid,
            SourceLockHash = inputs.SourceLockSha256, FingerprintHash = inputs.FingerprintSha256
        });
        await context.SaveChangesAsync(ct);
    }

    public static async Task<ImportMechanismReport> ImportAsync(string connectionString, string confirmedTarget,
        string runId, SourceLockInspection inputs, bool resume = false, int batchSize = DefaultBatchSize,
        CancellationToken ct = default, IProgress<ImportBatchProgress>? progress = null)
    {
        RequireInputs(inputs, runId);
        if (batchSize is < 1 or > 2000) throw new InvalidOperationException("IMPORT_BATCH_SIZE_INVALID");
        IsolatedImportTarget.Check(connectionString, confirmedTarget);
        // Unsupported review data is refused BEFORE any database write; never quietly drop the Vietnamese group.
        if (inputs.Lock!.Sources.Any(x => x.Kind is not ("jmdict" or "kanjidic2")))
            throw new InvalidOperationException("IMPORT_REVIEWED_MAPPING_REQUIRED");
        await VerifyAllFilesAsync(inputs.Lock, ct);
        await using var context = Context(connectionString);
        await context.Database.OpenConnectionAsync(ct);
        await LockAsync(context, ct);
        try
        {
            var run = await RequireOwnedAsync(context, confirmedTarget, runId, inputs, ct);
            if (run.State == "imported")
            {
                var actual = await ComputeFactsHashAsync(context, ct);
                if (actual != run.FactsHash) throw new InvalidOperationException("IMPORT_FACTS_CHANGED");
                await VerifyCheckpointsAsync(context, run, inputs.Lock, ct);
                return await ReportAsync(context, run, true, ct);
            }
            if (run.State != "new" && (!resume || run.State is not ("loading" or "failed" or "cancelled")))
                throw new InvalidOperationException("IMPORT_RUN_STATE_REFUSED");
            await VerifyCheckpointsAsync(context, run, inputs.Lock, ct);
            run.State = "loading";
            await context.SaveChangesAsync(ct);
            foreach (var source in inputs.Lock.Sources)
                await ImportSourceAsync(context, run, inputs, source, batchSize, ct, progress);
            run = await context.DictionaryImportRuns.SingleAsync(x => x.RunId == runId, ct);
            run.FactsHash = await ComputeFactsHashAsync(context, ct);
            run.State = "imported";
            await context.SaveChangesAsync(ct);
            return await ReportAsync(context, run, false, ct);
        }
        finally
        {
            await UnlockAsync(context);
        }
    }

    public static async Task<ImportMechanismReport> ValidateAsync(string connectionString, string confirmedTarget,
        string runId, SourceLockInspection inputs, CancellationToken ct = default)
    {
        RequireInputs(inputs, runId);
        IsolatedImportTarget.Check(connectionString, confirmedTarget);
        await VerifyAllFilesAsync(inputs.Lock!, ct);
        await using var context = Context(connectionString);
        await context.Database.OpenConnectionAsync(ct);
        await LockAsync(context, ct);
        try
        {
            var run = await RequireOwnedAsync(context, confirmedTarget, runId, inputs, ct);
            if (run.State != "imported") throw new InvalidOperationException("IMPORT_NOT_COMPLETE");
            await VerifyCheckpointsAsync(context, run, inputs.Lock!, ct);
            if (await ComputeFactsHashAsync(context, ct) != run.FactsHash)
                throw new InvalidOperationException("IMPORT_FACTS_CHANGED");
            // EARS[State] FR-013: Mechanism validation alone cannot approve independent corpus, sources or activation.
            return await ReportAsync(context, run, true, ct);
        }
        finally { await UnlockAsync(context); }
    }

    private static async Task ImportSourceAsync(AppDbContext context, DictionaryImportRun run,
        SourceLockInspection inputs, DictionarySourcePin source, int batchSize, CancellationToken ct, IProgress<ImportBatchProgress>? progress)
    {
        // Deny writers/deletion for the lifetime of hashing and parsing (including on Windows).
        await using var file = new FileStream(source.ArtifactPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await VerifyArtifactAsync(file, source, ct);
        file.Position = 0;
        using var expanded = source.Compression == "gzip" ? new GZipStream(file, CompressionMode.Decompress, true) : null;
        Stream input = expanded is null ? file : expanded;
        var records = ReadRecords(input, source, inputs.Lock!.Scope, ct);
        var checkpoint = await context.DictionaryImportCheckpoints.SingleOrDefaultAsync(
            x => x.RunId == run.RunId && x.SourceId == source.SourceId, ct);
        if (checkpoint is null)
        {
            await using var transaction = await context.Database.BeginTransactionAsync(ct);
            var manifest = new SourceManifest
            {
                SourceName = source.SourceId, Publisher = source.Publisher, Version = source.Version,
                License = source.LicenseApprovalReference, Attribution = source.Attribution,
                Checksum = source.ArtifactSha256, TransformationId = inputs.Lock!.TransformationId,
                RecordCount = 0, RejectedCount = 0, ValidationResult = "isolated-loading",
                Metadata = JsonSerializer.Serialize(new { source.ExpandedSha256, source.SchemaSha256,
                    source.LicenseSha256, inputs.FingerprintSha256 })
            };
            checkpoint = new DictionaryImportCheckpoint { RunId = run.RunId, SourceId = source.SourceId, Manifest = manifest };
            context.DictionaryImportCheckpoints.Add(checkpoint);
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        var committed = checkpoint.AcceptedRecords;
        string chain = "";
        long seen = 0;
        var batch = new List<ParsedDictionaryRecord>(batchSize);
        foreach (var record in records)
        {
            ct.ThrowIfCancellationRequested();
            seen++;
            if (record.Ordinal != seen) throw new InvalidOperationException("IMPORT_ORDINAL_INVALID");
            chain = Chain(chain, record);
            if (seen <= committed)
            {
                if (seen == committed && (record.SourceIdentity != checkpoint.LastSourceIdentity || chain != checkpoint.BatchChainHash))
                    throw new InvalidOperationException("IMPORT_CHECKPOINT_CHANGED");
                continue;
            }
            batch.Add(record);
            if (batch.Count == batchSize)
            {
                await WriteBatchAsync(context, checkpoint, batch, chain, ct);
                progress?.Report(new(source.SourceId, checkpoint.AcceptedRecords));
                batch.Clear();
            }
        }
        if (seen < committed || seen != source.ExpectedRecords) throw new InvalidOperationException("IMPORT_SOURCE_COUNT_MISMATCH");
        if (batch.Count > 0)
        {
            await WriteBatchAsync(context, checkpoint, batch, chain, ct);
            progress?.Report(new(source.SourceId, checkpoint.AcceptedRecords));
        }
        var sourceManifest = await context.SourceManifests.SingleAsync(x => x.Id == checkpoint.ManifestId, ct);
        sourceManifest.ValidationResult = "isolated-imported-not-approved";
        await context.SaveChangesAsync(ct);
    }

    private static async Task WriteBatchAsync(AppDbContext context, DictionaryImportCheckpoint checkpoint,
        List<ParsedDictionaryRecord> batch, string chain, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        // EARS[State]: FR-008 keeps each committed batch and its checkpoint atomic.
        // Reload scalar control rows: attaching the old checkpoint would traverse its
        // manifest navigation and retain every previously imported source record.
        context.ChangeTracker.Clear();
        var storedCheckpoint = await context.DictionaryImportCheckpoints.SingleAsync(
            x => x.RunId == checkpoint.RunId && x.SourceId == checkpoint.SourceId, ct);
        if (storedCheckpoint.ManifestId != checkpoint.ManifestId ||
            storedCheckpoint.AcceptedRecords != checkpoint.AcceptedRecords ||
            storedCheckpoint.LastSourceIdentity != checkpoint.LastSourceIdentity ||
            storedCheckpoint.BatchChainHash != checkpoint.BatchChainHash)
            throw new InvalidOperationException("IMPORT_CHECKPOINT_CHANGED");
        var manifest = await context.SourceManifests.SingleAsync(x => x.Id == checkpoint.ManifestId, ct);
        foreach (var record in batch)
        {
            var source = new SourceRecord { ManifestId = checkpoint.ManifestId,
                SourceIdentity = record.SourceIdentity, RecordData = record.RawJson };
            context.SourceRecords.Add(source);
            if (record.Entry is { } entry)
            {
                foreach (var gloss in entry.Senses.SelectMany(x => x.Glosses)) gloss.SourceRecord = source;
                context.DictionaryEntries.Add(entry);
            }
            else if (record.Kanji is { } kanji) context.KanjiRecords.Add(kanji);
            else throw new InvalidOperationException("IMPORT_EMPTY_CANONICAL_RECORD");
        }
        storedCheckpoint.AcceptedRecords += batch.Count;
        storedCheckpoint.LastSourceIdentity = batch[^1].SourceIdentity;
        storedCheckpoint.BatchChainHash = chain;
        manifest.RecordCount = checked((int)storedCheckpoint.AcceptedRecords);
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        checkpoint.AcceptedRecords = storedCheckpoint.AcceptedRecords;
        checkpoint.LastSourceIdentity = storedCheckpoint.LastSourceIdentity;
        checkpoint.BatchChainHash = storedCheckpoint.BatchChainHash;
    }

    private static async Task<DictionaryImportRun> RequireOwnedAsync(AppDbContext context, string target,
        string runId, SourceLockInspection inputs, CancellationToken ct)
    {
        var applied = await context.Database.GetAppliedMigrationsAsync(ct);
        if (!applied.SequenceEqual(context.Database.GetMigrations())) throw new InvalidOperationException("IMPORT_SCHEMA_MISMATCH");
        var runs = await context.DictionaryImportRuns.Include(x => x.Checkpoints).ToListAsync(ct);
        if (runs.Count != 1) throw new InvalidOperationException("IMPORT_UNOWNED_TARGET");
        var run = runs[0];
        if (context.Database.GetDbConnection().Database != target || run.TargetDatabase != target ||
            run.DatabaseOid != await DatabaseOidAsync(context, ct) || run.RunId != runId ||
            run.SourceLockHash != inputs.SourceLockSha256 || run.FingerprintHash != inputs.FingerprintSha256)
            throw new InvalidOperationException("IMPORT_OWNERSHIP_MISMATCH");
        if (await context.Users.AnyAsync(ct) || await context.LearningReferences.AnyAsync(ct) ||
            await context.EditorialMappings.AnyAsync(ct) || await context.KnowledgeReleases.AnyAsync(ct) ||
            await context.ResourceRevisions.AnyAsync(ct) || await context.CurrentKnowledgeRelease.AnyAsync(ct))
            throw new InvalidOperationException("IMPORT_MIXED_TARGET");
        // Reject unowned objects; ownership marker alone never authorizes an existing unknown schema.
        var allowed = context.Model.GetEntityTypes().Select(x => x.GetTableName()!).Append("__EFMigrationsHistory").ToHashSet();
        await using (var guard = new NpgsqlCommand("""
            SELECT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname NOT IN ('public','information_schema') AND nspname NOT LIKE 'pg_%')
                OR EXISTS(SELECT 1 FROM pg_event_trigger)
                OR EXISTS(SELECT 1 FROM pg_policy)
            """, (NpgsqlConnection)context.Database.GetDbConnection()))
            if ((bool)(await guard.ExecuteScalarAsync(ct))!) throw new InvalidOperationException("IMPORT_UNKNOWN_SCHEMA_OBJECT");
        await VerifyRoutinesAndTriggersAsync((NpgsqlConnection)context.Database.GetDbConnection(), ct);
        await VerifyColumnsAsync(context, ct);
        await using var command = new NpgsqlCommand("""
            SELECT c.relname, c.relkind::text FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='public' AND c.relkind IN ('r','p','v','m','f','S')
            """, (NpgsqlConnection)context.Database.GetDbConnection());
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var name = reader.GetString(0);
            if (reader.GetString(1) == "S" ? !allowed.Any(x => name == x + "_id_seq") : reader.GetString(1) != "r" || !allowed.Contains(name))
                throw new InvalidOperationException("IMPORT_UNKNOWN_SCHEMA_OBJECT");
        }
        return run;
    }

    private static async Task VerifyRoutinesAndTriggersAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        // The official migration SQL is the authority for function bodies, rather than a name-only allowlist.
        var sql = new EnforceKnowledgeReleaseImmutability().UpOperations.OfType<SqlOperation>()
            .Concat(new AddConjugationDatasets().UpOperations.OfType<SqlOperation>());
        var expectedBodies = sql.SelectMany(operation => Regex.Matches(operation.Sql,
                @"CREATE FUNCTION\s+(?<name>[a-z_]+)\(\)\s+RETURNS trigger.*?AS\s+\$\$(?<body>.*?)\$\$",
                RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Cast<Match>())
            .ToDictionary(match => match.Groups["name"].Value, match => NormalizeBody(match.Groups["body"].Value), StringComparer.Ordinal);
        await using (var routines = new NpgsqlCommand("""
            SELECT p.proname,p.pronargs,p.prokind::text,p.prorettype='pg_catalog.trigger'::regtype,
                p.prosecdef,p.proconfig IS NULL,l.lanname,p.prosrc
            FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace JOIN pg_language l ON l.oid=p.prolang
            WHERE n.nspname='public'
            """, connection))
        await using (var reader = await routines.ExecuteReaderAsync(ct))
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            while (await reader.ReadAsync(ct))
            {
                var name = reader.GetString(0);
                if (!expectedBodies.TryGetValue(name, out var body) || !seen.Add(name) || reader.GetInt16(1) != 0
                    || reader.GetString(2) != "f" || !reader.GetBoolean(3) || reader.GetBoolean(4)
                    || !reader.GetBoolean(5) || reader.GetString(6) != "plpgsql" || NormalizeBody(reader.GetString(7)) != body)
                    throw new InvalidOperationException("IMPORT_UNKNOWN_SCHEMA_OBJECT");
            }
            if (seen.Count != expectedBodies.Count) throw new InvalidOperationException("IMPORT_UNKNOWN_SCHEMA_OBJECT");
        }
        var expectedTriggers = new Dictionary<string, (string Table, string Function, short Type)>(StringComparer.Ordinal)
        {
            ["trg_guard_published_knowledge_release"] = ("knowledge_releases", "guard_published_knowledge_release", 31),
            ["trg_guard_resource_revision_release"] = ("resource_revisions", "guard_published_release_child", 31),
            ["trg_guard_knowledge_release_manifest_release"] = ("knowledge_release_manifests", "guard_published_release_child", 31),
            ["trg_guard_current_knowledge_release"] = ("current_knowledge_release", "guard_current_knowledge_release", 31),
            ["conjugation_dataset_immutable"] = ("conjugation_datasets", "reject_conjugation_dataset_mutation", 27),
            ["conjugation_dataset_no_truncate"] = ("conjugation_datasets", "reject_conjugation_dataset_mutation", 34)
        };
        await using var triggers = new NpgsqlCommand("""
            SELECT t.tgname,c.relname,p.proname,t.tgtype,t.tgenabled::text,t.tgnargs,t.tgqual IS NULL,
                t.tgconstraint=0,pn.nspname
            FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace
                JOIN pg_proc p ON p.oid=t.tgfoid JOIN pg_namespace pn ON pn.oid=p.pronamespace
            WHERE n.nspname='public' AND NOT t.tgisinternal
            """, connection);
        await using var triggerReader = await triggers.ExecuteReaderAsync(ct);
        var triggerNames = new HashSet<string>(StringComparer.Ordinal);
        while (await triggerReader.ReadAsync(ct))
        {
            var name = triggerReader.GetString(0);
            if (!expectedTriggers.TryGetValue(name, out var expected) || !triggerNames.Add(name)
                || triggerReader.GetString(1) != expected.Table || triggerReader.GetString(2) != expected.Function
                || triggerReader.GetInt16(3) != expected.Type || triggerReader.GetString(4) != "O"
                || triggerReader.GetInt16(5) != 0 || !triggerReader.GetBoolean(6) || !triggerReader.GetBoolean(7)
                || triggerReader.GetString(8) != "public")
                throw new InvalidOperationException("IMPORT_UNKNOWN_SCHEMA_OBJECT");
        }
        if (triggerNames.Count != expectedTriggers.Count) throw new InvalidOperationException("IMPORT_UNKNOWN_SCHEMA_OBJECT");

        static string NormalizeBody(string body) => body.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
    }

    private static async Task VerifyColumnsAsync(AppDbContext context, CancellationToken ct)
    {
        var expected = context.Model.GetRelationalModel().Tables
            .Where(table => table.Schema is null or "public")
            .SelectMany(table => table.Columns.Select(column => new
            {
                Key = (table.Name, column.Name), Value = (column.StoreType, column.IsNullable)
            })).ToDictionary(column => column.Key, column => column.Value);
        expected.Add(("__EFMigrationsHistory", "MigrationId"), ("character varying(150)", false));
        expected.Add(("__EFMigrationsHistory", "ProductVersion"), ("character varying(32)", false));
        await using var columns = new NpgsqlCommand("""
            SELECT c.relname,a.attname,pg_catalog.format_type(a.atttypid,a.atttypmod),NOT a.attnotnull
            FROM pg_attribute a JOIN pg_class c ON c.oid=a.attrelid JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='public' AND c.relkind='r' AND a.attnum>0 AND NOT a.attisdropped
            """, (NpgsqlConnection)context.Database.GetDbConnection());
        await using var reader = await columns.ExecuteReaderAsync(ct);
        var seen = new HashSet<(string, string)>();
        while (await reader.ReadAsync(ct))
        {
            var key = (reader.GetString(0), reader.GetString(1));
            if (!expected.TryGetValue(key, out var value) || !seen.Add(key)
                || value.StoreType != reader.GetString(2) || value.IsNullable != reader.GetBoolean(3))
                throw new InvalidOperationException("IMPORT_SCHEMA_COLUMN_MISMATCH");
        }
        if (seen.Count != expected.Count) throw new InvalidOperationException("IMPORT_SCHEMA_COLUMN_MISMATCH");
    }

    private static async Task VerifyCheckpointsAsync(AppDbContext context, DictionaryImportRun run,
        DictionarySourceLock inputs, CancellationToken ct)
    {
        var checkpoints = await context.DictionaryImportCheckpoints.Where(x => x.RunId == run.RunId).ToListAsync(ct);
        var complete = run.State == "imported";
        if (checkpoints.Any(x => !inputs.Sources.Any(s => s.SourceId == x.SourceId)) ||
            complete && checkpoints.Count != inputs.Sources.Count ||
            await context.SourceManifests.CountAsync(ct) != checkpoints.Count ||
            await context.SourceRecords.LongCountAsync(ct) != checkpoints.Sum(x => x.AcceptedRecords))
            throw new InvalidOperationException("IMPORT_CHECKPOINT_MISMATCH");
        foreach (var checkpoint in checkpoints)
        {
            var source = inputs.Sources.Single(x => x.SourceId == checkpoint.SourceId);
            if (checkpoint.AcceptedRecords < 0 || checkpoint.AcceptedRecords > source.ExpectedRecords
                || complete && checkpoint.AcceptedRecords != source.ExpectedRecords)
                throw new InvalidOperationException("IMPORT_SOURCE_COUNT_MISMATCH");
            if (await context.SourceRecords.LongCountAsync(x => x.ManifestId == checkpoint.ManifestId, ct) != checkpoint.AcceptedRecords)
                throw new InvalidOperationException("IMPORT_CHECKPOINT_MISMATCH");
            var manifest = await context.SourceManifests.AsNoTracking().SingleAsync(x => x.Id == checkpoint.ManifestId, ct);
            var metadata = JsonSerializer.Serialize(new { source.ExpandedSha256, source.SchemaSha256, source.LicenseSha256,
                FingerprintSha256 = run.FingerprintHash });
            if (manifest.SourceName != source.SourceId || manifest.Publisher != source.Publisher || manifest.Version != source.Version
                || manifest.License != source.LicenseApprovalReference || manifest.Attribution != source.Attribution
                || manifest.Checksum != source.ArtifactSha256 || manifest.TransformationId != inputs.TransformationId
                || manifest.RecordCount != checkpoint.AcceptedRecords || manifest.RejectedCount != 0
                || CanonicalJson(manifest.Metadata) != CanonicalJson(metadata)
                || complete && manifest.ValidationResult != "isolated-imported-not-approved")
                throw new InvalidOperationException("IMPORT_MANIFEST_CHANGED");
            await VerifyStoredSourceAsync(context, source, checkpoint, complete, inputs.Scope, ct);
        }
        var expectedEntries = checkpoints.Where(x => inputs.Sources.Single(s => s.SourceId == x.SourceId).Kind == "jmdict").Sum(x => x.AcceptedRecords);
        var expectedKanji = checkpoints.Where(x => inputs.Sources.Single(s => s.SourceId == x.SourceId).Kind == "kanjidic2").Sum(x => x.AcceptedRecords);
        if (await context.DictionaryEntries.LongCountAsync(ct) != expectedEntries || await context.KanjiRecords.LongCountAsync(ct) != expectedKanji)
            throw new InvalidOperationException("IMPORT_CANONICAL_COUNT_MISMATCH");
    }

    public static async Task<string> ComputeFactsHashAsync(AppDbContext context, CancellationToken ct = default)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        // EARS[State] FR-008: Stable linguistic keys/facts only; no generated IDs, timestamps or checkpoint state.
        string? after = null;
        while (true)
        {
            var query = context.DictionaryEntries.AsNoTracking();
            if (after is not null) query = query.Where(x => string.Compare(x.CanonicalId, after) > 0);
            var entries = await query.OrderBy(x => x.CanonicalId).Take(100)
                .Include(x => x.WrittenForms).Include(x => x.Readings)
                .Include(x => x.Senses).ThenInclude(x => x.Glosses)
                .Include(x => x.Senses).ThenInclude(x => x.Applicabilities).AsSplitQuery().ToListAsync(ct);
            if (entries.Count == 0) break;
            foreach (var e in entries)
            {
                Append(hash, EntryFacts(e));
            }
            after = entries[^1].CanonicalId;
        }
        await foreach (var k in context.KanjiRecords.AsNoTracking().OrderBy(x => x.CanonicalId).AsAsyncEnumerable().WithCancellation(ct))
            Append(hash, KanjiFacts(k));
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static void Append(IncrementalHash hash, object value)
    {
        hash.AppendData(Encoding.UTF8.GetBytes(CanonicalJson(JsonSerializer.Serialize(value))));
        hash.AppendData("\n"u8);
    }

    private static string Chain(string previous, ParsedDictionaryRecord record) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(previous + "\n" + record.SourceIdentity + "\n" + CanonicalJson(record.RawJson))));

    private static object EntryFacts(DictionaryEntry entry) => new
    {
        entry.CanonicalId,
        Forms = entry.WrittenForms.OrderBy(x => x.Form, StringComparer.Ordinal).Select(x => new { x.Form, x.Priority, x.IsCommon, x.Info }),
        Readings = entry.Readings.OrderBy(x => x.ReadingText, StringComparer.Ordinal).Select(x => new { x.ReadingText, x.Priority, x.IsCommon, x.Info, x.RestrictedToForms, x.IsNoKanji }),
        Senses = entry.Senses.OrderBy(x => x.Position).Select(x => new
        {
            x.SenseKey, x.Position, x.PartOfSpeech,
            Glosses = x.Glosses.OrderBy(g => g.LanguageTag, StringComparer.Ordinal).ThenBy(g => g.Position)
                .Select(g => new { g.LanguageTag, g.GlossText, g.Position, g.ReviewStatus }),
            Pairs = x.Applicabilities.Select(a => new
            {
                Form = a.WrittenForm?.Form ?? (a.WrittenFormId is { } w ? entry.WrittenForms.Single(f => f.Id == w).Form : null),
                Reading = a.ReadingEntity?.ReadingText ?? (a.ReadingId is { } r ? entry.Readings.Single(f => f.Id == r).ReadingText : null)
            }).OrderBy(a => a.Form, StringComparer.Ordinal).ThenBy(a => a.Reading, StringComparer.Ordinal)
        })
    };

    private static object KanjiFacts(KanjiRecord k) => new
    {
        k.CanonicalId, k.Character, k.StrokeCount, k.Grade, k.JlptLevel, k.JlptProvenance,
        k.Frequency, k.UnicodeCodepoint, k.RadicalNumber, k.HanViet, k.OnReadings, k.KunReadings,
        k.MeaningsEn, k.MeaningsVi, k.NanoriReadings,
        SourceMetadata = CanonicalJson(k.SourceMetadata)
    };

    private static string CanonicalJson(string? json)
    {
        if (json is null) return "null";
        using var document = JsonDocument.Parse(json);
        using var memory = new MemoryStream();
        using (var writer = new Utf8JsonWriter(memory)) Write(document.RootElement, writer);
        return Encoding.UTF8.GetString(memory.ToArray());

        static void Write(JsonElement element, Utf8JsonWriter writer)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    Write(property.Value, writer);
                }
                writer.WriteEndObject();
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                writer.WriteStartArray();
                foreach (var value in element.EnumerateArray()) Write(value, writer);
                writer.WriteEndArray();
            }
            else element.WriteTo(writer);
        }
    }

    private static async Task VerifyStoredSourceAsync(AppDbContext context, DictionarySourcePin source,
        DictionaryImportCheckpoint checkpoint, bool complete, string scope, CancellationToken ct)
    {
        await using var file = new FileStream(source.ArtifactPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await VerifyArtifactAsync(file, source, ct);
        using var expanded = source.Compression == "gzip" ? new GZipStream(file, CompressionMode.Decompress, true) : null;
        var records = ReadRecords(expanded is null ? file : expanded, source, scope, ct);
        var batch = new List<ParsedDictionaryRecord>(100);
        long seen = 0;
        var chain = "";
        var last = "";
        foreach (var record in records)
        {
            if (seen == checkpoint.AcceptedRecords)
            {
                if (complete) throw new InvalidOperationException("IMPORT_SOURCE_COUNT_MISMATCH");
                break;
            }
            seen++;
            if (record.Ordinal != seen) throw new InvalidOperationException("IMPORT_ORDINAL_INVALID");
            chain = Chain(chain, record);
            last = record.SourceIdentity;
            batch.Add(record);
            if (batch.Count == 100)
            {
                await VerifyStoredBatchAsync(context, checkpoint.ManifestId, batch, ct);
                batch.Clear();
            }
        }
        if (batch.Count > 0) await VerifyStoredBatchAsync(context, checkpoint.ManifestId, batch, ct);
        if (seen != checkpoint.AcceptedRecords || last != checkpoint.LastSourceIdentity || chain != checkpoint.BatchChainHash)
            throw new InvalidOperationException("IMPORT_CHECKPOINT_CHANGED");
    }

    private static IEnumerable<ParsedDictionaryRecord> ReadRecords(Stream input, DictionarySourcePin source, string scope, CancellationToken ct) => source.Kind switch
    {
        "jmdict" => new JmdictParser(source.SchemaEntities, artifactSha256: scope == "ja-en" ? source.ArtifactSha256 : null).Read(input, ct),
        "kanjidic2" => new KanjidicParser().Read(input, ct),
        _ => throw new InvalidOperationException("IMPORT_SOURCE_KIND_UNSUPPORTED")
    };

    private static async Task VerifyStoredBatchAsync(AppDbContext context, long manifestId,
        List<ParsedDictionaryRecord> batch, CancellationToken ct)
    {
        var sourceIds = batch.Select(x => x.SourceIdentity).ToArray();
        var records = await context.SourceRecords.AsNoTracking().Where(x => x.ManifestId == manifestId && sourceIds.Contains(x.SourceIdentity))
            .ToDictionaryAsync(x => x.SourceIdentity, ct);
        var entryIds = batch.Where(x => x.Entry is not null).Select(x => x.Entry!.CanonicalId).ToArray();
        var entries = await context.DictionaryEntries.AsNoTracking().Where(x => entryIds.Contains(x.CanonicalId))
            .Include(x => x.WrittenForms).Include(x => x.Readings).Include(x => x.Senses).ThenInclude(x => x.Glosses)
            .Include(x => x.Senses).ThenInclude(x => x.Applicabilities).AsSplitQuery().ToDictionaryAsync(x => x.CanonicalId, ct);
        var kanjiIds = batch.Where(x => x.Kanji is not null).Select(x => x.Kanji!.CanonicalId).ToArray();
        var kanji = await context.KanjiRecords.AsNoTracking().Where(x => kanjiIds.Contains(x.CanonicalId)).ToDictionaryAsync(x => x.CanonicalId, ct);
        foreach (var expected in batch)
        {
            if (!records.TryGetValue(expected.SourceIdentity, out var raw) || CanonicalJson(raw.RecordData) != CanonicalJson(expected.RawJson))
                throw new InvalidOperationException("IMPORT_SOURCE_RECORD_CHANGED");
            if (expected.Entry is { } e)
            {
                if (!entries.TryGetValue(e.CanonicalId, out var actual)
                    || CanonicalJson(JsonSerializer.Serialize(EntryFacts(actual))) != CanonicalJson(JsonSerializer.Serialize(EntryFacts(e)))
                    || actual.Senses.SelectMany(x => x.Glosses).Any(x => x.SourceRecordId != raw.Id))
                    throw new InvalidOperationException("IMPORT_FACTS_CHANGED");
            }
            else if (expected.Kanji is { } k)
            {
                if (!kanji.TryGetValue(k.CanonicalId, out var actual)
                    || CanonicalJson(JsonSerializer.Serialize(KanjiFacts(actual))) != CanonicalJson(JsonSerializer.Serialize(KanjiFacts(k))))
                    throw new InvalidOperationException("IMPORT_FACTS_CHANGED");
            }
        }
    }

    private static async Task VerifyAllFilesAsync(DictionarySourceLock inputs, CancellationToken ct)
    {
        // The explicit ja-en profile needs no Vietnamese mapping; any declared mapping remains pinned.
        if (inputs.Scope != "ja-en" || !string.IsNullOrWhiteSpace(inputs.MappingPath)
            || !string.IsNullOrWhiteSpace(inputs.MappingSha256) || !string.IsNullOrWhiteSpace(inputs.MappingApprovalReference))
            await VerifySmallFileAsync(inputs.MappingPath, inputs.MappingSha256, 256L * 1024 * 1024, ct);
        foreach (var source in inputs.Sources)
        {
            await VerifySmallFileAsync(source.SchemaPath, source.SchemaSha256, 32L * 1024 * 1024, ct);
            await VerifySmallFileAsync(source.LicensePath, source.LicenseSha256, 32L * 1024 * 1024, ct);
            await using var file = new FileStream(source.ArtifactPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await VerifyArtifactAsync(file, source, ct);
        }
    }

    private static async Task VerifySmallFileAsync(string path, string expected, long maximum, CancellationToken ct)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length is < 1 || stream.Length > maximum
            || !string.Equals(Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, ct)), expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("IMPORT_PINNED_FILE_CHANGED");
    }

    private static async Task VerifyArtifactAsync(FileStream file, DictionarySourcePin source, CancellationToken ct)
    {
        file.Position = 0;
        if (source.ArtifactBytes < 1 || source.ExpandedBytes < 1 || source.MaxExpandedBytes < source.ExpandedBytes
            || source.MaxExpandedBytes > 8L * 1024 * 1024 * 1024 || source.Compression is not ("none" or "gzip")
            || file.Length != source.ArtifactBytes
            || !string.Equals(Convert.ToHexStringLower(await SHA256.HashDataAsync(file, ct)), source.ArtifactSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("IMPORT_ARTIFACT_CHANGED");
        file.Position = 0;
        using var gzip = source.Compression == "gzip" ? new GZipStream(file, CompressionMode.Decompress, true) : null;
        Stream expanded = gzip is null ? file : gzip;
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var bytes = new byte[65536];
        long count = 0;
        int read;
        while ((read = await expanded.ReadAsync(bytes, ct)) > 0)
        {
            count = checked(count + read);
            if (count > source.MaxExpandedBytes) throw new InvalidOperationException("IMPORT_EXPANDED_BOUNDS_EXCEEDED");
            hash.AppendData(bytes, 0, read);
        }
        if (count != source.ExpandedBytes || !string.Equals(Convert.ToHexStringLower(hash.GetHashAndReset()), source.ExpandedSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("IMPORT_EXPANDED_ARTIFACT_CHANGED");
        file.Position = 0;
    }

    private static void RequireInputs(SourceLockInspection inputs, string runId)
    {
        if (!inputs.IsReady || string.IsNullOrWhiteSpace(runId) || runId.Length > 80 ||
            inputs.SourceLockSha256.Length != 64 || inputs.FingerprintSha256.Length != 64)
            throw new InvalidOperationException("IMPORT_INPUTS_NOT_CLEARED");
    }

    private static AppDbContext Context(string connection) => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options);

    private static async Task<long> DatabaseOidAsync(AppDbContext context, CancellationToken ct)
    {
        var wasClosed = context.Database.GetDbConnection().State != System.Data.ConnectionState.Open;
        if (wasClosed) await context.Database.OpenConnectionAsync(ct);
        try
        {
            await using var command = new NpgsqlCommand("SELECT oid::bigint FROM pg_database WHERE datname=current_database()",
                (NpgsqlConnection)context.Database.GetDbConnection());
            return (long)(await command.ExecuteScalarAsync(ct))!;
        }
        finally { if (wasClosed) await context.Database.CloseConnectionAsync(); }
    }

    private static async Task LockAsync(AppDbContext context, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand($"SELECT pg_try_advisory_lock({WriterLock})", (NpgsqlConnection)context.Database.GetDbConnection());
        if (!(bool)(await command.ExecuteScalarAsync(ct))!) throw new InvalidOperationException("IMPORT_WRITER_BUSY");
    }

    private static async Task UnlockAsync(AppDbContext context)
    {
        if (context.Database.GetDbConnection().State != System.Data.ConnectionState.Open) return;
        await using var command = new NpgsqlCommand($"SELECT pg_advisory_unlock({WriterLock})", (NpgsqlConnection)context.Database.GetDbConnection());
        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }

    private static async Task<ImportMechanismReport> ReportAsync(AppDbContext context, DictionaryImportRun run, bool verifiedNoOp, CancellationToken ct) =>
        new(run.RunId, run.TargetDatabase, run.State, run.SourceLockHash, run.FingerprintHash, run.FactsHash,
            await context.DictionaryEntries.LongCountAsync(ct), await context.KanjiRecords.LongCountAsync(ct), verifiedNoOp,
            false, ["source-and-semantic-owner-review", "independent-corpus", "grammar-bootstrap", "local-cutover-approval"]);
}

