namespace WebApi;

using System.Text.Json;
using Application.DictionaryImport;
using Infrastructure.DictionaryImport;
using Npgsql;

/// <summary>Operator-only dispatch, before creating the web host, DI container or configuration providers.</summary>
public static class DictionaryImportCommand
{
    public static async Task<DictionaryImportCommandResult> ExecuteStoreAsync(DictionaryImportInvocation invocation,
        SourceLockInspection inspection, CancellationToken ct)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection))
            return new(2, new { error_code = "missing_connection", message = "The operator connection environment variable is required.", run_id = invocation.RunId });
        // Re-read every pinned byte immediately before crossing the database boundary. The writer also holds each artifact handle.
        var fresh = await SourceLockInspector.VerifyAsync(invocation.SourceLockPath, ct);
        if (!fresh.IsReady || fresh.SourceLockSha256 != inspection.SourceLockSha256 || fresh.FingerprintSha256 != inspection.FingerprintSha256)
            return new(3, new { error_code = "inputs_changed", message = "Pinned inputs changed after inspection.", run_id = invocation.RunId });
        var target = invocation.ConfirmTarget ?? new NpgsqlConnectionStringBuilder(connection).Database;
        if (string.IsNullOrWhiteSpace(target))
            return new(2, new { error_code = "missing_target", message = "The isolated target database is required.", run_id = invocation.RunId });
        if (invocation.Verb == "prepare")
        {
            // EARS[Unwanted] FR-009: Preparation creates only an explicitly confirmed fresh loopback target.
            if (string.IsNullOrWhiteSpace(invocation.ConfirmTarget))
                return new(2, new { error_code = "missing_target_confirmation", message = "Preparation requires the exact new target database name.", run_id = invocation.RunId });
            IsolatedImportTarget.Check(connection, target);
            await DictionaryImportStore.PrepareAsync(connection, target, invocation.RunId!, fresh, ct);
            return new(0, new
            {
                run_id = invocation.RunId,
                target_database = target,
                state = "prepared",
                source_lock_sha256 = fresh.SourceLockSha256,
                fingerprint_sha256 = fresh.FingerprintSha256,
                ready_for_local_use = false
            });
        }
        var useCase = new ExecuteDictionaryImportUseCase(new IsolatedDictionaryImportStore(connection, target));
        return await useCase.ExecuteAsync(invocation, fresh, ct);
    }

    public static bool IsRequested(string[] args) => args.Contains("--dictionary-import", StringComparer.Ordinal)
        || args.Contains("--help", StringComparer.Ordinal);

    public static async Task<int> ExecuteAsync(string[] args, TextWriter output,
        Func<DictionaryImportInvocation, SourceLockInspection, CancellationToken, Task<DictionaryImportCommandResult>>? execute = null,
        CancellationToken ct = default)
    {
        string? runId = null;
        try
        {
            if (args.Length == 1 && args[0] == "--help"
                || args.Length == 2 && args[0] == "--dictionary-import" && args[1] is "--help" or "help")
            {
                await output.WriteLineAsync("Dictionary import (offline; no automatic fetch, preparation, reset or activation):\n"
                    + "  --dictionary-import inspect --source-lock <file>\n"
                    + "  --dictionary-import prepare --source-lock <file> --run-id <id> --confirm-target <exact-new-db-name>\n"
                    + "  --dictionary-import import|resume --source-lock <file> --run-id <id> --confirm-target <exact-db-name> [--batch-size 1..2000]\n"
                    + "  --dictionary-import validate --source-lock <file> --run-id <id>\n"
                    + "Connection: environment ConnectionStrings__DefaultConnection. Inspect is read-only. Prepare explicitly creates a new isolated database; it does not import or activate data. Canonical operations require approved source/license/mapping pins.");
                return 0;
            }
            if (args.Length < 2 || args[0] != "--dictionary-import" || args[1] is not ("inspect" or "prepare" or "import" or "resume" or "validate"))
                return await Error(2, "invalid_command", "Expected inspect, prepare, import, resume, validate or --help.");

            var verb = args[1];
            var options = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 2; index < args.Length; index += 2)
            {
                if (index + 1 >= args.Length || args[index] is not ("--source-lock" or "--run-id" or "--confirm-target" or "--batch-size")
                    || args[index + 1].StartsWith("--", StringComparison.Ordinal) || !options.TryAdd(args[index], args[index + 1]))
                    return await Error(2, "invalid_arguments", "Expected unique supported options with explicit values.");
            }
            options.TryGetValue("--run-id", out runId);
            if (runId is not null && (runId.Length > 64 || runId.Length == 0 || runId.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '_' and not '-')))
            {
                runId = null;
                return await Error(2, "invalid_run_id", "Run identity must be a bounded ASCII identifier.");
            }
            if (!options.TryGetValue("--source-lock", out var lockPath) || string.IsNullOrWhiteSpace(lockPath)
                || verb != "inspect" && runId is null)
                return await Error(2, "missing_arguments", "The command requires a source lock and, for database operations, a run identity.");
            if (verb is "prepare" or "import" or "resume" &&
                (!options.TryGetValue("--confirm-target", out var confirmedTarget) || string.IsNullOrWhiteSpace(confirmedTarget)))
                return await Error(2, "missing_target_confirmation", "Prepare, import and resume require the exact target database name.");
            if (verb is "inspect" && options.Keys.Any(key => key != "--source-lock")
                || verb is "validate" && options.Keys.Any(key => key is not ("--source-lock" or "--run-id"))
                || verb is "prepare" && options.Keys.Any(key => key is not ("--source-lock" or "--run-id" or "--confirm-target")))
                return await Error(2, "invalid_arguments", "This command does not accept write-operation options.");
            var batchSize = 500;
            if (options.TryGetValue("--batch-size", out var batch) && (!int.TryParse(batch, out batchSize) || batchSize is < 1 or > 2000))
                return await Error(2, "invalid_batch_size", "Batch size must be between 1 and 2000 records.");

            var inspection = await SourceLockInspector.VerifyAsync(lockPath, ct);
            if (verb == "inspect" || !inspection.IsReady)
            {
                await Write(new
                {
                    schema_version = 1,
                    run_id = runId,
                    state = inspection.IsReady ? "inspected" : "blocked",
                    source_lock_sha256 = inspection.SourceLockSha256,
                    fingerprint_sha256 = inspection.FingerprintSha256,
                    // A complete source lock is not proof that all source kinds can be written,
                    // semantically reviewed, validated, and activated.
                    source_inputs_ready = inspection.IsReady,
                    canonical_ready = false,
                    implementation_blockers = inspection.Lock?.Scope == "ja-en"
                        ? new[] { "independent_validation_and_activation_pending" }
                        : new[] { "reviewed_mapping_writer_not_implemented", "independent_validation_and_activation_pending" },
                    blockers = inspection.Blockers
                });
                return inspection.IsReady ? 0 : 3;
            }
            if (execute is null)
                return await Error(4, "operation_unavailable", "No canonical operation is configured; no database was opened.");
            options.TryGetValue("--confirm-target", out var target);
            var result = await execute(new(verb, lockPath, runId, target, batchSize), inspection, ct);
            await Write(result.Report);
            return result.ExitCode;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return await Error(130, "cancelled", "The operator command was cancelled.");
        }
        catch (InvalidOperationException error) when (error.Message.StartsWith("IMPORT_", StringComparison.Ordinal)
            && error.Message.Length <= 100 && error.Message.All(c => c is >= 'A' and <= 'Z' or '_'))
        {
            // Only bounded internal error identifiers may cross the console boundary; provider messages never do.
            var code = error.Message.Contains("TARGET", StringComparison.Ordinal) || error.Message.Contains("BATCH_SIZE", StringComparison.Ordinal) ? 2
                : error.Message.Contains("ARTIFACT", StringComparison.Ordinal) || error.Message.Contains("PINNED_FILE", StringComparison.Ordinal)
                    || error.Message.Contains("INPUTS", StringComparison.Ordinal) || error.Message.Contains("MAPPING_REQUIRED", StringComparison.Ordinal) ? 3
                : args.Length > 1 && args[1] == "validate" ? 5 : 4;
            return await Error(code, error.Message.ToLowerInvariant(), "The requested canonical operation was refused by its safety or integrity guard.");
        }
        catch (Exception)
        {
            // EARS[Unwanted]: FR-009/013 excludes provider errors, paths, connection strings and source data from console errors.
            return await Error(4, "operator_failed", "The operator command failed; inspect the approved local evidence and target state.");
        }

        async Task Write(object value) => await output.WriteLineAsync(JsonSerializer.Serialize(value, SourceLockInspector.JsonOptions));
        async Task<int> Error(int code, string errorCode, string message)
        {
            await Write(new { error_code = errorCode, message, run_id = runId });
            return code;
        }
    }
}
