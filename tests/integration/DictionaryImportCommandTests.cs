namespace IntegrationTests;

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.DictionaryImport;
using FluentAssertions;
using Infrastructure.DictionaryImport;
using WebApi;
using Xunit;

public sealed class DictionaryImportCommandTests
{
    [Theory]
    [InlineData("--help", null, 0)]
    [InlineData("--dictionary-import", "--help", 0)]
    [InlineData("--dictionary-import", "unexpected-secret-value", 2)]
    public async Task HelpAndUnknownVerbNeverInvokeDatabaseHandler(string first, string? second, int expected)
    {
        var called = false;
        using var output = new StringWriter();
        var args = second is null ? new[] { first } : [first, second];
        var result = await DictionaryImportCommand.ExecuteAsync(args, output, (_, _, _) =>
        {
            called = true;
            throw new InvalidOperationException("Host=secret;Password=secret");
        }, TestContext.Current.CancellationToken);
        result.Should().Be(expected);
        called.Should().BeFalse();
        output.ToString().Should().NotContain("unexpected-secret-value").And.NotContain("Password=");
    }

    [Fact]
    public async Task ActualProgramHelpAndUnknownVerbExitBeforeHostOrDatabaseStartup()
    {
        foreach (var verb in new[] { "--help", "unknown-operator-verb" })
        {
            var start = new System.Diagnostics.ProcessStartInfo("dotnet")
            {
                WorkingDirectory = ConjugationCorpusFiles.Root, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            start.ArgumentList.Add(Path.Combine(ConjugationCorpusFiles.Root, "src/interface/bin/Release/net10.0/WebApi.dll"));
            start.ArgumentList.Add("--dictionary-import");
            start.ArgumentList.Add(verb);
            start.Environment["ConnectionStrings__DefaultConnection"] = "Host=forbidden.invalid;Password=DO_NOT_PRINT";
            using var process = System.Diagnostics.Process.Start(start)!;
            var text = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
            var errors = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
            await process.WaitForExitAsync(TestContext.Current.CancellationToken);
            process.ExitCode.Should().Be(verb == "--help" ? 0 : 2);
            (await text).Should().NotContain("Now listening").And.NotContain("DO_NOT_PRINT").And.NotContain("forbidden.invalid");
            (await errors).Should().BeEmpty();
        }
    }

    [Fact]
    public async Task ValidOfflineInspectionHashesAllFilesAndMakesNoWrites()
    {
        using var fixture = new LockFixture();
        var before = fixture.Files();
        var result = await SourceLockInspector.VerifyAsync(fixture.LockPath, TestContext.Current.CancellationToken);
        result.Blockers.Should().BeEmpty();
        result.IsReady.Should().BeTrue();
        result.SourceLockSha256.Should().HaveLength(64);
        result.FingerprintSha256.Should().HaveLength(64);
        fixture.Files().Should().BeEquivalentTo(before);
    }

    [Fact]
    public async Task ExplicitEnglishScopeAllowsBothSourcesWithoutVietnameseMapping()
    {
        using var fixture = new LockFixture();
        fixture.Write(fixture.Lock with
        {
            Scope = "ja-en", Sources = fixture.Lock.Sources.Where(x => x.Kind is "jmdict" or "kanjidic2").ToArray(),
            MappingPath = "", MappingSha256 = "", MappingApprovalReference = ""
        });
        var inspection = await SourceLockInspector.VerifyAsync(fixture.LockPath, TestContext.Current.CancellationToken);
        inspection.Blockers.Should().BeEmpty();
        inspection.Lock!.Scope.Should().Be("ja-en");
    }

    [Fact]
    public async Task EnglishScopeRejectsIncompleteMappingAndAdditionalSources()
    {
        using var fixture = new LockFixture();
        fixture.Write(fixture.Lock with
        {
            Scope = "ja-en", MappingApprovalReference = "", MappingSha256 = ""
        });
        var inspection = await SourceLockInspector.VerifyAsync(fixture.LockPath, TestContext.Current.CancellationToken);
        inspection.Blockers.Select(b => b.ErrorCode).Should().Contain(["mapping_unapproved", "mapping_unavailable", "source_scope_mismatch"]);
    }

    [Fact]
    public async Task EnglishScopeRejectsChangedOptionalMapping()
    {
        using var fixture = new LockFixture();
        fixture.Write(fixture.Lock with { Scope = "ja-en", Sources = fixture.Lock.Sources.Take(2).ToArray() });
        File.WriteAllText(Path.Combine(fixture.Root, "mapping.json"), "changed");
        var inspection = await SourceLockInspector.VerifyAsync(fixture.LockPath, TestContext.Current.CancellationToken);
        inspection.Blockers.Select(b => b.ErrorCode).Should().Contain("mapping_hash_mismatch");
    }

    [Fact]
    public async Task OriginalLocksStillRequireAllFourSources()
    {
        using var fixture = new LockFixture();
        fixture.Write(fixture.Lock with { Sources = fixture.Lock.Sources.Take(2).ToArray() });
        var inspection = await SourceLockInspector.VerifyAsync(fixture.LockPath, TestContext.Current.CancellationToken);
        inspection.Blockers.Count(b => b.ErrorCode == "source_group_missing").Should().Be(2);
    }

    [Theory]
    [InlineData("ja-en")]
    [InlineData("unknown")]
    public async Task EnglishScopeRequiresBothSourceGroupsAndUnknownScopeIsRejected(string scope)
    {
        using var fixture = new LockFixture();
        fixture.Write(fixture.Lock with { Scope = scope, Sources = fixture.Lock.Sources.Take(1).ToArray() });
        var inspection = await SourceLockInspector.VerifyAsync(fixture.LockPath, TestContext.Current.CancellationToken);
        inspection.Blockers.Select(b => b.ErrorCode).Should().Contain(scope == "ja-en" ? "source_group_missing" : "source_scope_invalid");
    }

    [Fact]
    public async Task ReadySourcePinsDoNotClaimCanonicalDataIsReady()
    {
        using var fixture = new LockFixture();
        using var output = new StringWriter();
        var result = await DictionaryImportCommand.ExecuteAsync(
            ["--dictionary-import", "inspect", "--source-lock", fixture.LockPath], output,
            ct: TestContext.Current.CancellationToken);
        result.Should().Be(0);
        using var report = JsonDocument.Parse(output.ToString());
        report.RootElement.GetProperty("source_inputs_ready").GetBoolean().Should().BeTrue();
        report.RootElement.GetProperty("canonical_ready").GetBoolean().Should().BeFalse();
        report.RootElement.GetProperty("implementation_blockers").EnumerateArray()
            .Select(value => value.GetString()).Should().Contain("reviewed_mapping_writer_not_implemented");
    }

    [Fact]
    public async Task IncompleteLockAndPendingLicenseBlockBeforeAnyHandler()
    {
        using var fixture = new LockFixture();
        fixture.Write(fixture.Lock with
        {
            MappingApprovalReference = "",
            Sources = fixture.Lock.Sources.Select(pin => pin with { LicenseStatus = "pending" }).ToArray()
        });
        var called = false;
        using var output = new StringWriter();
        var result = await DictionaryImportCommand.ExecuteAsync(
            ["--dictionary-import", "import", "--source-lock", fixture.LockPath, "--run-id", "fixture", "--confirm-target", "jp_dict_real_fixture"], output,
            (_, _, _) => { called = true; throw new InvalidOperationException(); }, TestContext.Current.CancellationToken);
        result.Should().Be(3);
        called.Should().BeFalse();
        output.ToString().Should().Contain("license_unapproved").And.Contain("mapping_unapproved").And.Contain("\"state\":\"blocked\"");
    }

    [Fact]
    public async Task ChangedArtifactAndLicenseAreRefusedWithSanitizedCodes()
    {
        using var fixture = new LockFixture();
        File.WriteAllText(Path.Combine(fixture.Root, "artifact.txt"), "credential=do-not-echo");
        File.WriteAllText(Path.Combine(fixture.Root, "license.txt"), "modified-license");
        var result = await SourceLockInspector.VerifyAsync(fixture.LockPath, TestContext.Current.CancellationToken);
        result.Blockers.Should().Contain(b => b.ErrorCode == "artifact_hash_mismatch");
        result.Blockers.Should().Contain(b => b.ErrorCode == "license_hash_mismatch");
        JsonSerializer.Serialize(result.Blockers).Should().NotContain("credential").And.NotContain(fixture.Root);
    }

    [Fact]
    public async Task GzipExpansionIsBoundedAndExpandedChecksumVerified()
    {
        using var fixture = new LockFixture();
        var expanded = Encoding.UTF8.GetBytes(new string('x', 100_000));
        var compressedPath = Path.Combine(fixture.Root, "artifact.gz");
        await using (var file = File.Create(compressedPath))
        await using (var gzip = new GZipStream(file, CompressionLevel.SmallestSize))
            await gzip.WriteAsync(expanded, TestContext.Current.CancellationToken);
        var pin = fixture.Lock.Sources[0] with
        {
            ArtifactPath = "artifact.gz", Compression = "gzip", ArtifactBytes = new FileInfo(compressedPath).Length,
            ArtifactSha256 = LockFixture.Hash(File.ReadAllBytes(compressedPath)), ExpandedBytes = 100,
            MaxExpandedBytes = 100, ExpandedSha256 = LockFixture.Hash(expanded)
        };
        fixture.Write(fixture.Lock with { Sources = [pin, .. fixture.Lock.Sources.Skip(1)] });
        var result = await SourceLockInspector.VerifyAsync(fixture.LockPath, TestContext.Current.CancellationToken);
        result.Blockers.Should().Contain(b => b.ErrorCode == "artifact_unreadable");
        result.IsReady.Should().BeFalse();
    }

    [Fact]
    public async Task TraversalMissingSourceGroupAndUnpinnedMetadataFailClosed()
    {
        using var fixture = new LockFixture();
        var pin = fixture.Lock.Sources[0] with { ArtifactPath = "../outside.xml", Version = "latest" };
        fixture.Write(fixture.Lock with { Sources = [pin] });
        var result = await SourceLockInspector.VerifyAsync(fixture.LockPath, TestContext.Current.CancellationToken);
        result.Blockers.Select(b => b.ErrorCode).Should().Contain(["source_group_missing", "artifact_unavailable", "source_metadata_unpinned"]);
    }

    [Theory]
    [InlineData("--batch-size", "0")]
    [InlineData("--batch-size", "2001")]
    [InlineData("--source-lock", "duplicate")]
    public async Task InvalidArgumentsCannotReadOrInvokeHandler(string option, string value)
    {
        using var output = new StringWriter();
        (await DictionaryImportCommand.ExecuteAsync(
            ["--dictionary-import", "import", "--source-lock", "not-existing", "--run-id", "fixture", "--confirm-target", "jp_dict_real_fixture", option, value],
            output, ct: TestContext.Current.CancellationToken)).Should().Be(2);
    }

    [Fact]
    public async Task ReadinessDoesNotPretendToImportWithoutEngine()
    {
        using var fixture = new LockFixture();
        using var output = new StringWriter();
        (await DictionaryImportCommand.ExecuteAsync(
            ["--dictionary-import", "import", "--source-lock", fixture.LockPath, "--run-id", "fixture", "--confirm-target", "jp_dict_real_fixture"],
            output, ct: TestContext.Current.CancellationToken)).Should().Be(4);
        output.ToString().Should().Contain("operation_unavailable");
    }

    [Fact]
    public async Task PrepareRequiresExactTargetBeforeInspectingFiles()
    {
        using var output = new StringWriter();
        var called = false;
        var result = await DictionaryImportCommand.ExecuteAsync(
            ["--dictionary-import", "prepare", "--source-lock", "not-existing", "--run-id", "fixture"],
            output, (_, _, _) => { called = true; throw new InvalidOperationException(); }, TestContext.Current.CancellationToken);
        result.Should().Be(2);
        called.Should().BeFalse();
        output.ToString().Should().Contain("missing_target_confirmation");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpgradeRequiresExactTargetAndRefusesBatchOptions(bool withBatchSize)
    {
        using var output = new StringWriter();
        var called = false;
        string[] args = withBatchSize
            ? ["--dictionary-import", "upgrade", "--source-lock", "not-existing", "--run-id", "fixture", "--confirm-target", "jp_dict_real_fixture", "--batch-size", "100"]
            : ["--dictionary-import", "upgrade", "--source-lock", "not-existing", "--run-id", "fixture"];
        var result = await DictionaryImportCommand.ExecuteAsync(args, output,
            (_, _, _) => { called = true; throw new InvalidOperationException(); }, TestContext.Current.CancellationToken);
        result.Should().Be(2);
        called.Should().BeFalse();
        output.ToString().Should().Contain(withBatchSize ? "invalid_arguments" : "missing_target_confirmation");
    }

    [Fact]
    public async Task UpgradeIsDocumentedAndDispatchesWithItsConfirmedTarget()
    {
        using var help = new StringWriter();
        (await DictionaryImportCommand.ExecuteAsync(["--help"], help, ct: TestContext.Current.CancellationToken)).Should().Be(0);
        help.ToString().Should().Contain("--dictionary-import upgrade --source-lock <file> --run-id <id> --confirm-target <exact-db-name>");

        using var fixture = new LockFixture();
        using var output = new StringWriter();
        DictionaryImportInvocation? received = null;
        var result = await DictionaryImportCommand.ExecuteAsync(
            ["--dictionary-import", "upgrade", "--source-lock", fixture.LockPath, "--run-id", "fixture", "--confirm-target", "jp_dict_real_fixture"],
            output, (invocation, _, _) =>
            {
                received = invocation;
                return Task.FromResult(new DictionaryImportCommandResult(0, new { state = "imported" }));
            }, TestContext.Current.CancellationToken);
        result.Should().Be(0);
        received!.Verb.Should().Be("upgrade");
        received.ConfirmTarget.Should().Be("jp_dict_real_fixture");
    }

    [Fact]
    public async Task PrepareDispatchesOnlyAfterAllPinsAreVerified()
    {
        using var fixture = new LockFixture();
        using var output = new StringWriter();
        DictionaryImportInvocation? received = null;
        var result = await DictionaryImportCommand.ExecuteAsync(
            ["--dictionary-import", "prepare", "--source-lock", fixture.LockPath, "--run-id", "fixture", "--confirm-target", "jp_dict_real_fixture"],
            output, (invocation, inspection, _) =>
            {
                inspection.IsReady.Should().BeTrue();
                received = invocation;
                return Task.FromResult(new DictionaryImportCommandResult(0, new { state = "prepared" }));
            }, TestContext.Current.CancellationToken);
        result.Should().Be(0);
        received.Should().NotBeNull();
        received!.Verb.Should().Be("prepare");
        received.ConfirmTarget.Should().Be("jp_dict_real_fixture");
        output.ToString().Should().Contain("\"state\":\"prepared\"");
    }

    [Fact]
    public async Task CancelledInspectionReturnsSanitizedOperatorCancellation()
    {
        using var fixture = new LockFixture();
        using var output = new StringWriter();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        (await DictionaryImportCommand.ExecuteAsync(["--dictionary-import", "inspect", "--source-lock", fixture.LockPath],
            output, ct: cancellation.Token)).Should().Be(130);
        output.ToString().Should().Contain("\"error_code\":\"cancelled\"").And.NotContain(fixture.Root);
    }

    private sealed class LockFixture : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "dictionary-lock-test-" + Guid.NewGuid().ToString("N"));
        public string LockPath => Path.Combine(Root, "source-lock.json");
        public DictionarySourceLock Lock { get; }

        public LockFixture()
        {
            Directory.CreateDirectory(Root);
            File.WriteAllText(Path.Combine(Root, "artifact.txt"), "synthetic-owned-fixture");
            File.WriteAllText(Path.Combine(Root, "schema.txt"), "fixture-schema-1");
            File.WriteAllText(Path.Combine(Root, "license.txt"), "Project-owned synthetic fixture for tests only.");
            File.WriteAllText(Path.Combine(Root, "mapping.json"), "[]");
            var artifact = File.ReadAllBytes(Path.Combine(Root, "artifact.txt"));
            Lock = new()
            {
                TransformationId = "synthetic-test-v1", TransformationSha256 = Hash(Encoding.UTF8.GetBytes("synthetic-transform")),
                MappingPath = "mapping.json", MappingSha256 = Hash(File.ReadAllBytes(Path.Combine(Root, "mapping.json"))),
                MappingApprovalReference = "synthetic-test-decision-only",
                Sources = new[] { "jmdict", "kanjidic2", "vietnamese", "overrides" }.Select(kind => new DictionarySourcePin
                {
                    SourceId = kind, Kind = kind, Publisher = "synthetic-test", Version = "fixture-1",
                    ArtifactUrl = "https://example.invalid/fixture-1.txt", ArtifactPath = "artifact.txt", ArtifactBytes = artifact.Length,
                    ArtifactSha256 = Hash(artifact), ExpandedSha256 = Hash(artifact), ExpandedBytes = artifact.Length,
                    MaxExpandedBytes = artifact.Length, ExpectedRecords = 1, SchemaPath = "schema.txt",
                    SchemaSha256 = Hash(File.ReadAllBytes(Path.Combine(Root, "schema.txt"))), LicensePath = "license.txt",
                    LicenseSha256 = Hash(File.ReadAllBytes(Path.Combine(Root, "license.txt"))),
                    LicenseStatus = "approved", LicenseApprovalReference = "synthetic-test-only-not-upstream-clearance",
                    Attribution = "Project-owned synthetic fixtures"
                }).ToArray()
            };
            Write(Lock);
        }

        public void Write(DictionarySourceLock value) => File.WriteAllText(LockPath, JsonSerializer.Serialize(value, SourceLockInspector.JsonOptions));
        public IReadOnlyDictionary<string, string> Files() => Directory.GetFiles(Root).ToDictionary(path => Path.GetFileName(path)!, path => Hash(File.ReadAllBytes(path)));
        public static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
