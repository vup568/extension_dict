namespace Application.DictionaryImport;

/// <summary>Persistence boundary for an already inspected isolated local run.</summary>
public interface IDictionaryImportStore
{
    Task<ImportMechanismReport> ImportAsync(DictionaryImportInvocation invocation, SourceLockInspection inputs, CancellationToken ct);
    Task<ImportMechanismReport> ValidateAsync(DictionaryImportInvocation invocation, SourceLockInspection inputs, CancellationToken ct);
}

public sealed class ExecuteDictionaryImportUseCase(IDictionaryImportStore store)
{
    public async Task<DictionaryImportCommandResult> ExecuteAsync(DictionaryImportInvocation invocation,
        SourceLockInspection inputs, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        // EARS[Unwanted] FR-007/008: Implementation authorization never substitutes source or semantic review.
        if (!inputs.IsReady || string.IsNullOrWhiteSpace(invocation.RunId))
            throw new InvalidOperationException("IMPORT_INPUTS_NOT_CLEARED");
        if (invocation.BatchSize is < 1 or > 2000)
            throw new ArgumentOutOfRangeException(nameof(invocation));
        var report = invocation.Verb switch
        {
            "import" or "resume" => await store.ImportAsync(invocation, inputs, ct),
            "validate" => await store.ValidateAsync(invocation, inputs, ct),
            _ => throw new ArgumentException("Unsupported canonical import operation.", nameof(invocation))
        };
        // EARS[State] FR-009/013: Fixture/mechanism validation cannot activate an unreviewed local candidate.
        return new(invocation.Verb == "validate" && !report.ReadyForLocalUse ? 5 : 0, report);
    }
}

public sealed record ImportMechanismReport(string RunId, string TargetDatabase, string State, string SourceLockHash,
    string FingerprintHash, string? FactsHash, long DictionaryEntries, long KanjiCharacters, bool VerifiedNoOp,
    bool ReadyForLocalUse, string[] PendingGates);

public sealed record ImportBatchProgress(string SourceId, long AcceptedRecords);
