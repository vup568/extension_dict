using Application.Conjugation.Models;

namespace Application.Conjugation;

/// <summary>Interprets normalized knowledge; contains no provider-specific linguistic labels.</summary>
public static class ConjugationMatcher
{
    public static ConjugationMatchResult Match(
        string text, IReadOnlyList<ConjugationToken> tokens,
        IReadOnlyList<ConjugationRulePattern> rules, string datasetVersion)
    {
        // EARS[Ubiquitous]: Require an observed contiguous whole-word chain and a changed lexical head.
        if (string.IsNullOrWhiteSpace(text) || tokens.Count == 0 ||
            string.IsNullOrWhiteSpace(datasetVersion) ||
            string.IsNullOrWhiteSpace(tokens[0].BaseForm) ||
            tokens[0].BaseForm == tokens[0].Surface)
            return new(null);

        var position = 0;
        foreach (var token in tokens)
        {
            if (token.Start != position || token.End <= token.Start || token.End > text.Length ||
                text[token.Start..token.End] != token.Surface)
                return new(null);
            position = token.End;
        }
        if (position != text.Length)
            return new(null);

        // EARS[Unwanted]: A unique complete declarative pattern is required; never guess from suffixes.
        var matches = rules.Where(rule => rule.Tokens.Count == tokens.Count &&
            rule.Tokens.Select((pattern, i) =>
                pattern.Category == tokens[i].Category &&
                pattern.Inflections.Contains(tokens[i].Inflection, StringComparer.Ordinal)).All(x => x))
            .Take(2).ToArray();
        if (matches.Length != 1)
            return new(null);

        var match = matches[0];
        if (string.IsNullOrWhiteSpace(match.ExplanationVi) ||
            match.Tokens.Any(token => string.IsNullOrWhiteSpace(token.ExplanationVi)))
            return new(null, false);

        var segments = tokens.Select((token, i) => new ConjugationSegmentDto(
            token.Surface, match.Tokens[i].ExplanationVi!, new(token.Start, token.End))).ToArray();
        return new(new(text, tokens[0].BaseForm,
            match.ExplanationVi.Replace("{base_form}", tokens[0].BaseForm, StringComparison.Ordinal),
            "vi", new(0, text.Length), match.RuleId, match.RuleVersion, datasetVersion, segments));
    }
}
