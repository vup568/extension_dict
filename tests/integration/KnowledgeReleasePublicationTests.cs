using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.KnowledgeReleases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace IntegrationTests;

public sealed class KnowledgeReleasePublicationTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task Publish_sets_the_timestamp_and_switches_the_current_pointer_atomically()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        var publishedAt = new DateTime(2026, 8, 21, 10, 0, 0, DateTimeKind.Utc);
        long releaseId;

        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync(TestContext.Current.CancellationToken);

            var release = new KnowledgeRelease
            {
                Version = $"publication-{Guid.NewGuid():N}",
                Description = "Validated candidate"
            };
            var manifest = new SourceManifest
            {
                SourceName = $"test-source-{Guid.NewGuid():N}",
                Version = "1",
                ValidationResult = "passed"
            };
            release.ReleaseManifests.Add(new KnowledgeReleaseManifest
            {
                Release = release,
                Manifest = manifest
            });
            release.Revisions.Add(new ResourceRevision
            {
                Release = release,
                ResourceType = "dictionary",
                ResourceId = 1,
                RevisionData = "{}"
            });

            setupContext.KnowledgeReleases.Add(release);
            await setupContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            releaseId = release.Id;
        }

        var publication = new EfKnowledgeReleasePublication(new PooledDbContextFactory<AppDbContext>(options));
        await publication.PublishAsync(releaseId, publishedAt, TestContext.Current.CancellationToken);

        await using var verificationContext = new AppDbContext(options);
        var releaseState = await verificationContext.KnowledgeReleases
            .AsNoTracking()
            .SingleAsync(release => release.Id == releaseId, TestContext.Current.CancellationToken);
        var current = await verificationContext.CurrentKnowledgeRelease
            .AsNoTracking()
            .SingleAsync(TestContext.Current.CancellationToken);

        releaseState.PublishedAt.Should().Be(publishedAt);
        current.ReleaseId.Should().Be(releaseId);
    }

    [Fact]
    public async Task Publish_rejects_a_release_without_source_manifest_and_keeps_it_as_draft()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        var publishedAt = new DateTime(2026, 8, 21, 10, 0, 0, DateTimeKind.Utc);
        long releaseId;

        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync(TestContext.Current.CancellationToken);
            var release = new KnowledgeRelease
            {
                Version = $"missing-manifest-{Guid.NewGuid():N}"
            };
            setupContext.KnowledgeReleases.Add(release);
            await setupContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            releaseId = release.Id;
        }

        var publication = new EfKnowledgeReleasePublication(new PooledDbContextFactory<AppDbContext>(options));
        var publish = () => publication.PublishAsync(
            releaseId,
            publishedAt,
            TestContext.Current.CancellationToken);

        await publish.Should().ThrowAsync<InvalidOperationException>();

        await using var verificationContext = new AppDbContext(options);
        var releaseState = await verificationContext.KnowledgeReleases
            .AsNoTracking()
            .SingleAsync(release => release.Id == releaseId, TestContext.Current.CancellationToken);
        var current = await verificationContext.CurrentKnowledgeRelease
            .AsNoTracking()
            .SingleOrDefaultAsync(TestContext.Current.CancellationToken);

        releaseState.PublishedAt.Should().BeNull();
        current?.ReleaseId.Should().NotBe(releaseId);
    }

    [Fact]
    public async Task Publish_rolls_back_the_release_when_switching_current_fails()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        var publishedAt = new DateTime(2026, 8, 21, 10, 0, 0, DateTimeKind.Utc);
        long releaseId;

        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync(TestContext.Current.CancellationToken);
            var release = new KnowledgeRelease
            {
                Version = $"rollback-{Guid.NewGuid():N}"
            };
            var manifest = new SourceManifest
            {
                SourceName = $"rollback-source-{Guid.NewGuid():N}",
                Version = "1"
            };
            release.ReleaseManifests.Add(new KnowledgeReleaseManifest
            {
                Release = release,
                Manifest = manifest
            });
            setupContext.KnowledgeReleases.Add(release);
            await setupContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            releaseId = release.Id;

            await setupContext.Database.ExecuteSqlRawAsync(
                """
                CREATE OR REPLACE FUNCTION test_fail_current_pointer()
                RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'Simulated current pointer failure.' USING ERRCODE = 'P0001';
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_test_fail_current_pointer
                    BEFORE INSERT OR UPDATE ON current_knowledge_release
                    FOR EACH ROW
                    EXECUTE FUNCTION test_fail_current_pointer();
                """,
                TestContext.Current.CancellationToken);
        }

        var publication = new EfKnowledgeReleasePublication(new PooledDbContextFactory<AppDbContext>(options));

        try
        {
            var publish = () => publication.PublishAsync(
                releaseId,
                publishedAt,
                TestContext.Current.CancellationToken);

            var thrown = await publish.Should().ThrowAsync<DbUpdateException>();
            thrown.Which.InnerException.Should().BeOfType<Npgsql.PostgresException>()
                .Which.SqlState.Should().Be("P0001");
        }
        finally
        {
            await using var cleanupContext = new AppDbContext(options);
            await cleanupContext.Database.ExecuteSqlRawAsync(
                """
                DROP TRIGGER IF EXISTS trg_test_fail_current_pointer ON current_knowledge_release;
                DROP FUNCTION IF EXISTS test_fail_current_pointer();
                """,
                TestContext.Current.CancellationToken);
        }

        var retry = () => publication.PublishAsync(
            releaseId,
            publishedAt,
            TestContext.Current.CancellationToken);
        await retry.Should().NotThrowAsync();

        await using var verificationContext = new AppDbContext(options);
        var releaseState = await verificationContext.KnowledgeReleases
            .AsNoTracking()
            .SingleAsync(release => release.Id == releaseId, TestContext.Current.CancellationToken);

        releaseState.PublishedAt.Should().Be(publishedAt);
    }
}
