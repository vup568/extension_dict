namespace Application.Grammar.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Token được chuẩn hóa từ Sudachi Tokenizer Sidecar.
/// </summary>
public record NormalizedTokenDto(
    [property: JsonPropertyName("surface")] string Surface,
    [property: JsonPropertyName("base_form")] string BaseForm,
    [property: JsonPropertyName("pos")] string Pos,
    [property: JsonPropertyName("span_start")] int SpanStart,
    [property: JsonPropertyName("span_end")] int SpanEnd
);

/// <summary>
/// Yêu cầu tra cứu ngữ pháp.
/// </summary>
public record GrammarDetectionRequestDto(
    [property: JsonPropertyName("text")] string Text
);

/// <summary>
/// Offset vị trí ký tự của ngữ pháp xuất hiện trong input text.
/// </summary>
public record TextSpanDto(
    [property: JsonPropertyName("start")] int Start,
    [property: JsonPropertyName("end")] int End
);

/// <summary>
/// Kết quả một mẫu ngữ pháp phát hiện được.
/// </summary>
public record GrammarOccurrenceDto(
    [property: JsonPropertyName("grammar_id")] string GrammarId,
    [property: JsonPropertyName("pattern")] string Pattern,
    [property: JsonPropertyName("jlpt_level")] string? JlptLevel,
    [property: JsonPropertyName("meaning_vi")] string? MeaningVi,
    [property: JsonPropertyName("matched_text")] string MatchedText,
    [property: JsonPropertyName("span")] TextSpanDto Span
);

/// <summary>
/// Kết quả phản hồi tổng thể từ Use Case.
/// Status có thể là "completed" hoặc "failed" (Graceful degradation).
/// </summary>
public record GrammarDetectionResultDto(
    [property: JsonPropertyName("occurrences")] IReadOnlyList<GrammarOccurrenceDto> Occurrences,
    [property: JsonPropertyName("status")] string Status
);
