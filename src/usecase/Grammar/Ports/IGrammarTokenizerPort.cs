namespace Application.Grammar.Ports;

using Application.Grammar.Models;

/// <summary>
/// Port đại diện cho dịch vụ Tokenizer (gRPC Python Sudachi Sidecar).
/// </summary>
public interface IGrammarTokenizerPort
{
    Task<IReadOnlyList<NormalizedTokenDto>> TokenizeAsync(string text, CancellationToken ct = default);
}
