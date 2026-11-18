namespace IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Application;
using Application.Dictionary.Models;
using Application.Dictionary.Ports;
using Application.Grammar.Models;
using Application.Grammar.Ports;
using Application.Kanji.Ports;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WebApi.Endpoints;
using Xunit;

public sealed class ScanIntegrationTests
{
    private const string InteractionId = "a1d3b846-c077-4b5a-8b97-1d4d583ab0ad";
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Scan_TargetStartsBeforePointAndKeepsUtf16Spans()
    {
        await using var host = await StartAsync();
        using var response = await SendAsync(host.Client, "😀日本語、猫", 4);
        using var json = await ReadOkAsync(response);
        var root = json.RootElement;
        var meta = root.GetProperty("meta");
        Assert.Equal("1", meta.GetProperty("contract_version").GetString());
        Assert.Equal(InteractionId, meta.GetProperty("interaction_id").GetString());
        Assert.True(Guid.TryParseExact(meta.GetProperty("request_id").GetString(), "D", out _));
        var data = root.GetProperty("data");
        AssertSpan(data.GetProperty("target_span"), 2, 5);
        var analysis = data.GetProperty("analysis");
        Assert.Equal("😀日本語、猫", analysis.GetProperty("normalized_text").GetString());
        var occurrences = analysis.GetProperty("vocabulary_occurrences");
        Assert.Equal(2, occurrences.GetArrayLength());
        Assert.Equal("日本語", occurrences[0].GetProperty("surface").GetString());
        AssertSpan(occurrences[0].GetProperty("span"), 2, 5);
        Assert.Equal("猫", occurrences[1].GetProperty("surface").GetString());
    }

    [Fact]
    public async Task Scan_WrapsExactWordWithoutDeinflecting_AndAnalysisContractsRemainUnchanged()
    {
        await using var host = await StartAsync();
        using var response = await SendAsync(host.Client, "猫", 0);
        using var json = await ReadOkAsync(response);
        var data = json.RootElement.GetProperty("data");
        AssertSpan(data.GetProperty("target_span"), 0, 1);
        var occurrence = Assert.Single(data.GetProperty("analysis").GetProperty("vocabulary_occurrences").EnumerateArray());
        AssertSpan(occurrence.GetProperty("span"), 0, 1);
        Assert.Equal("dictionary:cat", occurrence.GetProperty("matches")[0].GetProperty("canonicalId").GetString());
        await host.Deinflector.DidNotReceiveWithAnyArgs().DeinflectAsync(default!, Cancellation);

        foreach (var version in new[] { "1", "2", "3" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/analysis")
            { Content = JsonContent.Create(new { text = "猫", interaction_id = InteractionId }) };
            request.Headers.Add("X-Contract-Version", version);
            using var legacyResponse = await host.Client.SendAsync(request, Cancellation);
            Assert.Equal(HttpStatusCode.OK, legacyResponse.StatusCode);
            using var legacyJson = JsonDocument.Parse(await legacyResponse.Content.ReadAsStringAsync(Cancellation));
            Assert.Equal(version, legacyJson.RootElement.GetProperty("meta").GetProperty("contract_version").GetString());
            Assert.False(legacyJson.RootElement.GetProperty("data").TryGetProperty("vocabulary_occurrences", out _));
        }
    }

    [Fact]
    public async Task Scan_NeverSubstitutesAdjacentWord_AndKeepsKanjiAtPoint()
    {
        await using var host = await StartAsync();
        using var response = await SendAsync(host.Client, "𠮟、猫", 0);
        using var json = await ReadOkAsync(response);
        var data = json.RootElement.GetProperty("data");
        Assert.Equal(JsonValueKind.Null, data.GetProperty("target_span").ValueKind);
        var analysis = data.GetProperty("analysis");
        Assert.Equal("completed", analysis.GetProperty("capabilities").GetProperty("vocabulary").GetString());
        Assert.Equal("猫", Assert.Single(analysis.GetProperty("vocabulary_occurrences").EnumerateArray())
            .GetProperty("surface").GetString());
        Assert.Equal("𠮟", Assert.Single(analysis.GetProperty("kanji").EnumerateArray()).GetProperty("character").GetString());
    }

    [Fact]
    public async Task Scan_ReportsUnavailableVocabularySeparatelyFromNoMatch()
    {
        await using var host = await StartAsync();
        using var response = await SendAsync(host.Client, "語", 0);
        using var json = await ReadOkAsync(response);
        var data = json.RootElement.GetProperty("data");
        Assert.Equal(JsonValueKind.Null, data.GetProperty("target_span").ValueKind);
        Assert.Equal("unavailable", data.GetProperty("analysis").GetProperty("capabilities")
            .GetProperty("vocabulary").GetString());
    }

    [Fact]
    public async Task Scan_RejectsInvalidRequestsBeforeDictionaryOrTokenizer()
    {
        await using var host = await StartAsync();
        var badRequests = new[]
        {
            (Body: new { text = "𠮟猫", offset = 1, interaction_id = InteractionId }, Version: "1"),
            (Body: new { text = "猫", offset = 1, interaction_id = InteractionId }, Version: "1"),
            (Body: new { text = "猫", offset = -1, interaction_id = InteractionId }, Version: "1"),
            (Body: new { text = "猫a", offset = 1, interaction_id = InteractionId }, Version: "1"),
            (Body: new { text = " 猫", offset = 1, interaction_id = InteractionId }, Version: "1"),
            (Body: new { text = "猫\uFEFF", offset = 0, interaction_id = InteractionId }, Version: "1"),
            (Body: new { text = new string('猫', 65), offset = 0, interaction_id = InteractionId }, Version: "1"),
            (Body: new { text = "猫", offset = 0, interaction_id = "{a1d3b846-c077-4b5a-8b97-1d4d583ab0ad}" }, Version: "1"),
            (Body: new { text = "猫", offset = 0, interaction_id = InteractionId }, Version: "3"),
        };
        foreach (var request in badRequests)
        {
            using var content = JsonContent.Create(request.Body);
            using var response = await SendAsync(host.Client, content, request.Version);
            await AssertInvalidAsync(response);
        }

        var prefix = $"\"interaction_id\":\"{InteractionId}\",\"text\":";
        var malformed = new[]
        {
            "{" + prefix + "\"猫\",\"offset\":0,\"context\":\"secret-context\"}",
            "{" + prefix + "\"猫\",\"offset\":0,\"offset\":0}",
            "{" + prefix + "\"猫\",\"offset\":\"0\"}",
            "{" + prefix + "\"猫\"}",
            "{" + prefix + "\"猫\\uD800\",\"offset\":0}",
        };
        foreach (var body in malformed)
        {
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await SendAsync(host.Client, content);
            await AssertInvalidAsync(response);
        }

        await host.Dictionary.DidNotReceiveWithAnyArgs().SearchByFormOrReadingAsync(default!, Cancellation);
        await host.Tokenizer.DidNotReceiveWithAnyArgs().TokenizeAsync(default!, Cancellation);
    }

    [Fact]
    public async Task Scan_BodyCapAppliesToContentLengthAndChunkedStreams()
    {
        await using var host = await StartAsync();
        var json = $"{{\"text\":\"猫\",\"offset\":0,\"interaction_id\":\"{InteractionId}\"}}";
        var exact = json + new string(' ', 4096 - Encoding.UTF8.GetByteCount(json));
        foreach (var chunked in new[] { false, true })
        {
            using var oversizedContent = new StringContent(exact + " ", Encoding.UTF8, "application/json");
            using var oversized = await SendAsync(host.Client, oversizedContent, chunked: chunked);
            await AssertInvalidAsync(oversized);
        }

        await host.Dictionary.DidNotReceiveWithAnyArgs().SearchByFormOrReadingAsync(default!, Cancellation);
        using var exactContent = new StringContent(exact, Encoding.UTF8, "application/json");
        using var response = await SendAsync(host.Client, exactContent, chunked: true);
        using var body = await ReadOkAsync(response);
        AssertSpan(body.RootElement.GetProperty("data").GetProperty("target_span"), 0, 1);
    }

    [Fact]
    public async Task Scan_RateLimitGivesRetryAfterAndDoesNotAffectAnalysis()
    {
        await using var host = await StartAsync();
        for (var i = 0; i < 60; i++)
        {
            using var permitted = await SendAsync(host.Client, "猫", 0);
            Assert.Equal(HttpStatusCode.OK, permitted.StatusCode);
        }

        using var response = await SendAsync(host.Client, "猫", 0);
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(true, response.Headers.CacheControl?.NoStore);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancellation));
        var body = json.RootElement;
        Assert.Equal("RATE_LIMITED", body.GetProperty("error_code").GetString());
        Assert.True(body.GetProperty("retryable").GetBoolean());
        var seconds = body.GetProperty("retry_after_seconds").GetInt32();
        Assert.InRange(seconds, 1, 60);
        Assert.Equal(TimeSpan.FromSeconds(seconds), response.Headers.RetryAfter?.Delta);

        using var analysis = await host.Client.PostAsJsonAsync("/api/analysis", new { text = "猫" }, Cancellation);
        Assert.Equal(HttpStatusCode.OK, analysis.StatusCode);
    }

    [Fact]
    public async Task Scan_DeadlineCancelsAnalysisAndReturnsStructuredTimeout()
    {
        await using var host = await StartAsync();
        host.Dictionary.SearchByFormOrReadingAsync("猫", Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await Task.Delay(Timeout.Infinite, call.Arg<CancellationToken>());
                return (IReadOnlyList<DictionaryEntry>)[];
            });
        using var response = await SendAsync(host.Client, "猫", 0);
        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
        Assert.Equal(true, response.Headers.CacheControl?.NoStore);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancellation));
        Assert.Equal("TIMEOUT", json.RootElement.GetProperty("error_code").GetString());
        Assert.True(json.RootElement.GetProperty("retryable").GetBoolean());
    }

    private static async Task AssertInvalidAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(true, response.Headers.CacheControl?.NoStore);
        var text = await response.Content.ReadAsStringAsync(Cancellation);
        Assert.DoesNotContain("secret-context", text);
        using var json = JsonDocument.Parse(text);
        Assert.Equal("VALIDATION_FAILED", json.RootElement.GetProperty("error_code").GetString());
        Assert.False(json.RootElement.GetProperty("retryable").GetBoolean());
    }

    private static void AssertSpan(JsonElement span, int start, int end)
    {
        Assert.Equal(start, span.GetProperty("start").GetInt32());
        Assert.Equal(end, span.GetProperty("end").GetInt32());
        Assert.Equal("utf16", span.GetProperty("unit").GetString());
    }

    private static async Task<JsonDocument> ReadOkAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(true, response.Headers.CacheControl?.NoStore);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancellation));
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, string text, int offset)
    {
        using var content = JsonContent.Create(new { text, offset, interaction_id = InteractionId });
        return await SendAsync(client, content);
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpContent content,
        string version = "1", bool chunked = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/scan") { Content = content };
        request.Headers.Add("X-Contract-Version", version);
        if (chunked) request.Headers.TransferEncodingChunked = true;
        return await client.SendAsync(request, Cancellation);
    }

    private static async Task<ScanHost> StartAsync()
    {
        var dictionary = Substitute.For<IDictionaryRepository>();
        dictionary.SearchByFormOrReadingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<DictionaryEntry>)[]);
        dictionary.SearchByFormOrReadingAsync("猫", Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<DictionaryEntry>)[Entry("cat", "猫", "ねこ", 1)]);
        dictionary.SearchByFormOrReadingAsync("日本語", Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<DictionaryEntry>)[Entry("japanese", "日本語", "にほんご", 2)]);

        var tokenizer = Substitute.For<IGrammarTokenizerPort>();
        tokenizer.TokenizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((IReadOnlyList<NormalizedTokenDto>)[]);
        tokenizer.TokenizeAsync("😀日本語、猫", Arg.Any<CancellationToken>()).Returns((IReadOnlyList<NormalizedTokenDto>)[
            new("😀", "😀", "記号", 0, 2), new("日本語", "日本語", "名詞", 2, 5),
            new("、", "、", "補助記号", 5, 6), new("猫", "猫", "名詞", 6, 7)]);
        tokenizer.TokenizeAsync("𠮟、猫", Arg.Any<CancellationToken>()).Returns((IReadOnlyList<NormalizedTokenDto>)[
            new("𠮟", "𠮟", "名詞", 0, 2), new("、", "、", "補助記号", 2, 3), new("猫", "猫", "名詞", 3, 4)]);

        var deinflector = Substitute.For<ITokenizerAdapter>();
        deinflector.DeinflectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => DeinflectionResult.Unavailable(call.Arg<string>()));
        var kanji = Substitute.For<IKanjiRepository>();
        kanji.GetByCharactersAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(call => (IReadOnlyList<KanjiRecord>)call.Arg<IReadOnlyList<string>>()
                .Select(character => new KanjiRecord { CanonicalId = "kanji:" + character, Character = character, UnicodeCodepoint = "test" }).ToArray());
        var grammar = Substitute.For<IGrammarRepository>();
        grammar.GetAllActiveRulesAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<GrammarRule>)[]);

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddApplicationServices();
        builder.Services.AddScanServices();
        builder.Services.AddSingleton(dictionary);
        builder.Services.AddSingleton(tokenizer);
        builder.Services.AddSingleton(deinflector);
        builder.Services.AddSingleton(kanji);
        builder.Services.AddSingleton(grammar);
        var app = builder.Build();
        app.UseRateLimiter();
        app.MapScanEndpoints();
        app.MapAnalysisEndpoints();
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync(Cancellation);
        var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        return new ScanHost(app, client, dictionary, tokenizer, deinflector);
    }

    private static DictionaryEntry Entry(string id, string written, string reading, long rowId) => new()
    {
        CanonicalId = "dictionary:" + id,
        WrittenForms = [new WrittenForm { Id = rowId, Form = written }],
        Readings = [new Reading { Id = rowId, ReadingText = reading }],
        Senses = [new DictionarySense { SenseKey = "1", PartOfSpeech = ["n"] }]
    };

    private sealed record ScanHost(WebApplication App, HttpClient Client, IDictionaryRepository Dictionary,
        IGrammarTokenizerPort Tokenizer, ITokenizerAdapter Deinflector) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.DisposeAsync();
        }
    }
}
