using System.Security.Cryptography;
using System.Text.Json;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Demo;

/// <summary>Explicit local-only synthetic fixture installation; never called by normal startup or HTTP.</summary>
public static class DemoDictionarySeeder
{
    public const string DatabaseName = "jp_dict_demo";
    public const string CorpusVersion = "f09-expanded-20261013.3";
    public const string CorpusSha256 = "ca3e3ff71b4d97a96e33c70695d5cfb067244bb6cf63a7d3340cd901dd7883c4";
    public const int FixtureCount = 17;
    public const string SourceName = "local-demo-fixtures";
    private const long LockKey = 0x4A5044454D4F;
    private const string Publisher = "Project-authored synthetic demo fixtures, not JMdict";
    private const string License = "Synthetic project fixtures; not an imported dictionary release";
    private const string Attribution = "Approved F09 corpus dictionary_fixtures; demo only";
    private const string Transformation = "local-demo-fixtures-v1";
    private const string DemoMetadata = "{\"synthetic\":true,\"production_dictionary\":false}";

    public static async Task<int> SeedAsync(string connectionString, byte[] corpusBytes,
        CancellationToken cancellationToken = default)
    {
        // EARS[Event]: WHEN the operator explicitly prepares the demo, reject non-demo targets before any DB write.
        ValidateTarget(connectionString);
        var fixtures = ReadFixtures(corpusBytes);
        cancellationToken.ThrowIfCancellationRequested();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        if (connection.Database != DatabaseName)
            throw new InvalidOperationException("Only the local demo database may be seeded.");
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection).Options);
        await ExecuteAsync(connection, "SET search_path TO public", cancellationToken);
        // Session lock covers schema inspection and official migration as well as the atomic data insertion.
        await ExecuteAsync(connection, $"SELECT pg_catalog.pg_advisory_lock({LockKey})", cancellationToken);
        try
        {
            var relations = await ReadRelationsAsync(connection, cancellationToken);
            await ValidateNamespacesAsync(connection, cancellationToken);
            await ValidateRoutinesAsync(connection, relations.Count == 0, cancellationToken);
            if (relations.Count != 0)
            {
                if (!relations.Contains(("source_manifests", "r")))
                    throw Unowned();
                var manifests = await context.SourceManifests.AsNoTracking().ToListAsync(cancellationToken);
                if (manifests.Count != 1 || !MatchesMarker(manifests[0])) throw Unowned();
                var allowedTables = context.Model.GetEntityTypes().Select(x => x.GetTableName()!)
                    .Append("__EFMigrationsHistory").ToHashSet(StringComparer.Ordinal);
                if (relations.Any(x => x.Kind == "S"
                        ? !allowedTables.Any(table => x.Name == $"{table}_id_seq")
                        : x.Kind != "r" || !allowedTables.Contains(x.Name)))
                    throw Changed();
                var applied = await context.Database.GetAppliedMigrationsAsync(cancellationToken);
                if (!applied.SequenceEqual(context.Database.GetMigrations())) throw Changed();
                await ValidateExistingAsync(context, manifests[0], fixtures, cancellationToken);
                return FixtureCount;
            }

            // Existing unowned schema is never reset, normalized or baselined, even if its rows are empty.
            await context.Database.MigrateAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var manifest = new SourceManifest
            {
                SourceName = SourceName,
                Version = CorpusVersion,
                Publisher = Publisher,
                License = License,
                Attribution = Attribution,
                Checksum = CorpusSha256,
                RecordCount = FixtureCount,
                RejectedCount = 0,
                TransformationId = Transformation,
                ValidationResult = "synthetic-demo-only",
                Metadata = DemoMetadata
            };
            context.SourceManifests.Add(manifest);
            foreach (var fixture in fixtures)
            {
                var source = new SourceRecord
                {
                    Manifest = manifest,
                    SourceIdentity = fixture.FixtureId,
                    RecordData = fixture.RawJson
                };
                context.SourceRecords.Add(source);
                context.DictionaryEntries.Add(new DictionaryEntry
                {
                    CanonicalId = fixture.CanonicalId,
                    WrittenForms = fixture.Forms.Select(x => new WrittenForm { Form = x }).ToList(),
                    Readings = fixture.Readings.Select(x => new Reading { ReadingText = x }).ToList(),
                    Senses = [new DictionarySense
                    {
                        SenseKey = "1", Position = 1, PartOfSpeech = [fixture.PartOfSpeech],
                        Glosses = [new LocalizedGloss
                        {
                            LanguageTag = "vi", GlossText = fixture.GlossVi, Position = 1,
                            SourceRecord = source, ReviewStatus = "demo"
                        }]
                    }]
                });
            }
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return FixtureCount;
        }
        finally
        {
            // Returning a pooled session with this lock held would block later preparations.
            if (connection.State == System.Data.ConnectionState.Open)
                await ExecuteAsync(connection, $"SELECT pg_catalog.pg_advisory_unlock({LockKey})", CancellationToken.None);
        }
    }

    private static void ValidateTarget(string connectionString)
    {
        NpgsqlConnectionStringBuilder builder;
        try { builder = new NpgsqlConnectionStringBuilder(connectionString); }
        catch (ArgumentException) { throw new InvalidOperationException("Invalid local demo connection configuration."); }
        if (builder.Database != DatabaseName ||
            !(string.Equals(builder.Host, "127.0.0.1", StringComparison.Ordinal) ||
              string.Equals(builder.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
              string.Equals(builder.Host, "::1", StringComparison.Ordinal)) ||
            (!string.IsNullOrEmpty(builder.SearchPath) && builder.SearchPath != "public") ||
            !string.IsNullOrEmpty(builder.Options) || builder.Multiplexing)
            throw new InvalidOperationException("Only a single loopback host and the local demo database are allowed.");
    }

    private static List<Fixture> ReadFixtures(byte[] corpusBytes)
    {
        if (corpusBytes is null || Convert.ToHexStringLower(SHA256.HashData(corpusBytes)) != CorpusSha256)
            throw new InvalidOperationException("The approved demo corpus checksum does not match.");
        using var corpus = JsonDocument.Parse(corpusBytes);
        if (corpus.RootElement.GetProperty("corpus_version").GetString() != CorpusVersion)
            throw new InvalidOperationException("The approved demo corpus version does not match.");
        // Read ONLY the fixture records. Expected cases/explanations are never persisted or read by the runtime.
        var fixtures = corpus.RootElement.GetProperty("dictionary_fixtures").EnumerateArray()
            .Select(f => new Fixture(
                f.GetProperty("fixture_id").GetString()!, f.GetProperty("canonical_id").GetString()!,
                f.GetProperty("forms").EnumerateArray().Select(x => x.GetString()!).ToArray(),
                f.GetProperty("readings").EnumerateArray().Select(x => x.GetString()!).ToArray(),
                f.GetProperty("gloss_vi").GetString()!, PartOfSpeech(f), f.GetRawText())).ToList();
        if (fixtures.Count != FixtureCount || fixtures.Select(x => x.CanonicalId).Distinct().Count() != FixtureCount ||
            fixtures.Select(x => x.FixtureId).Distinct().Count() != FixtureCount)
            throw new InvalidOperationException("The approved demo fixture inventory does not match.");
        return fixtures;
    }

    // Same fixture POS projection as F09CorpusFixture, not a general dictionary import/inference algorithm.
    private static string PartOfSpeech(JsonElement fixture)
    {
        var form = fixture.GetProperty("preferred_form").GetString()!;
        var group = fixture.GetProperty("word_group").GetString();
        var conjugation = fixture.GetProperty("conjugation_class");
        return group switch
        {
            "i_adjective" => "adj-i",
            "na_adjective" => "adj-na",
            "noun_copula" => "n",
            _ when conjugation.ValueKind == JsonValueKind.Null => "derived_entry",
            _ when conjugation.GetString() == "ichidan" => "v1",
            _ when conjugation.GetString() == "irregular" => form == "する" ? "vs-i" : "vk",
            _ when form == "行く" => "v5k-s",
            _ => form.EndsWith("く", StringComparison.Ordinal) ? "v5k" : "v5m"
        };
    }

    private static bool MatchesMarker(SourceManifest manifest) =>
        manifest.SourceName == SourceName && manifest.Version == CorpusVersion &&
        manifest.Checksum == CorpusSha256 && manifest.RecordCount == FixtureCount &&
        manifest.RejectedCount == 0 && manifest.Publisher == Publisher &&
        manifest.License == License && manifest.Attribution == Attribution &&
        manifest.TransformationId == Transformation && manifest.ValidationResult == "synthetic-demo-only" &&
        JsonMatches(manifest.Metadata, DemoMetadata);

    private static async Task ValidateExistingAsync(AppDbContext context, SourceManifest manifest,
        List<Fixture> fixtures, CancellationToken cancellationToken)
    {
        var entries = await context.DictionaryEntries.AsNoTracking()
            .Include(x => x.WrittenForms).Include(x => x.Readings)
            .Include(x => x.Senses).ThenInclude(x => x.Glosses).ToListAsync(cancellationToken);
        var sources = await context.SourceRecords.AsNoTracking().ToListAsync(cancellationToken);
        if (entries.Count != FixtureCount || sources.Count != FixtureCount ||
            await context.WrittenForms.CountAsync(cancellationToken) != fixtures.Sum(x => x.Forms.Length) ||
            await context.Readings.CountAsync(cancellationToken) != fixtures.Sum(x => x.Readings.Length) ||
            await context.DictionarySenses.CountAsync(cancellationToken) != FixtureCount ||
            await context.LocalizedGlosses.CountAsync(cancellationToken) != FixtureCount ||
            await context.SenseApplicabilities.AnyAsync(cancellationToken) ||
            await context.KanjiRecords.AnyAsync(cancellationToken) ||
            await context.Users.AnyAsync(cancellationToken) || await context.UserExternalLogins.AnyAsync(cancellationToken) ||
            await context.LearningReferences.AnyAsync(cancellationToken) ||
            await context.EditorialMappings.AnyAsync(cancellationToken) ||
            await context.KnowledgeReleases.AnyAsync(cancellationToken) ||
            await context.KnowledgeReleaseManifests.AnyAsync(cancellationToken) ||
            await context.ResourceRevisions.AnyAsync(cancellationToken) ||
            await context.CurrentKnowledgeRelease.AnyAsync(cancellationToken)) throw Changed();
        foreach (var fixture in fixtures)
        {
            var entry = entries.SingleOrDefault(x => x.CanonicalId == fixture.CanonicalId);
            var source = sources.SingleOrDefault(x => x.SourceIdentity == fixture.FixtureId);
            if (entry is null || source is null || source.ManifestId != manifest.Id ||
                !JsonMatches(source.RecordData, fixture.RawJson) ||
                !entry.WrittenForms.Select(x => x.Form).Order(StringComparer.Ordinal)
                    .SequenceEqual(fixture.Forms.Order(StringComparer.Ordinal)) ||
                !entry.Readings.Select(x => x.ReadingText).Order(StringComparer.Ordinal)
                    .SequenceEqual(fixture.Readings.Order(StringComparer.Ordinal)) ||
                entry.WrittenForms.Any(x => x.IsCommon || x.Priority is not null || x.Info is not null) ||
                entry.Readings.Any(x => x.IsCommon || x.Priority is not null || x.Info is not null || x.RestrictedToForms is not null) ||
                entry.Senses.Count != 1) throw Changed();
            var sense = entry.Senses.Single();
            if (sense.SenseKey != "1" || sense.Position != 1 ||
                sense.PartOfSpeech is null || !sense.PartOfSpeech.SequenceEqual([fixture.PartOfSpeech]) ||
                sense.Glosses.Count != 1) throw Changed();
            var gloss = sense.Glosses.Single();
            if (gloss.LanguageTag != "vi" || gloss.GlossText != fixture.GlossVi || gloss.Position != 1 ||
                gloss.SourceRecordId != source.Id || gloss.ReviewStatus != "demo") throw Changed();
        }
    }

    private static bool JsonMatches(string? actual, string expected)
    {
        if (actual is null) return false;
        try
        {
            using var actualDocument = JsonDocument.Parse(actual);
            using var expectedDocument = JsonDocument.Parse(expected);
            return JsonElement.DeepEquals(actualDocument.RootElement, expectedDocument.RootElement);
        }
        catch (JsonException) { return false; }
    }

    private static async Task<HashSet<(string Name, string Kind)>> ReadRelationsAsync(NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT c.relname, c.relkind::text FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='public' AND c.relkind IN ('r','p','v','m','f','S','c')
            """, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new HashSet<(string Name, string Kind)>();
        while (await reader.ReadAsync(cancellationToken)) result.Add((reader.GetString(0), reader.GetString(1)));
        return result;
    }

    private static async Task ValidateNamespacesAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace
                WHERE nspname NOT IN ('public','information_schema') AND nspname !~ '^pg_')
                OR EXISTS (SELECT 1 FROM pg_catalog.pg_type t
                    JOIN pg_catalog.pg_namespace n ON n.oid=t.typnamespace
                    WHERE n.nspname='public' AND t.typtype IN ('d','e','r','m'))
            """, connection);
        if ((bool)(await command.ExecuteScalarAsync(cancellationToken))!) throw Unowned();
    }

    private static async Task ValidateRoutinesAsync(NpgsqlConnection connection, bool fresh,
        CancellationToken cancellationToken)
    {
        string[] knownMigrationRoutines = ["guard_published_knowledge_release", "guard_published_release_child",
            "guard_current_knowledge_release", "reject_conjugation_dataset_mutation"];
        await using var command = new NpgsqlCommand("""
            SELECT p.proname, p.pronargs, p.prokind::text, p.prorettype='pg_catalog.trigger'::regtype
            FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='public'
            """, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var count = 0;
        while (await reader.ReadAsync(cancellationToken))
        {
            if (fresh || !knownMigrationRoutines.Contains(reader.GetString(0), StringComparer.Ordinal) ||
                reader.GetInt16(1) != 0 || reader.GetString(2) != "f" || !reader.GetBoolean(3)) throw Unowned();
            count++;
        }
        if (!fresh && count != knownMigrationRoutines.Length) throw Unowned();
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static InvalidOperationException Unowned() =>
        new("Existing local demo schema is unowned; no migration or seed was applied. Choose a fresh database explicitly.");
    private static InvalidOperationException Changed() =>
        new("Existing local demo data or schema differs from the approved fixture installation; nothing was repaired or reseeded.");
    private sealed record Fixture(string FixtureId, string CanonicalId, string[] Forms, string[] Readings,
        string GlossVi, string PartOfSpeech, string RawJson);
}
