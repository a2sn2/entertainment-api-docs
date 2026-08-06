# FoundationKit for .NET

[![FoundationKit CI](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Target: .NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)

FoundationKit is a focused set of reusable .NET building blocks for applications that use Clean Architecture and domain-driven design principles. This repository contains the reusable core only: no product domain, hosted application, database provider, migration, frontend product, or deployment topology is selected here.

## Packages

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

`FoundationKit.Infrastructure` references EF Core abstractions but intentionally does not select SQL Server, PostgreSQL, SQLite, or another provider. Database providers and migrations belong to consuming applications.

## Build and test

Requirements:

- .NET SDK 8
- Bash or PowerShell only when using the packaging scripts

```bash
dotnet restore FoundationKit.sln
dotnet build FoundationKit.sln --configuration Release --no-restore
dotnet test FoundationKit.sln --configuration Release --no-build
```

Create all five NuGet packages and symbol packages:

```bash
./scripts/pack.sh
```

Windows PowerShell:

```powershell
.\scripts\pack.ps1
```

Artifacts are written to `artifacts/packages`.

## Consume from a local package source

```bash
dotnet nuget add source ./artifacts/packages --name FoundationKitLocal
dotnet add package FoundationKit.Domain --version 0.1.0 --source FoundationKitLocal
```

Reference only the packages a project needs. A Domain project should not reference Infrastructure, WebApi, or Blazor merely for convenience.

## Domain-event delivery contract

The EF Core interceptor dispatches domain events in-process after a successful save. Events are cleared before handlers run so a handler failure does not cause an accidental duplicate dispatch during a later save.

This is a best-effort in-process mechanism, not durable messaging. A consuming product that requires retry, guaranteed delivery, cross-process delivery, or an audit trail must implement an outbox and its own delivery worker.

## Repository rules

- Keep product-specific rules, contracts, migrations, UI, and hosting outside this repository.
- Preserve inward dependency direction.
- Do not add a database provider to the reusable Infrastructure package.
- Add tests and documentation with every public behavior change.
- Keep all five packages buildable and packable independently.
- Treat the `main` branch as releasable.

More detail:

- [Architecture](docs/ARCHITECTURE.md)
- [Package contracts](docs/PACKAGES.md)
- [Contributing](CONTRIBUTING.md)
- [Security policy](SECURITY.md)

## Status

Current package version: `0.1.0`

The API surface is still pre-1.0 and may evolve. Breaking changes must be documented in `CHANGELOG.md`.
