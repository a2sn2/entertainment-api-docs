# FoundationKit for .NET

[![FoundationKit CI](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/ci.yml)
[![Blazor Pages Demo](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/pages.yml/badge.svg)](https://a2sn2.github.io/foundationkit-dotnet/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Target: .NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)

FoundationKit is the official reusable .NET core in this repository. Its reference application is deliberately split into two complete end-to-end sections:

1. **User Full Stack** — database, domain, application logic, request/response contracts, API, typed frontend client, Blazor UI, and user UX.
2. **Admin Full Stack** — database queries and review records, domain transition, application logic, admin contracts, API, typed frontend client, Blazor UI, and admin UX.

The two sections connect through one explicit request lifecycle:

```text
User creates request
        ↓
submitted
        ↓
Admin reviews request
        ↓
approved or rejected
        ↓
User reads the updated status
```

## Architecture at a glance

```text
                              REUSABLE CORE
        FoundationKit.Domain / Application / Infrastructure / WebApi / Blazor
                                     │
                  ┌──────────────────┴──────────────────┐
                  │                                     │
           USER FULL STACK                       ADMIN FULL STACK
                  │                                     │
          Pages/UserPortal.razor                Pages/AdminPortal.razor
                  │                                     │
           typed API client                      typed API client
                  │                                     │
       Contracts/User + User API            Contracts/Admin + Admin API
                  │                                     │
       CreateUserRequestUseCase             ReviewUserRequestUseCase
                  │                                     │
        BuildBrief aggregate            AdminReview + BuildBrief transition
                  │                                     │
          BuildBriefs table                  AdminReviews table
                  └──────────────────┬──────────────────┘
                                     │
                       SHARED WORKFLOW + UNIT OF WORK
```

Read the full architecture first:

- [Dual Full-Stack Architecture](docs/DUAL-FULL-STACK.md)
- [Technical Architecture](docs/ARCHITECTURE.md)
- [Workbench Operations](docs/WORKBENCH.md)
- [Implemented Core Capabilities](docs/FEATURES.md)

## What belongs to the reusable core

```text
src/FoundationKit.Domain
src/FoundationKit.Application
src/FoundationKit.Infrastructure
src/FoundationKit.WebApi
src/FoundationKit.Blazor
```

These packages remain product-independent. They do not own SQL Server, migrations, Workbench rules, admin behavior, user behavior, or UI design.

## What belongs to the reference application

```text
samples/FoundationKit.Workbench/                API host, domain, use cases, EF Core, SQL Server
samples/FoundationKit.Workbench.Contracts/      User, Admin, Workflow, and shared transport contracts
samples/FoundationKit.Workbench.Client/         Blazor WebAssembly + MudBlazor UI/UX
postman/                                         executable API walkthrough
deploy/                                          local Docker topology
```

## User Full Stack

```text
SQL Server → EF Core → BuildBrief → CreateUserRequestUseCase
→ CreateUserRequest → POST /api/user/requests
→ WorkbenchApiClient → /user Blazor UI
```

User endpoints:

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/user/requests` | Create a request in `submitted` state |
| `GET` | `/api/user/requests/{id}` | Read the latest status |

## Admin Full Stack

```text
SQL Server → EfAdminQueueReader / AdminReview
→ ReviewUserRequestUseCase → AdminReviewRequest
→ /api/admin/requests → WorkbenchApiClient → /admin Blazor UI
```

Admin endpoints:

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/admin/requests?status=submitted` | Read the admin queue |
| `POST` | `/api/admin/requests/{id}/review` | Approve or reject a user request |

The review operation inserts `AdminReviews` and changes `BuildBriefs.Status` through the same unit of work.

## Shared platform endpoints

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/runtime` | Local or static-demo runtime |
| `GET` | `/api/health` | API and SQL Server health |
| `GET` | `/api/catalog` | Canonical FoundationKit capability catalog |

Swagger documents all three groups: Shared Platform, User Full Stack, and Admin Full Stack.

## Run in Visual Studio 2026

Requirements:

- Visual Studio 2026 with ASP.NET and web development;
- .NET 8 SDK;
- SQL Server;
- SSMS for database inspection when needed.

Open:

```text
FoundationKit.sln
```

Set as startup project:

```text
FoundationKit.Workbench.Api
```

Set API project user secrets:

```json
{
  "ConnectionStrings": {
    "Workbench": "Server=.;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

Press `F5`.

Local URLs:

- Architecture landing page: <http://localhost:5057/>
- User portal: <http://localhost:5057/user>
- Admin portal: <http://localhost:5057/admin>
- Swagger: <http://localhost:5057/swagger>
- Health: <http://localhost:5057/api/health>

The host applies Workbench EF Core migrations automatically for this local reference application.

## Run with PowerShell or Docker

Existing SQL Server:

```powershell
$env:ConnectionStrings__Workbench="Server=.;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
dotnet run --project .\samples\FoundationKit.Workbench\FoundationKit.Workbench.Api.csproj
```

Complete Docker topology:

```powershell
.\scripts\run-workbench.ps1
```

Docker URL:

```text
http://localhost:8080/
```

Stop:

```powershell
.\scripts\stop-workbench.ps1
```

## Postman walkthrough

Import:

```text
postman/FoundationKit.Workbench.postman_collection.json
```

Run in this order:

1. `User Full Stack / Create User Request`.
2. `Admin Full Stack / Get Submitted Queue`.
3. `Admin Full Stack / Approve User Request`.
4. `User Full Stack / Get User Request Status`.
5. `Admin Full Stack / Get Approved Queue`.

The collection stores the created identifier in `userRequestId`.

## Database view

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

## Repository reading order

```text
1. README.md
2. docs/DUAL-FULL-STACK.md
3. src/FoundationKit.*
4. Contracts/User and Contracts/Admin
5. Application/User and Application/Admin
6. Endpoints/UserPortalEndpoints.cs and AdminPortalEndpoints.cs
7. Client/Pages/UserPortal.razor and AdminPortal.razor
8. Infrastructure/Migrations
9. Postman collection
```

## Build and verify

```bash
dotnet restore FoundationKit.sln
dotnet build FoundationKit.sln --configuration Release --no-restore
dotnet test FoundationKit.sln --configuration Release --no-build
bash scripts/verify-repository.sh
bash scripts/smoke-workbench.sh
```

CI verifies the hosted Blazor application, Swagger groups, migrations, SQL Server persistence, user creation, admin approval, and the status returned to the user.

## GitHub Pages

<https://a2sn2.github.io/foundationkit-dotnet/>

Pages publishes the same Blazor UI. It shows the architecture and both portal experiences, but disables real database writes and admin decisions because GitHub Pages cannot host ASP.NET Core or SQL Server.

## Production boundary

This is a reference architecture, not a complete production security model. Identity, authorization, per-user ownership, admin roles, private data handling, rate limiting, outbox delivery, production migrations, observability backends, and deployment topology remain consuming-product decisions.

Current reusable package version: `0.1.0`.
