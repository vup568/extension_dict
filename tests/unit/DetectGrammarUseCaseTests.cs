namespace UnitTests;

using Application.Grammar;
using Application.Grammar.Helpers;
using Application.Grammar.Models;
using Application.Grammar.Ports;
using Domain.Entities;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

public class DetectGrammarUseCaseTests
{
    private readonly IGrammarTokenizerPort _tokenizerPort = Substitute.For<IGrammarTokenizerPort>();
    private readonly IGrammarRepository _grammarRepository = Substitute.For<IGrammarRepository>();
    private readonly GrammarSequenceMatcher _matcher = new();
    private readonly DetectGrammarUseCase _useCase;

    public DetectGrammarUseCaseTests()
    {
        _useCase = new DetectGrammarUseCase(_tokenizerPort, _grammarRepository, _matcher);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_NullOrEmptyText_ThrowsArgumentException(string? text)
    {
        var request = new GrammarDetectionRequestDto(text!);
        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_TextExceeds2000Chars_ThrowsArgumentException()
    {
        var longText = new string('あ', 2001);
        var request = new GrammarDetectionRequestDto(longText);
        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_TokenizerThrowsException_ReturnsFailedStatus()
    {
        // Arrange
        var request = new GrammarDetectionRequestDto("雨が降っている");
        _tokenizerPort.TokenizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                      .ThrowsAsync(new InvalidOperationException("Sidecar unavailable"));

        // Act
        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("failed", result.Status);
        Assert.Empty(result.Occurrences);
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_ReturnsCompletedStatusWithOccurrences()
    {
        // Arrange
        var request = new GrammarDetectionRequestDto("雨が降っている");
        var tokens = new List<NormalizedTokenDto>
        {
            new("雨", "雨", "名詞,一般,*,*,*,*", 0, 1),
            new("が", "が", "助詞,格助詞,*,*,*,*", 1, 2),
            new("降っ", "降る", "動詞,一般,*,*,*,*", 2, 4),
            new("て", "て", "助詞,接続助詞,*,*,*,*", 4, 5),
            new("いる", "いる", "動詞,非自立可能,*,*,*,*", 5, 7)
        };

        var rules = new List<GrammarRule>
        {
            new()
            {
                CanonicalId = "grammar:001",
                Pattern = "〜ている",
                JlptLevel = "N5",
                MeaningVi = "Đang diễn ra",
                MatcherMetadata = """[ { "surface": "て" }, { "base": "いる" } ]"""
            }
        };

        _tokenizerPort.TokenizeAsync(request.Text, Arg.Any<CancellationToken>())
                      .Returns(tokens);
        _grammarRepository.GetAllActiveRulesAsync(Arg.Any<CancellationToken>())
                          .Returns(rules);

        // Act
        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("completed", result.Status);
        Assert.Single(result.Occurrences);
        Assert.Equal("grammar:001", result.Occurrences[0].GrammarId);
    }
}
