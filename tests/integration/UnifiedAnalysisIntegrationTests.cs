namespace IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Application;
using Application.Analysis;
using Application.Analysis.Models;
using Application.Dictionary;
using Application.Dictionary.Ports;
using Application.Grammar;
using Application.Grammar.Helpers;
using Application.Grammar.Models;
using Application.Grammar.Ports;
using Application.Kanji;
using Application.Kanji.Ports;
using Domain.Entities;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using WebApi;
using WebApi.Endpoints;
using Xunit;

/// <summary>
/// Integration and HTTP contract tests for Unified Analysis (F-05).
/// PostgreSQL is provided by the shared Testcontainers fixture. The grammar
/// tokenizer is deterministic so these tests do not depend on the sidecar.
/// </summary>
public sealed class UnifiedAnalysisIntegrationTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private DbContextOptions<AppDbContext> CreateDbContextOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
    }

    [Fact]
    public async Task AnalyzeText_WithRealDb_ReturnsResponseWithCapabilities()
    {
        await SeedAnalysisDataAsync();

        var options = CreateDbContextOptions();
        var grammarTokenizer = Substitute.For<IGrammarTokenizerPort>();
        grammarTokenizer.TokenizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<NormalizedTokenDto>());

        await using var dbContext = new AppDbContext(options);
        var useCase = CreateUseCase(dbContext, grammarTokenizer: grammarTokenizer);

        var result = await useCase.ExecuteAsync(
            new AnalysisRequestDto("食べた"),
            TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.NormalizedText.Should().Be("食べた");
        result.Capabilities.Should().NotBeNull();
        result.DictionaryMatches.Should().NotBeNull();
        result.Kanji.Should().NotBeNull();
        result.GrammarOccurrences.Should().NotBeNull();
        result.Tokens.Should().NotBeNull();
        result.Conjugations.Should().NotBeNull();
        result.Capabilities.Morphology.Should().Be(CapabilityStatus.Unavailable);
        result.Capabilities.Conjugation.Should().Be(CapabilityStatus.Unavailable);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AnalyzeText_EmptyText_ThrowsArgumentException(string text)
    {
        await SeedAnalysisDataAsync();
        await using var dbContext = new AppDbContext(CreateDbContextOptions());
        var useCase = CreateUseCase(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(
            new AnalysisRequestDto(text),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AnalyzeText_TextExceeds1000Chars_ThrowsArgumentException()
    {
        await SeedAnalysisDataAsync();
        await using var dbContext = new AppDbContext(CreateDbContextOptions());
        var useCase = CreateUseCase(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(
            new AnalysisRequestDto(new string('あ', 1001)),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AnalyzeText_ValidInteractionId_Accepted()
    {
        await SeedAnalysisDataAsync();
        var grammarTokenizer = Substitute.For<IGrammarTokenizerPort>();
        grammarTokenizer.TokenizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<NormalizedTokenDto>());

        await using var dbContext = new AppDbContext(CreateDbContextOptions());
        var useCase = CreateUseCase(dbContext, grammarTokenizer: grammarTokenizer);

        var result = await useCase.ExecuteAsync(
            new AnalysisRequestDto("食べた", Guid.NewGuid().ToString()),
            TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task AnalyzeText_NullInteractionId_NoError()
    {
        await SeedAnalysisDataAsync();
        var grammarTokenizer = Substitute.For<IGrammarTokenizerPort>();
        grammarTokenizer.TokenizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<NormalizedTokenDto>());

        await using var dbContext = new AppDbContext(CreateDbContextOptions());
        var useCase = CreateUseCase(dbContext, grammarTokenizer: grammarTokenizer);

        var result = await useCase.ExecuteAsync(
            new AnalysisRequestDto("食べた", null),
            TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task AnalyzeText_GrammarSidecarDown_GracefulDegradation()
    {
        await SeedAnalysisDataAsync();
        await using var api = await StartApiAsync(new ThrowingGrammarTokenizer());

        var response = await api.Client.PostAsJsonAsync(
            "/api/analysis",
            new { text = "食べた" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = await ReadJsonAsync(response);
        var data = document.RootElement.GetProperty("data");
        var capabilities = data.GetProperty("capabilities");

        capabilities.GetProperty("grammar").GetString().Should().Be(CapabilityStatus.Failed);
        data.GetProperty("grammar_occurrences").EnumerateArray().Should().BeEmpty();
        capabilities.GetProperty("vocabulary").GetString().Should().Be(CapabilityStatus.Completed);
        capabilities.GetProperty("kanji").GetString().Should().Be(CapabilityStatus.Completed);
    }

    [Fact]
    public async Task AnalyzeText_NoAuth_WorksWithoutAuthentication()
    {
        await SeedAnalysisDataAsync();
        await using var api = await StartApiAsync(new DeterministicGrammarTokenizer());

        var response = await api.Client.PostAsJsonAsync(
            "/api/analysis",
            new { text = "食べました" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HttpAnalysis_ReturnsEnvelopeAndCapabilityStatuses()
    {
        await SeedAnalysisDataAsync();
        await using var api = await StartApiAsync(new DeterministicGrammarTokenizer());

        var response = await api.Client.PostAsJsonAsync(
            "/api/analysis",
            new { text = "食べました" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = await ReadJsonAsync(response);
        var root = document.RootElement;
        var data = root.GetProperty("data");
        var capabilities = data.GetProperty("capabilities");
        var meta = root.GetProperty("meta");

        data.GetProperty("dictionary_matches").ValueKind.Should().Be(JsonValueKind.Array);
        data.GetProperty("kanji").ValueKind.Should().Be(JsonValueKind.Array);
        data.GetProperty("grammar_occurrences").ValueKind.Should().Be(JsonValueKind.Array);
        data.GetProperty("dictionary_matches").GetArrayLength().Should().BeGreaterThan(0);
        data.GetProperty("kanji").EnumerateArray()
            .Should().Contain(x => x.GetProperty("character").GetString() == "食");
        capabilities.GetProperty("vocabulary").GetString().Should().Be(CapabilityStatus.Completed);
        capabilities.GetProperty("kanji").GetString().Should().Be(CapabilityStatus.Completed);
        capabilities.GetProperty("grammar").GetString().Should().Be(CapabilityStatus.Completed);
        capabilities.GetProperty("morphology").GetString().Should().Be(CapabilityStatus.Unavailable);
        capabilities.GetProperty("conjugation").GetString().Should().Be(CapabilityStatus.Unavailable);
        var requestId = meta.GetProperty("request_id").GetString();
        requestId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(requestId, out _).Should().BeTrue();
        meta.GetProperty("contract_version").GetString().Should().Be("1");
        meta.GetProperty("interaction_id").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task HttpAnalysis_EchoesInteractionId_AndDoesNotRequireAuth()
    {
        await SeedAnalysisDataAsync();
        await using var api = await StartApiAsync(new DeterministicGrammarTokenizer());
        var interactionId = Guid.NewGuid().ToString();

        var response = await api.Client.PostAsJsonAsync(
            "/api/analysis",
            new { text = "食べました", interaction_id = interactionId },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = await ReadJsonAsync(response);
        document.RootElement.GetProperty("meta").GetProperty("interaction_id")
            .GetString().Should().Be(interactionId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HttpAnalysis_EmptyText_ReturnsStructuredValidationError(string text)
    {
        await SeedAnalysisDataAsync();
        await using var api = await StartApiAsync(new DeterministicGrammarTokenizer());

        var response = await api.Client.PostAsJsonAsync(
            "/api/analysis",
            new { text },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var document = await ReadJsonAsync(response);
        document.RootElement.GetProperty("error_code").GetString()
            .Should().Be("VALIDATION_FAILED");
        document.RootElement.GetProperty("request_id").GetString()
            .Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task HttpAnalysis_TextExceeds1000Chars_ReturnsBadRequest()
    {
        await SeedAnalysisDataAsync();
        await using var api = await StartApiAsync(new DeterministicGrammarTokenizer());

        var response = await api.Client.PostAsJsonAsync(
            "/api/analysis",
            new { text = new string('あ', 1001) },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HttpAnalysis_InvalidInteractionId_ReturnsBadRequest()
    {
        await SeedAnalysisDataAsync();
        await using var api = await StartApiAsync(new DeterministicGrammarTokenizer());

        var response = await api.Client.PostAsJsonAsync(
            "/api/analysis",
            new { text = "食べました", interaction_id = "not-a-uuid" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var document = await ReadJsonAsync(response);
        document.RootElement.GetProperty("error_code").GetString()
            .Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task HttpAnalysis_KanjiFailure_ReturnsPartialResult()
    {
        await SeedAnalysisDataAsync();
        var failingKanjiRepository = Substitute.For<IKanjiRepository>();
        failingKanjiRepository.GetByCharactersAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        await using var api = await StartApiAsync(
            new DeterministicGrammarTokenizer(),
            services =>
            {
                services.RemoveAll<IKanjiRepository>();
                services.AddSingleton(failingKanjiRepository);
            });

        var response = await api.Client.PostAsJsonAsync(
            "/api/analysis",
            new { text = "食べました" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = await ReadJsonAsync(response);
        var data = document.RootElement.GetProperty("data");
        data.GetProperty("dictionary_matches").GetArrayLength().Should().BeGreaterThan(0);
        data.GetProperty("kanji").GetArrayLength().Should().Be(0);
        data.GetProperty("capabilities").GetProperty("kanji").GetString()
            .Should().Be(CapabilityStatus.Failed);
    }

    [Fact]
    public async Task HttpAnalysis_AllCapabilitiesFailure_ReturnsServiceUnavailable()
    {
        var failingDictionaryRepository = Substitute.For<IDictionaryRepository>();
        failingDictionaryRepository.SearchByFormOrReadingAsync(
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("database unavailable"));
        var failingKanjiRepository = Substitute.For<IKanjiRepository>();
        failingKanjiRepository.GetByCharactersAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        await using var api = await StartApiAsync(
            new ThrowingGrammarTokenizer(),
            services =>
            {
                services.RemoveAll<IDictionaryRepository>();
                services.AddSingleton(failingDictionaryRepository);
                services.RemoveAll<IKanjiRepository>();
                services.AddSingleton(failingKanjiRepository);
            });

        var response = await api.Client.PostAsJsonAsync(
            "/api/analysis",
            new { text = "食べました" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        using var document = await ReadJsonAsync(response);
        document.RootElement.GetProperty("error_code").GetString()
            .Should().Be("SERVICE_UNAVAILABLE");
        document.RootElement.GetProperty("retryable").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task HttpAnalysis_MalformedJson_ReturnsStructuredValidationError()
    {
        await SeedAnalysisDataAsync();
        await using var api = await StartApiAsync(new DeterministicGrammarTokenizer());
        using var content = new StringContent("{\"text\":", System.Text.Encoding.UTF8, "application/json");

        var response = await api.Client.PostAsync(
            "/api/analysis",
            content,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var document = await ReadJsonAsync(response);
        document.RootElement.GetProperty("error_code").GetString()
            .Should().Be("VALIDATION_FAILED");
        document.RootElement.GetProperty("request_id").GetString()
            .Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task HttpAnalysis_DictionaryMatchesAreEquivalentToStandaloneEndpoint()
    {
        await SeedAnalysisDataAsync();
        await using var api = await StartApiAsync(new DeterministicGrammarTokenizer());

        var analysisResponse = await api.Client.PostAsJsonAsync(
            "/api/analysis",
            new { text = "食べました" },
            TestContext.Current.CancellationToken);
        var dictionaryResponse = await api.Client.GetAsync(
            "/api/dictionary/lookup?q=%E9%A3%9F%E3%81%B9%E3%81%BE%E3%81%97%E3%81%9F",
            TestContext.Current.CancellationToken);

        using var analysis = await ReadJsonAsync(analysisResponse);
        using var dictionary = await ReadJsonAsync(dictionaryResponse);
        var analysisMatches = analysis.RootElement.GetProperty("data")
            .GetProperty("dictionary_matches").EnumerateArray()
            .Select(x => x.GetRawText()).ToArray();
        var dictionaryMatches = dictionary.RootElement.GetProperty("data")
            .GetProperty("matches").EnumerateArray()
            .Select(x => x.GetRawText()).ToArray();

        analysisResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        dictionaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        analysisMatches.Should().Equal(dictionaryMatches);
    }

    [Fact]
    public async Task HttpAnalysis_KanjiMatchesAreEquivalentToStandaloneEndpoint()
    {
        await SeedAnalysisDataAsync();
        await using var api = await StartApiAsync(new DeterministicGrammarTokenizer());

        var analysisResponse = await api.Client.PostAsJsonAsync(
            "/api/analysis",
            new { text = "食べました" },
            TestContext.Current.CancellationToken);
        var kanjiResponse = await api.Client.PostAsJsonAsync(
            "/api/kanji/lookup",
            new { text = "食べました" },
            TestContext.Current.CancellationToken);

        using var analysis = await ReadJsonAsync(analysisResponse);
        using var kanji = await ReadJsonAsync(kanjiResponse);
        var analysisMatches = analysis.RootElement.GetProperty("data")
            .GetProperty("kanji").EnumerateArray()
            .Select(x => x.GetRawText()).ToArray();
        var kanjiMatches = kanji.RootElement.GetProperty("data").EnumerateArray()
            .Select(x => x.GetRawText()).ToArray();

        analysisResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        kanjiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        analysisMatches.Should().Equal(kanjiMatches);
    }

    [Fact]
    public async Task HttpAnalysis_GrammarOccurrencesAreEquivalentToStandaloneEndpoint()
    {
        await SeedAnalysisDataAsync();
        await using var api = await StartApiAsync(new DeterministicGrammarTokenizer());

        var analysisResponse = await api.Client.PostAsJsonAsync(
            "/api/analysis",
            new { text = "雨が降っている" },
            TestContext.Current.CancellationToken);
        var grammarResponse = await api.Client.PostAsJsonAsync(
            "/api/grammar/detect",
            new { text = "雨が降っている" },
            TestContext.Current.CancellationToken);

        using var analysis = await ReadJsonAsync(analysisResponse);
        using var grammar = await ReadJsonAsync(grammarResponse);
        var analysisOccurrences = analysis.RootElement.GetProperty("data")
            .GetProperty("grammar_occurrences");
        var grammarOccurrences = grammar.RootElement.GetProperty("data");

        analysisResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        grammarResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        analysisOccurrences.GetArrayLength().Should().BeGreaterThan(0);
        analysisOccurrences.GetRawText().Should().Be(grammarOccurrences.GetRawText());
    }

    private async Task SeedAnalysisDataAsync()
    {
        await using var dbContext = new AppDbContext(CreateDbContextOptions());
        await dbContext.Database.MigrateAsync(TestContext.Current.CancellationToken);

        if (!await dbContext.DictionaryEntries.AnyAsync(
                e => e.WrittenForms.Any(w => w.Form == "食べました"),
                TestContext.Current.CancellationToken))
        {
            dbContext.DictionaryEntries.Add(new DictionaryEntry
            {
                CanonicalId = $"dictionary:unified-analysis-{Guid.NewGuid():N}",
                WrittenForms = [new WrittenForm { Form = "食べました", IsCommon = true }],
                Readings = [new Reading { ReadingText = "たべました", IsCommon = true }],
                Senses = [new DictionarySense
                {
                    SenseKey = "1",
                    Position = 1,
                    PartOfSpeech = ["verb"],
                    Glosses = [new LocalizedGloss
                    {
                        LanguageTag = "vi",
                        GlossText = "đã ăn",
                        Position = 1
                    }]
                }]
            });
        }

        if (!await dbContext.KanjiRecords.AnyAsync(
                k => k.Character == "食",
                TestContext.Current.CancellationToken))
        {
            dbContext.KanjiRecords.Add(new KanjiRecord
            {
                CanonicalId = $"kanji:unified-analysis-{Guid.NewGuid():N}",
                Character = "食",
                StrokeCount = 9,
                Grade = 2,
                JlptLevel = 5,
                JlptProvenance = "derived",
                HanViet = "THỰC",
                UnicodeCodepoint = "U+98DF",
                OnReadings = ["ショク"],
                KunReadings = ["た.べる"],
                MeaningsVi = ["ăn"],
                MeaningsEn = ["eat"]
            });
        }

        if (!await dbContext.GrammarRules.AnyAsync(
                g => g.Pattern == "〜ている",
                TestContext.Current.CancellationToken))
        {
            dbContext.GrammarRules.Add(new GrammarRule
            {
                CanonicalId = $"grammar:unified-analysis-{Guid.NewGuid():N}",
                Pattern = "〜ている",
                JlptLevel = "N5",
                MeaningVi = "đang diễn ra",
                MatcherMetadata = """[ { "surface": "て" }, { "base": "いる" } ]""",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private AnalyzeTextUseCase CreateUseCase(
        AppDbContext dbContext,
        ITokenizerAdapter? vocabTokenizer = null,
        IGrammarTokenizerPort? grammarTokenizer = null)
    {
        vocabTokenizer ??= Substitute.For<ITokenizerAdapter>();
        grammarTokenizer ??= Substitute.For<IGrammarTokenizerPort>();

        var dictionaryRepo = new EfDictionaryRepository(dbContext);
        var kanjiRepo = new EfKanjiRepository(dbContext);
        var grammarRepo = new EfGrammarRepository(dbContext);

        var lookupWord = new LookupWordUseCase(dictionaryRepo, vocabTokenizer);
        var lookupKanji = new LookupKanjiUseCase(kanjiRepo);
        var matcher = new GrammarSequenceMatcher();
        var detectGrammar = new DetectGrammarUseCase(grammarTokenizer, grammarRepo, matcher);

        return new AnalyzeTextUseCase(lookupWord, lookupKanji, detectGrammar);
    }

    private async Task<RunningApi> StartApiAsync(
        IGrammarTokenizerPort tokenizer,
        Action<IServiceCollection>? configureServices = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
            ApplicationName = typeof(UnifiedAnalysisIntegrationTests).Assembly.GetName().Name
        });
        builder.Configuration["ConnectionStrings:DefaultConnection"] = fixture.ConnectionString;

        ApiComposition.RegisterServices(builder);
        builder.Services.RemoveAll<IGrammarTokenizerPort>();
        builder.Services.AddSingleton(tokenizer);
        configureServices?.Invoke(builder.Services);

        var app = builder.Build();
        ApiComposition.MapEndpoints(app);

        var port = GetFreePort();
        app.Urls.Add($"http://127.0.0.1:{port}");
        await app.StartAsync(TestContext.Current.CancellationToken);

        return new RunningApi(app, new HttpClient
        {
            BaseAddress = new Uri($"http://127.0.0.1:{port}")
        });
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(body);
    }

    private static string[] GetGrammarKeys(JsonElement occurrences)
    {
        return occurrences.EnumerateArray()
            .Select(x => $"{x.GetProperty("grammar_id").GetString()}|{x.GetProperty("matched_text").GetString()}|{x.GetProperty("span").GetProperty("start").GetInt32()}|{x.GetProperty("span").GetProperty("end").GetInt32()}")
            .ToArray();
    }

    private sealed class RunningApi(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public WebApplication App { get; } = app;
        public HttpClient Client { get; } = client;

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.StopAsync();
            await App.DisposeAsync();
        }
    }

    private sealed class DeterministicGrammarTokenizer : IGrammarTokenizerPort
    {
        public Task<IReadOnlyList<NormalizedTokenDto>> TokenizeAsync(
            string text,
            CancellationToken ct = default)
        {
            IReadOnlyList<NormalizedTokenDto> tokens = text == "雨が降っている"
                ? [
                    new("雨", "雨", "名詞,一般,*,*,*,*", 0, 1),
                    new("が", "が", "助詞,格助詞,*,*,*,*", 1, 2),
                    new("降っ", "降る", "動詞,一般,*,*,*,*", 2, 4),
                    new("て", "て", "助詞,接続助詞,*,*,*,*", 4, 5),
                    new("いる", "いる", "動詞,非自立可能,*,*,*,*", 5, 7)
                ]
                : [];

            return Task.FromResult(tokens);
        }
    }

    private sealed class ThrowingGrammarTokenizer : IGrammarTokenizerPort
    {
        public Task<IReadOnlyList<NormalizedTokenDto>> TokenizeAsync(
            string text,
            CancellationToken ct = default)
        {
            throw new InvalidOperationException("tokenizer unavailable");
        }
    }
}
