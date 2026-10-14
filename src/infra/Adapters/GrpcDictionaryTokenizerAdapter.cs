using Application.Conjugation;
using Application.Conjugation.Ports;
using Application.Dictionary.Models;
using Application.Dictionary.Ports;
using Grpc.Core;
using Infrastructure.Adapters.Grpc;
using Infrastructure.Conjugation;

namespace Infrastructure.Adapters;

/// <summary>Provider-isolated, data-driven whole-word deinflection. No suffix heuristics.</summary>
public sealed class GrpcDictionaryTokenizerAdapter(
    TokenizerService.TokenizerServiceClient client,
    IConjugationDatasetRepository datasets,
    string? datasetVersion) : ITokenizerAdapter
{
    public bool IsAvailable => !string.IsNullOrWhiteSpace(datasetVersion);

    public async Task<DeinflectionResult> DeinflectAsync(string surfaceText, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // EARS[State]: Only an explicitly pinned dataset activates the new linguistic capability.
        if (!IsAvailable) return DeinflectionResult.Unavailable(surfaceText);
        var dataset = await datasets.GetAsync(datasetVersion!, cancellationToken);
        if (dataset is null) return DeinflectionResult.Unavailable(surfaceText);
        var knowledge = ConjugationKnowledge.Parse(dataset);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromMilliseconds(500));
        TokenizeResponse response;
        try
        {
            response = await client.TokenizeAsync(
                new TokenizeRequest { Text = surfaceText, Mode = "C" }, cancellationToken: deadline.Token);
        }
        catch (RpcException) when (!cancellationToken.IsCancellationRequested)
        {
            return DeinflectionResult.Unavailable(surfaceText);
        }
        catch (RpcException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DeinflectionResult.Unavailable(surfaceText);
        }
        cancellationToken.ThrowIfCancellationRequested();
        var tokens = response.Tokens.Select(token => knowledge.Normalize(
            token.Surface, token.BaseForm, token.Pos.Split(','), token.SpanStart, token.SpanEnd)).ToArray();
        if (knowledge.Paradigms is { } paradigms)
        {
            var candidates = ConjugationParadigmMatcher.Decode(surfaceText, tokens, paradigms, dataset.Version);
            // Proposals stay internal until exact canonical entry/POS verification in the use case.
            return new(candidates.Count > 0, surfaceText, string.Empty, string.Empty,
                ConjugationStatus: candidates.Count > 0 ? "completed" : "unavailable", Candidates: candidates);
        }
        var result = ConjugationMatcher.Match(surfaceText, tokens, knowledge.Rules, dataset.Version);
        if (!result.IsContentAvailable)
            return new(false, surfaceText, string.Empty, string.Empty, ConjugationStatus: "unavailable");
        if (result.Explanation is not { } explanation)
            return new(false, surfaceText, string.Empty, string.Empty, ConjugationStatus: "unavailable");
        return new(true, surfaceText, explanation.BaseForm, explanation.Explanation,
            Detail: explanation, ConjugationStatus: "completed");
    }
}
