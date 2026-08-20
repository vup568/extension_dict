// EARS[Entity]: Editorial Mapping — reviewed link between source and canonical (ID-004, ID-006)
namespace Domain.Entities;

/// <summary>
/// Quyết định review liên kết Source Record với canonical resource.
/// Không được suy ra chỉ bằng form/reading trùng (ID-004, REQUIREMENT.md §6.2).
/// </summary>
public class EditorialMapping
{
    public long Id { get; set; }
    public long SourceRecordId { get; set; }

    /// <summary>Loại resource đích: "dictionary", "kanji", "grammar".</summary>
    public string TargetResourceType { get; set; } = string.Empty;

    /// <summary>ID trong bảng resource tương ứng (polymorphic).</summary>
    public long TargetResourceId { get; set; }

    /// <summary>Kiểu mapping: "link", "merge", "split", "retire".</summary>
    public string MappingType { get; set; } = string.Empty;

    public string? DecisionReason { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public SourceRecord SourceRecord { get; set; } = null!;
}
