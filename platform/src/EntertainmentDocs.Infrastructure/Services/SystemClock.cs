using EntertainmentDocs.Application.Abstractions;

namespace EntertainmentDocs.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
