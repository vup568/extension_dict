namespace Application.Dictionary;

using System.Text.Json.Serialization;
using Application.Conjugation.Models;
using Application.Dictionary.Models;
using Application.Grammar.Models;
using Application.Grammar.Ports;
using Domain.Entities;

/// <summary>A dictionary match tied to one exact occurrence in the selected text.</summary>
public sealed record VocabularyOccurrence(
    [property: JsonPropertyName("surface")] string Surface,
    [property: JsonPropertyName("span")] ConjugationSpanDto Span,
    [property: JsonPropertyName("matches")] IReadOnlyList<EntryMatchDto> Matches,
    [property: JsonPropertyName("conjugations")] IReadOnlyList<ConjugationExplanationDto> Conjugations);

public sealed record SentenceVocabularyResult(
    IReadOnlyList<VocabularyOccurrence> Occurrences, string Status, bool Truncated);

/// <summary>Uses tokenizer spans to find independent dictionary matches in a selection.</summary>
public sealed class SentenceVocabularyUseCase(IGrammarTokenizerPort tokenizer, LookupWordUseCase lookup)
{
    // Bound both the number of database/provider calls and elapsed time. Results explicitly
    // indicate a partial analysis if either limit is reached.
    public const int MaxLookupAttempts = 128;
    public const int MaxExaminedTokens = 512;
    private static readonly TimeSpan LookupDeadline = TimeSpan.FromMilliseconds(3500);

    public async Task<SentenceVocabularyResult> ExecuteAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text is required.", nameof(text));
        ct.ThrowIfCancellationRequested();
        IReadOnlyList<NormalizedTokenDto> tokens;
        try { tokens = await tokenizer.TokenizeAsync(text, ct); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return new([], "unavailable", false); }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { return new([], "unavailable", false); }
        ct.ThrowIfCancellationRequested();
        if (!ValidTokens(text, tokens)) return new([], "failed", false);
        if (tokens.Count == 0) return new([], "unavailable", false);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(LookupDeadline);
        // Different grammatical filters must not share projections of the same spelling.
        var exactCache = new Dictionary<(string Form, SenseClass Class), IReadOnlyList<EntryMatchDto>>();
        var inflectedCache = new Dictionary<string, LookupResultDto>(StringComparer.Ordinal);
        var results = new List<VocabularyOccurrence>();
        var lookups = 0;
        var examined = 0;
        bool truncated = false;
        try
        {
            for (var i = 0; i < tokens.Count; i++)
            {
                timeout.Token.ThrowIfCancellationRequested();
                if (++examined > MaxExaminedTokens) { truncated = true; break; }
                var token = tokens[i];
                var category = Category(token);
                // Standalone particles can share kana with unrelated nouns; they are not
                // independently labeled as vocabulary without reliable POS matching.
                if (category is "空白" or "補助記号" or "記号" or "助動詞" or "助詞" || string.IsNullOrWhiteSpace(token.Surface)) continue;

                // EARS[Event]: A tokenizer prefix and adjacent noun form one vocabulary
                // occurrence only when that exact spelling has a compatible dictionary sense.
                // Leave a following suru chain to the existing noun + verb handling below.
                if (category == "接頭辞" && i + 1 < tokens.Count && IsAdjacent(token, tokens[i + 1])
                    && Category(tokens[i + 1]) == "名詞" && !HasFollowingSuru(tokens, i + 1))
                {
                    var noun = tokens[i + 1];
                    var combined = text[token.SpanStart..noun.SpanEnd];
                    var combinedMatches = await Exact(combined, SenseClass.Noun);
                    if (combinedMatches.Count > 0)
                    {
                        results.Add(new(combined, new(token.SpanStart, noun.SpanEnd), combinedMatches, []));
                        i++;
                        continue;
                    }
                }
                var end = i;
                var isSuruNoun = category == "名詞" && HasPart(token, "サ変可能") && i + 1 < tokens.Count
                    && IsAdjacent(token, tokens[i + 1]) && Category(tokens[i + 1]) == "動詞"
                    && tokens[i + 1].BaseForm == "する";
                if (isSuruNoun) end = Extend(tokens, i + 1);
                else if (category is "動詞" or "形容詞" or "形状詞") end = Extend(tokens, i);

                var surface = text[token.SpanStart..tokens[end].SpanEnd];
                if (surface.Length > LookupWordUseCase.MaxQueryLength) { truncated = true; break; }
                if (isSuruNoun)
                {
                    // JMdict commonly stores e.g. 勉強 as n/vs, not as the written form 勉強する.
                    // Only attach a following する chain to an entry with an explicit suru POS.
                    var nounMatches = await Exact(token.BaseForm, SenseClass.Suru);
                    if (nounMatches.Count > 0)
                    {
                        results.Add(new(surface, new(token.SpanStart, tokens[end].SpanEnd), nounMatches, []));
                        i = end;
                        continue;
                    }
                    end = i;
                    surface = token.Surface;
                }

                if (end > i)
                {
                    // EARS[Event]: An entire inflected candidate must be checked by the existing
                    // dictionary/POS-aware conjugation matcher before its explanation is returned.
                    var candidate = await Inflected(surface);
                    if (candidate.Matches.Count > 0)
                    {
                        var details = candidate.Alternatives ?? (candidate.Conjugation?.Detail is { } detail ? [detail] : []);
                        results.Add(new(surface, new(token.SpanStart, tokens[end].SpanEnd), candidate.Matches, details));
                        i = end;
                        continue;
                    }
                    // When an inflection is not covered by F09, the tokenizer's base remains
                    // a candidate only; the dictionary and POS must independently confirm it.
                    var baseMatches = await Exact(token.BaseForm, ClassForInflected(category));
                    if (baseMatches.Count > 0)
                    {
                        results.Add(new(surface, new(token.SpanStart, tokens[end].SpanEnd), baseMatches, []));
                        i = end;
                        continue;
                    }
                    end = i;
                    surface = token.Surface;
                }

                IReadOnlyList<EntryMatchDto> matches;
                if (token.BaseForm != surface && category is "動詞" or "形容詞" or "形状詞")
                {
                    // An inflected head alone can also be another noun's exact spelling.
                    // Prefer Sudachi's base and require a compatible dictionary POS.
                    matches = await Exact(token.BaseForm, ClassForInflected(category));
                    if (matches.Count == 0) matches = await Exact(surface, ClassForInflected(category));
                }
                else matches = await Exact(surface);
                if (matches.Count > 0)
                    results.Add(new(surface, new(token.SpanStart, token.SpanEnd), matches, []));
            }
        }
        catch (LookupLimitReachedException) { truncated = true; }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested && timeout.IsCancellationRequested) { truncated = true; }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception) { return new([], "failed", false); }
        return new(results, "completed", truncated);

        async Task<IReadOnlyList<EntryMatchDto>> Exact(string form, SenseClass senseClass = SenseClass.Any)
        {
            if (string.IsNullOrWhiteSpace(form) || form.Length > LookupWordUseCase.MaxQueryLength) return [];
            var key = (form, senseClass);
            if (exactCache.TryGetValue(key, out var cached)) return cached;
            if (lookups >= MaxLookupAttempts) throw new LookupLimitReachedException();
            lookups++;
            var found = await lookup.LookupExactMatchesAsync(form, timeout.Token,
                senseClass == SenseClass.Any ? null : sense => MatchesSense(sense, senseClass));
            exactCache[key] = found;
            return found;
        }

        async Task<LookupResultDto> Inflected(string form)
        {
            if (inflectedCache.TryGetValue(form, out var cached)) return cached;
            if (lookups >= MaxLookupAttempts) throw new LookupLimitReachedException();
            lookups++;
            var found = await lookup.ExecuteAsync(form, timeout.Token);
            inflectedCache[form] = found;
            return found;
        }
    }

    private static int Extend(IReadOnlyList<NormalizedTokenDto> tokens, int start)
    {
        var end = start;
        while (end + 1 < tokens.Count && IsAdjacent(tokens[end], tokens[end + 1]))
        {
            var next = tokens[end + 1];
            if (Category(next) == "助動詞") { end++; continue; }
            if (Category(next) == "助詞" && HasPart(next, "接続助詞") && next.Surface is "て" or "で")
            {
                end++;
                if (end + 1 < tokens.Count && IsAdjacent(tokens[end], tokens[end + 1])
                    && Category(tokens[end + 1]) == "動詞" && HasPart(tokens[end + 1], "非自立可能"))
                    end++;
                continue;
            }
            break;
        }
        return end;
    }

    private enum SenseClass { Any, Noun, Suru, Verb, IAdjective, NaAdjective }

    // EARS[Unwanted]: The selected POS cannot authorize readings or spellings
    // which only apply to a different sense of the same dictionary entry.
    private static bool MatchesSense(DictionarySense sense, SenseClass senseClass) =>
        (sense.PartOfSpeech ?? []).Any(p => senseClass switch
        {
            SenseClass.Noun => p is "n" or "n-adv" or "n-pref" or "n-pr" or "n-suf" or "n-t",
            SenseClass.Suru => p is "vs" or "vs-i" or "vs-c" or "vs-s",
            SenseClass.Verb => p.StartsWith('v'),
            SenseClass.IAdjective => p is "adj-i" or "adj-ix",
            SenseClass.NaAdjective => p == "adj-na",
            _ => false
        });

    private static SenseClass ClassForInflected(string category) => category switch
    {
        "動詞" => SenseClass.Verb,
        "形容詞" => SenseClass.IAdjective,
        "形状詞" => SenseClass.NaAdjective,
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };

    private static bool HasFollowingSuru(IReadOnlyList<NormalizedTokenDto> tokens, int nounIndex) =>
        HasPart(tokens[nounIndex], "サ変可能") && nounIndex + 1 < tokens.Count
        && IsAdjacent(tokens[nounIndex], tokens[nounIndex + 1]) && Category(tokens[nounIndex + 1]) == "動詞"
        && tokens[nounIndex + 1].BaseForm == "する";

    private static bool ValidTokens(string text, IReadOnlyList<NormalizedTokenDto>? tokens)
    {
        if (tokens is null) return false;
        var previous = 0;
        foreach (var token in tokens)
        {
            if (token is null || string.IsNullOrEmpty(token.Surface) || string.IsNullOrEmpty(token.Pos)
                || token.BaseForm is null || token.SpanStart != previous || token.SpanEnd <= token.SpanStart
                || token.SpanEnd > text.Length || !Boundary(text, token.SpanStart) || !Boundary(text, token.SpanEnd)
                || text[token.SpanStart..token.SpanEnd] != token.Surface) return false;
            previous = token.SpanEnd;
        }
        return tokens.Count == 0 || previous == text.Length;
    }

    private static bool Boundary(string text, int index) => index >= 0 && index <= text.Length &&
        !(index > 0 && index < text.Length && char.IsHighSurrogate(text[index - 1]) && char.IsLowSurrogate(text[index]));
    private static string Category(NormalizedTokenDto token) => token.Pos.Split(',')[0];
    private static bool HasPart(NormalizedTokenDto token, string value) => token.Pos.Split(',').Contains(value, StringComparer.Ordinal);
    private static bool IsAdjacent(NormalizedTokenDto first, NormalizedTokenDto second) => first.SpanEnd == second.SpanStart;
    private sealed class LookupLimitReachedException : Exception;
}
