namespace Application.Grammar;

using Application.Grammar.Helpers;
using Application.Grammar.Models;
using Application.Grammar.Ports;

public class DetectGrammarUseCase
{
    private readonly IGrammarTokenizerPort _tokenizerPort;
    private readonly IGrammarRepository _grammarRepository;
    private readonly GrammarSequenceMatcher _matcher;

    public DetectGrammarUseCase(
        IGrammarTokenizerPort tokenizerPort,
        IGrammarRepository grammarRepository,
        GrammarSequenceMatcher matcher)
    {
        _tokenizerPort = tokenizerPort;
        _grammarRepository = grammarRepository;
        _matcher = matcher;
    }

    public async Task<GrammarDetectionResultDto> ExecuteAsync(
        GrammarDetectionRequestDto request,
        CancellationToken ct = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Text))
        {
            throw new ArgumentException("Nội dung tra cứu không được trống.");
        }

        var inputText = request.Text.Trim();
        // GRM-001: Use sentence context only when it contains one unambiguous
        // occurrence of the selected text. Token offsets are then filtered and
        // remapped so the public response remains relative to input text.
        var contextText = request.Context?.Trim();
        var selectionStart = FindUnambiguousSelectionStart(contextText, inputText);
        var analysisText = selectionStart is null ? inputText : contextText!;

        if (analysisText.Length > 2000)
        {
            throw new ArgumentException("Nội dung tra cứu không được vượt quá 2000 ký tự.");
        }

        IReadOnlyList<NormalizedTokenDto> tokens;
        try
        {
            tokens = await _tokenizerPort.TokenizeAsync(analysisText, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // NET-006: Graceful Degradation khi Tokenizer Sidecar không phản hồi
            return new GrammarDetectionResultDto(Array.Empty<GrammarOccurrenceDto>(), "failed");
        }

        if (tokens.Count == 0)
        {
            return new GrammarDetectionResultDto(Array.Empty<GrammarOccurrenceDto>(), "completed");
        }

        if (selectionStart is not null)
        {
            var selectionEnd = selectionStart.Value + inputText.Length;
            tokens = tokens
                .Where(token => token.SpanStart >= selectionStart.Value && token.SpanEnd <= selectionEnd)
                .Select(token => token with
                {
                    SpanStart = token.SpanStart - selectionStart.Value,
                    SpanEnd = token.SpanEnd - selectionStart.Value
                })
                .ToArray();
        }

        var activeRules = await _grammarRepository.GetAllActiveRulesAsync(ct);
        var occurrences = _matcher.Match(tokens, activeRules, inputText);

        return new GrammarDetectionResultDto(occurrences, "completed");
    }

    private static int? FindUnambiguousSelectionStart(string? context, string inputText)
    {
        if (string.IsNullOrWhiteSpace(context))
        {
            return null;
        }

        var firstIndex = context.IndexOf(inputText, StringComparison.Ordinal);
        if (firstIndex < 0 || context.IndexOf(inputText, firstIndex + inputText.Length, StringComparison.Ordinal) >= 0)
        {
            return null;
        }

        return firstIndex;
    }
}
