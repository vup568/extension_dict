using System.Text.Json;
using Application.Conjugation.Models;
using Domain.Entities;

namespace Infrastructure.Conjugation;

/// <summary>Provider normalization and schema validation stay outside the business engine.</summary>
public sealed class ConjugationKnowledge
{
    private readonly IReadOnlyList<ProviderMapping> categoryMappings;
    private readonly IReadOnlyList<ProviderMapping> inflectionMappings;

    private ConjugationKnowledge(
        IReadOnlyList<ConjugationRulePattern> rules,
        IReadOnlyList<ProviderMapping> categories, IReadOnlyList<ProviderMapping> inflections,
        IReadOnlyList<ConjugationParadigm>? paradigms = null)
    {
        Rules = rules;
        categoryMappings = categories;
        inflectionMappings = inflections;
        Paradigms = paradigms;
    }

    public IReadOnlyList<ConjugationRulePattern> Rules { get; }
    public IReadOnlyList<ConjugationParadigm>? Paradigms { get; }

    public static ConjugationKnowledge Parse(ConjugationDataset dataset)
    {
        try
        {
            using var document = JsonDocument.Parse(dataset.Content);
            var root = document.RootElement;
            if (root.GetProperty("schema_version").GetInt32() != 1 ||
                RequiredString(root, "version") != dataset.Version ||
                RequiredString(root.GetProperty("provenance"), "status") != "development")
                throw new FormatException("Invalid conjugation dataset schema or identity.");

            var sources = root.GetProperty("provenance").GetProperty("sources");
            if (sources.ValueKind != JsonValueKind.Array || sources.GetArrayLength() == 0 ||
                sources.EnumerateArray().Any(x => x.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(x.GetString())))
                throw new FormatException("Conjugation knowledge requires provenance.");

            var mapping = root.GetProperty("provider_mappings");
            var categories = ReadMappings(mapping.GetProperty("categories"));
            var inflections = ReadMappings(mapping.GetProperty("inflections"));
            if (root.TryGetProperty("paradigms", out var paradigmData))
                return new([], categories, inflections, ReadParadigms(paradigmData));
            var rules = new List<ConjugationRulePattern>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var ruleData = root.GetProperty("rules");
            if (ruleData.ValueKind != JsonValueKind.Array || ruleData.GetArrayLength() == 0)
                throw new FormatException("Conjugation dataset requires rule patterns.");
            foreach (var rule in ruleData.EnumerateArray())
            {
                var id = RequiredString(rule, "rule_id");
                var version = rule.GetProperty("rule_version").GetInt32();
                if (!ids.Add(id) || version <= 0)
                    throw new FormatException("Conjugation rule identities must be unique and versioned.");
                var tokens = new List<ConjugationRuleTokenPattern>();
                foreach (var token in rule.GetProperty("tokens").EnumerateArray())
                {
                    var category = RequiredString(token, "category");
                    var forms = token.GetProperty("inflections").EnumerateArray()
                        .Select(x => x.GetString()).ToArray();
                    if (!categories.Any(x => x.Value == category) || forms.Length == 0 ||
                        forms.Any(x => string.IsNullOrWhiteSpace(x) || !inflections.Any(i => i.Value == x)) ||
                        forms.Distinct(StringComparer.Ordinal).Count() != forms.Length)
                        throw new FormatException("Rule references unsupported normalized labels.");
                    tokens.Add(new(category, forms.Select(x => x!).ToArray(), OptionalString(token, "explanation_vi")));
                }
                if (tokens.Count == 0)
                    throw new FormatException("Empty conjugation pattern.");
                rules.Add(new(id, version, OptionalString(rule, "explanation_vi"), tokens));
            }
            return new(rules.ToArray(), categories, inflections);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or
            KeyNotFoundException or ArgumentException or OverflowException)
        {
            throw new FormatException("Invalid conjugation knowledge document.", exception);
        }
    }

    public ConjugationToken Normalize(
        string surface, string baseForm, IReadOnlyList<string> pos, int start, int end)
    {
        // EARS[Ubiquitous]: Interpret raw labels only using the pinned knowledge mappings.
        var category = Resolve(categoryMappings, baseForm, pos);
        var inflection = Resolve(inflectionMappings, baseForm, pos);
        return new(category, inflection, surface, baseForm, start, end);
    }

    private static string Resolve(IReadOnlyList<ProviderMapping> mappings, string baseForm, IReadOnlyList<string> pos) =>
        pos.Count == 6
            ? mappings.FirstOrDefault(x => (x.BaseForm is null || x.BaseForm == baseForm) &&
                x.Conditions.All(c => c.EqualsValue is not null
                    ? pos[c.Index] == c.EqualsValue
                    : pos[c.Index].StartsWith(c.Prefix!, StringComparison.Ordinal)))?.Value ?? "unknown"
            : "unknown";

    private static IReadOnlyList<ProviderMapping> ReadMappings(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Array || data.GetArrayLength() == 0)
            throw new FormatException("Missing provider mappings.");
        var result = new List<ProviderMapping>();
        foreach (var item in data.EnumerateArray())
        {
            var value = RequiredString(item, "value");
            var conditions = new List<ProviderCondition>();
            foreach (var condition in item.GetProperty("conditions").EnumerateArray())
            {
                var index = condition.GetProperty("index").GetInt32();
                var equalsValue = OptionalString(condition, "equals");
                var prefix = OptionalString(condition, "prefix");
                if (index is < 0 or > 5 || (equalsValue is null) == (prefix is null) ||
                    equalsValue is "" || prefix is "")
                    throw new FormatException("Invalid provider mapping condition.");
                conditions.Add(new(index, equalsValue, prefix));
            }
            if (conditions.Count == 0)
                throw new FormatException("Provider mapping must constrain observed metadata.");
            result.Add(new(value, OptionalString(item, "base_form"), conditions.ToArray()));
        }
        return result.ToArray();
    }

    private static string RequiredString(JsonElement data, string property) =>
        OptionalString(data, property) is { } value && !string.IsNullOrWhiteSpace(value)
            ? value : throw new FormatException($"Missing conjugation knowledge field: {property}.");

    private static IReadOnlyList<ConjugationParadigm> ReadParadigms(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Array || data.GetArrayLength() == 0 || data.GetArrayLength() > 2048)
            throw new FormatException("Missing or oversized conjugation paradigms.");
        var result = new List<ConjugationParadigm>();
        var versions = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var item in data.EnumerateArray())
        {
            var id = RequiredString(item, "rule_id");
            var version = item.GetProperty("rule_version").GetInt32();
            if (version <= 0 || (versions.TryGetValue(id, out var prior) && prior != version))
                throw new FormatException("Conflicting paradigm rule identity.");
            versions[id] = version;
            var suffix = OptionalString(item, "canonical_suffix") ?? throw new FormatException("Missing canonical suffix.");
            var pos = item.GetProperty("dictionary_pos").EnumerateArray().Select(x => x.GetString()).ToArray();
            if (pos.Length == 0 || pos.Any(string.IsNullOrWhiteSpace) || pos.Distinct(StringComparer.Ordinal).Count() != pos.Length)
                throw new FormatException("Paradigms require canonical dictionary class evidence.");
            var components = item.GetProperty("components").EnumerateArray().Select(c =>
                new ConjugationComponentPattern(RequiredString(c, "surface_template"), OptionalString(c, "explanation_vi"))).ToArray();
            if (components.Length == 0 || !components[0].SurfaceTemplate.StartsWith("{stem}", StringComparison.Ordinal) ||
                components.Sum(c => c.SurfaceTemplate.Split("{stem}", StringSplitOptions.None).Length - 1) != 1)
                throw new FormatException("A paradigm requires exactly one initial stem placeholder.");
            var exclusions = item.GetProperty("excluded_bases").EnumerateArray().Select(x => x.GetString()!).ToArray();
            if (exclusions.Any(string.IsNullOrWhiteSpace)) throw new FormatException("Blank lexical exception.");
            var heads = item.GetProperty("head_base_templates").EnumerateArray().Select(x => x.GetString()!).ToArray();
            var headCategories = item.GetProperty("head_categories").EnumerateArray().Select(x => x.GetString()!).ToArray();
            if (heads.Length == 0 || heads.Any(string.IsNullOrWhiteSpace) || headCategories.Length == 0 ||
                headCategories.Any(string.IsNullOrWhiteSpace)) throw new FormatException("Missing observed lexical-head constraints.");
            result.Add(new(id, version, suffix, OptionalString(item, "required_base"), exclusions,
                pos.Select(x => x!).ToArray(), OptionalString(item, "explanation_vi"), components, heads, headCategories));
        }
        return result;
    }

    private static string? OptionalString(JsonElement data, string property) =>
        data.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.GetString() : null;

    private sealed record ProviderMapping(string Value, string? BaseForm, IReadOnlyList<ProviderCondition> Conditions);
    private sealed record ProviderCondition(int Index, string? EqualsValue, string? Prefix);
}
