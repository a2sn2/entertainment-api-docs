# Madar

> Status: **v0.1 first vertical slice implemented and repository-verified**. Madar now has a working authentication → case lifecycle → SQL Server → audit timeline path, but this is not a claim of production approval or completion of the broader product roadmap.

Madar is an operational case-management and orchestration product built on FoundationKit. It is intentionally separate from the reusable FoundationKit packages, the Workbench architecture sample, and the Athar reference product.

## Product purpose

Madar turns operational work into traceable cases that can be created, assigned, progressed through controlled states, audited, and later governed by SLA and escalation policies.

Representative case types include:

- customer complaints;
- transaction or operational incidents;
- internal service requests;
- access requests;
- compliance cases;
- technical escalations;
- operational exceptions.

## Product boundary

Madar owns its business model, database schema, Identity configuration, permissions, Arabic UI copy, runtime composition, and deployment topology. Reusable capabilities are consumed from FoundationKit only where their contracts fit the product.

Runtime projects follow the repository's canonical product structure from `docs/ADDING-A-PROJECT-AR.md`:

```text
apps/Madar/
├── Madar.Domain
├── Madar.Application
├── Madar.Infrastructure
├── Madar.Contracts
├── Madar.Api
└── Madar.Client

tests/
└── Madar.Tests
```

The dependency direction remains:

```text
Madar.Domain
    ↑
Madar.Application ← Madar.Contracts
    ↑
Madar.Infrastructure
    ↑
Madar.Api ← Madar.Client hosting

Madar.Client → Madar.Contracts + FoundationKit.Blazor
```

Infrastructure dependencies do not enter Domain. The Blazor client does not reference Infrastructure or `MadarDbContext`. EF Core migrations under `Madar.Infrastructure/Migrations` are the schema source of truth.

## Implemented v0.1 vertical slice

The first end-to-end product path is now implemented:

```text
Authenticate
    ↓
Create Case
    ↓
Persist in SQL Server
    ↓
List / View according to access scope
    ↓
Assign to an Operator
    ↓
new → assigned → in-progress → resolved → closed
    ↓
Persist audit events
    ↓
Read audit timeline
    ↓
Blazor UI + API response
```

The `Case` aggregate currently owns:

- identifier;
- title and description;
- bounded case type;
- priority;
- lifecycle status;
- creator;
- current assignee;
- created/updated timestamps;
- resolved/closed timestamps;
- SQL Server `rowversion` concurrency metadata;
- creation, assignment, and status-change domain events.

The deterministic lifecycle uses `FoundationKit.Workflow`; Madar retains its product-specific states, triggers, authorization rules, and UI behavior.

## FoundationKit reuse proven by Madar

The current slice reuses existing FoundationKit capabilities rather than creating new reusable packages for product-specific needs:

- `FoundationKit.Domain` for aggregate/domain primitives;
- `FoundationKit.Application` for results, current-user, persistence, clock, and unit-of-work contracts;
- `FoundationKit.Infrastructure` for `EfRepository`, `EfUnitOfWork`, and domain-event dispatch after successful EF Core saves;
- `FoundationKit.WebApi` for the request pipeline and HTTP result mapping;
- `FoundationKit.Blazor` for typed resilient API-result handling;
- `FoundationKit.Security` for rate-limit partition conventions;
- `FoundationKit.Authorization` for role/permission evaluation;
- `FoundationKit.Auditing` for bounded audit events, context, recorder, and sink contracts;
- `FoundationKit.Workflow` for lifecycle transition resolution.

ASP.NET Core Identity, SQL Server schema, audit persistence, case query rules, API endpoints, and the Blazor product experience remain Madar-owned adapters and behavior.

## Authentication and authorization

Madar currently uses ASP.NET Core Identity with secure cookie authentication, anti-CSRF validation for write operations, password policy, login lockout, and authentication/write rate limits.

Product roles are:

| Role | v0.1 responsibility |
|---|---|
| `Requester` | create cases and see cases they created |
| `Operator` | see assigned cases and progress their own assignment |
| `Supervisor` | read all cases, assign cases, progress any case, and close resolved cases |
| `Administrator` | receives all currently defined Madar case permissions |

The Application layer makes the authorization decision before selecting the query path. Infrastructure does not infer permissions from a user ID. Unauthorized access to a particular case is intentionally exposed as not-found rather than revealing that the case exists.

Assignment also validates that the selected assignee is an Identity user holding the `Operator` role.

## SQL Server and audit persistence

`MadarDbContext` is an `IdentityDbContext<MadarUser, IdentityRole<Guid>, Guid>` and owns three schemas:

```text
identity/*  ASP.NET Core Identity tables
madar/Cases
 audit/AuditEvents
```

The first migration is:

```text
apps/Madar/Madar.Infrastructure/Migrations/20260808093000_InitialMadar.cs
```

Case writes and FoundationKit audit records share the same Madar DbContext/unit-of-work boundary. The SQL audit sink stores action, subject, actor, correlation identifier, outcome, reason code, source, and bounded attributes without moving Madar persistence into the reusable auditing package.

The case timeline reads those persisted audit records through an authorized Application service.

## API surface

The first slice exposes:

```text
GET  /health/live
GET  /api/security/antiforgery
POST /api/auth/login
POST /api/auth/logout
GET  /api/auth/me
GET  /api/users/operators

POST /api/cases/
GET  /api/cases/
GET  /api/cases/{caseId}
GET  /api/cases/{caseId}/timeline
POST /api/cases/{caseId}/assignment
POST /api/cases/{caseId}/transition
```

Swagger is enabled in Development. Case write endpoints require authentication, anti-CSRF validation, and the write rate-limit policy; the Application layer applies the finer product permission/ownership rules.

## Blazor UI

The current client includes:

```text
/                 product landing page
/login            cookie-authentication login
/cases            visible-case list + create form
/cases/{caseId}   details + assignment + lifecycle actions + audit timeline
```

The client uses a typed `MadarApiClient`, same-origin cookies, anti-CSRF tokens for protected writes, and an `AuthenticationStateProvider` backed by `/api/auth/me`.

## Local Docker run

The repository includes a development/test topology in `deploy/madar-compose.yml`. It requires explicit temporary credentials; no reusable passwords are committed.

Example PowerShell setup:

```powershell
$env:MADAR_SQL_PASSWORD = '<strong temporary SQL password>'
$env:MADAR_ADMIN_EMAIL = 'admin@madar.local'
$env:MADAR_ADMIN_PASSWORD = '<strong temporary administrator password>'
$env:MADAR_OPERATOR_EMAIL = 'operator@madar.local'
$env:MADAR_OPERATOR_PASSWORD = '<strong temporary operator password>'

docker compose -f deploy/madar-compose.yml up --build -d
```

Then open:

```text
http://localhost:8100/
http://localhost:8100/swagger
```

Stop and remove the development database volume with:

```powershell
docker compose -f deploy/madar-compose.yml down --volumes --remove-orphans
```

The compose file deliberately enables bootstrap users for this isolated development/test topology. Production identity provisioning, secrets, migration execution, and deployment controls require separate environment-specific decisions.

## Automated verification

Pull-request CI now treats Madar as an executable product consumer rather than only a compile-time shell. The repository gate covers:

- Release solution build with warnings as errors;
- Madar domain and Application authorization/audit tests;
- Madar publish output;
- non-root Madar container policy;
- SQL Server migration/startup;
- anonymous authorization boundary;
- administrator login with anti-CSRF protection;
- case creation and SQL persistence;
- operator discovery and assignment;
- operator-visible scoped queue;
- assigned-operator progress and resolution;
- administrator close;
- persisted audit timeline;
- final closed-case read;
- preservation of the 17 reusable NuGet + 17 symbol-package invariant;
- repository security and CodeQL workflows.

Exact evidence belongs to the specific PR/head that produced it; a previous green run is not proof for later behavior-relevant changes.

## Deliberately deferred after v0.1

The working first slice does **not** mean the broader Madar roadmap is complete. These remain deferred until a concrete product requirement justifies them:

- configurable workflow designer;
- SLA policies, breach tracking, and escalation rules;
- comments and watchers;
- document/file handling;
- advanced search and reporting;
- organizational hierarchy;
- multi-tenancy;
- background jobs;
- WhatsApp/email/external channel ingestion;
- production-grade backup/RPO/RTO and deployment-specific operational controls.

None of these should be moved into FoundationKit merely to make the capability roadmap look more complete.

## Product rule

When Madar reveals a missing capability, first decide whether the behavior is:

1. **Madar-specific** — keep it inside `apps/Madar`; or
2. **truly reusable** — extract or extend FoundationKit only with concrete evidence from multiple consumers and without breaking existing consumers.

This rule is central to using Madar as real-product validation for FoundationKit rather than turning FoundationKit into a framework secretly shaped around one product.

## Security and production boundary

The current automated evidence demonstrates the repository/runtime scope above. It does **not** by itself claim:

- production approval;
- independent Segregation-of-Duties approval;
- ISO/IEC 27001 certification;
- production secret/KMS topology;
- production network architecture;
- legal retention policy;
- production backup/restore acceptance;
- penetration/load acceptance.

Those remain deployment- and organization-specific controls.

## Tracking

The first product slice is tracked by GitHub issue **#71 — Madar v0.1: establish product foundation and first case vertical slice**. Broader roadmap capabilities should be tracked separately rather than silently expanding the v0.1 boundary.
