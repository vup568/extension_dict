using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace IntegrationTests;

public sealed class KnowledgeReleaseImmutabilityTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public Task Published_release_cannot_be_updated() => InTransactionAsync(async (connection, transaction) =>
    {
        var releaseId = await InsertReleaseAsync(connection, transaction, published: true);

        var update = () => ExecuteAsync(
            connection,
            transaction,
            "UPDATE knowledge_releases SET description = 'mutated' WHERE id = @releaseId;",
            releaseId);

        await update.Should().ThrowAsync<PostgresException>()
            .Where(exception => exception.SqlState == "P0001");
    });

    [Fact]
    public Task Published_release_cannot_be_deleted() => InTransactionAsync(async (connection, transaction) =>
    {
        var releaseId = await InsertReleaseAsync(connection, transaction, published: true);

        var delete = () => ExecuteAsync(
            connection,
            transaction,
            "DELETE FROM knowledge_releases WHERE id = @releaseId;",
            releaseId);

        await delete.Should().ThrowAsync<PostgresException>()
            .Where(exception => exception.SqlState == "P0001");
    });

    [Fact]
    public Task Published_release_rejects_new_resource_revision() => InTransactionAsync(async (connection, transaction) =>
    {
        var releaseId = await InsertReleaseAsync(connection, transaction, published: true);

        var insertRevision = () => ExecuteAsync(
            connection,
            transaction,
            """
            INSERT INTO resource_revisions (release_id, resource_type, resource_id, revision_data)
            VALUES (@releaseId, 'dictionary', 1, '{}'::jsonb);
            """,
            releaseId);

        await insertRevision.Should().ThrowAsync<PostgresException>()
            .Where(exception => exception.SqlState == "P0001");
    });

    [Fact]
    public Task Published_release_rejects_new_manifest_membership() => InTransactionAsync(async (connection, transaction) =>
    {
        var releaseId = await InsertReleaseAsync(connection, transaction, published: true);
        var manifestId = await InsertManifestAsync(connection, transaction);

        var attachManifest = () => ExecuteAsync(
            connection,
            transaction,
            """
            INSERT INTO knowledge_release_manifests (release_id, manifest_id)
            VALUES (@releaseId, @manifestId);
            """,
            releaseId,
            manifestId);

        await attachManifest.Should().ThrowAsync<PostgresException>()
            .Where(exception => exception.SqlState == "P0001");
    });

    [Theory]
    [InlineData("UPDATE resource_revisions SET revision_data = '{\"changed\":true}'::jsonb WHERE release_id = @releaseId;")]
    [InlineData("DELETE FROM resource_revisions WHERE release_id = @releaseId;")]
    public Task Published_release_rejects_existing_revision_mutation(string mutation) =>
        InTransactionAsync(async (connection, transaction) =>
        {
            var releaseId = await InsertReleaseAsync(connection, transaction, published: false);
            await ExecuteAsync(
                connection,
                transaction,
                """
                INSERT INTO resource_revisions (release_id, resource_type, resource_id, revision_data)
                VALUES (@releaseId, 'dictionary', 1, '{}'::jsonb);
                """,
                releaseId);
            await AttachManifestAndPublishAsync(connection, transaction, releaseId);

            var mutate = () => ExecuteAsync(connection, transaction, mutation, releaseId);

            await mutate.Should().ThrowAsync<PostgresException>()
                .Where(exception => exception.SqlState == "P0001");
        });

    [Theory]
    [InlineData("UPDATE knowledge_release_manifests SET manifest_id = manifest_id WHERE release_id = @releaseId;")]
    [InlineData("DELETE FROM knowledge_release_manifests WHERE release_id = @releaseId;")]
    public Task Published_release_rejects_manifest_membership_mutation(string mutation) =>
        InTransactionAsync(async (connection, transaction) =>
        {
            var releaseId = await InsertReleaseAsync(connection, transaction, published: false);
            await AttachManifestAndPublishAsync(connection, transaction, releaseId);

            var mutate = () => ExecuteAsync(connection, transaction, mutation, releaseId);

            await mutate.Should().ThrowAsync<PostgresException>()
                .Where(exception => exception.SqlState == "P0001");
        });

    [Fact]
    public Task Draft_without_manifest_cannot_be_published_directly() => InTransactionAsync(async (connection, transaction) =>
    {
        var releaseId = await InsertReleaseAsync(connection, transaction, published: false);
        await SetControlledPublicationAsync(connection, transaction);

        var publish = () => ExecuteAsync(
            connection,
            transaction,
            "UPDATE knowledge_releases SET published_at = now() WHERE id = @releaseId;",
            releaseId);

        await publish.Should().ThrowAsync<PostgresException>()
            .Where(exception => exception.SqlState == "P0001");
    });

    [Fact]
    public Task Current_pointer_rejects_a_draft_release() => InTransactionAsync(async (connection, transaction) =>
    {
        var releaseId = await InsertReleaseAsync(connection, transaction, published: false);
        await SetControlledPublicationAsync(connection, transaction);

        var pointToDraft = () => ExecuteAsync(
            connection,
            transaction,
            "INSERT INTO current_knowledge_release (singleton_id, release_id) VALUES (1, @releaseId);",
            releaseId);

        await pointToDraft.Should().ThrowAsync<PostgresException>()
            .Where(exception => exception.SqlState == "P0001");
    });

    [Fact]
    public Task Switching_current_pointer_does_not_update_the_old_release() => InTransactionAsync(async (connection, transaction) =>
    {
        var oldReleaseId = await InsertReleaseAsync(connection, transaction, published: true);
        var newReleaseId = await InsertReleaseAsync(connection, transaction, published: true);
        await SetControlledPublicationAsync(connection, transaction);

        await ExecuteAsync(
            connection,
            transaction,
            "INSERT INTO current_knowledge_release (singleton_id, release_id) VALUES (1, @releaseId);",
            oldReleaseId);
        var oldReleaseVersionBefore = await ReadRowVersionAsync(connection, transaction, oldReleaseId);

        await ExecuteAsync(
            connection,
            transaction,
            "UPDATE current_knowledge_release SET release_id = @releaseId WHERE singleton_id = 1;",
            newReleaseId);
        var oldReleaseVersionAfter = await ReadRowVersionAsync(connection, transaction, oldReleaseId);

        oldReleaseVersionAfter.Should().Be(oldReleaseVersionBefore);
    });

    [Fact]
    public Task Current_pointer_cannot_be_deleted_directly() => InTransactionAsync(async (connection, transaction) =>
    {
        var releaseId = await InsertReleaseAsync(connection, transaction, published: true);
        await SetControlledPublicationAsync(connection, transaction);
        await ExecuteAsync(
            connection,
            transaction,
            "INSERT INTO current_knowledge_release (singleton_id, release_id) VALUES (1, @releaseId);",
            releaseId);

        var deletePointer = () => ExecuteAsync(
            connection,
            transaction,
            "DELETE FROM current_knowledge_release WHERE release_id = @releaseId;",
            releaseId);

        await deletePointer.Should().ThrowAsync<PostgresException>()
            .Where(exception => exception.SqlState == "P0001");
    });

    [Fact]
    public async Task Publication_waits_for_an_inflight_child_write()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        await using (var dbContext = new AppDbContext(options))
        {
            await dbContext.Database.MigrateAsync(TestContext.Current.CancellationToken);
        }

        long releaseId;
        await using (var setupConnection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await setupConnection.OpenAsync(TestContext.Current.CancellationToken);
            await using var setupTransaction = await setupConnection.BeginTransactionAsync(TestContext.Current.CancellationToken);
            releaseId = await InsertReleaseAsync(setupConnection, setupTransaction, published: false);
            var manifestId = await InsertManifestAsync(setupConnection, setupTransaction);
            await ExecuteAsync(
                setupConnection,
                setupTransaction,
                "INSERT INTO knowledge_release_manifests (release_id, manifest_id) VALUES (@releaseId, @manifestId);",
                releaseId,
                manifestId);
            await setupTransaction.CommitAsync(TestContext.Current.CancellationToken);
        }

        await using var childConnection = new NpgsqlConnection(fixture.ConnectionString);
        await childConnection.OpenAsync(TestContext.Current.CancellationToken);
        await using var childTransaction = await childConnection.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await ExecuteAsync(
            childConnection,
            childTransaction,
            """
            INSERT INTO resource_revisions (release_id, resource_type, resource_id, revision_data)
            VALUES (@releaseId, 'dictionary', 1, '{}'::jsonb);
            """,
            releaseId);

        await using var publicationConnection = new NpgsqlConnection(fixture.ConnectionString);
        await publicationConnection.OpenAsync(TestContext.Current.CancellationToken);
        await using var publicationTransaction = await publicationConnection.BeginTransactionAsync(TestContext.Current.CancellationToken);
        var publicationBackendId = await ReadBackendIdAsync(publicationConnection, publicationTransaction);
        await SetControlledPublicationAsync(publicationConnection, publicationTransaction);
        var publish = ExecuteAsync(
            publicationConnection,
            publicationTransaction,
            "UPDATE knowledge_releases SET published_at = now() WHERE id = @releaseId;",
            releaseId);

        await using var observerConnection = new NpgsqlConnection(fixture.ConnectionString);
        await observerConnection.OpenAsync(TestContext.Current.CancellationToken);
        var publicationWasBlocked = await WaitUntilBlockedAsync(
            observerConnection,
            publicationBackendId,
            publish);

        await childTransaction.CommitAsync(TestContext.Current.CancellationToken);
        await publish;
        await publicationTransaction.RollbackAsync(TestContext.Current.CancellationToken);

        publicationWasBlocked.Should().BeTrue(
            "PostgreSQL must serialize publication with child writes for the same release");
    }

    [Fact]
    public Task Direct_insert_cannot_create_an_already_published_release() => InTransactionAsync(async (connection, transaction) =>
    {
        var insert = () => ExecuteWithoutReleaseIdAsync(
            connection,
            transaction,
            "INSERT INTO knowledge_releases (version, published_at) VALUES ('direct-published', now());");

        await insert.Should().ThrowAsync<PostgresException>()
            .Where(exception => exception.SqlState == "P0001");
    });

    [Fact]
    public Task Draft_with_manifest_cannot_be_published_outside_controlled_publication() => InTransactionAsync(async (connection, transaction) =>
    {
        var releaseId = await InsertReleaseAsync(connection, transaction, published: false);
        var manifestId = await InsertManifestAsync(connection, transaction);
        await ExecuteAsync(
            connection,
            transaction,
            "INSERT INTO knowledge_release_manifests (release_id, manifest_id) VALUES (@releaseId, @manifestId);",
            releaseId,
            manifestId);

        var publish = () => ExecuteAsync(
            connection,
            transaction,
            "UPDATE knowledge_releases SET published_at = now() WHERE id = @releaseId;",
            releaseId);

        await publish.Should().ThrowAsync<PostgresException>()
            .Where(exception => exception.SqlState == "P0001");
    });

    private async Task InTransactionAsync(Func<NpgsqlConnection, NpgsqlTransaction, Task> assertion)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        await using (var dbContext = new AppDbContext(options))
        {
            await dbContext.Database.MigrateAsync(TestContext.Current.CancellationToken);
        }

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken);

        try
        {
            await assertion(connection, transaction);
        }
        finally
        {
            await transaction.RollbackAsync(TestContext.Current.CancellationToken);
        }
    }

    private static async Task<long> InsertReleaseAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        bool published)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO knowledge_releases (version)
            VALUES (@version)
            RETURNING id;
            """;
        command.Parameters.AddWithValue("version", $"test-{Guid.NewGuid():N}");
        var releaseId = (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;

        if (published)
        {
            var manifestId = await InsertManifestAsync(connection, transaction);
            await ExecuteAsync(
                connection,
                transaction,
                "INSERT INTO knowledge_release_manifests (release_id, manifest_id) VALUES (@releaseId, @manifestId);",
                releaseId,
                manifestId);
            await SetControlledPublicationAsync(connection, transaction);
            await ExecuteAsync(
                connection,
                transaction,
                "UPDATE knowledge_releases SET published_at = now() WHERE id = @releaseId;",
                releaseId);
        }

        return releaseId;
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        long releaseId,
        long? manifestId = null)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.Parameters.AddWithValue("releaseId", releaseId);
        if (manifestId is not null)
        {
            command.Parameters.AddWithValue("manifestId", manifestId.Value);
        }
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private static async Task ExecuteWithoutReleaseIdAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private static Task SetControlledPublicationAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction) => ExecuteWithoutReleaseIdAsync(
            connection,
            transaction,
            "SET LOCAL jp_reading.controlled_publication = 'on';");

    private static async Task AttachManifestAndPublishAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long releaseId)
    {
        var manifestId = await InsertManifestAsync(connection, transaction);
        await ExecuteAsync(
            connection,
            transaction,
            "INSERT INTO knowledge_release_manifests (release_id, manifest_id) VALUES (@releaseId, @manifestId);",
            releaseId,
            manifestId);
        await SetControlledPublicationAsync(connection, transaction);
        await ExecuteAsync(
            connection,
            transaction,
            "UPDATE knowledge_releases SET published_at = now() WHERE id = @releaseId;",
            releaseId);
    }

    private static async Task<int> ReadBackendIdAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT pg_backend_pid();";
        return (int)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }

    private static async Task<bool> WaitUntilBlockedAsync(
        NpgsqlConnection observerConnection,
        int backendId,
        Task competingTask)
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            await using var command = observerConnection.CreateCommand();
            command.CommandText = "SELECT cardinality(pg_blocking_pids(@backendId)) > 0;";
            command.Parameters.AddWithValue("backendId", backendId);
            if ((bool)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!)
            {
                return true;
            }

            if (competingTask.IsCompleted)
            {
                return false;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken);
        }

        return false;
    }

    private static async Task<long> InsertManifestAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO source_manifests (source_name, version)
            VALUES (@sourceName, '1')
            RETURNING id;
            """;
        command.Parameters.AddWithValue("sourceName", $"test-source-{Guid.NewGuid():N}");
        return (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }

    private static async Task<string> ReadRowVersionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long releaseId)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT xmin::text FROM knowledge_releases WHERE id = @releaseId;";
        command.Parameters.AddWithValue("releaseId", releaseId);
        return (string)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }
}
