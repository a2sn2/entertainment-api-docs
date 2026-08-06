# FoundationKit Workbench

## Purpose

The Workbench is the official consumer and demonstration application for the FoundationKit core.

It is split into three projects:

```text
FoundationKit.Workbench.Api        ASP.NET Core API, application logic, EF Core, SQL Server, migrations
FoundationKit.Workbench.Client     Blazor WebAssembly, Razor Components, MudBlazor, typed HTTP client
FoundationKit.Workbench.Contracts  request, response, runtime, health, catalog, and route contracts
```

The reusable packages remain under `src/`. Workbench product logic, database provider, migrations, DTOs, UI, and deployment configuration must not move into the core.

## Visual Studio 2026

Open:

```text
FoundationKit.sln
```

Set the startup project to:

```text
FoundationKit.Workbench.Api
```

The API project references the Blazor client as a hosted WebAssembly application, so one startup project is sufficient. Pressing `F5` starts the API and serves the compiled Blazor client from the same origin.

The launch profile opens:

```text
http://localhost:5057
```

Swagger is available at:

```text
http://localhost:5057/swagger
```

## Configure your SQL Server

Use Visual Studio **Manage User Secrets** on `FoundationKit.Workbench.Api`.

Default SQL Server instance:

```json
{
  "ConnectionStrings": {
    "Workbench": "Server=.;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

SQL Express:

```json
{
  "ConnectionStrings": {
    "Workbench": "Server=.\\SQLEXPRESS;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

SQL authentication:

```json
{
  "ConnectionStrings": {
    "Workbench": "Server=localhost,1433;Database=FoundationKitWorkbench;User Id=foundationkit;Password=<local-password>;TrustServerCertificate=True;Encrypt=False"
  }
}
```

Do not commit real credentials.

## Local command line

```powershell
$env:ConnectionStrings__Workbench="Server=.;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
dotnet run --project .\samples\FoundationKit.Workbench\FoundationKit.Workbench.Api.csproj
```

The API host serves:

- the Blazor WebAssembly application;
- MudBlazor static assets;
- Swagger/OpenAPI;
- the Workbench API;
- the canonical catalog.

## Docker start

PowerShell:

```powershell
.\scripts\run-workbench.ps1
```

Bash:

```bash
./scripts/run-workbench.sh
```

Open:

```text
http://localhost:8080
```

Stop:

```powershell
.\scripts\stop-workbench.ps1
```

Delete containers and local Docker database data intentionally:

```bash
docker compose -f deploy/docker-compose.yml down --volumes
```

## Shared contracts

The canonical create contract is:

```text
samples/FoundationKit.Workbench.Contracts/BuildBriefContracts.cs
```

`BuildBriefRequest` is used by:

1. the Blazor form;
2. the typed `WorkbenchApiClient`;
3. the ASP.NET Core endpoint;
4. Swagger/OpenAPI;
5. the Postman collection.

This prevents the frontend, API documentation, and manual API testing from drifting into different payload shapes.

## API routes

| Route | Contract | Behavior |
|---|---|---|
| `GET /api/runtime` | `RuntimeResponse` | reports local runtime and persistence mode |
| `GET /api/catalog` | `CatalogResponse` | returns the canonical implemented capability catalog |
| `GET /api/health` | `HealthResponse` | verifies SQL Server connectivity |
| `POST /api/build-briefs` | `BuildBriefRequest` → `BuildBriefResponse` | validates and saves a BuildBrief aggregate |
| `GET /api/build-briefs/{id}` | `BuildBriefResponse` | reads a saved brief |

## Postman

Import:

```text
postman/FoundationKit.Workbench.postman_collection.json
```

The collection variable `baseUrl` defaults to:

```text
http://localhost:5057
```

The create request stores its returned identifier in `buildBriefId`. The next GET request uses that identifier automatically.

Swagger and Postman are API consumers. Neither bypasses application logic, domain validation, repositories, unit of work, or SQL persistence.

## Request flow

```text
MudBlazor form
      ↓
BuildBriefRequest from Contracts
      ↓
WorkbenchApiClient : FoundationKit.Blazor.ApiClientBase
      ↓
POST /api/build-briefs
      ↓
unknown capability validation
      ↓
BuildBrief.Create → Result<BuildBrief>
      ↓
IRepository + IUnitOfWork
      ↓
EF Core + SQL Server
      ↓
BuildBriefCreated domain event after successful save
      ↓
BuildBriefResponse
```

## Migrations

Migrations remain under:

```text
samples/FoundationKit.Workbench/Infrastructure/Migrations/
```

The API applies migrations at startup with bounded retries.

Create a migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project samples/FoundationKit.Workbench/FoundationKit.Workbench.Api.csproj \
  --startup-project samples/FoundationKit.Workbench/FoundationKit.Workbench.Api.csproj \
  --output-dir Infrastructure/Migrations
```

Review migration code and generated SQL before committing.

## GitHub Pages

GitHub Pages publishes the Blazor WebAssembly client itself. It does not host ASP.NET Core or SQL Server.

When `/api/runtime` is unavailable, the client switches to demo mode, reads the static catalog, and disables database submission. The local API path remains the authoritative implementation.

## Troubleshooting

Health:

```powershell
Invoke-RestMethod http://localhost:5057/api/health
```

Swagger JSON:

```powershell
Invoke-RestMethod http://localhost:5057/swagger/v1/swagger.json
```

Docker status:

```bash
docker compose -f deploy/docker-compose.yml ps
```

Docker logs:

```bash
docker compose -f deploy/docker-compose.yml logs --tail=300
```

SQL verification:

```sql
USE FoundationKitWorkbench;
GO

SELECT *
FROM dbo.BuildBriefs
ORDER BY CreatedUtc DESC;
```

## Production warning

The Workbench demonstrates architecture and integration. It does not yet include identity, authorization, rate limiting, production secret management, telemetry export, backups, high availability, ingress hardening, or a controlled production migration strategy.
