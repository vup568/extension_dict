// EARS[Entity]: Localized Gloss — meaning in a specific language (I18N-003, I18N-004)
namespace Domain.Entities;

/// <summary>
/// Diễn đạt nghĩa của một Dictionary Sense theo language tag ("vi" hoặc "en").
/// Có source/editorial provenance và review status (REQUIREMENT.md §6.2).
/// Thêm/sửa gloss không thay canonical identity của Sense hay Entry.
/// </summary>
public class LocalizedGloss
{
    public long Id { get; set; }
    public long SenseId { get; set; }

    /// <summary>Language tag: "vi" hoặc "en" (I18N-003).</summary>
    public string LanguageTag { get; set; } = string.Empty;

    /// <summary>Nội dung nghĩa đã được localized.</summary>
    public string GlossText { get; set; } = string.Empty;

    /// <summary>FK tới source_records — provenance của gloss (OD-009).</summary>
    public long? SourceRecordId { get; set; }

    /// <summary>Trạng thái review: "pending", "approved", "rejected".</summary>
    public string? ReviewStatus { get; set; }

    /// <summary>Thứ tự hiển thị trong cùng sense + language.</summary>
    public short Position { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public DictionarySense Sense { get; set; } = null!;
    public SourceRecord? SourceRecord { get; set; }
}
