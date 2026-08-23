using Application.Kanji;
using Application.Kanji.Helpers;
using Application.Kanji.Ports;
using Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace UnitTests;

public class LookupKanjiUseCaseTests
{
    private readonly IKanjiRepository _kanjiRepository = Substitute.For<IKanjiRepository>();
    private readonly LookupKanjiUseCase _sut;

    public LookupKanjiUseCaseTests()
    {
        _sut = new LookupKanjiUseCase(_kanjiRepository);
    }

    [Fact]
    public void KanjiExtractor_ShouldExtractStandardKanji_AndPreserveFirstOccurrenceOrder()
    {
        // Arrange
        var text = "日本語を勉強する";

        // Act
        var result = KanjiExtractor.ExtractUniqueKanji(text);

        // Assert
        result.Should().Equal("日", "本", "語", "勉", "強");
    }

    [Fact]
    public void KanjiExtractor_ShouldExtractSupplementarySurrogatePairKanji()
    {
        // Arrange
        // 𠮟 (U+20B9F) là CJK Extension B surrogate pair
        var text = "𠮟る";

        // Act
        var result = KanjiExtractor.ExtractUniqueKanji(text);

        // Assert
        result.Should().Equal("𠮟");
    }

    [Fact]
    public void KanjiExtractor_ShouldRemoveDuplicateKanji()
    {
        // Arrange
        var text = "食食食";

        // Act
        var result = KanjiExtractor.ExtractUniqueKanji(text);

        // Assert
        result.Should().Equal("食");
    }

    [Fact]
    public void KanjiExtractor_ShouldReturnEmptyList_WhenNoKanjiInInput()
    {
        // Arrange
        var text = "たべる ABC 123";

        // Act
        var result = KanjiExtractor.ExtractUniqueKanji(text);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_ShouldThrowArgumentException_WhenTextIsEmptyOrWhitespace(string? invalidText)
    {
        // Act
        var act = () => _sut.ExecuteAsync(invalidText!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Nội dung tra cứu không được trống*");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowArgumentException_WhenTextExceeds1000Chars()
    {
        // Arrange
        var longText = new string('あ', 1001);

        // Act
        var act = () => _sut.ExecuteAsync(longText, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*không được vượt quá 1000 ký tự*");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyMatches_WhenInputContainsNoKanji()
    {
        // Arrange
        var text = "たべる";

        // Act
        var result = await _sut.ExecuteAsync(text, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Matches.Should().BeEmpty();
        await _kanjiRepository.DidNotReceive().GetByCharactersAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnMappedKanjiDetailDtos_InFirstOccurrenceOrder()
    {
        // Arrange
        var text = "日本語";
        var kanjiNihon = CreateKanjiRecord("kanji:001", "日", "NHẬT", ["ニチ", "ジツ"], ["ひ", "か"], ["sun", "day"], ["ngày"], 9, 1, 5, "derived");
        var kanjiHon = CreateKanjiRecord("kanji:002", "本", "BỔN", ["ホン"], ["もと"], ["book", "main"], ["sách"], 5, 1, 5, "derived");
        var kanjiGo = CreateKanjiRecord("kanji:003", "語", "NGỮ", ["ゴ"], ["kataru"], ["language"], ["ngôn ngữ"], 14, 2, 4, "derived");

        // Giả lập DB trả về sai thứ tự (ví dụ Alphabet)
        _kanjiRepository.GetByCharactersAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns([kanjiGo, kanjiNihon, kanjiHon]);

        // Act
        var result = await _sut.ExecuteAsync(text, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Matches.Should().HaveCount(3);

        // Verify ordering matches input first-occurrence: 日 -> 本 -> 語
        result.Matches[0].Character.Should().Be("日");
        result.Matches[0].HanViet.Should().Be("NHẬT");

        result.Matches[1].Character.Should().Be("本");
        result.Matches[1].HanViet.Should().Be("BỔN");

        result.Matches[2].Character.Should().Be("語");
        result.Matches[2].HanViet.Should().Be("NGỮ");
    }

    private static KanjiRecord CreateKanjiRecord(
        string canonicalId,
        string character,
        string hanViet,
        string[] onyomi,
        string[] kunyomi,
        string[] meaningsEn,
        string[] meaningsVi,
        short strokeCount,
        short grade,
        short jlptLevel,
        string jlptProvenance)
    {
        return new KanjiRecord
        {
            Id = 1,
            CanonicalId = canonicalId,
            Character = character,
            HanViet = hanViet,
            OnReadings = onyomi,
            KunReadings = kunyomi,
            MeaningsEn = meaningsEn,
            MeaningsVi = meaningsVi,
            StrokeCount = strokeCount,
            Grade = grade,
            JlptLevel = jlptLevel,
            JlptProvenance = jlptProvenance,
            UnicodeCodepoint = $"U+{(int)character[0]:X4}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
