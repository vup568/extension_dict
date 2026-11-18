namespace Application.Translation;

public interface ITranslationProvider
{
    Task<string> TranslateJapaneseToEnglishAsync(string text, CancellationToken cancellationToken);
}

public sealed class TranslationProviderException(string code, bool retryable) : Exception(code)
{
    public string Code { get; } = code;
    public bool Retryable { get; } = retryable;
}

public sealed class TranslationUseCase(ITranslationProvider provider)
{
    public const int MaxTextLength = 1000;
    public const int MaxTranslatedLength = 8000;

    public async Task<string> ExecuteAsync(string? text, string? targetLanguage, string? interactionId,
        CancellationToken cancellationToken = default)
    {
        // EARS[Unwanted]: Never invoke a provider for invalid text, target or interaction.
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length > MaxTextLength || !WellFormed(text)
            || targetLanguage != "en" || !Guid.TryParse(interactionId, out _))
            throw new ArgumentException("Yêu cầu dịch không hợp lệ.");

        var result = await provider.TranslateJapaneseToEnglishAsync(text.Trim(), cancellationToken);
        if (string.IsNullOrWhiteSpace(result) || result.Length > MaxTranslatedLength || !WellFormed(result))
            throw new TranslationProviderException("SERVICE_UNAVAILABLE", true);
        return result;
    }

    private static bool WellFormed(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsHighSurrogate(value[i]))
            {
                if (++i >= value.Length || !char.IsLowSurrogate(value[i])) return false;
            }
            else if (char.IsLowSurrogate(value[i])) return false;
            else if (char.IsControl(value[i]) && value[i] is not ('\r' or '\n' or '\t')) return false;
        }
        return true;
    }
}
