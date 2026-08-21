// EARS[Entity]: Sense Applicability — restriction of sense to form/reading (VOC-007, VOC-008)
namespace Domain.Entities;

/// <summary>
/// Quan hệ giữa Dictionary Sense với Written Form và/hoặc Reading được phép.
/// Giữ restriction để không hiển thị nghĩa cho form/reading không tương thích.
/// Nếu không có applicability nào cho một sense → sense áp dụng cho mọi form/reading.
/// </summary>
public class SenseApplicability
{
    public long Id { get; set; }
    public long SenseId { get; set; }

    /// <summary>Written form mà sense này áp dụng. NULL nếu chỉ restrict reading.</summary>
    public long? WrittenFormId { get; set; }

    /// <summary>Reading mà sense này áp dụng. NULL nếu chỉ restrict form.</summary>
    public long? ReadingId { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public DictionarySense Sense { get; set; } = null!;
    public WrittenForm? WrittenForm { get; set; }
    public Reading? ReadingEntity { get; set; }
}
