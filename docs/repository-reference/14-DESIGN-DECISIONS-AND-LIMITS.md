# Design Decisions, Trade-offs, and Current Limits

This chapter explains why the repository is structured this way, what complexity it deliberately avoids, and which production capabilities are not yet claimed.

---

# 1. Why a modular monolith

Decision: one backend deployment with explicit internal boundaries.

Benefits:

- one process and database to operate;
- simple local debugging;
- straightforward transactions;
- lower deployment and observability cost;
- capability folders and projects still protect ownership;
- future extraction remains possible behind contracts.

Why not microservices now:

- no demonstrated independent scaling/deployment need;
- distributed transactions, network failure, versioning, tracing, and deployment coordination would add cost before product value;
- current team can evolve business models faster in one solution.

Extraction trigger should be real operational evidence such as independent scaling, ownership, release cadence, isolation, or technology requirements—not folder size alone.

---

# 2. Why Clean Architecture dependency direction

Decision: frameworks and adapters depend on business/application contracts.

```text
Domain <- Application <- Infrastructure/API
```

Benefits:

- domain tests run without SQL or HTTP;
- SQL Server can change without rewriting aggregates;
- handlers can be tested with fake ports;
- API route changes do not redefine domain meaning;
- browser clients consume contracts rather than server internals.

Trade-off: more projects/interfaces/mapping than a small CRUD application. The repository accepts this because it is intended as a reusable product foundation.

---

# 3. Why DDD-style aggregates

Decision: `DocumentationDocument` owns version and workflow rules.

Without an aggregate, any endpoint/repository/page could set status directly and create impossible states.

Aggregate benefits:

- review-before-publish rule lives once;
- archived version restriction lives once;
- child creation is controlled;
- tests target business behavior;
- persistence maps private collection rather than exposing setters.

The repository says “DDD-style” because not every strategic DDD practice is implemented. Bounded contexts are architectural guidance, and only the documentation aggregate is currently modeled deeply.

---

# 4. Why explicit commands and queries

Decision: one class/handler per use case.

Benefits:

- name communicates intent;
- dependencies are minimal and visible;
- test scope is small;
- endpoint is thin;
- adding cross-cutting pipelines later is possible;
- avoids a growing `DocumentManager` with unrelated methods.

Trade-off: more files and explicit DI registrations.

The repository prefers clarity over reducing file count.

---

# 5. Why not generic managers/controllers

Rejected pattern:

```text
GenericRepository<TEntity>
GenericManager<TEntity>
GenericService<TEntity>
GenericController<TEntity>
```

Problems:

- every product rule becomes an override/flag;
- routes expose data merely because a table exists;
- authorization becomes generic and error-prone;
- transaction intent is hidden;
- business language disappears behind CRUD verbs;
- updates can bypass aggregate methods.

Accepted hybrid:

- generic technical repository operations;
- specialized product repository queries;
- explicit commands/queries;
- explicit endpoints.

---

# 6. Why Unit of Work remains thin

EF Core DbContext already tracks entities and commits changes. `IUnitOfWork.SaveChangesAsync` provides an Application port without duplicating DbContext features.

The core does not invent generic transaction/repository managers around every EF method.

An explicit transaction abstraction should be introduced only when a use case needs multiple save points or atomic operations not covered by one SaveChanges.

---

# 7. Why specifications are in the core

Specifications allow Application-owned query intent without referencing EF Core:

- criteria;
- includes;
- order;
- paging;
- tracking mode.

They are useful for reusable read shapes. Product-specific repository methods remain appropriate when a query name itself is domain vocabulary or when specification plumbing would obscure meaning.

The repository does not require every query to be a specification.

---

# 8. Why FoundationKit is package-separated

Five packages let a product reference only what it needs:

- Domain product does not pull EF/ASP.NET;
- API-only product can use WebApi conventions;
- Blazor app can use browser transport without server packages;
- Infrastructure provider choice remains in product.

Trade-off: version coordination among internal packages. Current version is aligned at `0.1.0` and packed together.

---

# 9. Why FoundationKit is provider-neutral

Decision: base EF Core only in core Infrastructure.

Benefits:

- SQL Server product can coexist with PostgreSQL/SQLite products;
- core package does not impose connection/provider settings;
- architecture test prevents accidental provider coupling.

Product selects SQL Server through `UseSqlServer` and owns migrations.

---

# 10. Why Result types coexist with exceptions

Expected use-case outcomes use `Result`:

- not found;
- conflict;
- validation;
- authorization-related application outcome;
- business rule.

Domain currently throws for invalid operation/argument, and handlers translate expected exceptions.

Unexpected programmer/infrastructure failures should remain exceptions for centralized handling.

Trade-off: mixed style requires discipline. A future domain policy may standardize coded DomainException or non-throwing domain methods, but current implementation is documented rather than falsely described as unified.

---

# 11. Why RFC 7807 ProblemDetails

Standard error structure gives:

- HTTP status;
- machine code;
- human detail;
- request instance;
- error category;
- correlation ID.

It is more extensible than `{ "error": "..." }`.

Current user/Identity endpoints still use simple error bodies in places. The browser parser supports both. Full API consistency is a future improvement.

---

# 12. Why correlation IDs

A browser error and server logs need a common identifier. The middleware accepts a bounded caller value or generates a GUID, places it in:

- trace identifier;
- response header;
- logging scope;
- ProblemDetails extension.

This is foundational observability. It is not a substitute for distributed tracing.

---

# 13. Why security headers are baseline only

Current headers prevent MIME sniffing, framing, referrer leakage, and selected browser permissions.

Not yet implemented centrally:

- Content Security Policy;
- cross-origin isolation headers;
- production reverse-proxy security policy;
- WAF;
- security reporting endpoint.

Final static-host/gateway headers must be evaluated separately from API middleware.

---

# 14. Why JWT and sessionStorage are current, not final identity architecture

Current approach is easy to understand and test:

- Identity validates credentials;
- API issues short-lived HS256 token;
- browser stores it for tab session;
- typed requests attach Bearer header;
- API validates every protected request.

Risks/limits:

- sessionStorage is readable by same-origin JavaScript under XSS;
- no refresh rotation;
- symmetric key rotation/management is manual;
- no MFA/external federation;
- browser parser does not verify signature, by design for UX only.

Production options include BFF with HttpOnly cookies or external OIDC provider. The repository does not claim the current local pattern is the final enterprise identity design.

---

# 15. Why automatic migrations are configurable

Automatic startup migration is convenient for local/isolated testing.

Production risk:

- multiple instances may race;
- schema change may need maintenance window;
- destructive operation may require approval/backfill;
- application identity may have excessive DDL permissions.

Therefore `Database:ApplyMigrationsOnStartup` is configurable, and production-readiness documentation recommends deployment-controlled migration.

---

# 16. Why pre-check uniqueness plus database indexes

Application pre-check provides friendly conflict codes.

Database unique index provides concurrency safety.

Neither replaces the other:

- pre-check alone races;
- database exception alone gives poor product error unless translated.

Current gap: provider-specific unique violation should be translated to the same typed conflict after a race.

---

# 17. Why the static portal remains separate

The root portal:

- is public and deployable on GitHub Pages;
- documents an external API without a running backend;
- includes offline playground/search/quality context;
- remains available even when the dynamic platform is not hosted.

The dynamic platform:

- manages users and versioned documents;
- requires API/database/application hosting;
- demonstrates FoundationKit.

Merging them would either make public docs depend on infrastructure or constrain the dynamic product to static hosting.

---

# 18. Why Blazor WebAssembly and MudBlazor

Blazor WebAssembly aligns with the .NET stack and shared typed C# Contracts. MudBlazor supplies consistent controls, layout, forms, feedback, and theming.

Boundaries prevent WebAssembly from becoming a trusted backend:

- API remains authority;
- browser never gets DbContext;
- role visibility is UX;
- typed clients centralize HTTP.

Trade-offs:

- initial download larger than simple JavaScript;
- WebAssembly debugging/build toolchain;
- tokens in browser;
- component state can grow if not separated.

---

# 19. Why typed API clients

Typed clients own:

- routes;
- JSON serialization;
- authenticated request creation;
- response parsing;
- error mapping.

Pages own input/render/navigation, not transport mechanics.

FoundationKit.Blazor extracts only reusable transport behavior; product clients retain product routes/contracts.

---

# 20. Why Admin and Client are separate applications

Benefits:

- public client does not download admin/auth features;
- deployment/access policy can differ;
- simpler navigation;
- public and privileged UX evolve independently;
- shared visual components still reuse through UI library.

Trade-off: two WebAssembly builds and some composition duplication.

---

# 21. Why providers are hosted once in App.razor

Mud popover/dialog/snackbar providers register named sections. Multiple instances caused duplicate-section runtime exceptions during layout transitions.

Decision: one provider set per application root, never in multiple layouts.

---

# 22. Why Postman remains alongside Swagger

Swagger:

- generated discovery;
- good for ad hoc local calls;
- follows endpoint metadata.

Postman:

- ordered workflow;
- environment variables;
- token/ID capture;
- portable to any REST tester/user;
- executable request reference.

Both are useful. Postman is the repeatable operational suite; Swagger is not removed.

---

# 23. Why Docker test topology uses one gateway

A single Nginx origin:

- mirrors subpath deployment;
- avoids test CORS complexity;
- verifies SPA base paths;
- provides one readiness endpoint;
- tests routing among four surfaces.

The API still supports direct localhost cross-origin development through explicit CORS origins.

---

# 24. Why CI has two workflows

Platform workflow proves product and full stack.

Foundation workflow proves reusable package integrity and packaging.

Separate path triggers reduce unnecessary expensive integration runs and make package failures visible.

Foundation workflow still builds whole solution to catch consumer compatibility.

---

# 25. Deliberate current non-features

Not added without proven need:

- microservices;
- message broker;
- outbox;
- generic endpoint generator;
- event sourcing;
- distributed cache;
- multi-tenancy;
- distributed locks;
- automatic mapping framework;
- mediator package;
- repository-per-table rule;
- database provider in core;
- universal manager layer.

These may become appropriate in a future product. They are not maturity badges.

---

# 26. Current technical debt and known limits

## Domain/Application

- limited aggregate test coverage;
- Create handler blank-input exception translation can be improved;
- no concurrency token;
- domain events infrastructure exists but product events are not used;
- no archive API;
- no admin list/details/edit use cases;
- no server pagination.

## Identity/API

- user endpoints bypass Application handler pattern;
- role replacement is not explicitly atomic;
- no refresh/MFA/password reset/account lifecycle;
- mixed ProblemDetails/simple error bodies;
- shared coarse rate limiter;
- no full validation pipeline.

## Persistence

- audit model without automatic complete audit capture;
- owner/author GUIDs lack explicit Identity foreign keys;
- startup migrations default true;
- unique-race exception translation absent;
- no rowversion.

## Frontend

- page-local state/orchestration in large Razor files;
- no role edit UI despite typed client method;
- no rich/sanitized Markdown renderer;
- no localization/RTL implementation;
- no component test suite;
- browser token storage risk.

## Operations

- no production secret manager integration in code;
- no centralized telemetry;
- no WAF/CSP;
- no SAST/dependency/penetration claim;
- no load/recovery/backup drills;
- Postman collection not executed by Newman in CI.

## Static portal

- data is manually curated;
- search is substring scoring, not semantic;
- service-worker cache invalidation requires care;
- external API uncertainties remain explicitly listed.

---

# 27. Production-readiness boundary

The repository is a production-oriented foundation, not production certification.

Before release, decide and verify:

- hosting topology;
- TLS/domains/reverse proxy;
- secret/key rotation;
- managed SQL and encrypted identity;
- backup/restore/retention;
- migration deployment/rollback;
- logs/metrics/traces/alerts;
- account lifecycle/MFA;
- audit retention;
- threat model and security tests;
- dependency/SAST scans;
- load and availability targets;
- incident response;
- RPO/RTO;
- privacy/data classification;
- content sanitization.

---

# 28. Rule for evolving FoundationKit

Move behavior to FoundationKit only when:

1. at least one real product needs it now;
2. the behavior is technical and product-independent;
3. its public contract is stable enough to version;
4. architecture tests can protect its dependencies;
5. product-specific terminology/configuration remains outside;
6. package consumers are considered;
7. documentation and tests are added.

Similar-looking files are not sufficient evidence. Premature extraction creates a rigid framework instead of a useful foundation.

---

# 29. Decision status vocabulary

Use these labels in future discussions:

- **Implemented**: source currently performs it.
- **Documented convention**: repository workflow requires it.
- **Prepared extension point**: core type exists but product does not use it fully.
- **Recommended hardening**: identified future improvement.
- **Not selected**: deliberately absent pending need.
- **Production decision required**: environment-specific choice cannot be safely hard-coded.

This vocabulary prevents AI conversations and project meetings from treating proposals as completed work.
