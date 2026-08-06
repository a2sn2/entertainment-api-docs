# FoundationKit for .NET

[![FoundationKit CI](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/ci.yml)
[![Pages Demo](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/pages.yml/badge.svg)](https://a2sn2.github.io/foundationkit-dotnet/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Target: .NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)

FoundationKit is a reusable .NET core for Clean Architecture and domain-driven design. The repository has three deliberately separated roles:

1. **Reusable packages** under `src/` — product-independent building blocks.
2. **Local Workbench** under `samples/FoundationKit.Workbench/` — a real ASP.NET Core consumer that uses SQL Server and EF Core migrations.
3. **GitHub Pages demo** under `site/` — the same creative discovery experience without backend execution or persistence.

The SQL Server provider, migrations, hosted API, and user experience belong to the Workbench sample. They do not leak into the reusable core packages.

## Open the experience

Static GitHub Pages demo:

<https://a2sn2.github.io/foundationkit-dotnet/>

The Pages version is intentionally frontend-only. It loads the implemented capability catalog, asks what the visitor wants to build, and creates a public-safe contact summary. It does not run the .NET core, call SQL Server, or save answers.

## Run the real Workbench locally

### Windows PowerShell

```powershell
.\scripts\run-workbench.ps1
```

### Bash

```bash
./scripts/run-workbench.sh
```

The script:

- generates an ephemeral local SQL Server development password;
- builds and starts SQL Server and the ASP.NET Core Workbench with Docker Compose;
- applies the Workbench EF Core migrations;
- waits for the health endpoint;
- opens <http://localhost:8080> automatically.

Stop it with:

```powershell
.\scripts\stop-workbench.ps1
```

or:

```bash
./scripts/stop-workbench.sh
```

To use an existing Windows SQL Server instance instead of Docker:

```powershell
$env:ConnectionStrings__Workbench="Server=localhost;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
dotnet run --project samples/FoundationKit.Workbench
```

The browser opens automatically through `launchSettings.json`. See [Workbench setup and operations](docs/WORKBENCH.md) for named instances, ports, migrations, API routes, and troubleshooting.

## What the Workbench proves

The local sample consumes FoundationKit rather than imitating it:

- `BuildBrief` derives from `AggregateRoot<Guid>`;
- creation returns a classified `Result<BuildBrief>`;
- persistence uses `IRepository`, `IUnitOfWork`, `EfRepository`, and SQL Server;
- the EF Core interceptor dispatches `BuildBriefCreated` after a successful save;
- validation failures become RFC 7807 Problem Details;
- a real EF Core migration owns the `BuildBriefs` schema;
- the UI reads the same capability catalog used by documentation and GitHub Pages.

## Reusable packages

| Package | Responsibility |
|---|---|
| `FoundationKit.Domain` | Entities, aggregate roots, value objects, domain exceptions, and domain-event contracts |
| `FoundationKit.Application` | Commands, queries, results, validation, pagination, repository ports, and use-case abstractions |
| `FoundationKit.Infrastructure` | Provider-neutral EF Core repositories, unit of work adapter, specifications, and in-process event dispatch |
| `FoundationKit.WebApi` | ASP.NET Core result mapping, Problem Details, correlation IDs, and baseline response headers |
| `FoundationKit.Blazor` | Typed HTTP results, response parsing, and reusable asynchronous UI state |

Dependency direction:

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
    independent browser helper package
```

`FoundationKit.Infrastructure` references provider-neutral EF Core abstractions only. SQL Server, PostgreSQL, SQLite, migrations, connection strings, and transaction policy belong to consuming applications.

## Build, test, and package

Requirements:

- .NET SDK 8;
- Bash or PowerShell for helper scripts;
- Docker only for the full Workbench + SQL Server smoke path.

```bash
dotnet restore FoundationKit.sln
dotnet build FoundationKit.sln --configuration Release --no-restore
dotnet test FoundationKit.sln --configuration Release --no-build
./scripts/pack.sh
```

Packages and symbol packages are written to `artifacts/packages`.

Consume from a local package source:

```bash
dotnet nuget add source ./artifacts/packages --name FoundationKitLocal
dotnet add package FoundationKit.Domain --version 0.1.0 --source FoundationKitLocal
```

Reference only the packages a project needs. A product Domain project must not reference Infrastructure, WebApi, or Blazor merely for convenience.

## One source of truth for capabilities

`catalog/foundationkit.catalog.json` is the canonical description of implemented packages, capabilities, project ideas, adoption steps, and contact metadata.

It drives:

- the local Workbench through `/api/catalog`;
- the GitHub Pages demo directly in the browser;
- the generated [capability reference](docs/FEATURES.md).

When an implemented feature changes:

```bash
# 1. update code and tests
# 2. update catalog/foundationkit.catalog.json
# 3. regenerate documentation
dotnet run --project tools/FoundationKit.CatalogGenerator
# 4. update CHANGELOG.md
```

CI runs the generator in `--check` mode and fails when the generated documentation is stale or an idea references an unknown capability. The catalog accepts only `implemented` capabilities; design intent and future recommendations must stay explicitly separate.

## Domain-event contract

The EF Core interceptor dispatches domain events in-process after a successful save. Events are cleared before handlers run so a handler failure does not cause accidental duplicate dispatch during a later save.

This is best-effort in-process delivery, not durable messaging. A product that requires retry, guaranteed delivery, cross-process delivery, or an audit trail must implement an outbox and delivery worker.

## Repository map

```text
src/        reusable FoundationKit packages
tests/      core and Workbench tests
samples/    real local Workbench consumer
catalog/    canonical capability information
site/       static GitHub Pages demo
tools/      catalog validation and documentation generation
deploy/     local Workbench + SQL Server Docker topology
scripts/    build, package, verification, and Workbench commands
docs/       architecture, packages, capabilities, and operations
```

More detail:

- [Architecture and boundaries](docs/ARCHITECTURE.md)
- [Implemented capabilities](docs/FEATURES.md)
- [Package contracts](docs/PACKAGES.md)
- [Workbench setup](docs/WORKBENCH.md)
- [Contributing](CONTRIBUTING.md)
- [Security](SECURITY.md)

## Contact

After preparing a public-safe project summary in the Workbench or Pages demo, use the generated **Contact ALHassan ALShami** action. It opens a prefilled public GitHub issue in this repository. Do not place confidential information in a public issue.

Current package version: `0.1.0`. The package API is pre-1.0 and may evolve; breaking changes must be documented in `CHANGELOG.md`.
