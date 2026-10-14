using Application.Conjugation;
using Application.Conjugation.Models;
using FluentAssertions;
using Xunit;

namespace UnitTests;

public sealed class ConjugationParadigmTests
{
    private static ConjugationParadigm Pattern(string id = "conjugation:test", string suffix = "る") =>
        new(id, 1, suffix, null, [], ["v1"], "Dạng đã kiểm duyệt của {base_form}.",
            [new("{stem}", "Phần của {base_form}."), new("ました", "Lịch sự, quá khứ.")]);
    private static ConjugationToken[] Tokens(string text) => [new("lexical", "unknown", text, text, 0, text.Length)];

    [Theory]
    [InlineData("食べました", "食べる")]
    [InlineData("見ました", "見る")]
    public void UsesGeneralStemPatternNotAnExampleLookup(string text, string root)
    {
        var result = ConjugationParadigmMatcher.Decode(text, Tokens(text), [Pattern()], "candidate");
        result.Should().ContainSingle(); result[0].Detail.BaseForm.Should().Be(root);
        result[0].Detail.Segments.Select(s => s.Surface).Should().Equal(text[..^3], "ました");
        result[0].Detail.Span.End.Should().Be(text.Length);
    }

    [Fact]
    public void RetainsDistinctMeaningsAndDoesNotRankOneAsCorrect()
    {
        var result = ConjugationParadigmMatcher.Decode("食べました", Tokens("食べました"),
            [Pattern("conjugation:b"), Pattern("conjugation:a"), Pattern("conjugation:a")], "candidate");
        result.Select(x => x.Detail.RuleId).Should().Equal("conjugation:a", "conjugation:b");
    }

    [Fact]
    public void SupportsUnchangedNounAndSupplementaryUtf16Stem()
    {
        var pattern = Pattern(suffix: "") with { DictionaryPartOfSpeech = ["n"],
            Components = [new("{stem}", "Danh từ."), new("です", "Copula lịch sự.")] };
        var result = ConjugationParadigmMatcher.Decode("𠮷です", Tokens("𠮷です"), [pattern], "candidate");
        result[0].Detail.BaseForm.Should().Be("𠮷");
        result[0].Detail.Segments[0].Span.End.Should().Be(2);
    }

    [Fact]
    public void KeepsSameIdentityWithDifferentCanonicalClassesUntilDictionaryVerification()
    {
        var ichidan = Pattern("conjugation:passive") with {
            Components = [new("{stem}", "Phần động từ."), new("られる", "Bị động.")] };
        var godan = ichidan with { DictionaryPartOfSpeech = ["v5r"],
            Components = [new("{stem}ら", "Phần động từ."), new("れる", "Bị động.")] };
        var results = ConjugationParadigmMatcher.Decode("帰られる", Tokens("帰られる"), [ichidan, godan], "candidate");
        results.Should().HaveCount(2);
        results.Select(r => r.DictionaryPartOfSpeech.Single()).Should().Equal("v1", "v5r");
    }

    [Fact]
    public void RequiresWholeContiguousObservedWordAndValidSurrogateBoundaries()
    {
        foreach (var ts in new[] {
            Array.Empty<ConjugationToken>(),
            new[] { new ConjugationToken("lexical", "x", "食べ", "食べる", 0, 2) },
            new[] { new ConjugationToken("unknown", "x", "食べました", "食べる", 0, 5) },
            new[] { new ConjugationToken("lexical", "x", "食べました", "食べる", 1, 6) } })
            ConjugationParadigmMatcher.Decode("食べました", ts, [Pattern()], "candidate").Should().BeEmpty();
        var half = new[] { new ConjugationToken("lexical", "x", "𠮷"[..1], "𠮷", 0, 1),
            new ConjugationToken("lexical", "x", "𠮷です"[1..], "𠮷", 1, 4) };
        ConjugationParadigmMatcher.Decode("𠮷です", half, [Pattern(suffix: "")], "candidate").Should().BeEmpty();
    }

    [Fact]
    public void RespectsExplicitLexicalExceptionsAndKeepsMissingContentInternal()
    {
        ConjugationParadigmMatcher.Decode("見ました", Tokens("見ました"),
            [Pattern() with { RequiredBase = "食べる" }, Pattern() with { ExcludedBases = ["見る"] }], "candidate").Should().BeEmpty();
        var result = ConjugationParadigmMatcher.Decode("見ました", Tokens("見ました"),
            [Pattern() with { ExplanationVi = null }], "candidate");
        result.Should().ContainSingle(); result[0].Detail.Explanation.Should().BeEmpty();
        result[0].DictionaryPartOfSpeech.Should().Equal("v1");
    }

    [Fact]
    public void RequiresObservedLexicalHeadEvidenceAndResolvesDerivedHeadsOnlyThroughData()
    {
        var text = "猫を食べました";
        var constrained = Pattern() with { HeadBaseTemplates = ["{stem}る"], HeadCategories = ["verb"] };
        ConjugationParadigmMatcher.Decode(text, [new("verb", "x", text, "猫", 0, text.Length)],
            [constrained], "candidate").Should().BeEmpty();
        var potential = Pattern("conjugation:potential", "く") with { DictionaryPartOfSpeech = ["v5k"],
            Components = [new("{stem}け", "Phần thân thể khả năng."), new("る", "Không quá khứ.")],
            HeadBaseTemplates = ["{stem}ける"], HeadCategories = ["verb"] };
        var result = ConjugationParadigmMatcher.Decode("歩ける", [new("verb", "x", "歩ける", "歩ける", 0, 3)], [potential], "candidate");
        result.Single().Detail.BaseForm.Should().Be("歩く");
    }
}
