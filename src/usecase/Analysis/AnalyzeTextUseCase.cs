namespace Application.Analysis;

using Application.Analysis.Exceptions;
using Application.Analysis.Models;
using Application.Dictionary;
using Application.Dictionary.Models;
using Application.Grammar;
using Application.Grammar.Models;
using Application.Kanji;
using Application.Kanji.Models;

/// <summary>
/// Orchestrator Use Case cho Unified Analysis API (F-05).
/// Điều phối song song LookupWordUseCase (F-06), LookupKanjiUseCase (F-07),
/// DetectGrammarUseCase (F-08) và tổng hợp kết quả vào response envelope chuẩn hóa.
/// EARS[Ubiquitous]: THE system SHALL điều phối các capabilities song song khi không có dependency.
/// </summary>
public sealed class AnalyzeTextUseCase(
    LookupWordUseCase lookupWordUseCase,
    LookupKanjiUseCase lookupKanjiUseCase,
    DetectGrammarUseCase detectGrammarUseCase,
    SentenceVocabularyUseCase? sentenceVocabularyUseCase = null)
{
    /// <summary>
    /// Giới hạn độ dài input text baseline (OD-014).
    /// </summary>
    public const int MaxInputLength = 1000;

    public async Task<AnalysisResultDto> ExecuteAsync(
        AnalysisRequestDto request,
        CancellationToken cancellationToken = default,
        bool includeSentenceVocabulary = false)
    {
        // 1. Input validation
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new ArgumentException("Nội dung phân tích không được trống.", nameof(request));
        }

        var trimmedText = request.Text.Trim();
        if (trimmedText.Length > MaxInputLength)
        {
            throw new ArgumentException(
                $"Nội dung phân tích không được vượt quá {MaxInputLength} ký tự.",
                nameof(request));
        }

        // Validate interaction_id format (UUID only) if provided [AC-010]
        if (request.InteractionId is not null && !Guid.TryParse(request.InteractionId, out _))
        {
            throw new ArgumentException(
                "interaction_id phải là UUID hợp lệ.",
                nameof(request));
        }

        // 2. Parallel dispatch — per-capability try/catch [NET-006]
        // EARS[Ubiquitous]: Các capabilities SHOULD được thực thi song song.
        var vocabularyTask = ExecuteVocabularyAsync(trimmedText, includeSentenceVocabulary, cancellationToken);
        var kanjiTask = ExecuteKanjiAsync(trimmedText, cancellationToken);
        var grammarTask = ExecuteGrammarAsync(trimmedText, request.Context, cancellationToken);

        await Task.WhenAll(vocabularyTask, kanjiTask, grammarTask);

        var (vocabMatches, vocabStatus, conjugation, conjugationStatus, occurrences, truncated) = vocabularyTask.Result;
        var (kanjiMatches, kanjiStatus) = kanjiTask.Result;
        var (grammarOccurrences, grammarStatus) = grammarTask.Result;

        // 3. All-failed check → HTTP 503 [AC-007]
        if (vocabStatus == CapabilityStatus.Failed
            && kanjiStatus == CapabilityStatus.Failed
            && grammarStatus == CapabilityStatus.Failed)
        {
            throw new AllCapabilitiesFailedException();
        }

        // 4. Build response envelope
        return new AnalysisResultDto(
            NormalizedText: trimmedText,
            Tokens: [],                  // Future: Morphology/Tokenization [AC-006]
            DictionaryMatches: vocabMatches,
            Kanji: kanjiMatches,
            Conjugations: conjugation,
            GrammarOccurrences: grammarOccurrences,
            Capabilities: new AnalysisCapabilitiesDto(
                Vocabulary: vocabStatus,
                Kanji: kanjiStatus,
                Grammar: grammarStatus,
                Morphology: CapabilityStatus.Unavailable,
                Conjugation: conjugationStatus),
            VocabularyOccurrences: occurrences,
            VocabularyTruncated: truncated);
    }

    /// <summary>
    /// Gọi LookupWordUseCase — wrap trong try/catch riêng.
    /// Vocabulary có giới hạn 255 ký tự riêng; nếu text dài hơn → ArgumentException → "failed".
    /// </summary>
    private async Task<(IReadOnlyList<EntryMatchDto> Matches, string Status,
        IReadOnlyList<Application.Conjugation.Models.ConjugationExplanationDto> Conjugation, string ConjugationStatus,
        IReadOnlyList<VocabularyOccurrence>? Occurrences, bool? Truncated)> ExecuteVocabularyAsync(
        string text, bool includeSentenceVocabulary, CancellationToken ct)
    {
        try
        {
            // EARS[Event] F-06: Preserve exact and whole-word deinflection before sentence analysis.
            LookupResultDto? result = null;
            if (text.Length <= LookupWordUseCase.MaxQueryLength || !includeSentenceVocabulary || sentenceVocabularyUseCase is null)
                result = await lookupWordUseCase.ExecuteAsync(text, ct);
            if (result is not null && (result.Matches.Count > 0 || !includeSentenceVocabulary || sentenceVocabularyUseCase is null))
                return (result.Matches, CapabilityStatus.Completed, result.Alternatives ??
                    (result.Conjugation?.Detail is { } detail ? [detail] : []),
                    result.Capabilities.Conjugation, null, null);

            // EARS[Event] F-05: An opted-in sentence is tokenized and each occurrence is verified independently.
            var sentence = await sentenceVocabularyUseCase!.ExecuteAsync(text, ct);
            if (sentence.Status != CapabilityStatus.Completed)
                return ([], sentence.Status, [], CapabilityStatus.Unavailable, null, null);

            // Legacy clients see a deduplicated entry list; occurrence-aware clients retain
            // the exact surface, span and candidate-specific senses for every instance.
            var matches = sentence.Occurrences.SelectMany(occurrence => occurrence.Matches)
                .GroupBy(entry => (entry.CanonicalId, entry.MatchedWrittenForm, entry.MatchedReading))
                .Select(group => group.First() with
                {
                    WrittenForms = group.SelectMany(entry => entry.WrittenForms).DistinctBy(form => form.Id).ToArray(),
                    Readings = group.SelectMany(entry => entry.Readings).DistinctBy(reading => reading.Id).ToArray(),
                    Senses = group.SelectMany(entry => entry.Senses).DistinctBy(sense => sense.SenseKey)
                        .OrderBy(sense => sense.Position).ToArray()
                }).ToArray();
            return (matches, CapabilityStatus.Completed, [], CapabilityStatus.Unavailable,
                sentence.Occurrences, sentence.Truncated);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Graceful degradation: vocabulary failure không ảnh hưởng kanji/grammar
            return ([], CapabilityStatus.Failed, [], CapabilityStatus.Failed, null, null);
        }
    }

    /// <summary>
    /// Gọi LookupKanjiUseCase — wrap trong try/catch riêng.
    /// </summary>
    private async Task<(IReadOnlyList<KanjiDetailDto> Matches, string Status)> ExecuteKanjiAsync(
        string text, CancellationToken ct)
    {
        try
        {
            var result = await lookupKanjiUseCase.ExecuteAsync(text, ct);
            return (result.Matches, CapabilityStatus.Completed);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Graceful degradation: kanji failure không ảnh hưởng vocab/grammar
            return ([], CapabilityStatus.Failed);
        }
    }

    /// <summary>
    /// Gọi DetectGrammarUseCase — wrap trong try/catch riêng.
    /// Grammar phụ thuộc ngầm vào Tokenizer Sidecar (gRPC Sudachi) [NET-006].
    /// Khi Sidecar down, DetectGrammarUseCase tự trả Status: "failed" (graceful degradation nội bộ).
    /// Orchestrator vẫn cần try/catch để handle các exception khác (DB, unexpected errors).
    /// </summary>
    private async Task<(IReadOnlyList<GrammarOccurrenceDto> Occurrences, string Status)> ExecuteGrammarAsync(
        string text, string? context, CancellationToken ct)
    {
        try
        {
            var result = await detectGrammarUseCase.ExecuteAsync(
                new GrammarDetectionRequestDto(text, context), ct);
            return (result.Occurrences, result.Status);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Graceful degradation: grammar failure không ảnh hưởng vocab/kanji
            return ([], CapabilityStatus.Failed);
        }
    }
}
