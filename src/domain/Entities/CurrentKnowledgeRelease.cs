// EARS[Entity]: Current Knowledge Release pointer (DATA-005)
namespace Domain.Entities;

/// <summary>
/// Singleton pointer to the published release consumed as current knowledge.
/// The pointer is mutable; the referenced published release remains immutable.
/// </summary>
public class CurrentKnowledgeRelease
{
    public const short SingletonKey = 1;

    private CurrentKnowledgeRelease() { }

    public CurrentKnowledgeRelease(KnowledgeRelease release)
    {
        PointTo(release);
    }

    public short SingletonId { get; private set; } = SingletonKey;
    public long ReleaseId { get; private set; }
    public KnowledgeRelease Release { get; private set; } = null!;

    /// <summary>
    /// EARS[State]: WHILE a Knowledge Release is a draft,
    /// it SHALL NOT become the current knowledge release (DATA-005).
    /// </summary>
    public void PointTo(KnowledgeRelease release)
    {
        ArgumentNullException.ThrowIfNull(release);

        if (release.PublishedAt is null)
        {
            throw new InvalidOperationException("Only a published knowledge release can become current.");
        }

        Release = release;
        ReleaseId = release.Id;
    }
}
