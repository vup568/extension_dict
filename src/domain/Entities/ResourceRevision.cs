// EARS[Entity]: Resource Revision — snapshot of resource in a release (ID-006)
namespace Domain.Entities;

/// <summary>
/// Facts, presentation hoặc provenance của một Canonical Linguistic Resource
/// trong một Knowledge Release. Immutable sau publish; không đổi canonical identifier (ID-006).
/// </summary>
public class ResourceRevision
{
    public long Id { get; set; }
    public long ReleaseId { get; set; }

    /// <summary>Loại resource: "dictionary", "kanji", "grammar".</summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>ID trong bảng resource tương ứng.</summary>
    public long ResourceId { get; set; }

    /// <summary>Snapshot data dạng JSON.</summary>
    public string RevisionData { get; set; } = "{}";

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public KnowledgeRelease Release { get; set; } = null!;
}
