namespace UnitTests;

using Application.Analysis;
using Application.Analysis.Exceptions;
using Application.Analysis.Models;
using Application.Dictionary;
using Application.Dictionary.Models;
using Application.Dictionary.Ports;
using Application.Grammar;
using Application.Grammar.Helpers;
using Application.Grammar.Models;
using Application.Grammar.Ports;
using Application.Kanji;
using Application.Kanji.Models;
using Application.Kanji.Ports;
using Domain.Entities;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

/// <summary>
/// Unit Tests cho AnalyzeTextUseCase (F-05 Unified Analysis).
/// Mock tại tầng port/repository (giống pattern DetectGrammarUseCaseTests),
/// tạo real use cases và inject vào orchestrator.
/// </summary>
public class AnalyzeTextUseCaseTests
{
    // Vocabulary mocks
    private readonly IDictionaryRepository _dictionaryRepo = Substitute.For<IDictionaryRepository>();
    private readonly ITokenizerAdapter _tokenizerAdapter = Substitute.For<ITokenizerAdapter>();

    // Kanji mocks
    private readonly IKanjiRepository _kanjiRepo = Substitute.For<IKanjiRepository>();

    // Grammar mocks
    private readonly IGrammarTokenizerPort _grammarTokenizer = Substitute.For<IGrammarTokenizerPort>();
    private readonly IGrammarRepository _grammarRepo = Substitute.For<IGrammarRepository>();
    private readonly GrammarSequenceMatcher _matcher = new();

    // SUT
    private readonly AnalyzeTextUseCase _useCase;

    public AnalyzeTextUseCaseTests()
    {
        var lookupWord = new LookupWordUseCase(_dictionaryRepo, _tokenizerAdapter);
        var lookupKanji = new LookupKanjiUseCase(_kanjiRepo);
        var detectGrammar = new DetectGrammarUseCase(_grammarTokenizer, _grammarRepo, _matcher);

        _useCase = new AnalyzeTextUseCase(lookupWord, lookupKanji, detectGrammar);
    }

    // ============================================================
    // Input Validation Tests [AC-008, AC-009, AC-010, AC-004]
    // ============================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_NullOrEmptyText_ThrowsArgumentException(string? text)
    {
        // AC-008
        var request = new AnalysisRequestDto(text!);
        await Assert.ThrowsAsync<ArgumentException>(
            () => _useCase.ExecuteAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_TextExceeds1000Chars_ThrowsArgumentException()
    {
        // AC-009
        var longText = new string('あ', 1001);
        var request = new AnalysisRequestDto(longText);
        await Assert.ThrowsAsync<ArgumentException>(
            () => _useCase.ExecuteAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_TextExactly1000Chars_DoesNotThrow()
    {
        // Edge case: exactly at limit should pass validation
        var exactText = new string('あ', 1000);
        var request = new AnalysisRequestDto(exactText);

        // All use cases return empty results (mock default behavior)
        SetupEmptyDefaults();

        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_InvalidInteractionId_ThrowsArgumentException()
    {
        // AC-010
        var request = new AnalysisRequestDto("食べた", InteractionId: "not-a-uuid");
        await Assert.ThrowsAsync<ArgumentException>(
            () => _useCase.ExecuteAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_NullInteractionId_NoError()
    {
        // AC-004
        var request = new AnalysisRequestDto("食べた", InteractionId: null);
        SetupEmptyDefaults();

        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ValidInteractionId_Accepted()
    {
        // AC-003 (validation side — echo is endpoint responsibility)
        var validUuid = Guid.NewGuid().ToString();
        var request = new AnalysisRequestDto("食べた", InteractionId: validUuid);
        SetupEmptyDefaults();

        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);
        result.Should().NotBeNull();
    }

    // ============================================================
    // Happy Path Tests [AC-001, AC-006a]
    // ============================================================

    [Fact]
    public async Task ExecuteAsync_HappyPath_AllCapabilitiesCompleted()
    {
        // AC-001
        var request = new AnalysisRequestDto("食べた");

        // Vocabulary: return 1 match
        _dictionaryRepo.SearchByFormOrReadingAsync("食べた", Arg.Any<CancellationToken>())
            .Returns(new List<DictionaryEntry>
            {
                CreateDummyDictionaryEntry("dictionary:12345", "食べる")
            });

        // Kanji: return 1 match
        _kanjiRepo.GetByCharactersAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<KanjiRecord>
            {
                new() { CanonicalId = "kanji:98765", Character = "食", UnicodeCodepoint = "U+98DF" }
            });

        // Grammar: tokenizer returns tokens, no rules match
        _grammarTokenizer.TokenizeAsync("食べた", Arg.Any<CancellationToken>())
            .Returns(new List<NormalizedTokenDto>
            {
                new("食べ", "食べる", "動詞,一般,*,*,*,*", 0, 2),
                new("た", "た", "助動詞,*,*,*,*,*", 2, 3)
            });
        _grammarRepo.GetAllActiveRulesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<GrammarRule>());

        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert capabilities
        result.Capabilities.Vocabulary.Should().Be(CapabilityStatus.Completed);
        result.Capabilities.Kanji.Should().Be(CapabilityStatus.Completed);
        result.Capabilities.Grammar.Should().Be(CapabilityStatus.Completed);
        result.Capabilities.Morphology.Should().Be(CapabilityStatus.Unavailable); // AC-006
        result.Capabilities.Conjugation.Should().Be(CapabilityStatus.Unavailable); // AC-006

        // Assert data sections
        result.DictionaryMatches.Should().HaveCount(1);
        result.Kanji.Should().HaveCount(1);
        result.GrammarOccurrences.Should().BeEmpty(); // No rules matched
        result.Tokens.Should().BeEmpty(); // Future: morphology
        result.Conjugations.Should().BeEmpty(); // Future: F-09
        result.NormalizedText.Should().Be("食べた");
    }

    // ============================================================
    // Partial Failure Tests [AC-005, AC-006b]
    // ============================================================

    [Fact]
    public async Task ExecuteAsync_VocabOk_KanjiFailed_PartialResult()
    {
        // AC-005
        var request = new AnalysisRequestDto("食べた");

        // Vocabulary OK
        _dictionaryRepo.SearchByFormOrReadingAsync("食べた", Arg.Any<CancellationToken>())
            .Returns(new List<DictionaryEntry>
            {
                CreateDummyDictionaryEntry("dictionary:12345", "食べる")
            });

        // Kanji THROWS
        _kanjiRepo.GetByCharactersAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Grammar OK (empty)
        SetupGrammarEmpty();

        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        result.Capabilities.Vocabulary.Should().Be(CapabilityStatus.Completed);
        result.Capabilities.Kanji.Should().Be(CapabilityStatus.Failed);
        result.Capabilities.Grammar.Should().Be(CapabilityStatus.Completed);
        result.DictionaryMatches.Should().HaveCount(1);
        result.Kanji.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_GrammarSidecarDown_GrammarFailed_OthersOk()
    {
        // AC-006b: Grammar thất bại (Tokenizer Sidecar không khả dụng)
        var request = new AnalysisRequestDto("食べた");

        // Vocabulary OK
        _dictionaryRepo.SearchByFormOrReadingAsync("食べた", Arg.Any<CancellationToken>())
            .Returns(new List<DictionaryEntry>());

        // Kanji OK
        _kanjiRepo.GetByCharactersAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<KanjiRecord>());

        // Grammar: Tokenizer sidecar throws → DetectGrammarUseCase returns "failed" internally
        _grammarTokenizer.TokenizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Sidecar unavailable"));

        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        result.Capabilities.Grammar.Should().Be(CapabilityStatus.Failed);
        result.Capabilities.Vocabulary.Should().Be(CapabilityStatus.Completed);
        result.Capabilities.Kanji.Should().Be(CapabilityStatus.Completed);
        result.GrammarOccurrences.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_VocabFailsDueToLongText_OthersOk()
    {
        // Vocabulary has 255 char limit — text > 255 chars causes vocab to throw ArgumentException
        var longText = new string('あ', 300); // > 255, < 1000
        var request = new AnalysisRequestDto(longText);

        // Kanji OK (1000 char limit)
        _kanjiRepo.GetByCharactersAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<KanjiRecord>());

        // Grammar OK (2000 char limit)
        _grammarTokenizer.TokenizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<NormalizedTokenDto>());
        _grammarRepo.GetAllActiveRulesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<GrammarRule>());

        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        // Vocabulary should be "failed" because text > 255 chars
        result.Capabilities.Vocabulary.Should().Be(CapabilityStatus.Failed);
        result.Capabilities.Kanji.Should().Be(CapabilityStatus.Completed);
        result.Capabilities.Grammar.Should().Be(CapabilityStatus.Completed);
        result.DictionaryMatches.Should().BeEmpty();
    }

    // ============================================================
    // Static Capability Tests [AC-006]
    // ============================================================

    [Fact]
    public async Task ExecuteAsync_MorphologyAlwaysUnavailable()
    {
        // AC-006
        var request = new AnalysisRequestDto("食べた");
        SetupEmptyDefaults();

        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        result.Capabilities.Morphology.Should().Be(CapabilityStatus.Unavailable);
        result.Tokens.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ConjugationAlwaysUnavailable()
    {
        // AC-006
        var request = new AnalysisRequestDto("食べた");
        SetupEmptyDefaults();

        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        result.Capabilities.Conjugation.Should().Be(CapabilityStatus.Unavailable);
        result.Conjugations.Should().BeEmpty();
    }

    // ============================================================
    // All-Failed Test [AC-007]
    // ============================================================

    [Fact]
    public async Task ExecuteAsync_AllCapabilitiesFailed_ThrowsAllCapabilitiesFailedException()
    {
        // AC-007
        var request = new AnalysisRequestDto("食べた");

        // Vocabulary THROWS
        _dictionaryRepo.SearchByFormOrReadingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB down"));

        // Kanji THROWS
        _kanjiRepo.GetByCharactersAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB down"));

        // Grammar: tokenizer THROWS → use case returns "failed"
        _grammarTokenizer.TokenizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Sidecar down"));

        await Assert.ThrowsAsync<AllCapabilitiesFailedException>(
            () => _useCase.ExecuteAsync(request, CancellationToken.None));
    }

    // ============================================================
    // NormalizedText Tests
    // ============================================================

    [Fact]
    public async Task ExecuteAsync_TrimmedText_UsedAsNormalizedText()
    {
        var request = new AnalysisRequestDto("  食べた  ");
        SetupEmptyDefaults();

        var result = await _useCase.ExecuteAsync(request, CancellationToken.None);

        result.NormalizedText.Should().Be("食べた");
    }

    // ============================================================
    // Helpers
    // ============================================================

    private void SetupEmptyDefaults()
    {
        _dictionaryRepo.SearchByFormOrReadingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<DictionaryEntry>());
        _kanjiRepo.GetByCharactersAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<KanjiRecord>());
        SetupGrammarEmpty();
    }

    private void SetupGrammarEmpty()
    {
        _grammarTokenizer.TokenizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<NormalizedTokenDto>());
        _grammarRepo.GetAllActiveRulesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<GrammarRule>());
    }

    private static DictionaryEntry CreateDummyDictionaryEntry(string canonicalId, string form)
    {
        return new DictionaryEntry
        {
            CanonicalId = canonicalId,
            WrittenForms = new List<WrittenForm>
            {
                new()
                {
                    Form = form,
                    IsCommon = true,
                    Priority = 1
                }
            },
            Readings = new List<Reading>(),
            Senses = new List<DictionarySense>()
        };
    }
}
