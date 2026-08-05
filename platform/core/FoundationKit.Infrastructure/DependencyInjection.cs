using FoundationKit.Application.Events;
using FoundationKit.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;

namespace FoundationKit.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFoundationInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<DomainEventsSaveChangesInterceptor>();
        return services;
    }
}
