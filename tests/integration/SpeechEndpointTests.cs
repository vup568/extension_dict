namespace IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Application.Dictionary.Ports;
using Application.Speech;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using WebApi.Endpoints;
using Xunit;

public sealed class SpeechEndpointTests
{
    private const string InteractionId = "a1d3b846-c077-4b5a-8b97-1d4d583ab0ad";
    private static readonly byte[] Wav = SpeechWave.WrapPcm16Mono([1, 0, 255, 255], 24000);

    [Fact]
    public async Task SelectionReturnsNoStoreCorrelatedWav_AndPreservesTranscript()
    {
        var ct = TestContext.Current.CancellationToken;
        var dictionary = Substitute.For<IDictionaryRepository>();
        var provider = Substitute.For<ISpeechProvider>();
        provider.SynthesizeJapaneseAsync("電気代とガス代に国がお金を出す", Arg.Any<CancellationToken>()).Returns(Wav);
        await using var app = await StartAsync(dictionary, provider, ct);
        using var client = CreateClient(app);

        using var response = await client.PostAsJsonAsync("/api/speech", new
        {
            interaction_id = InteractionId,
            source = new { text = " 電気代とガス代に国がお金を出す ", kind = "selection" }
        }, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var data = json.RootElement.GetProperty("data");
        Assert.Equal(Wav, data.GetProperty("audio_base64").GetBytesFromBase64());
        Assert.Equal("audio/wav", data.GetProperty("mime_type").GetString());
        Assert.Equal("ja", data.GetProperty("language_tag").GetString());
        Assert.Equal("ai", data.GetProperty("source").GetString());
        var meta = json.RootElement.GetProperty("meta");
        Assert.Equal(InteractionId, meta.GetProperty("interaction_id").GetString());
        Assert.Equal("1", meta.GetProperty("contract_version").GetString());
        Assert.True(Guid.TryParseExact(meta.GetProperty("request_id").GetString(), "D", out _));
        await dictionary.DidNotReceiveWithAnyArgs().GetByCanonicalIdsAsync(default!, ct);
        await provider.Received(1).SynthesizeJapaneseAsync("電気代とガス代に国がお金を出す", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DictionaryResolvesExactJointPairBeforeSendingOnlyKana()
    {
        var ct = TestContext.Current.CancellationToken;
        var dictionary = Substitute.For<IDictionaryRepository>();
        var provider = Substitute.For<ISpeechProvider>();
        provider.SynthesizeJapaneseAsync("はし", Arg.Any<CancellationToken>()).Returns(Wav);
        var bridge = new DictionaryEntry
        {
            CanonicalId = "dictionary:bridge",
            WrittenForms = [new() { Id = 11, Form = "橋" }, new() { Id = 12, Form = "旧橋" }],
            Readings = [new() { Id = 21, ReadingText = "はし", RestrictedToForms = ["橋"] }, new() { Id = 22, ReadingText = "はし" }],
            Senses = [new() { Applicabilities = [new() { WrittenFormId = 11, ReadingId = 21 }] }]
        };
        var chopsticks = new DictionaryEntry
        {
            CanonicalId = "dictionary:chopsticks",
            WrittenForms = [new() { Id = 13, Form = "箸" }],
            Readings = [new() { Id = 23, ReadingText = "はし" }],
            Senses = [new()]
        };
        var kanaOnly = new DictionaryEntry
        {
            CanonicalId = "dictionary:kana",
            WrittenForms = [new() { Id = 14, Form = "御茶" }],
            Readings = [new() { Id = 24, ReadingText = "おちゃ", IsNoKanji = true }],
            Senses = [new()]
        };
        provider.SynthesizeJapaneseAsync("おちゃ", Arg.Any<CancellationToken>()).Returns(Wav);
        dictionary.GetByCanonicalIdsAsync(Arg.Is<IEnumerable<string>>(ids => ids.Single() == bridge.CanonicalId),
            Arg.Any<CancellationToken>()).Returns(new[] { bridge });
        dictionary.GetByCanonicalIdsAsync(Arg.Is<IEnumerable<string>>(ids => ids.Single() == chopsticks.CanonicalId),
            Arg.Any<CancellationToken>()).Returns(new[] { chopsticks });
        dictionary.GetByCanonicalIdsAsync(Arg.Is<IEnumerable<string>>(ids => ids.Single() == kanaOnly.CanonicalId),
            Arg.Any<CancellationToken>()).Returns(new[] { kanaOnly });
        await using var app = await StartAsync(dictionary, provider, ct);
        using var client = CreateClient(app);

        foreach (var (entry, reading, written, expected) in new[]
        {
            (bridge, 21, (int?)11, HttpStatusCode.OK),
            (bridge, 21, (int?)12, HttpStatusCode.BadRequest), // re_restr.
            (bridge, 22, (int?)11, HttpStatusCode.BadRequest), // Sense applicability.
            (bridge, 21, null, HttpStatusCode.BadRequest), // Omitted form is only allowed for a kana-only reading.
            (bridge, 23, (int?)11, HttpStatusCode.BadRequest), // Homophone reading belongs to a different entry.
            (chopsticks, 23, (int?)13, HttpStatusCode.OK),
            (kanaOnly, 24, null, HttpStatusCode.OK),
            (kanaOnly, 24, (int?)14, HttpStatusCode.BadRequest)
        })
        {
            using var response = await client.PostAsJsonAsync("/api/speech", new
            {
                interaction_id = InteractionId,
                source = new { kind = "dictionary", canonical_id = entry.CanonicalId, reading_id = reading, written_form_id = written }
            }, ct);
            Assert.Equal(expected, response.StatusCode);
        }
        await provider.Received(2).SynthesizeJapaneseAsync("はし", Arg.Any<CancellationToken>());
        await provider.Received(1).SynthesizeJapaneseAsync("おちゃ", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvalidUnionVersionUtf16AndOversizedBodiesNeverCallProvider()
    {
        var ct = TestContext.Current.CancellationToken;
        var provider = Substitute.For<ISpeechProvider>();
        await using var app = await StartAsync(Substitute.For<IDictionaryRepository>(), provider, ct);
        using var client = CreateClient(app);

        foreach (var json in new[]
        {
            Envelope("""{"kind":"selection","text":"猫","reading_id":null}"""),
            Envelope("""{"text":"猫"}"""),
            Envelope("""{"kind":"selection","text":"\uD800"}"""),
            Envelope("""{"kind":"selection","text":"猫","text":"犬"}"""),
            Envelope("""{"kind":"selection","text":"猫","model":"custom"}"""),
            Envelope("""{"kind":"selection","text":"猫\u0000"}"""),
            Envelope("""{"kind":"selection","text":""}"""),
            Envelope(JsonSerializer.Serialize(new { kind = "selection", text = new string('あ', 1001) })),
            Envelope("""{"kind":"unknown","text":"猫"}"""),
            Envelope("""{"kind":"dictionary","canonical_id":"dictionary:cat","reading_id":1,"text":null}"""),
            Envelope("""{"kind":"selection","text":"猫"}""", $" {InteractionId} ")
        })
        {
            using var response = await client.PostAsync("/api/speech", new StringContent(json, Encoding.UTF8, "application/json"), ct);
            await AssertInvalidAsync(response, ct);
        }
        var oversized = Envelope("""{"kind":"selection","text":"猫"}""") + new string(' ', 16 * 1024);
        using var withLength = await client.PostAsync("/api/speech", new StringContent(oversized, Encoding.UTF8, "application/json"), ct);
        await AssertInvalidAsync(withLength, ct);
        using var chunked = await client.PostAsync("/api/speech", new ChunkedJson(oversized), ct);
        await AssertInvalidAsync(chunked, ct);

        client.DefaultRequestHeaders.Remove("X-Contract-Version");
        using var noVersion = await client.PostAsync("/api/speech",
            new StringContent(Envelope("""{"kind":"selection","text":"猫"}"""), Encoding.UTF8, "application/json"), ct);
        await AssertInvalidAsync(noVersion, ct);
        await provider.DidNotReceiveWithAnyArgs().SynthesizeJapaneseAsync(default!, ct);
    }

    [Fact]
    public async Task FixedWindowQuotaRejectsEleventhCallBeforeProvider()
    {
        var ct = TestContext.Current.CancellationToken;
        var provider = Substitute.For<ISpeechProvider>();
        provider.SynthesizeJapaneseAsync("猫", Arg.Any<CancellationToken>()).Returns(Wav);
        await using var app = await StartAsync(Substitute.For<IDictionaryRepository>(), provider, ct);
        using var client = CreateClient(app);
        for (var i = 0; i < 10; i++)
        {
            using var success = await SelectCatAsync(client, ct);
            Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        }
        using var over = await SelectCatAsync(client, ct);
        Assert.Equal(HttpStatusCode.TooManyRequests, over.StatusCode);
        Assert.True(over.Headers.RetryAfter?.Delta > TimeSpan.Zero);
        await provider.Received(10).SynthesizeJapaneseAsync("猫", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConcurrentQuotaRejectsThirdCallBeforeProvider()
    {
        var ct = TestContext.Current.CancellationToken;
        var bothStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var provider = Substitute.For<ISpeechProvider>();
        provider.SynthesizeJapaneseAsync("猫", Arg.Any<CancellationToken>()).Returns(async call =>
        {
            if (Interlocked.Increment(ref calls) == 2) bothStarted.TrySetResult();
            await release.Task.WaitAsync(call.Arg<CancellationToken>());
            return Wav;
        });
        await using var app = await StartAsync(Substitute.For<IDictionaryRepository>(), provider, ct);
        using var client = CreateClient(app);
        var first = SelectCatAsync(client, ct);
        var second = SelectCatAsync(client, ct);
        try
        {
            await bothStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
            using var over = await SelectCatAsync(client, ct);
            Assert.Equal(HttpStatusCode.TooManyRequests, over.StatusCode);
            Assert.Equal(2, Volatile.Read(ref calls));
        }
        finally { release.TrySetResult(); }
        using var a = await first;
        using var b = await second;
        Assert.Equal(HttpStatusCode.OK, a.StatusCode);
        Assert.Equal(HttpStatusCode.OK, b.StatusCode);
    }

    [Theory]
    [InlineData("RATE_LIMITED", 429, true)]
    [InlineData("TIMEOUT", 504, true)]
    [InlineData("SERVICE_UNAVAILABLE", 503, false)]
    public async Task ProviderFailuresKeepThePublicErrorCodeAndNoStore(string code, int status, bool retryable)
    {
        var ct = TestContext.Current.CancellationToken;
        var provider = Substitute.For<ISpeechProvider>();
        provider.SynthesizeJapaneseAsync("猫", Arg.Any<CancellationToken>()).Returns(
            Task.FromException<byte[]>(new SpeechProviderException(code, retryable)));
        await using var app = await StartAsync(Substitute.For<IDictionaryRepository>(), provider, ct);
        using var client = CreateClient(app);

        using var response = await SelectCatAsync(client, ct);

        Assert.Equal(status, (int)response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        Assert.Equal(code, json.RootElement.GetProperty("error_code").GetString());
        Assert.Equal(retryable, json.RootElement.GetProperty("retryable").GetBoolean());
        await provider.Received(1).SynthesizeJapaneseAsync("猫", Arg.Any<CancellationToken>());
    }

    private static Task<HttpResponseMessage> SelectCatAsync(HttpClient client, CancellationToken ct) =>
        client.PostAsJsonAsync("/api/speech", new { interaction_id = InteractionId, source = new { kind = "selection", text = "猫" } }, ct);

    private static string Envelope(string source, string id = InteractionId) =>
        $$"""{"interaction_id":"{{id}}","source":{{source}}}""";

    private static async Task AssertInvalidAsync(HttpResponseMessage response, CancellationToken ct)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        using var error = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        Assert.Equal("VALIDATION_FAILED", error.RootElement.GetProperty("error_code").GetString());
    }

    private static async Task<WebApplication> StartAsync(IDictionaryRepository dictionary, ISpeechProvider provider, CancellationToken ct)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(dictionary);
        builder.Services.AddSingleton(provider);
        builder.Services.AddSingleton<SpeechQuota>();
        builder.Services.AddScoped<SpeechUseCase>();
        var app = builder.Build();
        app.MapSpeechEndpoints();
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync(ct);
        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        client.DefaultRequestHeaders.Add("X-Contract-Version", "1");
        return client;
    }

    private sealed class ChunkedJson : HttpContent
    {
        private readonly byte[] bytes;

        public ChunkedJson(string json)
        {
            bytes = Encoding.UTF8.GetBytes(json);
            Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            stream.WriteAsync(bytes).AsTask();
    }
}
