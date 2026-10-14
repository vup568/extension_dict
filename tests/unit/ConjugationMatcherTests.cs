using Application.Conjugation;
using Application.Conjugation.Models;
using FluentAssertions;
using Xunit;

namespace UnitTests;

public sealed class ConjugationMatcherTests
{
    private static readonly ConjugationToken[] Tokens =
    [new("verb", "regular", "食べ", "食べる", 0, 2), new("polite", "regular", "まし", "ます", 2, 4), new("past", "terminal", "た", "た", 4, 5)];
    private static readonly ConjugationRulePattern Rule = new("conjugation:test", 1, "Quá khứ của {base_form}",
        [new("verb", ["regular"], "Gốc"), new("polite", ["regular"], "Lịch sự"), new("past", ["terminal"], "Quá khứ")]);

    [Fact]
    public void FullChainReturnsDataExplanationAndObservedSegments()
    {
        var result = ConjugationMatcher.Match("食べました", Tokens, [Rule], "test-1");
        result.Explanation.Should().NotBeNull();
        result.Explanation!.BaseForm.Should().Be("食べる");
        result.Explanation.Explanation.Should().Be("Quá khứ của 食べる");
        result.Explanation.DatasetVersion.Should().Be("test-1");
        string.Concat(result.Explanation.Segments.Select(x => x.Surface)).Should().Be("食べました");
    }

    [Fact]
    public void GapsExtraTextUnknownCategoriesAndAmbiguityDoNotGuess()
    {
        ConjugationMatcher.Match("猫を食べました", Tokens, [Rule], "test").Explanation.Should().BeNull();
        ConjugationMatcher.Match("食べました", [Tokens[0], Tokens[1] with { Start = 3 }, Tokens[2]], [Rule], "test").Explanation.Should().BeNull();
        ConjugationMatcher.Match("食べました", [Tokens[0] with { Category = "unknown" }, Tokens[1], Tokens[2]], [Rule], "test").Explanation.Should().BeNull();
        ConjugationMatcher.Match("食べました", Tokens, [Rule, Rule with { RuleId = "another" }], "test").Explanation.Should().BeNull();
    }

    [Fact]
    public void MissingVietnameseIsUnavailableAndNewDataNeedsNoEngineChange()
    {
        var missing = ConjugationMatcher.Match("食べました", Tokens, [Rule with { ExplanationVi = null }], "test");
        missing.IsContentAvailable.Should().BeFalse();
        ConjugationMatcher.Match("食べました", Tokens, [Rule with { ExplanationVi = "Nội dung mới" }], "new-version")
            .Explanation!.Explanation.Should().Be("Nội dung mới");
    }
}
