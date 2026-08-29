namespace Application.Grammar.Helpers;

using System.Text.Json;
using Application.Grammar.Models;
using Domain.Entities;

public class TokenMatcherSpec
{
    public string? Surface { get; set; }
    public string? Base { get; set; }
    public string? Pos { get; set; }
    public bool Optional { get; set; }
}

public class GrammarMatcherConfig
{
    public List<TokenMatcherSpec> Tokens { get; set; } = new();
}

/// <summary>
/// Engine so khớp chuỗi token (Token-Sequence Matcher Engine)
/// Hỗ trợ: Sliding window không bỏ vị trí token, Wildcard POS matching,
/// Optional tokens, và bảo tồn tất cả các pattern trùng lấp (overlaps).
/// </summary>
public class GrammarSequenceMatcher
{
    public IReadOnlyList<GrammarOccurrenceDto> Match(
        IReadOnlyList<NormalizedTokenDto> tokens,
        IReadOnlyList<GrammarRule> rules,
        string originalText)
    {
        if (tokens.Count == 0 || rules.Count == 0)
            return Array.Empty<GrammarOccurrenceDto>();

        var occurrences = new List<GrammarOccurrenceDto>();

        // Pre-parse matcher metadata của tất cả các rules
        var parsedRules = rules
            .Select(r => (Rule: r, Specs: ParseMatcherSpecs(r.MatcherMetadata, r.Pattern)))
            .Where(pr => pr.Specs.Count > 0)
            .ToList();

        // Sliding window: Duyệt từng vị trí token i
        for (int i = 0; i < tokens.Count; i++)
        {
            foreach (var (rule, specs) in parsedRules)
            {
                var matchedSpans = MatchRuleAt(tokens, i, specs);
                foreach (var (startTokenIdx, endTokenIdx) in matchedSpans)
                {
                    int spanStart = tokens[startTokenIdx].SpanStart;
                    int spanEnd = tokens[endTokenIdx].SpanEnd;

                    // Lấy matched text thực tế từ originalText
                    string matchedText;
                    if (spanStart >= 0 && spanEnd <= originalText.Length && spanStart < spanEnd)
                    {
                        matchedText = originalText[spanStart..spanEnd];
                    }
                    else
                    {
                        matchedText = string.Concat(
                            tokens.Skip(startTokenIdx)
                                  .Take(endTokenIdx - startTokenIdx + 1)
                                  .Select(t => t.Surface)
                        );
                    }

                    occurrences.Add(new GrammarOccurrenceDto(
                        GrammarId: rule.CanonicalId,
                        Pattern: rule.Pattern,
                        JlptLevel: rule.JlptLevel,
                        MeaningVi: rule.MeaningVi,
                        MatchedText: matchedText,
                        Span: new TextSpanDto(spanStart, spanEnd)
                    ));
                }
            }
        }

        // Deduplicate & Order: Giữ nguyên overlaps nhưng ưu tiên Longest Match ở cùng vị trí
        return occurrences
            .GroupBy(o => (o.GrammarId, o.Span.Start, o.Span.End))
            .Select(g => g.First())
            .OrderBy(o => o.Span.Start)
            .ThenByDescending(o => o.Span.End - o.Span.Start)
            .ToList();
    }

    private static List<(int StartTokenIdx, int EndTokenIdx)> MatchRuleAt(
        IReadOnlyList<NormalizedTokenDto> tokens,
        int startIdx,
        List<TokenMatcherSpec> specs)
    {
        var results = new List<(int StartTokenIdx, int EndTokenIdx)>();
        BacktrackMatch(tokens, startIdx, specs, 0, startIdx, -1, results);
        return results;
    }

    private static void BacktrackMatch(
        IReadOnlyList<NormalizedTokenDto> tokens,
        int currentTokenIdx,
        List<TokenMatcherSpec> specs,
        int specIdx,
        int matchStartTokenIdx,
        int lastMatchedTokenIdx,
        List<(int StartTokenIdx, int EndTokenIdx)> results)
    {
        if (specIdx == specs.Count)
        {
            if (lastMatchedTokenIdx >= matchStartTokenIdx)
            {
                results.Add((matchStartTokenIdx, lastMatchedTokenIdx));
            }
            return;
        }

        var spec = specs[specIdx];

        // Trường hợp spec là Optional: Thử bỏ qua spec này
        if (spec.Optional)
        {
            BacktrackMatch(tokens, currentTokenIdx, specs, specIdx + 1, matchStartTokenIdx, lastMatchedTokenIdx, results);
        }

        // Trường hợp thử match token hiện tại với spec
        if (currentTokenIdx < tokens.Count)
        {
            var token = tokens[currentTokenIdx];
            if (IsMatch(token, spec))
            {
                BacktrackMatch(tokens, currentTokenIdx + 1, specs, specIdx + 1, matchStartTokenIdx, currentTokenIdx, results);
            }
        }
    }

    private static bool IsMatch(NormalizedTokenDto token, TokenMatcherSpec spec)
    {
        if (!string.IsNullOrEmpty(spec.Surface) && !MatchFieldWithOr(spec.Surface, token.Surface))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(spec.Base) && !MatchFieldWithOr(spec.Base, token.BaseForm))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(spec.Pos) && !MatchPos(spec.Pos, token.Pos))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// So khớp giá trị trường có hỗ trợ toán tử OR (|).
    /// Ví dụ: pattern "て|で" sẽ khớp nếu tokenValue là "て" HOẶC "で".
    /// </summary>
    private static bool MatchFieldWithOr(string pattern, string tokenValue)
    {
        if (pattern.Contains('|'))
        {
            var alternatives = pattern.Split('|');
            return alternatives.Any(alt =>
                string.Equals(alt.Trim(), tokenValue, StringComparison.OrdinalIgnoreCase));
        }

        return string.Equals(pattern, tokenValue, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchPos(string patternPos, string tokenPos)
    {
        if (patternPos == "*") return true;

        if (patternPos.EndsWith("*"))
        {
            var prefix = patternPos[..^1];
            return tokenPos.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(patternPos, tokenPos, StringComparison.OrdinalIgnoreCase);
    }

    private static List<TokenMatcherSpec> ParseMatcherSpecs(string? metadataJson, string pattern)
    {
        if (!string.IsNullOrWhiteSpace(metadataJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(metadataJson);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<TokenMatcherSpec>>(metadataJson, _jsonOptions) ?? new();
                }

                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("tokens", out var tokensProp))
                {
                    return JsonSerializer.Deserialize<List<TokenMatcherSpec>>(tokensProp.GetRawText(), _jsonOptions) ?? new();
                }
            }
            catch
            {
                // Fallback nếu JSON lỗi
            }
        }

        // Fallback đơn giản nếu không có JSON metadata: match bằng surface pattern
        var fallbackSurface = pattern.Replace("〜", "").Replace("~", "").Trim();
        if (!string.IsNullOrEmpty(fallbackSurface))
        {
            return new List<TokenMatcherSpec>
            {
                new TokenMatcherSpec { Surface = fallbackSurface }
            };
        }

        return new List<TokenMatcherSpec>();
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
