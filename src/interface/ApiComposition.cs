namespace WebApi;

using Application;
using Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Endpoints;

/// <summary>
/// Shared Web API composition used by production bootstrap and integration tests.
/// Keeping registration and endpoint mapping here prevents the test host from
/// drifting away from the production route surface.
/// </summary>
public static class ApiComposition
{
    public static void RegisterServices(WebApplicationBuilder builder)
    {
        builder.Services.AddApplicationServices();
        builder.Services.AddInfrastructureServices(builder.Configuration);
        builder.Services.AddHealthChecks();
    }

    public static void MapEndpoints(WebApplication app)
    {
        app.MapHealthChecks("/health/live");
        app.MapDictionaryEndpoints();
        app.MapKanjiEndpoints();
        app.MapGrammarEndpoints();
        app.MapAnalysisEndpoints();
    }
}
