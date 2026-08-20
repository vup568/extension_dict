// Join entity: Knowledge Release ↔ Source Manifest (M:N)
namespace Domain.Entities;

/// <summary>
/// Bảng trung gian M:N giữa Knowledge Release và Source Manifest.
/// Một release sử dụng nhiều manifest, một manifest có thể thuộc nhiều release.
/// </summary>
public class KnowledgeReleaseManifest
{
    public long ReleaseId { get; set; }
    public long ManifestId { get; set; }

    // Navigation properties
    public KnowledgeRelease Release { get; set; } = null!;
    public SourceManifest Manifest { get; set; } = null!;
}
