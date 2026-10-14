namespace WebApi;

using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            logger.LogCritical("Database initialization failed ({ErrorType}). Check PostgreSQL and ConnectionStrings__DefaultConnection.",
                error.GetType().Name);
            return false;
        }
    }
}
