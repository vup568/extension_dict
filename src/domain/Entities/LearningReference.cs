// EARS[Entity]: Learning Reference — user-owned saved resource (LEARN-001..008)
namespace Domain.Entities;

/// <summary>
/// Reference riêng tư của User Account tới một Canonical Linguistic Resource.
/// MVP target Dictionary Entry hoặc Grammar Rule, không dùng localized text
/// làm identity (ID-003, LEARN-002). Soft delete theo LEARN-008.
/// </summary>
public class LearningReference
{
    public long Id { get; set; }
    public long UserId { get; set; }

    /// <summary>Loại resource: "dictionary" hoặc "grammar".</summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>ID trong bảng dictionary_entries hoặc grammar_rules.</summary>
    public long ResourceId { get; set; }

    /// <summary>Thời điểm user lưu resource.</summary>
    public DateTime SavedAt { get; set; }

    /// <summary>
    /// Soft delete timestamp. NULL = active (LEARN-008).
    /// Deleted item bị loại khỏi active queries nhưng retained record còn phục hồi được.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}
