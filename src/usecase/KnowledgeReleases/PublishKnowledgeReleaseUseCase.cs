namespace Application.KnowledgeReleases;

public sealed class PublishKnowledgeReleaseUseCase(
    IKnowledgeReleasePublication publication,
    TimeProvider timeProvider)
{
    /// <summary>
    /// EARS[Event]: WHEN controlled publication is requested for an eligible draft release,
    /// the Backend SHALL publish and select it as current atomically (DATA-005).
    /// </summary>
    public Task ExecuteAsync(long releaseId, CancellationToken cancellationToken)
    {
        var publishedAt = timeProvider.GetUtcNow().UtcDateTime;
        return publication.PublishAsync(releaseId, publishedAt, cancellationToken);
    }
}
