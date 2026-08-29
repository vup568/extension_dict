using Application;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using WebApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Register Layer Services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();

// Tự động đảm bảo schema Database PostgreSQL được khởi tạo/cập nhật bảng đầy đủ
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Warning] Lỗi khi tự động migrate database: {ex.Message}");
    }
}

app.MapHealthChecks("/health/live");
app.MapDictionaryEndpoints();
app.MapKanjiEndpoints();
app.MapGrammarEndpoints();

app.Run();

public partial class Program;
