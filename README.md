# FoundationKit for .NET

[![Platform CI and Full-Stack Test](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/platform-ci.yml/badge.svg)](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/platform-ci.yml)
[![FoundationKit Packages](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/foundationkit-ci.yml/badge.svg)](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/foundationkit-ci.yml)

**FoundationKit** is a reusable .NET engineering foundation for building maintainable APIs, internal platforms, web applications, and business systems. It provides shared technical building blocks while leaving every product responsible for its own domain rules, contracts, database provider, routes, and user experience.

The repository is organized around four clearly separated roles:

1. **FoundationKit Core** — reusable NuGet-ready packages.
2. **Showcase** — the interactive GitHub Pages experience that asks: “What do you want to build today?”
3. **EntertainmentDocs** — the first validated reference consumer of FoundationKit.
4. **Future products** — independent repositories that consume versioned FoundationKit packages.

> The core is consumed, not hosted. The Showcase is the runnable experience. EntertainmentDocs proves the core in a real product.

## Live Showcase

https://a2sn2.github.io/foundationkit-dotnet/

The Showcase runs entirely in the browser. It does not send the visitor's idea anywhere until the visitor explicitly chooses a contact action.

## Repository map

```text
.
├── FoundationKit.sln              # Core package solution entry point
├── index.html                     # FoundationKit Showcase entry point
├── assets/css/showcase.css        # Showcase visual system
├── src/showcase.js                # Showcase interaction and idea analysis
│
├── platform/
│   ├── core/                      # FoundationKit package source
│   ├── src/                       # EntertainmentDocs backend and contracts
│   ├── apps/                      # EntertainmentDocs Admin, Client, and shared UI
│   ├── tests/                     # Core, domain, and architecture tests
│   ├── deploy/                    # Docker and Nginx integration stack
│   ├── postman/                   # API collection and environment
│   ├── scripts/                   # Setup, packaging, and smoke tests
│   └── EntertainmentDocs.sln      # Complete reference-consumer solution
│
├── core/README.md                 # Core ownership and package boundaries
├── samples/EntertainmentDocs/     # Reference-consumer navigation
├── templates/                     # Rules for starting future products
├── showcase/README.md             # Showcase behavior and contact configuration
├── docs/                          # Current architecture and historical reference
└── .github/                       # CI and project-idea intake
```

The current physical locations under `platform/` are intentionally retained to preserve the validated solution, Docker, migration, and CI paths. The architectural ownership is now explicit, while a future physical extraction can happen package-by-package without mixing it with behavior changes.

## FoundationKit packages

| Package | Responsibility |
|---|---|
| `FoundationKit.Domain` | Entities, aggregate roots, value objects, and domain events |
| `FoundationKit.Application` | Results, errors, commands, queries, persistence ports, specifications, and pagination |
| `FoundationKit.Infrastructure` | Provider-neutral EF Core repositories and domain-event adapters |
| `FoundationKit.WebApi` | RFC 7807 mapping, correlation IDs, and baseline HTTP security conventions |
| `FoundationKit.Blazor` | Typed HTTP execution, API errors, results, and asynchronous UI state |

FoundationKit does **not** own:

- product entities or workflows;
- product API routes or transport contracts;
- SQL Server or any other provider selection;
- product migrations;
- product authorization policies;
- product-specific pages, branding, or business rules;
- automatic CRUD exposure.

## Reference consumer: EntertainmentDocs

EntertainmentDocs is the first working product built on FoundationKit. It demonstrates:

- ASP.NET Core Identity and JWT authentication;
- role- and policy-based authorization;
- SQL Server and EF Core migrations;
- explicit commands, queries, and handlers;
- Blazor WebAssembly Admin and Client applications;
- MudBlazor shared UI;
- Postman contracts;
- Docker Compose and Nginx;
- architecture tests and a complete publishing smoke test.

Its business behavior remains product-owned. It is a reference consumer, not part of the reusable core.

## Quick start

### Build the FoundationKit packages

```bash
dotnet restore FoundationKit.sln
dotnet build FoundationKit.sln --configuration Release --no-restore
dotnet test platform/tests/FoundationKit.Tests/FoundationKit.Tests.csproj --configuration Release
```

### Build the complete EntertainmentDocs reference consumer

```bash
dotnet restore platform/EntertainmentDocs.sln
dotnet build platform/EntertainmentDocs.sln --configuration Release --no-restore
dotnet test platform/EntertainmentDocs.sln --configuration Release --no-build
```

### Run the Showcase locally

```bash
python -m http.server 8000
```

Then open `http://localhost:8000/`.

### Package FoundationKit

Windows:

```powershell
powershell -ExecutionPolicy Bypass -File .\platform\scripts\pack-foundation.ps1
```

Linux/macOS:

```bash
bash platform/scripts/pack-foundation.sh
```

Packages are written to `platform/artifacts/foundation/`.

## Dependency direction

```text
Product.Domain          → FoundationKit.Domain
Product.Application     → FoundationKit.Application + Product.Domain
Product.Infrastructure  → FoundationKit.Infrastructure + Product.Application
Product.Api             → FoundationKit.WebApi + Product.Application/Infrastructure
Product.Blazor          → FoundationKit.Blazor + Product.Contracts
```

Dependencies point inward. FoundationKit never references a consuming product.

## Starting another product

A new product should live in its own repository and consume versioned FoundationKit packages. See:

- [Core ownership](core/README.md)
- [EntertainmentDocs reference consumer](samples/EntertainmentDocs/README.md)
- [Future-product templates](templates/README.md)
- [Repository boundaries](docs/architecture/REPOSITORY-BOUNDARIES.md)
- [Safe reorganization decision](docs/REORGANIZATION.md)

## Documentation truth rules

- EF Core migrations are the database schema source of truth.
- Implemented behavior must be distinguished from design intent and future recommendations.
- Runtime changes must update the relevant documentation, contracts, tests, and operational assets.
- Historical repository-reference chapters remain available under `docs/repository-reference/`; current navigation starts at `docs/README.md`.

## Contact and project ideas

The Showcase can turn a visitor's idea into a lightweight foundation map and then offer a public GitHub project-inquiry form. Visitors are warned not to post confidential details in a public issue.

Repository owner and technical contact: **ALHassan ALShami** — [GitHub profile](https://github.com/a2sn2).
