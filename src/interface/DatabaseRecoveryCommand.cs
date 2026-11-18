namespace WebApi;

using Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Npgsql;

/// <summary>Explicit local operator command only; never invoked by normal startup or exposed over HTTP.</summary>
public static class DatabaseRecoveryCommand
{
    public static async Task<int> ExecuteAsync(IConfiguration configuration, TextWriter output, CancellationToken ct = default)
    {
        try
        {
            if (configuration["LocalDatabaseRecoveryConfirmed"] != "1")
                throw new InvalidOperationException("Explicit recovery confirmation is required.");
            var target = configuration.GetConnectionString("DefaultConnection");
            var reference = configuration["LegacySchemaReferenceConnection"];
            if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(reference))
                throw new InvalidOperationException("Both local database configurations are required.");
            var targetOptions = new NpgsqlConnectionStringBuilder(target);
            var referenceOptions = new NpgsqlConnectionStringBuilder(reference);
            // EARS[Unwanted]: Operator recovery cannot target a remote server or treat the original DB as its reference.
            static bool IsLoopback(string? host) => host is "127.0.0.1" or "localhost" or "::1";
            if (!IsLoopback(targetOptions.Host) || !IsLoopback(referenceOptions.Host)
                || targetOptions.Host != referenceOptions.Host || targetOptions.Port != referenceOptions.Port
                || targetOptions.Username != referenceOptions.Username || targetOptions.Password != referenceOptions.Password
                || targetOptions.Database == referenceOptions.Database
                || referenceOptions.Database is not { } referenceName
                || !System.Text.RegularExpressions.Regex.IsMatch(referenceName, "^jp_recovery_reference_[a-f0-9]{32}$"))
                throw new InvalidOperationException("Recovery requires distinct databases on the same local Compose server.");

            await LegacySchemaNormalizer.NormalizeAndBaselineAsync(target, reference, ct);
            await output.WriteLineAsync("Legacy baseline validated and recorded. Run the existing pending migrations next.");
            return 0;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception error)
        {
            // Only bounded metadata: never connection strings, raw provider messages, row values or full exceptions.
            await output.WriteLineAsync($"Local database recovery refused or failed ({error.GetType().Name}). Check the verified backup, reference schema and recovery configuration.");
            if (error.Data["LegacyValidationCheck"] is int check)
                await output.WriteLineAsync($"Schema validation check: {check}.");
            return 1;
        }
    }
}
