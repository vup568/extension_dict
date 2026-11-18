using Application.Conjugation.Models;
using Application.Dictionary;
using Application.Dictionary.Models;
using Application.Dictionary.Ports;
using Application.Grammar.Models;
using Application.Grammar.Ports;
using Domain.Entities;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace UnitTests;

public sealed class SentenceVocabularyUseCaseTests
{
    private readonly IGrammarTokenizerPort tokenizer = Substitute.For<IGrammarTokenizerPort>();
    private readonly IDictionaryRepository dictionary = Substitute.For<IDictionaryRepository>();
    private readonly ITokenizerAdapter deinflector = Substitute.For<ITokenizerAdapter>();
    private readonly Dictionary<string, IReadOnlyList<DictionaryEntry>> entries = new(StringComparer.Ordinal);
    private readonly SentenceVocabularyUseCase sut;

    public SentenceVocabularyUseCaseTests()
    {
        dictionary.SearchByFormOrReadingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => entries.GetValueOrDefault(call.ArgAt<string>(0), []));
        deinflector.IsAvailable.Returns(true);
        deinflector.DeinflectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => DeinflectionResult.Empty(call.ArgAt<string>(0)));
        sut = new(tokenizer, new LookupWordUseCase(dictionary, deinflector));
    }

    [Fact]
    public async Task SentenceRecognizesSuruNounWithoutInventingSuruDictionaryEntry()
    {
        const string text = "私は日本語を勉強しています。";
        tokenizer.TokenizeAsync(text, Arg.Any<CancellationToken>()).Returns([
            T("私", "私", "代名詞,*,*", 0, 1), T("は", "は", "助詞,係助詞,*", 1, 2),
            T("日本語", "日本語", "名詞,普通名詞,一般", 2, 5), T("を", "を", "助詞,格助詞,*", 5, 6),
            T("勉強", "勉強", "名詞,普通名詞,サ変可能", 6, 8), T("し", "する", "動詞,非自立可能,*", 8, 9),
            T("て", "て", "助詞,接続助詞,*", 9, 10), T("い", "いる", "動詞,非自立可能,*", 10, 11),
            T("ます", "ます", "助動詞,*,*", 11, 13), T("。", "。", "補助記号,句点,*", 13, 14)
        ]);
        entries["私"] = [Entry("private", "私", "pron")];
        entries["は"] = [Entry("unrelated-noun", "は", "n")];
        entries["日本語"] = [Entry("japanese", "日本語", "n")];
        entries["勉強"] = [Entry("study", "勉強", "vs", "n")];

        var result = await sut.ExecuteAsync(text, TestContext.Current.CancellationToken);
        result.Status.Should().Be("completed");
        result.Truncated.Should().BeFalse();
        result.Occurrences.Select(o => o.Surface).Should().Equal("私", "日本語", "勉強しています");
        var study = result.Occurrences[2];
        (study.Span.Start, study.Span.End, study.Span.Unit).Should().Be((6, 13, "utf16"));
        study.Matches.Should().ContainSingle().Which.CanonicalId.Should().Be("study");
        study.Conjugations.Should().BeEmpty();
        await dictionary.DidNotReceive().SearchByFormOrReadingAsync("勉強する", Arg.Any<CancellationToken>());
        await dictionary.DidNotReceive().SearchByFormOrReadingAsync("は", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdjacentPrefixAndNounCombineOnlyWhenTheFullFormHasANounSense()
    {
        const string text = "お茶を飲む";
        tokenizer.TokenizeAsync(text, Arg.Any<CancellationToken>()).Returns([
            T("お", "お", "接頭辞,*,*", 0, 1), T("茶", "茶", "名詞,普通名詞,一般", 1, 2),
            T("を", "を", "助詞,格助詞,*", 2, 3), T("飲む", "飲む", "動詞,一般,*", 3, 5)
        ]);
        entries["お茶"] = [Entry("tea", "お茶", "n"), Entry("unrelated-adverb", "お茶", "adv")];
        entries["お"] = [Entry("prefix", "お", "pref")];
        entries["茶"] = [Entry("tea-without-prefix", "茶", "n")];
        entries["飲む"] = [Entry("drink", "飲む", "v5m")];

        var result = await sut.ExecuteAsync(text, TestContext.Current.CancellationToken);

        result.Occurrences.Select(o => o.Surface).Should().Equal("お茶", "飲む");
        var tea = result.Occurrences[0];
        (tea.Span.Start, tea.Span.End, tea.Span.Unit).Should().Be((0, 2, "utf16"));
        tea.Matches.Should().ContainSingle().Which.CanonicalId.Should().Be("tea");
        await dictionary.DidNotReceive().SearchByFormOrReadingAsync("お", Arg.Any<CancellationToken>());
        await dictionary.DidNotReceive().SearchByFormOrReadingAsync("茶", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PrefixNounProjectionExcludesReadingsBelongingOnlyToAnotherSense()
    {
        const string text = "お茶を";
        tokenizer.TokenizeAsync(text, Arg.Any<CancellationToken>()).Returns([
            T("お", "お", "接頭辞,*,*", 0, 1), T("茶", "茶", "名詞,普通名詞,一般", 1, 2),
            T("を", "を", "助詞,格助詞,*", 2, 3)
        ]);
        entries["お茶"] = [EntryWithRestrictedSenses("tea", "お茶", "n", "adv")];

        var result = await sut.ExecuteAsync(text, TestContext.Current.CancellationToken);

        var match = result.Occurrences.Should().ContainSingle().Subject.Matches.Should().ContainSingle().Subject;
        match.Senses.Should().ContainSingle().Which.SenseKey.Should().Be("kept");
        match.Readings.Should().ContainSingle().Which.Id.Should().Be(21);
        match.Senses.Single().Restrictions.Should().ContainSingle().Which.ReadingId.Should().Be(21);
    }

    [Fact]
    public async Task PrefixNounProjectionExcludesWrittenFormsBelongingOnlyToAnotherSense()
    {
        const string text = "おちゃを";
        tokenizer.TokenizeAsync(text, Arg.Any<CancellationToken>()).Returns([
            T("お", "お", "接頭辞,*,*", 0, 1), T("ちゃ", "ちゃ", "名詞,普通名詞,一般", 1, 3),
            T("を", "を", "助詞,格助詞,*", 3, 4)
        ]);
        entries["おちゃ"] = [new DictionaryEntry
        {
            CanonicalId = "dictionary:synthetic-kana",
            WrittenForms = [new WrittenForm { Id = 11, Form = "お茶" }, new WrittenForm { Id = 12, Form = "御茶" }],
            Readings = [new Reading { Id = 21, ReadingText = "おちゃ" }],
            Senses = [RestrictedSense("kept", "n", 11, 21), RestrictedSense("excluded", "adv", 12, 21)]
        }];

        var result = await sut.ExecuteAsync(text, TestContext.Current.CancellationToken);

        var match = result.Occurrences.Should().ContainSingle().Subject.Matches.Should().ContainSingle().Subject;
        match.MatchedReading.Should().Be("おちゃ");
        match.WrittenForms.Should().ContainSingle().Which.Id.Should().Be(11);
        match.Senses.Should().ContainSingle().Which.SenseKey.Should().Be("kept");
    }

    [Fact]
    public async Task SuruProjectionExcludesReadingsBelongingOnlyToAnotherSense()
    {
        const string text = "勉強した";
        tokenizer.TokenizeAsync(text, Arg.Any<CancellationToken>()).Returns([
            T("勉強", "勉強", "名詞,普通名詞,サ変可能", 0, 2),
            T("し", "する", "動詞,非自立可能,*", 2, 3), T("た", "た", "助動詞,*,*", 3, 4)
        ]);
        entries["勉強"] = [EntryWithRestrictedSenses("study", "勉強", "vs", "n")];

        var result = await sut.ExecuteAsync(text, TestContext.Current.CancellationToken);

        var match = result.Occurrences.Should().ContainSingle().Subject.Matches.Should().ContainSingle().Subject;
        match.Senses.Should().ContainSingle().Which.SenseKey.Should().Be("kept");
        match.Readings.Should().ContainSingle().Which.Id.Should().Be(21);
    }

    [Fact]
    public async Task FilteredInflectionAndUnfilteredSameFormUseSeparateCachedProjections()
    {
        const string text = "食べました、食べる、食べました";
        tokenizer.TokenizeAsync(text, Arg.Any<CancellationToken>()).Returns([
            T("食べ", "食べる", "動詞,一般,*", 0, 2), T("まし", "ます", "助動詞,*,*", 2, 4),
            T("た", "た", "助動詞,*,*", 4, 5), T("、", "、", "補助記号,読点,*", 5, 6),
            T("食べる", "食べる", "動詞,一般,*", 6, 9), T("、", "、", "補助記号,読点,*", 9, 10),
            T("食べ", "食べる", "動詞,一般,*", 10, 12), T("まし", "ます", "助動詞,*,*", 12, 14),
            T("た", "た", "助動詞,*,*", 14, 15)
        ]);
        entries["食べる"] = [EntryWithRestrictedSenses("eat", "食べる", "v1", "n")];

        var result = await sut.ExecuteAsync(text, TestContext.Current.CancellationToken);

        result.Occurrences.Select(o => o.Surface).Should().Equal("食べました", "食べる", "食べました");
        foreach (var occurrence in new[] { result.Occurrences[0], result.Occurrences[2] })
        {
            var match = occurrence.Matches.Should().ContainSingle().Subject;
            match.Senses.Should().ContainSingle().Which.SenseKey.Should().Be("kept");
            match.Readings.Should().ContainSingle().Which.Id.Should().Be(21);
        }
        var plain = result.Occurrences[1].Matches.Should().ContainSingle().Subject;
        plain.Senses.Select(s => s.SenseKey).Should().Equal("kept", "excluded");
        plain.Readings.Select(r => r.Id).Should().BeEquivalentTo(new long[] { 21, 22 });
        await dictionary.Received(2).SearchByFormOrReadingAsync("食べる", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnverifiedOrIncompatibleCompoundFallsBackToIndividualTokens(bool unrelatedExactEntry)
    {
        const string text = "お猫";
        tokenizer.TokenizeAsync(text, Arg.Any<CancellationToken>()).Returns([
            T("お", "お", "接頭辞,*,*", 0, 1), T("猫", "猫", "名詞,普通名詞,一般", 1, 2)
        ]);
        entries["お"] = [Entry("prefix", "お", "pref")];
        entries["猫"] = [Entry("cat", "猫", "n")];
        if (unrelatedExactEntry) entries["お猫"] = [Entry("unrelated-adverb", "お猫", "adv")];

        var result = await sut.ExecuteAsync(text, TestContext.Current.CancellationToken);

        result.Occurrences.Select(o => o.Surface).Should().Equal("お", "猫");
        result.Occurrences.SelectMany(o => o.Matches).Select(m => m.CanonicalId).Should().Equal("prefix", "cat");
    }

    [Fact]
    public async Task PrefixDoesNotCombineAcrossWhitespaceOrPunctuation()
    {
        const string text = "お 茶、お、茶、お茶";
        tokenizer.TokenizeAsync(text, Arg.Any<CancellationToken>()).Returns([
            T("お", "お", "接頭辞,*,*", 0, 1), T(" ", " ", "空白,*,*", 1, 2),
            T("茶", "茶", "名詞,普通名詞,一般", 2, 3), T("、", "、", "補助記号,読点,*", 3, 4),
            T("お", "お", "接頭辞,*,*", 4, 5), T("、", "、", "補助記号,読点,*", 5, 6),
            T("茶", "茶", "名詞,普通名詞,一般", 6, 7), T("、", "、", "補助記号,読点,*", 7, 8),
            T("お", "お", "接頭辞,*,*", 8, 9), T("茶", "茶", "名詞,普通名詞,一般", 9, 10)
        ]);
        entries["お"] = [Entry("prefix", "お", "pref")];
        entries["茶"] = [Entry("tea-without-prefix", "茶", "n")];
        entries["お茶"] = [Entry("tea", "お茶", "n")];

        var result = await sut.ExecuteAsync(text, TestContext.Current.CancellationToken);

        result.Occurrences.Select(o => (o.Surface, o.Span.Start, o.Span.End))
            .Should().Equal(("お", 0, 1), ("茶", 2, 3), ("お", 4, 5), ("茶", 6, 7), ("お茶", 8, 10));
        await dictionary.Received(1).SearchByFormOrReadingAsync("お茶", Arg.Any<CancellationToken>());
        await dictionary.DidNotReceive().SearchByFormOrReadingAsync("お 茶", Arg.Any<CancellationToken>());
        await dictionary.DidNotReceive().SearchByFormOrReadingAsync("お、茶", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PrefixDoesNotConsumeNounThatStartsSuruChain()
    {
        const string text = "お勉強した";
        tokenizer.TokenizeAsync(text, Arg.Any<CancellationToken>()).Returns([
            T("お", "お", "接頭辞,*,*", 0, 1), T("勉強", "勉強", "名詞,普通名詞,サ変可能", 1, 3),
            T("し", "する", "動詞,非自立可能,*", 3, 4), T("た", "た", "助動詞,*,*", 4, 5)
        ]);
        entries["お勉強"] = [Entry("honorific-study", "お勉強", "n", "vs")];
        entries["勉強"] = [Entry("study", "勉強", "vs")];

        var result = await sut.ExecuteAsync(text, TestContext.Current.CancellationToken);

        var occurrence = result.Occurrences.Should().ContainSingle().Subject;
        occurrence.Surface.Should().Be("勉強した");
        occurrence.Matches.Should().ContainSingle().Which.CanonicalId.Should().Be("study");
        await dictionary.DidNotReceive().SearchByFormOrReadingAsync("お勉強", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RepeatedInflectionsPreserveBothUtf16OccurrencesAndRelativeExplanations()
    {
        const string text = "😀食べました 食べました";
        tokenizer.TokenizeAsync(text, Arg.Any<CancellationToken>()).Returns([
            T("😀", "😀", "補助記号,一般,*", 0, 2),
            T("食べ", "食べる", "動詞,一般,*", 2, 4), T("まし", "ます", "助動詞,*,*", 4, 6),
            T("た", "た", "助動詞,*,*", 6, 7), T(" ", " ", "空白,*,*", 7, 8),
            T("食べ", "食べる", "動詞,一般,*", 8, 10), T("まし", "ます", "助動詞,*,*", 10, 12),
            T("た", "た", "助動詞,*,*", 12, 13)
        ]);
        entries["食べる"] = [Entry("eat", "食べる", "v1")];
        var detail = new ConjugationExplanationDto("食べました", "食べる", "Lịch sự quá khứ", "vi", new(0, 5),
            "polite-past", 1, "fixture", [new("食べ", "gốc", new(0, 2)), new("まし", "lịch sự", new(2, 4)), new("た", "quá khứ", new(4, 5))]);
        deinflector.DeinflectAsync("食べました", Arg.Any<CancellationToken>())
            .Returns(new DeinflectionResult(true, "食べました", "食べる", "Lịch sự quá khứ", Detail: detail, ConjugationStatus: "completed"));

        var result = await sut.ExecuteAsync(text, TestContext.Current.CancellationToken);
        result.Occurrences.Should().HaveCount(2);
        result.Occurrences.Select(o => (o.Span.Start, o.Span.End)).Should().Equal((2, 7), (8, 13));
        result.Occurrences.Should().OnlyContain(o => o.Surface == "食べました" && o.Matches.Single().CanonicalId == "eat");
        result.Occurrences.Should().OnlyContain(o => o.Conjugations.Single().Span.Start == 0 && o.Conjugations.Single().Span.End == 5);
        await deinflector.Received(1).DeinflectAsync("食べました", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnrecognizedInflectionCanFindVerifiedBaseButDoesNotInventExplanation()
    {
        const string text = "食べなかった";
        tokenizer.TokenizeAsync(text, Arg.Any<CancellationToken>()).Returns([
            T("食べ", "食べる", "動詞,一般,*", 0, 2), T("なかっ", "ない", "助動詞,*,*", 2, 5),
            T("た", "た", "助動詞,*,*", 5, 6)
        ]);
        entries["食べる"] = [Entry("noun-same-spelling", "食べる", "n"), Entry("verb", "食べる", "v1")];
        var result = await sut.ExecuteAsync(text, TestContext.Current.CancellationToken);
        var occurrence = result.Occurrences.Should().ContainSingle().Subject;
        occurrence.Surface.Should().Be(text);
        occurrence.Matches.Should().ContainSingle().Which.CanonicalId.Should().Be("verb");
        occurrence.Conjugations.Should().BeEmpty();
    }

    [Fact]
    public async Task InflectedHeadDoesNotPreferAnUnrelatedNounWithTheSameSpelling()
    {
        const string text = "書きながら";
        tokenizer.TokenizeAsync(text, Arg.Any<CancellationToken>()).Returns([
            T("書き", "書く", "動詞,一般,*", 0, 2), T("ながら", "ながら", "助詞,接続助詞,*", 2, 5)
        ]);
        entries["書き"] = [Entry("unrelated-noun", "書き", "n")];
        entries["書く"] = [Entry("write", "書く", "v5k")];
        var result = await sut.ExecuteAsync(text, TestContext.Current.CancellationToken);
        var occurrence = result.Occurrences.Should().ContainSingle().Subject;
        occurrence.Surface.Should().Be("書き");
        occurrence.Matches.Should().ContainSingle().Which.CanonicalId.Should().Be("write");
        occurrence.Conjugations.Should().BeEmpty();
    }

    [Fact]
    public async Task InvalidTokenOffsetsFailWithoutQueryingDictionary()
    {
        const string text = "😀猫";
        tokenizer.TokenizeAsync(text, Arg.Any<CancellationToken>()).Returns([T("😀", "😀", "補助記号", 0, 1), T("猫", "猫", "名詞", 1, 3)]);
        var result = await sut.ExecuteAsync(text, TestContext.Current.CancellationToken);
        result.Status.Should().Be("failed");
        result.Occurrences.Should().BeEmpty();
        await dictionary.DidNotReceive().SearchByFormOrReadingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProviderFailureAndCallerCancellationHaveDifferentResults()
    {
        tokenizer.TokenizeAsync("猫", Arg.Any<CancellationToken>()).Throws(new InvalidOperationException());
        (await sut.ExecuteAsync("猫", TestContext.Current.CancellationToken)).Status.Should().Be("unavailable");
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.ExecuteAsync("猫", cancelled.Token));
    }

    [Fact]
    public async Task LookupLimitMarksResultsAsTruncated()
    {
        var text = string.Concat(Enumerable.Range(0, SentenceVocabularyUseCase.MaxLookupAttempts + 1).Select(i => (char)(0x4e00 + i)));
        tokenizer.TokenizeAsync(text, Arg.Any<CancellationToken>()).Returns(
            Enumerable.Range(0, text.Length).Select(i => T(text[i].ToString(), text[i].ToString(), "名詞,普通名詞", i, i + 1)).ToArray());
        entries[text[0].ToString()] = [Entry("first", text[0].ToString(), "n")];
        var result = await sut.ExecuteAsync(text, TestContext.Current.CancellationToken);
        result.Status.Should().Be("completed");
        result.Truncated.Should().BeTrue();
        result.Occurrences.Should().ContainSingle().Which.Matches.Single().CanonicalId.Should().Be("first");
        await dictionary.Received(SentenceVocabularyUseCase.MaxLookupAttempts).SearchByFormOrReadingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static NormalizedTokenDto T(string surface, string baseForm, string pos, int start, int end) =>
        new(surface, baseForm, pos, start, end);

    private static DictionaryEntry EntryWithRestrictedSenses(string canonicalId, string form, string keptPos, string excludedPos) => new()
    {
        CanonicalId = canonicalId,
        WrittenForms = [new WrittenForm { Id = 11, Form = form }],
        Readings = [new Reading { Id = 21, ReadingText = "よみいち" }, new Reading { Id = 22, ReadingText = "よみに" }],
        Senses = [RestrictedSense("kept", keptPos, 11, 21), RestrictedSense("excluded", excludedPos, 11, 22)]
    };

    private static DictionarySense RestrictedSense(string key, string part, long writtenId, long readingId) => new()
    {
        SenseKey = key, Position = key == "kept" ? (short)1 : (short)2, PartOfSpeech = [part],
        Glosses = [new LocalizedGloss { LanguageTag = "en", GlossText = key, Position = 1 }],
        Applicabilities = [new SenseApplicability { WrittenFormId = writtenId, ReadingId = readingId }]
    };

    private static DictionaryEntry Entry(string canonicalId, string form, params string[] parts) => new()
    {
        Id = 1, CanonicalId = canonicalId,
        WrittenForms = [new WrittenForm { Id = 11, Form = form }],
        Readings = [new Reading { Id = 21, ReadingText = form }],
        Senses = [new DictionarySense { Id = 31, SenseKey = "1", Position = 1, PartOfSpeech = parts,
            Glosses = [new LocalizedGloss { Id = 101, LanguageTag = "en", GlossText = "fixture", Position = 1 }] }]
    };
}
