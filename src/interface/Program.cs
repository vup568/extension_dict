var builder = WebApplication.CreateBuilder(args);

// Register Layer Services
WebApi.ApiComposition.RegisterServices(builder);

var app = builder.Build();

// Complete existing migration initialization before exposing even the liveness endpoint.
if (!await WebApi.DatabaseStartup.InitializeAsync(app.Services, app.Logger))
{
    await app.DisposeAsync();
    Environment.ExitCode = 1;
    return;
}

WebApi.ApiComposition.MapEndpoints(app);

await app.RunAsync();

public partial class Program;
