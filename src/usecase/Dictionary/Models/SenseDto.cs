namespace Application.Dictionary.Models;

/// <summary>
/// DTO chứa nội dung dịch nghĩa theo language tag.
/// </summary>
public sealed record GlossDto(
    string LanguageTag,
    string GlossText);

/// <summary>
/// DTO chứa ràng buộc ngữ nghĩa (restriction) của sense đối với written form hoặc reading.
/// </summary>
public sealed record SenseRestrictionDto(
    long? WrittenFormId,
    long? ReadingId);

/// <summary>
/// DTO đại diện cho một nét nghĩa (Sense) của từ vựng tiếng Nhật.
/// Tuân thủ VOC-004, VOC-005 (Vietnamese-first, English fallback) và VOC-007, VOC-008 (Match Provenance).
/// </summary>
public sealed record SenseDto(
    string SenseKey,
    IReadOnlyList<string> PartOfSpeech,
    short Position,
    IReadOnlyList<GlossDto> Glosses,
    IReadOnlyList<SenseRestrictionDto> Restrictions);
