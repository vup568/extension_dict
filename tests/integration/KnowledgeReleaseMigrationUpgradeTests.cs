using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace IntegrationTests;

public sealed class KnowledgeReleaseMigrationUpgradeTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task Migration_preserves_the_existing_published_current_release()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        long releaseId;

        await using (var initialContext = new AppDbContext(options))
        {
            var migrator = initialContext.Database.GetService<IMigrator>();
            await migrator.MigrateAsync("20260821075041_InitialCreate", TestContext.Current.CancellationToken);
        }

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var insert = connection.CreateCommand();
            insert.CommandText = """
                WITH inserted_release AS (
                    INSERT INTO knowledge_releases (version, published_at, is_current)
                    VALUES (@version, @publishedAt, TRUE)
                    RETURNING id
                ), inserted_manifest AS (
                    INSERT INTO source_manifests (source_name, version)
                    VALUES (@sourceName, '1')
                    RETURNING id
                ), inserted_membership AS (
                    INSERT INTO knowledge_release_manifests (release_id, manifest_id)
                    SELECT inserted_release.id, inserted_manifest.id
                    FROM inserted_release, inserted_manifest
                )
                SELECT id FROM inserted_release;
                """;
            insert.Parameters.AddWithValue("version", $"upgrade-{Guid.NewGuid():N}");
            insert.Parameters.AddWithValue("publishedAt", DateTime.UtcNow);
            insert.Parameters.AddWithValue("sourceName", $"upgrade-source-{Guid.NewGuid():N}");
            releaseId = (long)(await insert.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
        }

        await using (var upgradedContext = new AppDbContext(options))
        {
            await upgradedContext.Database.MigrateAsync(TestContext.Current.CancellationToken);
        }

        await using var verificationContext = new AppDbContext(options);
        var current = await verificationContext.CurrentKnowledgeRelease
            .AsNoTracking()
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(releaseId, current.ReleaseId);
    }

    [Fact]
    public async Task Migration_rejects_a_published_legacy_release_without_manifest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        long releaseId;

        await using (var initialContext = new AppDbContext(options))
        {
            var migrator = initialContext.Database.GetService<IMigrator>();
            await migrator.MigrateAsync("20260821075041_InitialCreate", TestContext.Current.CancellationToken);
        }

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var insert = connection.CreateCommand();
            insert.CommandText = """
                INSERT INTO knowledge_releases (version, published_at, is_current)
                VALUES (@version, @publishedAt, FALSE)
                RETURNING id;
                """;
            insert.Parameters.AddWithValue("version", $"invalid-upgrade-{Guid.NewGuid():N}");
            insert.Parameters.AddWithValue("publishedAt", DateTime.UtcNow);
            releaseId = (long)(await insert.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
        }

        try
        {
            await using var upgradedContext = new AppDbContext(options);
            var migrate = () => upgradedContext.Database.MigrateAsync(TestContext.Current.CancellationToken);

            var exception = await Assert.ThrowsAsync<PostgresException>(migrate);
            Assert.Equal("23514", exception.SqlState);
        }
        finally
        {
            await using var cleanupConnection = new NpgsqlConnection(fixture.ConnectionString);
            await cleanupConnection.OpenAsync(TestContext.Current.CancellationToken);
            await using var cleanup = cleanupConnection.CreateCommand();
            cleanup.CommandText = "DELETE FROM knowledge_releases WHERE id = @releaseId;";
            cleanup.Parameters.AddWithValue("releaseId", releaseId);
            await cleanup.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }
    }
}
