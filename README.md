# FoundationKit for .NET

[![FoundationKit CI](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/ci.yml)
[![Blazor Pages Demo](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/pages.yml/badge.svg)](https://a2sn2.github.io/foundationkit-dotnet/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Target: .NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)

FoundationKit is the official reusable core for Clean Architecture and domain-driven .NET applications in this repository.

The repository separates four responsibilities:

1. **Reusable core packages** under `src/`.
2. **ASP.NET Core API** under `samples/FoundationKit.Workbench/`.
3. **Blazor WebAssembly + MudBlazor client** under `samples/FoundationKit.Workbench.Client/`.
4. **Shared request/response contracts** under `samples/FoundationKit.Workbench.Contracts/`.

The API, Blazor client, Swagger, Postman collection, SQL Server provider, EF Core migrations, and product-specific Workbench logic are consumers of the core. They do not leak into the reusable packages.

## Official Workbench flow

```text
Blazor WebAssembly + MudBlazor
              │
              │ typed HTTP using FoundationKit.Blazor
              ▼
FoundationKit.Workbench.Contracts
              │
              ▼
ASP.NET Core API + Swagger
              │
              ▼
Domain / Application / Infrastructure / WebApi core
              │
              ▼
EF Core + SQL Server + Workbench-owned migrations
```

`BuildBriefRequest` is the canonical transport contract. The Blazor client serializes it, Swagger documents it, and the Postman collection sends the same JSON shape.

## Run in Visual Studio 2026

Requirements:

- Visual Studio 2026 with the ASP.NET and web development workload;
- .NET 8 SDK;
- SQL Server running locally;
- SQL Server Management Studio when database inspection is needed.

Open:

```text
FoundationKit.sln
```

Set this project as the startup project:

```text
FoundationKit.Workbench.Api
```

Configure the API project user secrets:

```json
{
  "ConnectionStrings": {
    "Workbench": "Server=.;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

For SQL Express use:

```json
{
  "ConnectionStrings": {
    "Workbench": "Server=.\\SQLEXPRESS;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

Press `F5`. The API host serves the Blazor WebAssembly application and applies the Workbench EF Core migrations automatically.

Local URLs:

- UI: <http://localhost:5057/>
- Swagger: <http://localhost:5057/swagger>
- Health: <http://localhost:5057/api/health>
- Catalog: <http://localhost:5057/api/catalog>

## Run from PowerShell

```powershell
$env:ConnectionStrings__Workbench="Server=.;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
dotnet run --project .\samples\FoundationKit.Workbench\FoundationKit.Workbench.Api.csproj
```

Or start the complete Docker topology:

```powershell
.\scripts\run-workbench.ps1
```

Stop it with:

```powershell
.\scripts\stop-workbench.ps1
```

Docker serves the Workbench at <http://localhost:8080/>.

## Use the API from Postman

Import:

```text
postman/FoundationKit.Workbench.postman_collection.json
```

The collection contains:

- runtime;
- health;
- canonical capability catalog;
- create build brief;
- get build brief by identifier.

The create request stores the returned identifier in the `buildBriefId` collection variable so the following GET request can read the same SQL Server record.

## Shared API contract

```json
{
  "projectName": "نظام إدارة مراسلات",
  "projectType": "نظام خدمة عملاء",
  "audience": "موظفو خدمة العملاء والمشرفون",
  "goal": "إدارة المحادثات والمهام والتصعيد من واجهة داخلية موحدة",
  "selectedCapabilityIds": [
    "aggregate-domain-events",
    "commands-queries",
    "repository-ports",
    "ef-repository",
    "result-http-mapping",
    "typed-api-results"
  ],
  "priorities": "الصلاحيات، سجل التدقيق، سرعة الاستجابة",
  "notes": "مثال عام غير سري"
}
```

Endpoint:

```http
POST /api/build-briefs
Content-Type: application/json
```

## Reusable packages

| Package | Responsibility |
|---|---|
| `FoundationKit.Domain` | Entities, aggregate roots, value objects, exceptions, and domain events |
| `FoundationKit.Application` | Commands, queries, results, validation, pagination, repository ports, and use-case abstractions |
| `FoundationKit.Infrastructure` | Provider-neutral EF Core repositories, unit of work, specifications, and in-process domain-event dispatch |
| `FoundationKit.WebApi` | Result mapping, Problem Details, correlation IDs, and baseline response headers |
| `FoundationKit.Blazor` | Typed API results, resilient response parsing, and asynchronous UI state |

SQL Server remains outside `src/`. The Workbench API explicitly owns the provider and migrations.

## Build and verify

```bash
dotnet restore FoundationKit.sln
dotnet build FoundationKit.sln --configuration Release --no-restore
dotnet test FoundationKit.sln --configuration Release --no-build
bash scripts/verify-repository.sh
bash scripts/pack.sh
```

CI also publishes the Blazor-hosted API and runs a real SQL Server persistence smoke test.

## GitHub Pages

<https://a2sn2.github.io/foundationkit-dotnet/>

Pages publishes the same Blazor WebAssembly client. Because GitHub Pages cannot run ASP.NET Core or SQL Server, the deployed client automatically operates in catalog demo mode and disables database submission. The real persistence path is local through the API host.

## Repository map

```text
src/                                      reusable FoundationKit core
samples/FoundationKit.Workbench/          ASP.NET Core API, domain logic, SQL Server, migrations
samples/FoundationKit.Workbench.Client/   Blazor WebAssembly and MudBlazor UI
samples/FoundationKit.Workbench.Contracts shared API request and response contracts
tests/                                    core and Workbench tests
postman/                                  reusable API collection
catalog/                                  canonical capability data
deploy/                                   Docker Compose topology
scripts/                                  verification and local launch commands
docs/                                     architecture and operations documentation
```

More detail:

- [Architecture](docs/ARCHITECTURE.md)
- [Workbench operations](docs/WORKBENCH.md)
- [Implemented capabilities](docs/FEATURES.md)
- [Package contracts](docs/PACKAGES.md)

Current package version: `0.1.0`.
