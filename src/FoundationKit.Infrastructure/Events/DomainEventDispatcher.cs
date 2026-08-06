using FoundationKit.Application.Events;
using FoundationKit.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace FoundationKit.Infrastructure.Events;

public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))
                ?? throw new InvalidOperationException(
                    $"Handler method was not found for {handlerType.Name}.");

            foreach (var handler in serviceProvider.GetServices(handlerType))
            {
                var task = handleMethod.Invoke(handler, [domainEvent, cancellationToken]) as Task
                    ?? throw new InvalidOperationException(
                        $"{handlerType.Name} returned an invalid task.");

                await task.ConfigureAwait(false);
            }
        }
    }
}
