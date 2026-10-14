using System.Diagnostics;
using System.Text.Json.Nodes;
using Application.Conjugation.Ports;
using Domain.Entities;
using FluentAssertions;
using Grpc.Core;
using Infrastructure.Adapters;
using Infrastructure.Adapters.Grpc;
using Infrastructure.Conjugation;
using NSubstitute;
using Xunit;

namespace IntegrationTests;

/// <summary>Isolated infrastructure boundary tests; live linguistic behavior is tested separately.</summary>
public sealed class DictionaryTokenizerAdapterTests
{
    [Theory]
    [InlineData("MIS-01")]
    [InlineData("ERR-01")]
    [InlineData("ERR-02")]
    [InlineData("DATA-01")]
    [InlineData("VI-01")]
    [InlineData("DB-01")]
    [InlineData("CANCEL-01")]
    public async Task ApprovedFaultScenariosExerciseTheCandidatePath(string caseId)
    {
        using var corpus = ConjugationCorpusFiles.Read(Path.Combine(ConjugationCorpusFiles.Packet, "cases.json"));
        var c = corpus.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("case_id").GetString() == caseId);
        var text = c.GetProperty("input").GetProperty("text").GetString()!;
        var fault = c.GetProperty("scenario").GetProperty("fault").GetString();
        var content = JsonNode.Parse(ConjugationCandidateData.ReadContent())!;
        if (fault == "vietnamese_content_missing")
            foreach (var p in content["paradigms"]!.AsArray().Where(p => p!["rule_id"]!.GetValue<string>() == "conjugation:verb-polite-past"))
                p!["explanation_vi"] = null;
        var datasets = Substitute.For<IConjugationDatasetRepository>();
        datasets.GetAsync(ConjugationCandidateData.Version, Arg.Any<CancellationToken>()).Returns(fault == "dataset_missing" ? null :
            new ConjugationDataset { Version = ConjugationCandidateData.Version, Content = content.ToJsonString() });
        var client = new TestClient(async (_, ct) => {
            if (fault == "sidecar_unavailable") throw new RpcException(new Status(StatusCode.Unavailable, "test unavailable"));
            if (fault == "sidecar_timeout_500ms") await Task.Delay(Timeout.Infinite, ct);
            return ObservedPolitePast();
        });
        var words = Substitute.For<Application.Dictionary.Ports.IDictionaryRepository>();
        words.SearchByFormOrReadingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);
        if (fault != "missing_base_entry") words.SearchByFormOrReadingAsync("食べる", Arg.Any<CancellationToken>()).Returns([
            new DictionaryEntry { CanonicalId = "dictionary:approved-fault", WrittenForms = [new() { Form = "食べる" }],
                Senses = [new() { SenseKey = "1", PartOfSpeech = ["v1"] }] }]);
        if (fault == "dictionary_repository_failure")
            words.SearchByFormOrReadingAsync("食べる", Arg.Any<CancellationToken>()).Returns<Task<IReadOnlyList<DictionaryEntry>>>(_ =>
                throw new InvalidOperationException("synthetic repository failure"));
        var useCase = new Application.Dictionary.LookupWordUseCase(words,
            new GrpcDictionaryTokenizerAdapter(client, datasets, ConjugationCandidateData.Version));
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        if (fault == "caller_cancelled") {
            caller.Cancel(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => useCase.ExecuteAsync(text, caller.Token));
            client.CallCount.Should().Be(0); return;
        }
        if (fault == "dictionary_repository_failure") {
            await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(text, caller.Token)); return;
        }
        var result = await useCase.ExecuteAsync(text, caller.Token);
        result.Conjugation.Should().BeNull(); result.Alternatives.Should().BeNullOrEmpty();
        result.Capabilities.Conjugation.Should().Be(c.GetProperty("expected").GetProperty("capability").GetString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task UnpinnedDatasetDoesNotContactRepositoryOrProvider(string? version)
    {
        var repository = Substitute.For<IConjugationDatasetRepository>();
        var client = new TestClient((_, _) => Task.FromResult(ObservedPolitePast()));
        var adapter = new GrpcDictionaryTokenizerAdapter(client, repository, version);

        var result = await adapter.DeinflectAsync("食べました", TestContext.Current.CancellationToken);

        adapter.IsAvailable.Should().BeFalse();
        result.IsAvailable.Should().BeFalse();
        result.Detail.Should().BeNull();
        client.CallCount.Should().Be(0);
        repository.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task AbsentPinnedDatasetDoesNotCallProvider()
    {
        var repository = Substitute.For<IConjugationDatasetRepository>();
        var client = new TestClient((_, _) => Task.FromResult(ObservedPolitePast()));

        var result = await new GrpcDictionaryTokenizerAdapter(client, repository, "absent")
            .DeinflectAsync("食べました", TestContext.Current.CancellationToken);

        result.IsAvailable.Should().BeFalse();
        result.ConjugationStatus.Should().Be("unavailable");
        client.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task UnavailableGrpcProviderDegradesWithoutInventingResult()
    {
        var client = new TestClient((_, _) => Task.FromException<TokenizeResponse>(
            new RpcException(new Status(StatusCode.Unavailable, "provider unavailable"))));

        var result = await Adapter(client).DeinflectAsync("食べました", TestContext.Current.CancellationToken);

        result.IsAvailable.Should().BeFalse();
        result.IsSuccess.Should().BeFalse();
        result.Detail.Should().BeNull();
    }

    [Fact]
    public async Task ProviderDeadlineCancelsCallAndDegradesWithinBoundedTime()
    {
        var providerCancelled = false;
        var client = new TestClient(async (_, ct) =>
        {
            try { await Task.Delay(Timeout.Infinite, ct); }
            catch (OperationCanceledException) { providerCancelled = true; throw; }
            return new();
        });
        var stopwatch = Stopwatch.StartNew();

        var result = await Adapter(client).DeinflectAsync("食べました", TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        providerCancelled.Should().BeTrue();
        stopwatch.Elapsed.Should().BeGreaterThan(TimeSpan.FromMilliseconds(350));
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));
        result.IsAvailable.Should().BeFalse();
        result.Detail.Should().BeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CallerCancellationPropagatesForTaskAndGrpcCancellation(bool grpcFailure)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var client = new TestClient(async (_, ct) =>
        {
            started.TrySetResult();
            try { await Task.Delay(Timeout.Infinite, ct); }
            catch (OperationCanceledException) when (grpcFailure)
            {
                throw new RpcException(new Status(StatusCode.Cancelled, "caller cancelled"));
            }
            return new();
        });
        var pending = Adapter(client).DeinflectAsync("食べました", caller.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        caller.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        exception.CancellationToken.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public async Task AlreadyCancelledCallerDoesNotContactProvider()
    {
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        var client = new TestClient((_, _) => Task.FromResult(ObservedPolitePast()));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Adapter(client).DeinflectAsync("食べました", caller.Token));

        client.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task MissingVietnameseRuleContentReportsUnavailableNotGuessedExplanation()
    {
        var document = JsonNode.Parse(ConjugationSeedData.ReadDevelopmentContent())!;
        var rule = document["rules"]!.AsArray().Single(x =>
            x!["rule_id"]!.GetValue<string>() == "conjugation:verb-polite-past")!;
        rule["explanation_vi"] = null;
        var client = new TestClient((_, _) => Task.FromResult(ObservedPolitePast()));

        var result = await Adapter(client, document.ToJsonString())
            .DeinflectAsync("食べました", TestContext.Current.CancellationToken);

        result.IsAvailable.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.ConjugationStatus.Should().Be("unavailable");
        result.RuleExplanation.Should().BeEmpty();
        result.Detail.Should().BeNull();
    }

    [Fact]
    public async Task RepositoryFailureIsNotMisclassifiedAsProviderUnavailability()
    {
        var repository = Substitute.For<IConjugationDatasetRepository>();
        repository.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ConjugationDataset?>(new InvalidOperationException("dataset database failure")));
        var client = new TestClient((_, _) => Task.FromResult(ObservedPolitePast()));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new GrpcDictionaryTokenizerAdapter(client, repository, ConjugationSeedData.DevelopmentVersion)
                .DeinflectAsync("食べました", TestContext.Current.CancellationToken));

        client.CallCount.Should().Be(0);
    }

    private static GrpcDictionaryTokenizerAdapter Adapter(TestClient client, string? content = null)
    {
        var repository = Substitute.For<IConjugationDatasetRepository>();
        repository.GetAsync(ConjugationSeedData.DevelopmentVersion, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ConjugationDataset?>(new()
            {
                Version = ConjugationSeedData.DevelopmentVersion,
                Content = content ?? ConjugationSeedData.ReadDevelopmentContent()
            }));
        return new(client, repository, ConjugationSeedData.DevelopmentVersion);
    }

    private static TokenizeResponse ObservedPolitePast() => new()
    {
        Tokens =
        {
            new TokenSpec { Surface = "食べ", BaseForm = "食べる", Pos = "動詞,一般,*,*,下一段-バ行,連用形-一般", SpanStart = 0, SpanEnd = 2 },
            new TokenSpec { Surface = "まし", BaseForm = "ます", Pos = "助動詞,*,*,*,助動詞-マス,連用形-一般", SpanStart = 2, SpanEnd = 4 },
            new TokenSpec { Surface = "た", BaseForm = "た", Pos = "助動詞,*,*,*,助動詞-タ,終止形-一般", SpanStart = 4, SpanEnd = 5 }
        }
    };

    private sealed class TestClient(Func<TokenizeRequest, CancellationToken, Task<TokenizeResponse>> respond)
        : TokenizerService.TokenizerServiceClient
    {
        public int CallCount { get; private set; }

        public override AsyncUnaryCall<TokenizeResponse> TokenizeAsync(TokenizeRequest request, CallOptions options)
        {
            CallCount++;
            return new(respond(request, options.CancellationToken), Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess, () => new Metadata(), () => { });
        }
    }
}
