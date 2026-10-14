namespace Application.Conjugation.Models;

/// <summary>Versioned, provider-neutral full-word grammar; never a surface-to-answer table.</summary>
public sealed record ConjugationParadigm(
    string RuleId, int RuleVersion, string CanonicalSuffix, string? RequiredBase,
    IReadOnlyList<string> ExcludedBases, IReadOnlyList<string> DictionaryPartOfSpeech,
    string? ExplanationVi, IReadOnlyList<ConjugationComponentPattern> Components,
    IReadOnlyList<string>? HeadBaseTemplates = null, IReadOnlyList<string>? HeadCategories = null);

public sealed record ConjugationComponentPattern(string SurfaceTemplate, string? ExplanationVi);

/// <summary>Internal proposal. A dictionary class/applicable sense must verify it before presentation.</summary>
public sealed record ConjugationCandidate(
    ConjugationExplanationDto Detail, IReadOnlyList<string> DictionaryPartOfSpeech);
