# Architecture

## Repository purpose

FoundationKit supplies reusable technical building blocks and a verified way to explore them. The repository separates three responsibilities:

```text
Reusable core packages
        ↓ consumed by
Local Workbench sample ── SQL Server + migrations + hosted UI

Canonical capability catalog
        ├── local Workbench API and UI
        ├── generated Markdown reference
        └── static GitHub Pages demo
```

The Workbench is a consuming sample, not a sixth core package. GitHub Pages is a static demo, not a hosted .NET application.

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

May depend on Domain. It owns use-case contracts and ports but does not depend on EF Core, ASP.NET Core, SQL Server, or a UI framework.

### Infrastructure

May depend on Application, Domain, and provider-neutral EF Core abstractions. It must not reference a relational provider package or an ASP.NET Core host.

### WebApi

May depend on Application and the ASP.NET Core shared framework. It adapts classified results to HTTP and supplies reusable middleware.

### Blazor

Owns browser-side transport and state helpers. It must not reference Domain, Application, Infrastructure, EF Core, or server hosting.

Architecture tests enforce assembly references. `scripts/verify-repository.sh` additionally prevents SQL Server provider packages and EF migrations from entering `src/`.

## Local Workbench

`samples/FoundationKit.Workbench` is an executable ASP.NET Core application that demonstrates a valid consumer boundary:

```text
FoundationKit.Workbench
    ├── references Domain, Application, Infrastructure, and WebApi
    ├── selects Microsoft.EntityFrameworkCore.SqlServer
    ├── owns WorkbenchDbContext
    ├── owns entity configuration and EF Core migrations
    ├── hosts API endpoints and static UI assets
    └── stores BuildBrief aggregates in SQL Server
```

The sample uses FoundationKit in its implemented flow:

```text
POST /api/build-briefs
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
```

The Workbench applies its own migrations on startup with bounded retry because the Docker SQL Server container may still be starting. This behavior is appropriate for the local sample; production products must choose an explicit deployment and migration policy.

## Database schema source of truth

For the Workbench only, EF Core migrations under:

```text
samples/FoundationKit.Workbench/Infrastructure/Migrations/
```

are the schema source of truth. The initial migration creates `BuildBriefs` with the project summary, selected capabilities serialized as JSON, notes, priorities, and UTC creation time.

No core package owns a database schema.

## Canonical capability catalog

`catalog/foundationkit.catalog.json` is the only hand-maintained source for:

- implemented package capabilities;
- public types associated with each capability;
- project idea recommendations;
- adoption steps;
- contact metadata.

`FoundationKit.CatalogGenerator` validates unique IDs, implemented status, and idea references, then generates `docs/FEATURES.md`. CI compares generated output with the committed file and rejects drift.

The local Workbench returns the catalog from `/api/catalog`. The Pages deployment copies the same file into its static artifact. There is no second feature list hidden in JavaScript or HTML.

## GitHub Pages runtime boundary

The Pages workflow deploys only:

- `site/` static assets;
- the canonical catalog JSON.

The browser first tries the relative `api/runtime` endpoint. In the local Workbench it receives `mode=local`; on GitHub Pages the endpoint does not exist, so the UI explicitly switches to demo mode. Demo mode:

- does not call a backend;
- does not connect to SQL Server;
- does not save visitor answers;
- creates the contact summary locally in the browser;
- sends nothing until the visitor opens the external GitHub contact action.

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

A database failure dispatches nothing and leaves aggregate queues unchanged. A handler failure occurs after the commit and is surfaced to the caller, but cleared events are not dispatched again automatically. Use an outbox for durable delivery guarantees.

## HTTP

`FoundationKit.WebApi` supplies classified result mapping, RFC 7807 Problem Details, a bounded correlation-ID middleware, and baseline response headers. The Workbench demonstrates these features but still owns its routes and product behavior.

Authentication, authorization, rate limiting, OpenAPI, production TLS, secrets, user identity, and product-specific audit requirements are intentionally not implemented by the reusable core or the public demo.

## CI and operational verification

The primary CI workflow performs:

1. repository-boundary verification;
2. JSON, JavaScript, and HTML validation;
3. restore and Release build with warnings as errors;
4. catalog validation and generated-doc drift detection;
5. unit and architecture tests;
6. creation of five NuGet and symbol packages;
7. a Dockerized Workbench + SQL Server smoke test that saves and reads a real brief.

A separate Pages workflow assembles and deploys only static assets.

## Known limits and production gaps

Implemented behavior must not be confused with production completeness:

- the Workbench is a local demonstration and discovery tool;
- there is no authentication or authorization around saved local briefs;
- startup migration is not a recommended production migration strategy;
- SQL Server credentials are ephemeral in helper scripts but the Docker topology remains development-only;
- GitHub contact issues are public;
- in-process events are not durable;
- no telemetry backend, distributed cache, queue, outbox, secrets store, or production deployment topology is selected.

These are product and operational decisions, not hidden features of FoundationKit.

## Versioning

The core is pre-1.0. Package versions are coordinated in `src/Directory.Build.props`. Public API changes must update tests, the canonical catalog when capability behavior changes, generated documentation, and `CHANGELOG.md`.
