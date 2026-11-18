namespace IntegrationTests;

using System.Net;
using System.Text;
using System.Text.Json;
using Application.Translation;
using Infrastructure.Adapters;
using Xunit;

public sealed class GeminiTranslationProviderTests
{
    [Fact]
    public async Task SendsSelectedTextInBodyAndReadsTranslation()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, "{\"candidates\":[{\"finishReason\":\"STOP\",\"content\":{\"parts\":[{\"text\":\"I study Japanese.\"}]}}]}");
        var result = await new GeminiTranslationProvider(new HttpClient(handler), "test-key").TranslateJapaneseToEnglishAsync("日本語を勉強します。", TestContext.Current.CancellationToken);
        Assert.Equal("I study Japanese.", result);
        Assert.Equal("generativelanguage.googleapis.com", handler.Host);
        Assert.DoesNotContain("test-key", handler.Uri!);
        Assert.Equal("test-key", handler.Key);
        using var json = JsonDocument.Parse(handler.Body);
        Assert.Equal("日本語を勉強します。", json.RootElement.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString());
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, "{}", "RATE_LIMITED")]
    [InlineData(HttpStatusCode.OK, "{}", "SERVICE_UNAVAILABLE")]
    [InlineData(HttpStatusCode.OK, "{\"candidates\":[{\"finishReason\":\"MAX_TOKENS\"}]}", "SERVICE_UNAVAILABLE")]
    public async Task NormalizesFailures(HttpStatusCode status, string body, string code)
    {
        var handler = new FakeHandler(status, body);
        var ex = await Assert.ThrowsAsync<TranslationProviderException>(() => new GeminiTranslationProvider(new HttpClient(handler), "test-key").TranslateJapaneseToEnglishAsync("猫", TestContext.Current.CancellationToken));
        Assert.Equal(code, ex.Code);
    }

    [Fact]
    public async Task MissingKeyDoesNotCallProvider()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, "{}");
        await Assert.ThrowsAsync<TranslationProviderException>(() => new GeminiTranslationProvider(new HttpClient(handler), null).TranslateJapaneseToEnglishAsync("猫", TestContext.Current.CancellationToken));
        Assert.Equal(0, handler.Calls);
    }

    private sealed class FakeHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? Host { get; private set; }
        public string? Uri { get; private set; }
        public string? Key { get; private set; }
        public string Body { get; private set; } = "";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Host = request.RequestUri?.Host;
            Uri = request.RequestUri?.ToString();
            Key = request.Headers.GetValues("x-goog-api-key").Single();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
