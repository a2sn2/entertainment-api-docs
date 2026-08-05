# Entertainment Services API Documentation & FoundationKit

[![Platform CI and Full-Stack Test](https://github.com/a2sn2/entertainment-api-docs/actions/workflows/platform-ci.yml/badge.svg)](https://github.com/a2sn2/entertainment-api-docs/actions/workflows/platform-ci.yml)
[![FoundationKit Packages](https://github.com/a2sn2/entertainment-api-docs/actions/workflows/foundationkit-ci.yml/badge.svg)](https://github.com/a2sn2/entertainment-api-docs/actions/workflows/foundationkit-ci.yml)

This repository contains two connected deliverables:

1. **A static interactive portal** that documents the Entertainment Services API and is published through GitHub Pages.
2. **A reusable .NET engineering foundation and reference platform** for building secure, maintainable products with Clean Architecture, DDD-style boundaries, ASP.NET Core, SQL Server, Blazor WebAssembly, and repeatable testing.

The repository is therefore not limited to API documentation. The documentation platform is the first working product built on top of the reusable core named **FoundationKit**.

## Live static documentation

https://a2sn2.github.io/entertainment-api-docs/

The GitHub Pages site is a static preview only. The dynamic API, SQL Server database, Identity system, Admin application, and Client application require a .NET application host or the included Docker test stack.

---

## Repository goals

The repository is designed to provide:

- a clear separation between reusable technical building blocks and product-specific business behavior;
- a modular monolith that can grow without prematurely introducing microservices;
- explicit commands, queries, handlers, repositories, and contracts instead of large generic managers;
- a provider-agnostic reusable core while allowing each product to choose its own database provider;
- a database schema controlled by EF Core migrations;
- an API that remains the authorization and business-rule boundary;
- Blazor frontends that communicate only through typed HTTP contracts;
- Postman collections for repeatable API testing outside Swagger;
- architecture tests that prevent accidental dependency violations;
- full-stack testing with SQL Server, API, Admin, Client, static documentation, and Nginx;
- internal NuGet packages that can be reused by future products.

The guiding rule is:

> Share technical behavior that is genuinely reusable. Keep business rules, routes, contracts, database-provider configuration, and product UI inside the product that owns them.

---

## Technology overview

| Area | Technology |
|---|---|
| Reusable core | FoundationKit `0.1.0` |
| Backend runtime | .NET 8 / ASP.NET Core Minimal APIs |
| Domain and application style | Clean Architecture, DDD-style aggregates, explicit commands and queries |
| Persistence | Entity Framework Core 8 |
| Product database | Microsoft SQL Server |
| Authentication | ASP.NET Core Identity and JWT Bearer tokens |
| Authorization | Role- and policy-based authorization |
| Admin frontend | Blazor WebAssembly and MudBlazor |
| Public client | Blazor WebAssembly and MudBlazor |
| Shared frontend behavior | FoundationKit.Blazor and EntertainmentDocs.Ui |
| Static portal | HTML, CSS, browser-native JavaScript modules, PWA manifest, service worker |
| API testing | Postman collection and environment |
| Automated tests | xUnit, architecture rules, shell smoke tests |
| Integration environment | Docker Compose, SQL Server 2022 container, Nginx gateway |
| Continuous integration | GitHub Actions |

Global .NET settings are centralized under `platform/Directory.Build.props` and `platform/Directory.Packages.props`. Nullable reference types and implicit usings are enabled, and compiler warnings are treated as errors.

---

## Repository map

```text
.
├── index.html                     # Static documentation entry point
├── 404.html                       # GitHub Pages fallback
├── manifest.webmanifest           # Static portal PWA metadata
├── sw.js                          # Static portal service worker
├── assets/
│   └── css/                       # Static portal design tokens, layout, components, pages
├── pages/                         # Static HTML shells for each documentation page
├── src/                           # Static portal JavaScript architecture
│   ├── domain/                    # API contracts, purchase flow, quality and governance data
│   ├── application/               # Search and portal use cases
│   ├── infrastructure/            # Static repository and browser preferences
│   └── presentation/              # Page renderers, components, interactions and main entry point
│
├── platform/
│   ├── EntertainmentDocs.sln      # 15-project .NET solution
│   ├── core/                      # Reusable FoundationKit projects
│   ├── src/                       # Product backend and HTTP contracts
│   ├── apps/                      # Admin, Client and shared UI projects
│   ├── tests/                     # Product, core and architecture tests
│   ├── postman/                   # Importable Postman collection and local environment
│   ├── deploy/                    # Dockerfiles, Compose stack and Nginx gateway
│   ├── scripts/                   # Local setup, stack, smoke-test and package scripts
│   └── docs/                      # Detailed architecture and operational guides
│
├── .github/workflows/             # Platform and FoundationKit CI pipelines
├── .devcontainer/                 # Optional .NET 8 development container
└── .vscode/                       # Repository tasks for VS Code/Codespaces
```

---

# 1. Static interactive documentation portal

The repository root hosts an independent static portal for the Entertainment Services API. It requires no API server or database and can be hosted directly by GitHub Pages.

## Static portal architecture

```text
src/domain
    ↓
src/application
    ↓
src/infrastructure
    ↓
src/presentation
    ↓
GitHub Pages / browser
```

### `src/domain`

Contains the documentation source data rather than runtime business entities:

- document control metadata;
- observed API contracts;
- request and response fields;
- purchase workflow and identifier rules;
- test scenarios and error scenarios;
- known limitations;
- open questions requiring backend confirmation.

Contract spellings such as `massage`, `statues`, `feilds`, and related names are intentionally preserved where they reflect the observed external API contract. They must not be silently corrected in requests unless the external contract changes.

### `src/application`

Contains browser-side use cases. The current implementation builds a documentation search index and ranks matches by exact title, title prefix, title content, and indexed body content.

### `src/infrastructure`

Provides adapters for the static portal:

- `StaticDocumentationRepository` exposes domain documentation to the presentation layer;
- browser preference storage manages theme and navigation preferences.

### `src/presentation`

Contains the browser entry point, reusable UI components, interactions, and page renderers.

The current navigation includes:

- Overview;
- Quick Start;
- Purchase Flow;
- API Reference;
- Offline Playground;
- Error Assistant;
- Test Coverage;
- Platform Architecture;
- Governance;
- Known Limitations;
- Open Questions.

The portal also includes a PWA manifest and service worker for installability and cached static access.

---

# 2. FoundationKit reusable core

FoundationKit is the reusable engineering core under `platform/core/`. It is intentionally divided into small projects so a product references only the layers it needs.

```text
platform/core/
├── FoundationKit.Domain
├── FoundationKit.Application
├── FoundationKit.Infrastructure
├── FoundationKit.WebApi
└── FoundationKit.Blazor
```

## FoundationKit.Domain

Framework-independent domain primitives:

- `Entity<TId>` with identity-based equality and safe transient-entity behavior;
- `AggregateRoot<TId>`;
- `ValueObject`;
- domain-event primitives;
- domain exceptions.

This project does not reference Entity Framework Core, ASP.NET Core, Blazor, a database provider, or any product project.

## FoundationKit.Application

Reusable use-case and application abstractions:

- `Result` and `Result<T>`;
- classified `Error` values;
- `ICommand`, `IQuery`, and their handlers;
- `IReadRepository` and `IRepository`;
- specifications;
- pagination models;
- `IUnitOfWork`;
- `ICurrentUser`;
- `IClock`;
- validation abstractions;
- domain-event handler contracts.

Supported error categories include:

```text
Validation
NotFound
Conflict
Unauthorized
Forbidden
BusinessRule
Failure
```

## FoundationKit.Infrastructure

Provider-agnostic infrastructure adapters:

- generic EF Core repository behavior;
- specification evaluation;
- EF-based unit-of-work support;
- domain-event dispatching;
- save-change event integration.

This project references base EF Core only. It does not choose SQL Server, PostgreSQL, SQLite, or another provider. Database-provider selection belongs to the consuming product.

## FoundationKit.WebApi

Shared ASP.NET Core API behavior:

- mapping `Result` values to HTTP responses;
- RFC 7807 `ProblemDetails` generation for result-based endpoints;
- correlation IDs;
- baseline security headers;
- reusable request-pipeline registration.

Result-based errors map to HTTP status codes as follows:

| Error type | HTTP status |
|---|---:|
| Validation | 400 |
| Unauthorized | 401 |
| Forbidden | 403 |
| Not Found | 404 |
| Conflict | 409 |
| Business Rule | 422 |
| Failure | 500 |

Authentication and ASP.NET Core Identity endpoints may still return their native status responses where appropriate.

## FoundationKit.Blazor

Shared browser-side transport and state behavior:

- `ApiClientBase`;
- `ApiResult` and `ApiResult<T>`;
- structured `ApiError` values;
- API response and `ProblemDetails` reading;
- network and timeout failure handling;
- `AsyncState<T>` for loading, success, empty, and failure state.

This prevents each Blazor feature from duplicating raw `HttpClient`, deserialization, status-code handling, and exception handling.

## What does not belong in FoundationKit

FoundationKit deliberately does not contain:

- product-specific business rules;
- product entities or workflows;
- HTTP routes;
- request and response contracts owned by a product;
- SQL Server configuration;
- product migrations;
- product authorization policies;
- product-specific MudBlazor pages or branding;
- generic controllers that automatically expose CRUD;
- generic business managers that hide use-case intent.

Generic behavior stops at technical building blocks. Business actions remain explicit commands, queries, and handlers.

---

# 3. EntertainmentDocs reference product

`platform/src`, `platform/apps`, and the product tests form the first reference product built on FoundationKit.

## Solution projects

The solution currently contains 15 projects.

### Reusable core

| Project | Responsibility |
|---|---|
| `FoundationKit.Domain` | Domain primitives and events |
| `FoundationKit.Application` | Results, messaging, persistence ports, specifications and pagination |
| `FoundationKit.Infrastructure` | Provider-neutral EF Core and event adapters |
| `FoundationKit.WebApi` | API result mapping and middleware |
| `FoundationKit.Blazor` | Typed HTTP and UI state behavior |

### Product backend

| Project | Responsibility |
|---|---|
| `EntertainmentDocs.Domain` | Documentation aggregates, versions, statuses and invariants |
| `EntertainmentDocs.Application` | Product commands, queries, handlers, DTOs and ports |
| `EntertainmentDocs.Contracts` | Transport-only HTTP request and response records |
| `EntertainmentDocs.Infrastructure` | SQL Server, EF Core migrations, Identity, JWT and repositories |
| `EntertainmentDocs.Api` | Minimal API endpoints, policies and composition root |

### Product frontend

| Project | Responsibility |
|---|---|
| `EntertainmentDocs.Admin` | Authenticated administration interface |
| `EntertainmentDocs.Client` | Public read-only documentation interface |
| `EntertainmentDocs.Ui` | Shared MudBlazor theme and reusable UI states/components |

### Tests

| Project | Responsibility |
|---|---|
| `FoundationKit.Tests` | Result, entity and architecture dependency tests |
| `EntertainmentDocs.Domain.Tests` | Documentation aggregate behavior tests |

---

## Dependency direction

```text
FoundationKit.Domain
        ↑
FoundationKit.Application
        ↑
FoundationKit.Infrastructure / FoundationKit.WebApi / FoundationKit.Blazor

Product.Domain
        ↑
Product.Application
        ↑
Product.Infrastructure
        ↑
Product.Api

Product.Admin / Product.Client
        ├── Product.Contracts
        ├── Product.Ui
        └── FoundationKit.Blazor
```

Key rules enforced by architecture tests:

- FoundationKit.Domain has no outer-layer or framework dependencies;
- FoundationKit.Infrastructure is independent of SQL Server, PostgreSQL, SQLite, and ASP.NET Core hosting;
- Product Domain does not reference Application, Infrastructure, API, EF Core, or ASP.NET Core;
- Product Application does not reference Product Infrastructure or API;
- Contracts remain transport-only;
- Product Infrastructure may reference inward but not API or frontend projects;
- FoundationKit.Application does not reference outer adapters.

---

## Product bounded contexts

The platform is organized as a modular monolith with these current or planned business areas:

1. **Identity & Access** — users, authentication, roles and authorization.
2. **Documentation Catalog** — document references, slugs, versions and retrieval.
3. **Publishing Workflow** — draft, review, publish and archive behavior.
4. **Audit & Operations** — audit storage, health and operational support.
5. **Integration Registry** — future environments, schemas and provider metadata.

The modular monolith keeps deployment simple while preserving boundaries that can later be extracted if a real operational need appears.

---

## Documentation aggregate and workflow

The `DocumentationDocument` aggregate owns its state and version rules.

```text
Draft
  ↓ submit for review
InReview
  ↓ publish
Published
  ↓ archive
Archived
```

Important invariants:

- a document starts as `Draft`;
- a document needs at least one version before entering review;
- only a draft document can enter review;
- only an in-review document can be published;
- archived documents cannot receive new versions;
- adding a new version to a published document returns it to `Draft`;
- reference and slug uniqueness are checked by the application and repository boundary.

Business operations are implemented as dedicated use cases, including:

- `CreateDocumentCommand`;
- `AddDocumentVersionCommand`;
- `SubmitDocumentForReviewCommand`;
- `PublishDocumentCommand`;
- `ListPublishedDocumentsQuery`;
- `GetPublishedDocumentQuery`.

There is no large generic document manager. Each use case has one explicit purpose and one handler.

---

## Persistence and database ownership

The product chooses Microsoft SQL Server in `EntertainmentDocs.Infrastructure`.

`AppDbContext`:

- derives from ASP.NET Core Identity's GUID-based `IdentityDbContext`;
- implements the product unit-of-work contract;
- stores documentation documents and versions;
- stores Identity users, roles, claims, logins and tokens;
- contains an audit-entry model;
- applies entity configurations from the Infrastructure assembly.

EF Core migrations are the source of truth for schema evolution. Do not manually evolve the schema in SSMS and then leave the change outside migrations.

Current database environments:

| Environment | Database | Authentication |
|---|---|---|
| Local Development | `EntertainmentDocs_Dev` | Windows Authentication |
| Automated Testing | `EntertainmentDocs_Test` | Isolated SQL Server container credentials |
| Production | Product deployment decision | Managed credentials or workload identity required |

The audit storage model exists, while production-grade automatic capture, immutable retention, monitoring, and operational policy remain production-readiness work.

---

## Authentication and authorization

The API uses ASP.NET Core Identity and JWT Bearer authentication.

Current roles:

```text
Administrator
Editor
Reviewer
Reader
```

Current policy behavior:

| Capability | Allowed roles |
|---|---|
| Read published documents | Public |
| Create documents and versions | Administrator, Editor |
| Submit documents for review | Administrator, Editor |
| Publish reviewed documents | Administrator, Reviewer |
| Manage users and roles | Administrator |

The Admin application uses browser `sessionStorage`, a custom `AuthenticationStateProvider`, role-aware navigation, and Bearer-token request creation. These frontend checks improve user experience only. The API remains the real authorization boundary.

Security baseline currently includes:

- JWT issuer, audience, signing-key and lifetime validation;
- 30-second token clock skew;
- fixed-window API rate limiting;
- CORS that is permissive only in Development and Testing;
- fail-closed CORS configuration outside Development and Testing;
- HSTS outside Development and Testing;
- HTTPS redirection outside Testing;
- health checks for the API and database;
- correlation IDs;
- `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, and `Permissions-Policy` headers.

Swagger is enabled only in Development and Testing.

---

## Frontend architecture

The frontend uses Blazor WebAssembly and MudBlazor.

```text
Razor page or component
        ↓
Feature-specific typed API client / feature state
        ↓
EntertainmentDocs.Contracts
        ↓ HTTP
EntertainmentDocs.Api
        ↓
Application use case
        ↓
Domain and persistence
```

Frontend rules:

- Razor pages render state, collect input and trigger feature actions;
- typed API clients own routes, serialization, authentication headers and response handling;
- shared transport behavior belongs in FoundationKit.Blazor;
- shared visual behavior belongs in EntertainmentDocs.Ui;
- request and response payloads belong in EntertainmentDocs.Contracts;
- frontend projects do not reference EF Core, SQL Server, Product Infrastructure, or Product Domain;
- hiding a button is never treated as backend authorization.

Current Admin areas:

- authentication;
- dashboard;
- documents and publishing;
- users and roles;
- API request reference.

Current Client areas:

- published-document catalog;
- search;
- document details.

---

## Current API surface

| Method | Route | Authentication |
|---|---|---|
| `GET` | `/` | Public |
| `GET` | `/health` | Public |
| `POST` | `/api/v1/auth/login` | Public |
| `GET` | `/api/v1/documents` | Public |
| `GET` | `/api/v1/documents/{slug}` | Public |
| `POST` | `/api/v1/admin/documents` | Administrator or Editor |
| `POST` | `/api/v1/admin/documents/{id}/versions` | Administrator or Editor |
| `POST` | `/api/v1/admin/documents/{id}/submit-review` | Administrator or Editor |
| `POST` | `/api/v1/admin/documents/{id}/publish` | Administrator or Reviewer |
| `GET` | `/api/v1/admin/users` | Administrator |
| `POST` | `/api/v1/admin/users` | Administrator |
| `PUT` | `/api/v1/admin/users/{id}/roles` | Administrator |

For complete bodies, examples, environment variables, saved identifiers and execution order, use the Postman assets described below.

---

# 4. Local development

## Requirements

Recommended Windows development environment:

- Visual Studio 2026 or another .NET 8-capable IDE;
- .NET 8 SDK;
- Microsoft SQL Server;
- SQL Server Management Studio for database inspection;
- Git;
- Postman for repeatable API testing.

Docker Desktop is optional for the isolated full-stack test environment.

## Clone and open

```powershell
git clone https://github.com/a2sn2/entertainment-api-docs.git
cd entertainment-api-docs
```

Open:

```text
platform/EntertainmentDocs.sln
```

## Configure local SQL Server

From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\platform\scripts\setup-local-sqlserver.ps1
```

For a named or explicit SQL Server instance:

```powershell
powershell -ExecutionPolicy Bypass -File .\platform\scripts\setup-local-sqlserver.ps1 -Server "MACHINE\INSTANCE"
```

The script:

1. restores local .NET tools;
2. restores solution packages;
3. builds the solution;
4. applies committed EF Core migrations to `EntertainmentDocs_Dev`.

Development-only administrator:

```text
Email:    admin@local.test
Password: LocalAdmin!2026
```

These values are for local Development only and must never be reused in staging or production.

## Visual Studio startup

Configure multiple startup projects using the `http` profiles:

```text
EntertainmentDocs.Api      Start
EntertainmentDocs.Client   Start
EntertainmentDocs.Admin    Start
```

The profiles intentionally do not open debugger-managed browser windows. After all three applications report that they are listening, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\platform\scripts\open-local-platform.ps1
```

Local URLs:

| Surface | URL |
|---|---|
| API Swagger | `http://localhost:5080/swagger` |
| API health | `http://localhost:5080/health` |
| Client | `http://localhost:5081` |
| Admin | `http://localhost:5082/login` |

The page-opening script needs to be run only once per active local session.

## Command-line build and tests

```powershell
cd platform
dotnet restore EntertainmentDocs.sln
dotnet build EntertainmentDocs.sln --configuration Release --no-restore
dotnet test EntertainmentDocs.sln --configuration Release --no-build
```

---

# 5. Postman API testing

Swagger is useful for local discovery. Postman is the repeatable operational request suite.

Import both files:

```text
platform/postman/EntertainmentDocs.postman_collection.json
platform/postman/EntertainmentDocs.Local.postman_environment.json
```

Select:

```text
Entertainment Docs - Local
```

Recommended execution order:

1. Health Check;
2. Login;
3. Create Document;
4. Add Document Version;
5. Submit Document for Review;
6. Publish Document;
7. List Published Documents;
8. Get Published Document by Slug;
9. Create User;
10. List Users;
11. Replace User Roles.

The collection stores these variables automatically after successful requests:

```text
accessToken
documentId
documentVersionId
userId
```

Detailed request bodies and examples are documented in [`platform/docs/POSTMAN-REQUESTS.md`](platform/docs/POSTMAN-REQUESTS.md).

Contract synchronization rule:

1. update `EntertainmentDocs.Contracts`;
2. update the API endpoint;
3. update the typed frontend client;
4. update the Postman collection and documentation;
5. update affected tests in the same change.

---

# 6. Full-stack Docker test environment

The repository includes an isolated integration topology:

```text
Nginx gateway
├── API
├── Admin Blazor app
├── Client Blazor app
└── Static documentation portal

API
└── SQL Server test container
```

Start the stack:

```bash
chmod +x platform/scripts/*.sh
platform/scripts/start-test-stack.sh
```

Or directly:

```bash
docker compose -f platform/deploy/docker-compose.test.yml up --build -d
```

Test URLs:

| Surface | URL |
|---|---|
| Client | `http://localhost:8080/client/` |
| Admin | `http://localhost:8080/admin/` |
| Static documentation | `http://localhost:8080/docs/` |
| API health | `http://localhost:8080/api/health` |
| SQL Server | `localhost,14333` |

Run the complete smoke test:

```bash
export TEST_ADMIN_EMAIL=admin@test.local
export TEST_ADMIN_PASSWORD='TestAdmin!2026'
platform/scripts/smoke-test.sh
```

Stop and remove the isolated test volume:

```bash
docker compose -f platform/deploy/docker-compose.test.yml down --volumes --remove-orphans
```

All credentials in the test Compose file are explicitly test-only.

---

# 7. FoundationKit packages

FoundationKit projects are packable internal NuGet packages at version `0.1.0`.

Create packages on Windows:

```powershell
powershell -ExecutionPolicy Bypass -File .\platform\scripts\pack-foundation.ps1
```

Create packages on Linux or macOS:

```bash
bash platform/scripts/pack-foundation.sh
```

Output:

```text
platform/artifacts/foundation/
├── FoundationKit.Domain.0.1.0.nupkg
├── FoundationKit.Application.0.1.0.nupkg
├── FoundationKit.Infrastructure.0.1.0.nupkg
├── FoundationKit.WebApi.0.1.0.nupkg
└── FoundationKit.Blazor.0.1.0.nupkg
```

Symbol packages are produced as well. GitHub Actions uploads the package set as a workflow artifact. The repository does not currently publish these packages to a public NuGet feed.

---

# 8. Starting a new product with FoundationKit

Recommended product shape:

```text
src/
├── Product.Domain
├── Product.Application
├── Product.Contracts
├── Product.Infrastructure
└── Product.Api

apps/
├── Product.Admin
├── Product.Client
└── Product.Ui

tests/
├── Product.Domain.Tests
├── Product.Application.Tests
├── Product.IntegrationTests
└── Product.ArchitectureTests
```

Reference direction:

```text
Product.Domain          → FoundationKit.Domain
Product.Application     → FoundationKit.Application + Product.Domain
Product.Infrastructure  → FoundationKit.Infrastructure + Product.Application
Product.Api             → FoundationKit.WebApi + Product.Application/Infrastructure
Product.Admin/Client    → FoundationKit.Blazor + Product.Contracts + Product.Ui
```

For every new business capability:

1. model the aggregate, entity or value object in Product Domain;
2. define one command or query per use case;
3. implement a dedicated handler;
4. add a repository port only when persistence is needed;
5. extend generic repositories only with business-language queries;
6. define transport contracts separately;
7. map a thin API endpoint;
8. add a typed Blazor client and feature state;
9. add Postman requests and automated tests;
10. preserve the dependency rules with architecture tests.

Do not move behavior into FoundationKit merely because two product files look similar. Extract it only after it proves reusable across real products.

See [`platform/docs/NEW-PROJECT-BOOTSTRAP.md`](platform/docs/NEW-PROJECT-BOOTSTRAP.md).

---

# 9. Continuous integration and quality gates

## Platform CI and Full-Stack Test

`.github/workflows/platform-ci.yml` performs:

- Postman collection and environment JSON validation;
- .NET restore;
- Release build;
- all solution tests;
- test-result artifact upload;
- Docker Compose validation;
- SQL Server startup;
- EF Core migration application;
- API, Admin, Client and static docs build/start;
- gateway health checks;
- end-to-end create → version → review → publish → public retrieval;
- test-stack and volume cleanup.

## FoundationKit Packages

`.github/workflows/foundationkit-ci.yml` performs:

- solution restore and Release build;
- FoundationKit unit and architecture tests;
- package creation;
- symbol-package creation;
- workflow artifact upload.

No feature or core change should be merged while its required CI checks are failing.

---

# 10. Publishing and production boundary

## Static portal

The root static portal can be published through GitHub Pages because it contains browser assets only.

## Dynamic platform

The following require an application platform and cannot run on GitHub Pages:

- ASP.NET Core API;
- SQL Server;
- Admin authentication and authorization;
- dynamic document management;
- Blazor Admin and Client applications when connected to the API.

Before production release, the repository still requires environment-specific work including:

- managed secrets;
- approved migration deployment strategy;
- managed SQL Server or Azure SQL;
- encrypted connections, backups and recovery procedures;
- domain names, TLS, reverse proxy, CORS and WAF configuration;
- structured logs, metrics, tracing and alerting;
- stronger account lifecycle, password reset, e-mail verification and MFA;
- production-grade audit capture and retention;
- authorization, security, load, recovery and penetration testing;
- threat modeling, SAST and dependency scanning;
- rollback, incident response, RPO and RTO definitions.

This repository provides a production-oriented foundation, not a claim of production certification.

---

# 11. Security rules

This repository is public.

Never commit:

- production passwords;
- production JWT signing keys;
- production connection strings;
- access or refresh tokens;
- personal data;
- real customer or player identifiers;
- private provider credentials;
- unredacted operational screenshots.

Development and test credentials committed in configuration or Compose files are isolated, non-production values and must never be reused elsewhere.

---

# 12. Detailed documentation

| Document | Purpose |
|---|---|
| [`platform/README.md`](platform/README.md) | Platform quick start |
| [`platform/core/README.md`](platform/core/README.md) | FoundationKit package overview |
| [`platform/docs/ARCHITECTURE.md`](platform/docs/ARCHITECTURE.md) | Modular monolith and bounded contexts |
| [`platform/docs/FRONTEND-ARCHITECTURE.md`](platform/docs/FRONTEND-ARCHITECTURE.md) | Blazor and MudBlazor frontend rules |
| [`platform/docs/LOCAL-SQLSERVER.md`](platform/docs/LOCAL-SQLSERVER.md) | Local SQL Server and SSMS setup |
| [`platform/docs/POSTMAN-REQUESTS.md`](platform/docs/POSTMAN-REQUESTS.md) | Endpoint request and response guide |
| [`platform/docs/RUN-TEST-STACK.md`](platform/docs/RUN-TEST-STACK.md) | Complete Docker integration environment |
| [`platform/docs/NEW-PROJECT-BOOTSTRAP.md`](platform/docs/NEW-PROJECT-BOOTSTRAP.md) | Starting future products with FoundationKit |
| [`platform/docs/PRODUCTION-READINESS.md`](platform/docs/PRODUCTION-READINESS.md) | Remaining production controls |

---

## Current status

```text
Static GitHub Pages portal                  Available
FoundationKit reusable core                 v0.1.0
.NET solution                               15 projects
Local SQL Server setup                      Automated
API / Admin / Client local startup          Verified
Postman collection                          Available
FoundationKit package generation            Verified
Architecture dependency tests               Enabled
SQL Server full-stack smoke test             Enabled
Production certification                    Not claimed
```
