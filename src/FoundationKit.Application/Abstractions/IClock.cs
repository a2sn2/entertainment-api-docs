namespace FoundationKit.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
