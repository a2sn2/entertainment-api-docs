# AI Handoff — Entertainment Docs and FoundationKit

Use this document as the initial context for a new AI conversation about this repository. It is intentionally self-contained and distinguishes implemented behavior from future work.

---

## 1. Repository identity

- **Repository:** `a2sn2/entertainment-api-docs`
- **Primary branch:** `main`
- **Reference baseline:** `5f9eb687860c72b076e2640645144dd8ecd6458c`
- **Static site:** <https://a2sn2.github.io/entertainment-api-docs/>
- **Primary platform solution:** `platform/EntertainmentDocs.sln`
- **.NET target:** `net8.0`
- **Reusable core version:** `FoundationKit 0.1.0`
- **Repository visibility:** public

The repository is not only an API documentation project. It contains:

1. a static, browser-only Entertainment Services API documentation portal; and
2. a reusable engineering core called FoundationKit, with EntertainmentDocs as its first working reference product.

---

## 2. Current implemented systems

### 2.1 Static portal

The repository root contains a GitHub Pages portal implemented with HTML, CSS, and native JavaScript modules. Its source uses a lightweight layered structure:

```text
src/domain
src/application
src/infrastructure
src/presentation
```

It documents observed Entertainment Services API behavior, including authentication, catalog, initial purchase, execution, state checking, request fields, response fields, test scenarios, known limitations, and open questions.

Some external contract spellings are intentionally preserved because they match the observed API, including:

```text
massage
statues
feilds
feildName
faild
```

Do not silently correct these names in external request examples unless the upstream contract changes.

### 2.2 Dynamic platform

The `platform/` directory contains a 15-project .NET solution with:

- a reusable FoundationKit core;
- product Domain, Application, Contracts, Infrastructure, and API projects;
- Blazor WebAssembly Admin and Client applications;
- a shared MudBlazor UI project;
- unit and architecture tests;
- SQL Server EF Core migrations;
- Postman assets;
- Docker/Nginx integration stack;
- GitHub Actions workflows.

---

## 3. FoundationKit projects

```text
platform/core/
├── FoundationKit.Domain
├── FoundationKit.Application
├── FoundationKit.Infrastructure
├── FoundationKit.WebApi
└── FoundationKit.Blazor
```

### FoundationKit.Domain

Implemented reusable primitives:

- `Entity<TId>` with identity equality;
- safe transient-entity equality behavior;
- `AggregateRoot<TId>`;
- `ValueObject` component equality;
- `IDomainEvent`;
- `IHasDomainEvents`;
- `DomainException`.

It has no EF Core, ASP.NET Core, Blazor, SQL Server, or product dependencies.

### FoundationKit.Application

Implemented reusable application concepts:

- `Result` and `Result<T>`;
- typed `Error` and `ErrorType`;
- command/query marker interfaces and handlers;
- repository read/write ports;
- specification abstractions;
- pagination models;
- `IUnitOfWork`;
- `IClock`;
- `ICurrentUser`;
- validation abstractions;
- domain-event handler/dispatcher contracts.

### FoundationKit.Infrastructure

Implemented provider-neutral adapters:

- `EfRepository<TEntity,TId,TDbContext>`;
- `EfUnitOfWork<TDbContext>`;
- `SpecificationEvaluator`;
- `DomainEventDispatcher`;
- `DomainEventsSaveChangesInterceptor`;
- DI registration.

It references base EF Core but does not select SQL Server, PostgreSQL, or SQLite.

### FoundationKit.WebApi

Implemented common HTTP behavior:

- `Result` to HTTP mapping;
- RFC 7807 `ProblemDetails`;
- correlation IDs;
- baseline security headers;
- reusable request-pipeline registration.

Error mapping:

```text
Validation    -> 400
Unauthorized  -> 401
Forbidden     -> 403
NotFound      -> 404
Conflict      -> 409
BusinessRule  -> 422
Failure       -> 500
```

### FoundationKit.Blazor

Implemented browser transport/state behavior:

- `ApiClientBase`;
- `ApiResult` and `ApiResult<T>`;
- `ApiError`;
- API error/ProblemDetails parser;
- network and timeout handling;
- `AsyncState<T>`.

---

## 4. Dependency rules

The intended direction is inward:

```text
Product.Domain
      ↑
Product.Application
      ↑
Product.Infrastructure
      ↑
Product.Api
```

Frontend direction:

```text
Product.Admin / Product.Client
       ├── Product.Contracts
       ├── Product.Ui
       └── FoundationKit.Blazor
```

Architecture tests enforce that:

- FoundationKit.Domain has no outer-layer dependencies;
- FoundationKit.Infrastructure is database-provider and web-host agnostic;
- product Domain has no Application, Infrastructure, API, EF Core, or ASP.NET Core dependency;
- product Application has no Infrastructure or API dependency;
- Contracts do not reference product Domain/Application/Infrastructure;
- product Infrastructure does not reference API or frontend projects;
- FoundationKit.Application does not reference outer adapters.

---

## 5. EntertainmentDocs product behavior

### 5.1 Document aggregate

`DocumentationDocument` is the aggregate root. It owns a private collection of `DocumentVersion` children.

Statuses:

```text
Draft
InReview
Published
Archived
```

Implemented rules:

- a new document begins in `Draft`;
- required strings are trimmed and rejected when blank;
- an archived document cannot receive a new version;
- a published document returns to `Draft` when a new version is added;
- a document needs at least one version before review;
- only `Draft` can become `InReview`;
- only `InReview` can become `Published`;
- archive changes the state to `Archived`.

### 5.2 Explicit use cases

Commands:

- `CreateDocumentCommand`
- `AddDocumentVersionCommand`
- `SubmitDocumentForReviewCommand`
- `PublishDocumentCommand`

Queries:

- `ListPublishedDocumentsQuery`
- `GetPublishedDocumentQuery`

Each use case has a dedicated handler. There is no single large generic document manager.

### 5.3 Hybrid repository

`IDocumentRepository` extends the generic FoundationKit repository and adds product-language operations:

- reference uniqueness check;
- slug uniqueness check;
- load by ID with versions;
- load published document by slug;
- list published documents;
- explicitly persist a new version.

This is intentionally hybrid: common persistence behavior is generic, while domain-specific queries remain explicit.

---

## 6. Database and persistence

### Local development

```text
Provider: Microsoft SQL Server
Database: EntertainmentDocs_Dev
Authentication: Windows Authentication
ORM: Entity Framework Core 8
```

Schema source of truth:

```text
platform/src/EntertainmentDocs.Infrastructure/Persistence/Migrations/
```

Current migration:

```text
20260805113706_InitialSqlServerSchema
```

Current product tables include:

- ASP.NET Core Identity tables;
- `documentation_documents`;
- `documentation_versions`;
- `audit_entries`;
- `__EFMigrationsHistory`.

`FoundationKit.Infrastructure` is provider-neutral. SQL Server is selected only in `EntertainmentDocs.Infrastructure`.

Do not manually change production schema in SSMS without a corresponding reviewed EF Core migration.

---

## 7. Authentication and authorization

Backend authentication uses ASP.NET Core Identity and JWT Bearer tokens.

Roles:

```text
Administrator
Editor
Reviewer
Reader
```

Policies:

```text
ManageContent  -> Administrator or Editor
PublishContent -> Administrator or Reviewer
ManageUsers    -> Administrator
```

JWT implementation currently creates HS256 tokens containing issuer, audience, subject, JWT ID, issue/not-before/expiry times, name identifier, display name, email, and role claims.

The Admin browser stores the access token in `sessionStorage`. Browser-side claim parsing is only for UI state. The API validates the token cryptographically and remains the actual security boundary.

---

## 8. Current API surface

Public:

```text
GET  /
GET  /health
POST /api/v1/auth/login
GET  /api/v1/documents
GET  /api/v1/documents/{slug}
```

Protected content:

```text
POST /api/v1/admin/documents
POST /api/v1/admin/documents/{id}/versions
POST /api/v1/admin/documents/{id}/submit-review
POST /api/v1/admin/documents/{id}/publish
```

Protected user administration:

```text
GET  /api/v1/admin/users
POST /api/v1/admin/users
PUT  /api/v1/admin/users/{id}/roles
```

Current local URLs:

```text
API Swagger  http://localhost:5080/swagger
API health   http://localhost:5080/health
Client       http://localhost:5081
Admin        http://localhost:5082/login
```

Development-only administrator:

```text
Email:    admin@local.test
Password: LocalAdmin!2026
```

Testing-only administrator:

```text
Email:    admin@test.local
Password: TestAdmin!2026
```

These credentials are not production credentials and must never be reused outside their isolated environments.

---

## 9. API request bodies

Login:

```json
{
  "email": "admin@local.test",
  "password": "LocalAdmin!2026"
}
```

Create document:

```json
{
  "reference": "API-ENT-DOC-001",
  "slug": "purchase-guide",
  "title": "Purchase API Integration Guide"
}
```

Add version:

```json
{
  "version": "1.0.0",
  "content": "# Purchase API\n\nComplete documentation content."
}
```

Create user:

```json
{
  "email": "editor@local.test",
  "displayName": "Documentation Editor",
  "temporaryPassword": "TempEditor!2026",
  "roles": ["Editor", "Reader"]
}
```

Replace roles:

```json
{
  "roles": ["Reviewer", "Reader"]
}
```

Submit-review and publish requests have no body.

Postman files:

```text
platform/postman/EntertainmentDocs.postman_collection.json
platform/postman/EntertainmentDocs.Local.postman_environment.json
```

---

## 10. Frontend architecture

The product has:

- `EntertainmentDocs.Admin` — authenticated administration;
- `EntertainmentDocs.Client` — public published-document browser;
- `EntertainmentDocs.Ui` — shared MudBlazor theme and reusable visual states.

Admin capabilities currently include:

- login/logout;
- dashboard;
- create document;
- add version;
- submit for review;
- publish;
- list and create users;
- display API request examples.

Client capabilities currently include:

- list published documents;
- client-side search by title/reference/slug;
- open document by slug;
- show latest published version content.

Frontends use product Contracts and typed API clients. They do not reference EF Core, SQL Server, product Infrastructure, or product Domain.

---

## 11. Local execution

From repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\platform\scripts\setup-local-sqlserver.ps1
```

Open `platform/EntertainmentDocs.sln`, configure multiple startup projects:

```text
EntertainmentDocs.Api    Start
EntertainmentDocs.Client Start
EntertainmentDocs.Admin  Start
```

After F5 and service startup:

```powershell
powershell -ExecutionPolicy Bypass -File .\platform\scripts\open-local-platform.ps1
```

Package FoundationKit:

```powershell
powershell -ExecutionPolicy Bypass -File .\platform\scripts\pack-foundation.ps1
```

Packages are produced under:

```text
platform/artifacts/foundation/
```

---

## 12. Full-stack test topology

Docker Compose test stack:

```text
Nginx gateway :8080
├── /api/    -> ASP.NET Core API
├── /admin/  -> Admin Blazor/Nginx
├── /client/ -> Client Blazor/Nginx
└── /docs/   -> static portal/Nginx

API -> SQL Server container
```

Smoke test validates:

1. gateway;
2. API health and database connectivity;
3. Admin, Client, and static portal availability;
4. Identity login and JWT issuance;
5. protected document creation;
6. version creation;
7. review submission;
8. publication;
9. public retrieval.

---

## 13. CI workflows

`platform-ci.yml`:

- validates Postman JSON;
- restores, builds in Release, and tests the solution;
- builds the Docker test stack;
- waits for health;
- runs the full workflow smoke test;
- uploads test results;
- removes containers and volumes.

`foundationkit-ci.yml`:

- restores and builds;
- runs FoundationKit unit and architecture tests;
- packs all FoundationKit projects;
- uploads `.nupkg` and `.snupkg` artifacts.

---

## 14. How to add a product capability

Use this sequence:

```text
Domain aggregate/entity/value object or policy
        -> command/query
        -> dedicated handler
        -> repository port when persistence is needed
        -> infrastructure adapter
        -> transport request/response contract
        -> thin API endpoint
        -> typed frontend client
        -> feature UI/state
        -> Postman request
        -> tests and migration when required
```

Do not introduce:

- automatic generic CRUD controllers;
- generic business managers that hide use-case intent;
- direct SQL or DbContext usage in frontend or Application;
- Domain entities as HTTP contracts;
- a microservice merely to create a folder boundary.

---

## 15. Important implemented security baseline

- JWT issuer, audience, lifetime, and signature validation;
- minimum signing-key length check at startup;
- Identity password length/digit/uppercase requirements;
- account lockout threshold;
- role policies;
- fixed-window API rate limiting;
- CORS that is permissive only in Development/Testing and fail-closed elsewhere;
- HSTS outside Development/Testing;
- HTTPS redirection outside Testing;
- correlation IDs;
- security response headers;
- Swagger limited to Development/Testing;
- public repository secret warnings.

---

## 16. Known gaps and cautions

The repository is production-oriented, not production-certified.

Not yet claimed complete:

- refresh-token rotation or external identity provider;
- MFA, password-reset, and complete account lifecycle;
- production-grade audit interception and immutable retention;
- structured centralized logs, tracing, metrics, and alerting;
- deployment-managed migration policy;
- WAF and production domain/TLS configuration;
- threat model, SAST, dependency scanning, penetration testing;
- load, recovery, backup, RPO/RTO, rollback, and incident-response validation;
- server-side pagination and advanced product features;
- automatic Markdown rendering/sanitization policy for document content.

The `AuditEntry` persistence model exists, but its presence must not be mistaken for a complete audit system.

---

## 17. Guidance for future AI work

Before proposing a code change:

1. inspect the current file on `main` rather than assuming this handoff is the latest revision;
2. identify which layer owns the change;
3. preserve dependency direction;
4. distinguish reusable FoundationKit behavior from product behavior;
5. update Contracts, API, frontend client, Postman, docs, and tests together when an HTTP shape changes;
6. use EF Core migrations for schema changes;
7. work through a feature branch and PR;
8. do not merge while required checks fail;
9. do not reveal or invent production credentials;
10. state clearly whether a recommendation is implemented behavior, inferred intent, or future work.

For full detail, start at [`README.md`](README.md) in this directory.
