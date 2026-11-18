namespace Infrastructure.Adapters;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Speech;

public sealed class GeminiSpeechProvider(HttpClient client, string? speechKey, string? translationKey,
    string? model, string? voice) : ISpeechProvider
{
    private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/interactions";
    private const int SampleRate = 24000;
    private const int MaxBase64Chars = SpeechWave.MaxBytes / 3 * 4;
    private const int MaxResponseBytes = MaxBase64Chars + 16 * 1024;
    private readonly string? apiKey = string.IsNullOrWhiteSpace(speechKey) ? translationKey : speechKey;

    public async Task<byte[]> SynthesizeJapaneseAsync(string text, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length > 512 || apiKey.Any(char.IsControl)
            || model is not ("gemini-3.8-flash-lite-tts" or "gemini-3.8-flash-tts")
            || voice is not ("Kore" or "Aoede"))
            throw new SpeechProviderException("SERVICE_UNAVAILABLE", false);

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(25));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            request.Headers.TryAddWithoutValidation("x-goog-api-key", apiKey);
            // EARS[Unwanted] YAU-04/06/09: Send only the transcript, with stateless unary generation and fixed audio settings.
            request.Content = JsonContent.Create(new
            {
                model,
                store = false,
                input = new[] { new { type = "user_input", content = new[] { new { type = "text", text } } } },
                response_format = new { type = "audio", mime_type = "audio/wav", sample_rate = SampleRate },
                generation_config = new { speech_config = new[] { new { voice } } }
            });
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (response.StatusCode == HttpStatusCode.TooManyRequests) throw new SpeechProviderException("RATE_LIMITED", true);
            if (response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout)
                throw new SpeechProviderException("TIMEOUT", true);
            if (!response.IsSuccessStatusCode) throw Unavailable((int)response.StatusCode >= 500);
            if (response.Content.Headers.ContentLength > MaxResponseBytes) throw AudioTooLong();

            await using var input = await response.Content.ReadAsStreamAsync(deadline.Token);
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];
            int read;
            while ((read = await input.ReadAsync(chunk, deadline.Token)) > 0)
            {
                if (buffer.Length + read > MaxResponseBytes) throw AudioTooLong();
                buffer.Write(chunk, 0, read);
            }
            buffer.Position = 0;
            using var json = await JsonDocument.ParseAsync(buffer, cancellationToken: deadline.Token);
            return ReadAudio(json.RootElement);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SpeechProviderException("TIMEOUT", true);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException or FormatException
            or InvalidOperationException or KeyNotFoundException)
        {
            // Provider errors can embed the selected text. Keep all upstream messages out of the public response.
            throw Unavailable();
        }
    }

    private static byte[] ReadAudio(JsonElement interaction)
    {
        if (interaction.GetProperty("status").GetString() != "completed") throw Unavailable(false);

        JsonElement? audio = null;
        foreach (var step in interaction.GetProperty("steps").EnumerateArray())
        {
            if (step.GetProperty("type").GetString() != "model_output") continue;
            foreach (var part in step.GetProperty("content").EnumerateArray())
            {
                if (part.GetProperty("type").GetString() != "audio") continue;
                if (audio.HasValue) throw Unavailable();
                audio = part;
            }
        }
        if (audio is not { } output) throw Unavailable();
        var data = output.GetProperty("data").GetString();
        if (string.IsNullOrEmpty(data)) throw Unavailable();
        if (data.Length > MaxBase64Chars) throw AudioTooLong();

        var rate = output.TryGetProperty("sample_rate", out var sampleRate) ? sampleRate.GetInt32() : (int?)null;
        var channels = output.TryGetProperty("channels", out var channelCount) ? channelCount.GetInt32() : (int?)null;
        if ((rate is not null && rate != SampleRate) || (channels is not null && channels != 1)) throw Unavailable();
        var bytes = Convert.FromBase64String(data);
        return output.GetProperty("mime_type").GetString() switch
        {
            "audio/wav" when SpeechWave.Validate(bytes) == SampleRate => bytes,
            "audio/l16" when rate == SampleRate && channels == 1 && !bytes.AsSpan().StartsWith("RIFF"u8) =>
                SpeechWave.WrapPcm16Mono(bytes, SampleRate),
            _ => throw Unavailable()
        };
    }

    private static SpeechProviderException Unavailable(bool retryable = true) => new("SERVICE_UNAVAILABLE", retryable);
    private static SpeechProviderException AudioTooLong() => new("SERVICE_UNAVAILABLE", false, audioTooLong: true);
}
