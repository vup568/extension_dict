// EARS[Entity]: Knowledge Release — immutable snapshot (DATA-005)
namespace Domain.Entities;

/// <summary>
/// Snapshot bất biến của linguistic data đã được validate, có provenance
/// và có thể được publish làm current knowledge cho Backend (DATA-005).
/// Chỉ controlled publication mới thay current facts.
/// </summary>
public class KnowledgeRelease
{
    public long Id { get; set; }

    /// <summary>Phiên bản release, ví dụ "1.0.0".</summary>
    public string Version { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>NULL nếu draft, set khi publish.</summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>Chỉ một release được đánh dấu current tại một thời điểm.</summary>
    public bool IsCurrent { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<KnowledgeReleaseManifest> ReleaseManifests { get; set; } = [];
    public ICollection<ResourceRevision> Revisions { get; set; } = [];
}
