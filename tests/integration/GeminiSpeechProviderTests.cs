namespace IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Speech;
using Infrastructure.Adapters;
using Xunit;

public sealed class GeminiSpeechProviderTests
{
    private const string Model = "gemini-3.8-flash-lite-tts";
    private static readonly byte[] Pcm = [0x01, 0, 0xff, 0xff];
    private static readonly byte[] Wav = SpeechWave.WrapPcm16Mono(Pcm, 24000);

    [Theory]
    [InlineData("audio/wav")]
    [InlineData("audio/l16")]
    public async Task Synthesize_UsesStatelessFixedEndpoint_AndReturnsValidatedWav(string mime)
    {
        using var handler = new CaptureHandler((_, _) => Task.FromResult(
            AudioResponse(mime, Convert.ToBase64String(mime == "audio/wav" ? Wav : Pcm))));
        using var client = new HttpClient(handler);
        var provider = new GeminiSpeechProvider(client, null, "translation-test-key", Model, "Kore");

        var result = await provider.SynthesizeJapaneseAsync("電気代を払います。", TestContext.Current.CancellationToken);

        Assert.Equal(Wav, result);
        Assert.Equal("https://generativelanguage.googleapis.com/v1beta/interactions", handler.Url);
        Assert.Equal("translation-test-key", handler.Key);
        using var request = JsonDocument.Parse(handler.Body!);
        var json = request.RootElement;
        Assert.False(json.GetProperty("store").GetBoolean());
        Assert.Equal(Model, json.GetProperty("model").GetString());
        Assert.Equal("電気代を払います。", json.GetProperty("input")[0].GetProperty("content")[0].GetProperty("text").GetString());
        var responseFormat = json.GetProperty("response_format");
        Assert.Equal("audio/wav", responseFormat.GetProperty("mime_type").GetString());
        Assert.Equal(24000, responseFormat.GetProperty("sample_rate").GetInt32());
        Assert.False(responseFormat.TryGetProperty("delivery", out _));
        Assert.Equal("Kore", json.GetProperty("generation_config").GetProperty("speech_config")[0].GetProperty("voice").GetString());
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData(null, null, Model, "Kore")]
    [InlineData("speech-key", "translation-key", null, "Kore")]
    [InlineData("speech-key", "translation-key", "https://invalid.example", "Kore")]
    [InlineData("speech-key", null, Model, "unverified-voice")]
    [InlineData("bad\nkey", "translation-key", Model, "Kore")]
    public async Task InvalidConfigurationNeverCallsProvider(string? key, string? fallback, string? model, string? voice)
    {
        using var handler = new CaptureHandler((_, _) => throw new InvalidOperationException("Unexpected outbound request."));
        using var client = new HttpClient(handler);
        var provider = new GeminiSpeechProvider(client, key, fallback, model, voice);

        var error = await Assert.ThrowsAsync<SpeechProviderException>(() =>
            provider.SynthesizeJapaneseAsync("ねこ", TestContext.Current.CancellationToken));

        Assert.Equal("SERVICE_UNAVAILABLE", error.Code);
        Assert.False(error.Retryable);
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData(429, "RATE_LIMITED", true)]
    [InlineData(504, "TIMEOUT", true)]
    [InlineData(403, "SERVICE_UNAVAILABLE", false)]
    [InlineData(302, "SERVICE_UNAVAILABLE", false)]
    [InlineData(500, "SERVICE_UNAVAILABLE", true)]
    public async Task ProviderErrorsAreSanitizedAndNeverRetried(int status, string code, bool retryable)
    {
        using var handler = new CaptureHandler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
        {
            Content = new StringContent("Private transcript from the provider")
        }));
        using var client = new HttpClient(handler);
        var provider = new GeminiSpeechProvider(client, "speech-test-key", "translation-test-key", Model, "Kore");

        var error = await Assert.ThrowsAsync<SpeechProviderException>(() =>
            provider.SynthesizeJapaneseAsync("ねこ", TestContext.Current.CancellationToken));

        Assert.Equal(code, error.Message);
        Assert.Equal(retryable, error.Retryable);
        Assert.Equal("speech-test-key", handler.Key);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task MalformedAndOversizedAudioNeverPasses()
    {
        var corruptedWav = Wav.ToArray();
        corruptedWav[22] = 2; // Stereo.
        foreach (var (mime, data, rate, channels, tooLong) in new[]
        {
            ("audio/wav", "!bad-base64", 24000, 1, false),
            ("audio/wav", Convert.ToBase64String(Pcm), 24000, 1, false),
            ("audio/wav", Convert.ToBase64String(corruptedWav), 24000, 1, false),
            ("audio/wav", Convert.ToBase64String(Wav), 16000, 1, false),
            ("audio/l16", Convert.ToBase64String(Wav), 24000, 1, false),
            ("audio/l16", Convert.ToBase64String(new byte[24000 * 2 * 60 + 2]), 24000, 1, true),
            ("audio/l16", Convert.ToBase64String(new byte[SpeechWave.MaxBytes + 2]), 24000, 1, true)
        })
        {
            using var handler = new CaptureHandler((_, _) => Task.FromResult(AudioResponse(mime, data, rate, channels)));
            using var client = new HttpClient(handler);
            var provider = new GeminiSpeechProvider(client, "speech-test-key", null, Model, "Kore");
            var error = await Assert.ThrowsAsync<SpeechProviderException>(() =>
                provider.SynthesizeJapaneseAsync("ねこ", TestContext.Current.CancellationToken));
            Assert.Equal("SERVICE_UNAVAILABLE", error.Code);
            Assert.Equal(tooLong, error.AudioTooLong);
        }
    }

    [Fact]
    public async Task TransportCancellationDistinguishesTimeoutFromCallerAbort()
    {
        using var timedOut = new CaptureHandler((_, _) => throw new OperationCanceledException());
        using var client = new HttpClient(timedOut);
        var provider = new GeminiSpeechProvider(client, "speech-test-key", null, Model, "Kore");
        var timeout = await Assert.ThrowsAsync<SpeechProviderException>(() =>
            provider.SynthesizeJapaneseAsync("ねこ", TestContext.Current.CancellationToken));
        Assert.Equal("TIMEOUT", timeout.Code);

        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var cancelled = new CaptureHandler((_, token) => { caller.Cancel(); token.ThrowIfCancellationRequested(); return Task.FromResult(AudioResponse("audio/wav", "")); });
        using var cancelledClient = new HttpClient(cancelled);
        var cancelledProvider = new GeminiSpeechProvider(cancelledClient, "speech-test-key", null, Model, "Kore");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledProvider.SynthesizeJapaneseAsync("ねこ", caller.Token));
    }

    private static HttpResponseMessage AudioResponse(string mime, string data, int rate = 24000, int channels = 1) =>
        new(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                status = "completed",
                steps = new[] { new { type = "model_output", content = new[] { new { type = "audio", mime_type = mime, data, sample_rate = rate, channels } } } }
            })
        };

    private sealed class CaptureHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? Url { get; private set; }
        public string? Key { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Url = request.RequestUri?.AbsoluteUri;
            Key = request.Headers.GetValues("x-goog-api-key").Single();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return await respond(request, cancellationToken);
        }
    }
}
