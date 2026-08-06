# Complete Repository Engineering Reference

**Repository:** `a2sn2/entertainment-api-docs`  
**Reference baseline:** `main` at commit `5f9eb687860c72b076e2640645144dd8ecd6458c`  
**Documentation language:** English  
**Format:** Markdown  
**Audience:** developers, reviewers, architects, QA engineers, DevOps engineers, security reviewers, and AI assistants

---

## 1. Purpose

This directory is the detailed engineering reference for the complete repository. It is intended to let a reader understand the codebase without relying on a previous chat, undocumented tribal knowledge, or a live walkthrough from the original author.

The repository contains two connected systems:

1. a static browser-only Entertainment Services API documentation portal hosted through GitHub Pages; and
2. a reusable .NET engineering core named **FoundationKit**, plus the **EntertainmentDocs** reference product built on that core.

The reference explains:

- every top-level folder;
- every tracked source, configuration, deployment, test, documentation, and generated file;
- the responsibility and dependency direction of every .NET project;
- the purpose of each meaningful code block;
- the runtime flow of requests from browser to API to application to domain to SQL Server;
- the reasons behind the architectural choices;
- the syntax and recurring keywords used by C#, Razor, JavaScript, JSON, XML/MSBuild, YAML, PowerShell, Bash, Docker, Nginx, HTML, and CSS;
- local development, Postman, test-stack, package, CI, and publishing procedures;
- security boundaries and known production-readiness limits.

---

## 2. How this reference handles “every line”

A literal repetition of the same language keyword on every occurrence would make the documentation less accurate and harder to use. This reference therefore uses three complementary levels of explanation.

### 2.1 Line-by-line semantic walkthrough

Files that contain business logic, reusable framework behavior, security behavior, persistence logic, request processing, UI state, or operational automation are explained in execution order. Each statement or tightly related statement group is described with:

- what it does;
- why it exists;
- what depends on it;
- what can fail;
- what must remain true when it is modified.

### 2.2 Exact file-by-file catalog

Every tracked file is listed in the file catalog with its type, ownership, responsibility, runtime relevance, and the chapter that explains it.

### 2.3 Shared syntax glossary

Recurring tokens such as `public`, `sealed`, `record`, `async`, `await`, `Task`, `?`, `!`, `=>`, `where`, `@inject`, `${...}`, `set -euo pipefail`, and XML/MSBuild elements are defined once in the syntax glossary. Individual file walkthroughs then explain the role of the construct in that file instead of repeating the generic language definition hundreds of times.

### 2.4 Generated files

EF Core migration designer files, model snapshots, and Visual Studio solution configuration contain large amounts of deterministic generated content. They are documented by generated section, schema object, and operational meaning. They should not be manually edited line by line unless a specific incident requires forensic analysis.

---

## 3. Reference chapters

| Chapter | Scope |
|---|---|
| [`00-AI-HANDOFF.md`](00-AI-HANDOFF.md) | Self-contained context package for a new AI conversation |
| [`01-REPOSITORY-MAP.md`](01-REPOSITORY-MAP.md) | Repository tree, ownership boundaries, and navigation guide |
| [`02-LANGUAGE-SYNTAX-GLOSSARY.md`](02-LANGUAGE-SYNTAX-GLOSSARY.md) | Detailed glossary for all languages and configuration formats used |
| [`03-STATIC-PORTAL.md`](03-STATIC-PORTAL.md) | Root GitHub Pages portal, JavaScript layers, HTML shells, CSS, PWA, and search/playground behavior |
| [`04-FOUNDATIONKIT.md`](04-FOUNDATIONKIT.md) | Reusable Domain, Application, Infrastructure, WebApi, and Blazor core projects |
| [`05-PRODUCT-BACKEND.md`](05-PRODUCT-BACKEND.md) | EntertainmentDocs Domain, Application, Contracts, commands, queries, and workflow |
| [`06-DATABASE-AND-INFRASTRUCTURE.md`](06-DATABASE-AND-INFRASTRUCTURE.md) | EF Core, SQL Server, Identity persistence, repository adapters, configurations, and migrations |
| [`07-API-AND-SECURITY.md`](07-API-AND-SECURITY.md) | ASP.NET Core composition, endpoints, JWT, policies, CORS, rate limiting, and errors |
| [`08-BLAZOR-FRONTEND.md`](08-BLAZOR-FRONTEND.md) | Admin, Client, shared UI, MudBlazor, browser authentication, typed clients, and page state |
| [`09-POSTMAN-AND-CONTRACTS.md`](09-POSTMAN-AND-CONTRACTS.md) | HTTP contracts, Postman assets, request order, variables, and synchronization rules |
| [`10-TESTING-DOCKER-CI-SCRIPTS.md`](10-TESTING-DOCKER-CI-SCRIPTS.md) | Unit tests, architecture tests, PowerShell/Bash scripts, Docker, Nginx, and GitHub Actions |
| [`11-FILE-BY-FILE-CATALOG.md`](11-FILE-BY-FILE-CATALOG.md) | Exhaustive tracked-file coverage matrix |
| [`12-RUNTIME-WALKTHROUGHS.md`](12-RUNTIME-WALKTHROUGHS.md) | End-to-end execution sequences and state transitions |
| [`13-GENERATED-FILES-AND-SCHEMA.md`](13-GENERATED-FILES-AND-SCHEMA.md) | EF migration artifacts, solution metadata, and generated-file maintenance rules |
| [`14-DESIGN-DECISIONS-AND-LIMITS.md`](14-DESIGN-DECISIONS-AND-LIMITS.md) | Architectural reasons, deliberate non-features, risks, and production boundaries |

---

## 4. Fast reading paths

### 4.1 A new developer joining the repository

Read in this order:

1. `00-AI-HANDOFF.md`
2. `01-REPOSITORY-MAP.md`
3. `12-RUNTIME-WALKTHROUGHS.md`
4. the chapter for the layer being changed;
5. `11-FILE-BY-FILE-CATALOG.md` for exact file ownership.

### 4.2 A developer starting a new product with FoundationKit

Read:

1. `04-FOUNDATIONKIT.md`
2. `14-DESIGN-DECISIONS-AND-LIMITS.md`
3. `platform/docs/NEW-PROJECT-BOOTSTRAP.md`
4. `10-TESTING-DOCKER-CI-SCRIPTS.md`

### 4.3 An API or Postman tester

Read:

1. `09-POSTMAN-AND-CONTRACTS.md`
2. `07-API-AND-SECURITY.md`
3. `12-RUNTIME-WALKTHROUGHS.md`
4. `platform/docs/POSTMAN-REQUESTS.md`

### 4.4 A database reviewer

Read:

1. `06-DATABASE-AND-INFRASTRUCTURE.md`
2. `13-GENERATED-FILES-AND-SCHEMA.md`
3. `platform/docs/LOCAL-SQLSERVER.md`
4. `platform/docs/PRODUCTION-READINESS.md`

### 4.5 A frontend developer

Read:

1. `08-BLAZOR-FRONTEND.md`
2. the Blazor and Razor sections in `02-LANGUAGE-SYNTAX-GLOSSARY.md`
3. `09-POSTMAN-AND-CONTRACTS.md`
4. `platform/docs/FRONTEND-ARCHITECTURE.md`

---

## 5. Architecture summary

The dynamic platform is a modular monolith with Clean Architecture and DDD-style boundaries.

```text
FoundationKit reusable technical core
                 │
                 ▼
EntertainmentDocs product layers

Browser / Postman
        │ HTTP
        ▼
ASP.NET Core API
        │
        ▼
Application commands and queries
        │
        ▼
Domain aggregates and business invariants
        │ ports
        ▼
Infrastructure adapters
        │ EF Core
        ▼
Microsoft SQL Server
```

The frontend dependency path is separate from backend implementation details:

```text
Razor page/component
        ▼
Feature typed API client
        ▼
EntertainmentDocs.Contracts
        ▼ HTTP
ASP.NET Core API
```

The browser does not reference Domain, Infrastructure, EF Core, SQL Server, or connection strings.

---

## 6. Core non-negotiable rules

1. Domain code must not depend on database, HTTP, UI, or hosting frameworks.
2. Application code defines use cases and ports; it must not query SQL Server directly.
3. Infrastructure implements ports and owns provider-specific configuration.
4. API endpoints must remain thin transport adapters.
5. Product contracts are transport models, not domain entities.
6. Frontends call typed API clients rather than issuing raw HTTP requests in reusable page logic.
7. The API is the real authorization boundary; hidden UI controls are only a user-experience measure.
8. EF Core migrations are the schema source of truth.
9. Production secrets must not be committed.
10. Reusable technical behavior may move to FoundationKit only when it is genuinely product-independent.
11. Product-specific rules must not be hidden behind generic managers or automatic CRUD controllers.
12. A change to a request or response shape must update Contracts, API, frontend client, Postman, documentation, and tests together.

---

## 7. Documentation maintenance rule

When code changes, update the chapter that owns the changed file and update the file catalog if a file is added, removed, renamed, or changes responsibility. The baseline commit in this index must be advanced when the reference is reviewed against a newer `main` commit.

A documentation statement must be classified as one of:

- **implemented behavior** — directly supported by current source;
- **operational convention** — required by repository scripts or documented workflow;
- **design intent** — an explicit architectural direction not necessarily fully implemented;
- **production gap** — required before production but not claimed as complete.

This classification prevents future readers or AI assistants from silently treating plans as implemented code.
