# FoundationKit Workbench

## Purpose

The Workbench is the official consumer and executable reference for FoundationKit. It demonstrates two complete product-facing vertical slices:

```text
User Full Stack   database → domain → use case → contracts → API → Blazor UI/UX
Admin Full Stack  database → domain → use case → contracts → API → Blazor UI/UX
```

They connect through a shared request workflow:

```text
submitted → approved | rejected
```

For the architecture and code map, read [DUAL-FULL-STACK.md](DUAL-FULL-STACK.md).

## Projects

```text
FoundationKit.Workbench.Api        ASP.NET Core host, product domain, use cases, EF Core, SQL Server, migrations
FoundationKit.Workbench.Client     Blazor WebAssembly, Razor Components, MudBlazor, user and admin UI/UX
FoundationKit.Workbench.Contracts  shared, user, admin, workflow, runtime, health, catalog, and route contracts
```

Reusable packages remain under `src/`. Workbench product rules, SQL Server selection, migrations, transport DTOs, portal behavior, and deployment configuration must not move into those reusable packages.

## Visual Studio 2026

Open:

```text
FoundationKit.sln
```

Set the startup project:

```text
FoundationKit.Workbench.Api
```

One startup project is sufficient because the API hosts the compiled Blazor WebAssembly application.

Use **Manage User Secrets** on the API project.

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

Press `F5`.

## Local URLs

| Surface | URL |
|---|---|
| Architecture map | `http://localhost:5057/` |
| User portal | `http://localhost:5057/user` |
| Admin portal | `http://localhost:5057/admin` |
| Swagger UI | `http://localhost:5057/swagger` |
| Health | `http://localhost:5057/api/health` |
| Capability catalog | `http://localhost:5057/api/catalog` |

## Command line

```powershell
$env:ConnectionStrings__Workbench="Server=.;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
dotnet run --project .\samples\FoundationKit.Workbench\FoundationKit.Workbench.Api.csproj
```

## Docker

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

Stop without deleting the database volume:

```powershell
.\scripts\stop-workbench.ps1
```

Delete containers and data intentionally:

```bash
docker compose -f deploy/docker-compose.yml down --volumes
```

## User API

Contracts:

```text
samples/FoundationKit.Workbench.Contracts/User/UserContracts.cs
```

Routes:

| Method | Route | Contract | Behavior |
|---|---|---|---|
| `POST` | `/api/user/requests` | `CreateUserRequest` → `UserRequestResponse` | creates a request in `submitted` state |
| `GET` | `/api/user/requests/{id}` | `UserRequestResponse` | returns the latest state visible to the user |

Execution path:

```text
UserPortal.razor
      ↓
WorkbenchApiClient.CreateUserRequestAsync
      ↓
CreateUserRequest
      ↓
UserPortalEndpoints
      ↓
CreateUserRequestUseCase
      ↓
BuildBrief.Create
      ↓
BuildBriefs
```

## Admin API

Contracts:

```text
samples/FoundationKit.Workbench.Contracts/Admin/AdminContracts.cs
```

Routes:

| Method | Route | Contract | Behavior |
|---|---|---|---|
| `GET` | `/api/admin/requests?status=submitted` | `AdminQueueItemResponse[]` | returns the SQL-backed admin queue |
| `POST` | `/api/admin/requests/{id}/review` | `AdminReviewRequest` → `AdminReviewResponse` | approves or rejects the linked user request |

Execution path:

```text
AdminPortal.razor
      ↓
WorkbenchApiClient.ReviewUserRequestAsync
      ↓
AdminReviewRequest
      ↓
AdminPortalEndpoints
      ↓
ReviewUserRequestUseCase
      ↓
AdminReview.Create + BuildBrief.ApplyReview
      ↓
AdminReviews insert + BuildBriefs status update
```

The review write and request status update are committed through the same `IUnitOfWork`.

## Shared endpoints

| Method | Route | Contract | Behavior |
|---|---|---|---|
| `GET` | `/api/runtime` | `RuntimeResponse` | reports local or static-demo mode |
| `GET` | `/api/catalog` | `CatalogResponse` | returns implemented FoundationKit capabilities |
| `GET` | `/api/health` | `HealthResponse` | verifies API and SQL Server connectivity |

## Postman

Import:

```text
postman/FoundationKit.Workbench.postman_collection.json
```

The collection is organized into:

```text
Shared Platform
User Full Stack
Admin Full Stack
```

Recommended sequence:

1. Create User Request.
2. Get Submitted Queue.
3. Approve User Request.
4. Get User Request Status.
5. Get Approved Queue.

The collection stores the created identifier in `userRequestId`.

Swagger and Postman use the same contracts as Blazor and do not bypass use cases, domain rules, repositories, or SQL Server.

## Database schema

Migrations:

```text
samples/FoundationKit.Workbench/Infrastructure/Migrations/
```

Current workflow tables:

| Table | Responsibility |
|---|---|
| `BuildBriefs` | user request data, status, created timestamp, updated timestamp |
| `AdminReviews` | admin decision, reviewer, notes, reviewed timestamp, request foreign key |

Inspect with SSMS:

```sql
USE FoundationKitWorkbench;
GO

SELECT *
FROM dbo.BuildBriefs
ORDER BY CreatedUtc DESC;

SELECT *
FROM dbo.AdminReviews
ORDER BY ReviewedUtc DESC;
```

Create a future migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project samples/FoundationKit.Workbench/FoundationKit.Workbench.Api.csproj \
  --startup-project samples/FoundationKit.Workbench/FoundationKit.Workbench.Api.csproj \
  --output-dir Infrastructure/Migrations
```

Review generated migration code and SQL before committing.

## GitHub Pages

GitHub Pages publishes the same Blazor client but cannot run ASP.NET Core or SQL Server.

Demo behavior:

- the architecture map is available;
- the user portal and JSON preview are visible;
- real user submission is disabled;
- the admin portal shows clearly labeled demo queue data;
- approve and reject actions are disabled;
- no database persistence is claimed.

The local API-hosted path is authoritative.

## CI verification

The Docker smoke test executes the real integration sequence:

```text
POST user request
        ↓
GET submitted admin queue
        ↓
POST admin approval
        ↓
GET user request = approved
        ↓
GET approved admin queue
```

This verifies both full stacks and their connection against a real SQL Server container.

## Troubleshooting

Health:

```powershell
Invoke-RestMethod http://localhost:5057/api/health
```

Submitted admin queue:

```powershell
Invoke-RestMethod 'http://localhost:5057/api/admin/requests?status=submitted'
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

## Production warning

The Workbench demonstrates architecture and integration. It does not implement production identity, authorization, per-user ownership, admin roles, rate limiting, durable integration events, production secret management, telemetry export, backups, high availability, hardened ingress, or controlled deployment migrations.
