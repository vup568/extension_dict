using Application.Dictionary;
using Application.Dictionary.Models;
using Application.Dictionary.Ports;
using Domain.Entities;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace UnitTests;

public class LookupWordUseCaseTests
{
    private readonly IDictionaryRepository _dictionaryRepository = Substitute.For<IDictionaryRepository>();
    private readonly ITokenizerAdapter _tokenizerAdapter = Substitute.For<ITokenizerAdapter>();
    private readonly LookupWordUseCase _sut;

    public LookupWordUseCaseTests()
    {
        _sut = new LookupWordUseCase(_dictionaryRepository, _tokenizerAdapter);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_ShouldThrowArgumentException_WhenQueryIsEmptyOrWhitespace(string? invalidQuery)
    {
        // Act
        var act = () => _sut.ExecuteAsync(invalidQuery!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Từ khóa tra cứu không được trống*");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowArgumentException_WhenQueryExceeds255Chars()
    {
        // Arrange
        var longQuery = new string('あ', 256);

        // Act
        var act = () => _sut.ExecuteAsync(longQuery, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*không được vượt quá 255 ký tự*");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnExactMatch_WhenExactEntryExists()
    {
        // Arrange
        var query = "日本語";
        var entry = CreateSampleEntry("dictionary:1001", "日本語", "にほんご", "Tiếng Nhật", "vi");

        _dictionaryRepository.SearchByFormOrReadingAsync(query, Arg.Any<CancellationToken>())
            .Returns(new List<DictionaryEntry> { entry });

        // Act
        var result = await _sut.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Matches.Should().HaveCount(1);
        result.Matches[0].CanonicalId.Should().Be("dictionary:1001");
        result.Matches[0].MatchedWrittenForm.Should().Be("日本語");
        result.Matches[0].Senses[0].Glosses[0].GlossText.Should().Be("Tiếng Nhật");
        result.Conjugation.Should().BeNull();
        result.Capabilities.Morphology.Should().Be("completed");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPerformDeinflection_WhenExactMatchEmptyAndSidecarSucceeds()
    {
        // Arrange
        var surfaceQuery = "食べました";
        var baseForm = "食べる";

        _dictionaryRepository.SearchByFormOrReadingAsync(surfaceQuery, Arg.Any<CancellationToken>())
            .Returns([]);

        _tokenizerAdapter.DeinflectAsync(surfaceQuery, Arg.Any<CancellationToken>())
            .Returns(new DeinflectionResult(true, surfaceQuery, baseForm, "Dạng lịch sự quá khứ (-ました)"));

        var baseEntry = CreateSampleEntry("dictionary:2002", "食べる", "たべる", "ăn", "vi");
        _dictionaryRepository.SearchByFormOrReadingAsync(baseForm, Arg.Any<CancellationToken>())
            .Returns([baseEntry]);

        // Act
        var result = await _sut.ExecuteAsync(surfaceQuery, TestContext.Current.CancellationToken);

        // Assert
        result.Matches.Should().HaveCount(1);
        result.Matches[0].CanonicalId.Should().Be("dictionary:2002");
        result.Conjugation.Should().NotBeNull();
        result.Conjugation!.SurfaceText.Should().Be("食べました");
        result.Conjugation.BaseForm.Should().Be("食べる");
        result.Conjugation.Explanation.Should().Be("Dạng lịch sự quá khứ (-ました)");
        result.Capabilities.Morphology.Should().Be("completed");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDegradeGracefully_WhenSidecarFails()
    {
        // Arrange
        var surfaceQuery = "食べました";

        _dictionaryRepository.SearchByFormOrReadingAsync(surfaceQuery, Arg.Any<CancellationToken>())
            .Returns([]);

        _tokenizerAdapter.DeinflectAsync(surfaceQuery, Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Sidecar offline"));

        // Act
        var result = await _sut.ExecuteAsync(surfaceQuery, TestContext.Current.CancellationToken);

        // Assert
        result.Matches.Should().BeEmpty();
        result.Conjugation.Should().BeNull();
        result.Capabilities.Morphology.Should().Be("unavailable");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPrioritizeVietnameseGlosses_OverEnglishGlosses()
    {
        // Arrange
        var query = "猫";
        var entry = new DictionaryEntry
        {
            Id = 1,
            CanonicalId = "dictionary:3003",
            WrittenForms = [new WrittenForm { Id = 10, Form = "猫", IsCommon = true }],
            Readings = [new Reading { Id = 20, ReadingText = "ねこ", IsCommon = true }],
            Senses = [
                new DictionarySense
                {
                    Id = 30,
                    SenseKey = "1",
                    Position = 1,
                    Glosses = [
                        new LocalizedGloss { Id = 101, LanguageTag = "en", GlossText = "cat", Position = 1 },
                        new LocalizedGloss { Id = 102, LanguageTag = "vi", GlossText = "con mèo", Position = 1 }
                    ]
                }
            ]
        };

        _dictionaryRepository.SearchByFormOrReadingAsync(query, Arg.Any<CancellationToken>())
            .Returns([entry]);

        // Act
        var result = await _sut.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.Matches[0].Senses[0].Glosses.Should().HaveCount(1);
        result.Matches[0].Senses[0].Glosses[0].LanguageTag.Should().Be("vi");
        result.Matches[0].Senses[0].Glosses[0].GlossText.Should().Be("con mèo");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFallbackToEnglishGloss_WhenVietnameseGlossIsMissing()
    {
        // Arrange
        var query = "猫";
        var entry = new DictionaryEntry
        {
            Id = 1,
            CanonicalId = "dictionary:3003",
            WrittenForms = [new WrittenForm { Id = 10, Form = "猫", IsCommon = true }],
            Readings = [new Reading { Id = 20, ReadingText = "ねこ", IsCommon = true }],
            Senses = [
                new DictionarySense
                {
                    Id = 30,
                    SenseKey = "1",
                    Position = 1,
                    Glosses = [
                        new LocalizedGloss { Id = 101, LanguageTag = "en", GlossText = "cat", Position = 1 }
                    ]
                }
            ]
        };

        _dictionaryRepository.SearchByFormOrReadingAsync(query, Arg.Any<CancellationToken>())
            .Returns([entry]);

        // Act
        var result = await _sut.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.Matches[0].Senses[0].Glosses.Should().HaveCount(1);
        result.Matches[0].Senses[0].Glosses[0].LanguageTag.Should().Be("en");
        result.Matches[0].Senses[0].Glosses[0].GlossText.Should().Be("cat");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPreserveSenseApplicabilities()
    {
        // Arrange
        var query = "表";
        var entry = new DictionaryEntry
        {
            Id = 1,
            CanonicalId = "dictionary:4004",
            WrittenForms = [new WrittenForm { Id = 11, Form = "表", IsCommon = true }],
            Readings = [new Reading { Id = 21, ReadingText = "おもて", IsCommon = true }],
            Senses = [
                new DictionarySense
                {
                    Id = 31,
                    SenseKey = "1",
                    Position = 1,
                    Glosses = [new LocalizedGloss { Id = 103, LanguageTag = "vi", GlossText = "bề mặt", Position = 1 }],
                    Applicabilities = [
                        new SenseApplicability { Id = 50, SenseId = 31, WrittenFormId = 11, ReadingId = 21 }
                    ]
                }
            ]
        };

        _dictionaryRepository.SearchByFormOrReadingAsync(query, Arg.Any<CancellationToken>())
            .Returns([entry]);

        // Act
        var result = await _sut.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // Assert
        var restrictions = result.Matches[0].Senses[0].Restrictions;
        restrictions.Should().HaveCount(1);
        restrictions[0].WrittenFormId.Should().Be(11);
        restrictions[0].ReadingId.Should().Be(21);
    }

    private static DictionaryEntry CreateSampleEntry(string canonicalId, string form, string readingText, string glossText, string lang)
    {
        return new DictionaryEntry
        {
            Id = 1,
            CanonicalId = canonicalId,
            WrittenForms = [new WrittenForm { Id = 10, Form = form, IsCommon = true }],
            Readings = [new Reading { Id = 20, ReadingText = readingText, IsCommon = true }],
            Senses = [
                new DictionarySense
                {
                    Id = 30,
                    SenseKey = "1",
                    Position = 1,
                    Glosses = [new LocalizedGloss { Id = 100, LanguageTag = lang, GlossText = glossText, Position = 1 }]
                }
            ]
        };
    }
}
