using Application.Conjugation.Models;
using Application.Dictionary;
using Application.Dictionary.Models;
using Application.Dictionary.Ports;
using Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace UnitTests;

public sealed class DictionaryReadCorrectnessTests
{
    private static DictionaryEntry Entry() => new()
    {
        CanonicalId = "dictionary:synthetic-restrictions",
        WrittenForms = [new() { Id = 1, Form = "生" }, new() { Id = 2, Form = "生成" }],
        Readings = [new() { Id = 11, ReadingText = "なま", RestrictedToForms = ["生"] },
            new() { Id = 12, ReadingText = "せいせい", RestrictedToForms = ["生成"] }],
        Senses = [Sense("raw", "n", 1, 11), Sense("generate", "v1", 2, 12),
            Sense("impossible", "v1", 1, 12)]
    };

    private static DictionarySense Sense(string key, string pos, long? written, long? reading) => new()
    {
        SenseKey = key, PartOfSpeech = [pos],
        Glosses = [new() { LanguageTag = "en", GlossText = key }],
        Applicabilities = [new() { WrittenFormId = written, ReadingId = reading }]
    };

    private static async Task<LookupResultDto> Lookup(DictionaryEntry entry, string query, string[]? candidatePos = null)
    {
        var repository = Substitute.For<IDictionaryRepository>();
        repository.SearchByFormOrReadingAsync(query, Arg.Any<CancellationToken>()).Returns([entry]);
        var tokenizer = Substitute.For<ITokenizerAdapter>();
        tokenizer.IsAvailable.Returns(true);
        if (candidatePos is not null)
        {
            ConjugationCandidate candidate = new(new("生成した", query, "Giải thích thử nghiệm.", "vi",
                new(0, 4), "conjugation:synthetic", 1, "synthetic", []), candidatePos);
            repository.SearchByFormOrReadingAsync("生成した", Arg.Any<CancellationToken>()).Returns([]);
            tokenizer.DeinflectAsync("生成した", Arg.Any<CancellationToken>()).Returns(
                new DeinflectionResult(true, "生成した", "", "", Candidates: [candidate], ConjugationStatus: "completed"));
            query = "生成した";
        }
        return await new LookupWordUseCase(repository, tokenizer).ExecuteAsync(query, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("生", "raw", 1, 11)]
    [InlineData("なま", "raw", 1, 11)]
    [InlineData("生成", "generate", 2, 12)]
    [InlineData("せいせい", "generate", 2, 12)]
    public async Task ExactProjectionFiltersJointFormReadingAndSense(string query, string sense, long form, long reading)
    {
        var match = (await Lookup(Entry(), query)).Matches.Should().ContainSingle().Subject;
        match.Senses.Should().ContainSingle().Which.SenseKey.Should().Be(sense);
        match.WrittenForms.Should().ContainSingle().Which.Id.Should().Be(form);
        match.Readings.Should().ContainSingle().Which.Id.Should().Be(reading);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task KanaOnlyReadingDoesNotAuthorizeARequiredWrittenForm(bool f09)
    {
        var entry = Entry(); entry.Readings.First().IsNoKanji = true;
        entry.Senses.Add(Sense("kana-only", "n", null, 11));
        var result = await Lookup(entry, "なま", f09 ? ["n"] : null);
        var match = result.Matches.Should().ContainSingle().Subject;
        match.WrittenForms.Should().BeEmpty(); match.MatchedWrittenForm.Should().BeNull();
        match.Readings.Should().ContainSingle().Which.Info.Should().Contain("reading:no-kanji");
        match.Senses.Should().ContainSingle().Which.SenseKey.Should().Be("kana-only");
    }

    [Fact]
    public async Task WrittenLookupCannotUseKanaOnlyReadingOrItsSense()
    {
        var entry = Entry(); entry.Readings.First().IsNoKanji = true;
        (await Lookup(entry, "生")).Matches.Should().BeEmpty();
        (await Lookup(entry, "生", ["n"])).Alternatives.Should().BeEmpty();
    }

    [Theory]
    [InlineData("生")]
    [InlineData("かな")]
    public async Task ContradictoryWrittenAndKanaOnlyConstraintsAreNeverServed(string query)
    {
        var entry = Entry();
        entry.Readings.Add(new() { Id = 13, ReadingText = "かな", IsNoKanji = true });
        entry.Senses.Add(Sense("contradictory", "n", 1, 13));
        entry.Senses.Add(Sense("kana", "n", null, 13));
        var matches = (await Lookup(entry, query)).Matches;
        matches.Should().ContainSingle();
        matches.SelectMany(m => m.Senses).Should().NotContain(s => s.SenseKey == "contradictory");
        matches.SelectMany(m => m.Senses).Should().Contain(s => s.SenseKey == (query == "かな" ? "kana" : "raw"));
    }

    [Theory]
    [InlineData("生")]
    [InlineData("なま")]
    public async Task F09CannotBorrowPosFromAnInapplicableSense(string query)
    {
        var result = await Lookup(Entry(), query, ["v1"]);
        result.Matches.Should().BeEmpty(); result.Alternatives.Should().BeEmpty();
    }

    [Fact]
    public async Task F09ProjectionExcludesApplicableSensesWithAnotherPos()
    {
        var entry = Entry(); entry.Senses.Add(Sense("noun", "n", 2, 12));
        var result = await Lookup(entry, "生成", ["v1"]);
        result.Matches.Single().Senses.Should().ContainSingle().Which.SenseKey.Should().Be("generate");
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("unknown", false)]
    [InlineData("pending", false)]
    [InlineData("rejected", false)]
    [InlineData("approved", true)]
    [InlineData("demo", true)]
    public async Task VietnameseRequiresExplicitReviewOrSyntheticDemoStatus(string? status, bool served)
    {
        var entry = Entry();
        entry.Senses.First().Glosses.Add(new() { LanguageTag = "vi", GlossText = "nghĩa thử nghiệm", ReviewStatus = status });
        var gloss = (await Lookup(entry, "生")).Matches.Single().Senses.Single().Glosses.Should().ContainSingle().Subject;
        gloss.LanguageTag.Should().Be(served ? "vi" : "en");
        entry.Senses.First().Glosses.Last().ReviewStatus.Should().Be(status);
    }

    [Fact]
    public async Task ReviewFilteringAndEnglishFallbackApplyIndependentlyPerSense()
    {
        var entry = Entry();
        entry.Senses.Add(Sense("second", "n", 1, 11));
        entry.Senses.First().Glosses.Add(new() { LanguageTag = "vi", GlossText = "đã duyệt", ReviewStatus = "approved" });
        entry.Senses.First().Glosses.Add(new() { LanguageTag = "vi", GlossText = "bị loại", ReviewStatus = "rejected" });
        entry.Senses.Last().Glosses.Add(new() { LanguageTag = "vi", GlossText = "chờ duyệt", ReviewStatus = "pending" });
        var senses = (await Lookup(entry, "生")).Matches.Single().Senses;
        senses.Single(s => s.SenseKey == "raw").Glosses.Should().ContainSingle().Which.GlossText.Should().Be("đã duyệt");
        senses.Single(s => s.SenseKey == "second").Glosses.Should().ContainSingle().Which.LanguageTag.Should().Be("en");
    }

    [Fact]
    public async Task ReadingOnlyEntryHasNoSyntheticWrittenForm()
    {
        var entry = Entry(); entry.WrittenForms.Clear();
        entry.Readings = [new() { Id = 11, ReadingText = "かな" }];
        entry.Senses = [Sense("kana", "n", null, 11)];
        var result = await Lookup(entry, "かな");
        result.Matches.Single().WrittenForms.Should().BeEmpty();
        result.Matches.Single().MatchedReading.Should().Be("かな");
    }
}
