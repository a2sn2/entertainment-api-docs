# FoundationKit Reusable Core

FoundationKit is a set of small .NET libraries that provide reusable technical behavior without owning product-specific business rules. Version at the reference baseline: `0.1.0`.

---

## 1. Package dependency map

```text
FoundationKit.Domain
        ↑
FoundationKit.Application
        ↑
FoundationKit.Infrastructure

FoundationKit.Application
        ↑
FoundationKit.WebApi

FoundationKit.Blazor
    independent browser-side package
```

`FoundationKit.Infrastructure` references Domain and Application. `FoundationKit.WebApi` references Application. `FoundationKit.Blazor` contains browser transport/state behavior and does not need the server packages.

---

# 2. Build and package configuration

## `platform/core/Directory.Build.props`

This file imports the parent `platform/Directory.Build.props` before adding package metadata. The explicit import matters because MSBuild searches upward for the nearest `Directory.Build.props`; a core-local file would otherwise replace rather than automatically merge the parent configuration.

Core package settings include:

- `IsPackable=true`;
- package version `0.1.0`;
- author/company/repository metadata;
- symbols package generation;
- source inclusion conventions.

Reason: each core project can be packed as an internal NuGet package while still using `net8.0`, nullable reference types, implicit usings, and warnings-as-errors.

## Core project files

### `FoundationKit.Domain.csproj`

Uses `Microsoft.NET.Sdk` and declares no package references. Its description states that it contains reusable domain primitives with no framework dependencies.

### `FoundationKit.Application.csproj`

References only `FoundationKit.Domain`. This enforces an inward dependency.

### `FoundationKit.Infrastructure.csproj`

References Domain and Application plus base `Microsoft.EntityFrameworkCore` and DI abstractions. It intentionally does not reference `Microsoft.EntityFrameworkCore.SqlServer`, Npgsql, SQLite, or ASP.NET Core hosting.

### `FoundationKit.WebApi.csproj`

References Application and the shared framework `Microsoft.AspNetCore.App`. The framework reference supplies HTTP and middleware types.

### `FoundationKit.Blazor.csproj`

A plain class library containing types that depend on browser-compatible .NET HTTP/JSON APIs. Product Blazor projects reference it.

---

# 3. FoundationKit.Domain

## 3.1 `Primitives/Entity.cs`

```csharp
public abstract class Entity<TId> where TId : notnull
```

- `abstract`: the base itself is not a business entity.
- `TId`: identity type may be `Guid`, integer, string, or another non-null type.
- `where TId : notnull`: prevents a nullable identity contract.

```csharp
protected Entity(TId id) => Id = id;
protected Entity() { Id = default!; }
```

Two construction paths exist:

- normal derived construction supplies an ID;
- persistence frameworks can use the parameterless path and populate the property later.

`default!` suppresses a compiler warning for the materialization path. It does not mean default identity is a valid persistent identity.

```csharp
public TId Id { get; protected set; }
```

Everyone may read identity; only the entity or derived classes may change it.

```csharp
private bool IsTransient => EqualityComparer<TId>.Default.Equals(Id, default!);
```

A transient entity has the default ID. For `Guid`, this is `Guid.Empty`; for integer, zero.

### Equality algorithm

```csharp
if (obj is null || obj.GetType() != GetType()) return false;
```

Null and different concrete entity types are not equal, even if IDs match.

```csharp
if (ReferenceEquals(this, obj)) return true;
```

The same object instance is always equal to itself.

```csharp
var other = (Entity<TId>)obj;
if (IsTransient || other.IsTransient) return false;
```

Two separate unsaved entities with default IDs are deliberately not equal. Without this rule, every new entity would compare equal to every other new entity.

```csharp
return EqualityComparer<TId>.Default.Equals(Id, other.Id);
```

Persistent entities of the same concrete type compare by ID.

### Hash code algorithm

Transient entities use `RuntimeHelpers.GetHashCode(this)`, an instance-oriented hash. Persistent entities combine concrete type and ID. This keeps equality and hash behavior aligned for dictionary/set use.

The `==` and `!=` operators delegate to `Equals`, giving consistent operator and method behavior.

### Modification rule

Do not add mutable business properties to the generic base. Identity equality is reusable; product lifecycle rules are not.

---

## 3.2 `Primitives/AggregateRoot.cs`

```csharp
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
```

An aggregate root is an entity and can accumulate domain events.

```csharp
private readonly List<IDomainEvent> _domainEvents = [];
```

The private mutable list is owned by the aggregate. Callers cannot replace it.

```csharp
public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
```

External code may inspect pending events but cannot add/remove through the exposed collection.

```csharp
protected void RaiseDomainEvent(IDomainEvent domainEvent)
```

Only the aggregate or derived classes can raise events. `ArgumentNullException.ThrowIfNull` fails immediately when a caller attempts to raise null.

```csharp
public void ClearDomainEvents() => _domainEvents.Clear();
```

Infrastructure clears events after successful dispatch. It is public because the infrastructure works through `IHasDomainEvents`, but application code should not clear events arbitrarily.

### Event timing

The current interceptor dispatches after successful database save. This favors “state committed before in-process side effects.” It is not a transactional outbox and does not guarantee delivery after process failure.

---

## 3.3 `Primitives/ValueObject.cs`

A value object has no independent identity. Derived types define equality components:

```csharp
protected abstract IEnumerable<object?> GetEqualityComponents();
```

`Equals` requires:

1. the other object is a `ValueObject`;
2. both concrete runtime types match;
3. equality component sequences match in value and order.

`GetHashCode` folds components from seed `17` using `HashCode.Combine`.

Derived types must:

- return all components that define value identity;
- return them in a stable order;
- avoid mutable equality components where possible.

---

## 3.4 `Events/IDomainEvent.cs`

```csharp
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
```

Every event states when the domain occurrence happened. `DateTimeOffset` preserves an absolute instant and offset, avoiding ambiguous local times.

An event should describe a past fact, commonly named in past tense, such as `DocumentPublishedDomainEvent`.

---

## 3.5 `Events/IHasDomainEvents.cs`

Defines the minimum infrastructure-facing contract:

- read pending events;
- clear them after dispatch.

The interface avoids coupling interceptors to a specific aggregate base class.

---

## 3.6 `Exceptions/DomainException.cs`

```csharp
public sealed class DomainException(string code, string message) : Exception(message)
```

A domain exception carries a machine-readable `Code` and human-readable base exception message.

The `Code` property initializer:

- rejects null, empty, or whitespace codes;
- uses `nameof(code)` in the argument exception;
- trims surrounding whitespace.

Current product aggregate methods mostly throw `ArgumentException` and `InvalidOperationException`; `DomainException` is an available reusable primitive, not yet the universal product exception policy.

---

# 4. FoundationKit.Application

## 4.1 `Results/ErrorType.cs`

Closed error categories:

```text
None = 0
Validation = 1
NotFound = 2
Conflict = 3
Unauthorized = 4
Forbidden = 5
BusinessRule = 6
Failure = 7
```

The numeric values make serialization/debugging stable. `None` is reserved for successful results.

## 4.2 `Results/Error.cs`

```csharp
public sealed record Error(string Code, string Description, ErrorType Type)
```

The record provides value equality, useful in tests and result consistency checks.

`Error.None` represents absence of error. Static factories create correctly classified values:

```csharp
Error.Validation(code, description)
Error.NotFound(...)
...
```

The code should be stable and namespace-like, for example `Documents.NotFound`. The description may evolve for readability; clients should not branch on description text.

## 4.3 `Results/Result.cs`

### Non-generic result

Constructor invariants:

- success must contain `Error.None`;
- failure must contain a non-None error.

These checks prevent impossible states such as “successful with error” or “failed without reason.”

Properties:

- `IsSuccess`: stored outcome;
- `IsFailure`: negation convenience;
- `Error`: typed failure or `None`.

Factories hide constructor details:

```csharp
Result.Success()
Result.Failure(error)
```

`Match` forces the caller to supply success and failure branches and returns one common result type.

### Generic result

`Result<T>` stores a private nullable backing field.

Successful construction sets a value and `Error.None`. Failed construction sets only an error.

`Value` throws when accessed on failure. This deliberate guard exposes incorrect caller logic immediately.

`ValueOrDefault` supports optional inspection without an exception.

`new static Failure` hides the base static factory and returns `Result<T>`.

### Intended use

Use Result for expected application outcomes such as not found, conflict, validation, and business-rule rejection. Do not convert programmer bugs, corrupted state, or infrastructure outages into false “normal” results unless a policy explicitly defines that behavior.

---

## 4.4 Messaging interfaces

### `ICommand.cs`

Marker interfaces:

- `ICommand` for an operation with no business response value;
- `ICommand<TResponse>` for an operation returning data such as a created ID.

Commands represent intent to change state.

### `ICommandHandler.cs`

Two handler shapes:

```csharp
Task<Result> HandleAsync(...)
Task<Result<TResponse>> HandleAsync(...)
```

The command type is contravariant (`in`) because the handler consumes commands. Cancellation is part of every asynchronous contract.

### `IQuery.cs`

`IQuery<out TResponse>` marks read intent and declares the response shape.

### `IQueryHandler.cs`

Returns `Result<TResponse>`, allowing not-found or other expected read failures without exceptions.

### Why no mediator package

Handlers are registered directly in DI and injected into endpoints. This keeps the core small and explicit. A mediator can be added later only if cross-cutting pipelines, dispatch indirection, or handler discovery justify it.

---

## 4.5 Environment abstractions

### `IClock`

Exposes `UtcNow`. Business code does not call `DateTimeOffset.UtcNow` directly, making time deterministic in tests and replaceable in products.

### `ICurrentUser`

Exposes:

- nullable user ID;
- nullable email;
- authentication state;
- role check.

Application code can make user-aware decisions without depending on `HttpContext` or browser claims types.

### `IUnitOfWork`

Defines one asynchronous save boundary. Returning affected row count preserves EF semantics, although current handlers do not branch on the count.

The abstraction does not expose transactions directly. A future transaction abstraction should be introduced only for use cases that need explicit multi-save transaction control.

---

## 4.6 Persistence abstractions

### `IReadRepository<TEntity,TId>`

Constraints require a FoundationKit entity and non-null ID.

Methods:

- `GetByIdAsync`: direct identity lookup;
- `FirstOrDefaultAsync`: first result matching a specification;
- `ListAsync`: all or specification-filtered results;
- `CountAsync`: count all or matching results.

### `IRepository<TEntity,TId>`

Extends read behavior with write tracking:

- add one;
- add range;
- remove one;
- remove range.

There is no generic `Update` method because EF Core tracks loaded entities and domain methods mutate them. Blind generic update can overwrite unintended columns.

### `ISpecification<TEntity>`

Describes a query without importing EF Core into Application:

- optional filter criteria;
- include expressions;
- ascending or descending ordering;
- skip/take;
- no-tracking choice.

Expressions are retained so EF Core can translate them to SQL.

### `Specification<TEntity>`

Base class stores the specification state.

- constructor accepts optional criteria;
- `AddInclude` accumulates eager-load expressions;
- applying ascending order clears descending order;
- applying descending order clears ascending order;
- paging rejects negative skip and non-positive take;
- `UseNoTracking` marks read-only query intent.

Only derived specification types can configure it because mutator methods are protected.

### Hybrid repository rule

Generic operations cover mechanical persistence. Product repositories extend them with business-language queries. A product must not force all domain queries through a generic string/filter API merely to avoid one interface method.

---

## 4.7 Pagination

### `PageRequest`

Defaults:

```text
page = 1
pageSize = 20
maximum = 200
```

Constructor behavior:

- page below 1 becomes 1;
- page size clamps to 1..200;
- `Skip` computes `(Page - 1) * PageSize`.

Clamping protects APIs from negative offsets and unbounded result requests.

### `PagedResult<T>`

Stores items, page, page size, and total count.

`TotalPages` uses ceiling division, returning zero when there are no records. Navigation flags compare current page with boundaries.

Current product endpoints do not yet use these pagination primitives; they are prepared core behavior.

---

## 4.8 Validation

`IValidator<in T>` returns a `ValueTask<IReadOnlyList<ValidationFailure>>`.

`ValidationFailure` carries:

- property name;
- stable error code;
- human-readable message.

The core provides contracts but does not impose FluentValidation or another library. A product can implement validators or later add a handler pipeline.

---

## 4.9 Domain event application contracts

`IDomainEventHandler<TEvent>` executes one event type asynchronously.

`IDomainEventDispatcher` accepts an event sequence. Application defines these contracts because event handling is use-case coordination; Infrastructure supplies runtime resolution.

---

# 5. FoundationKit.Infrastructure

## 5.1 `DependencyInjection.cs`

`AddFoundationInfrastructure` registers:

- `IDomainEventDispatcher` -> `DomainEventDispatcher` as scoped;
- `DomainEventsSaveChangesInterceptor` as scoped.

Scoped lifetime aligns the dispatcher/interceptor with a DbContext/request scope.

The method returns `services` for fluent startup composition.

---

## 5.2 `Persistence/EfRepository.cs`

```csharp
public class EfRepository<TEntity,TId,TDbContext>(TDbContext dbContext)
```

Constraints connect the reusable entity contract to an EF Core DbContext.

Protected members:

- `DbContext`: available to specialized repositories;
- `Set`: typed `DbSet<TEntity>` obtained from the context.

Methods are `virtual` so product repositories may replace behavior when required.

### `GetByIdAsync`

Uses an expression comparing `entity.Id` with the requested ID. This keeps the implementation generic and async.

### `FirstOrDefaultAsync`

Runs the query through `SpecificationEvaluator` and materializes at most one first result.

### `ListAsync`

Applies optional specification and asynchronously materializes a list, returned through read-only interface typing.

### `CountAsync`

Applies only criteria, not includes/order/paging. Count should represent the full filtered set, not the page slice.

### Add methods

`DbSet.AddAsync` returns `ValueTask<EntityEntry<TEntity>>`; `.AsTask()` adapts it to the repository's `Task` contract. AddRange tracks multiple entities.

### Remove methods

Synchronous because removal only changes EF tracking state; SQL executes during `SaveChangesAsync`.

---

## 5.3 `Persistence/SpecificationEvaluator.cs`

`Apply` accepts an existing `IQueryable` and optional specification.

Order of operations:

1. return unchanged query when specification is null;
2. apply `AsNoTracking`;
3. apply filter criteria;
4. aggregate includes;
5. apply one ordering direction;
6. apply skip;
7. apply take;
8. return the composed query without executing it.

Execution occurs later through `ToListAsync`, `FirstOrDefaultAsync`, or similar methods.

Ordering before paging is essential for stable page results.

---

## 5.4 `Persistence/EfUnitOfWork.cs`

A thin adapter:

```csharp
public Task<int> SaveChangesAsync(...) => dbContext.SaveChangesAsync(...);
```

This lets a product use a separate unit-of-work object when its DbContext does not implement the product interface directly.

EntertainmentDocs currently registers its `AppDbContext` as the product `IUnitOfWork`; the generic adapter remains available to other products.

---

## 5.5 `Events/DomainEventDispatcher.cs`

The dispatcher receives `IServiceProvider`.

For each event:

1. create the closed generic type `IDomainEventHandler<ActualEventType>`;
2. locate its `HandleAsync` method;
3. resolve all registered handlers of that closed type;
4. invoke each with event and cancellation token;
5. require the reflected return to be a `Task`;
6. await each handler sequentially.

### Why reflection

The dispatcher does not know event types at compile time. Reflection permits generic runtime dispatch without an external mediator package.

### Consequences

- missing handlers are allowed; the inner loop is empty;
- multiple handlers execute in DI enumeration order;
- one handler failure stops later handlers and propagates;
- execution is sequential, not parallel;
- method signatures are verified at runtime.

For external reliable events, use an outbox/message broker rather than assuming this in-process dispatcher guarantees delivery.

---

## 5.6 `Events/DomainEventsSaveChangesInterceptor.cs`

The EF Core interceptor has three phases.

### Before save: `SavingChangesAsync`

It scans ChangeTracker entries, selects entities implementing `IHasDomainEvents`, flattens their pending events, and stores them in `_pendingEvents`.

Capturing before save matters because EF tracking can change during persistence callbacks.

### After successful save: `SavedChangesAsync`

When pending events exist:

1. dispatch them;
2. scan tracked event-owning entities;
3. clear their event lists;
4. reset `_pendingEvents`;
5. delegate to base interceptor behavior.

Events are dispatched only after the database save succeeded.

### Failed save: `SaveChangesFailedAsync`

Clears pending event state and delegates to base. No events are dispatched for failed persistence.

### Lifetime warning

The interceptor stores `_pendingEvents` in instance state. Scoped registration and one active save operation per scoped context are important. It is not intended as a singleton shared across concurrent contexts.

---

# 6. FoundationKit.WebApi

## 6.1 `Results/ResultHttpExtensions.cs`

### Non-generic `ToHttpResult`

- success returns the supplied success result or `204 No Content`;
- failure maps the typed error to ProblemDetails.

### Generic `ToHttpResult<T>`

Requires a success mapping function, for example ID -> `201 Created` response.

### `ToProblem`

Switches on `ErrorType` and produces the corresponding HTTP status.

ProblemDetails fields:

- `status`: mapped HTTP code;
- `title`: stable error code;
- `detail`: human-readable description;
- extensions `code` and `errorType`.

The global ProblemDetails customization later adds request instance and correlation ID.

The default/failure branch maps to 500. `ErrorType.None` should never reach this method from a valid failed Result.

---

## 6.2 `Middleware/CorrelationIdMiddleware.cs`

Constant header name:

```text
X-Correlation-ID
```

Accepted caller value rules:

- exactly one header value;
- not blank;
- maximum length 128.

Otherwise a 32-character GUID (`N` format) is generated.

The middleware sets:

- `HttpContext.TraceIdentifier`;
- response correlation header;
- structured logging scope property `CorrelationId`.

It then calls the next middleware.

The length limit reduces abuse of log/header storage. A stricter character policy may be added for production telemetry systems.

---

## 6.3 `Middleware/SecurityHeadersMiddleware.cs`

Uses `Response.OnStarting` so headers are applied immediately before response transmission.

Headers:

- `X-Content-Type-Options: nosniff`;
- `X-Frame-Options: DENY`;
- `Referrer-Policy: no-referrer`;
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`.

`TryAdd` avoids overwriting a host-specific value already set earlier.

This is a baseline, not a full Content Security Policy or WAF.

---

## 6.4 `DependencyInjection.cs`

### `AddFoundationWebApi`

Registers ASP.NET Core ProblemDetails and customizes each response:

- `Instance` defaults to request path;
- `correlationId` extension uses `TraceIdentifier`.

### `UseFoundationRequestPipeline`

Adds correlation middleware first, then security-header middleware. Correlation comes first so downstream logs/errors share the ID.

---

# 7. FoundationKit.Blazor

## 7.1 `Api/ApiError.cs`

Immutable browser error record:

- stable code;
- user-readable message;
- nullable HTTP status;
- nullable correlation ID.

Network errors have no HTTP status because no response was received.

## 7.2 `Api/ApiResult.cs`

The non-generic result stores success, optional error, and optional status.

`Succeeded` is a compatibility alias for `IsSuccess` used by existing pages.

`Error` exposes message convenience; `ErrorDetails` retains machine-readable metadata.

The generic result adds nullable `Value`. Unlike server `Result<T>`, browser `ApiResult<T>.Value` does not throw on failure; callers check `IsSuccess`/`Succeeded` and null.

## 7.3 `Api/ApiClientBase.cs`

Holds a protected `HttpClient` for derived typed clients.

### Non-generic send

1. send request;
2. dispose response after processing;
3. success -> `ApiResult.Success(status)`;
4. HTTP failure -> parse structured error;
5. `HttpRequestException` -> network failure;
6. `OperationCanceledException` not caused by caller token -> timeout.

The exception filter distinguishes a timeout from deliberate user/navigation cancellation.

### Generic send

After success, deserializes JSON to `T`. A null payload becomes `Response.Empty` failure.

It centralizes repeated HTTP boilerplate while leaving route and product contract selection to feature clients.

## 7.4 `Api/ApiResponseReader.cs`

Reads the response body as text.

When non-empty, it tries JSON and searches in priority order:

- code: `code`, then `title`;
- message: `detail`, then legacy `error`, then `title`;
- correlation: `correlationId`.

If JSON parsing fails, raw body becomes the message and correlation is read from the header.

If body is empty/unusable, a generic message includes status number and reason phrase.

Helpers:

- `NetworkFailure`: `Network.Unavailable`;
- `Timeout`: `Network.Timeout`;
- `ReadString`: only accepts JSON strings;
- `ReadCorrelationHeader`: reads `X-Correlation-ID`.

## 7.5 `State/AsyncState.cs`

Tracks:

- current value;
- current API error;
- loading flag;
- derived HasValue/HasError flags.

`ExecuteAsync`:

1. rejects null operation delegate;
2. sets loading;
3. clears prior error;
4. awaits operation;
5. stores value on success or error on failure;
6. returns original result;
7. always clears loading.

`Reset` clears all state.

A product component should trigger re-rendering after state changes; `AsyncState<T>` itself is not an observable state container.

---

# 8. FoundationKit tests and package policy

Core tests protect:

- persistent entity equality;
- transient entity inequality;
- Result success/value/error consistency;
- failure value-access guard;
- layer dependency restrictions;
- provider neutrality.

Package workflow builds, tests, packs, and uploads `.nupkg` and `.snupkg` artifacts. It does not currently publish to a public feed.

Versioning rule:

- patch/minor changes should reflect compatibility impact;
- breaking public contracts require deliberate version increase;
- a product-specific convenience must not be added merely to reduce local typing.

---

# 9. What FoundationKit intentionally does not provide

Not currently included:

- generic controllers;
- automatic CRUD endpoint exposure;
- business managers;
- mediator/request bus;
- transaction pipeline;
- outbox;
- message broker;
- caching abstraction;
- multi-tenancy;
- distributed locks;
- event sourcing;
- provider-specific database configuration;
- product authentication policies;
- product contracts/pages.

These are extension points, not omissions to fill preemptively. They should enter the core only after real products demonstrate reusable need.
