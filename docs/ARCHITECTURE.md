# Architecture

## Repository purpose

FoundationKit supplies reusable technical building blocks and an official Workbench consumer that proves the integration path.

```text
Reusable core packages under src/
        ↓ consumed by
ASP.NET Core Workbench API
        ↓ shares contracts with
Blazor WebAssembly + MudBlazor client
        ↓ sends the same JSON used by
Swagger and Postman
        ↓ persists through
EF Core + SQL Server + Workbench-owned migrations
```

The Workbench projects are consumers, not additional reusable core packages.

## Reusable dependency rules

```text
Domain <- Application <- Infrastructure
             ^
             |
           WebApi

Blazor is independent from server-side packages.
```

### Domain

May depend only on the .NET base class library.

### Application

May depend on Domain. It owns use-case contracts and ports but does not depend on EF Core, ASP.NET Core, SQL Server, MudBlazor, or product DTOs.

### Infrastructure

May depend on Application, Domain, and provider-neutral EF Core abstractions. It must not reference a relational provider or hosted application.

### WebApi

May depend on Application and the ASP.NET Core shared framework. It adapts classified results to HTTP and supplies reusable middleware.

### Blazor

Owns browser-side transport and state helpers. It must not reference Domain, Application, Infrastructure, EF Core, SQL Server, or server hosting.

Architecture tests enforce assembly references. `scripts/verify-repository.sh` additionally rejects provider packages and migrations under `src/`.

## Official Workbench projects

```text
samples/FoundationKit.Workbench/
    ASP.NET Core API
    product domain and application logic
    WorkbenchDbContext
    SQL Server provider
    EF Core migrations
    Swagger/OpenAPI

samples/FoundationKit.Workbench.Client/
    Blazor WebAssembly
    Razor Components
    MudBlazor
    WorkbenchApiClient
    FoundationKit.Blazor consumption

samples/FoundationKit.Workbench.Contracts/
    ApiRoutes
    BuildBriefRequest
    BuildBriefResponse
    RuntimeResponse
    HealthResponse
    CatalogResponse and nested catalog DTOs
```

The API references the client as a hosted Blazor WebAssembly project. Running the API project serves both the backend and the frontend from one origin.

## Contract boundary

Transport contracts belong to `FoundationKit.Workbench.Contracts`, not to Domain or Application.

```text
MudBlazor form
      ↓
BuildBriefRequest
      ↓
WorkbenchApiClient
      ↓
ASP.NET Core endpoint
      ↓
BuildBrief.Create
```

The same request shape is visible in:

- Blazor serialization;
- Swagger/OpenAPI;
- the Postman collection;
- API endpoint binding.

This separation keeps HTTP transport reusable without exposing EF Core entities or aggregate internals to the browser.

## API execution flow

```text
POST /api/build-briefs
        ↓
BuildBriefRequest binding
        ↓
capability ID validation against canonical catalog
        ↓
BuildBrief.Create returns Result<BuildBrief>
        ↓
IRepository.AddAsync
        ↓
IUnitOfWork.SaveChangesAsync
        ↓
SQL Server commit
        ↓
DomainEventsSaveChangesInterceptor
        ↓
BuildBriefCreatedHandler
        ↓
BuildBriefResponse
```

`FoundationKit.WebApi` maps classified failures to RFC 7807 Problem Details. `FoundationKit.Blazor` classifies success, API failures, network failures, timeouts, empty payloads, and invalid JSON.

## Database ownership

For the Workbench only, migrations under:

```text
samples/FoundationKit.Workbench/Infrastructure/Migrations/
```

are the schema source of truth.

The API project owns:

- `Microsoft.EntityFrameworkCore.SqlServer`;
- `WorkbenchDbContext`;
- entity configuration;
- connection strings;
- migrations;
- startup migration policy.

No core package owns a provider or database schema.

## Canonical capability catalog

`catalog/foundationkit.catalog.json` is the hand-maintained source for implemented capabilities, package metadata, project ideas, adoption steps, and contact metadata.

It feeds:

- `GET /api/catalog`;
- the Blazor client;
- the GitHub Pages Blazor demo;
- generated `docs/FEATURES.md`.

`FoundationKit.CatalogGenerator` validates IDs, implemented status, and idea references. CI rejects generated documentation drift.

## Swagger and Postman

The API enables Swagger/OpenAPI at `/swagger`.

Swagger documents the same Contracts assembly used by the client. The Postman collection under `postman/` sends matching request JSON and stores the created identifier for follow-up retrieval.

Swagger and Postman are external API clients. They do not bypass domain creation, repositories, unit of work, migrations, or SQL Server.

## GitHub Pages boundary

The Pages workflow publishes the Blazor WebAssembly client, not a second JavaScript implementation.

GitHub Pages cannot execute ASP.NET Core or SQL Server. When `/api/runtime` is unavailable, the client:

- switches to demo mode;
- reads the static canonical catalog;
- disables database submission;
- does not pretend that persistence occurred.

The local API-hosted path remains authoritative.

## Domain events

`DomainEventsSaveChangesInterceptor` supports synchronous and asynchronous EF Core saves.

```text
Capture pending events
        ↓
Database save succeeds
        ↓
Clear aggregate event queues
        ↓
Dispatch handlers in process
```

A database failure dispatches nothing and leaves event queues unchanged. A handler failure occurs after commit and is surfaced to the caller. Use an outbox when durable delivery is required.

## CI verification

CI performs:

1. repository-boundary verification;
2. catalog and Postman JSON validation;
3. restore and Release build;
4. generated capability documentation drift detection;
5. core and Workbench tests;
6. Blazor-hosted API publish;
7. NuGet and symbol packaging;
8. Dockerized API + Blazor + SQL Server smoke testing;
9. real create and read persistence verification.

## Known production gaps

The Workbench is an architecture and integration reference. It does not yet provide:

- authentication and authorization;
- production secret management;
- rate limiting;
- telemetry export;
- backups and high availability;
- controlled deployment migrations;
- durable domain-event delivery;
- production ingress and TLS policy.

These are explicit product decisions, not hidden FoundationKit features.

## Versioning

The core is pre-1.0. Public API changes require tests, package documentation, catalog updates when capabilities change, generated documentation, and `CHANGELOG.md`.
