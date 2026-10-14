using Application.Conjugation.Models;
using Application.Dictionary;
using Application.Dictionary.Models;
using Application.Dictionary.Ports;
using Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace UnitTests;

public sealed class LookupConjugationCandidateTests
{
    private static ConjugationCandidate Candidate(string id = "conjugation:a", string explanation = "Giải thích đã duyệt.") =>
        new(new("食べられる", "食べる", explanation, "vi", new(0, 5), id, 1, "candidate",
            [new("食べられる", "Thành phần đã duyệt.", new(0, 5))]), ["v1"]);
    private static DictionaryEntry Entry(string pos = "v1") => new() { CanonicalId = "dictionary:test", WrittenForms = [new() { Id = 1, Form = "食べる" }],
        Senses = [new() { SenseKey = "1", PartOfSpeech = [pos] }] };
    private static (LookupWordUseCase UseCase, IDictionaryRepository Repository) Setup(ConjugationCandidate[] candidates, DictionaryEntry[] entries)
    {
        var repository = Substitute.For<IDictionaryRepository>();
        repository.SearchByFormOrReadingAsync("食べられる", Arg.Any<CancellationToken>()).Returns([]);
        repository.SearchByFormOrReadingAsync("食べる", Arg.Any<CancellationToken>()).Returns(entries);
        var adapter = Substitute.For<ITokenizerAdapter>(); adapter.IsAvailable.Returns(true);
        adapter.DeinflectAsync("食べられる", Arg.Any<CancellationToken>()).Returns(new DeinflectionResult(true, "食べられる", "", "",
            ConjugationStatus: "completed", Candidates: candidates));
        return (new(repository, adapter), repository);
    }

    [Fact]
    public async Task VerifiesCanonicalClassThenKeepsAllMeaningsWithOneRootQuery()
    {
        var (useCase, repository) = Setup([Candidate("conjugation:b"), Candidate("conjugation:a")], [Entry()]);
        var result = await useCase.ExecuteAsync("食べられる", TestContext.Current.CancellationToken);
        result.Matches.Should().ContainSingle(); result.Conjugation.Should().BeNull();
        result.Alternatives!.Select(a => a.RuleId).Should().Equal("conjugation:a", "conjugation:b");
        await repository.Received(1).SearchByFormOrReadingAsync("食べる", Arg.Any<CancellationToken>());
        System.Text.Json.JsonSerializer.Serialize(result).Should().NotContain("Alternatives");
    }

    [Fact]
    public async Task PreservesSingularDictionaryShapeWhenOnlyOneMeaningIsVerified()
    {
        var (useCase, _) = Setup([Candidate()], [Entry()]);
        var result = await useCase.ExecuteAsync("食べられる", TestContext.Current.CancellationToken);
        result.Conjugation!.Detail!.BaseForm.Should().Be("食べる");
        result.Capabilities.Conjugation.Should().Be("completed");
    }

    [Fact]
    public async Task DeduplicatesPublicIdentityOnlyAfterCanonicalClassVerification()
    {
        var (useCase, _) = Setup([Candidate() with { DictionaryPartOfSpeech = ["n"] }, Candidate(),
            Candidate() with { DictionaryPartOfSpeech = ["v1", "verb:ichidan"] }], [Entry()]);
        var result = await useCase.ExecuteAsync("食べられる", TestContext.Current.CancellationToken);
        result.Alternatives.Should().ContainSingle(); result.Conjugation.Should().NotBeNull();
    }

    [Theory]
    [InlineData("n")]
    [InlineData("adj-na")]
    public async Task RejectsIncorrectDictionaryClass(string pos)
    {
        var (useCase, _) = Setup([Candidate()], [Entry(pos)]);
        var result = await useCase.ExecuteAsync("食べられる", TestContext.Current.CancellationToken);
        result.Alternatives.Should().BeEmpty(); result.Capabilities.Conjugation.Should().Be("unavailable");
    }

    [Fact]
    public async Task DoesNotUsePosFromASenseRestrictedToAnotherWrittenForm()
    {
        var entry = Entry(); entry.Senses.Single().Applicabilities.Add(new() { WrittenFormId = 99 });
        var (useCase, _) = Setup([Candidate()], [entry]);
        (await useCase.ExecuteAsync("食べられる", TestContext.Current.CancellationToken)).Alternatives.Should().BeEmpty();
    }

    [Fact]
    public async Task MissingCanonicalEntryIsCompletedEmptyButMissingViIsUnavailable()
    {
        var (missing, _) = Setup([Candidate()], []);
        var empty = await missing.ExecuteAsync("食べられる", TestContext.Current.CancellationToken);
        empty.Matches.Should().BeEmpty(); empty.Capabilities.Conjugation.Should().Be("completed");
        var (blank, _) = Setup([Candidate(explanation: " ")], [Entry()]);
        var result = await blank.ExecuteAsync("食べられる", TestContext.Current.CancellationToken);
        result.Alternatives.Should().BeEmpty(); result.Capabilities.Conjugation.Should().Be("unavailable");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadingOnlyBaseHonorsTheJointReadingAndWrittenSenseRestriction(bool allowed)
    {
        var entry = Entry(); entry.Readings = [new() { Id = 2, ReadingText = "たべる", RestrictedToForms = [allowed ? "食べる" : "他の表記"] }];
        entry.Senses.Single().Applicabilities.Add(new() { WrittenFormId = 1, ReadingId = 2 });
        var c = Candidate() with { Detail = Candidate().Detail with { BaseForm = "たべる" } };
        var (useCase, repository) = Setup([c], []);
        repository.SearchByFormOrReadingAsync("たべる", Arg.Any<CancellationToken>()).Returns([entry]);
        var result = await useCase.ExecuteAsync("食べられる", TestContext.Current.CancellationToken);
        result.Alternatives!.Count.Should().Be(allowed ? 1 : 0);
    }

    [Fact]
    public async Task AReadingRestrictedToAnotherSpellingCannotAuthorizeTheMatchedSpelling()
    {
        var entry = Entry(); entry.Readings = [new() { Id = 2, ReadingText = "食べる", RestrictedToForms = ["他の表記"] }];
        entry.Senses.Single().Applicabilities.Add(new() { WrittenFormId = 1, ReadingId = 2 });
        var (useCase, _) = Setup([Candidate()], [entry]);
        (await useCase.ExecuteAsync("食べられる", TestContext.Current.CancellationToken)).Alternatives.Should().BeEmpty();
    }

    [Fact]
    public async Task CanonicalRepositoryFailureAndCallerCancellationPropagate()
    {
        var (useCase, repo) = Setup([Candidate()], [Entry()]);
        repo.SearchByFormOrReadingAsync("食べる", Arg.Any<CancellationToken>()).Returns<Task<IReadOnlyList<DictionaryEntry>>>(_ => throw new InvalidOperationException("test failure"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync("食べられる", TestContext.Current.CancellationToken));
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => useCase.ExecuteAsync("食べられる", cancelled.Token));
    }
}
