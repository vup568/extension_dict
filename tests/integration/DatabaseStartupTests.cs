using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Infrastructure;
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
