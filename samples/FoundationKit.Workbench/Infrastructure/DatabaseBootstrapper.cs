using Microsoft.EntityFrameworkCore;

namespace FoundationKit.Workbench.Infrastructure;

public static class DatabaseBootstrapper
{
    public static async Task MigrateAsync(
        IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        using var configurationScope = services.CreateScope();
        var configuration = configurationScope.ServiceProvider.GetRequiredService<IConfiguration>();
        var attempts = Math.Clamp(configuration.GetValue("Database:MigrationAttempts", 30), 1, 120);
        var delaySeconds = Math.Clamp(configuration.GetValue("Database:MigrationDelaySeconds", 2), 1, 30);

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                await using var scope = services.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<WorkbenchDbContext>();
                await dbContext.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("FoundationKit Workbench database is ready.");
                return;
            }
            catch (Exception exception) when (
                attempt < attempts &&
                !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    exception,
                    "SQL Server is not ready. Migration attempt {Attempt}/{Attempts} will retry.",
                    attempt,
                    attempts);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "Could not migrate the FoundationKit Workbench database. " +
            "Verify the SQL Server connection string and availability.");
    }
}
