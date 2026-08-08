# Madar

> Status: **v0.1 and v0.1.1 are implemented and repository-verified; v0.2 SLA/escalation is in development**. Madar already has a working authentication → case lifecycle → SQL Server → audit timeline path plus bounded readiness/startup retry and repository operational integration. v0.2 adds product-owned SLA targets, breach evidence, and a bounded escalation-evaluation command. This is not a claim of production approval or completion of the broader product roadmap.

Madar is an operational case-management and orchestration product built on FoundationKit. It is intentionally separate from the reusable FoundationKit packages, the Workbench architecture sample, and the Athar reference product.

## Product purpose

Madar turns operational work into traceable cases that can be created, assigned, progressed through controlled states, audited, and governed by explicit service-level expectations.

Representative case types include customer complaints, operational incidents, internal service requests, access requests, compliance cases, technical escalations, and operational exceptions.

## Product boundary

Madar owns its business model, database schema, Identity configuration, permissions, Arabic UI copy, SLA policy values, runtime composition, and deployment topology. Reusable capabilities are consumed from FoundationKit only where their contracts fit the product.

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

## Implemented base vertical slice

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

The `Case` aggregate owns identity, title/description, bounded type and priority, lifecycle status, creator/assignee, timestamps, SQL Server `rowversion`, lifecycle domain events, and now the SLA snapshot/evidence fields described below.

The deterministic lifecycle uses `FoundationKit.Workflow`; Madar retains its product-specific states, triggers, authorization rules, SLA semantics, and UI behavior.

## v0.2 SLA model

SLA is deliberately a **Madar product policy**, not a new FoundationKit package.

Configuration is keyed by the existing priorities:

```text
Madar:Sla:Enabled
Madar:Sla:Low
Madar:Sla:Medium
Madar:Sla:High
Madar:Sla:Critical
```

When SLA is enabled, all four durations must be positive and no greater than 365 days. The actual duration values are deployment/product decisions. The repository does not claim that its development or CI values are production policy.

At case creation Madar resolves the duration once and snapshots:

```text
SlaTargetUtc = CreatedUtc + configured duration
```

That means later configuration changes do not silently rewrite historical expectations.

The current SLA state is one of:

| State | Meaning |
|---|---|
| `not-applicable` | SLA policy was disabled when the case was created |
| `active` | unresolved and still on/before its target |
| `met` | resolved on or before the target |
| `breached` | unresolved after the target or resolved after the target |

The time boundary is explicit: **exactly at `SlaTargetUtc` is still within SLA; breach starts only after the target**. Resolving exactly at the target therefore counts as `met`.

First breach persists:

```text
SlaBreachedUtc = SlaTargetUtc
EscalatedUtc   = first detection/evaluation time
```

`SlaBreachedUtc` is the deterministic instant at which the case crossed the contract boundary. `EscalatedUtc` records when Madar first materialized the breach/escalation evidence.

The domain operation is idempotent: evaluating an already breached case does not create another breach event or change the first escalation time.

### Late resolution without prior scan

A case can be resolved after its target before a periodic evaluator has run. `CaseManager` therefore checks SLA immediately after a successful `resolve` transition. A late resolution persists the breach and audit event in that same application operation.

### Bounded evaluation command

Madar exposes an explicit product command:

```text
POST /api/cases/sla/evaluate
```

with a bounded batch size of `1..100`. It:

1. requires authentication;
2. requires `madar.cases.sla.evaluate`;
3. is available to Supervisor/Administrator roles in the current role map;
4. queries only unresolved, not-yet-materialized due cases;
5. processes at most the requested batch;
6. records first breach/escalation audit evidence once;
7. returns evaluated count, breached count, and whether another batch remains.

This endpoint is intentionally the seam a future scheduler may call. v0.2 does **not** choose Hangfire, Quartz, a hosted service, a broker, or a cloud scheduler and does **not** extract `FoundationKit.Jobs`. Scheduling/provider selection needs real deployment evidence first.

The first slice also intentionally uses elapsed UTC time from case creation. Business calendars, holidays, work shifts, and SLA pause/resume semantics are not guessed.

## FoundationKit reuse proven by Madar

The product continues to reuse:

- `FoundationKit.Domain` for aggregate/domain primitives;
- `FoundationKit.Application` for results, current-user, persistence, clock, and unit-of-work contracts;
- `FoundationKit.Infrastructure` for `EfRepository`, `EfUnitOfWork`, and domain-event dispatch;
- `FoundationKit.WebApi` for request pipeline and HTTP result mapping;
- `FoundationKit.Blazor` for typed API-result handling;
- `FoundationKit.Security` for rate-limit partition conventions;
- `FoundationKit.Authorization` for role/permission evaluation;
- `FoundationKit.Auditing` for bounded audit events and sink contracts;
- `FoundationKit.Workflow` for lifecycle transition resolution.

ASP.NET Core Identity, SQL Server schema, audit persistence, SLA policy, case/SLA query rules, API endpoints, readiness policy, Docker topology, and Arabic UI remain Madar-owned behavior/adapters.

## Authentication and authorization

Madar uses ASP.NET Core Identity with secure cookie authentication, anti-CSRF validation for write operations, password policy, login lockout, and authentication/write rate limits.

| Role | Current responsibility |
|---|---|
| `Requester` | create cases and see cases they created |
| `Operator` | see assigned cases and progress their own assignment |
| `Supervisor` | read all cases, assign/progress/close cases, evaluate SLA batches |
| `Administrator` | receives all currently defined Madar case permissions |

The Application layer makes fine-grained authorization decisions. Infrastructure does not infer permissions from a user ID. Assignment validates that the selected assignee holds the `Operator` role.

## SQL Server and audit persistence

`MadarDbContext` owns:

```text
identity/*
madar/Cases
audit/AuditEvents
```

Migrations include:

```text
20260808093000_InitialMadar.cs
20260808110000_AddMadarSla.cs
```

The SLA migration adds nullable `SlaTargetUtc`, `SlaBreachedUtc`, and `EscalatedUtc` columns plus a due-case query index. Nullable columns preserve compatibility with pre-SLA cases and policy-disabled environments.

Case writes and audit records share the same Madar DbContext/unit-of-work boundary. SLA breach auditing stores bounded attributes such as priority, target, and escalation time; case description/body content is not copied into audit attributes.

## Startup and readiness

Madar retains the v0.1.1 database startup policy:

```text
Madar:DatabaseStartup:ApplyMigrationsOnStartup
Madar:DatabaseStartup:SeedRolesOnStartup
Madar:DatabaseStartup:MigrationAttempts
Madar:DatabaseStartup:DelaySeconds
```

Startup retries transient database failures within configured bounds. If automatic migrations are disabled, startup verifies connectivity and rejects a schema with pending migrations.

`GET /health/live` proves process liveness. `GET /health/ready` verifies SQL connectivity and that no EF migration remains pending without exposing connection strings/infrastructure details.

## API surface

```text
GET  /health/live
GET  /health/ready
GET  /api/security/antiforgery
POST /api/auth/login
POST /api/auth/logout
GET  /api/auth/me
GET  /api/users/operators

POST /api/cases/
GET  /api/cases/
POST /api/cases/sla/evaluate
GET  /api/cases/{caseId}
GET  /api/cases/{caseId}/timeline
POST /api/cases/{caseId}/assignment
POST /api/cases/{caseId}/transition
```

All case writes, including SLA evaluation, use the normal anti-CSRF and write-rate-limit path. The Application layer applies the finer permission and ownership rules.

## Blazor UI

```text
/                         product landing page
/login                    cookie-authentication login
/cases                    list/create + SLA state/target + supervisor evaluation
/cases/{CaseId:guid}      details + lifecycle + SLA/breach/escalation + audit timeline
```

Supervisors and administrators receive a bounded “تقييم SLA” action in the case list. Case details surface the snapshotted target, SLA state, breach time, escalation time, and the SLA breach event in the audit timeline.

## Local run

The current supported operational path is Docker:

```powershell
.\foundationkit.ps1 start  -Target Madar -Mode Docker
.\foundationkit.ps1 status -Target Madar
.\foundationkit.ps1 logs   -Target Madar
.\foundationkit.ps1 stop   -Target Madar
```

The specialized launcher is also available:

```powershell
.\scripts\madar-product.ps1 start
```

It creates `.local/madar-product.env`, restricts it to the current Windows account, generates local credentials, and writes SLA settings with:

```text
MADAR_SLA_ENABLED=false
```

The duration values in that local file are placeholders only and have no production-policy meaning. To exercise SLA locally, deliberately set `MADAR_SLA_ENABLED=true` and choose product-appropriate development durations before restarting the stack.

The Compose project name is `madar-product`; stop preserves the SQL volume. Destructive Madar reset remains intentionally excluded from the unified manager.

Read [`../../docs/MADAR-OPERATIONS-AR.md`](../../docs/MADAR-OPERATIONS-AR.md) for the detailed runbook.

## Automated verification

The repository gate covers the existing Madar baseline plus v0.2 behavior:

- Release solution build with warnings as errors;
- domain boundary tests for SLA target/state and exact-target semantics;
- Application tests for SLA target snapshot and late-resolution breach audit;
- authorized/bounded/idempotent SLA evaluation tests;
- SQL migration/readiness;
- Swagger/API surface including `EvaluateMadarCaseSla`;
- SQL E2E with an explicitly short **CI-only** critical SLA policy proving target snapshot → elapsed breach → persisted escalation/audit → second evaluation no duplicate;
- a separate normal case proving SLA `met` on the existing assignment/lifecycle flow;
- existing Workbench/Athar regressions;
- non-root container, Trivy image gate/SARIF, CodeQL, Atlas, Windows launcher, and reusable 17+17 package invariant.

Exact evidence belongs to the exact PR head that produced it; a previous green run is not proof for later behavior-relevant changes.

## Deliberately deferred after this slice

- production scheduler/provider selection;
- reusable Background Jobs extraction;
- business-hours calendars/holidays and shift arithmetic;
- SLA pause/resume semantics;
- multiple escalation levels/chains;
- notification delivery on breach;
- comments/watchers;
- document/file handling;
- advanced search/reporting;
- organization hierarchy and multi-tenancy;
- WhatsApp/email/external channel ingestion;
- production backup/RPO/RTO and deployment-specific controls.

## Product rule

When Madar reveals a missing capability, first decide whether the behavior is Madar-specific or truly reusable. A reusable FoundationKit package requires concrete independent evidence; it is not created merely to reduce roadmap checkboxes.

## Security and production boundary

The repository evidence does not itself claim Production Approval, independent Segregation-of-Duties approval, ISO/IEC 27001 certification, production secret/KMS/network topology, legal retention policy, backup/RPO/RTO acceptance, or penetration/load acceptance.

## Tracking

- #71 — v0.1 first case vertical slice: complete.
- #74 — v0.1.1 operational integration/readiness closure: complete.
- #76 — v0.2 SLA deadlines, breach detection, and escalation semantics: current work.
