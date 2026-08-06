using FoundationKit.Application.Events;
using FoundationKit.Domain.Events;
using FoundationKit.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;

namespace FoundationKit.Tests;

public sealed class DomainEventDispatcherTests
{
    [Fact]
    public async Task Dispatcher_invokes_all_registered_handlers()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Counter>();
        services.AddTransient<IDomainEventHandler<TestEvent>, FirstHandler>();
        services.AddTransient<IDomainEventHandler<TestEvent>, SecondHandler>();

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(provider);

        await dispatcher.DispatchAsync([new TestEvent()]);

        Assert.Equal(2, provider.GetRequiredService<Counter>().Value);
    }

    private sealed record TestEvent : IDomainEvent;

    private sealed class Counter
    {
        public int Value { get; set; }
    }

    private sealed class FirstHandler(Counter counter) : IDomainEventHandler<TestEvent>
    {
        public Task HandleAsync(
            TestEvent domainEvent,
            CancellationToken cancellationToken = default)
        {
            counter.Value++;
            return Task.CompletedTask;
        }
    }

    private sealed class SecondHandler(Counter counter) : IDomainEventHandler<TestEvent>
    {
        public Task HandleAsync(
            TestEvent domainEvent,
            CancellationToken cancellationToken = default)
        {
            counter.Value++;
            return Task.CompletedTask;
        }
    }
}
