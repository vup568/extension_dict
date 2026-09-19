namespace Application.Analysis.Models;

using System.Text.Json.Serialization;
using Application.Dictionary.Models;
using Application.Grammar.Models;
using Application.Kanji.Models;

/// <summary>
/// Yêu cầu phân tích tổng hợp (Unified Analysis).
/// </summary>
public sealed record AnalysisRequestDto(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("interaction_id")] string? InteractionId = null,
    [property: JsonPropertyName("context")] string? Context = null);

/// <summary>
/// Hằng số trạng thái cho mỗi capability trong phân tích tổng hợp.
/// EARS[Ubiquitous]: Partial Result Semantics — mỗi capability có status riêng.
/// </summary>
public static class CapabilityStatus
{
    public const string Completed = "completed";
    public const string Unavailable = "unavailable";
    public const string Failed = "failed";
}

/// <summary>
/// Map trạng thái khả năng xử lý của từng thành phần phân tích.
/// </summary>
public sealed record AnalysisCapabilitiesDto(
    [property: JsonPropertyName("vocabulary")] string Vocabulary,
    [property: JsonPropertyName("kanji")] string Kanji,
    [property: JsonPropertyName("grammar")] string Grammar,
    [property: JsonPropertyName("morphology")] string Morphology,
    [property: JsonPropertyName("conjugation")] string Conjugation);

/// <summary>
/// Output DTO tổng hợp cho kết quả phân tích Unified Analysis (data portion).
/// Reuse các DTO có sẵn từ F-06 (EntryMatchDto), F-07 (KanjiDetailDto), F-08 (GrammarOccurrenceDto).
/// </summary>
public sealed record AnalysisResultDto(
    [property: JsonPropertyName("normalized_text")] string NormalizedText,
    [property: JsonPropertyName("tokens")] IReadOnlyList<object> Tokens,
    [property: JsonPropertyName("dictionary_matches")] IReadOnlyList<EntryMatchDto> DictionaryMatches,
    [property: JsonPropertyName("kanji")] IReadOnlyList<KanjiDetailDto> Kanji,
    [property: JsonPropertyName("conjugations")] IReadOnlyList<object> Conjugations,
    [property: JsonPropertyName("grammar_occurrences")] IReadOnlyList<GrammarOccurrenceDto> GrammarOccurrences,
    [property: JsonPropertyName("capabilities")] AnalysisCapabilitiesDto Capabilities);
