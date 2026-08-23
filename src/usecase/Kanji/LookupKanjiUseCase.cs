using Application.Kanji.Helpers;
using Application.Kanji.Models;
using Application.Kanji.Ports;
using Domain.Entities;

namespace Application.Kanji;

/// <summary>
/// Use Case thực hiện tra cứu thông tin Kanji tập trung.
/// Bóc tách Kanji unique từ text, truy vấn dữ liệu canonical và bảo toàn thứ tự xuất hiện đầu tiên.
/// </summary>
public sealed class LookupKanjiUseCase(IKanjiRepository kanjiRepository)
{
    public const int MaxInputLength = 1000;

    public async Task<KanjiLookupResultDto> ExecuteAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Nội dung tra cứu không được trống.", nameof(text));
        }

        var trimmedText = text.Trim();
        if (trimmedText.Length > MaxInputLength)
        {
            throw new ArgumentException($"Nội dung tra cứu không được vượt quá {MaxInputLength} ký tự.", nameof(text));
        }

        // 1. Trích xuất Kanji unique bảo toàn thứ tự first-occurrence
        var uniqueKanji = KanjiExtractor.ExtractUniqueKanji(trimmedText);
        if (uniqueKanji.Count == 0)
        {
            return new KanjiLookupResultDto([]);
        }

        // 2. Truy vấn DB lấy danh sách KanjiRecord
        var records = await kanjiRepository.GetByCharactersAsync(uniqueKanji, cancellationToken);
        if (records.Count == 0)
        {
            return new KanjiLookupResultDto([]);
        }

        // 3. Re-order kết quả theo đúng thứ tự first-occurrence trong uniqueKanji
        var recordMap = records.ToDictionary(r => r.Character, StringComparer.Ordinal);
        var matches = new List<KanjiDetailDto>();

        foreach (var character in uniqueKanji)
        {
            if (recordMap.TryGetValue(character, out var record))
            {
                matches.Add(MapToDto(record));
            }
        }

        return new KanjiLookupResultDto(matches);
    }

    private static KanjiDetailDto MapToDto(KanjiRecord entity)
    {
        return new KanjiDetailDto(
            CanonicalId: entity.CanonicalId,
            Character: entity.Character,
            StrokeCount: entity.StrokeCount,
            Grade: entity.Grade,
            JlptLevel: entity.JlptLevel,
            JlptProvenance: entity.JlptProvenance,
            Frequency: entity.Frequency,
            UnicodeCodepoint: entity.UnicodeCodepoint,
            RadicalNumber: entity.RadicalNumber,
            HanViet: entity.HanViet,
            OnReadings: entity.OnReadings ?? [],
            KunReadings: entity.KunReadings ?? [],
            MeaningsVi: entity.MeaningsVi ?? [],
            MeaningsEn: entity.MeaningsEn ?? [],
            NanoriReadings: entity.NanoriReadings ?? []
        );
    }
}
