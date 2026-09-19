namespace Infrastructure.Adapters;

using Application.Grammar.Models;
using Application.Grammar.Ports;
using global::Grpc.Core;
using Infrastructure.Adapters.Grpc;

public class GrpcGrammarTokenizerAdapter : IGrammarTokenizerPort
{
    private readonly TokenizerService.TokenizerServiceClient _client;

    public GrpcGrammarTokenizerAdapter(TokenizerService.TokenizerServiceClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlyList<NormalizedTokenDto>> TokenizeAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<NormalizedTokenDto>();
        }

        // Strict timeout 500ms (NFR-PERF-01)
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromMilliseconds(500));

        var request = new TokenizeRequest
        {
            Text = text,
            Mode = "C"
        };

        try
        {
            var response = await _client.TokenizeAsync(request, cancellationToken: cts.Token);
            if (response == null || response.Tokens == null || response.Tokens.Count == 0)
            {
                return Array.Empty<NormalizedTokenDto>();
            }

            return response.Tokens
                .Select(t => new NormalizedTokenDto(
                    Surface: t.Surface ?? string.Empty,
                    BaseForm: t.BaseForm ?? string.Empty,
                    Pos: t.Pos ?? string.Empty,
                    SpanStart: t.SpanStart,
                    SpanEnd: t.SpanEnd
                ))
                .ToList();
        }
        catch (RpcException)
        {
            // NET-006: Re-throw so UseCase catches it for graceful degradation
            throw;
        }
    }
}
