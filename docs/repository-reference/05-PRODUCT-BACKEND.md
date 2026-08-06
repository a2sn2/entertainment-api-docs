# EntertainmentDocs Product Backend

This chapter explains the product-specific Domain, Application, and Contracts projects. Infrastructure and API hosting are covered separately.

---

# 1. Product project dependency direction

```text
EntertainmentDocs.Domain
          ↑
EntertainmentDocs.Application
          ↑
EntertainmentDocs.Infrastructure
          ↑
EntertainmentDocs.Api

EntertainmentDocs.Contracts
    shared by API and Blazor clients
```

Contracts are adjacent transport definitions rather than a layer that business code should depend on. Domain entities are not returned directly over HTTP.

---

# 2. EntertainmentDocs.Domain

## 2.1 Project file

`EntertainmentDocs.Domain.csproj` references `FoundationKit.Domain` and no framework packages. This ensures the product model receives reusable primitives without acquiring EF Core, ASP.NET Core, or UI dependencies.

---

## 2.2 `Common/Entity.cs`

```csharp
public abstract class Entity : FoundationKit.Domain.Primitives.Entity<Guid>
```

This is a product-local alias specializing generic identity to `Guid`.

Why keep the wrapper:

- product entities do not repeat `<Guid>`;
- a future product-wide entity convention can be added locally;
- Domain files depend on product vocabulary while reusing core behavior.

Constructors simply forward an explicit ID or permit persistence materialization.

## 2.3 `Common/AggregateRoot.cs`

Specializes `FoundationKit.Domain.Primitives.AggregateRoot<Guid>`. It inherits entity equality and pending domain-event capability.

The current document aggregate does not yet raise domain events, but the capability is available without changing its inheritance later.

---

## 2.4 `Documents/DocumentStatus.cs`

```text
Draft = 0
InReview = 1
Published = 2
Archived = 3
```

Meaning:

- `Draft`: editable working state;
- `InReview`: submitted and awaiting publish authority;
- `Published`: visible through public document queries;
- `Archived`: closed to new version creation.

The enum is part of the persisted product model. Reordering or changing numeric values after data exists requires a deliberate migration/data strategy.

---

## 2.5 `Documents/DocumentVersion.cs`

```csharp
public sealed class DocumentVersion : Entity
```

A version has identity and belongs to one document.

### Construction boundaries

```csharp
private DocumentVersion() { }
```

Reserved for EF Core materialization.

```csharp
internal DocumentVersion(...)
```

Only code in the Domain assembly may call it. Normal creation happens through `DocumentationDocument.AddVersion`, protecting aggregate rules.

### Constructor assignments

- `Id`: unique version identity;
- `DocumentId`: parent aggregate key;
- `Version`: product version label;
- `Content`: complete document body;
- `AuthorId`: user responsible for this version;
- `CreatedAt`: immutable creation instant.

### Properties

All have `private set`; callers can read but not directly modify. Defaults on strings satisfy nullable/materialization requirements.

### Current limitation

The constructor trusts already validated strings supplied by the aggregate. It does not normalize semantic-version format; `1.0`, `1.0.0`, or another nonblank label can currently be stored, subject to database length and uniqueness constraints.

---

## 2.6 `Documents/DocumentationDocument.cs`

This is the central aggregate root.

### Child collection

```csharp
private readonly List<DocumentVersion> _versions = [];
public IReadOnlyCollection<DocumentVersion> Versions => _versions.AsReadOnly();
```

The aggregate owns mutation. Consumers receive a read-only view.

### Constructors

The private parameterless constructor exists for EF Core.

The private full constructor:

1. calls the aggregate base with ID;
2. validates and trims reference;
3. validates, trims, and lowercases slug;
4. validates and trims title;
5. assigns owner and timestamps;
6. initializes status as `Draft`.

Using a private constructor forces creation through the named factory.

### Properties

- `Reference`: unique business/document reference;
- `Slug`: unique URL-safe lookup key, normalized to lowercase;
- `Title`: display name;
- `Status`: workflow state;
- `OwnerId`: creator/owner identity;
- `CreatedAt`: creation instant;
- `UpdatedAt`: last aggregate workflow/content update;
- `PublishedAt`: nullable publication instant;
- `Versions`: owned child collection.

### `Create`

```csharp
public static DocumentationDocument Create(...)
```

Creates a new GUID and delegates invariant initialization to the private constructor. A named factory expresses business intent more clearly than a public constructor.

### `AddVersion`

Execution:

1. reject `Archived` state;
2. validate version and content with `Require`;
3. create child with new GUID, parent ID, author, and time;
4. add child to owned list;
5. update aggregate timestamp;
6. if currently `Published`, return to `Draft`;
7. return the child so Application can explicitly attach it to persistence.

Why published returns to Draft: a new unpublished body must not automatically replace the approved public version.

Current database mapping also has a composite unique index on `(DocumentId, Version)`; duplicate labels fail at the database boundary even though the aggregate does not currently pre-check its list.

### `SubmitForReview`

Rules:

- at least one version must exist;
- current status must be `Draft`.

On success:

- status becomes `InReview`;
- update timestamp changes.

### `Publish`

Only `InReview` can publish. On success:

- status becomes `Published`;
- publication and update times are set to the supplied instant.

### `Archive`

Sets status to `Archived` and updates time. Current implementation permits archiving from any status. No public archive endpoint is currently mapped.

### `Require`

Private string guard:

- throws `ArgumentException` for null/empty/whitespace;
- identifies the parameter through the provided name;
- returns trimmed text.

### Domain behavior vs application behavior

The aggregate owns state validity. Uniqueness needs persistence access, so reference/slug checks live in Application through the repository port.

---

# 3. EntertainmentDocs.Application

## 3.1 Project file

References:

- product Domain;
- FoundationKit.Application;
- `Microsoft.AspNetCore.App` shared framework.

The shared-framework reference currently supports application-level integration types available through the environment, but the architecture test prevents Infrastructure/API and SQL-provider dependencies. Future refactoring may reduce framework exposure further if no Application code requires it.

---

## 3.2 Product abstraction aliases

### `IClock.cs`

Extends `FoundationKit.Application.Abstractions.IClock` without new members.

### `ICurrentUser.cs`

Extends the core current-user contract.

### `IUnitOfWork.cs`

Extends the core save boundary.

Why aliases exist:

- handlers import a product namespace;
- DI can bind product ports explicitly;
- the product can extend its contract later without modifying FoundationKit;
- migration from pre-core abstractions remained source-compatible.

They should not diverge gratuitously from core behavior.

---

## 3.3 `Abstractions/IDocumentRepository.cs`

```csharp
public interface IDocumentRepository
    : IRepository<DocumentationDocument, Guid>
```

Inherits generic persistence operations and adds domain-language queries.

### `ReferenceExistsAsync`

Supports a pre-insert conflict result. Database uniqueness remains the final concurrency-safe guard.

### `SlugExistsAsync`

Checks normalized route key uniqueness.

### `GetWithVersionsAsync`

Loads the aggregate and child versions because review/publish/add-version rules depend on collection state.

### `GetPublishedBySlugAsync`

Public read query constrained to published status.

### `ListPublishedAsync`

Public catalog query.

### `AddVersionAsync`

Explicitly tracks a new child in the current persistence model. The aggregate adds it to its in-memory list; the repository ensures EF tracks the child set as expected.

### Why no `GetAllDocumentsAsync`

Only implemented use cases are represented. A generic “get everything” API would expose uncontrolled data and states without product intent.

---

# 4. Application result vocabulary

## `Documents/DocumentErrors.cs`

Centralizes stable product error codes.

### Authentication required

```text
Code: Documents.AuthenticationRequired
Type: Unauthorized
```

### Not found

```text
Code: Documents.NotFound
Type: NotFound
```

### Uniqueness conflicts

```text
Documents.ReferenceAlreadyExists
Documents.SlugAlreadyExists
```

Both map to conflict semantics.

### Business rule

`BusinessRule(description)` keeps stable code `Documents.BusinessRuleViolation` while preserving the aggregate exception message.

Why centralize:

- endpoints receive consistent HTTP mapping;
- browser clients receive machine-readable codes;
- tests can compare value records;
- descriptions can be improved independently of branching logic.

---

# 5. Application DTOs

## `Documents/DocumentDtos.cs`

### `DocumentSummaryDto`

Contains fields required for a list/card:

- ID;
- reference;
- slug;
- title;
- status string;
- updated time.

### `DocumentDetailsDto`

Adds latest version label and content.

These DTOs are Application outputs. API endpoints map them to transport responses, maintaining a boundary even when the current shapes match.

---

# 6. Commands and handlers

## 6.1 `CreateDocument.cs`

### Command

```csharp
public sealed record CreateDocumentCommand(
    string Reference,
    string Slug,
    string Title) : ICommand<Guid>;
```

Immutable intent returning the created document ID.

### Handler dependencies

- `IDocumentRepository`: persistence/query port;
- `IUnitOfWork`: commit boundary;
- `ICurrentUser`: creator identity;
- `IClock`: deterministic time.

### Handler execution

1. Pattern-match nullable `UserId` to a concrete GUID. Failure returns Unauthorized.
2. Check reference existence. Failure returns Conflict.
3. Check slug existence. Failure returns Conflict.
4. Call Domain factory with input, authenticated user ID, and clock.
5. Track aggregate through generic repository `AddAsync`.
6. Commit through unit of work.
7. Return success with aggregate ID.

### Important concurrency note

Pre-checks improve error clarity but two concurrent requests can pass them. Unique database indexes are still required; a complete concurrency translation policy should convert unique-constraint exceptions into the same conflict codes.

### Current validation behavior

Blank strings cause Domain `ArgumentException`. This handler does not currently catch it, so transport/model validation should reject obvious invalid input before Domain construction or the handler should translate it. This is a current improvement area, not hidden behavior.

---

## 6.2 `AddDocumentVersion.cs`

### Command

Carries document ID, version label, and content; returns created version ID.

### Handler execution

1. require authenticated user ID;
2. load document with versions;
3. return NotFound when absent;
4. call aggregate `AddVersion`;
5. attach version through repository;
6. save unit of work;
7. return version ID.

### Exception translation

- `ArgumentException` -> Validation code `Documents.InvalidVersion` with exception message;
- `InvalidOperationException` -> BusinessRule.

The validation code name covers both version label and content failures; a future validator could provide field-specific codes.

---

## 6.3 `SubmitDocumentForReview.cs`

Command contains document ID and returns no value.

Handler:

1. load aggregate with versions;
2. NotFound if absent;
3. call `SubmitForReview(clock.UtcNow)`;
4. save;
5. return success;
6. translate invalid state/version absence to BusinessRule.

Authentication/role enforcement occurs at the API policy boundary. The handler itself does not inspect role.

---

## 6.4 `PublishDocument.cs`

Same coordination shape as submit review:

1. load aggregate;
2. NotFound if absent;
3. call Domain publish rule;
4. save;
5. return success;
6. translate invalid state to BusinessRule.

Only API callers satisfying PublishContent policy can reach it through current HTTP endpoints.

---

# 7. Queries and handlers

## 7.1 `ListPublishedDocuments.cs`

The query has no parameters and returns a read-only list of summary DTOs.

Handler:

1. asks repository for published entities;
2. projects each entity to summary DTO;
3. converts enum to string;
4. materializes array;
5. returns successful Result.

No pagination is used yet. FoundationKit pagination primitives are available for a future server-side catalog query.

## 7.2 `GetPublishedDocument.cs`

Query carries slug and returns details DTO.

Handler:

1. loads published document with versions;
2. orders versions descending by `CreatedAt`;
3. selects latest;
4. fails NotFound when document or version is missing;
5. maps aggregate/latest version to details DTO.

The latest version is selected by creation time, not semantic version ordering. This is deliberate current behavior and must be considered when importing historical versions.

---

# 8. Application dependency injection

## `DependencyInjection.cs`

`AddApplication` explicitly registers each interface-to-handler mapping as scoped.

Why explicit registration:

- easy to audit;
- no assembly-scanning magic;
- compile-time interface shapes remain visible;
- startup composition documents available use cases.

Trade-off: every new handler requires one registration line. This is acceptable at current scale and avoids a mediator dependency.

---

# 9. EntertainmentDocs.Contracts

## 9.1 Project file

Plain .NET class library with no Domain/Application/Infrastructure reference. Architecture tests enforce transport-only independence.

---

## 9.2 Authentication contracts

### `LoginRequest`

```json
{
  "email": "...",
  "password": "..."
}
```

### `AuthenticatedUserResponse`

Returns GUID, display name, and nullable email.

### `LoginResponse`

Returns:

- access token;
- user summary;
- assigned roles.

It does not return password hashes, security stamps, or complete Identity entity data.

---

## 9.3 Document contracts

### `CreateDocumentRequest`

Reference, slug, title.

### `AddDocumentVersionRequest`

Version and content.

### `CreatedDocumentResponse`

Created document GUID.

### `CreatedDocumentVersionResponse`

Created child version GUID.

### `DocumentSummaryResponse`

Public catalog representation.

### `DocumentDetailsResponse`

Public latest published document representation.

Transport records are immutable by default and serialize through property names generated from positional record parameters using configured JSON naming policy defaults.

---

## 9.4 User contracts

### `CreateUserRequest`

- email;
- display name;
- temporary password;
- roles array.

The API validates role names against `SystemRoles.All`; Identity validates password rules.

### `UpdateUserRolesRequest`

Complete replacement role array, not an additive patch.

### `CreatedUserResponse`

User GUID.

### `UserSummaryResponse`

ID, display name, nullable email, active state, read-only role list.

---

## 9.5 `Common/ApiErrorResponse.cs`

A one-field legacy/simple error shape:

```json
{ "error": "..." }
```

User administration endpoints currently use it for some Identity failures, while Result-based document endpoints use RFC 7807 ProblemDetails. Browser `ApiResponseReader` supports both shapes.

A future consistency change should migrate all expected errors to one documented ProblemDetails policy without breaking clients silently.

---

# 10. End-to-end ownership example: create document

```text
CreateDocumentRequest
    API transport shape
        ↓ maps to
CreateDocumentCommand
    application intent
        ↓ handled by
CreateDocumentCommandHandler
    authentication + uniqueness coordination
        ↓ calls
DocumentationDocument.Create
    domain validation and initial state
        ↓ tracked through
IDocumentRepository / DocumentRepository
        ↓ committed through
IUnitOfWork / AppDbContext
        ↓ returns
CreatedDocumentResponse
```

Each type has a different reason to change:

- Contract changes when HTTP agreement changes.
- Command changes when use-case input changes.
- Aggregate changes when business rules change.
- Repository changes when persistence/query implementation changes.
- Endpoint changes when HTTP route/status/policy changes.

---

# 11. Product backend modification rules

1. Put state invariants in Domain.
2. Put uniqueness and orchestration in Application through ports.
3. Do not inject DbContext into handlers.
4. Do not return Domain entities from endpoints.
5. Keep one command/query per explicit use case.
6. Use classified Result errors for expected outcomes.
7. Pass CancellationToken to all I/O.
8. Use `IClock` and `ICurrentUser`, not static HTTP/time access in handlers.
9. Add a specialized repository method only when it expresses product language or query shape that generic specifications do not express clearly.
10. Update Domain tests when state rules change.
11. Update Contracts, Postman, frontend client, API metadata, and tests in the same change when HTTP shapes change.
