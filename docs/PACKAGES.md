# Package Contracts

## FoundationKit.Domain

Public building blocks:

- `Entity<TId>`;
- `AggregateRoot<TId>`;
- `ValueObject`;
- `DomainException`;
- `IDomainEvent`;
- `IHasDomainEvents`.

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

The package references EF Core abstractions but no relational provider.

A consuming application selects its provider and registers the interceptor:

```csharp
services.AddFoundationInfrastructure();

services.AddDbContext<ProductDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(connectionString); // consumer-owned provider decision
    options.AddInterceptors(
        serviceProvider.GetRequiredService<DomainEventsSaveChangesInterceptor>());
});
```

Provider selection, DbContext, configurations, migrations, transactions, concurrency policy, specialized repositories, and read models remain in the consuming application.

The local Workbench under `samples/` is the reference implementation for SQL Server ownership. Its migrations are not package assets.

## FoundationKit.WebApi

Public building blocks:

- `AddFoundationWebApi`;
- `UseFoundationRequestPipeline`;
- `ToHttpResult`;
- correlation-ID middleware;
- baseline security-header middleware.

A consuming API still chooses authentication, authorization, CORS, rate limiting, OpenAPI, forwarded headers, and operational policy.

## FoundationKit.Blazor

Public building blocks:

- `ApiError`;
- `ApiResult` and `ApiResult<T>`;
- `ApiClientBase`;
- `ApiResponseReader`;
- `AsyncState<T>`.

Successful responses with invalid JSON become `Response.InvalidJson` failures rather than escaping as deserialization exceptions.

## Capability catalog contract

The human-facing implemented feature list is maintained in `catalog/foundationkit.catalog.json`. Every catalog capability must correspond to existing tested behavior and public surface. The catalog generator rejects unknown idea references and any status other than `implemented`.

The catalog is documentation metadata; it does not change runtime package behavior or add package dependencies.
