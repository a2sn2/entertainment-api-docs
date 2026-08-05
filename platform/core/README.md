# FoundationKit

FoundationKit is the reusable .NET engineering core used by products in this repository. It is organized as small packages so a future solution can reference only the layers it needs.

## Packages

| Package | Responsibility |
|---|---|
| `FoundationKit.Domain` | Entities, aggregate roots, value objects, and domain events |
| `FoundationKit.Application` | Results, errors, commands, queries, handlers, repositories, specifications, and pagination |
| `FoundationKit.Infrastructure` | Provider-agnostic EF Core repository and event adapters |
| `FoundationKit.WebApi` | RFC 7807 mapping, correlation IDs, and API security conventions |
| `FoundationKit.Blazor` | Typed HTTP execution, API errors, results, and async UI state |

`FoundationKit.Infrastructure` references base EF Core only. A product chooses its provider separately, such as SQL Server, PostgreSQL, SQLite, or an in-memory test provider.

## Pack locally

Windows:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\pack-foundation.ps1
```

Linux/macOS:

```bash
bash scripts/pack-foundation.sh
```

Packages are written to:

```text
artifacts/foundation/
```

Current pre-release foundation version:

```text
0.1.0
```

## Consumption rule

A product references inward only:

```text
Product.Domain          → FoundationKit.Domain
Product.Application     → FoundationKit.Application + Product.Domain
Product.Infrastructure  → FoundationKit.Infrastructure + Product.Application
Product.Api             → FoundationKit.WebApi + Product.Application/Infrastructure
Product.Blazor          → FoundationKit.Blazor + Product.Contracts
```

Business rules, routes, contracts, database-provider configuration, and product UI remain outside FoundationKit.
