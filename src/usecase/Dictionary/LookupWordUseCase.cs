using Application.Dictionary.Models;
using Application.Dictionary.Ports;
using Domain.Entities;

namespace Application.Dictionary;

/// <summary>
/// Use Case thực hiện tra cứu từ vựng tiếng Nhật tập trung.
/// Hỗ trợ Exact Match, Deinflection qua Sidecar (Graceful degradation) và Vietnamese-first Gloss mapping.
/// </summary>
public sealed class LookupWordUseCase(
    IDictionaryRepository dictionaryRepository,
    ITokenizerAdapter tokenizerAdapter)
{
    public const int MaxQueryLength = 255;

    public async Task<LookupResultDto> ExecuteAsync(string query, CancellationToken cancellationToken = default)
    {
        // 1. Validation
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Từ khóa tra cứu không được trống.", nameof(query));
        }

        var trimmedQuery = query.Trim();
        if (trimmedQuery.Length > MaxQueryLength)
        {
            throw new ArgumentException($"Từ khóa tra cứu không được vượt quá {MaxQueryLength} ký tự.", nameof(query));
        }

        // EARS[State]: WHILE no tokenizer provider is configured, exact lookup remains usable and morphology is unavailable.
        string morphologyCapability = tokenizerAdapter.IsAvailable ? "completed" : "unavailable";
        string conjugationCapability = "unavailable";
        ConjugationDto? conjugation = null;

        // 2. Step 1: Tra cứu Exact Match
        var exactEntries = await dictionaryRepository.SearchByFormOrReadingAsync(trimmedQuery, cancellationToken);

        IReadOnlyList<DictionaryEntry> finalEntries = exactEntries;
        string activeSearchKeyword = trimmedQuery;

        // 3. Step 2: Nếu không có Exact Match (hoặc để kiểm tra dạng chia), thử Deinflect qua Sidecar
        if (exactEntries.Count == 0)
        {
            DeinflectionResult deinflectResult;
            try
            {
                deinflectResult = await tokenizerAdapter.DeinflectAsync(trimmedQuery, cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                deinflectResult = DeinflectionResult.Unavailable(trimmedQuery);
            }
            catch (HttpRequestException)
            {
                deinflectResult = DeinflectionResult.Unavailable(trimmedQuery);
            }

            cancellationToken.ThrowIfCancellationRequested();
            // EARS[State]: Missing provider/content is distinct from completed-empty analysis.
            morphologyCapability = deinflectResult.IsAvailable ? "completed" : "unavailable";
            conjugationCapability = deinflectResult.ConjugationStatus;
            if (deinflectResult.Candidates is { } candidates)
                return await ResolveCandidatesAsync(trimmedQuery, candidates, morphologyCapability,
                    conjugationCapability, cancellationToken);
            if (deinflectResult.IsSuccess && !string.IsNullOrWhiteSpace(deinflectResult.BaseForm))
            {
                // EARS[Unwanted]: Repository failures SHALL not be swallowed as tokenizer failures.
                var baseEntries = await dictionaryRepository.SearchByFormOrReadingAsync(deinflectResult.BaseForm, cancellationToken);
                if (baseEntries.Count > 0)
                {
                    finalEntries = baseEntries;
                    activeSearchKeyword = deinflectResult.BaseForm;
                    conjugation = new ConjugationDto(
                        SurfaceText: trimmedQuery,
                        BaseForm: deinflectResult.BaseForm,
                        Explanation: deinflectResult.RuleExplanation,
                        Detail: deinflectResult.Detail);
                }
            }
        }

        // 4. Step 3: Map Entities sang DTOs tuân thủ Vietnamese-first & Match Provenance
        var matches = MapEntriesToDtos(finalEntries, activeSearchKeyword);

        return new LookupResultDto(
            Matches: matches,
            Conjugation: conjugation,
            Capabilities: new CapabilityStatusDto(Morphology: morphologyCapability, Conjugation: conjugationCapability));
    }

    private static List<EntryMatchDto> MapEntriesToDtos(IEnumerable<DictionaryEntry> entries, string searchKeyword)
    {
        var result = new List<EntryMatchDto>();

        foreach (var entry in entries)
        {
            string? matchedForm = entry.WrittenForms
                .FirstOrDefault(w => string.Equals(w.Form, searchKeyword, StringComparison.Ordinal))?.Form;

            string? matchedReading = entry.Readings
                .FirstOrDefault(r => string.Equals(r.ReadingText, searchKeyword, StringComparison.Ordinal))?.ReadingText;

            var writtenFormDtos = entry.WrittenForms
                .OrderByDescending(w => w.IsCommon)
                .ThenBy(w => w.Priority ?? short.MaxValue)
                .Select(w => new WrittenFormDto(
                    Id: w.Id,
                    Form: w.Form,
                    Priority: w.Priority,
                    IsCommon: w.IsCommon,
                    Info: w.Info))
                .ToList();

            var readingDtos = entry.Readings
                .OrderByDescending(r => r.IsCommon)
                .ThenBy(r => r.Priority ?? short.MaxValue)
                .Select(r => new ReadingDto(
                    Id: r.Id,
                    ReadingText: r.ReadingText,
                    Priority: r.Priority,
                    IsCommon: r.IsCommon,
                    Info: r.Info,
                    RestrictedToForms: r.RestrictedToForms))
                .ToList();

            var senseDtos = entry.Senses
                .OrderBy(s => s.Position)
                .Select(s => MapSenseToDto(s))
                .ToList();

            result.Add(new EntryMatchDto(
                CanonicalId: entry.CanonicalId,
                MatchedWrittenForm: matchedForm,
                MatchedReading: matchedReading,
                WrittenForms: writtenFormDtos,
                Readings: readingDtos,
                Senses: senseDtos));
        }

        return result;
    }

    private async Task<LookupResultDto> ResolveCandidatesAsync(string text,
        IReadOnlyList<Application.Conjugation.Models.ConjugationCandidate> candidates,
        string morphology, string status, CancellationToken ct)
    {
        var alternatives = new List<Application.Conjugation.Models.ConjugationExplanationDto>();
        var matches = new List<EntryMatchDto>();
        bool foundBase = false, contentMissing = false;
        foreach (var group in candidates.GroupBy(c => c.Detail.BaseForm, StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();
            // EARS[Unwanted]: A decoded form is only a proposal; verify exact canonical identity and applicable POS.
            var entries = (await dictionaryRepository.SearchByFormOrReadingAsync(group.Key, ct))
                .Where(e => e.WrittenForms.Any(w => w.Form == group.Key) || e.Readings.Any(r => r.ReadingText == group.Key)).ToArray();
            foundBase |= entries.Length > 0;
            foreach (var candidate in group)
            {
                var applicable = entries.Where(e => e.Senses.Any(s =>
                    (s.PartOfSpeech ?? []).Intersect(candidate.DictionaryPartOfSpeech, StringComparer.Ordinal).Any() &&
                    Applicable(e, s, group.Key))).ToArray();
                if (applicable.Length == 0) continue;
                if (string.IsNullOrWhiteSpace(candidate.Detail.Explanation) || candidate.Detail.Segments.Any(s =>
                    string.IsNullOrWhiteSpace(s.Explanation))) { contentMissing = true; continue; }
                alternatives.Add(candidate.Detail);
                matches.AddRange(MapEntriesToDtos(applicable, group.Key));
            }
        }
        ct.ThrowIfCancellationRequested();
        if (contentMissing) { alternatives.Clear(); status = "unavailable"; }
        else if (alternatives.Count > 0) status = "completed";
        else if (foundBase) status = "unavailable";
        // Missing canonical entries remain completed-empty only when the observed full-word grammar matched.
        // EARS[Event]: Dedupe public readings only after canonical class/sense verification.
        var ordered = alternatives.DistinctBy(a => (a.RuleId, a.RuleVersion, a.BaseForm))
            .OrderBy(a => a.RuleId, StringComparer.Ordinal).ThenBy(a => a.BaseForm, StringComparer.Ordinal).ToArray();
        var single = ordered.Length == 1 ? ordered[0] : null;
        return new(matches.DistinctBy(m => (m.CanonicalId, m.MatchedWrittenForm, m.MatchedReading)).ToArray(),
            single is null ? null : new(text, single.BaseForm, single.Explanation, single),
            new(morphology, status), ordered);
    }

    private static bool Applicable(DictionaryEntry entry, DictionarySense sense, string form)
    {
        // EARS[Unwanted] F09 FR-008: Validate joint form/reading pairs, not independent ID sets.
        var pairs = new List<(long? Written, long? Reading)>();
        foreach (var written in entry.WrittenForms.Where(w => w.Form == form))
        {
            if (entry.Readings.Count == 0) pairs.Add((written.Id, null));
            foreach (var reading in entry.Readings.Where(r => Allows(r, written.Form)))
                pairs.Add((written.Id, reading.Id));
        }
        foreach (var reading in entry.Readings.Where(r => r.ReadingText == form))
        {
            if (entry.WrittenForms.Count == 0 && Allows(reading, form)) pairs.Add((null, reading.Id));
            foreach (var written in entry.WrittenForms.Where(w => Allows(reading, w.Form)))
                pairs.Add((written.Id, reading.Id));
        }
        return pairs.Any(pair => sense.Applicabilities.Count == 0 || sense.Applicabilities.Any(a =>
            (a.WrittenFormId is null || pair.Written == a.WrittenFormId) &&
            (a.ReadingId is null || pair.Reading == a.ReadingId)));
    }

    private static bool Allows(Reading reading, string written) =>
        reading.RestrictedToForms is null || reading.RestrictedToForms.Length == 0 ||
        reading.RestrictedToForms.Contains(written, StringComparer.Ordinal);

    private static SenseDto MapSenseToDto(DictionarySense sense)
    {
        // Sort/Filter glosses: ưu tiên tiếng Việt ("vi"), fallback tiếng Anh ("en")
        var glosses = sense.Glosses ?? [];
        var viGlosses = glosses.Where(g => string.Equals(g.LanguageTag, "vi", StringComparison.OrdinalIgnoreCase)).ToList();
        var selectedGlosses = viGlosses.Count > 0
            ? viGlosses
            : glosses.Where(g => string.Equals(g.LanguageTag, "en", StringComparison.OrdinalIgnoreCase)).ToList();

        var glossDtos = selectedGlosses
            .OrderBy(g => g.Position)
            .Select(g => new GlossDto(LanguageTag: g.LanguageTag, GlossText: g.GlossText))
            .ToList();

        var restrictionDtos = (sense.Applicabilities ?? [])
            .Select(a => new SenseRestrictionDto(WrittenFormId: a.WrittenFormId, ReadingId: a.ReadingId))
            .ToList();

        return new SenseDto(
            SenseKey: sense.SenseKey,
            PartOfSpeech: sense.PartOfSpeech ?? [],
            Position: sense.Position,
            Glosses: glossDtos,
            Restrictions: restrictionDtos);
    }
}
