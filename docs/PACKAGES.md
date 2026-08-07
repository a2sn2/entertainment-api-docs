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

A consuming API still chooses authentication, authorization, CORS, OpenAPI, and operational policy. Reusable trusted-proxy and authentication-assurance conventions live in the opt-in `FoundationKit.Security` package rather than being forced by WebApi.

## FoundationKit.Blazor

Public building blocks:

- `ApiError`;
- `ApiResult` and `ApiResult<T>`;
- `ApiClientBase`;
- `ApiResponseReader`;
- `AsyncState<T>`.

Successful responses with invalid JSON become `Response.InvalidJson` failures rather than escaping as deserialization exceptions.

## FoundationKit.Auditing

Public building blocks:

- immutable audit request/event/context contracts;
- `IAuditSink`;
- `IAuditRecorder` / `AuditRecorder`;
- bounded metadata validation and defensive copying;
- rejection of common secret/credential attribute names.

The package does not select a database, SIEM, logging framework, retention policy, or failure policy. Consumers provide the sink and data-classification rules.

## FoundationKit.Security

Public building blocks:

- `TrustedProxyOptions`;
- `TrustedProxySecurity`;
- `AddFoundationTrustedProxyForwarding`;
- `UseFoundationTrustedProxyForwarding`;
- `FoundationRateLimitPartitions`;
- `FoundationAuthenticationAssurance`;
- `RequireFoundationMultiFactor`.

The package is opt-in and depends on `FoundationKit.WebApi`; the kernel and application layers do not depend on it.

Trusted forwarded headers are fail-closed: when enabled, at least one explicit trusted proxy IP is required, trust-all defaults are cleared, and forwarded `for`/`proto` values are processed only through ASP.NET Core forwarded-header middleware. Consumers must place the forwarding middleware before any security decision that uses scheme or remote address.

Rate-limit helpers define partition keys only; they do not force permit counts, windows, queuing, storage, or distributed rate-limit providers. The consuming product still owns those policy values.

The shared MFA convention uses the standard authentication-method reference claim shape `amr=mfa`. The future Identity capability may issue this assurance, but Security does not own user storage, authentication flows, MFA enrollment, recovery, or session persistence.

## Capability catalog contract

The human-facing implemented feature list is maintained in `catalog/foundationkit.catalog.json`. Every catalog capability must correspond to existing tested behavior and public surface. The catalog generator rejects unknown idea references and any status other than `implemented`.

The catalog is documentation metadata; it does not change runtime package behavior or add package dependencies.
