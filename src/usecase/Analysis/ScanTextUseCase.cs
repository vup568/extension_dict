namespace Application.Analysis;

using System.Buffers;
using System.Text;
using System.Text.Json.Serialization;
using Application.Analysis.Models;
using Application.Conjugation.Models;
using Application.Dictionary;

public sealed record ScanResultDto(
    [property: JsonPropertyName("analysis")] AnalysisResultDto Analysis,
    [property: JsonPropertyName("target_span")] ConjugationSpanDto? TargetSpan);

public sealed class ScanTextUseCase(AnalyzeTextUseCase analyze)
{
    public const int MaxContextLength = 64;

    public async Task<ScanResultDto> ExecuteAsync(string? text, int? offset, string? interactionId,
        CancellationToken cancellationToken = default)
    {
        // EARS[Unwanted] YSC-03/06: Validate the whole window and the Japanese UTF-16 point before analysis.
        if (text is null || offset is not int position || !TryGetPoint(text, position, out var point)
            || interactionId?.Length != 36 || !Guid.TryParseExact(interactionId, "D", out _))
            throw new ArgumentException("Yêu cầu quét từ không hợp lệ.");

        var analysis = await analyze.ExecuteAsync(new AnalysisRequestDto(text, interactionId),
            cancellationToken, includeSentenceVocabulary: true);

        // EARS[Event] YSC-06: Use the exact-first whole-word result as one complete occurrence.
        if (analysis.VocabularyOccurrences is null && analysis.DictionaryMatches.Count > 0)
        {
            analysis = analysis with
            {
                VocabularyOccurrences = [new VocabularyOccurrence(text, new ConjugationSpanDto(0, text.Length),
                    analysis.DictionaryMatches, analysis.Conjugations)]
            };
        }

        var target = analysis.VocabularyOccurrences?
            .Where(occurrence => occurrence.Span.Start <= position && position < occurrence.Span.End)
            .OrderByDescending(occurrence => occurrence.Span.End - occurrence.Span.Start)
            .ThenBy(occurrence => occurrence.Span.Start)
            .FirstOrDefault()?.Span;

        if (target is null)
        {
            var character = point.ToString();
            analysis = analysis with { Kanji = analysis.Kanji.Where(item => item.Character == character).ToArray() };
        }

        return new ScanResultDto(analysis, target);
    }

    private static bool TryGetPoint(string text, int offset, out Rune point)
    {
        point = default;
        if (text.Length == 0 || text.Length > MaxContextLength || offset < 0 || offset >= text.Length
            || char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[^1]) || text[0] == '\uFEFF' || text[^1] == '\uFEFF')
            return false;

        for (var index = 0; index < text.Length;)
        {
            if (Rune.DecodeFromUtf16(text.AsSpan(index), out var rune, out var length) != OperationStatus.Done)
                return false;
            if (index == offset) point = rune;
            index += length;
        }

        // Match the extension's F-01 Unicode 17.0.0 trigger ranges, including supplementary kana/kanji.
        return point.Value is 0x3007 or (>= 0x3041 and <= 0x3096) or 0x309F
            or (>= 0x30A1 and <= 0x30FA) or 0x30FF or (>= 0x31F0 and <= 0x31FF)
            or (>= 0x3400 and <= 0x4DBF) or (>= 0x4E00 and <= 0x9FFF)
            or (>= 0xF900 and <= 0xFA6D) or (>= 0xFA70 and <= 0xFAD9)
            or (>= 0xFF66 and <= 0xFF6F) or (>= 0xFF71 and <= 0xFF9D)
            or (>= 0x1B000 and <= 0x1B122) or 0x1B132 or (>= 0x1B150 and <= 0x1B152)
            or 0x1B155 or (>= 0x1B164 and <= 0x1B167)
            or (>= 0x20000 and <= 0x2A6DF) or (>= 0x2A700 and <= 0x2B81D)
            or (>= 0x2B820 and <= 0x2CEAD) or (>= 0x2CEB0 and <= 0x2EBE0)
            or (>= 0x2EBF0 and <= 0x2EE5D) or (>= 0x2F800 and <= 0x2FA1D)
            or (>= 0x30000 and <= 0x3134A) or (>= 0x31350 and <= 0x33479);
    }
}
