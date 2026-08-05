namespace FoundationKit.Domain.Events;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
