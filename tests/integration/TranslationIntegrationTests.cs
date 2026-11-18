namespace IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Application.Translation;
using Infrastructure.Adapters;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Endpoints;
using Xunit;

public sealed class TranslationIntegrationTests
{
    [Theory]
    [InlineData(null, "test-key:fx", "api-free.deepl.com")]
    [InlineData(null, "test-key", "api.deepl.com")]
    [InlineData("Free", "test-key", "api-free.deepl.com")]
    [InlineData("Pro", "test-key:fx", "api.deepl.com")]
    public async Task Adapter_UsesOfficialEndpoint_AndOnlySelectedText(string? plan, string apiKey, string host)
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent("{\"translations\":[{\"text\":\"I study Japanese.\"}]}", Encoding.UTF8, "application/json") });
        var provider = new DeepLTranslationProvider(new HttpClient(handler), apiKey, plan);
        var result = await provider.TranslateJapaneseToEnglishAsync("日本語を勉強しています。", TestContext.Current.CancellationToken);
        Assert.Equal("I study Japanese.", result);
        Assert.Equal(host, handler.Host);
        Assert.Equal("/v2/translate", handler.Path);
        Assert.Equal($"DeepL-Auth-Key {apiKey}", handler.Authorization);
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("日本語を勉強しています。", body.RootElement.GetProperty("text")[0].GetString());
        Assert.Equal("JA", body.RootElement.GetProperty("source_lang").GetString());
        Assert.Equal("EN", body.RootElement.GetProperty("target_lang").GetString());
        Assert.Equal(3, body.RootElement.EnumerateObject().Count());
    }

    [Fact]
    public async Task Adapter_MissingOrInvalidConfigurationNeverMakesNetworkRequest()
    {
        var handler = new CapturingHandler(_ => throw new InvalidOperationException("must not send"));
        foreach (var (key, plan) in new[] { ((string?)null, "Free"), ("key", "https://bad.example"), ("bad\nkey", "Pro") })
        {
            var ex = await Assert.ThrowsAsync<TranslationProviderException>(() =>
                new DeepLTranslationProvider(new HttpClient(handler, false), key, plan).TranslateJapaneseToEnglishAsync("猫", TestContext.Current.CancellationToken));
            Assert.Equal("SERVICE_UNAVAILABLE", ex.Code);
            Assert.False(ex.Retryable);
        }
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, "RATE_LIMITED")]
    [InlineData((HttpStatusCode)529, "RATE_LIMITED")]
    [InlineData(HttpStatusCode.Unauthorized, "SERVICE_UNAVAILABLE")]
    [InlineData(HttpStatusCode.Found, "SERVICE_UNAVAILABLE")]
    [InlineData(HttpStatusCode.InternalServerError, "SERVICE_UNAVAILABLE")]
    public async Task Adapter_NormalizesProviderFailures_WithoutExposingResponse(HttpStatusCode status, string expected)
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(status) { Content = new StringContent("sensitive provider error") });
        var ex = await Assert.ThrowsAsync<TranslationProviderException>(() =>
            new DeepLTranslationProvider(new HttpClient(handler), "key", "Free").TranslateJapaneseToEnglishAsync("猫", TestContext.Current.CancellationToken));
        Assert.Equal(expected, ex.Code);
        Assert.DoesNotContain("sensitive", ex.Message);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Adapter_RejectsMalformedAndOversizedResponses()
    {
        foreach (var body in new[] { "{}", "{not json", new string('a', 65537) })
        {
            var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
            var ex = await Assert.ThrowsAsync<TranslationProviderException>(() =>
                new DeepLTranslationProvider(new HttpClient(handler), "key", null).TranslateJapaneseToEnglishAsync("猫", TestContext.Current.CancellationToken));
            Assert.Equal("SERVICE_UNAVAILABLE", ex.Code);
        }
    }

    [Fact]
    public async Task Adapter_MapsTimeoutButPropagatesCallerCancellation()
    {
        var timedOut = new CapturingHandler(_ => throw new OperationCanceledException());
        var timeout = await Assert.ThrowsAsync<TranslationProviderException>(() =>
            new DeepLTranslationProvider(new HttpClient(timedOut), "key", null)
                .TranslateJapaneseToEnglishAsync("猫", TestContext.Current.CancellationToken));
        Assert.Equal("TIMEOUT", timeout.Code);

        using var cancellation = new CancellationTokenSource();
        var cancelled = new CapturingHandler(_ => { cancellation.Cancel(); throw new OperationCanceledException(cancellation.Token); });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new DeepLTranslationProvider(new HttpClient(cancelled), "key", null)
                .TranslateJapaneseToEnglishAsync("猫", cancellation.Token));
    }

    [Fact]
    public async Task Endpoint_ValidatesAndReturnsProviderNeutralEnvelope_WithoutExposingSecrets()
    {
        var provider = new FakeProvider();
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<ITranslationProvider>(provider);
        builder.Services.AddScoped<TranslationUseCase>();
        await using var app = builder.Build();
        app.MapTranslationEndpoints();
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        app.Urls.Add($"http://127.0.0.1:{port}");
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
        var id = Guid.NewGuid().ToString("D");
        using var success = await client.PostAsJsonAsync("/api/translations", new { text = "猫です。", target_language = "en", interaction_id = id }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        using var json = JsonDocument.Parse(await success.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("It is a cat.", json.RootElement.GetProperty("data").GetProperty("translated_text").GetString());
        Assert.Equal("ja", json.RootElement.GetProperty("data").GetProperty("source_language").GetString());
        Assert.Equal("en", json.RootElement.GetProperty("data").GetProperty("target_language").GetString());
        Assert.Equal(id, json.RootElement.GetProperty("meta").GetProperty("interaction_id").GetString());
        Assert.True(Guid.TryParse(json.RootElement.GetProperty("meta").GetProperty("request_id").GetString(), out _));
        using var invalid = await client.PostAsJsonAsync("/api/translations", new { text = "猫です。", target_language = "vi", interaction_id = id }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(1, provider.Calls);
        provider.Fail = true;
        using var failure = await client.PostAsJsonAsync("/api/translations", new { text = "private synthetic", target_language = "en", interaction_id = id }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failure.StatusCode);
        var error = await failure.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("private synthetic", error);
        Assert.DoesNotContain("test-key", error);
        using var failureJson = JsonDocument.Parse(error);
        Assert.Equal("SERVICE_UNAVAILABLE", failureJson.RootElement.GetProperty("error_code").GetString());
    }

    private sealed class FakeProvider : ITranslationProvider
    {
        public int Calls { get; private set; }
        public bool Fail { get; set; }
        public Task<string> TranslateJapaneseToEnglishAsync(string text, CancellationToken cancellationToken)
        {
            Calls++;
            if (Fail) throw new TranslationProviderException("SERVICE_UNAVAILABLE", true);
            return Task.FromResult("It is a cat.");
        }
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? Host { get; private set; }
        public string? Path { get; private set; }
        public string? Authorization { get; private set; }
        public string? Body { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Host = request.RequestUri?.Host;
            Path = request.RequestUri?.AbsolutePath;
            Authorization = request.Headers.Authorization?.ToString();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return response(request);
        }
    }
}
