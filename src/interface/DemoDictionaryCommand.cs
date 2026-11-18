namespace WebApi;

using Infrastructure.Demo;
using Microsoft.Extensions.Configuration;

/// <summary>Opt-in local demo provisioning. Never called by HTTP or normal backend startup.</summary>
public static class DemoDictionaryCommand
{
    public static async Task<int> ExecuteAsync(IConfiguration configuration, TextWriter output, CancellationToken ct = default)
    {
        try
        {
            if (configuration["LocalDemoConfirmed"] != "1")
                throw new InvalidOperationException("Explicit demo confirmation is required.");
            var connection = configuration.GetConnectionString("DefaultConnection");
            var path = configuration["DemoCorpusPath"];
            if (string.IsNullOrWhiteSpace(connection) || string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Demo configuration is required.");

            var corpusBytes = await File.ReadAllBytesAsync(path, ct);
            var count = await DemoDictionarySeeder.SeedAsync(connection, corpusBytes, ct);
            await output.WriteLineAsync($"Demo jp_dict_demo ready: {count} synthetic dictionary fixtures. Not a real dictionary import.");
            return 0;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception error)
        {
            // EARS[Unwanted]: Provider messages, paths and connection strings must not expose secrets or fixture rows.
            await output.WriteLineAsync($"Local demo refused or failed ({error.GetType().Name}). Check the approved corpus and isolated demo configuration; existing data is not reset.");
            return 1;
        }
    }
}
