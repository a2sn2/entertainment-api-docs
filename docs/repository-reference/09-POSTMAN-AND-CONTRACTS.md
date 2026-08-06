# Postman and HTTP Contract Reference

Swagger provides generated local discovery. Postman provides the repeatable, ordered, environment-aware API test suite that can be run outside the application UI.

---

# 1. Source files

```text
platform/postman/EntertainmentDocs.postman_collection.json
platform/postman/EntertainmentDocs.Local.postman_environment.json
platform/docs/POSTMAN-REQUESTS.md
```

- The collection stores requests, scripts, headers, and variables.
- The local environment stores base URL and local values.
- The Markdown guide explains bodies, meanings, roles, statuses, and troubleshooting.

The collection is executable documentation. JSON parsing alone proves syntax, not endpoint correctness; the Docker smoke test validates the main workflow independently.

---

# 2. Import and environment selection

Import both JSON files into Postman and select:

```text
Entertainment Docs - Local
```

Local API base:

```text
http://localhost:5080
```

Common headers:

```http
Content-Type: application/json
Accept: application/json
```

Protected requests add:

```http
Authorization: Bearer {{accessToken}}
```

---

# 3. Environment variables

## `baseUrl`

API origin without a trailing route, normally `http://localhost:5080`.

## `accessToken`

JWT captured after successful login. Treat it as sensitive even in local testing.

## `documentId`

GUID captured after Create Document. Used by version, review, and publish requests.

## `documentVersionId`

GUID captured after Add Document Version.

## `documentSlug`

Slug used for public detail lookup.

## `userId`

GUID captured after Create User, used by role replacement.

Variables reduce copy/paste mistakes and preserve workflow linkage.

---

# 4. Recommended execution order

```text
1. Health Check
2. Login
3. Create Document
4. Add Document Version
5. Submit Document for Review
6. Publish Document
7. List Published Documents
8. Get Published Document by Slug
9. Create User
10. List Users
11. Replace User Roles
```

The order reflects data dependencies:

- login produces token;
- create produces document ID;
- add version is required before review;
- review is required before publish;
- publish is required before public retrieval;
- create user produces user ID for role replacement.

---

# 5. Platform requests

## `GET /`

Purpose: process/service metadata.

Body: none.

Authentication: none.

Expected: 200 and metadata containing service, environment, database provider, and running status.

## `GET /health`

Purpose: API plus DbContext health.

Body: none.

Authentication: none.

Expected body at current local setup:

```text
Healthy
```

A failed health check should be investigated before individual endpoint failures.

---

# 6. Login

## Request

```http
POST {{baseUrl}}/api/v1/auth/login
Content-Type: application/json
```

```json
{
  "email": "admin@local.test",
  "password": "LocalAdmin!2026"
}
```

## Contract

`LoginRequest`:

| Field | Type | Required | Meaning |
|---|---|---|---|
| `email` | string | yes | Existing active Identity account email |
| `password` | string | yes | Plaintext submitted over the transport for Identity verification |

Passwords must be sent only over trusted HTTPS outside isolated local development.

## Success response

```json
{
  "accessToken": "<jwt>",
  "user": {
    "id": "<guid>",
    "displayName": "Bootstrap Administrator",
    "email": "admin@local.test"
  },
  "roles": ["Administrator"]
}
```

The Postman test script stores `accessToken`.

## Failure

`401 Unauthorized` when:

- user does not exist;
- password is wrong;
- account is inactive.

The endpoint deliberately does not reveal which condition failed.

---

# 7. Public documents

## List published

```http
GET {{baseUrl}}/api/v1/documents
```

No body/token.

Response is an array of:

```json
{
  "id": "<guid>",
  "reference": "API-ENT-DOC-001",
  "slug": "purchase-guide",
  "title": "Purchase API Integration Guide",
  "status": "Published",
  "updatedAt": "2026-08-05T12:00:00+00:00"
}
```

Only Published records are returned.

## Get by slug

```http
GET {{baseUrl}}/api/v1/documents/{{documentSlug}}
```

Returns latest version by creation time or RFC 7807 404.

---

# 8. Create document

```http
POST {{baseUrl}}/api/v1/admin/documents
Authorization: Bearer {{accessToken}}
Content-Type: application/json
```

Roles: Administrator or Editor.

Body:

```json
{
  "reference": "API-ENT-DOC-POSTMAN-001",
  "slug": "purchase-guide",
  "title": "Purchase API Integration Guide"
}
```

Rules:

- all three fields are required by Domain intent;
- reference unique;
- slug unique and normalized lowercase by Domain;
- database length limits: reference 64, slug 120, title 240;
- use a new reference/slug when rerunning against the same database.

Success:

```text
201 Created
Location: /api/v1/admin/documents/<id>
```

```json
{ "id": "<guid>" }
```

Postman stores `documentId` and should store/use matching `documentSlug`.

Expected failures:

- 401 no/invalid token;
- 403 wrong role;
- 409 duplicate reference/slug;
- validation/unexpected failure for invalid values depending on current validation path.

---

# 9. Add document version

```http
POST {{baseUrl}}/api/v1/admin/documents/{{documentId}}/versions
Authorization: Bearer {{accessToken}}
Content-Type: application/json
```

Roles: Administrator or Editor.

Body:

```json
{
  "version": "1.0.0",
  "content": "# Purchase API\n\nThis is the first documentation version."
}
```

Rules:

- target document must exist;
- archived document rejects new versions;
- version/content must be nonblank;
- version label max 32;
- version label unique within the document;
- content stored in `nvarchar(max)`;
- adding to Published changes document back to Draft.

Success currently returns 200:

```json
{ "versionId": "<guid>" }
```

Postman stores `documentVersionId`.

---

# 10. Submit for review

```http
POST {{baseUrl}}/api/v1/admin/documents/{{documentId}}/submit-review
Authorization: Bearer {{accessToken}}
```

Body: none.

Roles: Administrator or Editor.

Rules:

- document exists;
- current status Draft;
- at least one version.

Success: `204 No Content`.

Business-state rejection: `422` ProblemDetails.

---

# 11. Publish

```http
POST {{baseUrl}}/api/v1/admin/documents/{{documentId}}/publish
Authorization: Bearer {{accessToken}}
```

Body: none.

Roles: Administrator or Reviewer.

Rule: current status must be InReview.

Success: `204 No Content`.

After this request, public list/detail can return the document.

---

# 12. List users

```http
GET {{baseUrl}}/api/v1/admin/users
Authorization: Bearer {{accessToken}}
```

Role: Administrator.

Body: none.

Response item:

```json
{
  "id": "<guid>",
  "displayName": "Documentation Editor",
  "email": "editor@local.test",
  "isActive": true,
  "roles": ["Editor", "Reader"]
}
```

Current endpoint returns all users and resolves roles per user; no server pagination.

---

# 13. Create user

```http
POST {{baseUrl}}/api/v1/admin/users
Authorization: Bearer {{accessToken}}
Content-Type: application/json
```

Role: Administrator.

Body:

```json
{
  "email": "editor.postman@local.test",
  "displayName": "Postman Editor",
  "temporaryPassword": "TempEditor!2026",
  "roles": ["Editor", "Reader"]
}
```

Field meaning:

| Field | Meaning |
|---|---|
| `email` | unique Identity username/email |
| `displayName` | UI and JWT name |
| `temporaryPassword` | initial password, validated and hashed by Identity |
| `roles` | exact supported role names |

Current password policy includes minimum 12, digit, uppercase. Identity defaults may add other requirements depending on framework defaults/configuration.

Supported roles are case-sensitive:

```text
Administrator
Editor
Reviewer
Reader
```

Success:

```text
201 Created
```

```json
{ "id": "<guid>" }
```

Postman stores `userId`.

Use a new email when rerunning against the same database.

---

# 14. Replace roles

```http
PUT {{baseUrl}}/api/v1/admin/users/{{userId}}/roles
Authorization: Bearer {{accessToken}}
Content-Type: application/json
```

Role: Administrator.

Body:

```json
{
  "roles": ["Reviewer", "Reader"]
}
```

This replaces all current roles. It is not additive.

Remove all roles:

```json
{ "roles": [] }
```

Success: `204 No Content`.

Errors:

- 400 unsupported role or Identity operation failure;
- 401/403 auth failure;
- 404 target user absent.

---

# 15. Error shapes

## Result-based RFC 7807

Example:

```json
{
  "type": "about:blank",
  "title": "Documents.NotFound",
  "status": 404,
  "detail": "Document was not found.",
  "instance": "/api/v1/documents/missing",
  "code": "Documents.NotFound",
  "errorType": "NotFound",
  "correlationId": "4dd2..."
}
```

Fields supplied by framework may vary slightly, but code/detail/status/correlation semantics are stable intentions.

## Simple Identity error

```json
{
  "error": "Human-readable Identity error description."
}
```

## Empty auth response

401/403 may contain no JSON body.

The shared Blazor parser handles these variants.

---

# 16. Collection test scripts

Postman scripts should:

- verify expected status code;
- parse JSON only when a body is expected;
- store IDs/tokens only after successful parse;
- avoid logging full JWT/password values;
- assert required response fields;
- keep variable names aligned with environment.

A request's script must not create false success by swallowing parse/assertion errors.

---

# 17. Synchronization with C# contracts

Authoritative transport files:

```text
EntertainmentDocs.Contracts/Authentication/AuthenticationContracts.cs
EntertainmentDocs.Contracts/Documents/DocumentContracts.cs
EntertainmentDocs.Contracts/Users/UserContracts.cs
EntertainmentDocs.Contracts/Common/ApiErrorResponse.cs
```

When a field changes, update in one PR:

1. C# request/response record;
2. API endpoint mapping and OpenAPI metadata;
3. Application DTO mapping when applicable;
4. Admin/Client typed API client;
5. Razor examples/forms;
6. Postman collection;
7. Postman environment variable usage;
8. `POSTMAN-REQUESTS.md`;
9. smoke/integration tests;
10. repository reference documentation.

Do not change only Swagger annotations. Runtime serialization follows C# types and endpoint code.

---

# 18. Postman troubleshooting

## Connection refused

- ensure API process still runs on 5080;
- call `/health`;
- verify environment selection/base URL;
- check Visual Studio output.

## 401 after previous success

- rerun Login;
- confirm `accessToken` updated;
- inspect Authorization header in Postman Console;
- API restart does not necessarily invalidate HMAC token if signing key unchanged, but expiry/config may.

## 403

Token is valid but role policy rejects it. Inspect roles in login response and endpoint policy.

## Duplicate create failure

Change reference/slug/email or reset isolated test database. Do not interpret uniqueness protection as infrastructure failure.

## 422 review/publish failure

Follow state order and ensure version exists.

## Unexpected HTML response

Likely wrong base URL/gateway route or SPA response. Inspect response URL/content type.

## SQL/health failure

Run local SQL setup script, verify SQL service and database, inspect EF logs.

---

# 19. Contract-testing quality improvements

Current assets provide a strong manual/repeatable suite. Future hardening may add:

- Newman execution in CI for the collection itself;
- JSON schema or OpenAPI contract assertions;
- negative authorization cases per role;
- duplicate/conflict tests;
- malformed body validation tests;
- token expiry tests;
- user-role partial-failure tests;
- correlation-ID propagation assertions;
- concurrency tests;
- server pagination contract tests when introduced.

These are future improvements, not current claims.
