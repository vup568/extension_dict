using Application.DictionaryImport;

namespace Infrastructure.DictionaryImport;

/// <summary>Connection configuration stays at the infrastructure boundary.</summary>
public sealed class IsolatedDictionaryImportStore(string connectionString, string targetDatabase) : IDictionaryImportStore
{
    public Task<ImportMechanismReport> ImportAsync(DictionaryImportInvocation invocation, SourceLockInspection inputs, CancellationToken ct) =>
        DictionaryImportStore.ImportAsync(connectionString, targetDatabase, invocation.RunId!, inputs,
            invocation.Verb == "resume", invocation.BatchSize, ct);

    public Task<ImportMechanismReport> ValidateAsync(DictionaryImportInvocation invocation, SourceLockInspection inputs, CancellationToken ct) =>
        DictionaryImportStore.ValidateAsync(connectionString, targetDatabase, invocation.RunId!, inputs, ct);

    public Task<ImportMechanismReport> UpgradeSchemaAsync(DictionaryImportInvocation invocation, SourceLockInspection inputs, CancellationToken ct) =>
        DictionaryImportStore.UpgradeSchemaAsync(connectionString, targetDatabase, invocation.RunId!, inputs, ct);
}
