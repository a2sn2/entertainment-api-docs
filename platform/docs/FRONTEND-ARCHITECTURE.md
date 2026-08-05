# Frontend Architecture

## Decision

The platform frontend uses **Blazor WebAssembly** with **MudBlazor** as the official UI system. It follows a feature-based, MVVM-inspired architecture rather than placing HTTP calls and business workflow logic directly inside Razor pages.

The frontend boundary is split into three projects:

```text
apps/
├── EntertainmentDocs.Admin   # authenticated administration experience
├── EntertainmentDocs.Client  # public/read-only documentation experience
└── EntertainmentDocs.Ui      # shared Razor Class Library and design system

src/
└── EntertainmentDocs.Contracts # shared HTTP request/response contracts
```

## Dependency direction

```text
Admin / Client
     │
     ├──> EntertainmentDocs.Ui
     ├──> EntertainmentDocs.Contracts
     └──> Typed API Clients ──HTTP──> ASP.NET Core API
                                      │
                                      └──> Application → Domain → SQL Server
```

The UI never references Infrastructure, Entity Framework Core, or SQL Server. The API remains the only boundary allowed to enforce business operations and authorization.

## Feature structure

Each application is organized by business capability:

```text
Features/
└── Users/
    ├── Pages/
    ├── Components/
    ├── ViewModels or page state
    └── UsersApiClient.cs
```

The initial Admin features are:

- Authentication
- Dashboard
- Documents and publishing
- Users and roles
- API testing reference

The initial Client features are:

- Published documentation catalog
- Search
- Document details

## Presentation responsibilities

A Razor page or component may:

- render state;
- collect user input;
- trigger a feature action;
- show loading, empty, success, and failure feedback;
- enforce navigation visibility for the current role.

A Razor page or component must not:

- execute SQL;
- reference Entity Framework Core;
- contain connection strings;
- construct raw JWT tokens;
- duplicate API request/response models;
- become the source of truth for backend authorization;
- contain reusable cross-feature HTTP behavior.

## Typed API clients

Every backend capability is accessed through a feature-specific client:

```text
AuthenticationApiClient
UsersApiClient
DocumentsApiClient
DocumentationApiClient
```

Typed clients own:

- endpoint URLs;
- JSON serialization;
- authenticated request creation;
- response deserialization;
- HTTP status handling;
- mapping API failures into frontend result objects.

Razor pages depend on these clients rather than using `HttpClient` directly.

## Contracts

`EntertainmentDocs.Contracts` is the source of truth for HTTP payload shapes shared between the API and Blazor applications.

Contract groups:

```text
Authentication/
Documents/
Users/
Common/
```

Contracts are transport models only. Domain entities are not exposed to WebAssembly clients.

## Authentication and authorization

The Admin application uses:

- `IAccessTokenStore` backed by browser `sessionStorage`;
- `JwtAuthenticationStateProvider`;
- `CascadingAuthenticationState`;
- `AuthorizeRouteView`;
- role-aware `AuthorizeView` navigation;
- Bearer-token request creation through `AuthenticatedRequestFactory`.

Frontend authorization controls navigation and visibility. The ASP.NET Core API remains responsible for the real security decision and returns `401` or `403` when access is not allowed.

Current roles:

```text
Administrator
Editor
Reviewer
Reader
```

## MudBlazor design system

MudBlazor is the first choice for controls, layout, forms, navigation, feedback, tables, and dialogs. Custom HTML and CSS are limited to branding, responsive adjustments, code presentation, and behavior not provided by MudBlazor.

The shared UI project currently provides:

- `AppTheme`
- `PageHeader`
- `LoadingState`
- `EmptyState`
- `ErrorState`

Applications provide their own layouts while consuming the same theme and reusable UI states.

## State flow

```text
User action
    ↓
Razor Page / Component
    ↓
Feature state or typed API client
    ↓
HTTP contract
    ↓
ASP.NET Core API
    ↓
Result returned to page
    ↓
MudBlazor feedback and refreshed state
```

Long-running calls expose a busy/loading state and prevent duplicate submissions. Failures are displayed through shared alerts, page error states, or snackbars.

## API and Postman synchronization

The same shared contracts used by Blazor are documented in:

- `docs/POSTMAN-REQUESTS.md`
- `postman/EntertainmentDocs.postman_collection.json`
- `postman/EntertainmentDocs.Local.postman_environment.json`

Changes to a request body require all of the following in the same pull request:

1. update the shared contract;
2. update the API endpoint;
3. update the typed API client;
4. update the Postman request and documentation;
5. update tests when behavior changes.

## Quality rules

- nullable reference types remain enabled;
- warnings are treated as errors;
- all solution projects must build in Release mode;
- unit tests must pass;
- Postman JSON files must parse successfully;
- the SQL Server full-stack smoke test must pass;
- no feature branch is merged while CI is failing.

## Planned extension pattern

New Admin or Client capabilities should be added as self-contained feature folders. Shared visual behavior belongs in `EntertainmentDocs.Ui`; shared HTTP payloads belong in `EntertainmentDocs.Contracts`; backend business rules remain in Domain/Application.
