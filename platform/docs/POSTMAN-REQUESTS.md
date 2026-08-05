# Postman Request Guide

This guide documents every currently exposed endpoint in the Entertainment Docs platform. It is aligned with the shared request and response contracts under `src/EntertainmentDocs.Contracts`.

## Importable files

Import both files into Postman:

- `platform/postman/EntertainmentDocs.postman_collection.json`
- `platform/postman/EntertainmentDocs.Local.postman_environment.json`

Select the **Entertainment Docs - Local** environment and start the API on `http://localhost:5080`.

## Standard headers

Requests with JSON bodies:

```http
Content-Type: application/json
Accept: application/json
```

Protected requests:

```http
Authorization: Bearer {{accessToken}}
```

The collection stores `accessToken`, `documentId`, `documentVersionId`, and `userId` automatically after successful create/login requests.

## Recommended execution order

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

---

## Platform

### `GET /`

Returns service metadata. No request body and no authentication.

```http
GET {{baseUrl}}/
```

### `GET /health`

Checks API and SQL Server connectivity. No request body and no authentication.

```http
GET {{baseUrl}}/health
```

Expected response body:

```text
Healthy
```

---

## Authentication

### `POST /api/v1/auth/login`

Authenticates an active user and returns a JWT token, user information, and assigned roles.

Request body:

```json
{
  "email": "admin@local.test",
  "password": "LocalAdmin!2026"
}
```

Field rules:

| Field | Type | Required | Meaning |
|---|---:|---:|---|
| `email` | string | Yes | Existing active account email |
| `password` | string | Yes | Account password |

Example response:

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "00000000-0000-0000-0000-000000000000",
    "displayName": "Local Administrator",
    "email": "admin@local.test"
  },
  "roles": [
    "Administrator",
    "Editor",
    "Reviewer",
    "Reader"
  ]
}
```

Possible results:

- `200 OK` — login succeeded.
- `401 Unauthorized` — email/password is invalid or the account is inactive.

---

## Public Documents

### `GET /api/v1/documents`

Returns all published documentation records. No request body and no authentication.

```http
GET {{baseUrl}}/api/v1/documents
```

Example response item:

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "reference": "API-ENT-DOC-001",
  "slug": "purchase-guide",
  "title": "Purchase API Integration Guide",
  "status": "Published",
  "updatedAt": "2026-08-05T12:00:00+00:00"
}
```

### `GET /api/v1/documents/{slug}`

Returns the latest published version of one document.

```http
GET {{baseUrl}}/api/v1/documents/{{documentSlug}}
```

Path variable:

| Variable | Example | Meaning |
|---|---|---|
| `slug` | `purchase-guide` | Unique URL-safe document identifier |

Possible results:

- `200 OK` — document found.
- `404 Not Found` — no published document exists for the slug.

---

## Admin Documents

All endpoints in this section require a Bearer token.

### `POST /api/v1/admin/documents`

Required role: `Administrator` or `Editor`.

Creates a new draft document.

Request body:

```json
{
  "reference": "API-ENT-DOC-POSTMAN-001",
  "slug": "purchase-guide",
  "title": "Purchase API Integration Guide"
}
```

Field rules:

| Field | Type | Required | Meaning |
|---|---:|---:|---|
| `reference` | string | Yes | Unique business/document reference |
| `slug` | string | Yes | Unique URL-safe identifier |
| `title` | string | Yes | Human-readable title |

Example response:

```json
{
  "id": "00000000-0000-0000-0000-000000000000"
}
```

Possible results:

- `201 Created` — document created.
- `400 Bad Request` — duplicate reference/slug or invalid domain state.
- `401 Unauthorized` — no valid token.
- `403 Forbidden` — token does not contain a permitted role.

### `POST /api/v1/admin/documents/{id}/versions`

Required role: `Administrator` or `Editor`.

Adds a documentation version to an existing document.

Request body:

```json
{
  "version": "1.0.0",
  "content": "# Purchase API\n\nThis is the first documentation version."
}
```

Field rules:

| Field | Type | Required | Meaning |
|---|---:|---:|---|
| `version` | string | Yes | Version label, for example `1.0.0` |
| `content` | string | Yes | Complete documentation content |

Example response:

```json
{
  "versionId": "00000000-0000-0000-0000-000000000000"
}
```

### `POST /api/v1/admin/documents/{id}/submit-review`

Required role: `Administrator` or `Editor`.

Moves a document into review.

Request body: **none**.

```http
POST {{baseUrl}}/api/v1/admin/documents/{{documentId}}/submit-review
Authorization: Bearer {{accessToken}}
```

Expected success: `204 No Content`.

### `POST /api/v1/admin/documents/{id}/publish`

Required role: `Administrator` or `Reviewer`.

Publishes a reviewed document.

Request body: **none**.

```http
POST {{baseUrl}}/api/v1/admin/documents/{{documentId}}/publish
Authorization: Bearer {{accessToken}}
```

Expected success: `204 No Content`.

---

## Admin Users

All endpoints in this section require the `Administrator` role.

Supported role values are case-sensitive:

```text
Administrator
Editor
Reviewer
Reader
```

### `GET /api/v1/admin/users`

Returns users, active state, and assigned roles.

Request body: **none**.

Example response item:

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "displayName": "Postman Editor",
  "email": "editor.postman@local.test",
  "isActive": true,
  "roles": [
    "Editor",
    "Reader"
  ]
}
```

### `POST /api/v1/admin/users`

Creates a user and assigns roles.

Request body:

```json
{
  "email": "editor.postman@local.test",
  "displayName": "Postman Editor",
  "temporaryPassword": "TempEditor!2026",
  "roles": [
    "Editor",
    "Reader"
  ]
}
```

Field rules:

| Field | Type | Required | Meaning |
|---|---:|---:|---|
| `email` | string | Yes | Unique login/email |
| `displayName` | string | Yes | Name shown in the UI and token |
| `temporaryPassword` | string | Yes | Must satisfy ASP.NET Core Identity password rules |
| `roles` | string[] | Yes | Zero or more supported role names |

Example response:

```json
{
  "id": "00000000-0000-0000-0000-000000000000"
}
```

### `PUT /api/v1/admin/users/{id}/roles`

Replaces all roles assigned to the selected user. This is replacement semantics, not additive semantics.

Request body:

```json
{
  "roles": [
    "Reviewer",
    "Reader"
  ]
}
```

To remove all roles:

```json
{
  "roles": []
}
```

Possible results:

- `204 No Content` — roles replaced.
- `400 Bad Request` — one or more role values are unsupported.
- `401 Unauthorized` — no valid token.
- `403 Forbidden` — current user is not an Administrator.
- `404 Not Found` — target user does not exist.

---

## Common error body

Business and validation failures use:

```json
{
  "error": "Human-readable error message."
}
```

Authentication and authorization failures may return an empty body with status `401` or `403`.

## Postman troubleshooting

- Run **Login** again whenever the API restarts or the JWT expires.
- Change `reference`, `slug`, or test user email before rerunning create requests against an existing database.
- Ensure `EntertainmentDocs.Api` is still running on `http://localhost:5080`.
- Check `GET /health` before diagnosing individual requests.
- Use the Postman Console to confirm the `Authorization: Bearer ...` header is present on protected requests.
