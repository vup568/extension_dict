namespace Application.KnowledgeReleases;

/// <summary>
/// Transaction boundary for publishing one release and selecting it as current.
/// </summary>
public interface IKnowledgeReleasePublication
{
    Task PublishAsync(long releaseId, DateTime publishedAt, CancellationToken cancellationToken);
}
