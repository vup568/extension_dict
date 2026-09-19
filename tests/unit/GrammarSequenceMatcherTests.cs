namespace UnitTests;

using Application.Grammar.Helpers;
using Application.Grammar.Models;
using Domain.Entities;
using Xunit;

public class GrammarSequenceMatcherTests
{
    private readonly GrammarSequenceMatcher _matcher = new();

    [Fact]
    public void Match_StandardPattern_ReturnsOccurrence()
    {
        // Arrange
        var text = "雨が降っているから";
        var tokens = new List<NormalizedTokenDto>
        {
            new("雨", "雨", "名詞,普通名詞,一般,*,*,*", 0, 1),
            new("が", "が", "助詞,格助詞,一般,*,*,*", 1, 2),
            new("降っ", "降る", "動詞,一般,*,*,*,*", 2, 4),
            new("て", "て", "助詞,接続助詞,一般,*,*,*", 4, 5),
            new("いる", "いる", "動詞,非自立可能,*,*,*,*", 5, 7),
            new("から", "から", "助詞,接続助詞,一般,*,*,*", 7, 9),
        };

        var rules = new List<GrammarRule>
        {
            new()
            {
                CanonicalId = "grammar:001",
                Pattern = "〜ている",
                JlptLevel = "N5",
                MeaningVi = "Đang làm gì",
                MatcherMetadata = """
                [
                  { "surface": "て" },
                  { "base": "いる" }
                ]
                """
            }
        };

        // Act
        var result = _matcher.Match(tokens, rules, text);

        // Assert
        Assert.Single(result);
        var match = result[0];
        Assert.Equal("grammar:001", match.GrammarId);
        Assert.Equal("〜ている", match.Pattern);
        Assert.Equal("ている", match.MatchedText);
        Assert.Equal(4, match.Span.Start);
        Assert.Equal(7, match.Span.End);
    }

    [Fact]
    public void Match_WildcardPos_MatchesHierarchicalPos()
    {
        // Arrange
        var text = "行かないで";
        var tokens = new List<NormalizedTokenDto>
        {
            new("行か", "行く", "動詞,一般,*,*,*,*", 0, 2),
            new("ないで", "ないで", "助詞,接続助詞,*,*,*,*", 2, 5)
        };

        var rules = new List<GrammarRule>
        {
            new()
            {
                CanonicalId = "grammar:002",
                Pattern = "〜ないで",
                JlptLevel = "N5",
                MeaningVi = "Xin đừng...",
                MatcherMetadata = """
                [
                  { "pos": "助詞,接続助詞*" }
                ]
                """
            }
        };

        // Act
        var result = _matcher.Match(tokens, rules, text);

        // Assert
        Assert.Single(result);
        Assert.Equal("〜ないで", result[0].Pattern);
        Assert.Equal("ないで", result[0].MatchedText);
    }

    [Fact]
    public void Match_OverlappingPatterns_PreservesAllOccurrences()
    {
        // Arrange
        var text = "食べてはいけない";
        var tokens = new List<NormalizedTokenDto>
        {
            new("食べ", "食べる", "動詞,一般,*,*,*,*", 0, 2),
            new("て", "て", "助詞,接続助詞,一般,*,*,*", 2, 3),
            new("は", "は", "助詞,係助詞,*,*,*,*", 3, 4),
            new("いけない", "いけない", "形容詞,一般,*,*,*,*", 4, 8)
        };

        var rules = new List<GrammarRule>
        {
            new()
            {
                CanonicalId = "grammar:te",
                Pattern = "〜て",
                JlptLevel = "N5",
                MeaningVi = "Dạng nối て",
                MatcherMetadata = """[ { "surface": "て" } ]"""
            },
            new()
            {
                CanonicalId = "grammar:te-wa-ikenai",
                Pattern = "〜てはいけない",
                JlptLevel = "N4",
                MeaningVi = "Không được phép...",
                MatcherMetadata = """
                [
                  { "surface": "て" },
                  { "surface": "は" },
                  { "base": "いけない" }
                ]
                """
            }
        };

        // Act
        var result = _matcher.Match(tokens, rules, text);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.GrammarId == "grammar:te");
        Assert.Contains(result, r => r.GrammarId == "grammar:te-wa-ikenai");
    }
}
