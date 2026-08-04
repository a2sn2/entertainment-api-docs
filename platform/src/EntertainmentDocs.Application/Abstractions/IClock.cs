namespace EntertainmentDocs.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
