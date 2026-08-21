using System.Data;
using Application.KnowledgeReleases;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.KnowledgeReleases;

public sealed class EfKnowledgeReleasePublication(
    IDbContextFactory<AppDbContext> dbContextFactory) : IKnowledgeReleasePublication
{
    /// <summary>
    /// EARS[Event]: WHEN controlled publication succeeds,
    /// publishing the draft and changing the current pointer SHALL commit atomically (DATA-005).
    /// </summary>
    public async Task PublishAsync(
        long releaseId,
        DateTime publishedAt,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "SET LOCAL jp_reading.controlled_publication = 'on'",
                cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(
                "LOCK TABLE current_knowledge_release IN EXCLUSIVE MODE",
                cancellationToken);

            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT id FROM knowledge_releases WHERE id = {releaseId} FOR UPDATE",
                cancellationToken);

            var release = await dbContext.KnowledgeReleases
                .Include(candidate => candidate.ReleaseManifests)
                .SingleOrDefaultAsync(candidate => candidate.Id == releaseId, cancellationToken)
                ?? throw new KeyNotFoundException($"Knowledge release {releaseId} was not found.");

            if (release.ReleaseManifests.Count == 0)
            {
                throw new InvalidOperationException("A knowledge release requires at least one source manifest before publication.");
            }

            release.Publish(publishedAt);
            await dbContext.SaveChangesAsync(cancellationToken);

            var current = await dbContext.CurrentKnowledgeRelease
                .SingleOrDefaultAsync(cancellationToken);

            if (current is null)
            {
                dbContext.CurrentKnowledgeRelease.Add(new CurrentKnowledgeRelease(release));
            }
            else
            {
                current.PointTo(release);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
