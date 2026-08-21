// EARS[Entity]: Written Form — orthography of a Dictionary Entry
namespace Domain.Entities;

/// <summary>
/// Dạng biểu ký (orthography) của một Dictionary Entry, ví dụ "食べる".
/// Không tự là identity của toàn entry (REQUIREMENT.md §6.2).
/// </summary>
public class WrittenForm
{
    public long Id { get; set; }
    public long EntryId { get; set; }

    /// <summary>Dạng viết, ví dụ "食べる", "たべる".</summary>
    public string Form { get; set; } = string.Empty;

    /// <summary>Thứ tự ưu tiên từ nguồn dữ liệu.</summary>
    public short? Priority { get; set; }

    /// <summary>Đánh dấu form phổ biến (common).</summary>
    public bool IsCommon { get; set; }

    /// <summary>Thông tin bổ sung từ nguồn (ke_inf trong JMdict).</summary>
    public string[]? Info { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public DictionaryEntry Entry { get; set; } = null!;
    public ICollection<SenseApplicability> SenseApplicabilities { get; set; } = [];
}
