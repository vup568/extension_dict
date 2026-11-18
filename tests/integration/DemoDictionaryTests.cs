using System.Text.Json;
using FluentAssertions;
using Infrastructure.Demo;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace IntegrationTests;

public sealed class DemoDictionaryTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static byte[] Corpus() => File.ReadAllBytes(Path.Combine(ConjugationCorpusFiles.Packet, "cases.json"));
    private static PostgreSqlContainer Container() => new PostgreSqlBuilder("postgres:16")
        .WithDatabase(DemoDictionarySeeder.DatabaseName).Build();
    private static AppDbContext Context(string connection) => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(connection).Options);

    [Theory]
    [InlineData("Host=remote.example;Database=jp_dict_demo")]
    [InlineData("Host=127.0.0.1,remote.example;Database=jp_dict_demo")]
    [InlineData("Host=/tmp;Database=jp_dict_demo")]
    [InlineData("Host=127.0.0.1;Database=postgres")]
    [InlineData("Host=127.0.0.1;Database=jp_dict_demo;Search Path=other")]
    [InlineData("Host=127.0.0.1;Database=jp_dict_demo;Options=-c search_path=other")]
    [InlineData("Host=127.0.0.1;Database=jp_dict_demo;Multiplexing=true")]
    public async Task UnsafeTargetsFailBeforeAnyConnection(string connection)
    {
        var action = () => DemoDictionarySeeder.SeedAsync(connection, Corpus(), Token);
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*local demo*");
    }

    [Fact]
    public async Task AlteredCorpusFailsBeforeOpeningTheUnavailableEndpoint()
    {
        var bytes = Corpus();
        bytes[^1] ^= 1;
        var action = () => DemoDictionarySeeder.SeedAsync("Host=127.0.0.1;Port=1;Database=jp_dict_demo", bytes, Token);
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*corpus checksum*");
    }

    [Fact]
    public async Task FreshSeedMakesRealSqlLookupAvailableAndRepeatingKeepsAllIdsAndOriginalDatabase()
    {
        await using var container = Container();
        await container.StartAsync(TestContext.Current.CancellationToken);
        var connection = container.GetConnectionString();
        await using (var raw = new NpgsqlConnection(connection))
        {
            await raw.OpenAsync(Token);
            await using var command = new NpgsqlCommand("SELECT count(*) FROM pg_catalog.pg_tables WHERE schemaname='public'", raw);
            (await command.ExecuteScalarAsync(Token)).Should().Be(0L);
        }

        (await DemoDictionarySeeder.SeedAsync(connection, Corpus(), Token)).Should().Be(17);
        await using var db = Context(connection);
        var repository = new EfDictionaryRepository(db);
        var found = await repository.SearchByFormOrReadingAsync("食べる", Token);
        found.Should().ContainSingle();
        found[0].CanonicalId.Should().Be("fixture:word:001");
        found[0].Readings.Single().ReadingText.Should().Be("たべる");
        found[0].Senses.Single().PartOfSpeech.Should().Equal("v1");
        found[0].Senses.Single().Glosses.Single().GlossText.Should().Be("Ăn");
        (await repository.SearchByFormOrReadingAsync("学生", Token)).Single().Senses.Single().Glosses.Single().GlossText
            .Should().Be("Học sinh/sinh viên");
        (await repository.SearchByFormOrReadingAsync("未収録語", Token)).Should().BeEmpty();
        var before = await IdSnapshot(db);
        (await DemoDictionarySeeder.SeedAsync(connection, Corpus(), Token)).Should().Be(17);
        (await IdSnapshot(db)).Should().Equal(before);
        (await db.SourceManifests.SingleAsync(Token)).SourceName.Should().Be("local-demo-fixtures");
        var sources = await db.SourceRecords.ToListAsync(Token);
        sources.Should().HaveCount(17);
        foreach (var source in sources)
        {
            using var json = JsonDocument.Parse(source.RecordData);
            json.RootElement.TryGetProperty("expected", out _).Should().BeFalse();
            json.RootElement.GetProperty("provenance").GetString().Should().Contain("synthetic");
        }
        (await db.LocalizedGlosses.Select(g => g.ReviewStatus).Distinct().ToListAsync(Token)).Should().Equal("demo");

        var original = new NpgsqlConnectionStringBuilder(connection) { Database = "postgres" };
        await using var originalConnection = new NpgsqlConnection(original.ConnectionString);
        await originalConnection.OpenAsync(Token);
        await using var originalCommand = new NpgsqlCommand("SELECT count(*) FROM pg_catalog.pg_tables WHERE schemaname='public'", originalConnection);
        (await originalCommand.ExecuteScalarAsync(Token)).Should().Be(0L);
    }

    [Fact]
    public async Task ConcurrentInitialSeedIsSerializedAndDoesNotDuplicateEntries()
    {
        await using var container = Container();
        await container.StartAsync(TestContext.Current.CancellationToken);
        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
            DemoDictionarySeeder.SeedAsync(container.GetConnectionString(), Corpus(), Token)));
        results.Should().Equal(17, 17);
        await using var db = Context(container.GetConnectionString());
        (await db.DictionaryEntries.CountAsync(Token)).Should().Be(17);
        (await db.SourceManifests.CountAsync(Token)).Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SameNameUnownedDatabaseIsRefusedWithoutNormalizingIt(bool migrated)
    {
        await using var container = Container();
        await container.StartAsync(TestContext.Current.CancellationToken);
        await using var db = Context(container.GetConnectionString());
        if (migrated) await db.Database.MigrateAsync(Token);
        else await db.Database.ExecuteSqlRawAsync("CREATE TABLE public.do_not_touch (id int PRIMARY KEY); INSERT INTO public.do_not_touch VALUES (42)", Token);
        var action = () => DemoDictionarySeeder.SeedAsync(container.GetConnectionString(), Corpus(), Token);
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*unowned*");
        if (migrated) (await db.DictionaryEntries.CountAsync(Token)).Should().Be(0);
        else
        {
            await db.Database.OpenConnectionAsync(Token);
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT id FROM public.do_not_touch";
            (await command.ExecuteScalarAsync(Token)).Should().Be(42);
        }
    }

    [Theory]
    [InlineData("CREATE SCHEMA other; CREATE TABLE other.do_not_touch(id int)")]
    [InlineData("CREATE FUNCTION public.do_not_touch() RETURNS integer LANGUAGE sql AS 'SELECT 42'")]
    [InlineData("CREATE TYPE public.do_not_touch AS ENUM ('untouched')")]
    [InlineData("CREATE TYPE public.do_not_touch AS (untouched int)")]
    public async Task UnknownSchemaOrStandaloneObjectIsRefusedBeforeMigration(string schema)
    {
        await using var container = Container();
        await container.StartAsync(Token);
        await using var db = Context(container.GetConnectionString());
        await db.Database.ExecuteSqlRawAsync(schema, Token);
        var action = () => DemoDictionarySeeder.SeedAsync(container.GetConnectionString(), Corpus(), Token);
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*unowned*");
        await db.Database.OpenConnectionAsync(Token);
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT pg_catalog.to_regclass('public.dictionary_entries') IS NULL";
        (await command.ExecuteScalarAsync(Token)).Should().Be(true);
    }

    [Theory]
    [InlineData("UPDATE localized_glosses SET gloss_text='changed' WHERE id=(SELECT min(id) FROM localized_glosses)")]
    [InlineData("UPDATE dictionary_senses SET part_of_speech=ARRAY['n'] WHERE id=(SELECT min(id) FROM dictionary_senses)")]
    [InlineData("UPDATE written_forms SET form='changed' WHERE id=(SELECT min(id) FROM written_forms)")]
    [InlineData("UPDATE readings SET reading='changed' WHERE id=(SELECT min(id) FROM readings)")]
    [InlineData("UPDATE source_records SET record_data='{}'::jsonb WHERE id=(SELECT min(id) FROM source_records)")]
    [InlineData("UPDATE source_manifests SET checksum='changed'")]
    [InlineData("DELETE FROM localized_glosses WHERE id=(SELECT min(id) FROM localized_glosses)")]
    [InlineData("INSERT INTO dictionary_entries(canonical_id) VALUES ('other:entry')")]
    [InlineData("CREATE TABLE public.do_not_touch(id int)")]
    [InlineData("CREATE SCHEMA other")]
    [InlineData("CREATE FUNCTION public.do_not_touch() RETURNS integer LANGUAGE sql AS 'SELECT 42'")]
    [InlineData("CREATE SEQUENCE public.do_not_touch")]
    public async Task ChangedOrMixedDemoDataIsRefusedWithoutRepair(string mutation)
    {
        await using var container = Container();
        await container.StartAsync(TestContext.Current.CancellationToken);
        var connection = container.GetConnectionString();
        await DemoDictionarySeeder.SeedAsync(connection, Corpus(), Token);
        await using var db = Context(connection);
        await using (var mutationConnection = new NpgsqlConnection(connection))
        {
            await mutationConnection.OpenAsync(Token);
            await using var command = new NpgsqlCommand(mutation, mutationConnection);
            await command.ExecuteNonQueryAsync(Token);
        }
        var before = await IdSnapshot(db);
        var action = () => DemoDictionarySeeder.SeedAsync(connection, Corpus(), Token);
        await action.Should().ThrowAsync<InvalidOperationException>();
        (await IdSnapshot(db)).Should().Equal(before);
    }

    private static async Task<long[]> IdSnapshot(AppDbContext db) =>
        (await db.DictionaryEntries.OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync(Token))
        .Concat(await db.WrittenForms.OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync(Token))
        .Concat(await db.Readings.OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync(Token))
        .Concat(await db.DictionarySenses.OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync(Token))
        .Concat(await db.LocalizedGlosses.OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync(Token))
        .Concat(await db.SourceRecords.OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync(Token)).ToArray();
}
