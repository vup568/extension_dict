namespace Application.Dictionary.Models;

/// <summary>
/// DTO thông tin giải thích biến đổi từ (Conjugation explanation).
/// </summary>
public sealed record ConjugationDto(
    string SurfaceText,
    string BaseForm,
    string Explanation);

/// <summary>
/// DTO trạng thái các khả năng xử lý của hệ thống (Graceful degradation tracking).
/// </summary>
public sealed record CapabilityStatusDto(
    string Morphology);

/// <summary>
/// Output DTO tổng hợp cho kết quả tra cứu từ điển.
/// </summary>
public sealed record LookupResultDto(
    IReadOnlyList<EntryMatchDto> Matches,
    ConjugationDto? Conjugation,
    CapabilityStatusDto Capabilities);
