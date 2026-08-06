using FoundationKit.Application.Abstractions;

namespace FoundationKit.Workbench.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
