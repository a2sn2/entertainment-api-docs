using Madar.Infrastructure;
using Xunit;

namespace Madar.Tests;

public sealed class DatabaseStartupRetryTests
{
    [Fact]
    public async Task ExecuteAsync_RetriesTransientFailureUntilSuccess()
    {
        var attempts = 0;
        var retries = new List<int>();

        await DatabaseStartupRetry.ExecuteAsync(
            (attempt, _) =>
            {
                attempts++;
                if (attempt < 3)
                    throw new InvalidOperationException("transient");

                return Task.CompletedTask;
            },
            attempts: 4,
            delay: TimeSpan.Zero,
            onRetry: (attempt, _, _) => retries.Add(attempt));

        Assert.Equal(3, attempts);
        Assert.Equal([1, 2], retries);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAllAttemptsFail_ThrowsBoundedFailure()
    {
        var attempts = 0;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DatabaseStartupRetry.ExecuteAsync(
                (_, _) =>
                {
                    attempts++;
                    throw new InvalidOperationException("still unavailable");
                },
                attempts: 3,
                delay: TimeSpan.Zero));

        Assert.Equal(3, attempts);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal(
            "Madar database startup failed after the configured retry attempts.",
            exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_CancellationStopsFurtherRetries()
    {
        using var cancellation = new CancellationTokenSource();
        var attempts = 0;

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            DatabaseStartupRetry.ExecuteAsync(
                (_, _) =>
                {
                    attempts++;
                    cancellation.Cancel();
                    throw new InvalidOperationException("transient");
                },
                attempts: 5,
                delay: TimeSpan.FromSeconds(1),
                cancellationToken: cancellation.Token));

        Assert.Equal(1, attempts);
    }
}
