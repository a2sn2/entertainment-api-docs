# Language and Syntax Glossary

This chapter defines recurring language constructs once so file walkthroughs can focus on repository-specific intent.

---

# 1. C# fundamentals used in the repository

## `using`

Imports a namespace so its types can be referenced by short name.

```csharp
using Microsoft.EntityFrameworkCore;
```

Without the import, code would need the fully qualified name such as `Microsoft.EntityFrameworkCore.DbContext`.

`using var resource = ...;` has a different meaning: it declares a disposable local variable and guarantees disposal when the current scope ends.

## `namespace`

Places types in a logical name scope.

```csharp
namespace FoundationKit.Application.Results;
```

The repository uses file-scoped namespaces, ending with `;`, so the remainder of the file belongs to that namespace without another brace block.

## Access modifiers

- `public`: callable from any referencing assembly.
- `internal`: callable only inside the same assembly.
- `protected`: callable from the declaring type and derived types.
- `private`: callable only inside the declaring type.

Example: `DocumentVersion` has an `internal` constructor so the aggregate can create versions while outside assemblies cannot freely bypass aggregate behavior.

## `class`

Defines a reference type with identity and behavior.

```csharp
public sealed class SystemClock : IClock
```

## `interface`

Defines a contract without selecting a concrete implementation.

```csharp
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
```

The `I` prefix is the repository's C# naming convention for interfaces.

## `record`

Defines a data-oriented reference type with value-based equality generated from its declared members.

```csharp
public sealed record LoginRequest(string Email, string Password);
```

Records are used for immutable commands, queries, DTOs, errors, and HTTP contracts. Domain entities are classes because their identity and lifecycle differ from value-based transport data.

## `enum`

Defines a closed set of named integral values.

```csharp
public enum DocumentStatus
{
    Draft = 0,
    InReview = 1
}
```

Explicit numbers make persistence representation stable and readable.

## `static`

A static member belongs to the type rather than an instance. A static class cannot be instantiated.

Examples:

- `DocumentErrors` stores reusable error definitions.
- extension-method classes are static.
- `SystemRoles.All` provides a shared role list.

## `sealed`

Prevents inheritance. It communicates that extension should occur through composition or interfaces rather than subclassing.

## `abstract`

Prevents direct construction and may require derived types to implement members. `Entity<TId>`, `AggregateRoot<TId>`, and `ValueObject` are abstract because they are foundations for concrete domain types.

## Inheritance and interface implementation: `:`

```csharp
public sealed class DocumentRepository
    : EfRepository<DocumentationDocument, Guid, AppDbContext>, IDocumentRepository
```

The class derives from the generic repository implementation and promises to implement the product repository interface.

## Generic types: `<T>`

A generic type or method accepts a type parameter.

```csharp
Result<T>
Entity<TId>
IRepository<TEntity, TId>
```

This permits reusable behavior without reducing everything to `object` or runtime casting.

## Generic constraints: `where`

```csharp
where TEntity : Entity<TId>
where TId : notnull
```

Constraints tell the compiler what a generic parameter must provide. The repository can safely access `entity.Id` because `TEntity` must derive from `Entity<TId>`.

## Variance: `in` and `out` on generic parameters

- `out TResponse` marks covariance: the type parameter is produced, not consumed.
- `in TCommand` marks contravariance: the type parameter is consumed, not produced.

These markers make generic interfaces more type-compatible while preserving safety.

## Primary constructors

Modern C# allows constructor parameters immediately after a type name:

```csharp
public sealed class SystemClockDependency(SomeService service)
```

Repository examples include middleware, API clients, handlers, and services. The parameters are available throughout the type body.

## Traditional constructors

```csharp
protected Entity(TId id) => Id = id;
```

A constructor initializes an object. Access level controls who may create it.

## Parameterless constructors for EF Core

Domain entities include private or protected parameterless constructors because EF Core materializes objects from database rows. Restricting the constructor prevents normal application code from bypassing factories or invariants.

## Properties

```csharp
public string Title { get; private set; } = string.Empty;
```

- `get` allows reading.
- `set` allows mutation.
- `private set` allows mutation only inside the declaring type.
- `init` allows assignment only during initialization.
- `= string.Empty` provides a non-null default for object materialization and nullable analysis.

## Fields

```csharp
private readonly List<IDomainEvent> _domainEvents = [];
```

A field stores implementation state. `_` is the repository convention for private instance fields. `readonly` prevents replacing the field reference after construction, but the referenced list may still change.

## `const`

A compile-time constant.

```csharp
public const string HeaderName = "X-Correlation-ID";
```

Use for values that are truly fixed at compile time.

## `static readonly`

Initialized once at runtime and then cannot be reassigned.

```csharp
public static readonly Error None = ...;
```

Used when the value is not a compile-time primitive constant.

## Nullable reference types: `?`

```csharp
string?
Guid?
T?
```

The value may be absent. Nullable reference types enable compile-time warnings for unsafe null use.

## Null-forgiving operator: `!`

```csharp
_value!
```

Tells the compiler the developer has proved the value is non-null at that point. It does not perform a runtime check. Misuse can still cause a null-reference exception.

## Null-coalescing operator: `??`

```csharp
configuredValue ?? defaultValue
```

Returns the left side when non-null; otherwise returns the right side.

## Null-conditional operator: `?.`

```csharp
Principal?.Identity?.IsAuthenticated
```

Stops and returns null if the preceding value is null.

## Conditional operator: `condition ? trueValue : falseValue`

A compact expression-level branch.

## Equality operators

- `==` and `!=`: equality/inequality; may be overloaded.
- `Equals`: virtual equality method.
- `ReferenceEquals`: whether two variables point to the same object instance.

FoundationKit entities overload equality to use non-default identity while keeping two new transient entities distinct.

## `new`

Creates an object or hides an inherited static member depending on context.

```csharp
new ApiError(...)
public new static Result<T> Failure(...)
```

In the second example, `new` explicitly hides the base static method with a generic return type.

## Target-typed `new()`

```csharp
new()
```

The compiler infers the constructed type from the assignment or return context.

## Collection expressions: `[]`

```csharp
private readonly List<IDomainEvent> _domainEvents = [];
```

Modern C# syntax for an empty collection where the target type is known.

## Array/list spread is not used here

Do not confuse C# collection expressions with JavaScript spread syntax. This repository primarily uses empty `[]` and normal LINQ conversion methods.

## `var`

Requests local type inference. The type remains static and compile-time checked.

```csharp
var document = DocumentationDocument.Create(...);
```

## Expression-bodied member: `=>`

```csharp
public bool IsFailure => !IsSuccess;
```

Equivalent to a one-expression getter or method body.

## Lambda expression

```csharp
document => document.Status == DocumentStatus.Published
```

An inline function. EF Core can translate expression trees into SQL when used with `IQueryable`.

## `async` and `await`

- `async` allows a method to suspend while awaiting asynchronous work.
- `await` asynchronously waits without blocking the request thread.

Repository I/O—HTTP, database, browser storage, process orchestration—is asynchronous.

## `Task` and `Task<T>`

Represent asynchronous completion, optionally with a result.

```csharp
Task<Result<Guid>>
```

## `ValueTask` and `ValueTask<T>`

An allocation-conscious async result type used where completion may often be synchronous or a framework interface requires it. EF interceptors and JS interop store methods use it.

## `CancellationToken`

Allows a caller to request cancellation of database, HTTP, or handler work. The default token means “not cancellable unless supplied.” It should be passed through every asynchronous layer.

## `try`, `catch`, `finally`

- `try`: code that may fail.
- `catch`: handles a selected exception type.
- `finally`: always runs, typically resetting loading state.

Handlers translate expected domain exceptions into typed errors. Unexpected exceptions should reach centralized exception handling rather than be silently swallowed.

## `throw`

Raises an exception. `throw new ...` creates one; an exception expression can also be embedded in validation code.

## `nameof`

Returns the compile-time name of a symbol.

```csharp
nameof(code)
```

Safer than repeating a string that can drift during refactoring.

## `typeof`

Returns a runtime `Type` object, used for reflection, assembly inspection, or generic handler discovery.

## Pattern matching

```csharp
if (currentUser.UserId is not Guid userId)
```

Checks type/null state and introduces a typed local variable when successful.

```csharp
if (specification.Skip is int skip)
```

Checks that a nullable integer has a value and binds it.

## Switch expression

```csharp
var code = type switch
{
    ErrorType.Validation => 400,
    _ => 500
};
```

Maps a value to an expression. `_` is the fallback pattern.

## LINQ

Language Integrated Query methods used throughout:

- `Where`: filter.
- `Select`: project to a new shape.
- `SelectMany`: flatten nested sequences.
- `OrderBy` / `OrderByDescending`: sort.
- `AnyAsync`: database existence test.
- `SingleOrDefaultAsync`: zero or one expected row; throws if more than one.
- `FirstOrDefaultAsync`: first row or null.
- `Include`: eager-load navigation data.
- `AsNoTracking`: skip change tracking for read-only queries.
- `ToListAsync`: execute and materialize a list.
- `ToArray`: materialize an array.
- `Aggregate`: fold a sequence into one value.

## `IEnumerable<T>` vs `IQueryable<T>`

- `IEnumerable<T>` represents in-memory iteration.
- `IQueryable<T>` represents a query expression a provider such as EF Core can translate, commonly to SQL.

Filtering should stay on `IQueryable` until materialization when server-side execution is intended.

## `IReadOnlyList<T>` and `IReadOnlyCollection<T>`

Expose collection data without a public mutation API. This does not make each contained object immutable, but protects collection ownership.

## `Object.freeze` equivalent

C# records/read-only properties provide some of the intent JavaScript gets from `Object.freeze`, but they are not identical. A record can still contain mutable members.

## Extension methods

A static method whose first parameter is prefixed with `this` can be called as if it were an instance method.

Examples:

```csharp
services.AddFoundationWebApi();
result.ToHttpResult();
```

## Dependency injection lifetimes

- `AddSingleton`: one instance for the process.
- `AddScoped`: one instance per web request; in Blazor WebAssembly, scoped behaves similarly to app-session lifetime.
- `AddTransient`: a new instance per resolution.

The repository uses scoped DbContext/repositories/handlers and singleton stateless system clock.

## `IOptions<T>`

ASP.NET Core typed configuration wrapper. `JwtTokenService` receives `IOptions<JwtOptions>` rather than reading raw configuration keys repeatedly.

## Reflection

`DomainEventDispatcher` builds a closed generic handler type at runtime and invokes `HandleAsync` on all registered handlers. Reflection trades compile-time direct calls for a generic dispatcher that does not depend on a mediator library.

---

# 2. Razor and Blazor syntax

## `.razor`

A Razor component combines markup and C#.

## `@page`

Defines a routable component template.

```razor
@page "/documents/{Slug}"
```

`{Slug}` becomes a route parameter bound to a `[Parameter]` property.

## `@layout`

Selects a layout component for the page.

## `@attribute`

Adds a .NET attribute to the generated component class.

```razor
@attribute [Authorize(Roles = "Administrator")]
```

## `@inject`

Requests a service from dependency injection and exposes it as a component property.

## `@inherits`

Changes the generated component base class. Layouts inherit `LayoutComponentBase` to receive `Body`.

## `@code`

Contains the component's C# fields, parameters, lifecycle methods, and event handlers.

## `@if`, `@foreach`

Razor control flow that decides what markup to render.

## Component parameters

```csharp
[Parameter]
public string Message { get; set; }
```

A parent supplies values through attributes.

`[EditorRequired]` gives tooling a warning when a required parameter is omitted; it is not a runtime validation system.

## `RenderFragment`

Represents a block of UI content supplied by a parent. Named fragments such as `Actions` allow reusable components to expose layout slots.

## `EventCallback`

A component event parameter designed to integrate with Blazor rendering. `HasDelegate` indicates whether a parent supplied a handler.

## `@bind-Value`

Two-way binding: display the field value and update it when the control changes.

## `@bind-IsValid`

Binds MudForm validation state to a C# boolean.

## `@ref`

Captures a component reference, allowing code to call methods such as `Validate()` or `ResetValidation()`.

## Lifecycle methods

- `OnInitialized` / `OnInitializedAsync`: first component initialization.
- `OnParametersSetAsync`: runs when route or parent parameters are assigned or changed.

## `AuthorizeRouteView`

Renders a route only when authorization succeeds and exposes `NotAuthorized` and `Authorizing` UI.

## `AuthorizeView`

Conditionally renders fragments based on the current principal or roles. This is not a substitute for API authorization.

## `CascadingAuthenticationState`

Makes authentication state available to descendant components.

## MudBlazor providers

`MudPopoverProvider`, `MudDialogProvider`, and `MudSnackbarProvider` are hosted once at each app root. Duplicating them in layouts caused a previous runtime exception and is intentionally avoided.

---

# 3. JavaScript syntax used by the static portal

## `import` / `export`

ES module boundaries.

```javascript
export function searchDocumentation(...) { }
import { searchDocumentation } from './file.js';
```

The browser loads modules through `<script type="module">`.

## `const` and `let`

- `const`: binding cannot be reassigned.
- `let`: binding may be reassigned.

Objects referenced by `const` can still mutate unless frozen.

## Arrow functions

```javascript
(value) => String(value).trim()
```

Compact functions with lexical `this` behavior.

## Destructuring

```javascript
([key, value])
```

Extracts array or object parts into local names.

## Spread syntax

```javascript
[...scenarios]
```

Creates a shallow copy or inserts iterable items.

## Optional chaining and nullish coalescing

```javascript
value?.property
value ?? ''
```

Optional chaining stops on null/undefined. `??` falls back only for null/undefined, unlike `||`, which also treats empty string, zero, and false as absent.

## Template literals

```javascript
`pages/${slug}.html`
```

Backtick strings with `${...}` interpolation.

## `Object.freeze`

Prevents direct mutation of the frozen object's own properties. It is shallow unless nested objects are also frozen.

## `Object.entries` / `Object.fromEntries`

Convert object properties to key/value arrays and back. Used to normalize request builder input.

## DOM APIs

- `document.getElementById`
- `querySelector` / `querySelectorAll`
- `addEventListener`
- `innerHTML`
- `classList`
- `dataset`

Presentation code owns these browser details.

## `localStorage`

Persistent browser key/value storage used for preferences, not secrets.

## `matchMedia`

Reads browser media-query state such as preferred color scheme.

## Service worker registration

`navigator.serviceWorker.register(...)` installs the static cache worker when the browser and protocol support it.

---

# 4. JSON

JSON represents configuration and HTTP payloads.

- Object: `{ "key": "value" }`
- Array: `["a", "b"]`
- String: quoted UTF-8 text
- Number: unquoted numeric value
- Boolean: `true` / `false`
- Null: `null`

JSON has no comments. Secret values should be supplied through environment-specific configuration rather than committed production JSON.

ASP.NET Core maps double-underscore environment variable separators to nested configuration:

```text
ConnectionStrings__SqlServer
```

maps to:

```json
{
  "ConnectionStrings": {
    "SqlServer": "..."
  }
}
```

---

# 5. XML and MSBuild project files

## `<Project Sdk="...">`

Selects the SDK and build targets.

- `Microsoft.NET.Sdk`: class library/test project.
- `Microsoft.NET.Sdk.Web`: ASP.NET Core host.
- `Microsoft.NET.Sdk.BlazorWebAssembly`: browser application.
- `Microsoft.NET.Sdk.Razor`: Razor Class Library.

## `<PropertyGroup>`

Contains scalar build properties such as `TargetFramework`, `RootNamespace`, and package metadata.

## `<ItemGroup>`

Contains collections such as references.

## `<ProjectReference>`

Compile-time reference to another project in the repository.

## `<PackageReference>`

NuGet dependency. Versions are centralized in `Directory.Packages.props`.

## `<FrameworkReference>`

References a shared framework such as `Microsoft.AspNetCore.App` rather than a standalone package.

## `<PrivateAssets>all</PrivateAssets>`

Prevents a build/design dependency from flowing transitively to consumers.

## Central Package Management

`ManagePackageVersionsCentrally=true` means project files name packages while `Directory.Packages.props` owns versions.

---

# 6. YAML used by GitHub Actions and Docker Compose

YAML uses indentation to define nested mappings and lists.

```yaml
jobs:
  build-test:
    runs-on: ubuntu-latest
```

Important constructs:

- `on`: workflow triggers.
- `jobs`: independently scheduled work units.
- `steps`: ordered actions/commands.
- `uses`: reusable GitHub Action.
- `run`: shell command.
- `env`: environment variables.
- `needs`: job dependency.
- `if: always()`: execute cleanup even after failure.
- `${{ ... }}`: GitHub Actions expression.

Docker Compose fields:

- `services`: containers.
- `build`: Docker build context and Dockerfile.
- `image`: existing image.
- `environment`: container variables.
- `ports`: host-to-container mapping.
- `expose`: internal network port metadata.
- `depends_on`: startup dependency conditions.
- `healthcheck`: readiness test.
- `volumes`: persistent or mounted data.

---

# 7. PowerShell

## `[CmdletBinding()]`

Makes a script behave like an advanced cmdlet.

## `param(...)`

Declares script parameters, types, defaults, switches, and validation attributes.

## `$variable`

PowerShell variable syntax.

## `$env:NAME`

Reads or writes an environment variable.

## `$ErrorActionPreference = "Stop"`

Turns non-terminating command errors into terminating errors so the script fails reliably.

## `Push-Location` / `Pop-Location`

Temporarily changes the working directory and restores it in `finally`.

## `$LASTEXITCODE`

Exit code of the last native executable, used to detect `dotnet` failure.

## Backtick line continuation

A trailing backtick continues a PowerShell command on the next line. Trailing spaces after the backtick can break continuation.

## `Join-Path`, `Split-Path`, `Resolve-Path`

Construct and resolve platform-correct filesystem paths.

## `Invoke-WebRequest`

HTTP probe used by the local page launcher.

## `Start-Process`

Opens a URL with the system browser.

## `[pscustomobject]`

Creates a lightweight structured object, used for service targets.

---

# 8. Bash

## Shebang

```bash
#!/usr/bin/env bash
```

Selects Bash through the current environment.

## `set -euo pipefail`

- `-e`: exit after unhandled command failure.
- `-u`: treat unset variables as errors.
- `-o pipefail`: a pipeline fails when any component fails.

This prevents false-positive automation success.

## `${NAME:-default}`

Use environment variable value or fallback.

## `${NAME:?message}`

Require the variable; terminate with a message when missing.

## `$(command)`

Command substitution: run a command and capture its output.

## `[[ ... ]]`

Bash conditional expression.

## `mapfile -t`

Reads command output lines into an array without trailing newline characters.

## `curl --fail --silent --show-error`

Treat HTTP 4xx/5xx as failure, suppress progress, but still show errors.

---

# 9. Dockerfiles

## `FROM`

Selects a base image. Multi-stage builds use multiple `FROM` statements.

## `WORKDIR`

Sets the working directory for following instructions.

## `COPY`

Copies build context files into the image.

## `RUN`

Executes a build-time command and commits the resulting layer.

## `ARG`

Build-time input, used by the reusable Blazor Dockerfile to select a project and base path.

## `ENV`

Image/container environment variable.

## `EXPOSE`

Documents the listening container port; it does not publish it to the host by itself.

## `ENTRYPOINT`

Defines the executable run when the container starts.

---

# 10. Nginx

## `server`

Virtual server configuration.

## `listen`

Port on which Nginx accepts requests.

## `location`

Path matching and routing block.

## `proxy_pass`

Forwards a request to another service.

## `try_files`

Attempts a physical file and then a fallback such as `index.html`, required for SPA client-side routes.

## Proxy headers

Headers such as `Host`, `X-Forwarded-For`, and `X-Forwarded-Proto` preserve caller and scheme context for upstream services.

---

# 11. HTML and CSS

## HTML attributes used by the portal

- `data-page`: logical page key read from `document.body.dataset.page`.
- `data-root`: relative path from a page shell to repository root.
- `id`: stable DOM lookup target.
- `class`: CSS and behavior grouping.
- `type="module"`: enables ES module loading.
- `meta viewport`: responsive layout behavior.

## CSS custom properties

```css
--color-primary: ...;
```

Design tokens referenced with `var(--color-primary)`.

## Flexbox and Grid

Used for responsive layout. Media queries adapt navigation and content for narrower screens.

## Specificity and cascade

Later or more specific selectors may override earlier declarations. The repository separates tokens, base, layout, components, and pages to reduce accidental cascade coupling.

---

# 12. SQL and EF Core terminology

## Table

Persistent row collection.

## Primary key

Unique row identity, configured with `HasKey`.

## Foreign key

Column linking a child to a parent; `DocumentId` links versions to documents.

## Index

Additional structure that improves lookups or enforces uniqueness.

## Unique index

Prevents duplicate reference, slug, or document-version label combinations.

## Cascade delete

Deleting a document deletes its related versions. This behavior must be considered carefully before adding product deletion endpoints.

## Migration

Ordered code describing schema changes from one version to another.

## Model snapshot

EF Core's latest known database model, used to calculate the next migration.

## Designer file

Generated metadata for a specific migration and its model.

## Change tracking

EF Core records entity changes for update generation. `AsNoTracking` disables it for read-only operations.

## Multiple Active Result Sets

SQL Server connection option allowing multiple active commands on a connection. It is enabled in local/test connection strings.

## Retry on failure

SQL Server provider policy that retries selected transient failures with bounded count and delay. It must not be confused with retrying business operations or non-idempotent HTTP requests.

---

# 13. HTTP terminology

## Method

- `GET`: retrieve.
- `POST`: create or trigger an operation.
- `PUT`: replace a resource representation or assignment set.

## Status codes used

- `200 OK`: successful response with body.
- `201 Created`: resource created, commonly with `Location`.
- `204 No Content`: successful operation without response body.
- `400 Bad Request`: invalid transport/validation input.
- `401 Unauthorized`: authentication missing or invalid.
- `403 Forbidden`: authenticated but not permitted.
- `404 Not Found`: target absent.
- `409 Conflict`: uniqueness or current-state conflict.
- `422 Unprocessable Entity`: request syntax valid but a business rule rejects it.
- `500 Internal Server Error`: unexpected failure.

## Bearer token

```http
Authorization: Bearer <token>
```

The token is presented to the API; it is not a password and should still be protected from disclosure.

## CORS

Browser policy governing cross-origin requests. It is not authentication and does not protect non-browser clients.

## HSTS

Tells browsers to use HTTPS for a host after a trusted HTTPS response.

## Correlation ID

Request identifier propagated through response and logging scope so one operation can be traced.

## RFC 7807 ProblemDetails

Standard JSON error shape with fields such as `title`, `status`, `detail`, `instance`, and extensions like `code`, `errorType`, and `correlationId`.

---

# 14. Architectural terms

## Clean Architecture

Dependency direction points toward business rules. Frameworks are adapters, not the center of the model.

## DDD-style

The repository uses aggregates, entities, value objects, domain events, business vocabulary, and bounded-context thinking. It does not claim full strategic DDD implementation for every capability.

## Aggregate root

The entry point for modifying an aggregate. External code should not mutate child versions independently of document rules.

## Command

Represents an intent to change state.

## Query

Represents an intent to read state without changing business data.

## Handler

Executes one command or query.

## Port

Interface defined inward, such as `IDocumentRepository`.

## Adapter

Outer implementation of a port, such as `DocumentRepository` using EF Core.

## Unit of Work

Commits tracked persistence changes as one save boundary.

## Specification

Object describing reusable query criteria, includes, ordering, tracking, and paging without exposing EF Core to Application.

## Modular monolith

One deployable backend organized into explicit capability boundaries. It avoids distributed-system cost while keeping extraction possible when justified.

## Typed API client

A class exposing domain-oriented methods over HTTP and shared contracts instead of scattered URL strings and JSON parsing in UI pages.

## Composition root

The startup location where concrete implementations are wired to interfaces. `Program.cs` and DI extension methods form the platform composition roots.
