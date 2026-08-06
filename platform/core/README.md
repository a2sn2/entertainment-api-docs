# FoundationKit Core Packages

FoundationKit is the reusable .NET engineering core of this repository. The root [`FoundationKit.sln`](../../FoundationKit.sln) loads only the five package projects; the complete EntertainmentDocs reference consumer remains in [`platform/EntertainmentDocs.sln`](../EntertainmentDocs.sln).

The existing `FoundationKit.Tests` project also checks consumer dependency boundaries, so it intentionally remains outside the package-only solution and is executed separately by CI.

## Packages

| Package | Responsibility |
|---|---|
| `FoundationKit.Domain` | Entities, aggregate roots, value objects, and domain events |
| `FoundationKit.Application` | Results, errors, commands, queries, handlers, repositories, specifications, and pagination |
| `FoundationKit.Infrastructure` | Provider-neutral EF Core repository and event adapters |
| `FoundationKit.WebApi` | RFC 7807 mapping, correlation IDs, and API security conventions |
| `FoundationKit.Blazor` | Typed HTTP execution, API errors, results, and async UI state |

`FoundationKit.Infrastructure` references base EF Core only. A consuming product chooses SQL Server, PostgreSQL, SQLite, an in-memory provider, or another compatible provider.

## Pack locally

Windows:

```powershell
powershell -ExecutionPolicy Bypass -File .\platform\scripts\pack-foundation.ps1
```

Linux/macOS:

```bash
bash platform/scripts/pack-foundation.sh
```

Packages are written to `platform/artifacts/foundation/`.

Current pre-release version: `0.1.0`.

## Consumption rule

```text
Product.Domain          → FoundationKit.Domain
Product.Application     → FoundationKit.Application + Product.Domain
Product.Infrastructure  → FoundationKit.Infrastructure + Product.Application
Product.Api             → FoundationKit.WebApi + Product.Application/Infrastructure
Product.Blazor          → FoundationKit.Blazor + Product.Contracts
```

Business rules, routes, contracts, database-provider configuration, migrations, authorization policies, and product UI remain outside FoundationKit.
