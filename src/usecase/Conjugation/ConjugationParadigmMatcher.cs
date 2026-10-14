using Application.Conjugation.Models;

namespace Application.Conjugation;

/// <summary>Decodes reviewed full-word paradigms, never asserting a lemma without dictionary verification.</summary>
public static class ConjugationParadigmMatcher
{
    public static IReadOnlyList<ConjugationCandidate> Decode(string text,
        IReadOnlyList<ConjugationToken> tokens, IReadOnlyList<ConjugationParadigm> paradigms, string datasetVersion)
    {
        // EARS[Unwanted]: Partial chains, unknown metadata and malformed UTF-16 cannot establish a word.
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(datasetVersion) || tokens.Count == 0) return [];
        int position = 0;
        foreach (var token in tokens)
        {
            if (token.Category == "unknown" || token.Start != position || token.End <= token.Start ||
                !Boundary(text, token.Start) || !Boundary(text, token.End) ||
                text[token.Start..token.End] != token.Surface) return [];
            position = token.End;
        }
        if (position != text.Length) return [];

        var results = new List<ConjugationCandidate>();
        // Lexical specificity chooses an equivalent realization, never one interpretation over another.
        foreach (var p in paradigms.OrderByDescending(p => p.RequiredBase is not null))
        {
            var tail = string.Concat(p.Components.Select(c => c.SurfaceTemplate)).Replace("{stem}", "", StringComparison.Ordinal);
            if (!text.EndsWith(tail, StringComparison.Ordinal)) continue;
            var stemLength = text.Length - tail.Length;
            if (!Boundary(text, stemLength)) continue;
            var stem = text[..stemLength];
            var baseForm = stem + p.CanonicalSuffix;
            if (string.IsNullOrWhiteSpace(baseForm) || (p.RequiredBase is not null && p.RequiredBase != baseForm) ||
                p.ExcludedBases.Contains(baseForm, StringComparer.Ordinal)) continue;
            if (p.HeadCategories is { } categories && !categories.Contains(tokens[0].Category, StringComparer.Ordinal)) continue;
            if (p.HeadBaseTemplates is { } heads && !heads.Any(h =>
                h.Replace("{stem}", stem, StringComparison.Ordinal) == tokens[0].BaseForm)) continue;
            var segments = new List<ConjugationSegmentDto>();
            int offset = 0;
            foreach (var component in p.Components)
            {
                var surface = component.SurfaceTemplate.Replace("{stem}", stem, StringComparison.Ordinal);
                // An empty lexical stem cannot create a zero-length UI component.
                if (surface.Length == 0) { segments.Clear(); break; }
                var end = offset + surface.Length;
                if (!Boundary(text, end) || text[offset..end] != surface) { segments.Clear(); break; }
                segments.Add(new(surface, Expand(component.ExplanationVi, baseForm), new(offset, end)));
                offset = end;
            }
            if (segments.Count == 0 || offset != text.Length) continue;
            // EARS[Event]: Keep competing canonical classes until the dictionary has verified them.
            // Do not union POS: different classes can have different component boundaries/wording.
            if (results.Any(r => r.Detail.RuleId == p.RuleId && r.Detail.RuleVersion == p.RuleVersion && r.Detail.BaseForm == baseForm &&
                r.DictionaryPartOfSpeech.Order(StringComparer.Ordinal).SequenceEqual(p.DictionaryPartOfSpeech.Order(StringComparer.Ordinal)))) continue;
            results.Add(new(new(text, baseForm, Expand(p.ExplanationVi, baseForm), "vi", new(0, text.Length),
                p.RuleId, p.RuleVersion, datasetVersion, segments), p.DictionaryPartOfSpeech));
        }
        return results.OrderBy(c => c.Detail.RuleId, StringComparer.Ordinal).ThenBy(c => c.Detail.BaseForm, StringComparer.Ordinal).ToArray();
    }

    private static string Expand(string? value, string baseForm) =>
        value?.Replace("{base_form}", baseForm, StringComparison.Ordinal) ?? string.Empty;
    private static bool Boundary(string text, int index) => index >= 0 && index <= text.Length &&
        !(index > 0 && index < text.Length && char.IsHighSurrogate(text[index - 1]) && char.IsLowSurrogate(text[index]));
}
