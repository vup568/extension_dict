// EARS[Entity]: Dictionary Sense — language-neutral semantic unit (VOC-007, VOC-008)
namespace Domain.Entities;

/// <summary>
/// Đơn vị ngữ nghĩa language-neutral thuộc một Dictionary Entry.
/// Có sense key ổn định trong parent entry, giữ part of speech
/// và các localized gloss / sense applicability (REQUIREMENT.md §6.2).
/// </summary>
public class DictionarySense
{
    public long Id { get; set; }
    public long EntryId { get; set; }

    /// <summary>Khóa ổn định trong parent entry (ví dụ: "1", "2").</summary>
    public string SenseKey { get; set; } = string.Empty;

    /// <summary>Danh sách part of speech (ví dụ: ["noun", "verb"]).</summary>
    public string[]? PartOfSpeech { get; set; }

    /// <summary>Thứ tự hiển thị trong entry.</summary>
    public short Position { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public DictionaryEntry Entry { get; set; } = null!;
    public ICollection<LocalizedGloss> Glosses { get; set; } = [];
    public ICollection<SenseApplicability> Applicabilities { get; set; } = [];
}
