namespace Domain.Entities;

/// <summary>Local isolated import control; never a published KnowledgeRelease.</summary>
public sealed class DictionaryImportRun
{
    public string RunId { get; set; } = string.Empty;
    public string TargetDatabase { get; set; } = string.Empty;
    public long DatabaseOid { get; set; }
    public string SourceLockHash { get; set; } = string.Empty;
    public string FingerprintHash { get; set; } = string.Empty;
    public string State { get; set; } = "new";
    public string? FactsHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<DictionaryImportCheckpoint> Checkpoints { get; set; } = [];
}

public sealed class DictionaryImportCheckpoint
{
    public string RunId { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public long ManifestId { get; set; }
    public long AcceptedRecords { get; set; }
    public string LastSourceIdentity { get; set; } = string.Empty;
    public string BatchChainHash { get; set; } = string.Empty;
    public DictionaryImportRun Run { get; set; } = null!;
    public SourceManifest Manifest { get; set; } = null!;
}
