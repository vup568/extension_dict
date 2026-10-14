using Application.Conjugation.Models;
using System.Text.Json.Serialization;

namespace Application.Dictionary.Models;

/// <summary>
/// DTO thông tin giải thích biến đổi từ (Conjugation explanation).
/// </summary>
public sealed record ConjugationDto(
    string SurfaceText,
    string BaseForm,
    string Explanation,
    ConjugationExplanationDto? Detail = null);

/// <summary>
/// DTO trạng thái các khả năng xử lý của hệ thống (Graceful degradation tracking).
/// </summary>
public sealed record CapabilityStatusDto(
    string Morphology,
    string Conjugation = "unavailable");

/// <summary>
/// Output DTO tổng hợp cho kết quả tra cứu từ điển.
/// </summary>
public sealed record LookupResultDto(
    IReadOnlyList<EntryMatchDto> Matches,
    ConjugationDto? Conjugation,
    CapabilityStatusDto Capabilities,
    [property: JsonIgnore] IReadOnlyList<ConjugationExplanationDto>? Alternatives = null);
