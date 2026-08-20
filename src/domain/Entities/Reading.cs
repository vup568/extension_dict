// EARS[Entity]: Reading — kana reading of a Dictionary Entry
namespace Domain.Entities;

/// <summary>
/// Dạng kana (đọc) của một Dictionary Entry, ví dụ "たべる".
/// Có thể áp dụng cho toàn entry hoặc chỉ một subset form (REQUIREMENT.md §6.2).
/// </summary>
public class Reading
{
    public long Id { get; set; }
    public long EntryId { get; set; }

    /// <summary>Chuỗi kana, ví dụ "たべる".</summary>
    public string ReadingText { get; set; } = string.Empty;

    /// <summary>Thứ tự ưu tiên từ nguồn dữ liệu.</summary>
    public short? Priority { get; set; }

    /// <summary>Đánh dấu reading phổ biến (common).</summary>
    public bool IsCommon { get; set; }

    /// <summary>Thông tin bổ sung từ nguồn (re_inf trong JMdict).</summary>
    public string[]? Info { get; set; }

    /// <summary>
    /// Danh sách written form mà reading này bị giới hạn (re_restr trong JMdict).
    /// NULL hoặc rỗng = áp dụng cho mọi form.
    /// </summary>
    public string[]? RestrictedToForms { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public DictionaryEntry Entry { get; set; } = null!;
    public ICollection<SenseApplicability> SenseApplicabilities { get; set; } = [];
}
