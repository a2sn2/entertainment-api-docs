# End-to-End Runtime Walkthroughs

This chapter follows actual execution paths across layers. It answers “what runs next?” and “why does this type exist?” more directly than a folder-by-folder view.

---

# 1. API process startup

```text
Operating system / dotnet host
        ↓
EntertainmentDocs.Api Program.cs
        ↓
Configuration and DI registration
        ↓
Middleware and endpoints
        ↓
Database migration decision
        ↓
Identity role/admin seed
        ↓
Kestrel accepts requests
```

Detailed sequence:

1. .NET loads appsettings, environment-specific JSON, environment variables, and command-line values.
2. `WebApplication.CreateBuilder` creates host services.
3. Application handlers are registered.
4. Infrastructure validates SQL connection and registers DbContext, Identity, repositories, clock, and token service.
5. FoundationKit registers ProblemDetails, domain-event components, and reusable middleware.
6. Health, Swagger discovery, CORS, JWT, policies, and rate limiting are configured.
7. `builder.Build()` freezes the service graph into the application host.
8. Middleware order is assembled.
9. Endpoint groups are mapped.
10. A startup scope resolves DbContext.
11. Migrations run when configured.
12. Identity roles and optional bootstrap admin are created.
13. `app.Run()` starts the request loop.

A failure in connection string, signing key, production origins, migrations, or admin password validation stops startup rather than exposing a partially configured service.

---

# 2. Local multi-project startup

```text
Visual Studio F5
├── API    :5080
├── Client :5081
└── Admin  :5082
```

Launch profiles intentionally set `launchBrowser=false`. Visual Studio owns the processes, while `open-local-platform.ps1` waits for readiness and opens ordinary browser tabs. Closing a tab therefore does not terminate all debug processes.

The browser downloads WebAssembly applications from 5081/5082. Their Development appsettings point HTTP clients to the API at 5080.

---

# 3. Admin application boot

```text
Admin index.html
    ↓
Blazor WebAssembly runtime
    ↓
Admin Program.cs
    ↓
App.razor
    ↓
AuthenticationStateProvider restores token
    ↓
Router chooses page
```

1. Browser loads host document, CSS, MudBlazor assets, and Blazor runtime.
2. WebAssembly downloads .NET assemblies.
3. Program resolves API base and registers clients/services.
4. `App.razor` creates Mud providers and authentication cascade.
5. Router discovers route components.
6. For a protected route, AuthorizeRouteView asks the custom state provider.
7. Provider reads token from sessionStorage.
8. Missing/malformed/expired token becomes anonymous and may be removed.
9. Anonymous protected route renders RedirectToLogin.
10. Redirect component preserves a local encoded return path.

---

# 4. Login request

```text
Login.razor
    ↓ LoginRequest
AuthenticationApiClient
    ↓ POST /api/v1/auth/login
AuthEndpoints
    ↓ UserManager / PasswordHasher
JwtTokenService
    ↓ LoginResponse
AuthenticationStateProvider
    ↓ sessionStorage + ClaimsPrincipal
Router / protected UI
```

## Browser side

1. User enters email/password.
2. MudForm validates required fields.
3. Submit button enters busy state.
4. Page constructs immutable `LoginRequest`.
5. Typed client creates JSON POST.
6. FoundationKit ApiClientBase sends and parses response.

## API side

7. Rate limiter evaluates request.
8. Endpoint loads user by email.
9. It checks active state and hashed password.
10. It loads role names.
11. Token service creates signed HS256 JWT.
12. Endpoint returns token, safe user summary, roles.

## Browser completion

13. Typed client validates token presence.
14. Authentication provider writes sessionStorage.
15. It parses payload into UI ClaimsPrincipal.
16. It notifies Blazor authentication state.
17. Login page validates ReturnUrl is local.
18. Navigation replaces login history entry.

Failure at step 8–9 returns 401 without revealing account existence.

---

# 5. Protected request authorization

```text
Typed client
    ↓ AuthenticatedRequestFactory
sessionStorage token
    ↓ Authorization header
API authentication middleware
    ↓ signature/issuer/audience/lifetime validation
ClaimsPrincipal
    ↓ policy middleware
Endpoint or 401/403
```

Distinction:

- no valid authenticated principal -> 401;
- valid principal lacking required role -> 403;
- browser showing a button does not influence the API decision.

---

# 6. Create document workflow

```text
Documents.razor
    ↓ CreateDocumentRequest
DocumentsApiClient
    ↓ HTTP POST
AdminDocumentEndpoints
    ↓ CreateDocumentCommand
CreateDocumentCommandHandler
    ├── ICurrentUser
    ├── IDocumentRepository uniqueness checks
    ├── IClock
    └── DocumentationDocument.Create
             ↓
DocumentRepository.AddAsync
             ↓
AppDbContext.SaveChangesAsync
             ↓ SQL INSERT
201 Created + ID
```

Detailed behavior:

1. Browser form validates nonempty fields.
2. Typed client attaches token and JSON.
3. API authenticates and applies ManageContent policy.
4. Endpoint maps transport request to command.
5. Handler requires authenticated GUID.
6. Repository issues SQL existence checks for reference and slug.
7. Handler calls Domain factory.
8. Domain trims values, lowercases slug, sets Draft and timestamps.
9. Generic repository tracks aggregate.
10. Unit of work saves.
11. EF converts entity/configuration model to parameterized INSERT.
12. Unique indexes enforce final consistency.
13. Endpoint maps success ID to 201 and response body.
14. Browser stores the working ID and shows snackbar.

No SQL or workflow rule exists in Razor.

---

# 7. Add version workflow

```text
Browser version form
    ↓ AddDocumentVersionRequest
POST /admin/documents/{id}/versions
    ↓ AddDocumentVersionCommand
Handler loads aggregate + versions tracked
    ↓ DocumentationDocument.AddVersion
new DocumentVersion child
    ↓ repository attaches child
SaveChanges
    ↓ INSERT version + possible UPDATE document state/time
response version ID
```

Important behavior:

- archived aggregate throws business-rule exception;
- blank version/content throws validation exception;
- published aggregate becomes Draft;
- database prevents duplicate `(DocumentId, Version)`.

The child constructor is internal, so ordinary external code cannot create a detached version outside aggregate intent.

---

# 8. Submit for review

```text
POST /{id}/submit-review
    ↓ ManageContent policy
SubmitDocumentForReviewCommandHandler
    ↓ load aggregate with versions
aggregate.SubmitForReview
    ↓ require Draft + at least one version
SaveChanges
    ↓ UPDATE status/time
204 No Content
```

Invalid state becomes typed BusinessRule -> HTTP 422.

---

# 9. Publish

```text
POST /{id}/publish
    ↓ PublishContent policy
PublishDocumentCommandHandler
    ↓ aggregate.Publish
require InReview
    ↓ set Published/PublishedAt/UpdatedAt
SaveChanges
    ↓ SQL UPDATE
204
```

Administrator or Reviewer can publish. Editor alone cannot.

---

# 10. Public list

```text
Client Index.razor initializes
    ↓ DocumentationApiClient.ListAsync
GET /api/v1/documents
    ↓ ListPublishedDocumentsQueryHandler
DocumentRepository.ListPublishedAsync
    ↓ SQL SELECT Published ORDER BY Title, no tracking
Application DTOs
    ↓ API response contracts
Client cards
```

No Bearer token required.

The Client then filters loaded cards locally by title/reference/slug. Search does not query SQL per keystroke.

---

# 11. Public details

```text
Browser route /documents/{slug}
    ↓ OnParametersSetAsync
DocumentationApiClient.GetBySlugAsync
    ↓ URL-escaped GET
GetPublishedDocumentQueryHandler
    ↓ repository: Published + slug, include versions, no tracking
latest version by CreatedAt
    ↓ DocumentDetailsResponse
preformatted content display
```

Not found produces ProblemDetails, which FoundationKit.Blazor converts into ApiError and ErrorState.

Current content is shown as text in `<pre>`, not rendered HTML/Markdown.

---

# 12. Create user

```text
Users.razor
    ↓ CreateUserRequest
UsersApiClient
    ↓ POST /admin/users
ManageUsers policy
    ↓ role validation
UserManager.CreateAsync
    ↓ password validation/hash + SQL Identity insert
AddToRolesAsync
    ↓ Identity join rows
201 + user ID
```

The endpoint currently coordinates Identity directly rather than through Application command/handler.

Potential partial state: user insert can succeed before role assignment fails. This is a documented hardening area.

---

# 13. Replace roles

```text
PUT /admin/users/{id}/roles
    ↓ find user
validate complete target role set
    ↓ remove current roles
add requested roles
    ↓ 204
```

The body describes final state, not incremental additions. An empty array removes all roles.

The current remove-then-add sequence is not explicitly wrapped in a transaction, so role-add failure after removal is a known atomicity risk.

---

# 14. Result-to-error flow

```text
Domain exception / expected condition
    ↓ handler translation
FoundationKit Error + Result failure
    ↓ endpoint ToHttpResult
HTTP ProblemDetails
    ↓ correlation middleware/header
FoundationKit.Blazor ApiResponseReader
    ↓ ApiError
Razor ErrorState / Snackbar
```

Stable machine code survives the round trip. Human-readable descriptions are shown to user. Correlation ID can connect browser failure to server logs.

Unexpected exceptions bypass expected Result mapping and reach centralized exception handler.

---

# 15. EF save and domain events

```text
Aggregate raises event
    ↓ pending list
SaveChangesAsync starts
    ↓ interceptor captures events
SQL transaction/save succeeds
    ↓ SavedChangesAsync
DomainEventDispatcher resolves handlers
    ↓ invokes sequentially
aggregate event list cleared
```

Current document aggregate does not raise specific events yet. The infrastructure path is implemented and tested by architecture, not by a product event workflow.

Failure before save completion clears interceptor pending state and dispatches nothing.

This is in-process post-save delivery, not durable outbox delivery.

---

# 16. Local SQL setup execution

```text
PowerShell script
    ↓ validate server
set environment connection
    ↓ dotnet tool restore
restore solution
    ↓ build
EF database update
    ↓ design-time factory/API startup project
SQL Server creates/updates EntertainmentDocs_Dev
```

`__EFMigrationsHistory` records applied migration. Re-running is idempotent when no new migration exists.

---

# 17. Docker full-stack startup

```text
Docker Compose
    ↓ SQL Server starts and becomes healthy
API builds/starts, migrates/seeds, becomes healthy
    ↓ Admin/Client/Docs Nginx start
Gateway waits for all
    ↓ host port 8080 ready
```

Internal DNS uses service names such as `sqlserver` and `api`. Only gateway and SQL test port are published to host.

---

# 18. Smoke test runtime

```text
probe gateway/API/apps/docs
    ↓ login
create document
    ↓ add version
submit review
    ↓ publish
public GET by slug
    ↓ assert title
```

Unique timestamp data avoids collisions. Any non-2xx curl response terminates the strict script.

---

# 19. FoundationKit package build

```text
pack script finds core project files
    ↓ dotnet pack Release per project
MSBuild imports common/core props
    ↓ compile/package/symbols
artifacts/foundation/*.nupkg + *.snupkg
```

Product projects currently reference source projects in the same solution. Future repositories can consume the packages through an internal feed or local source.

---

# 20. Static portal page load

```text
GitHub Pages HTML shell
    ↓ CSS + main.js
read data-page/data-root
    ↓ load static repository and theme preference
render shared shell
    ↓ choose page renderer
build command-palette search index
    ↓ attach interactions/page initializer
register service worker
```

No call to dynamic API occurs in this path.

---

# 21. Failure diagnosis map

| Symptom | First boundary to inspect |
|---|---|
| API process will not start | Program configuration validation, SQL, signing key, CORS origins |
| `/health` unhealthy | SQL service/connection/migration |
| Admin redirects repeatedly | session token, expiry, JWT claim parsing, API login |
| Admin UI shows action but API returns 403 | token roles and backend policy; UI is not authority |
| Document create 409 | duplicate reference/slug or SQL uniqueness race |
| Review/publish 422 | aggregate current state/version requirement |
| Client empty | no Published documents or list error/filter |
| Direct SPA route 404 in Docker | Nginx `try_files`/base path |
| Duplicate Mud provider exception | provider repeated outside app root |
| Constant Blazor error banner | host CSS for `#blazor-error-ui` |
| Compose gateway timeout | inspect service health and logs |
| Package script finds zero projects | path/pattern/core project naming |
| Static portal stale | service-worker cache version/activation |

---

# 22. Invariants across all flows

- request contracts are transport-only;
- use cases own orchestration;
- aggregate owns document state validity;
- repository owns persistence shape, not business decisions;
- one scoped DbContext commits use-case changes;
- API owns authentication and authorization;
- browser state is untrusted UX state;
- errors retain stable codes and correlation;
- migrations own schema;
- CI reproduces a clean environment.
