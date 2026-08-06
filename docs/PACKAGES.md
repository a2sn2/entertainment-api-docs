# Package Contracts

## FoundationKit.Domain

Public building blocks:

- `Entity<TId>`
- `AggregateRoot<TId>`
- `ValueObject`
- `DomainException`
- `IDomainEvent`
- `IHasDomainEvents`

No framework packages are referenced.

## FoundationKit.Application

Public building blocks:

- command and query contracts;
- classified `Error`, `Result`, and `Result<T>`;
- current-user, clock, and unit-of-work ports;
- repository and specification contracts;
- pagination records;
- validation contracts;
- domain-event handler and dispatcher contracts.

Application does not query a database or inspect HTTP context directly.

## FoundationKit.Infrastructure

Public building blocks:

- `EfRepository<TEntity, TId, TContext>`;
- `EfUnitOfWork<TContext>`;
- `SpecificationEvaluator`;
- `DomainEventDispatcher`;
- `DomainEventsSaveChangesInterceptor`;
- `AddFoundationInfrastructure`.

The package references EF Core itself but no provider.

A consuming application registers the interceptor with its DbContext:

```csharp
services.AddFoundationInfrastructure();

services.AddDbContext<ProductDbContext>((serviceProvider, options) =>
{
    options.UseYourSelectedProvider(connectionString);
    options.AddInterceptors(
        serviceProvider.GetRequiredService<DomainEventsSaveChangesInterceptor>());
});
```

Provider selection and migrations remain in the consuming application.

## FoundationKit.WebApi

Public building blocks:

- `AddFoundationWebApi`;
- `UseFoundationRequestPipeline`;
- `ToHttpResult`;
- correlation-ID middleware;
- baseline security-header middleware.

A consuming API chooses all host-specific security and operational settings.

## FoundationKit.Blazor

Public building blocks:

- `ApiError`;
- `ApiResult` and `ApiResult<T>`;
- `ApiClientBase`;
- `ApiResponseReader`;
- `AsyncState<T>`.

Successful responses with invalid JSON are returned as `Response.InvalidJson` failures rather than escaping as deserialization exceptions.
