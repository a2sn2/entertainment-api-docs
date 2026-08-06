# FoundationKit for .NET

[![FoundationKit CI](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/ci.yml)
[![Blazor Pages Demo](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/pages.yml/badge.svg)](https://a2sn2.github.io/foundationkit-dotnet/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Target: .NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)

FoundationKit is a reusable .NET production baseline for domain-driven, Clean Architecture applications.

The repository now has three deliberately different layers of proof:

```text
src/FoundationKit.*          reusable core packages
samples/Workbench            architecture discovery and dual-stack workflow
examples/Athar               complete Arabic production-reference product
```

## الصورة الكاملة

```text
                         FOUNDATIONKIT CORE
 Domain | Application | Infrastructure | WebApi | Blazor
                                │
              ┌─────────────────┴─────────────────┐
              │                                   │
      WORKBENCH REFERENCE                 ATHAR PRODUCT EXAMPLE
  user/admin architecture map         complete Arabic full-stack app
              │                                   │
      SQL-backed review flow       Identity + CSRF + Roles + Audit
                                                  │
                              User UI ↔ API ↔ Admin UI ↔ SQL Server
```

## 1 — Reusable Core

```text
src/FoundationKit.Domain
src/FoundationKit.Application
src/FoundationKit.Infrastructure
src/FoundationKit.WebApi
src/FoundationKit.Blazor
```

Core capabilities include:

- entities, aggregate roots, value objects, and domain events;
- commands, queries, use cases, validation, pagination, and classified results;
- generic repository/specification/unit-of-work abstractions;
- provider-neutral EF Core adapters;
- Problem Details, correlation IDs, and security headers;
- typed Blazor API results and resilient response parsing;
- `EntityDto<TId>` and `AuditedEntityDto<TId>`;
- Blazor-oriented `ViewModelBase` and `ListViewModel<T>`.

The core never owns a product database, SQL Server migrations, roles, UI design, or business rules.

## 2 — Workbench Reference

The Workbench explains two connected vertical slices:

```text
User Full Stack
    ↓ submitted request
Admin Full Stack
    ↓ approved or rejected
User reads updated status
```

Projects:

```text
samples/FoundationKit.Workbench/
samples/FoundationKit.Workbench.Contracts/
samples/FoundationKit.Workbench.Client/
```

Documentation:

- [Dual Full-Stack Architecture](docs/DUAL-FULL-STACK.md)
- [Workbench Operations](docs/WORKBENCH.md)
- [Technical Architecture](docs/ARCHITECTURE.md)

## 3 — منصة أثَر: المشروع العربي الاحترافي

**أثَر** هو المثال الكامل الذي يثبت ربط FoundationKit بمنتج حقيقي من البداية إلى النهاية.

فكرته: منصة لإدارة المبادرات المجتمعية؛ المستخدم ينشئ حسابًا ويقدّم مبادرة ويتابعها، والإدارة تراجع وتعتمد أو ترفض مع سجل تدقيق دائم.

```text
examples/Athar/
├── Athar.Domain
├── Athar.Application
├── Athar.Infrastructure
├── Athar.Contracts
├── Athar.Api
└── Athar.Client

tests/Athar.Tests
postman/Athar.Api.postman_collection.json
deploy/athar-compose.yml
```

### User Full Stack

```text
Blazor Arabic UI
    ↓
InitiativesViewModel
    ↓
AtharApiClient
    ↓
CreateInitiativeRequest
    ↓
POST /api/v1/initiatives
    ↓
InitiativeManager
    ↓
Initiative Aggregate
    ↓
EF Core + SQL Server
```

### Admin Full Stack

```text
Arabic Admin Dashboard
    ↓
AdminViewModel
    ↓
AtharApiClient
    ↓
GET /api/v1/admin/initiatives
POST /api/v1/admin/initiatives/{id}/review
    ↓
InitiativeManager
    ↓
InitiativeReview + AuditEntry + Status transition
    ↓
SQL Server
```

### Security and operational baseline

- ASP.NET Core Identity;
- Cookie Authentication;
- User and Administrator roles;
- password policy and lockout;
- anti-CSRF token on every write;
- rate limiting;
- idempotent initiative creation;
- optimistic concurrency with `RowVersion`;
- audit trail;
- SQL Server migrations and startup retry;
- live/ready health endpoints;
- Swagger and Postman;
- Docker and CI smoke testing.

Read first:

- [منصة أثر](examples/Athar/README.md)
- [جاهزية الإنتاج](docs/PRODUCTION-READINESS-AR.md)
- [إضافة مشروع جديد](docs/ADDING-A-PROJECT-AR.md)

## Repository layout

```text
src/          reusable FoundationKit packages
samples/      architecture samples
examples/     complete reference products
apps/         reserved for real products using the same boundaries
tests/        core, Workbench, and product tests
postman/      executable API collections
deploy/       Docker topologies
scripts/      verification and smoke tests
docs/         architecture, operations, and production gates
catalog/      canonical core capability catalog
tools/        repository tooling
```

## Visual Studio 2026

Open:

```text
FoundationKit.sln
```

Available startup projects:

```text
FoundationKit.Workbench.Api   architecture reference
Athar.Api                     complete Arabic product example
```

Local execution will be performed after the repository implementation and CI verification are complete.

## Build and verify

```bash
dotnet restore FoundationKit.sln
dotnet build FoundationKit.sln --configuration Release --no-restore
dotnet test FoundationKit.sln --configuration Release --no-build
bash scripts/verify-repository.sh
```

Docker smoke paths:

```bash
bash scripts/smoke-workbench.sh
bash scripts/smoke-athar.sh
```

## Production statement

FoundationKit provides a tested production baseline. A real deployment is approved only after the product-specific environment passes the security, data recovery, observability, performance, and acceptance gates documented in [PRODUCTION-READINESS-AR.md](docs/PRODUCTION-READINESS-AR.md).

Current package version: `0.1.0`.
