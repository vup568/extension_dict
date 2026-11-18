using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Domain.Entities;

namespace Infrastructure.DictionaryImport.Parsing;

public sealed class KanjidicParser(SourceParserLimits? limits = null)
{
    public IEnumerable<ParsedDictionaryRecord> Read(Stream input, CancellationToken ct = default)
    {
        string? headerXml = null;
        foreach (var (ordinal, source, rawXml) in BoundedXmlSource.Read(input, "kanjidic2", "character",
                     limits ?? new(), new Dictionary<string, string>(), ct, header => headerXml = header))
        {
            var literal = source.Element("literal")?.Value;
            if (string.IsNullOrEmpty(literal) || !Rune.TryGetRuneAt(literal, 0, out var rune) || rune.Utf16SequenceLength != literal.Length)
                throw new InvalidDataException("KANJIDIC2 literal must contain exactly one Unicode scalar.");
            var scalarHex = rune.Value.ToString("X", CultureInfo.InvariantCulture);
            var ucs = source.Element("codepoint")?.Elements("cp_value").Where(c => (string?)c.Attribute("cp_type") == "ucs").ToArray() ?? [];
            if (ucs.Length != 1 || !int.TryParse(ucs[0].Value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var sourceScalar) || sourceScalar != rune.Value)
                throw new InvalidDataException("KANJIDIC2 literal/codepoint mismatch.");
            var groups = source.Element("reading_meaning")?.Elements("rmgroup").ToArray() ?? [];
            var readings = groups.SelectMany(g => g.Elements("reading")).ToArray();
            var misc = source.Element("misc");
            var metadata = JsonSerializer.Serialize(new { format = "kanjidic2-xml", headerXml, rawXml, jlptPolicy = "legacy-level-preserved-only-in-raw" });
            var kanji = new KanjiRecord
            {
                CanonicalId = $"kanji:{scalarHex}", Character = literal, UnicodeCodepoint = $"U+{scalarHex}",
                StrokeCount = OptionalShort(misc?.Elements("stroke_count").FirstOrDefault()),
                Grade = OptionalShort(misc?.Element("grade")), Frequency = OptionalShort(misc?.Element("freq")),
                RadicalNumber = OptionalShort(source.Element("radical")?.Elements("rad_value").FirstOrDefault(r => (string?)r.Attribute("rad_type") == "classical")),
                OnReadings = ReadingValues(readings, "ja_on"), KunReadings = ReadingValues(readings, "ja_kun"),
                // EARS[FR-005]: Hán Việt readings are distinct from semantic Vietnamese meanings.
                HanViet = JoinOrNull(ReadingValues(readings, "vietnam")), MeaningsVi = [],
                MeaningsEn = groups.SelectMany(g => g.Elements("meaning")).Where(m => m.Attribute("m_lang") is null || (string?)m.Attribute("m_lang") == "en").Select(m => m.Value).ToArray(),
                NanoriReadings = source.Element("reading_meaning")?.Elements("nanori").Select(n => n.Value).ToArray() ?? [],
                SourceMetadata = metadata
            };
            yield return new(ordinal, scalarHex, metadata, null, kanji);
        }
    }

    private static string[] ReadingValues(IEnumerable<XElement> readings, string type) =>
        readings.Where(r => (string?)r.Attribute("r_type") == type).Select(r => r.Value).ToArray();

    private static string? JoinOrNull(string[] values) => values.Length == 0 ? null : string.Join("; ", values);

    private static short? OptionalShort(XElement? element)
    {
        if (element is null) return null;
        if (!short.TryParse(element.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number < 1)
            throw new InvalidDataException("Invalid KANJIDIC2 numeric fact.");
        return number;
    }
}
