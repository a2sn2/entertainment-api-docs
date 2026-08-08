# Madar

> Status: **Product foundation / v0.1 planning**. This directory does not yet represent a completed runtime product.

Madar is an operational case-management and orchestration product built on FoundationKit. It is intentionally separate from the FoundationKit reusable packages, the Workbench architecture sample, and the Athar reference product.

## Product purpose

Madar turns operational work into traceable cases that can be assigned, progressed through controlled states, reviewed, audited, measured, and later governed by SLA and escalation policies.

Representative case types include:

- customer complaints;
- transaction or operational incidents;
- internal service requests;
- access requests;
- compliance cases;
- technical escalations;
- operational exceptions.

## Product boundary

Madar owns its business model and product behavior. Reusable capabilities are consumed from FoundationKit when their contracts fit the product. Madar-specific concepts must not be moved into FoundationKit merely to avoid local code.

Planned product projects:

```text
apps/Madar/
├── Madar.Domain
├── Madar.Application
├── Madar.Infrastructure
├── Madar.Contracts
├── Madar.Api
├── Madar.Client
└── Madar.Tests
```

The intended dependency direction is:

```text
Madar.Domain
    ↓
Madar.Application
    ↓
Madar.Infrastructure
    ↓
Madar.Api

Madar.Client → Madar.Contracts / HTTP API
```

Infrastructure dependencies must not leak into Domain. UI concerns must not become domain rules. EF Core migrations are the schema source of truth.

## FoundationKit capabilities expected to be reused

The first implementation is expected to evaluate and reuse the existing FoundationKit packages where appropriate, especially:

- Domain;
- Application;
- Infrastructure;
- WebApi;
- Blazor;
- Security;
- Identity;
- Authorization;
- Auditing;
- Workflow;
- Approvals;
- Notifications;
- Settings;
- Feature Management;
- Localization;
- Caching.

Reuse is evidence-driven: a package is adopted only where its current contract satisfies Madar's need without forcing Madar-specific behavior into the reusable core.

## v0.1 first vertical slice

The first working slice is deliberately small:

```text
Authenticate
    ↓
Create Case
    ↓
Persist in SQL Server
    ↓
List / View
    ↓
Assign
    ↓
New → Assigned → InProgress → Resolved → Closed
    ↓
Audit Timeline
```

Initial case data is expected to include:

- identifier;
- title;
- description;
- case type;
- priority;
- status;
- creator;
- current assignee when assigned;
- created/updated timestamps;
- concurrency metadata where required.

## Deferred until the first slice works

The following are product goals, not v0.1 implementation claims:

- configurable workflow designer;
- SLA policies and breach tracking;
- escalation rules;
- comments and watchers;
- document/file handling;
- advanced search and reporting;
- organizational hierarchy;
- multi-tenancy;
- background jobs;
- WhatsApp/email/external channel ingestion.

These will be added incrementally after a working product path proves the required boundaries.

## Product rule

When Madar reveals a missing capability, first decide whether the behavior is:

1. **Madar-specific** — keep it inside `apps/Madar`; or
2. **truly reusable** — extract or extend FoundationKit only with concrete consumer evidence and without breaking existing consumers.

This rule is central to using Madar as a real-product validation surface for FoundationKit rather than turning FoundationKit into a product-specific framework.

## Tracking

Initial product work is tracked by GitHub issue **#71 — Madar v0.1: establish product foundation and first case vertical slice**.
