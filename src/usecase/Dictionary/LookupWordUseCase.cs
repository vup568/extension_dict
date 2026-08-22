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

        string morphologyCapability = "completed";
        ConjugationDto? conjugation = null;

        // 2. Step 1: Tra cứu Exact Match
        var exactEntries = await dictionaryRepository.SearchByFormOrReadingAsync(trimmedQuery, cancellationToken);

        IReadOnlyList<DictionaryEntry> finalEntries = exactEntries;
        string activeSearchKeyword = trimmedQuery;

        // 3. Step 2: Nếu không có Exact Match (hoặc để kiểm tra dạng chia), thử Deinflect qua Sidecar
        if (exactEntries.Count == 0)
        {
            try
            {
                var deinflectResult = await tokenizerAdapter.DeinflectAsync(trimmedQuery, cancellationToken);
                if (deinflectResult.IsSuccess && !string.IsNullOrWhiteSpace(deinflectResult.BaseForm))
                {
                    var baseEntries = await dictionaryRepository.SearchByFormOrReadingAsync(deinflectResult.BaseForm, cancellationToken);
                    if (baseEntries.Count > 0)
                    {
                        finalEntries = baseEntries;
                        activeSearchKeyword = deinflectResult.BaseForm;
                        conjugation = new ConjugationDto(
                            SurfaceText: trimmedQuery,
                            BaseForm: deinflectResult.BaseForm,
                            Explanation: deinflectResult.RuleExplanation);
                    }
                }
            }
            catch
            {
                // Graceful degradation per SPEC §3.2 & NFR-REL-01
                morphologyCapability = "unavailable";
            }
        }

        // 4. Step 3: Map Entities sang DTOs tuân thủ Vietnamese-first & Match Provenance
        var matches = MapEntriesToDtos(finalEntries, activeSearchKeyword);

        return new LookupResultDto(
            Matches: matches,
            Conjugation: conjugation,
            Capabilities: new CapabilityStatusDto(Morphology: morphologyCapability));
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
