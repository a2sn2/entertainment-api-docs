# Madar

> Status: **v0.1–v0.6 are implemented and repository-verified; v0.7 department administration is in development**. Repository evidence demonstrates the implemented product behavior for the exact verified commit; it is not Production Approval or an external security certification.

Madar is an operational case-management and orchestration product built on FoundationKit. It is intentionally separate from the reusable FoundationKit packages, the Workbench architecture sample, and the Athar reference product.

## Product purpose

Madar turns operational work into traceable cases that can be created, routed, assigned, progressed through controlled states, audited, governed by SLA expectations, collaborated on, approved where sensitive, and accompanied by bounded operational notifications.

Representative case types include customer complaints, operational incidents, internal service requests, access requests, compliance cases, technical escalations, and operational exceptions.

## Product boundary

Madar owns its business model, SQL schema, Identity configuration, permissions, Arabic UI copy, organization/routing semantics, SLA policy values, runtime composition, and deployment topology. FoundationKit capabilities are reused only where their contracts fit the product.

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

Dependency direction:

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

Infrastructure dependencies do not enter Domain. The Blazor client does not reference Infrastructure or `MadarDbContext`. EF Core migrations under `Madar.Infrastructure/Migrations` are the product schema source of truth.

## Implemented product depth

```text
v0.1   Auth + SQL + case lifecycle + audit + Arabic API/Blazor
v0.1.1 Readiness + startup retry + local/Docker operational integration
v0.2   SLA deadlines + first breach/escalation evidence
v0.3   Append-only case comments
v0.4   Maker-checker approval gate for sensitive case resolution
v0.5   Bounded operational notifications
v0.6   Department queues + routing + operator claim flow
v0.7   Department administration + safe Operator membership   ← current
```

The deterministic lifecycle remains:

```text
new → assigned → in-progress → resolved → closed
```

Routing is deliberately contextual rather than a new workflow state:

```text
new/unassigned
     ↓ route
Department queue
     ↓ claim or direct assignment
assigned
     ↓
in-progress → resolved → closed
```

## Department routing model

Madar owns:

```text
Department
├── Id
├── Code
├── Name
├── IsActive
├── CreatedUtc
├── UpdatedUtc
└── RowVersion

DepartmentMembership
├── DepartmentId
├── UserId
└── JoinedUtc

Case
├── DepartmentId?
└── RoutedUtc?
```

A Supervisor/Administrator can route a `new`, unassigned case to an active department. The case remains `new`. The department queue contains only cases for that department that are still `new` and unassigned.

An Operator can read a department queue only when the user is an active member of that department. Claiming requires Operator eligibility, the `madar.cases.claim` permission, and membership; claim reuses `Case.Assign(...)` and assigns the case to the current user.

If a routed case is assigned directly, the assignee must be an Operator member of the routed department. Unrouted direct assignment remains supported for compatibility with the previous product flow.

See [`../../docs/MADAR-DEPARTMENT-ROUTING-AR.md`](../../docs/MADAR-DEPARTMENT-ROUTING-AR.md).

## v0.7 department administration

v0.7 makes the proven v0.6 department model operationally manageable instead of depending on bootstrap-only organization data.

The product permission is:

```text
madar.departments.manage
```

and is granted only to `Administrator` in the current role map.

Department codes are normalized and immutable after creation. Administrators may rename departments and change active state. Deactivation fails closed while any non-closed case still belongs to the department, preventing operational work from disappearing behind an inactive department.

Membership administration is intentionally narrower than generic user/role management. An added member must already exist and hold the `Operator` role. Duplicate membership returns a deterministic conflict. Removal is blocked while the Operator still owns any non-closed assigned case in that department.

Administration audit actions contain bounded operational identifiers only:

```text
madar.department.created
madar.department.updated
madar.department.member-added
madar.department.member-removed
```

See [`../../docs/MADAR-DEPARTMENT-ADMINISTRATION-AR.md`](../../docs/MADAR-DEPARTMENT-ADMINISTRATION-AR.md).

## Existing SLA behavior

When SLA is enabled, Madar snapshots an absolute target at case creation:

```text
SlaTargetUtc = CreatedUtc + configured duration
```

States are `not-applicable`, `active`, `met`, and `breached`. Exactly at the target is still within SLA; breach begins only after the target. First breach persists `SlaBreachedUtc = SlaTargetUtc` and the first materialization time in `EscalatedUtc`.

The bounded evaluator remains:

```text
POST /api/cases/sla/evaluate
```

Madar still does not choose a reusable jobs/scheduler package from this evidence alone.

## Collaboration, approvals, and notifications

- Comments are product-owned append-only case collaboration data.
- `access-request` and `compliance-case` use a maker-checker approval gate before resolution; Madar reuses `FoundationKit.Approvals` rather than duplicating generic approval decision semantics.
- Assignment, approval decision, and cross-user resolution can trigger bounded best-effort notifications through `FoundationKit.Notifications` and the current optional SMTP provider.
- Notification transport failure does not undo an already-saved business operation, and notification destination/body are excluded from audit metadata.

## FoundationKit reuse

Madar currently reuses:

- `FoundationKit.Domain` — aggregate/domain primitives;
- `FoundationKit.Application` — results, persistence, clock, unit-of-work contracts;
- `FoundationKit.Infrastructure` — EF repository/unit-of-work and domain-event dispatch;
- `FoundationKit.WebApi` — request pipeline and HTTP result mapping;
- `FoundationKit.Blazor` — typed API-result handling;
- `FoundationKit.Security` — rate-limit conventions;
- `FoundationKit.Authorization` — role/permission evaluation;
- `FoundationKit.Auditing` — bounded audit events/sink contracts;
- `FoundationKit.Workflow` — case lifecycle transition resolution;
- `FoundationKit.Approvals` — generic approval eligibility/decision semantics;
- `FoundationKit.Notifications` and `.Smtp` — bounded notification contract/current provider.

Madar does **not** introduce `FoundationKit.Organization` in v0.7. Department routing and administration remain product-owned until another independent product demonstrates a sufficiently general organization contract.

## Authentication and authorization

Madar uses ASP.NET Core Identity with secure cookie authentication, anti-CSRF validation for writes, password policy, login lockout, and authentication/write rate limits.

| Role | Current responsibility |
|---|---|
| `Requester` | create cases and see cases they created |
| `Operator` | see assigned cases, see member department queues, claim queued cases, progress own assignments |
| `Supervisor` | read all cases, route/assign/progress/close, evaluate SLA, make approval decisions |
| `Administrator` | receives all currently defined Madar permissions, including department/membership administration |

Application code makes fine-grained authorization decisions. Infrastructure does not infer permissions merely from a user ID.

## SQL Server persistence

`MadarDbContext` owns:

```text
identity/*
madar/Cases
madar/CaseComments
madar/CaseApprovals
madar/Departments
madar/DepartmentMemberships
audit/AuditEvents
```

Current migrations include:

```text
20260808093000_InitialMadar
20260808110000_AddMadarSla
20260808143000_AddCaseComments
20260808155000_AddCaseApprovals
20260808173000_AddDepartmentRouting
20260808180000_AddDepartmentAdministration
```

The routing migration adds the two organization tables plus nullable case `DepartmentId`/`RoutedUtc`, FK constraints, and queue/membership indexes. The v0.7 migration adds non-null `Departments.UpdatedUtc`, backfilled from `CreatedUtc` for existing records, while retaining SQL rowversion concurrency.

## Bootstrap and local run

The supported local operational path is Docker:

```powershell
.\foundationkit.ps1 start  -Target Madar -Mode Docker
.\foundationkit.ps1 status -Target Madar
.\foundationkit.ps1 logs   -Target Madar
.\foundationkit.ps1 stop   -Target Madar
```

The specialized launcher remains:

```powershell
.\scripts\madar-product.ps1 start
```

When bootstrap is enabled, Madar seeds Administrator and Operator users. It also ensures one deterministic development/CI department:

```text
Code: operations
Name: العمليات
```

and attaches the seeded Operator to it. This is test/development topology, not a production organization policy. v0.7 adds the administration flow for creating and managing additional product-owned departments.

Read [`../../docs/MADAR-OPERATIONS-AR.md`](../../docs/MADAR-OPERATIONS-AR.md) for the runbook.

## API surface highlights

```text
GET  /health/live
GET  /health/ready
GET  /api/security/antiforgery
POST /api/auth/login
POST /api/auth/logout
GET  /api/auth/me
GET  /api/users/operators

GET  /api/cases
POST /api/cases
GET  /api/cases/{caseId}
POST /api/cases/{caseId}/assignment
POST /api/cases/{caseId}/route
POST /api/cases/{caseId}/claim
POST /api/cases/{caseId}/transition
GET  /api/cases/{caseId}/timeline
POST /api/cases/sla/evaluate

GET  /api/departments
GET  /api/departments/{departmentId}/queue

GET    /api/admin/departments
POST   /api/admin/departments
PUT    /api/admin/departments/{departmentId}
GET    /api/admin/departments/{departmentId}/members
POST   /api/admin/departments/{departmentId}/members
DELETE /api/admin/departments/{departmentId}/members/{userId}

GET/POST /api/cases/{caseId}/comments
GET/POST /api/cases/{caseId}/approvals
POST     /api/cases/{caseId}/approvals/{approvalId}/decision
```

All writes use the normal anti-CSRF/write-rate-limit path. Application handlers apply permission, ownership, membership, lifecycle, and maker-checker rules.

## Blazor UI

```text
/                         product landing page
/login                    cookie-authentication login
/cases                    cases + department queue + create + SLA evaluation
/cases/{CaseId:guid}      details + route/claim/assign + lifecycle + collaboration + audit
/admin/departments        Administrator department + Operator membership management
```

The administration route is role-gated in the client and remains permission-gated again in the Application layer.

## Automated verification

The repository gate is expected to cover:

- Release solution build with warnings as errors;
- Madar domain/application routing, department administration, and membership tests;
- migration/snapshot/readiness correctness;
- authenticated API/Swagger surface for routing and department administration;
- existing Workbench and Athar regressions;
- existing Madar v0.1–v0.6 SQL/E2E flows;
- dedicated Madar SQL administration proof covering create → membership → guarded deactivation/removal → close → cleanup → persisted audit metadata;
- Security Scan and CodeQL;
- unchanged reusable 17 NuGet + 17 symbol package output.

Exact evidence belongs to the exact PR head that produced it. A previous green run is not proof for later behavior-relevant changes.

## Deliberately deferred

- production organization tree / branch/team hierarchy;
- multi-tenancy;
- arbitrary product user/role administration;
- transfer/reassignment workflow and rich transfer history;
- multiple queues per department;
- skill-based, round-robin, capacity, presence, or automatic routing;
- reusable `FoundationKit.Organization` extraction;
- routing-specific business-hours/SLA policy;
- durable notification outbox/retries/background scheduler;
- documents/files;
- advanced search/reporting;
- WhatsApp/email/external channel ingestion.

## Product rule

When Madar reveals a missing capability, first decide whether the behavior is product-specific or truly reusable. A FoundationKit package requires concrete independent evidence and a clean general contract; it is not created merely to reduce roadmap checkboxes.

## Tracking

- #71 — v0.1 first case vertical slice: complete.
- #74 — v0.1.1 readiness/operational integration: complete.
- #76 — v0.2 SLA/escalation: complete.
- #78 — v0.3 comments: complete.
- #80 — v0.4 approvals: complete.
- #82 — v0.5 notifications: complete.
- #84 — v0.6 department queues/routing: complete.
- #86 — v0.7 department administration: current work.
