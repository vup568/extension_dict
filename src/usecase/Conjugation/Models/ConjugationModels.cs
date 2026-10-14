using System.Text.Json.Serialization;

namespace Application.Conjugation.Models;

public sealed record ConjugationToken(
    string Category, string Inflection, string Surface, string BaseForm, int Start, int End);

public sealed record ConjugationRuleTokenPattern(
    string Category, IReadOnlyList<string> Inflections, string? ExplanationVi);

public sealed record ConjugationRulePattern(
    string RuleId, int RuleVersion, string? ExplanationVi,
    IReadOnlyList<ConjugationRuleTokenPattern> Tokens);

public sealed record ConjugationSpanDto(
    [property: JsonPropertyName("start")] int Start,
    [property: JsonPropertyName("end")] int End,
    [property: JsonPropertyName("unit")] string Unit = "utf16");

public sealed record ConjugationSegmentDto(
    [property: JsonPropertyName("surface")] string Surface,
    [property: JsonPropertyName("explanation")] string Explanation,
    [property: JsonPropertyName("span")] ConjugationSpanDto Span);

public sealed record ConjugationExplanationDto(
    [property: JsonPropertyName("surface")] string Surface,
    [property: JsonPropertyName("base_form")] string BaseForm,
    [property: JsonPropertyName("explanation")] string Explanation,
    [property: JsonPropertyName("language_tag")] string LanguageTag,
    [property: JsonPropertyName("span")] ConjugationSpanDto Span,
    [property: JsonPropertyName("rule_id")] string RuleId,
    [property: JsonPropertyName("rule_version")] int RuleVersion,
    [property: JsonPropertyName("dataset_version")] string DatasetVersion,
    [property: JsonPropertyName("segments")] IReadOnlyList<ConjugationSegmentDto> Segments);

public sealed record ConjugationMatchResult(
    ConjugationExplanationDto? Explanation, bool IsContentAvailable = true);
