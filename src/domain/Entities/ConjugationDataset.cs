namespace Domain.Entities;

/// <summary>Immutable, independently versioned development conjugation knowledge.</summary>
public sealed class ConjugationDataset
{
    public string Version { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
