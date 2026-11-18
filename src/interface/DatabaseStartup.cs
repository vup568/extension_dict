namespace WebApi;

using Infrastructure.Persistence;
using Infrastructure.DictionaryImport;
using Infrastructure.Conjugation;
using Infrastructure.Persistence.Repositories;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

/// <summary>Initialize the existing database before opening HTTP; never expose connection secrets in failure logs.</summary>
public static class DatabaseStartup
{
    public static async Task<bool> InitializeAsync(IServiceProvider services, ILogger logger, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var configuration = services.GetService<IConfiguration>();
            var candidate = context.Database.GetDbConnection().Database.StartsWith("jp_dict_real_", StringComparison.Ordinal);
            var hasCandidateConfiguration = new[] { "LocalTarget", "LocalRunId", "LocalSourceLock", "LocalSourceLockSha256" }
                .Any(key => !string.IsNullOrWhiteSpace(configuration?[$"DictionaryImport:{key}"]));
            if (!candidate && hasCandidateConfiguration)
                throw new InvalidOperationException("Dictionary candidate configuration requires an isolated target.");
            // EARS[Unwanted] FR-009: An isolated target is never served through ordinary startup.
            if (candidate)
            {
                var target = configuration?["DictionaryImport:LocalTarget"];
                var runId = configuration?["DictionaryImport:LocalRunId"];
                var lockPath = configuration?["DictionaryImport:LocalSourceLock"];
                var lockHash = configuration?["DictionaryImport:LocalSourceLockSha256"];
                var connection = context.Database.GetDbConnection();
                if (string.IsNullOrWhiteSpace(target) || target != connection.Database ||
                    string.IsNullOrWhiteSpace(runId) || string.IsNullOrWhiteSpace(lockPath) ||
                    string.IsNullOrWhiteSpace(lockHash))
                    throw new InvalidOperationException("Dictionary candidate requires an explicit local target, run and source lock.");

                var inspection = await SourceLockInspector.VerifyAsync(lockPath, ct);
                if (!inspection.IsReady || inspection.Lock?.Scope != "ja-en" ||
                    !string.Equals(inspection.SourceLockSha256, lockHash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Dictionary candidate source lock refused.");
                // Validate ownership, database identity, pinned source files, checkpoints and facts before HTTP.
                // Validation opens its own connection and does not migrate or modify the candidate.
                var report = await DictionaryImportStore.ValidateAsync(connection.ConnectionString, target, runId, inspection, ct);
                if (report.State != "imported" || !report.VerifiedNoOp)
                    throw new InvalidOperationException("Dictionary candidate has not passed local integrity validation.");
                await VerifyCandidateKnowledgeAsync(context, configuration!, ct);
                logger.LogInformation("Starting the verified Japanese-English dictionary candidate for local use.");
                return true;
            }
            await context.Database.MigrateAsync(ct);
            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            // EARS[Unwanted]: A failed startup is not healthy; log metadata, never raw provider messages/secrets.
            var sqlState = error is PostgresException postgres ? postgres.SqlState : "unavailable";
            logger.LogCritical("Database initialization failed ({ErrorType}, SQLSTATE {SqlState}). Check PostgreSQL, EF migration history and ConnectionStrings__DefaultConnection. Do not reset an existing database to bypass this error.",
                error.GetType().Name, sqlState);
            return false;
        }
    }

    private static async Task VerifyCandidateKnowledgeAsync(AppDbContext context, IConfiguration configuration, CancellationToken ct)
    {
        var version = configuration["ConjugationDatasetVersion"];
        if (string.IsNullOrWhiteSpace(version))
            throw new InvalidOperationException("Candidate conjugation dataset version is required.");
        var dataset = await context.ConjugationDatasets.AsNoTracking().SingleOrDefaultAsync(d => d.Version == version, ct);
        if (dataset is null)
            throw new InvalidOperationException("Candidate conjugation dataset version is unavailable.");
        _ = ConjugationKnowledge.Parse(dataset);

        // Read the project seed as an embedded resource so a missing working-directory file
        // cannot make a different fallback silently count as the expected grammar dataset.
        await using var seedStream = typeof(EfGrammarRepository).Assembly.GetManifestResourceStream(
            "Infrastructure.Persistence.SeedData.grammar_rules_seed.json")
            ?? throw new InvalidOperationException("Grammar seed resource is unavailable.");
        var seed = await JsonSerializer.DeserializeAsync<List<GrammarSeed>>(seedStream,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }, ct);
        if (seed is null || seed.Count == 0 || seed.Any(s => string.IsNullOrWhiteSpace(s.CanonicalId)))
            throw new InvalidOperationException("Grammar seed resource is invalid.");
        // This existing repository seeds from the project seed if the table is empty. It catches
        // write failures internally, so always verify the rows after the attempted bootstrap.
        var actual = await new EfGrammarRepository(context).GetAllActiveRulesAsync(ct);
        if (actual.Count != seed.Count || seed.Any(expected =>
            actual.SingleOrDefault(rule => rule.CanonicalId == expected.CanonicalId) is not { } rule ||
            rule.Pattern != expected.Pattern || rule.JlptLevel != expected.JlptLevel ||
            rule.MeaningVi != expected.MeaningVi || rule.MeaningEn != expected.MeaningEn ||
            rule.Formation != expected.Formation || !JsonEqual(rule.MatcherMetadata, expected.MatcherMetadata)))
            throw new InvalidOperationException("Candidate grammar bootstrap verification failed.");
    }

    private static bool JsonEqual(string? left, string? right) =>
        left is null || right is null ? left == right : JsonNode.DeepEquals(JsonNode.Parse(left), JsonNode.Parse(right));

    private sealed record GrammarSeed(string CanonicalId, string Pattern, string? JlptLevel,
        string? MeaningVi, string? MeaningEn, string? Formation, string? MatcherMetadata);
}
