// EARS[Entity]: Grammar Rule — canonical grammar pattern (GRM-005, GRM-006)
namespace Domain.Entities;

/// <summary>
/// Canonical grammar pattern có localized meaning, formation, examples,
/// matcher metadata và provenance (GRM-006). Grammar knowledge là versioned
/// data tách khỏi matching engine (GRM-005).
/// </summary>
public class GrammarRule
{
    public long Id { get; set; }

    /// <summary>Canonical identifier, format "grammar:xxxxx" (ID-005).</summary>
    public string CanonicalId { get; set; } = string.Empty;

    /// <summary>Pattern ngữ pháp, ví dụ "～ている".</summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>Cấp JLPT: "N5", "N4", "N3", "N2", "N1".</summary>
    public string? JlptLevel { get; set; }

    /// <summary>Nghĩa tiếng Việt (Vietnamese-first, CONJ-002).</summary>
    public string? MeaningVi { get; set; }

    /// <summary>Nghĩa tiếng Anh.</summary>
    public string? MeaningEn { get; set; }

    /// <summary>Cách cấu tạo/formation.</summary>
    public string? Formation { get; set; }

    /// <summary>Ví dụ minh họa dạng JSON array (JSONB trong DB).</summary>
    public string? Examples { get; set; }

    /// <summary>Metadata cho matcher engine, JSONB linh hoạt (OD-008).</summary>
    public string? MatcherMetadata { get; set; }

    /// <summary>Ghi chú bổ sung.</summary>
    public string? Notes { get; set; }

    /// <summary>Metadata provenance từ nguồn dữ liệu.</summary>
    public string? SourceMetadata { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
