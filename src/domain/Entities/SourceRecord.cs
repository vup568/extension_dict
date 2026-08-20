// EARS[Entity]: Source Record — individual fact from a source (ID-004)
namespace Domain.Entities;

/// <summary>
/// Một fact/record từ Source Manifest cụ thể. Liên kết tới Canonical
/// Linguistic Resource qua Editorial Mapping khi equivalence đã được review (ID-004).
/// </summary>
public class SourceRecord
{
    public long Id { get; set; }
    public long ManifestId { get; set; }

    /// <summary>Upstream ID, ví dụ JMdict ent_seq "1234567".</summary>
    public string SourceIdentity { get; set; } = string.Empty;

    /// <summary>Dữ liệu record gốc dạng JSON.</summary>
    public string RecordData { get; set; } = "{}";

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public SourceManifest Manifest { get; set; } = null!;
    public ICollection<EditorialMapping> EditorialMappings { get; set; } = [];
    public ICollection<LocalizedGloss> Glosses { get; set; } = [];
}
