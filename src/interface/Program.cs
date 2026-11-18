// Explicit operator commands must never initialize HTTP, configuration providers or migrations.
if (WebApi.DictionaryImportCommand.IsRequested(args))
{
    using var cancellation = new CancellationTokenSource();
    ConsoleCancelEventHandler cancel = (_, signal) => { signal.Cancel = true; cancellation.Cancel(); };
    Console.CancelKeyPress += cancel;
    try
    {
        Environment.ExitCode = await WebApi.DictionaryImportCommand.ExecuteAsync(args, Console.Out,
            WebApi.DictionaryImportCommand.ExecuteStoreAsync, cancellation.Token);
    }
    finally { Console.CancelKeyPress -= cancel; }
    return;
}

var recoveryRequested = args.Contains("--recover-legacy-schema", StringComparer.Ordinal);
var demoRequested = args.Contains("--seed-demo-dictionary", StringComparer.Ordinal);
// Consume our valueless operator switch before the framework's key/value configuration parser.
var builder = WebApplication.CreateBuilder(args.Where(value => value is not "--recover-legacy-schema" and not "--seed-demo-dictionary").ToArray());

if (recoveryRequested && demoRequested)
{
    Console.WriteLine("Choose one explicit local database command; no database was changed.");
    Environment.ExitCode = 1;
    return;
}

if (demoRequested)
{
    Environment.ExitCode = await WebApi.DemoDictionaryCommand.ExecuteAsync(builder.Configuration, Console.Out);
    return;
}

if (recoveryRequested)
{
    Environment.ExitCode = await WebApi.DatabaseRecoveryCommand.ExecuteAsync(builder.Configuration, Console.Out);
    return;
}

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
