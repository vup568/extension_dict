namespace Application.Speech;

using System.Buffers;
using System.Text;
using Application.Dictionary;
using Application.Dictionary.Ports;

public interface ISpeechProvider
{
    Task<byte[]> SynthesizeJapaneseAsync(string text, CancellationToken cancellationToken);
}

public sealed class SpeechProviderException(string code, bool retryable, bool audioTooLong = false) : Exception(code)
{
    public string Code { get; } = code;
    public bool Retryable { get; } = retryable;
    public bool AudioTooLong { get; } = audioTooLong;
}

public sealed class SpeechUseCase(IDictionaryRepository dictionary, ISpeechProvider provider)
{
    public Task<byte[]> ReadSelectionAsync(string? text, CancellationToken cancellationToken)
    {
        var trimmed = text?.Trim();
        // EARS[Unwanted] YAU-04: Reject invalid selections before sending their text to Gemini.
        if (!IsValidText(trimmed, 1000)) throw new ArgumentException("Invalid speech selection.");
        return SynthesizeAsync(trimmed!, cancellationToken);
    }

    public async Task<byte[]> ReadDictionaryAsync(string? canonicalId, long readingId, long? writtenFormId,
        CancellationToken cancellationToken)
    {
        if (canonicalId is null || canonicalId.Length is <= 11 or > 255
            || !canonicalId.StartsWith("dictionary:", StringComparison.Ordinal)
            || canonicalId.Any(c => char.IsWhiteSpace(c) || char.IsControl(c))
            || readingId <= 0 || writtenFormId is <= 0)
            throw new ArgumentException("Invalid dictionary speech source.");

        var entries = await dictionary.GetByCanonicalIdsAsync([canonicalId], cancellationToken);
        var entry = entries.SingleOrDefault(e => e.CanonicalId == canonicalId);
        var reading = entry?.Readings.SingleOrDefault(r => r.Id == readingId);
        if (entry is null || reading is null) throw new ArgumentException("Invalid dictionary speech source.");

        // EARS[Unwanted] YAU-02/03: Resolve the canonical reading only if this exact pair has an applicable sense.
        var allowsPair = writtenFormId is null
            ? reading.IsNoKanji || entry.WrittenForms.Count == 0
            : entry.WrittenForms.Any(form => form.Id == writtenFormId && LookupWordUseCase.Allows(reading, form.Form));
        if (!allowsPair || !entry.Senses.Any(sense => LookupWordUseCase.Applicable(sense, (writtenFormId, readingId)))
            || !IsValidText(reading.ReadingText, 255))
            throw new ArgumentException("Invalid dictionary speech source.");

        return await SynthesizeAsync(reading.ReadingText, cancellationToken);
    }

    private async Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var wav = await provider.SynthesizeJapaneseAsync(text, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        SpeechWave.Validate(wav);
        return wav;
    }

    private static bool IsValidText(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > maxLength) return false;
        var remaining = text.AsSpan();
        while (!remaining.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(remaining, out var rune, out var consumed) != OperationStatus.Done
                || Rune.IsControl(rune) && rune.Value is not ('\t' or '\r' or '\n'))
                return false;
            remaining = remaining[consumed..];
        }
        return true;
    }
}
