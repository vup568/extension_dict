// EARS[Entity]: Source Manifest — dataset provenance (DATA-001)
namespace Domain.Entities;

/// <summary>
/// Provenance của một dataset release: publisher, version, license,
/// attribution, transformation, checksum và validation evidence (DATA-001).
/// Là evidence bắt buộc cho Knowledge Release.
/// </summary>
public class SourceManifest
{
    public long Id { get; set; }

    /// <summary>Tên nguồn: "JMdict", "KANJIDIC2", "N5-N4 Grammar Corpus".</summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>Nhà xuất bản, ví dụ "EDRDG".</summary>
    public string? Publisher { get; set; }

    /// <summary>Phiên bản/ngày của source release.</summary>
    public string Version { get; set; } = string.Empty;

    public string? License { get; set; }
    public string? Attribution { get; set; }

    /// <summary>SHA256 checksum của file nguồn.</summary>
    public string? Checksum { get; set; }

    public int? RecordCount { get; set; }
    public int? RejectedCount { get; set; }

    /// <summary>Pipeline version/commit đã tạo ra dữ liệu.</summary>
    public string? TransformationId { get; set; }

    public string? ValidationResult { get; set; }

    /// <summary>Metadata bổ sung dạng JSON.</summary>
    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<SourceRecord> Records { get; set; } = [];
    public ICollection<KnowledgeReleaseManifest> ReleaseManifests { get; set; } = [];
}
