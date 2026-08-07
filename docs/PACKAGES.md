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

The shared MFA convention uses the standard authentication-method reference claim shape `amr=mfa`. Security does not own user storage, authentication flows, MFA enrollment, recovery, or session persistence.

## FoundationKit.Identity

Public building blocks:

- `AccountSecurityOptions` and `AccountSecurityOptionsValidator`;
- `IAccountNotificationSender`;
- `AccountSecurityNotification`;
- `IdentitySensitiveOperation`;
- `IdentityStepUpFactor`;
- `IdentityStepUpPolicy`.

The package depends on `FoundationKit.Security` but does not provide or select a user store, ASP.NET Core Identity implementation, EF schema, token provider, SMTP provider, OAuth/OIDC server, or external IdP.

`AccountSecurityOptions` centralizes the reusable account policy that consumers bind to their configuration. The supported structural range is validated, while the consuming organization remains responsible for selecting approved policy values.

`IAccountNotificationSender` is a provider port. It carries confirmation/reset tokens and security notification intent to a consumer-owned adapter; implementations must avoid logging token or destination contents.

`IdentityStepUpPolicy` expresses factor requirements for sensitive account operations without deciding how those factors are verified. Athar remains responsible for the actual ASP.NET Core Identity verification and SMTP adapter.

See `docs/capabilities/IDENTITY.md` for the full boundary and consumer evidence.

## FoundationKit.Authorization

Public building blocks:

- `IAuthorizationSubject`;
- `PermissionDefinition` and `PermissionId`;
- `RolePermissionGrant` and `RolePermissionMap`;
- `IAuthorizationEvaluator`;
- `RolePermissionAuthorizationEvaluator`.

The package depends on `FoundationKit.Identity` but does not own product roles, product permission IDs, role/permission persistence, EF migrations, ASP.NET Core policy registration, tenant scope, or external policy engines.

`RolePermissionMap` is an immutable in-memory mapping primitive. Unknown permissions fail closed. `RolePermissionAuthorizationEvaluator` grants only to authenticated subjects with a matching product-owned role, and `CanAccessOwnedResource` allows ownership or an explicitly supplied privileged permission without a universal administrator bypass.

Athar is the first consumer: it owns its `athar.*` permission IDs and maps its own `Administrator` role to them. The business layer asks for semantic permissions instead of hard-coding that role inside `InitiativeManager`, while the existing ASP.NET Core administrator policy remains a coarse outer defense.

See `docs/capabilities/AUTHORIZATION.md` for the full boundary and consumer evidence.

## FoundationKit.Workflow

Public building blocks:

- `WorkflowTransitionDefinition`;
- `WorkflowTransition`;
- `WorkflowDefinition`;
- `WorkflowId`;
- `WorkflowTransitionAudit`.

The package depends on `FoundationKit.Auditing` and remains independent of Security, Identity, Authorization, EF, ASP.NET Core, and product assemblies.

`WorkflowDefinition` validates a deterministic state/trigger graph: duplicate transition IDs and ambiguous `fromState + trigger` pairs are rejected, transition collections are read-only, and unknown transitions fail closed.

`WorkflowTransitionAudit` maps a successful transition into the bounded `AuditRequest` contract without selecting an audit sink or persistence strategy.

Athar is the first consumer: `InitiativeWorkflow` defines its own submitted/approve/reject state machine and the aggregate uses the reusable resolver while retaining product-owned validation, mutation, events, concurrency, and persistence.

See `docs/capabilities/WORKFLOW.md` for the full boundary and consumer evidence.

## Capability catalog contract

The human-facing implemented feature list is maintained in `catalog/foundationkit.catalog.json`. Every catalog capability must correspond to existing tested behavior and public surface. The catalog generator rejects unknown idea references and any status other than `implemented`.

The catalog is documentation metadata; it does not change runtime package behavior or add package dependencies.
