# Blazor WebAssembly Frontend

The dynamic product has two browser applications and one shared Razor Class Library:

```text
EntertainmentDocs.Admin
EntertainmentDocs.Client
EntertainmentDocs.Ui
```

Both applications use MudBlazor and communicate with the backend exclusively through HTTP contracts.

---

# 1. Frontend dependency direction

```text
Admin / Client
    ├── EntertainmentDocs.Contracts
    ├── EntertainmentDocs.Ui
    └── FoundationKit.Blazor
              │
              ▼
          HttpClient
              │
              ▼
      EntertainmentDocs.Api
```

Frontend projects do not reference:

- Product Domain;
- Product Application;
- Product Infrastructure;
- EF Core;
- SQL Server;
- connection strings.

This keeps WebAssembly payloads and trust boundaries clean.

---

# 2. Shared UI project

## 2.1 `EntertainmentDocs.Ui.csproj`

Uses `Microsoft.NET.Sdk.Razor`, making it a Razor Class Library. It references MudBlazor and Blazor component web APIs.

It owns visual reuse only. It does not own HTTP clients, routes, authorization policies, or product business state.

## 2.2 `_Imports.razor`

Makes common namespaces and MudBlazor types available to every component in the shared project. Razor imports reduce repetitive fully qualified names.

---

## 2.3 `Theme/AppTheme.cs`

`AppTheme` is a static holder with one reusable `MudTheme`.

### Light palette

Defines:

- blue primary;
- teal secondary;
- dark app bar with white text;
- light page background and white surfaces;
- readable drawer and text colors;
- semantic success/warning/error/info colors.

### Dark palette

Defines brighter accent colors and dark background/surface/drawer values with high-contrast text.

### Layout property

```text
DefaultBorderRadius = 10px
```

This gives components a consistent visual radius without custom CSS per control.

The theme is product branding, so it belongs in `EntertainmentDocs.Ui`, not FoundationKit.Blazor.

---

## 2.4 `Components/PageHeader.razor`

Structure:

- outlined `MudPaper` container;
- horizontal stack with title area and optional actions;
- optional icon;
- required title;
- optional subtitle;
- optional `RenderFragment` action slot.

Parameters:

```text
Title      required by editor tooling
Subtitle   optional
Icon       optional
Actions    optional markup fragment
```

Why a component exists: every page receives consistent spacing, typography, icon position, and action alignment.

`EditorRequired` is a compile-time/editor signal; callers should still ensure runtime values are meaningful.

## 2.5 `Components/LoadingState.razor`

Displays:

- outlined paper;
- centered indeterminate progress indicator;
- configurable message defaulting to `Loading...`.

It standardizes asynchronous waiting UI.

## 2.6 `Components/EmptyState.razor`

Displays configurable icon, title, message, and optional actions. Defaults describe an empty inbox-like state.

The optional action fragment can host “Create,” “Clear filter,” or navigation buttons without hard-coding a business action.

## 2.7 `Components/ErrorState.razor`

Displays an outlined error alert with title and message. When `Retry.HasDelegate` is true, it renders a Try Again button.

Using `EventCallback` integrates with Blazor event dispatch and rerendering.

---

# 3. Admin project configuration

## 3.1 Project file

Uses Blazor WebAssembly SDK and references:

- shared UI;
- Contracts;
- FoundationKit.Blazor;
- Blazor authorization;
- WebAssembly runtime/dev server;
- MudBlazor.

No server or database package is present.

## 3.2 `Program.cs`

### Root components

```csharp
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
```

Mounts application into `#app` and allows dynamic title/head content.

### API base URL resolution

1. read `ApiBaseUrl` from app configuration;
2. get application base URI;
3. when setting is blank, resolve `../` relative to app;
4. when absolute, use it directly;
5. otherwise resolve relative setting against app base.

This supports:

- separate localhost ports;
- same-origin gateway subpaths;
- environment-configured external API.

### HttpClient registration

Scoped HttpClient receives resolved base address.

### MudBlazor and authorization

- `AddMudServices()` registers dialogs, snackbars, and other services;
- `AddAuthorizationCore()` enables browser authorization evaluation.

### Authentication services

- `IAccessTokenStore` -> browser session storage;
- custom JWT AuthenticationStateProvider;
- base `AuthenticationStateProvider` -> same custom instance;
- authenticated request factory.

### Feature clients

Registers authentication, users, and documents typed clients as scoped.

### Start

`Build().RunAsync()` downloads/starts the WebAssembly runtime and application.

---

# 4. Admin application root

## `App.razor`

MudBlazor providers are declared once:

```razor
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />
```

They were intentionally moved out of layouts because multiple providers caused runtime duplicate-section exceptions during navigation.

`CascadingAuthenticationState` makes the current principal available.

`Router` scans the Admin assembly for `@page` components.

`AuthorizeRouteView`:

- uses `MainLayout` by default;
- redirects unauthorized routes to login;
- shows shared loading state while authentication is being restored.

`FocusOnNavigate` moves focus to the first `h1`, improving keyboard/screen-reader navigation.

Not-found routes render a shared empty state inside the main layout.

---

# 5. Browser token and principal handling

## 5.1 `Infrastructure/Authentication/AccessTokenStore.cs`

### Interface

```text
GetAsync
SetAsync
ClearAsync
```

Separates authentication state from storage mechanism.

### Browser implementation

Uses JS interop with `sessionStorage` key:

```text
entertainmentdocs.admin.access-token
```

`sessionStorage` persists within the browser tab/session and is cleared when the tab session ends. It is accessible to JavaScript running in the origin, so XSS prevention remains essential.

No refresh token is stored because none is issued.

## 5.2 `JwtClaimsParser.cs`

This parser reads token payload for UI state only.

### `CreatePrincipal`

1. split token on periods;
2. require exactly three parts;
3. Base64URL-decode payload;
4. parse JSON;
5. convert scalar properties to one claim;
6. convert array properties to multiple claims;
7. create `ClaimsIdentity` with authentication type `jwt`;
8. configure name and role claim types;
9. return principal.

Malformed Base64 or JSON returns anonymous principal.

The parser does not verify signature. This is correct only because its output controls browser presentation, while the API independently validates every protected request.

### `IsExpired`

Reads `exp` claim, parses Unix seconds, and treats missing/invalid/past expiry as expired.

### `Decode`

Converts Base64URL alphabet back to Base64, restores required padding, and decodes bytes.

## 5.3 `JwtAuthenticationStateProvider.cs`

### Anonymous principal

A static principal with empty identity avoids allocating one repeatedly.

### Restore state

1. read token;
2. absent -> anonymous;
3. parse principal;
4. if unauthenticated or expired, clear token and return anonymous;
5. otherwise return authenticated state.

### Set authenticated

1. store token;
2. parse principal;
3. call `NotifyAuthenticationStateChanged`.

### Sign out

Clear token and notify with anonymous state.

A browser can forge local claims, but forged state does not bypass API signature/policy validation.

## 5.4 `AuthenticatedRequestFactory.cs`

Creates `HttpRequestMessage` from method, URI, and optional content.

Then reads access token and, when present, sets:

```http
Authorization: Bearer <token>
```

Why a factory: feature clients do not repeat token retrieval/header construction.

A more advanced future implementation could use a delegating handler, but the explicit factory makes protected vs public calls visible.

---

# 6. Admin authentication feature

## 6.1 `AuthenticationApiClient.cs`

Derives from FoundationKit `ApiClientBase`.

### Login flow

1. create POST request to `api/v1/auth/login`;
2. serialize `LoginRequest` with `JsonContent.Create`;
3. call generic `SendAsync<LoginResponse>`;
4. convert 401 to product-friendly `Authentication.InvalidCredentials` message while preserving correlation ID;
5. reject a successful response with missing token as `Authentication.TokenMissing`;
6. store token/update authentication state;
7. return original successful result.

### Logout

Delegates to authentication state provider sign-out.

The typed client owns route and HTTP behavior; the login page owns form state and navigation.

## 6.2 `Features/Authentication/Pages/Login.razor`

Directives:

- route `/login`;
- `EmptyLayout`;
- anonymous access;
- injected authentication client and navigation manager.

### UI

MudPaper card with:

- admin icon/avatar;
- explanatory title;
- conditional error alert;
- MudForm;
- required email/password fields;
- disabled/submitting button state;
- development-only credential hint.

### State

- form model defaults email to local admin;
- form reference;
- validity flag;
- submitting flag;
- nullable error;
- optional `ReturnUrl` supplied from query string.

### `SubmitAsync`

1. require form reference;
2. validate form;
3. stop on invalid;
4. set submitting and clear error;
5. call typed client;
6. show error on failure;
7. validate return URL as local path;
8. navigate with replace;
9. clear submitting in `finally`.

### Open-redirect defense

`IsSafeLocalUrl` requires one leading `/` and rejects `//`. This prevents redirecting credentials/session flow to an external protocol-relative URL.

Development credential hint must not be included in production environment builds without review.

---

# 7. Admin layouts and routing helpers

## 7.1 `Shared/EmptyLayout.razor`

Uses shared theme and centers `Body` in a small container covering viewport height. Intended for login and other pages without admin navigation.

MudBlazor providers are not repeated here.

## 7.2 `Shared/MainLayout.razor`

### App bar

- responsive drawer toggle;
- product title;
- dark/light toggle;
- authenticated user menu;
- sign-out action.

### Drawer

Navigation:

- dashboard for all authenticated users;
- documents for Administrator/Editor/Reviewer;
- users and roles for Administrator;
- API testing for authenticated users.

`AuthorizeView` hides irrelevant links. API policy still decides real access.

### Main content

Uses extra-large container and shared body slot.

### State/actions

- drawer initially open;
- dark mode initially false;
- toggle methods invert booleans;
- sign out clears token and navigates to login.

## 7.3 `Shared/RedirectToLogin.razor`

On initialization:

1. convert current absolute URI to base-relative path;
2. create local return path;
3. URL-encode it;
4. navigate to `/login?returnUrl=...` with replace.

The login page validates the return URL again before using it.

---

# 8. Admin dashboard and API reference

## 8.1 `Pages/Index.razor`

Protected `/` dashboard.

Uses shared PageHeader and MudCard grid for:

- document workflow;
- user/role management, visible only to Administrator;
- API testing.

A foundation-connected panel states that Blazor, API, EF migrations, and SQL Server are configured together. This is informational UI, not a runtime health check.

## 8.2 `Pages/ApiReference.razor`

Protected page showing repository Postman import paths and sample bodies in expansion panels.

It is a convenient in-app reference. The authoritative executable collection remains under `platform/postman`, and detailed guide under `platform/docs/POSTMAN-REQUESTS.md`.

Hard-coded examples must be updated whenever Contracts change.

---

# 9. Admin document feature

## 9.1 `DocumentsApiClient.cs`

Derives from `ApiClientBase` and uses authenticated request factory.

Methods:

- `CreateAsync` -> POST documents with JSON;
- `AddVersionAsync` -> POST child version with JSON;
- `SubmitForReviewAsync` -> bodyless POST;
- `PublishAsync` -> bodyless POST.

Private `PostWithoutBodyAsync` removes duplication for workflow actions.

Routes remain inside the typed client, not the Razor page.

## 9.2 `Pages/Documents.razor`

Route `/documents`, browser role requirement Administrator/Editor/Reviewer.

### Working ID

A top text field stores document GUID. Newly created ID is filled automatically; a reviewer can paste an existing ID because no admin document-list endpoint exists yet.

### Tab 1: create

Visible form for Administrator/Editor:

- reference;
- slug;
- title;
- create draft button.

Reviewer sees warning instead.

### Tab 2: add version

Administrator/Editor form:

- version label;
- multiline content;
- add button.

### Tab 3: review and publish

- submit button visible to Administrator/Editor;
- publish button visible to Administrator/Reviewer.

### Component state

- two local form models;
- form references and validity flags;
- shared busy flag;
- document ID text.

### Create handler

- validate form;
- use typed request contract;
- call API client;
- show error snackbar or save ID and success message.

### Add-version handler

- validate form;
- parse GUID;
- call typed client;
- show status snackbar.

### Workflow handlers

Parse GUID, call corresponding client, and show success/error.

### `TryGetDocumentId`

Uses `Guid.TryParse`, avoiding exception-driven invalid input. Shows warning when invalid.

### `RunAsync`

Sets busy before action and clears in `finally`, preventing duplicate button submissions during calls.

### Current architecture note

The page still contains feature orchestration/state directly in Razor. It is acceptable at current size but future complexity should move into a feature state/facade so page markup does not grow indefinitely. FoundationKit `AsyncState<T>` is available but not yet used here.

---

# 10. Admin user feature

## 10.1 `UsersApiClient.cs`

### List

Sends authenticated GET and deserializes array. Converts successful array result to `IReadOnlyList` result; propagates structured failure.

### Create

POSTs `CreateUserRequest`.

### Replace roles

PUTs full `UpdateUserRolesRequest` to user role route.

The current Users page uses list/create; replacement method is prepared for UI use.

## 10.2 `Pages/Users.razor`

Administrator-only route.

### Header

Includes refresh action disabled while loading.

### User list states

- loading -> LoadingState;
- error -> ErrorState with retry;
- empty -> EmptyState;
- data -> MudTable with name, email, active state, roles, pager.

The table pager is client-side because all users are loaded at once.

### Create form

Fields:

- display name;
- email;
- temporary password;
- role checkboxes.

Reader defaults to selected.

### Initialization/load

`OnInitializedAsync` calls `LoadAsync`.

`LoadAsync` manages loading/error and assigns result list. `finally` guarantees loading reset.

### Create

1. validate form;
2. set creating;
3. build role list from booleans;
4. construct contract;
5. call typed client;
6. show error or success;
7. reset form;
8. reload list;
9. clear creating.

### Reset

Clears fields/role choices, restores Reader default, and resets validation messages.

### Current limitations

- temporary password is displayed/entered in browser and sent over current local HTTP; production requires HTTPS;
- no force-change-password flag is implemented;
- no active-state edit UI;
- role replacement UI is not yet present;
- no server pagination;
- role strings are repeated in UI and should remain synchronized with backend contracts/policy docs.

---

# 11. Client project

## 11.1 Project file

References shared UI, Contracts, FoundationKit.Blazor, Blazor runtime/dev server, and MudBlazor. It does not include authorization package because public pages do not restore an authenticated principal.

## 11.2 `Program.cs`

Same API base resolution pattern as Admin, registers HttpClient, MudBlazor, and `DocumentationApiClient`, then runs.

## 11.3 `App.razor`

Hosts MudBlazor providers once, uses normal `RouteView` rather than AuthorizeRouteView, focuses `h1`, and renders shared not-found state.

---

# 12. Client typed API

## `Services/DocumentationApiClient.cs`

### `ListAsync`

- GET `api/v1/documents`;
- deserialize array;
- expose read-only list result;
- propagate structured error.

### `GetBySlugAsync`

- URL-escape slug using `Uri.EscapeDataString`;
- GET route;
- deserialize details response.

Escaping prevents a slug from injecting additional path/query syntax.

---

# 13. Client pages

## 13.1 `Pages/Index.razor`

Public catalog route `/`.

### Header/search

Shared header plus immediate MudTextField search.

### Render states

- loading;
- API error with retry;
- no filtered results;
- card grid.

### Card content

Reference, status, title, local-time update string, and link to slug route.

### `FilteredDocuments`

When search blank, returns full list. Otherwise filters in memory by case-insensitive title, reference, or slug substring.

This is client-side filtering over the loaded published set, not server full-text search.

### Load

Calls typed client on initialization, assigns error or documents, always clears loading.

## 13.2 `Pages/DocumentDetails.razor`

Route `/documents/{Slug}`.

### Lifecycle

Uses `OnParametersSetAsync`, so navigating to another slug with the same component instance reloads correctly.

### States

Loading, error with retry, or document display.

### Content display

Uses `<pre>` with preserved whitespace and word breaking. The current implementation displays Markdown source text; it does not parse Markdown to HTML.

This avoids immediate HTML injection but does not provide rich Markdown rendering. A future renderer must include a sanitization policy.

---

# 14. Client layout

`Shared/MainLayout.razor` provides:

- shared theme with dark-mode binding;
- app bar brand;
- documentation-home link;
- theme toggle;
- extra-large content container.

No drawer or authentication menu is needed for current public client.

---

# 15. Web root files

Both Admin and Client contain:

- `wwwroot/index.html`: WebAssembly host document, CSS/JS references, favicon, loading/error UI;
- `wwwroot/css/app.css`: app-specific layout/code/error/bootstrap styles;
- `wwwroot/favicon.svg`: product icon;
- `wwwroot/appsettings.json`: deployable API base configuration;
- `wwwroot/appsettings.Development.json`: local API base;
- `Properties/launchSettings.json`: local ports and no auto-browser behavior;
- `_Imports.razor`: common namespaces/components.

## Blazor host error UI

`#blazor-error-ui` is hidden by default CSS and shown by runtime only for unhandled errors. A previous missing/overridden hide rule made the banner appear constantly; CSS must preserve correct behavior.

## Launch ports

```text
Client http 5081 / https 7081
Admin  http 5082 / https 7082
```

`launchBrowser=false` prevents browser lifecycle from terminating the Visual Studio multi-project debug session.

---

# 16. Frontend security model

Browser responsibilities:

- collect input;
- show/hide navigation by roles;
- attach Bearer token;
- clear expired token;
- display server errors;
- prevent unsafe return URL;
- URL-escape route values.

API responsibilities:

- validate signature/lifetime/issuer/audience;
- enforce policies;
- validate business state;
- prevent unauthorized data mutation;
- return authoritative status.

Never trust a role check or disabled button in WebAssembly as security enforcement.

---

# 17. Frontend modification checklist

1. Define or update a shared Contract first when transport changes.
2. Put HTTP details in a typed client.
3. Reuse `ApiClientBase` for response/error behavior.
4. Keep Bearer handling centralized.
5. Represent loading, empty, error, and success explicitly.
6. Disable duplicate submissions.
7. propagate cancellation for long-running/navigation-sensitive calls;
8. use shared UI components for repeated visual states;
9. keep MudBlazor providers once at app root;
10. maintain role-aware UX but test API 403 independently;
11. update Postman examples and Admin API reference;
12. verify both light/dark and responsive layout;
13. avoid rendering untrusted document HTML without sanitization;
14. build both Admin and Client in Release;
15. test direct SPA routes through Nginx `try_files` fallback.
