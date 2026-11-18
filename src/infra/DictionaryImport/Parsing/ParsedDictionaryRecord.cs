using Domain.Entities;

namespace Infrastructure.DictionaryImport.Parsing;

public sealed record ParsedDictionaryRecord(
    long Ordinal, string SourceIdentity, string RawJson, DictionaryEntry? Entry, KanjiRecord? Kanji);

public sealed record DictionarySourceAuditRecord(long Ordinal, string? SourceIdentity, string RawJson,
    ParsedDictionaryRecord? Parsed, string? Error);

/// <summary>Limits apply to expanded UTF-8 source streams, before any record is materialized.</summary>
public sealed record SourceParserLimits
{
    public long MaxInputCharacters { get; init; } = 2L * 1024 * 1024 * 1024;
    public int MaxPrologCharacters { get; init; } = 256 * 1024;
    public int MaxRecordCharacters { get; init; } = 2 * 1024 * 1024;
    public int MaxDepth { get; init; } = 32;
    public int MaxEntities { get; init; } = 512;

    internal void Validate()
    {
        if (MaxInputCharacters < 1 || MaxPrologCharacters < 1 || MaxRecordCharacters < 1 ||
            MaxDepth < 2 || MaxEntities < 0)
            throw new ArgumentOutOfRangeException(nameof(SourceParserLimits));
    }
}
