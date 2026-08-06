# Technical Architecture

## Repository purpose

FoundationKit provides reusable technical building blocks and a reference application that proves two complete vertical slices:

```text
Reusable FoundationKit packages
        ↓ consumed by
User Full Stack + Admin Full Stack
        ↓ connected through
Shared request lifecycle and unit of work
        ↓ persisted by
EF Core + SQL Server + Workbench-owned migrations
```

The reference application is not a sixth core package. It demonstrates how a product consumes the core without leaking product behavior into `src/`.

For the functional walkthrough, read [Dual Full-Stack Architecture](DUAL-FULL-STACK.md).

## Reusable dependency rules

```text
Domain <- Application <- Infrastructure
             ^
             |
           WebApi

Blazor is independent from server-side packages.
```

### FoundationKit.Domain

May depend only on the .NET base class library. It provides entities, aggregate roots, value objects, and domain events.

### FoundationKit.Application

May depend on Domain. It provides results, commands, queries, repository ports, pagination, validation abstractions, and application contracts.

### FoundationKit.Infrastructure

May depend on Application, Domain, and provider-neutral EF Core abstractions. It must not select SQL Server or own migrations.

### FoundationKit.WebApi

May depend on Application and the ASP.NET Core shared framework. It maps classified results to HTTP, Problem Details, correlation IDs, and baseline response headers.

### FoundationKit.Blazor

Owns browser-side typed API results, response parsing, network classification, and asynchronous UI state. It must not reference EF Core, SQL Server, or server hosting.

Architecture tests and `scripts/verify-repository.sh` enforce these boundaries.

## Reference application composition

```text
samples/FoundationKit.Workbench/
    ASP.NET Core host
    product domain aggregates
    user and admin application use cases
    endpoint route groups
    WorkbenchDbContext
    SQL Server provider
    EF Core migrations
    Swagger/OpenAPI

samples/FoundationKit.Workbench.Contracts/
    Shared platform contracts
    User contracts
    Admin contracts
    Workflow vocabulary
    API route constants

samples/FoundationKit.Workbench.Client/
    Blazor WebAssembly shell
    MudBlazor
    architecture landing page
    user portal UI/UX
    admin portal UI/UX
    typed WorkbenchApiClient
```

One ASP.NET Core host serves the Blazor application and all API groups from one origin. This keeps local startup simple without merging the user and admin use cases.

## Vertical slice 1 — User

```text
Pages/UserPortal.razor
        ↓
WorkbenchApiClient.CreateUserRequestAsync
        ↓
CreateUserRequest contract
        ↓
POST /api/user/requests
        ↓
CreateUserRequestUseCase
        ↓
BuildBrief.Create
        ↓
IRepository<BuildBrief, Guid>
        ↓
IUnitOfWork
        ↓
BuildBriefs
```

The user reads the latest workflow state through:

```text
GET /api/user/requests/{id}
```

Transport contracts are not EF entities. The API maps the aggregate to `UserRequestResponse`.

## Vertical slice 2 — Admin

```text
Pages/AdminPortal.razor
        ↓
WorkbenchApiClient.GetAdminQueueAsync
        ↓
GET /api/admin/requests?status=submitted
        ↓
IAdminQueueReader
        ↓
EfAdminQueueReader
        ↓
BuildBriefs
```

A review follows:

```text
AdminReviewRequest
        ↓
POST /api/admin/requests/{id}/review
        ↓
ReviewUserRequestUseCase
        ↓
AdminReview.Create
        +
BuildBrief.ApplyReview
        ↓
AdminReviews insert + BuildBriefs status update
        ↓
IUnitOfWork.SaveChangesAsync
```

The admin write path does not call the user UI. It changes shared domain state and persistence, which the user API then exposes.

## Integration boundary

The current connection is a synchronous workflow transition:

```text
submitted → approved | rejected
```

`ReviewUserRequestUseCase` creates the audit record and changes the user request status before one unit-of-work save. The domain emits `BuildBriefReviewed` after the transition.

For a distributed product, this boundary may evolve into:

```text
Admin transaction
        ↓
Outbox message
        ↓
Integration event
        ↓
User read model update
```

The UI and transport contracts do not need to become coupled when that evolution happens.

## Database ownership

The reference application owns:

- `Microsoft.EntityFrameworkCore.SqlServer`;
- `WorkbenchDbContext`;
- `BuildBriefConfiguration`;
- `AdminReviewConfiguration`;
- connection strings;
- startup migration behavior;
- migrations under `samples/FoundationKit.Workbench/Infrastructure/Migrations/`.

Current tables:

| Table | Owner | Responsibility |
|---|---|---|
| `BuildBriefs` | User workflow | Request data, current status, created and updated timestamps |
| `AdminReviews` | Admin workflow | Decision, reviewer, notes, timestamp, foreign key to request |

No reusable package owns a relational provider or schema.

## Contracts and HTTP

Contracts are divided by audience:

```text
Contracts/User/CreateUserRequest
Contracts/User/UserRequestResponse
Contracts/Admin/AdminReviewRequest
Contracts/Admin/AdminQueueItemResponse
Contracts/Admin/AdminReviewResponse
Contracts/Workflow/WorkflowStatuses
Contracts/Workflow/ReviewDecisions
```

The same types and JSON shapes are used by:

- Blazor serialization;
- minimal API binding;
- Swagger/OpenAPI;
- Postman;
- smoke tests.

Swagger groups endpoints as:

- Shared platform;
- User full stack;
- Admin full stack.

`FoundationKit.WebApi` maps classified failures to RFC 7807 Problem Details. `FoundationKit.Blazor` classifies successful responses, API failures, network errors, timeouts, empty payloads, and invalid JSON.

## Canonical capability catalog

`catalog/foundationkit.catalog.json` remains the hand-maintained source for implemented reusable capabilities, packages, ideas, adoption steps, and contact metadata.

It feeds:

- `GET /api/catalog`;
- the user portal capability selector;
- the architecture landing page;
- the GitHub Pages demo;
- generated `docs/FEATURES.md`.

The API embeds the catalog as an assembly resource for reliable local and container execution. The client receives a static copy for GitHub Pages fallback.

## GitHub Pages boundary

GitHub Pages publishes the same Blazor WebAssembly client. It cannot execute ASP.NET Core or SQL Server.

In demo mode:

- the architecture landing page remains active;
- the user form and JSON preview remain visible;
- database submission is disabled;
- the admin page shows explicit demo queue data;
- approve and reject actions are disabled;
- no persistence is claimed.

The local API-hosted path is authoritative.

## Domain events

`DomainEventsSaveChangesInterceptor` dispatches in-process events only after a successful database save.

```text
Capture events
        ↓
Database save succeeds
        ↓
Clear aggregate queues
        ↓
Dispatch registered handlers
```

A database failure dispatches nothing. In-process events are not durable; use an outbox for production delivery guarantees.

## CI verification

CI performs:

1. repository-boundary verification;
2. catalog and Postman JSON validation;
3. restore and Release build;
4. generated capability documentation drift detection;
5. core and Workbench tests;
6. Blazor-hosted API publish;
7. package creation;
8. Dockerized Blazor + API + SQL Server startup;
9. user request creation;
10. admin queue retrieval;
11. admin approval;
12. user status retrieval;
13. approved queue verification.

## Security and production boundary

The reference application intentionally does not implement production identity or authorization. A consuming product must decide:

- user authentication and ownership;
- admin authentication and roles;
- authorization policies for each route group;
- audit identity guarantees;
- rate limiting;
- private-data handling;
- secret management;
- outbox and queue strategy;
- telemetry and alerting;
- backup and recovery;
- controlled deployment migrations;
- ingress and TLS.

The two vertical slices make those decisions easy to place: user-facing concerns stay in the user slice, admin-facing concerns stay in the admin slice, and cross-slice behavior stays at the integration boundary.
