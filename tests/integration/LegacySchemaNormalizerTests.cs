using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace IntegrationTests;

public sealed class LegacySchemaNormalizerTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private const string Initial = "20260821075041_InitialCreate";

    [Fact]
    public async Task Known_legacy_schema_recovers_without_losing_data_or_sequence_high_water()
    {
        var (target, reference) = await CreatePairAsync();
        await ExecuteAsync(target, """
            ALTER TABLE dictionary_entries RENAME CONSTRAINT "PK_dictionary_entries" TO dictionary_entries_pkey;
            ALTER TABLE resource_revisions RENAME CONSTRAINT "FK_resource_revisions_knowledge_releases_release_id" TO resource_revisions_release_id_fkey;
            ALTER TABLE dictionary_entries ALTER COLUMN id DROP IDENTITY;
            CREATE SEQUENCE dictionary_entries_id_seq OWNED BY dictionary_entries.id;
            ALTER TABLE dictionary_entries ALTER COLUMN id SET DEFAULT nextval('dictionary_entries_id_seq'::regclass);
            INSERT INTO dictionary_entries(id, canonical_id) VALUES (42, 'dictionary:recovery-fixture');
            SELECT setval('dictionary_entries_id_seq', 900, true);
            ALTER TABLE kanji_records ALTER COLUMN source_metadata SET NOT NULL;
            ALTER TABLE grammar_rules ALTER COLUMN examples SET NOT NULL;
            ALTER TABLE grammar_rules ALTER COLUMN matcher_metadata SET NOT NULL;
            ALTER TABLE grammar_rules ALTER COLUMN source_metadata SET NOT NULL;
            ALTER TABLE source_manifests ALTER COLUMN metadata SET NOT NULL;
            DROP INDEX uq_dictionary_entries_canonical_id;
            ALTER TABLE dictionary_entries ADD CONSTRAINT dictionary_entries_canonical_id_key UNIQUE(canonical_id);
            DROP INDEX "IX_knowledge_release_manifests_manifest_id";
            DROP INDEX "IX_localized_glosses_source_record_id";
            DROP INDEX "IX_sense_applicabilities_reading_id";
            DROP INDEX "IX_sense_applicabilities_written_form_id";
            DROP INDEX uq_sense_applicabilities_combo;
            CREATE UNIQUE INDEX uq_sense_applicabilities_combo ON sense_applicabilities
                (sense_id, COALESCE(written_form_id, 0), COALESCE(reading_id, 0));
            CREATE FUNCTION trg_set_updated_at() RETURNS trigger LANGUAGE plpgsql AS
                $$ BEGIN NEW.updated_at = now(); RETURN NEW; END; $$;
            CREATE TRIGGER trg_dictionary_entries_updated_at BEFORE UPDATE ON dictionary_entries
                FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();
            """);
        await using (var context = Context(target))
        {
            await Assert.ThrowsAsync<PostgresException>(() => context.Database.MigrateAsync(TestContext.Current.CancellationToken));
        }

        await LegacySchemaNormalizer.NormalizeAndBaselineAsync(target, reference, TestContext.Current.CancellationToken);

        Assert.Equal("dictionary:recovery-fixture", await ScalarAsync(target, "SELECT canonical_id FROM dictionary_entries WHERE id=42"));
        Assert.Equal("900", await ScalarAsync(target, "SELECT last_value::text FROM dictionary_entries_id_seq"));
        Assert.Equal(Initial, await ScalarAsync(target, "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\""));
        Assert.Equal("901", await ScalarAsync(target, "INSERT INTO dictionary_entries(canonical_id) VALUES ('dictionary:next-fixture') RETURNING id::text"));
        Assert.Equal("2", await ScalarAsync(target, "SELECT count(*)::text FROM pg_indexes WHERE tablename='sense_applicabilities' AND indexname LIKE '%sense_applicabilities_combo%'"));
        await using (var context = Context(target))
        {
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
            Assert.Equal(context.Database.GetMigrations(),
                await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken));
        }
    }

    [Theory]
    [InlineData("CREATE TABLE unrelated_object(id bigint)")]
    [InlineData("ALTER TABLE dictionary_entries ADD COLUMN unexpected text")]
    [InlineData("ALTER TABLE dictionary_entries ADD CONSTRAINT unexpected CHECK(id > 0)")]
    [InlineData("CREATE FUNCTION unexpected() RETURNS integer LANGUAGE SQL AS $$ SELECT 1 $$")]
    [InlineData("CREATE INDEX unexpected ON dictionary_entries(created_at)")]
    [InlineData("DROP INDEX idx_written_forms_form")]
    [InlineData("CREATE RULE unexpected AS ON INSERT TO dictionary_entries DO ALSO NOTIFY recovery_fixture")]
    [InlineData("ALTER TABLE grammar_rules INHERIT dictionary_entries")]
    [InlineData("CREATE SCHEMA outside; CREATE TABLE outside.inherited_entry () INHERITS(public.dictionary_entries)")]
    [InlineData("DROP INDEX uq_dictionary_entries_canonical_id; ALTER TABLE dictionary_entries ADD CONSTRAINT dictionary_entries_canonical_id_key UNIQUE(canonical_id) DEFERRABLE INITIALLY DEFERRED")]
    [InlineData("CREATE FUNCTION trg_set_updated_at() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN NEW.updated_at = now(); RETURN NEW; END; $$; CREATE SCHEMA other_schema; CREATE FUNCTION other_schema.trg_set_updated_at() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN NEW.updated_at = now(); RETURN NEW; END; $$; CREATE TRIGGER trg_dictionary_entries_updated_at BEFORE UPDATE ON dictionary_entries FOR EACH ROW EXECUTE FUNCTION other_schema.trg_set_updated_at()")]
    public async Task Unknown_schema_is_rejected_without_any_baseline_or_normalization(string drift)
    {
        var (target, reference) = await CreatePairAsync();
        await ExecuteAsync(target, "ALTER TABLE dictionary_entries RENAME CONSTRAINT \"PK_dictionary_entries\" TO dictionary_entries_pkey; " + drift);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => LegacySchemaNormalizer.NormalizeAndBaselineAsync(target, reference, TestContext.Current.CancellationToken));

        Assert.DoesNotContain(new NpgsqlConnectionStringBuilder(target).Password!, error.Message);
        Assert.IsType<int>(error.Data["LegacyValidationCheck"]);
        Assert.Equal("0", await ScalarAsync(target, "SELECT count(*)::text FROM \"__EFMigrationsHistory\""));
        Assert.Equal("1", await ScalarAsync(target, "SELECT count(*)::text FROM pg_constraint WHERE conname='dictionary_entries_pkey'"));
    }

    [Fact]
    public async Task Nonempty_history_requires_normal_migration_not_a_second_recovery()
    {
        var (target, reference) = await CreatePairAsync();
        await LegacySchemaNormalizer.NormalizeAndBaselineAsync(target, reference, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => LegacySchemaNormalizer.NormalizeAndBaselineAsync(target, reference, TestContext.Current.CancellationToken));
        Assert.Equal("1", await ScalarAsync(target, "SELECT count(*)::text FROM \"__EFMigrationsHistory\""));
    }

    [Fact]
    public async Task Reference_with_later_migrations_is_rejected()
    {
        var (target, reference) = await CreatePairAsync();
        await using (var context = Context(reference)) await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => LegacySchemaNormalizer.NormalizeAndBaselineAsync(target, reference, TestContext.Current.CancellationToken));
        Assert.Equal("0", await ScalarAsync(target, "SELECT count(*)::text FROM \"__EFMigrationsHistory\""));
    }

    [Fact]
    public async Task Serial_sequence_behind_existing_ids_is_rejected_without_resetting_it()
    {
        var (target, reference) = await CreatePairAsync();
        await ExecuteAsync(target, """
            ALTER TABLE dictionary_entries ALTER COLUMN id DROP IDENTITY;
            CREATE SEQUENCE dictionary_entries_id_seq OWNED BY dictionary_entries.id;
            ALTER TABLE dictionary_entries ALTER COLUMN id SET DEFAULT nextval('dictionary_entries_id_seq'::regclass);
            INSERT INTO dictionary_entries(id, canonical_id) VALUES (42, 'dictionary:recovery-fixture');
            """);
        await Assert.ThrowsAsync<InvalidOperationException>(() => LegacySchemaNormalizer.NormalizeAndBaselineAsync(target, reference, TestContext.Current.CancellationToken));
        Assert.Equal("1", await ScalarAsync(target, "SELECT last_value::text FROM dictionary_entries_id_seq"));
        Assert.Equal("0", await ScalarAsync(target, "SELECT count(*)::text FROM \"__EFMigrationsHistory\""));
    }

    private async Task<(string Target, string Reference)> CreatePairAsync()
    {
        var connections = new List<string>();
        foreach (var role in new[] { "target", "reference" })
        {
            var name = "recovery_" + role + "_" + Guid.NewGuid().ToString("N");
            await ExecuteAsync(fixture.ConnectionString, "CREATE DATABASE \"" + name + "\"");
            var connection = new NpgsqlConnectionStringBuilder(fixture.ConnectionString) { Database = name }.ConnectionString;
            await using var context = Context(connection);
            await context.GetService<IMigrator>().MigrateAsync(Initial);
            connections.Add(connection);
        }
        await ExecuteAsync(connections[0], "DELETE FROM \"__EFMigrationsHistory\"");
        return (connections[0], connections[1]);
    }

    private static AppDbContext Context(string connection) => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options);

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string> ScalarAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }
}
