namespace Application.Dictionary.Models;

/// <summary>
/// DTO đại diện cho dạng viết (orthography) của entry.
/// </summary>
public sealed record WrittenFormDto(
    long Id,
    string Form,
    short? Priority,
    bool IsCommon,
    IReadOnlyList<string>? Info);

/// <summary>
/// DTO đại diện cho dạng đọc (Kana) của entry.
/// </summary>
public sealed record ReadingDto(
    long Id,
    string ReadingText,
    short? Priority,
    bool IsCommon,
    IReadOnlyList<string>? Info,
    IReadOnlyList<string>? RestrictedToForms);

/// <summary>
/// DTO đại diện cho 1 kết quả từ vựng khớp với truy vấn tra cứu từ điển.
/// </summary>
public sealed record EntryMatchDto(
    string CanonicalId,
    string? MatchedWrittenForm,
    string? MatchedReading,
    IReadOnlyList<WrittenFormDto> WrittenForms,
    IReadOnlyList<ReadingDto> Readings,
    IReadOnlyList<SenseDto> Senses);
