# Dual Full-Stack Reference Architecture

## The repository in one sentence

FoundationKit is a reusable .NET core demonstrated through two complete vertical slices: a **User Full Stack** and an **Admin Full Stack**, connected by one explicit workflow boundary.

## First-minute mental model

```text
                         FOUNDATIONKIT REUSABLE CORE
          Domain | Application | Infrastructure | WebApi | Blazor
                                      │
                    ┌─────────────────┴─────────────────┐
                    │                                   │
              USER FULL STACK                     ADMIN FULL STACK
                    │                                   │
        Blazor user UI and UX              Blazor admin UI and UX
                    │                                   │
          WorkbenchApiClient                  WorkbenchApiClient
                    │                                   │
        CreateUserRequest contract            AdminReviewRequest contract
                    │                                   │
        POST /api/user/requests       GET /api/admin/requests
                    │                 POST /api/admin/requests/{id}/review
                    │                                   │
       CreateUserRequestUseCase          ReviewUserRequestUseCase
                    │                                   │
            BuildBrief domain             AdminReview + state transition
                    │                                   │
            BuildBriefs table                   AdminReviews table
                    └─────────────────┬─────────────────┘
                                      │
                          SHARED WORKFLOW BOUNDARY
                   submitted → approved or rejected
```

## Section 1 — User Full Stack

The user side owns the experience of creating a request and reading its current status.

```text
SQL Server
  ↓
WorkbenchDbContext.BuildBriefs
  ↓
BuildBrief aggregate
  ↓
CreateUserRequestUseCase
  ↓
CreateUserRequest / UserRequestResponse
  ↓
/api/user/requests
  ↓
WorkbenchApiClient
  ↓
Pages/UserPortal.razor
```

### User routes

| Method | Route | Responsibility |
|---|---|---|
| `POST` | `/api/user/requests` | Validate and create a request in `submitted` state |
| `GET` | `/api/user/requests/{id}` | Read the latest status visible to the user |

### User code map

```text
samples/FoundationKit.Workbench.Contracts/User/
samples/FoundationKit.Workbench/Application/User/
samples/FoundationKit.Workbench/Endpoints/UserPortalEndpoints.cs
samples/FoundationKit.Workbench.Client/Pages/UserPortal.razor
```

## Section 2 — Admin Full Stack

The admin side owns the work queue, request review, decision, and review audit record.

```text
SQL Server
  ↓
BuildBriefs + AdminReviews
  ↓
EfAdminQueueReader / AdminReview aggregate
  ↓
ReviewUserRequestUseCase
  ↓
AdminQueueItemResponse / AdminReviewRequest / AdminReviewResponse
  ↓
/api/admin/requests
  ↓
WorkbenchApiClient
  ↓
Pages/AdminPortal.razor
```

### Admin routes

| Method | Route | Responsibility |
|---|---|---|
| `GET` | `/api/admin/requests?status=submitted` | Read the SQL-backed admin queue |
| `POST` | `/api/admin/requests/{id}/review` | Approve or reject a user request |

### Admin code map

```text
samples/FoundationKit.Workbench.Contracts/Admin/
samples/FoundationKit.Workbench/Application/Admin/
samples/FoundationKit.Workbench/Endpoints/AdminPortalEndpoints.cs
samples/FoundationKit.Workbench.Client/Pages/AdminPortal.razor
```

## Where the two sections connect

The sections connect at the request lifecycle, not by directly calling each other's UI code.

```text
User creates request
        ↓
BuildBrief.Status = Submitted
        ↓
Admin queue reads Submitted requests
        ↓
Admin submits approve or reject decision
        ↓
AdminReview is inserted
        +
BuildBrief.Status changes to Approved or Rejected
        ↓
User GET returns the new status
```

`ReviewUserRequestUseCase` performs the review insert and the user-request state transition through the same `IUnitOfWork`. The database migration adds:

- `BuildBriefs.Status`;
- `BuildBriefs.UpdatedUtc`;
- `AdminReviews` with a foreign key to `BuildBriefs`.

This is the current integration boundary. Future products may replace the in-process transition with an outbox and asynchronous integration events without changing the portal contracts.

## What is shared

The following are shared intentionally:

- FoundationKit packages under `src/`;
- SQL Server connection and EF Core unit of work in the reference application;
- runtime, health, and capability-catalog endpoints;
- workflow status vocabulary;
- typed HTTP transport infrastructure;
- one ASP.NET Core host for local simplicity.

## What remains separate

The following must remain separated by portal:

- request DTOs;
- application use cases;
- API route groups;
- UI pages and UX decisions;
- admin queue and review behavior;
- user create and status behavior.

A UI component must never become the integration boundary. The connection belongs in contracts, use cases, domain state, and persistence.

## Repository reading order

A new developer should read in this order:

1. `README.md` — repository purpose and launch instructions.
2. `docs/DUAL-FULL-STACK.md` — the two complete stacks and their connection.
3. `src/FoundationKit.*` — reusable technical core.
4. `samples/FoundationKit.Workbench.Contracts/User` and `Admin` — transport boundaries.
5. `samples/FoundationKit.Workbench/Application/User` and `Admin` — business use cases.
6. `samples/FoundationKit.Workbench/Endpoints` — HTTP composition.
7. `samples/FoundationKit.Workbench.Client/Pages/UserPortal.razor` and `AdminPortal.razor` — UI/UX endpoints.
8. `Infrastructure/Migrations` — persisted workflow shape.
9. `postman/FoundationKit.Workbench.postman_collection.json` — executable API tour.

## Definition of a complete future vertical slice

A feature is not considered complete until its path is visible end to end:

```text
Database or external source
        ↓
Infrastructure adapter
        ↓
Domain and application use case
        ↓
Request and response contracts
        ↓
Documented API endpoint
        ↓
Typed frontend client
        ↓
Blazor page or component
        ↓
UI states: loading, empty, success, validation, and failure
        ↓
Automated test or smoke verification
```

Use this definition for both admin-facing and user-facing features.

## Local demonstration

1. Open `/user` and create a request.
2. Copy or keep the generated identifier.
3. Open `/admin`, select the submitted request, and approve or reject it.
4. Return to `/user` and refresh the request status.
5. Inspect `BuildBriefs` and `AdminReviews` in SQL Server.

The Postman collection executes the same sequence without the UI.

## Boundaries not implemented yet

The reference intentionally does not claim production completeness. Authentication, authorization, per-user ownership, admin roles, audit identity, rate limiting, outbox delivery, secrets management, and production deployment policy remain product decisions.
