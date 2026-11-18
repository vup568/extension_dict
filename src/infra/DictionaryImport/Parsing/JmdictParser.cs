using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using Domain.Entities;

namespace Infrastructure.DictionaryImport.Parsing;

public sealed class JmdictParser(
    IReadOnlyDictionary<string, string>? approvedEntityTable = null, SourceParserLimits? limits = null,
    string? artifactSha256 = null)
{
    // The caller must verify the artifact hash before supplying it. This exception is
    // limited to the audited source snapshot; a new snapshot goes through strict audit.
    private const string AuditedSnapshotSha256 = "1e89ac398ec0230b7bb1daa246ce0095421bf3667bb6509aab93e26e7fdfd1b5";
    public IEnumerable<ParsedDictionaryRecord> Read(Stream input, CancellationToken ct = default)
    {
        foreach (var audited in Audit(input, ct))
        {
            if (audited.Error is not null)
                throw new InvalidDataException($"JMdict record {audited.Ordinal} ({audited.SourceIdentity}): {audited.Error}");
            yield return audited.Parsed!;
        }
    }

    /// <summary>Audits all structurally valid records without granting canonical import permission to rejected facts.</summary>
    public IEnumerable<DictionarySourceAuditRecord> Audit(Stream input, CancellationToken ct = default)
    {
        foreach (var (ordinal, element, rawXml) in BoundedXmlSource.Read(input, "JMdict", "entry",
                     limits ?? new(), approvedEntityTable ?? new Dictionary<string, string>(), ct))
        {
            var rawJson = JsonSerializer.Serialize(new { format = "jmdict-xml", rawXml });
            ParsedDictionaryRecord? parsed = null;
            string? error = null;
            try { parsed = Normalize(ordinal, element, rawJson, artifactSha256); }
            catch (InvalidDataException exception) { error = exception.Message; }
            yield return new(ordinal, element.Element("ent_seq")?.Value, rawJson, parsed, error);
        }
    }

    private static ParsedDictionaryRecord Normalize(long ordinal, XElement element, string rawJson, string? artifactSha256)
    {
        var identity = Required(element, "ent_seq");
        if (!long.TryParse(identity, NumberStyles.None, CultureInfo.InvariantCulture, out var sequence) || sequence < 1)
            throw new InvalidDataException("Invalid JMdict ent_seq.");
        if (sequence.ToString(CultureInfo.InvariantCulture) != identity)
            throw new InvalidDataException("Noncanonical JMdict ent_seq.");
        var entry = new DictionaryEntry { CanonicalId = $"dictionary:jmdict:{identity}" };
        foreach (var form in element.Elements("k_ele"))
            entry.WrittenForms.Add(new WrittenForm
            {
                Entry = entry, Form = Required(form, "keb"), Info = Values(form, "ke_inf"),
                IsCommon = HasCommonPriority(form, "ke_pri")
            });
        foreach (var reading in element.Elements("r_ele"))
            entry.Readings.Add(new Reading
            {
                Entry = entry, ReadingText = Required(reading, "reb"), IsNoKanji = reading.Element("re_nokanji") is not null,
                RestrictedToForms = Values(reading, "re_restr"), Info = Values(reading, "re_inf"),
                IsCommon = HasCommonPriority(reading, "re_pri")
            });
        if (entry.Readings.Count == 0 || entry.Readings.Select(r => r.ReadingText).Distinct(StringComparer.Ordinal).Count() != entry.Readings.Count ||
            entry.WrittenForms.Select(f => f.Form).Distinct(StringComparer.Ordinal).Count() != entry.WrittenForms.Count)
            throw new InvalidDataException("Missing or duplicate JMdict forms/readings.");
        foreach (var reading in entry.Readings)
        {
            if (reading.IsNoKanji && reading.RestrictedToForms is { Length: > 0 })
                throw new InvalidDataException("No-kanji reading has written restrictions.");
            if ((reading.RestrictedToForms ?? []).Any(f => !entry.WrittenForms.Any(w => w.Form == f)))
                throw new InvalidDataException("Unknown reading form restriction.");
        }
        string[] inheritedPos = [];
        short position = 0;
        foreach (var sourceSense in element.Elements("sense"))
        {
            position = checked((short)(position + 1));
            var declaredPos = Values(sourceSense, "pos");
            if (declaredPos.Length > 0) inheritedPos = declaredPos;
            var sense = new DictionarySense
            {
                Entry = entry, SenseKey = position.ToString(CultureInfo.InvariantCulture), Position = position,
                PartOfSpeech = inheritedPos.ToArray()
            };
            short glossPosition = 0;
            foreach (var gloss in sourceSense.Elements("gloss"))
            {
                var language = gloss.Attribute(XNamespace.Xml + "lang")?.Value ?? "eng";
                // EARS[FR-004]: Only reviewed mapping can attach Vietnamese meanings to a canonical sense.
                if (language != "eng") continue;
                if (string.IsNullOrWhiteSpace(gloss.Value)) throw new InvalidDataException("Empty JMdict gloss.");
                sense.Glosses.Add(new LocalizedGloss
                {
                    Sense = sense, LanguageTag = "en", GlossText = gloss.Value,
                    Position = glossPosition = checked((short)(glossPosition + 1)), ReviewStatus = "approved"
                });
            }
            AddApplicabilities(entry, sense, Values(sourceSense, "stagk"), Values(sourceSense, "stagr"), identity, artifactSha256);
            entry.Senses.Add(sense);
        }
        if (entry.Senses.Count == 0) throw new InvalidDataException("Missing JMdict senses.");
        return new(ordinal, identity, rawJson, entry, null);
    }

    private static void AddApplicabilities(DictionaryEntry entry, DictionarySense sense, string[] forms, string[] readings,
        string identity, string? artifactSha256)
    {
        if (forms.Length == 0 && readings.Length == 0) return;
        if (forms.Any(f => !entry.WrittenForms.Any(w => w.Form == f)) ||
            readings.Any(r => !entry.Readings.Any(candidate => candidate.ReadingText == r)))
            throw new InvalidDataException("Unknown sense form/reading restriction.");
        // EARS[FR-001]: stagk AND stagr constrain the same pair; never emit independent OR wildcards.
        foreach (var reading in entry.Readings.Where(r => readings.Length == 0 || readings.Contains(r.ReadingText, StringComparer.Ordinal)))
        {
            if (forms.Length == 0 && (reading.IsNoKanji || entry.WrittenForms.Count == 0))
                sense.Applicabilities.Add(new SenseApplicability { Sense = sense, ReadingEntity = reading });
            if (reading.IsNoKanji) continue;
            foreach (var form in entry.WrittenForms.Where(w => forms.Length == 0 || forms.Contains(w.Form, StringComparer.Ordinal)))
            {
                if (reading.RestrictedToForms is { Length: > 0 } restricted && !restricted.Contains(form.Form, StringComparer.Ordinal)) continue;
                sense.Applicabilities.Add(new SenseApplicability { Sense = sense, WrittenForm = form, ReadingEntity = reading });
            }
        }
        if (sense.Applicabilities.Count == 0)
        {
            var auditedException = string.Equals(artifactSha256, AuditedSnapshotSha256, StringComparison.OrdinalIgnoreCase)
                && identity == "1339160" && sense.SenseKey == "2"
                && forms.Length == 1 && forms[0] == "出し"
                && readings.Length == 1 && readings[0] == "ダシ"
                && entry.Readings.Any(r => r.ReadingText == "ダシ" && r.IsNoKanji);
            if (!auditedException)
                throw new InvalidDataException($"Sense {sense.SenseKey} has no applicable form/reading pair.");

            // Preserve the upstream AND constraints without inventing a valid written/reading pairing.
            // EARS[Unwanted]: Lookup constructs only compatible form/reading pairs, so these
            // contradictory predicates cannot produce a match. Strict auditing remains the default.
            foreach (var form in entry.WrittenForms.Where(w => forms.Contains(w.Form, StringComparer.Ordinal)))
            foreach (var reading in entry.Readings.Where(r => readings.Contains(r.ReadingText, StringComparer.Ordinal)))
                sense.Applicabilities.Add(new SenseApplicability { Sense = sense, WrittenForm = form, ReadingEntity = reading });
        }
    }

    private static bool HasCommonPriority(XElement element, string tag) =>
        element.Elements(tag).Any(p => p.Value is "ichi1" or "news1" or "spec1" or "spec2" or "gai1");

    private static string[] Values(XElement element, string name) => element.Elements(name).Select(e => e.Value).ToArray();

    private static string Required(XElement element, string name)
    {
        var values = element.Elements(name).ToArray();
        if (values.Length != 1 || string.IsNullOrWhiteSpace(values[0].Value))
            throw new InvalidDataException($"Missing or repeated {name}.");
        return values[0].Value;
    }
}
