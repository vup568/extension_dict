using System.Security.Cryptography;
using System.Text;
using Application.DictionaryImport;
using Infrastructure.DictionaryImport;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace IntegrationTests;

public sealed class DictionaryImportStoreTests(PostgreSqlFixture postgres) : IClassFixture<PostgreSqlFixture>
{
    [Theory]
    [InlineData("jp_dict_demo", "127.0.0.1")]
    [InlineData("postgres", "127.0.0.1")]
    [InlineData("jp_dict_real_test", "example.org")]
    [InlineData("jp_dict_real_x;DROP", "127.0.0.1")]
    public void ProtectedOrRemoteTargetsAreRefusedBeforeConnecting(string database, string host)
    {
        var builder = new NpgsqlConnectionStringBuilder { Host = host, Database = database };
        Assert.Throws<InvalidOperationException>(() => IsolatedImportTarget.Check(builder.ConnectionString, database));
    }

    [Fact]
    public async Task FreshTargetIsRequiredAndRebuildIsDeterministic()
    {
        using var fixture = new SourceFixture();
        var first = Target();
        await DictionaryImportStore.PrepareAsync(first.Connection, first.Name, "run", fixture.Inputs, ct: TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DictionaryImportStore.PrepareAsync(first.Connection, first.Name, "run", fixture.Inputs, ct: TestContext.Current.CancellationToken));
        var report = await DictionaryImportStore.ImportAsync(first.Connection, first.Name, "run", fixture.Inputs, batchSize: 1, ct: TestContext.Current.CancellationToken);
        Assert.Equal(3, report.DictionaryEntries);
        Assert.False(report.ReadyForLocalUse);
        var repeated = await DictionaryImportStore.ImportAsync(first.Connection, first.Name, "run", fixture.Inputs, ct: TestContext.Current.CancellationToken);
        Assert.True(repeated.VerifiedNoOp);
        Assert.Equal(report.FactsHash, repeated.FactsHash);
        var second = Target();
        await DictionaryImportStore.PrepareAsync(second.Connection, second.Name, "rebuild", fixture.Inputs, ct: TestContext.Current.CancellationToken);
        var rebuilt = await DictionaryImportStore.ImportAsync(second.Connection, second.Name, "rebuild", fixture.Inputs, batchSize: 2, ct: TestContext.Current.CancellationToken);
        Assert.Equal(report.FactsHash, rebuilt.FactsHash);
        await using var db = Context(first.Connection);
        Assert.Equal(3, await db.SourceRecords.CountAsync(TestContext.Current.CancellationToken));
        Assert.All(await db.LocalizedGlosses.ToListAsync(TestContext.Current.CancellationToken), g => Assert.NotNull(g.SourceRecordId));
    }

    [Fact]
    public async Task EnglishScopeImportsVocabularyAndKanjiWithoutVietnameseMapping()
    {
        using var fixture = new SourceFixture(includeKanji: true);
        var target = Target();
        await DictionaryImportStore.PrepareAsync(target.Connection, target.Name, "english", fixture.Inputs, ct: TestContext.Current.CancellationToken);
        var report = await DictionaryImportStore.ImportAsync(target.Connection, target.Name, "english", fixture.Inputs,
            batchSize: 1, ct: TestContext.Current.CancellationToken);
        Assert.Equal(3, report.DictionaryEntries);
        Assert.Equal(1, report.KanjiCharacters);
        await using var db = Context(target.Connection);
        Assert.Equal(4, await db.SourceRecords.CountAsync(TestContext.Current.CancellationToken));
        var kanji = await db.KanjiRecords.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("日", kanji.Character);
        Assert.Contains("sun", kanji.MeaningsEn!);
        Assert.Empty(kanji.MeaningsVi!);
        Assert.Equal("nhật", kanji.HanViet);
        var retainedEntry = await db.DictionaryEntries.Include(e => e.Senses).ThenInclude(s => s.Applicabilities)
            .SingleAsync(e => e.CanonicalId == "dictionary:jmdict:1", TestContext.Current.CancellationToken);
        Assert.Single(retainedEntry.Senses.Single(s => s.Position == 2).Applicabilities);
        var verified = await DictionaryImportStore.ValidateAsync(target.Connection, target.Name, "english", fixture.Inputs,
            ct: TestContext.Current.CancellationToken);
        Assert.Equal(report.FactsHash, verified.FactsHash);
    }

    [Fact]
    public async Task CancelledBatchResumesOnlyWithSameInputsAndNoDuplicates()
    {
        using var fixture = new SourceFixture();
        var target = Target();
        await DictionaryImportStore.PrepareAsync(target.Connection, target.Name, "resume", fixture.Inputs, ct: TestContext.Current.CancellationToken);
        using var cancelled = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => DictionaryImportStore.ImportAsync(
            target.Connection, target.Name, "resume", fixture.Inputs, batchSize: 1, ct: cancelled.Token,
            progress: new InlineProgress(_ => cancelled.Cancel())));
        await using (var db = Context(target.Connection))
        {
            Assert.Equal(1, await db.DictionaryEntries.CountAsync(TestContext.Current.CancellationToken));
            Assert.Equal(1, (await db.DictionaryImportCheckpoints.SingleAsync(TestContext.Current.CancellationToken)).AcceptedRecords);
        }
        var changed = fixture.Inputs with { FingerprintSha256 = new string('b', 64) };
        await Assert.ThrowsAsync<InvalidOperationException>(() => DictionaryImportStore.ImportAsync(
            target.Connection, target.Name, "resume", changed, resume: true, ct: TestContext.Current.CancellationToken));
        var report = await DictionaryImportStore.ImportAsync(target.Connection, target.Name, "resume", fixture.Inputs, resume: true, batchSize: 1, ct: TestContext.Current.CancellationToken);
        Assert.Equal(3, report.DictionaryEntries);
        await using var final = Context(target.Connection);
        Assert.Equal(3, await final.SourceRecords.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ChangedCanonicalFactsAndSealedRunsAreRefused()
    {
        using var fixture = new SourceFixture();
        var target = Target();
        await DictionaryImportStore.PrepareAsync(target.Connection, target.Name, "tamper", fixture.Inputs, ct: TestContext.Current.CancellationToken);
        await DictionaryImportStore.ImportAsync(target.Connection, target.Name, "tamper", fixture.Inputs, ct: TestContext.Current.CancellationToken);
        await using var db = Context(target.Connection);
        var gloss = await db.LocalizedGlosses.FirstAsync(TestContext.Current.CancellationToken);
        gloss.GlossText = "tampered";
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => DictionaryImportStore.ValidateAsync(target.Connection, target.Name, "tamper", fixture.Inputs, ct: TestContext.Current.CancellationToken));
        var run = await db.DictionaryImportRuns.SingleAsync(TestContext.Current.CancellationToken);
        run.State = "sealed";
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => DictionaryImportStore.ImportAsync(target.Connection, target.Name, "tamper", fixture.Inputs, resume: true, ct: TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("gloss")]
    [InlineData("raw")]
    [InlineData("manifest")]
    public async Task ResumeRefusesTamperedCommittedChildrenRawRecordsAndProvenance(string change)
    {
        using var fixture = new SourceFixture();
        var target = Target();
        await DictionaryImportStore.PrepareAsync(target.Connection, target.Name, "interrupted", fixture.Inputs, ct: TestContext.Current.CancellationToken);
        using var cancelled = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => DictionaryImportStore.ImportAsync(
            target.Connection, target.Name, "interrupted", fixture.Inputs, batchSize: 1, ct: cancelled.Token,
            progress: new InlineProgress(_ => cancelled.Cancel())));
        await using (var db = Context(target.Connection))
        {
            if (change == "gloss") (await db.LocalizedGlosses.SingleAsync(TestContext.Current.CancellationToken)).GlossText = "changed child";
            if (change == "raw") (await db.SourceRecords.SingleAsync(TestContext.Current.CancellationToken)).RecordData = "{\"unexpected\":true}";
            if (change == "manifest") (await db.SourceManifests.SingleAsync(TestContext.Current.CancellationToken)).Version = "unapproved-version";
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => DictionaryImportStore.ImportAsync(
            target.Connection, target.Name, "interrupted", fixture.Inputs, resume: true, ct: TestContext.Current.CancellationToken));
        await using var final = Context(target.Connection);
        Assert.Equal(1, await final.DictionaryEntries.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CompletedRunCannotBecomeValidatedAfterDeletingAnEntireSourceGraph()
    {
        using var fixture = new SourceFixture();
        var target = Target();
        await DictionaryImportStore.PrepareAsync(target.Connection, target.Name, "removed", fixture.Inputs, ct: TestContext.Current.CancellationToken);
        await DictionaryImportStore.ImportAsync(target.Connection, target.Name, "removed", fixture.Inputs, ct: TestContext.Current.CancellationToken);
        await using (var db = Context(target.Connection))
        {
            await db.DictionaryEntries.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await db.DictionaryImportCheckpoints.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await db.SourceManifests.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => DictionaryImportStore.ValidateAsync(
            target.Connection, target.Name, "removed", fixture.Inputs, ct: TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("CREATE SCHEMA unrelated")]
    [InlineData("CREATE FUNCTION public.unrelated() RETURNS int LANGUAGE SQL AS 'SELECT 1'")]
    [InlineData("CREATE OR REPLACE FUNCTION public.guard_published_knowledge_release() RETURNS trigger LANGUAGE plpgsql AS 'BEGIN RETURN NEW; END'")]
    [InlineData("ALTER TABLE conjugation_datasets DISABLE TRIGGER conjugation_dataset_immutable")]
    [InlineData("ALTER TABLE dictionary_entries ADD COLUMN unrelated_data text")]
    [InlineData("ALTER TABLE dictionary_entries ALTER COLUMN canonical_id DROP NOT NULL")]
    public async Task UnknownNamespaceAndRoutineAreRefused(string statement)
    {
        using var fixture = new SourceFixture();
        var target = Target();
        await DictionaryImportStore.PrepareAsync(target.Connection, target.Name, "unknown", fixture.Inputs, ct: TestContext.Current.CancellationToken);
        await using var connection = new NpgsqlConnection(target.Connection);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using (var command = new NpgsqlCommand(statement, connection)) await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => DictionaryImportStore.ImportAsync(
            target.Connection, target.Name, "unknown", fixture.Inputs, ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UnknownSchemaAndConcurrentWriterAreRefused()
    {
        using var fixture = new SourceFixture();
        var target = Target();
        await DictionaryImportStore.PrepareAsync(target.Connection, target.Name, "guard", fixture.Inputs, ct: TestContext.Current.CancellationToken);
        await using var connection = new NpgsqlConnection(target.Connection);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using (var writer = new NpgsqlCommand("SELECT pg_advisory_lock(81708626447696)", connection))
        {
            // Lock exact key used by importer, acquired in another live session.
            writer.CommandText = $"SELECT pg_advisory_lock({0x4A5044494D50L})";
            await writer.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            await Assert.ThrowsAsync<InvalidOperationException>(() => DictionaryImportStore.ImportAsync(target.Connection, target.Name, "guard", fixture.Inputs, ct: TestContext.Current.CancellationToken));
        }
        await using (var unlock = new NpgsqlCommand($"SELECT pg_advisory_unlock({0x4A5044494D50L})", connection)) await unlock.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        await using (var unknown = new NpgsqlCommand("CREATE TABLE unrelated_data(id int)", connection)) await unknown.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => DictionaryImportStore.ImportAsync(target.Connection, target.Name, "guard", fixture.Inputs, ct: TestContext.Current.CancellationToken));
        await using var db = Context(target.Connection);
        Assert.Empty(await db.DictionaryEntries.ToListAsync(TestContext.Current.CancellationToken));
    }

    private const string DatasetMigration = "20261118075656_AddYcoConjugationCandidate";

    [Fact]
    public async Task UpgradeAddsOnlyTheMissingDatasetMigrationAndKeepsFacts()
    {
        using var fixture = new SourceFixture(includeKanji: true);
        var target = Target();
        await DictionaryImportStore.PrepareAsync(target.Connection, target.Name, "upgrade", fixture.Inputs, ct: TestContext.Current.CancellationToken);
        var imported = await DictionaryImportStore.ImportAsync(target.Connection, target.Name, "upgrade", fixture.Inputs, ct: TestContext.Current.CancellationToken);
        await RemoveMigrationsAsync(target.Connection, DatasetMigration);
        var refused = await Assert.ThrowsAsync<InvalidOperationException>(() => DictionaryImportStore.ValidateAsync(
            target.Connection, target.Name, "upgrade", fixture.Inputs, ct: TestContext.Current.CancellationToken));
        Assert.Equal("IMPORT_SCHEMA_MISMATCH", refused.Message);

        var upgraded = await DictionaryImportStore.UpgradeSchemaAsync(target.Connection, target.Name, "upgrade", fixture.Inputs, TestContext.Current.CancellationToken);

        Assert.False(upgraded.VerifiedNoOp);
        Assert.Equal(imported.FactsHash, upgraded.FactsHash);
        var validated = await DictionaryImportStore.ValidateAsync(target.Connection, target.Name, "upgrade", fixture.Inputs, ct: TestContext.Current.CancellationToken);
        Assert.Equal(imported.FactsHash, validated.FactsHash);
        await using (var db = Context(target.Connection))
        {
            Assert.Equal(db.Database.GetMigrations(), await db.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken));
            Assert.True(await db.ConjugationDatasets.AnyAsync(d => d.Version == "candidate-20261110.1", TestContext.Current.CancellationToken));
        }
        var repeated = await DictionaryImportStore.UpgradeSchemaAsync(target.Connection, target.Name, "upgrade", fixture.Inputs, TestContext.Current.CancellationToken);
        Assert.True(repeated.VerifiedNoOp);
    }

    [Theory]
    [InlineData("schema", "IMPORT_SCHEMA_UPGRADE_REFUSED")]
    [InlineData("gap", "IMPORT_SCHEMA_MISMATCH")]
    [InlineData("facts", "IMPORT_FACTS_CHANGED")]
    public async Task UpgradeRefusesUnsafeCandidatesBeforeApplyingAnything(string fault, string expected)
    {
        using var fixture = new SourceFixture(includeKanji: true);
        var target = Target();
        await DictionaryImportStore.PrepareAsync(target.Connection, target.Name, "refused", fixture.Inputs, ct: TestContext.Current.CancellationToken);
        await DictionaryImportStore.ImportAsync(target.Connection, target.Name, "refused", fixture.Inputs, ct: TestContext.Current.CancellationToken);
        string[] removed;
        await using (var db = Context(target.Connection))
        {
            var all = db.Database.GetMigrations().ToList();
            // A missing schema migration, or a hole in the history, must never be filled in by an upgrade.
            removed = fault switch
            {
                "schema" => [all[^2], DatasetMigration],
                "gap" => [all[^2]],
                _ => [DatasetMigration],
            };
        }
        await RemoveMigrationsAsync(target.Connection, removed);
        if (fault == "facts")
        {
            await using var db = Context(target.Connection);
            (await db.LocalizedGlosses.FirstAsync(TestContext.Current.CancellationToken)).GlossText = "tampered";
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => DictionaryImportStore.UpgradeSchemaAsync(
            target.Connection, target.Name, "refused", fixture.Inputs, TestContext.Current.CancellationToken));

        Assert.Equal(expected, error.Message);
        await using var after = Context(target.Connection);
        var applied = await after.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(applied, id => removed.Contains(id));
        if (removed.Contains(DatasetMigration))
            Assert.False(await after.ConjugationDatasets.AnyAsync(d => d.Version == "candidate-20261110.1", TestContext.Current.CancellationToken));
    }

    // Recreates a candidate imported before the given migrations existed: their history rows and, for the
    // dataset migration, its inserted dataset row are removed (the immutability trigger is paused only for that).
    private static async Task RemoveMigrationsAsync(string connection, params string[] migrations)
    {
        await using var db = new NpgsqlConnection(connection);
        await db.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand("""
            ALTER TABLE conjugation_datasets DISABLE TRIGGER conjugation_dataset_immutable;
            DELETE FROM conjugation_datasets WHERE version = 'candidate-20261110.1' AND @dataset;
            ALTER TABLE conjugation_datasets ENABLE TRIGGER conjugation_dataset_immutable;
            DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" = ANY(@migrations);
            """, db);
        command.Parameters.AddWithValue("dataset", migrations.Contains(DatasetMigration));
        command.Parameters.AddWithValue("migrations", migrations);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private (string Connection, string Name) Target()
    {
        var name = "jp_dict_real_" + Guid.NewGuid().ToString("N");
        return (new NpgsqlConnectionStringBuilder(postgres.ConnectionString) { Database = name }.ConnectionString, name);
    }

    private static AppDbContext Context(string connection) => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options);
    private sealed class InlineProgress(Action<ImportBatchProgress> report) : IProgress<ImportBatchProgress>
    {
        public void Report(ImportBatchProgress value) => report(value);
    }

    private sealed class SourceFixture : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "jp-import-test-" + Guid.NewGuid().ToString("N"));
        public SourceLockInspection Inputs { get; }
        public SourceFixture(bool includeKanji = false)
        {
            Directory.CreateDirectory(directory);
            var xml = "<JMdict>" + string.Concat(Enumerable.Range(1, 3).Select(i =>
                includeKanji && i == 1
                    ? "<entry><ent_seq>1</ent_seq><k_ele><keb>仮名</keb></k_ele><r_ele><reb>かな1</reb></r_ele><r_ele><reb>カナ</reb><re_nokanji/></r_ele><sense><gloss>fixture 1</gloss></sense><sense><stagk>仮名</stagk><stagr>かな1</stagr><gloss>restricted fixture</gloss></sense></entry>"
                    : $"<entry><ent_seq>{i}</ent_seq><r_ele><reb>かな{i}</reb></r_ele><sense><gloss>fixture {i}</gloss></sense></entry>")) + "</JMdict>";
            var path = Path.Combine(directory, "source.xml");
            File.WriteAllText(path, xml, new UTF8Encoding(false));
            var schemaPath = Path.Combine(directory, "schema.txt");
            var licensePath = Path.Combine(directory, "license.txt");
            var mappingPath = Path.Combine(directory, "mapping.json");
            File.WriteAllText(schemaPath, "Synthetic fixture schema v1", new UTF8Encoding(false));
            File.WriteAllText(licensePath, "Project-authored synthetic test fixture", new UTF8Encoding(false));
            File.WriteAllText(mappingPath, "[]", new UTF8Encoding(false));
            static string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
            var source = new DictionarySourcePin
            {
                SourceId = "fixture-jmdict", Kind = "jmdict", Version = "synthetic-v1", Publisher = "project test fixture",
                ArtifactUrl = "https://example.invalid/synthetic-v1.xml", ArtifactPath = path, ArtifactSha256 = Hash(path),
                ArtifactBytes = new FileInfo(path).Length, ExpandedBytes = new FileInfo(path).Length,
                ExpandedSha256 = Hash(path), MaxExpandedBytes = new FileInfo(path).Length,
                ExpectedRecords = 3, LicenseApprovalReference = "synthetic-test-only", LicenseStatus = "approved",
                Attribution = "project-authored fixture", SchemaPath = schemaPath, SchemaSha256 = Hash(schemaPath),
                LicensePath = licensePath, LicenseSha256 = Hash(licensePath)
            };
            var sources = new List<DictionarySourcePin> { source };
            if (includeKanji)
            {
                var kanjiPath = Path.Combine(directory, "kanjidic2.xml");
                File.WriteAllText(kanjiPath, "<kanjidic2><header><file_version>fixture</file_version></header><character><literal>日</literal><codepoint><cp_value cp_type=\"ucs\">65e5</cp_value></codepoint><reading_meaning><rmgroup><reading r_type=\"vietnam\">nhật</reading><meaning>sun</meaning></rmgroup></reading_meaning></character></kanjidic2>", new UTF8Encoding(false));
                sources.Add(source with
                {
                    SourceId = "fixture-kanjidic2", Kind = "kanjidic2", ArtifactPath = kanjiPath,
                    ArtifactBytes = new FileInfo(kanjiPath).Length, ExpandedBytes = new FileInfo(kanjiPath).Length,
                    MaxExpandedBytes = new FileInfo(kanjiPath).Length, ArtifactSha256 = Hash(kanjiPath),
                    ExpandedSha256 = Hash(kanjiPath), ExpectedRecords = 1
                });
            }
            Inputs = new(new DictionarySourceLock
            {
                Scope = includeKanji ? "ja-en" : "ja-vi",
                TransformationId = "fixture-v1", TransformationSha256 = new string('d', 64),
                MappingPath = includeKanji ? "" : mappingPath, MappingSha256 = includeKanji ? "" : Hash(mappingPath),
                MappingApprovalReference = includeKanji ? "" : "synthetic-test-only",
                Sources = sources
            }, new string('a', 64), new string('c', 64), []);
        }
        public void Dispose()
        {
            // Exact temporary directory created by this fixture; no repository or database cleanup.
            foreach (var name in new[] { "source.xml", "kanjidic2.xml", "schema.txt", "license.txt", "mapping.json" })
                File.Delete(Path.Combine(directory, name));
            Directory.Delete(directory);
        }
    }
}
