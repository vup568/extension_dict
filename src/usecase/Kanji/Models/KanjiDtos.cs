namespace Application.Kanji.Models;

public record KanjiLookupRequestDto(string Text);

public record KanjiDetailDto(
    string CanonicalId,
    string Character,
    short? StrokeCount,
    short? Grade,
    short? JlptLevel,
    string? JlptProvenance,
    short? Frequency,
    string UnicodeCodepoint,
    short? RadicalNumber,
    string? HanViet,
    IReadOnlyList<string> OnReadings,
    IReadOnlyList<string> KunReadings,
    IReadOnlyList<string> MeaningsVi,
    IReadOnlyList<string> MeaningsEn,
    IReadOnlyList<string> NanoriReadings
);

public record KanjiLookupResultDto(
    IReadOnlyList<KanjiDetailDto> Matches
);
