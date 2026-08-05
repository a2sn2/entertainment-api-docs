using FoundationKit.Application.Events;
using FoundationKit.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FoundationKit.Infrastructure.Events;

public sealed class DomainEventsSaveChangesInterceptor(IDomainEventDispatcher dispatcher) : SaveChangesInterceptor
{
    private IReadOnlyList<IDomainEvent> _pendingEvents = [];

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        _pendingEvents = eventData.Context?.ChangeTracker
            .Entries()
            .Select(entry => entry.Entity)
            .OfType<IHasDomainEvents>()
            .SelectMany(entity => entity.DomainEvents)
            .ToArray() ?? [];

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_pendingEvents.Count > 0)
        {
            await dispatcher.DispatchAsync(_pendingEvents, cancellationToken);
            foreach (var entity in eventData.Context?.ChangeTracker.Entries()
                         .Select(entry => entry.Entity)
                         .OfType<IHasDomainEvents>() ?? [])
            {
                entity.ClearDomainEvents();
            }
        }

        _pendingEvents = [];
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _pendingEvents = [];
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }
}
