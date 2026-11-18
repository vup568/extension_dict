namespace Infrastructure.Adapters;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Translation;

/// <summary>Official DeepL API adapter; never sends credentials or text in a URL or logs.</summary>
public sealed class DeepLTranslationProvider(HttpClient client, string? apiKey, string? plan) : ITranslationProvider
{
    private const int MaxResponseBytes = 64 * 1024;
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

    public async Task<string> TranslateJapaneseToEnglishAsync(string text, CancellationToken cancellationToken)
    {
        // EARS[Unwanted]: Do not call a provider without explicit, valid operator configuration.
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length > 512 || apiKey.Any(char.IsControl)
            || plan is not (null or "" or "Free" or "Pro"))
            throw new TranslationProviderException("SERVICE_UNAVAILABLE", false);

        var useFree = plan == "Free" || (plan is null or "" && apiKey.EndsWith(":fx", StringComparison.Ordinal));
        var endpoint = useFree ? "https://api-free.deepl.com/v2/translate" : "https://api.deepl.com/v2/translate";
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Deadline);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.TryAddWithoutValidation("Authorization", $"DeepL-Auth-Key {apiKey}");
            request.Content = JsonContent.Create(new { text = new[] { text }, source_lang = "JA", target_lang = "EN" });
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode == 529)
                throw new TranslationProviderException("RATE_LIMITED", true);
            if (!response.IsSuccessStatusCode)
                throw new TranslationProviderException("SERVICE_UNAVAILABLE", (int)response.StatusCode >= 500);
            if (response.Content.Headers.ContentLength > MaxResponseBytes)
                throw new TranslationProviderException("SERVICE_UNAVAILABLE", true);
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var buffer = new MemoryStream();
            var chunk = new byte[4096];
            int read;
            while ((read = await stream.ReadAsync(chunk, timeout.Token)) > 0)
            {
                if (buffer.Length + read > MaxResponseBytes) throw new TranslationProviderException("SERVICE_UNAVAILABLE", true);
                buffer.Write(chunk, 0, read);
            }
            buffer.Position = 0;
            using var json = await JsonDocument.ParseAsync(buffer, cancellationToken: timeout.Token);
            if (!json.RootElement.TryGetProperty("translations", out var translations)
                || translations.ValueKind != JsonValueKind.Array || translations.GetArrayLength() != 1
                || !translations[0].TryGetProperty("text", out var translated)
                || translated.ValueKind != JsonValueKind.String)
                throw new TranslationProviderException("SERVICE_UNAVAILABLE", true);
            return translated.GetString() ?? throw new TranslationProviderException("SERVICE_UNAVAILABLE", true);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TranslationProviderException("TIMEOUT", true);
        }
        catch (OperationCanceledException) { throw; }
        catch (TranslationProviderException) { throw; }
        catch (Exception) { throw new TranslationProviderException("SERVICE_UNAVAILABLE", true); }
    }
}
