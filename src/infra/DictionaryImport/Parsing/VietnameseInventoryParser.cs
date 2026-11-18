using System.Text;

namespace Infrastructure.DictionaryImport.Parsing;

public sealed record VietnameseInventoryRecord(long Ordinal, string SourceIdentity, string? Headword,
    string? Content, string RawLine, string Disposition, string Reason);

/// <summary>Audits tab records losslessly. It never creates canonical glosses or interprets HTML.</summary>
public sealed class VietnameseInventoryParser(string snapshotSha256, SourceParserLimits? limits = null)
{
    public IEnumerable<VietnameseInventoryRecord> Read(Stream input, CancellationToken ct = default)
    {
        if (snapshotSha256.Length != 64 || snapshotSha256.Any(c => !char.IsAsciiHexDigit(c)))
            throw new ArgumentException("A pinned snapshot SHA256 is required.", nameof(snapshotSha256));
        var bounds = limits ?? new();
        bounds.Validate();
        using var source = new StreamReader(input, new UTF8Encoding(false, true), true, 8192, leaveOpen: true);
        using var bounded = new BoundedXmlSource.LimitedTextReader(source, bounds.MaxInputCharacters, ct);
        long ordinal = 0;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var line = ReadLine(bounded, bounds.MaxRecordCharacters);
            if (line is null) yield break;
            ordinal++;
            var separator = line.IndexOf('\t');
            var valid = separator > 0 && separator < line.Length - 1;
            yield return new(ordinal, $"{snapshotSha256.ToLowerInvariant()}:{ordinal}",
                separator < 0 ? null : line[..separator], separator < 0 ? null : line[(separator + 1)..], line,
                "quarantined", valid ? "pending-license-and-semantic-review" : "malformed-tab-record");
        }
    }

    private static string? ReadLine(TextReader reader, int maxCharacters)
    {
        var line = new StringBuilder();
        while (true)
        {
            var value = reader.Read();
            if (value == -1) return line.Length == 0 ? null : line.ToString();
            if (value == '\n') return line.ToString();
            if (value == '\r') { if (reader.Peek() == '\n') reader.Read(); return line.ToString(); }
            if (line.Length >= maxCharacters) throw new InvalidDataException("Vietnamese inventory record limit exceeded.");
            line.Append((char)value);
        }
    }
}
