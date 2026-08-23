using Application;
using Infrastructure;
using WebApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Register Layer Services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health/live");
app.MapDictionaryEndpoints();
app.MapKanjiEndpoints();

app.Run();

public partial class Program;
