using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Infrastructure.DictionaryImport.Parsing;

/// <summary>Streams one bounded record. DTD declarations are inspected before the XML parser sees them.</summary>
internal static class BoundedXmlSource
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    public static IEnumerable<(long Ordinal, XElement Element, string RawXml)> Read(
        Stream input, string root, string recordName, SourceParserLimits limits,
        IReadOnlyDictionary<string, string> approvedEntities, CancellationToken cancellationToken, Action<string>? onHeader = null)
    {
        limits.Validate();
        using var source = new StreamReader(input, new UTF8Encoding(false, true), true, 8192, leaveOpen: true);
        using var bounded = new LimitedTextReader(source, limits.MaxInputCharacters, cancellationToken, limits.MaxRecordCharacters);
        var prefix = ReadProlog(bounded, limits.MaxPrologCharacters);
        var entities = ValidateProlog(prefix, root, approvedEntities, limits.MaxEntities);
        using var replay = new ReplayTextReader(prefix, bounded);
        // EARS[Security]: Never resolve external/file entities or recursively expand entity values.
        using var reader = new XmlTextReader(replay)
        {
            DtdProcessing = DtdProcessing.Parse,
            XmlResolver = null,
            EntityHandling = EntityHandling.ExpandCharEntities,
            Namespaces = true
        };
        long ordinal = 0;
        var sawRoot = false;
        var sawHeader = false;
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reader.Depth > limits.MaxDepth) throw new InvalidDataException("XML depth limit exceeded.");
            if (reader.NodeType == XmlNodeType.Element && reader.Depth == 0)
            {
                if (sawRoot || reader.Name != root) throw new InvalidDataException("Unexpected XML source root.");
                sawRoot = true;
            }
            else if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1)
            {
                if (reader.Name != recordName)
                {
                    // KANJIDIC2 has one bounded header. Unknown top-level records fail closed.
                    if (root != "kanjidic2" || reader.Name != "header" || sawHeader || ordinal > 0)
                        throw new InvalidDataException("Unexpected XML source record.");
                    sawHeader = true;
                    var header = ReadRecord(reader, limits, entities, cancellationToken);
                    onHeader?.Invoke(header.RawXml);
                    continue;
                }
                var record = ReadRecord(reader, limits, entities, cancellationToken);
                yield return (++ordinal, record.Element, record.RawXml);
            }
            else if (reader.NodeType == XmlNodeType.EntityReference)
                throw new InvalidDataException("Entity outside a source record.");
            else if (reader.NodeType is XmlNodeType.Text or XmlNodeType.CDATA && !string.IsNullOrWhiteSpace(reader.Value))
                throw new InvalidDataException("Text outside a source record.");
        }
        if (!sawRoot) throw new InvalidDataException("Missing XML source root.");
    }

    private static (XElement Element, string RawXml) ReadRecord(XmlReader reader, SourceParserLimits limits,
        HashSet<string> entities, CancellationToken cancellationToken)
    {
        using var rawText = new LimitedStringWriter(limits.MaxRecordCharacters);
        using var normalizedText = new LimitedStringWriter(limits.MaxRecordCharacters);
        var settings = new XmlWriterSettings { OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Fragment };
        using (var raw = XmlWriter.Create(rawText, settings))
        using (var normalized = XmlWriter.Create(normalizedText, settings))
        {
            var initialDepth = reader.Depth;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (reader.Depth > limits.MaxDepth) throw new InvalidDataException("XML depth limit exceeded.");
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        foreach (var writer in new[] { raw, normalized })
                        {
                            writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                            if (reader.MoveToFirstAttribute())
                            {
                                do { writer.WriteAttributeString(reader.Prefix, reader.LocalName, reader.NamespaceURI, reader.Value); }
                                while (reader.MoveToNextAttribute());
                                reader.MoveToElement();
                            }
                            if (reader.IsEmptyElement) writer.WriteEndElement();
                        }
                        break;
                    case XmlNodeType.EndElement:
                        raw.WriteFullEndElement(); normalized.WriteFullEndElement(); break;
                    case XmlNodeType.EntityReference:
                        if (!entities.Contains(reader.Name)) throw new InvalidDataException("Unapproved XML entity.");
                        raw.WriteEntityRef(reader.Name);
                        normalized.WriteString(reader.Name); // Preserve JMdict source codes, not their English labels.
                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        raw.WriteString(reader.Value); normalized.WriteString(reader.Value); break;
                    case XmlNodeType.CDATA:
                        raw.WriteCData(reader.Value); normalized.WriteCData(reader.Value); break;
                    case XmlNodeType.Comment:
                        raw.WriteComment(reader.Value); normalized.WriteComment(reader.Value); break;
                    default:
                        throw new InvalidDataException("Unsupported XML node inside a source record.");
                }
                if ((reader.NodeType == XmlNodeType.EndElement || reader.IsEmptyElement) && reader.Depth == initialDepth)
                    break;
            } while (reader.Read());
        }
        return (XElement.Parse(normalizedText.ToString(), LoadOptions.PreserveWhitespace), rawText.ToString());
    }

    private static string ReadProlog(TextReader source, int maxCharacters)
    {
        var result = new StringBuilder();
        char Next()
        {
            var value = source.Read();
            if (value < 0) throw new InvalidDataException("Truncated XML prolog.");
            if (result.Length >= maxCharacters) throw new InvalidDataException("XML prolog limit exceeded.");
            result.Append((char)value);
            return (char)value;
        }
        void Until(string end)
        {
            while (true)
            {
                Next();
                if (result.Length >= end.Length && result.ToString(result.Length - end.Length, end.Length) == end) return;
            }
        }
        while (true)
        {
            var current = Next();
            if (char.IsWhiteSpace(current)) continue;
            if (current != '<') throw new InvalidDataException("Malformed XML prolog.");
            current = Next();
            if (char.IsLetter(current)) return result.ToString();
            if (current == '?') { Until("?>"); continue; }
            if (current != '!') throw new InvalidDataException("Malformed XML declaration.");
            current = Next();
            if (current == '-')
            {
                if (Next() != '-') throw new InvalidDataException("Malformed XML comment.");
                Until("-->"); continue;
            }
            if (current != 'D') throw new InvalidDataException("Unsupported XML declaration.");
            foreach (var expected in "OCTYPE")
                if (Next() != expected) throw new InvalidDataException("Malformed XML doctype.");
            var depth = 0;
            char quote = '\0';
            while (true)
            {
                current = Next();
                if (quote != '\0') { if (current == quote) quote = '\0'; continue; }
                if (result.Length >= 4 && result.ToString(result.Length - 4, 4) == "<!--") { Until("-->"); continue; }
                if (current is '\'' or '"') { quote = current; continue; }
                if (current == '[') depth++;
                else if (current == ']') depth--;
                else if (current == '>' && depth == 0) break;
                if (depth < 0) throw new InvalidDataException("Malformed XML doctype.");
            }
        }
    }

    private static HashSet<string> ValidateProlog(string prefix, string root,
        IReadOnlyDictionary<string, string> approvedEntities, int maxEntities)
    {
        var withoutComments = Regex.Replace(prefix, "<!--[\\s\\S]*?-->", "", RegexOptions.CultureInvariant, RegexTimeout);
        var declarations = Regex.Matches(withoutComments, "<!DOCTYPE[\\s\\S]*?(?=<(?![!?]))", RegexOptions.CultureInvariant, RegexTimeout);
        if (declarations.Count > 1) throw new InvalidDataException("Multiple XML doctypes.");
        var entities = new HashSet<string>(StringComparer.Ordinal);
        if (declarations.Count == 0) return entities;
        var dtd = declarations[0].Value.Trim();
        var match = Regex.Match(dtd, "^<!DOCTYPE\\s+" + root + "\\s*(?:\\[([\\s\\S]*)\\]\\s*)?>$", RegexOptions.CultureInvariant, RegexTimeout);
        if (!match.Success) throw new InvalidDataException("External or unexpected XML doctype.");
        var subset = match.Groups[1].Value;
        var position = 0;
        var entityDeclarations = 0;
        foreach (Match item in Regex.Matches(subset, "<![^>]*>", RegexOptions.CultureInvariant, RegexTimeout))
        {
            if (!string.IsNullOrWhiteSpace(subset[position..item.Index])) throw new InvalidDataException("Unsupported DTD content.");
            position = item.Index + item.Length;
            var entity = Regex.Match(item.Value, "^<!ENTITY\\s+([A-Za-z][A-Za-z0-9_.-]*)\\s+([\"'])([^<&%]*?)\\2\\s*>$", RegexOptions.CultureInvariant, RegexTimeout);
            if (entity.Success)
            {
                var name = entity.Groups[1].Value;
                if (!approvedEntities.TryGetValue(name, out var description) || description != entity.Groups[3].Value ||
                    ++entityDeclarations > maxEntities)
                    throw new InvalidDataException("DTD entity does not match the pinned entity table.");
                // JMdict's pinned DTD repeats an identical 'ik' declaration. Every occurrence must match the pin.
                entities.Add(name);
            }
            else if (!Regex.IsMatch(item.Value, "^<!(?:ELEMENT|ATTLIST)\\s+[^<&%]+>$", RegexOptions.CultureInvariant, RegexTimeout))
                throw new InvalidDataException("External, nested or unsupported DTD declaration.");
        }
        if (!string.IsNullOrWhiteSpace(subset[position..])) throw new InvalidDataException("Unsupported DTD content.");
        return entities;
    }

    internal sealed class LimitedTextReader(TextReader inner, long limit, CancellationToken cancellationToken,
        int maxNodeCharacters = int.MaxValue) : TextReader
    {
        private long count;
        private int nodeCharacters;
        private readonly StringBuilder nodePrefix = new();
        private char previous;
        private char beforePrevious;
        private bool markup;
        private char quote;
        private string? delimitedEnd;
        public override int Peek() => inner.Peek();
        public override int Read()
        {
            cancellationToken.ThrowIfCancellationRequested();
            var value = inner.Read();
            if (value >= 0 && ++count > limit) throw new InvalidDataException("Expanded source character limit exceeded.");
            if (value >= 0) CheckNode((char)value);
            return value;
        }
        public override int Read(char[] buffer, int index, int count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = inner.Read(buffer, index, (int)Math.Min(count, Math.Max(1, limit - this.count + 1)));
            this.count += read;
            if (this.count > limit) throw new InvalidDataException("Expanded source character limit exceeded.");
            for (var i = index; i < index + read; i++) CheckNode(buffer[i]);
            return read;
        }

        // Bound lexical nodes before XmlTextReader allocates Value for a huge text/comment/attribute node.
        private void CheckNode(char value)
        {
            if (maxNodeCharacters == int.MaxValue) return;
            if (++nodeCharacters > maxNodeCharacters) throw new InvalidDataException("XML lexical node limit exceeded.");
            if (delimitedEnd is not null)
            {
                if (beforePrevious == delimitedEnd[0] && previous == delimitedEnd[1] && value == delimitedEnd[2])
                { markup = false; delimitedEnd = null; nodeCharacters = 0; }
                beforePrevious = previous;
                previous = value;
                return;
            }
            if (!markup)
            {
                if (value == '<') { markup = true; nodeCharacters = 1; nodePrefix.Clear().Append(value); }
                return;
            }
            if (nodePrefix.Length < 9)
            {
                nodePrefix.Append(value);
                if (nodePrefix.Length == 4 && nodePrefix.ToString() == "<!--") { delimitedEnd = "-->"; return; }
                if (nodePrefix.Length == 9 && nodePrefix.ToString() == "<![CDATA[") { delimitedEnd = "]]>"; return; }
            }
            if (quote != '\0') { if (value == quote) quote = '\0'; return; }
            if (value is '\'' or '"') { quote = value; return; }
            if (value == '>') { markup = false; nodeCharacters = 0; }
        }
    }

    private sealed class ReplayTextReader(string prefix, TextReader remainder) : TextReader
    {
        private int position;
        public override int Read() => position < prefix.Length ? prefix[position++] : remainder.Read();
        public override int Read(char[] buffer, int index, int count)
        {
            if (position >= prefix.Length) return remainder.Read(buffer, index, count);
            var copied = Math.Min(count, prefix.Length - position);
            prefix.CopyTo(position, buffer, index, copied);
            position += copied;
            return copied;
        }
    }

    private sealed class LimitedStringWriter(int limit) : StringWriter
    {
        public override void Write(char value) { Check(1); base.Write(value); }
        public override void Write(string? value) { Check(value?.Length ?? 0); base.Write(value); }
        public override void Write(char[] buffer, int index, int count) { Check(count); base.Write(buffer, index, count); }
        public override void Write(ReadOnlySpan<char> value) { Check(value.Length); base.Write(value); }
        private void Check(int count)
        {
            if (GetStringBuilder().Length + count > limit) throw new InvalidDataException("XML record character limit exceeded.");
        }
    }
}
