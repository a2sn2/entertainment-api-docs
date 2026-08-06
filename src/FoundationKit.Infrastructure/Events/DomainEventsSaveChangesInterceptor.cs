using FoundationKit.Application.Events;
using FoundationKit.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FoundationKit.Infrastructure.Events;

/// <summary>
/// Dispatches in-process domain events after a successful EF Core save.
/// Events are cleared before dispatch so a handler failure cannot cause an
/// accidental duplicate dispatch on a later save. Use a durable outbox when
/// delivery guarantees are required.
/// </summary>
public sealed class DomainEventsSaveChangesInterceptor(IDomainEventDispatcher dispatcher)
    : SaveChangesInterceptor
{
    private IReadOnlyList<IDomainEvent> _pendingEvents = [];
    private IReadOnlyList<IHasDomainEvents> _pendingEntities = [];

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        var events = PrepareDispatch();
        if (events.Count > 0)
            dispatcher.DispatchAsync(events).GetAwaiter().GetResult();

        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var events = PrepareDispatch();
        if (events.Count > 0)
            await dispatcher.DispatchAsync(events, cancellationToken).ConfigureAwait(false);

        return await base.SavedChangesAsync(eventData, result, cancellationToken)
            .ConfigureAwait(false);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        Reset();
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Reset();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void Capture(DbContext? context)
    {
        _pendingEntities = context?.ChangeTracker
            .Entries()
            .Select(entry => entry.Entity)
            .OfType<IHasDomainEvents>()
            .Where(entity => entity.DomainEvents.Count > 0)
            .Distinct()
            .ToArray() ?? [];

        _pendingEvents = _pendingEntities
            .SelectMany(entity => entity.DomainEvents)
            .ToArray();
    }

    private IReadOnlyList<IDomainEvent> PrepareDispatch()
    {
        var events = _pendingEvents;
        foreach (var entity in _pendingEntities)
            entity.ClearDomainEvents();

        Reset();
        return events;
    }

    private void Reset()
    {
        _pendingEvents = [];
        _pendingEntities = [];
    }
}
