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

        if (request.Text.Length > 2000)
        {
            throw new ArgumentException("Nội dung tra cứu không được vượt quá 2000 ký tự.");
        }

        IReadOnlyList<NormalizedTokenDto> tokens;
        try
        {
            tokens = await _tokenizerPort.TokenizeAsync(request.Text, ct);
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

        var activeRules = await _grammarRepository.GetAllActiveRulesAsync(ct);
        var occurrences = _matcher.Match(tokens, activeRules, request.Text);

        return new GrammarDetectionResultDto(occurrences, "completed");
    }
}
