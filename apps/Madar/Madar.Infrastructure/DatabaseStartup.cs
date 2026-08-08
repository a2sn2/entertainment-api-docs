using Microsoft.EntityFrameworkCore;

namespace Madar.Infrastructure;

public sealed class MadarDatabaseStartupOptions
{
    public const string SectionName = "Madar:DatabaseStartup";

    public bool ApplyMigrationsOnStartup { get; set; } = true;

    public bool SeedRolesOnStartup { get; set; } = true;

    public int MigrationAttempts { get; set; } = 60;

    public int DelaySeconds { get; set; } = 2;
}

internal static class DatabaseStartupRetry
{
    internal static async Task ExecuteAsync(
        Func<int, CancellationToken, Task> operation,
        int attempts,
        TimeSpan delay,
        Action<int, int, Exception>? onRetry = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentOutOfRangeException.ThrowIfLessThan(attempts, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);

        Exception? lastError = null;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await operation(attempt, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                lastError = exception;
                if (attempt >= attempts)
                    break;

                onRetry?.Invoke(attempt, attempts, exception);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException(
            "Madar database startup failed after the configured retry attempts.",
            lastError);
    }
}

public interface IMadarReadinessProbe
{
    Task<bool> IsReadyAsync(CancellationToken cancellationToken = default);
}

public sealed class MadarReadinessProbe(MadarDbContext dbContext) : IMadarReadinessProbe
{
    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await dbContext.Database.CanConnectAsync(cancellationToken))
                return false;

            var pending = await dbContext.Database
                .GetPendingMigrationsAsync(cancellationToken);
            return !pending.Any();
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }
}
