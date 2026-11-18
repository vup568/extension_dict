using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.DictionaryImport;
using FluentAssertions;
using Infrastructure;
using Infrastructure.DictionaryImport;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IntegrationTests;

public sealed class DatabaseStartupFailureTests
{
    [Fact]
    public async Task CandidateTargetCannotBeServedOrMigratedByNormalStartup()
    {
        // An unreachable port proves the candidate guard executes before connection/migration.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = "127.0.0.1", Port = 1, Database = "jp_dict_real_not_approved", Timeout = 1
            }.ConnectionString).Options;
        var services = new ServiceCollection();
        services.AddSingleton(new AppDbContext(options));
        using var provider = services.BuildServiceProvider();
        var logger = new StartupLog();
        (await WebApi.DatabaseStartup.InitializeAsync(provider, logger, TestContext.Current.CancellationToken)).Should().BeFalse();
        logger.Messages.Should().ContainSingle().Which.Should().Contain("InvalidOperationException");
    }

    [Fact]
    public async Task CandidateStartupRefusesAnIncorrectExplicitTargetBeforeConnecting()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(
            "Host=127.0.0.1;Port=1;Database=jp_dict_real_expected;Timeout=1").Options;
        var services = new ServiceCollection();
        services.AddSingleton(new AppDbContext(options));
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DictionaryImport:LocalTarget"] = "jp_dict_real_other",
            ["DictionaryImport:LocalRunId"] = "run",
            ["DictionaryImport:LocalSourceLock"] = "not-read.json",
            ["DictionaryImport:LocalSourceLockSha256"] = new string('a', 64)
        }).Build());
        using var provider = services.BuildServiceProvider();
        var logger = new StartupLog();
        (await WebApi.DatabaseStartup.InitializeAsync(provider, logger, TestContext.Current.CancellationToken)).Should().BeFalse();
        logger.Messages.Should().ContainSingle().Which.Should().Contain("InvalidOperationException");
    }

    [Theory]
    [InlineData("LocalTarget")]
    [InlineData("LocalRunId")]
    [InlineData("LocalSourceLock")]
    [InlineData("LocalSourceLockSha256")]
    public async Task CandidateSettingsCannotStartOrMigrateTheOriginalDatabase(string configuredKey)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(
            "Host=127.0.0.1;Port=1;Database=app_db;Timeout=1").Options;
        var services = new ServiceCollection();
        services.AddSingleton(new AppDbContext(options));
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"DictionaryImport:{configuredKey}"] = "set"
        }).Build());
        using var provider = services.BuildServiceProvider();
        var logger = new StartupLog();
        (await WebApi.DatabaseStartup.InitializeAsync(provider, logger, TestContext.Current.CancellationToken)).Should().BeFalse();
        // An unreachable port would have yielded NpgsqlException if the guard had not stopped before migration.
        logger.Messages.Should().ContainSingle().Which.Should().Contain("InvalidOperationException");
    }

    [Fact]
    public async Task StartupFailureStopsInitializationAndDoesNotExposeProviderMessages()
    {
        string privateSentinel = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddTransient<AppDbContext>(_ => throw new InvalidOperationException(privateSentinel));
        using var provider = services.BuildServiceProvider(); var logger = new StartupLog();
        (await WebApi.DatabaseStartup.InitializeAsync(provider, logger, TestContext.Current.CancellationToken)).Should().BeFalse();
        logger.Messages.Should().ContainSingle().Which.Should().Contain("ConnectionStrings__DefaultConnection").And.NotContain(privateSentinel);
        logger.Exceptions.Should().OnlyContain(e => e == null);
    }

    [Fact]
    public async Task AnUnreachablePostgresDatabaseCannotInitializeSuccessfully()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["ConnectionStrings:DefaultConnection"] = "Host=127.0.0.1;Port=1;Database=isolated-startup-probe;Timeout=1" }).Build();
        var services = new ServiceCollection(); services.AddInfrastructureServices(configuration);
        using var provider = services.BuildServiceProvider(); var logger = new StartupLog();
        (await WebApi.DatabaseStartup.InitializeAsync(provider, logger, TestContext.Current.CancellationToken)).Should().BeFalse();
        logger.Messages.Should().ContainSingle().Which.Should().Contain("Database initialization failed");
    }

    [Fact]
    public async Task CancelledStartupDoesNotResolveOrConnectToDatabase()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel(); var logger = new StartupLog();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WebApi.DatabaseStartup.InitializeAsync(provider, logger, cancelled.Token));
        logger.Messages.Should().BeEmpty();
    }
}

public sealed class DatabaseStartupHttpTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task ExplicitCandidateRequiresTheExactLockAndRejectsTamperedData()
    {
        using var source = new StartupSourceFixture();
        var ct = TestContext.Current.CancellationToken;
        var inspection = await SourceLockInspector.VerifyAsync(source.LockPath, ct);
        inspection.IsReady.Should().BeTrue();
        var target = "jp_dict_real_" + Guid.NewGuid().ToString("N");
        var connection = new Npgsql.NpgsqlConnectionStringBuilder(fixture.ConnectionString) { Database = target }.ConnectionString;
        await DictionaryImportStore.PrepareAsync(connection, target, "startup", inspection, ct);
        await DictionaryImportStore.ImportAsync(connection, target, "startup", inspection, ct: ct);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DictionaryImport:LocalTarget"] = target,
            ["DictionaryImport:LocalRunId"] = "startup",
            ["DictionaryImport:LocalSourceLock"] = source.LockPath,
            ["DictionaryImport:LocalSourceLockSha256"] = inspection.SourceLockSha256,
            ["ConjugationDatasetVersion"] = "candidate-20261013.1"
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddScoped(_ => new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options));
        using var provider = services.BuildServiceProvider();
        (await WebApi.DatabaseStartup.InitializeAsync(provider, new StartupLog(), ct)).Should().BeTrue();
        await using (var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options))
        {
            (await db.GrammarRules.CountAsync(ct)).Should().Be(5);
        }
        configuration["ConjugationDatasetVersion"] = "missing-version";
        (await WebApi.DatabaseStartup.InitializeAsync(provider, new StartupLog(), ct)).Should().BeFalse();
        configuration["ConjugationDatasetVersion"] = "candidate-20261013.1";

        await using (var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options))
        {
            var rule = await db.GrammarRules.FirstAsync(ct);
            var originalPattern = rule.Pattern;
            rule.Pattern = "changed grammar";
            await db.SaveChangesAsync(ct);
            (await WebApi.DatabaseStartup.InitializeAsync(provider, new StartupLog(), ct)).Should().BeFalse();
            rule.Pattern = originalPattern;
            await db.SaveChangesAsync(ct);
        }

        // Mutating an imported fact invalidates the bound candidate before the HTTP host opens.
        await using (var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options))
        {
            var gloss = await db.LocalizedGlosses.SingleAsync(ct);
            gloss.GlossText = "tampered";
            await db.SaveChangesAsync(ct);
        }
        (await WebApi.DatabaseStartup.InitializeAsync(provider, new StartupLog(), ct)).Should().BeFalse();
        configuration["DictionaryImport:LocalSourceLockSha256"] = new string('f', 64);
        (await WebApi.DatabaseStartup.InitializeAsync(provider, new StartupLog(), ct)).Should().BeFalse();
    }

    [Fact]
    public async Task SuccessfulIsolatedDatabaseInitializationMakesTheOriginalAnalysisBoundaryUsable()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["ConnectionStrings:DefaultConnection"] = fixture.ConnectionString;
        WebApi.ApiComposition.RegisterServices(builder);
        await using var app = builder.Build(); var logger = new StartupLog();
        (await WebApi.DatabaseStartup.InitializeAsync(app.Services, logger, TestContext.Current.CancellationToken)).Should().BeTrue();
        WebApi.ApiComposition.MapEndpoints(app); app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var client = new HttpClient { BaseAddress = new(app.Urls.Single()) };
        (await client.GetAsync("/health/live", TestContext.Current.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        client.DefaultRequestHeaders.Add("X-Contract-Version", "3");
        var response = await client.PostAsJsonAsync("/api/analysis", new { text = "裁判官", interaction_id = Guid.NewGuid().ToString() }, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    private sealed class StartupSourceFixture : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), "jp-startup-" + Guid.NewGuid().ToString("N"));
        public string LockPath => Path.Combine(root, "source-lock.json");

        public StartupSourceFixture()
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "schema.txt"), "fixture schema", new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(root, "license.txt"), "fixture licence", new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(root, "jmdict.xml"), "<JMdict><entry><ent_seq>1</ent_seq><r_ele><reb>かな</reb></r_ele><sense><gloss>fixture</gloss></sense></entry></JMdict>", new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(root, "kanjidic2.xml"), "<kanjidic2><header><file_version>fixture</file_version></header><character><literal>日</literal><codepoint><cp_value cp_type=\"ucs\">65e5</cp_value></codepoint><reading_meaning><rmgroup><meaning>sun</meaning></rmgroup></reading_meaning></character></kanjidic2>", new UTF8Encoding(false));
            string Hash(string name) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, name))));
            DictionarySourcePin Pin(string kind) => new()
            {
                SourceId = kind, Kind = kind, Publisher = "fixture", Version = "fixture-1", ArtifactUrl = "https://example.invalid/fixture",
                ArtifactPath = kind + ".xml", ArtifactBytes = new FileInfo(Path.Combine(root, kind + ".xml")).Length,
                ExpandedBytes = new FileInfo(Path.Combine(root, kind + ".xml")).Length,
                MaxExpandedBytes = new FileInfo(Path.Combine(root, kind + ".xml")).Length,
                ArtifactSha256 = Hash(kind + ".xml"), ExpandedSha256 = Hash(kind + ".xml"), ExpectedRecords = 1,
                SchemaPath = "schema.txt", SchemaSha256 = Hash("schema.txt"), LicensePath = "license.txt", LicenseSha256 = Hash("license.txt"),
                LicenseStatus = "approved", LicenseApprovalReference = "fixture-only", Attribution = "fixture"
            };
            var sourceLock = new DictionarySourceLock
            {
                Scope = "ja-en", TransformationId = "fixture-v1", TransformationSha256 = new string('a', 64),
                Sources = [Pin("jmdict"), Pin("kanjidic2")]
            };
            File.WriteAllText(LockPath, JsonSerializer.Serialize(sourceLock, SourceLockInspector.JsonOptions), new UTF8Encoding(false));
        }

        public void Dispose()
        {
            foreach (var name in new[] { "schema.txt", "license.txt", "jmdict.xml", "kanjidic2.xml", "source-lock.json" })
                File.Delete(Path.Combine(root, name));
            Directory.Delete(root);
        }
    }
}

internal sealed class StartupLog : ILogger
{
    public List<string> Messages { get; } = [];
    public List<Exception?> Exceptions { get; } = [];
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel level) => true;
    public void Log<TState>(LogLevel level, EventId id, TState state, Exception? error, Func<TState, Exception?, string> formatter)
    { Messages.Add(formatter(state, error)); Exceptions.Add(error); }
}
