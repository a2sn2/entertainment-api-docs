using FoundationKit.Domain.Events;
using FoundationKit.Domain.Primitives;

namespace FoundationKit.Tests;

public sealed class AggregateRootTests
{
    [Fact]
    public void Raised_events_are_exposed_and_can_be_cleared()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.Raise(new TestEvent());

        Assert.Single(aggregate.DomainEvents);
        aggregate.ClearDomainEvents();
        Assert.Empty(aggregate.DomainEvents);
    }

    private sealed class TestAggregate(Guid id) : AggregateRoot<Guid>(id)
    {
        public void Raise(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    private sealed record TestEvent : IDomainEvent;
}
