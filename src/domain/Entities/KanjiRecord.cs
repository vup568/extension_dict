// EARS[Entity]: Kanji Record — canonical kanji character (KAN-001, KAN-002)
namespace Domain.Entities;

/// <summary>
/// Canonical record cho một character kanji. Phân biệt giá trị authoritative
/// (grade, stroke_count) với giá trị derived/approximate (jlpt_level) theo KAN-002.
/// </summary>
public class KanjiRecord
{
    public long Id { get; set; }

    /// <summary>Canonical identifier, format "kanji:xxxxx" (ID-005).</summary>
    public string CanonicalId { get; set; } = string.Empty;

    /// <summary>Ký tự kanji, ví dụ "食".</summary>
    public string Character { get; set; } = string.Empty;

    /// <summary>Số nét (authoritative từ KANJIDIC2).</summary>
    public short? StrokeCount { get; set; }

    /// <summary>Cấp học tiểu học/trung học Nhật Bản (1-6, 8, 9-10).</summary>
    public short? Grade { get; set; }

    /// <summary>Cấp JLPT ước lượng (derived/approximate, KAN-002).</summary>
    public short? JlptLevel { get; set; }

    /// <summary>Nguồn gốc giá trị JLPT: "authoritative", "derived", "approximate".</summary>
    public string? JlptProvenance { get; set; }

    /// <summary>Thứ hạng tần suất sử dụng.</summary>
    public short? Frequency { get; set; }

    /// <summary>Unicode codepoint, ví dụ "U+98DF".</summary>
    public string UnicodeCodepoint { get; set; } = string.Empty;

    /// <summary>Số bộ thủ cổ điển (1-214).</summary>
    public short? RadicalNumber { get; set; }

    /// <summary>Âm Hán Việt, ví dụ "THỰC".</summary>
    public string? HanViet { get; set; }

    /// <summary>Danh sách 音読み (on'yomi).</summary>
    public string[]? OnReadings { get; set; }

    /// <summary>Danh sách 訓読み (kun'yomi).</summary>
    public string[]? KunReadings { get; set; }

    /// <summary>Nghĩa tiếng Anh từ KANJIDIC2.</summary>
    public string[]? MeaningsEn { get; set; }

    /// <summary>Nghĩa tiếng Việt (nếu có).</summary>
    public string[]? MeaningsVi { get; set; }

    /// <summary>Cách đọc dùng cho tên riêng (nanori).</summary>
    public string[]? NanoriReadings { get; set; }

    /// <summary>Metadata bổ sung từ nguồn dữ liệu.</summary>
    public string? SourceMetadata { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
