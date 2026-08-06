# ASP.NET Core API and Security Boundary

This chapter explains `EntertainmentDocs.Api`, including service composition, middleware order, authentication, authorization, rate limiting, CORS, endpoints, response mapping, and environment configuration.

---

# 1. API project role

`EntertainmentDocs.Api` is the HTTP host and the real external security boundary. It references:

- Product Application;
- Product Contracts;
- Product Infrastructure;
- FoundationKit.WebApi;
- JWT Bearer authentication;
- EF design support;
- EF health checks;
- Swagger/OpenAPI tooling.

It must adapt HTTP to use cases rather than contain document state rules or SQL queries.

---

# 2. `Program.cs` execution order

The file uses top-level statements. Execution proceeds from top to bottom during host startup.

## 2.1 Imports

Imports cover:

- text encoding for signing-key bytes;
- rate limiting;
- endpoint and policy modules;
- Application and Infrastructure DI;
- current-user adapter;
- FoundationKit WebApi extensions;
- authentication, EF Core, and token validation.

Imports do not instantiate behavior; they make types available.

## 2.2 Builder creation

```csharp
var builder = WebApplication.CreateBuilder(args);
```

Creates configuration, logging, environment, DI container, and web-host defaults from command-line arguments, JSON, environment variables, and host sources.

## 2.3 Layer registration

```csharp
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFoundationWebApi();
```

Order communicates composition:

- handlers;
- concrete database/Identity/repository adapters;
- common HTTP behavior.

`AddHttpContextAccessor` enables scoped services to inspect the current request without injecting `HttpContext` into Application.

`ICurrentUser` is bound to `HttpCurrentUser`.

## 2.4 Health checks

```csharp
AddHealthChecks().AddDbContextCheck<AppDbContext>()
```

The `/health` endpoint verifies that the context can reach its database. It is more meaningful than a process-only “alive” check, but it does not validate every downstream dependency or business function.

## 2.5 Endpoint metadata and Swagger

- `AddEndpointsApiExplorer`: discovers Minimal API metadata.
- `AddSwaggerGen`: produces OpenAPI document and UI support.

Swagger middleware is enabled only in Development and Testing later.

---

# 3. CORS policy

Policy name: `WebClients`.

## Configured origins

When `AllowedOrigins` contains entries:

```csharp
policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
```

Only exact configured browser origins may call cross-origin. Any header/method is allowed within those origins.

## Development/Testing fallback

When no origins are configured in Development or Testing, the API allows any origin/header/method. This supports isolated local and CI execution.

## Non-development fail-closed behavior

When no origins are configured outside Development/Testing, startup throws. This prevents accidental production deployment with an open origin policy.

CORS affects browsers only. It is not authentication, authorization, or a defense against Postman/curl.

---

# 4. JWT validation configuration

The API binds `JwtOptions` and validates signing-key presence/length before registering authentication.

```text
Minimum current signing-key length: 32 characters
```

`AddAuthentication(JwtBearerDefaults.AuthenticationScheme)` sets Bearer as default authentication scheme.

Token validation parameters require:

- valid issuer;
- valid audience;
- unexpired lifetime;
- valid signing key;
- configured issuer and audience;
- symmetric key from UTF-8 signing-key bytes;
- 30-second clock skew.

Clock skew tolerates small machine-time differences around not-before/expiry boundaries.

The API validates cryptographic signature. The Admin browser claim parser does not.

---

# 5. Authorization policies

## `Authorization/Policies.cs`

Defines stable policy names using `nameof`:

```text
ManageContent
PublishContent
ManageUsers
```

## Policy registration

```text
ManageContent  -> Administrator OR Editor
PublishContent -> Administrator OR Reviewer
ManageUsers    -> Administrator
```

A role list in `RequireRole` is OR semantics.

Policy names decouple endpoint intent from role strings. Roles can evolve without changing every route attribute when policy meaning remains stable.

---

# 6. Rate limiting

Policy name: `api`.

Fixed window:

```text
Permit limit: 120
Window: 1 minute
Queue limit: 0
```

Requests over limit are rejected rather than queued.

Current policy is shared rather than partitioned per user/IP/endpoint. It provides a baseline against accidental bursts but is not a complete abuse or distributed-rate-limit strategy.

Endpoint groups opt into the policy through `RequireRateLimiting("api")`.

---

# 7. Middleware pipeline order

After `builder.Build()`:

```text
Foundation correlation ID
Foundation security headers
Exception handler
HSTS outside Development/Testing
HTTPS redirection outside Testing
CORS
Rate limiter
Authentication
Authorization
Endpoints
```

Order matters:

- correlation ID exists for downstream errors/logs;
- security headers apply broadly;
- exception handling wraps later failures;
- CORS executes before endpoint completion;
- authentication builds the principal before authorization evaluates it;
- endpoints run only after policy middleware.

Testing skips HTTPS redirection because the container gateway uses internal HTTP.

---

# 8. Swagger environment boundary

Swagger/OpenAPI UI is available only when:

```text
Development OR Testing
```

Production does not expose it by current code. Postman assets remain repository-controlled executable documentation.

---

# 9. Platform endpoints

## `GET /`

Returns anonymous service metadata:

```json
{
  "service": "EntertainmentDocs.Api",
  "environment": "Development",
  "databaseProvider": "Microsoft SQL Server",
  "status": "running"
}
```

Purpose: simple discovery/diagnostic metadata. It should not expose secrets or connection details.

## `GET /health`

Maps health checks and includes DbContext connectivity.

---

# 10. Startup database and Identity actions

The API creates an async scope and resolves `AppDbContext`.

Configuration:

```text
Database:ApplyMigrationsOnStartup
```

- when true: `Database.MigrateAsync()` applies committed migrations;
- when false in Testing: `EnsureCreatedAsync()` may create schema directly.

Then `IdentitySeeder.SeedAsync` ensures roles and optional bootstrap administrator.

Production guidance is to move migration application to controlled deployment unless explicitly approved for startup.

`public partial class Program;` provides a concrete type hook commonly needed by integration-test host factories and tooling.

---

# 11. Current-user adapter

## `Services/HttpCurrentUser.cs`

Receives `IHttpContextAccessor` and exposes a private current `ClaimsPrincipal`.

Properties:

- `IsAuthenticated`: checks identity flag;
- `UserId`: reads NameIdentifier claim and parses GUID;
- `Email`: reads email claim;
- `IsInRole`: delegates to ClaimsPrincipal.

Application handlers depend on product `ICurrentUser`, never `HttpContext`.

When no active request/principal/valid ID exists, properties return false or null rather than throw.

---

# 12. Authentication endpoint

## `POST /api/v1/auth/login`

Defined in `AuthEndpoints.cs` under group:

```text
/api/v1/auth
```

Tagged `Authentication` and rate limited. It is anonymous because no group authorization is required.

### Input

`LoginRequest` with email and password.

### Execution

1. find Identity user by email;
2. reject when absent;
3. reject when `IsActive` is false;
4. verify password through Identity's hasher;
5. load roles;
6. create access token;
7. return token, safe user summary, and roles.

### Failure

Returns `401 Unauthorized` with no password/account-detail distinction. This reduces account-enumeration information.

### Metadata

OpenAPI documents 200 `LoginResponse` and 401.

### Current security limitations

- no refresh token;
- no MFA;
- no password reset flow;
- no explicit email verification workflow beyond seeded confirmed email;
- no external identity provider;
- login endpoint uses shared fixed-window policy, not account/IP-specific throttling.

---

# 13. Public document endpoints

## Group

```text
/api/v1/documents
```

Tagged `Documents` and rate limited, no authentication required.

## `GET /api/v1/documents`

Injects:

```text
IQueryHandler<ListPublishedDocumentsQuery, IReadOnlyList<DocumentSummaryDto>>
```

Flow:

1. construct parameterless query;
2. execute handler;
3. map Application DTOs to `DocumentSummaryResponse` records;
4. return 200;
5. map failure through FoundationKit if one occurs.

Current query normally returns success and empty array when no documents exist.

## `GET /api/v1/documents/{slug}`

Injects details query handler.

Flow:

1. receive route slug;
2. execute query;
3. map DTO to `DocumentDetailsResponse`;
4. success 200;
5. not found -> RFC 7807 404.

The repository lowercases slug during lookup; route input is not treated as SQL text because EF parameterizes the expression.

---

# 14. Admin document endpoints

## Group

```text
/api/v1/admin/documents
```

The group requires authentication, rate limiting, and tag `Admin Documents`. Individual routes add policies.

## `POST /api/v1/admin/documents`

Policy: ManageContent.

Input: `CreateDocumentRequest`.

Mapping:

```text
Request -> CreateDocumentCommand -> handler -> Result<Guid>
```

Success maps to:

- `201 Created`;
- location `/api/v1/admin/documents/{id}`;
- `CreatedDocumentResponse` body.

The location currently points to a conceptual resource route; no GET admin-by-ID endpoint is mapped yet.

Documented errors:

- 400 validation;
- 409 conflict;
- 401;
- 403.

## `POST /api/v1/admin/documents/{id}/versions`

Policy: ManageContent.

Input: GUID route ID and `AddDocumentVersionRequest`.

Success returns 200 with `CreatedDocumentVersionResponse`. Although creation could use 201, current contract uses 200.

Documented errors include 400, 404, 422, 401, 403.

## `POST /api/v1/admin/documents/{id}/submit-review`

Policy: ManageContent.

No body. Success uses FoundationKit default `204 No Content`.

## `POST /api/v1/admin/documents/{id}/publish`

Policy: PublishContent.

No body. Success 204. Invalid aggregate state maps to 422.

### Thin endpoint principle

The endpoint files do not load DbContext, mutate entities, or decide state transitions. They adapt route/body/auth to a handler and map its result.

---

# 15. Admin user endpoints

## Group

```text
/api/v1/admin/users
```

Entire group requires ManageUsers (Administrator), rate limiting, and tag `Admin Users`.

These endpoints currently use Identity directly in the API rather than Application command handlers. That is a pragmatic current implementation, but it is less consistent with the document use-case pattern.

## `GET /api/v1/admin/users`

1. query Identity users with no tracking;
2. order by display name;
3. materialize users;
4. for each user, query assigned roles;
5. build response list;
6. return 200.

Potential scalability issue: role loading is one additional operation per user. Server pagination and a joined/projection query may be needed at scale.

## `POST /api/v1/admin/users`

### Role normalization

- null roles -> empty array;
- remove blank values;
- exact-case distinct values;
- compare against supported roles.

Invalid roles return 400 with `ApiErrorResponse`.

### User creation

Creates GUID, user name/email, display name, and confirmed-email flag. Identity validates uniqueness/password and hashes password.

On Identity failure, descriptions are joined and returned as 400 simple error shape.

Roles are added when requested.

Success: 201 with location and created ID.

### Consistency caution

If user creation succeeds but role assignment fails, current endpoint does not roll back user creation. A future Application use case/transactional policy should define compensation.

## `PUT /api/v1/admin/users/{id}/roles`

Replacement semantics:

1. find user;
2. 404 if absent;
3. normalize/validate requested roles;
4. get current roles;
5. remove all current roles;
6. add requested roles;
7. return 204.

Potential partial-failure risk: removal can succeed and addition fail, leaving no roles. A transaction/compensation strategy is a future hardening point.

---

# 16. Result and error behavior

## Result-based document endpoints

Use RFC 7807:

```json
{
  "title": "Documents.NotFound",
  "status": 404,
  "detail": "Document was not found.",
  "instance": "/api/v1/documents/example",
  "code": "Documents.NotFound",
  "errorType": "NotFound",
  "correlationId": "..."
}
```

## Identity/admin-user endpoints

Some expected failures use:

```json
{ "error": "..." }
```

Authentication/authorization may return empty 401/403 bodies.

Browser error parser supports all current variants.

## Unexpected exceptions

`UseExceptionHandler()` plus registered ProblemDetails handles exceptions through ASP.NET Core. The exact production detail disclosure follows environment/framework behavior and should be verified during deployment hardening.

---

# 17. Configuration files

## `appsettings.json`

Source-controlled safe defaults/placeholders:

- empty SQL connection;
- JWT issuer/audience;
- empty signing key;
- 30-minute access token;
- GitHub Pages origin;
- empty bootstrap admin;
- migrations-on-start true;
- conservative logging levels;
- all hosts accepted.

Empty sensitive values force external configuration in non-development environments.

## `appsettings.Development.json`

Development-only values:

- localhost SQL Server with Windows auth;
- development signing key;
- local issuer/audience;
- 60-minute access token;
- localhost HTTP/HTTPS origins;
- local bootstrap administrator;
- verbose ASP.NET Core/EF command logging.

These are explicitly non-production.

## `Properties/launchSettings.json`

Profiles:

- `http`: port 5080;
- `https`: ports 7080 and 5080.

`launchBrowser=false` is intentional. Visual Studio previously closed the multi-project debug session when debugger-managed browser windows terminated. Pages are opened through the separate script.

`launchUrl=swagger` remains useful for tools that choose to honor it.

---

# 18. Security headers and browser applications

API headers protect API responses. Admin/Client static hosting through Nginx may need equivalent or stronger headers at the gateway/static host. Security policy must be evaluated at the final deployment edge, not assumed from API middleware alone.

A complete Content Security Policy is not currently implemented.

---

# 19. Postman vs Swagger

Swagger is local discovery generated from endpoint metadata. Postman is the repository's repeatable operational request suite.

When an endpoint changes:

- update Contracts;
- update endpoint metadata;
- update typed clients;
- update Postman collection/environment/scripts;
- update `platform/docs/POSTMAN-REQUESTS.md`;
- update tests and this reference.

---

# 20. API modification checklist

Before adding or changing an endpoint:

1. identify the Application use case;
2. define/modify transport Contract;
3. choose route and method semantics;
4. define policy rather than scattering role strings when possible;
5. apply rate limiting;
6. declare success and ProblemDetails responses;
7. pass cancellation token;
8. avoid DbContext in document endpoints;
9. keep secrets out of response/logs;
10. update Postman and typed clients;
11. add authorization and workflow tests;
12. verify CORS and gateway routing;
13. preserve correlation ID in failures;
14. test 401, 403, 404, conflict, validation, and business-rule cases.
