# Repository Map and Ownership Boundaries

This chapter explains where each kind of behavior lives, how to navigate the repository, and which folder owns a proposed change.

---

## 1. Top-level view

```text
entertainment-api-docs/
├── .devcontainer/
├── .github/
├── .vscode/
├── assets/
├── pages/
├── platform/
├── src/
├── 404.html
├── index.html
├── manifest.webmanifest
├── sw.js
└── README.md
```

The top level intentionally combines two deliverables:

- the static documentation portal lives at the repository root because GitHub Pages serves repository-root assets directly;
- the dynamic .NET platform lives under `platform/` so its solution, packages, deployment files, tests, and product code remain isolated from the static site.

This is not an accidental duplicate application. The static portal can run without a backend, while the dynamic platform demonstrates managed documents, users, authorization, database persistence, and reusable product foundations.

---

## 2. Repository-level support folders

### `.devcontainer/`

Contains the optional Development Container/Codespaces definition.

Owner: developer experience / DevOps.

It selects a .NET 8 image, adds Docker-in-Docker and GitHub CLI features, forwards the gateway port, restores the solution after container creation, and installs VS Code extensions. It is not required for local Visual Studio development.

### `.github/workflows/`

Contains GitHub Actions automation.

```text
foundationkit-ci.yml
platform-ci.yml
```

Owner: CI/CD and quality engineering.

- `foundationkit-ci.yml` validates the reusable packages independently.
- `platform-ci.yml` validates the full product and integration topology.

### `.vscode/`

Contains VS Code/Codespaces task definitions. These tasks wrap common stack commands so a developer can start, test, or stop the environment from the editor. Visual Studio users do not need these tasks.

### `.gitignore`

Defines generated, secret-prone, local, build, package, IDE, and database artifacts that Git must not track.

### `.nojekyll`

An empty marker file telling GitHub Pages not to process the static site with Jekyll. This is important for paths and files that Jekyll might otherwise ignore or transform.

---

## 3. Static portal root

```text
index.html
404.html
manifest.webmanifest
sw.js
assets/css/
pages/
src/
```

### `index.html`

The main HTML document. It declares page metadata, loads CSS, creates the page root, identifies the current logical page through `data-page`, and loads `src/presentation/main.js` as an ES module.

### `404.html`

GitHub Pages fallback. Its minimal form routes a missing static request back to the portal entry behavior rather than exposing a server error page.

### `manifest.webmanifest`

PWA metadata such as application name, start URL, display mode, theme, and background color.

### `sw.js`

Service worker for static caching. It is separate from the dynamic Blazor applications.

### `assets/css/`

```text
tokens.css      design values and CSS custom properties
base.css        global reset, typography, and element defaults
layout.css      page shell, sidebar, header, and responsive layout
components.css  reusable component presentation
pages.css       page-specific presentation
```

The division prevents all styles from becoming one unowned stylesheet.

### `pages/`

Contains one small HTML shell per logical documentation page. Each shell sets `data-page` and the correct relative root so the shared JavaScript entry point can render the appropriate page.

### root `src/`

This folder belongs to the static portal, not to the .NET product.

```text
src/
├── domain/
├── application/
├── infrastructure/
└── presentation/
```

The naming mirrors Clean Architecture concepts, but the contents are static documentation data and browser behavior rather than server-side domain entities.

---

## 4. Static portal layer ownership

### `src/domain/`

```text
api-contracts.js
 documentation-model.js
purchase-flow.js
quality-data.js
```

Owns stable documentation facts:

- endpoint definitions;
- observed external contract spelling;
- request/response examples;
- purchase sequence;
- identifier meanings;
- tested/observed/pending quality data;
- governance metadata;
- limitations and open questions.

A visual component must not become the source of truth for these facts.

### `src/application/use-cases/`

```text
build-purchase-requests.js
filter-test-scenarios.js
resolve-error-action.js
search-documentation.js
```

Owns browser-independent operations over domain data:

- normalize and build request examples;
- validate required fields by operation mode;
- filter test scenarios;
- map an error selection to recommended action;
- construct and search a documentation index.

### `src/infrastructure/`

```text
repositories/static-documentation-repository.js
storage/browser-preferences.js
```

Owns adapters:

- the repository presents domain arrays and objects through query methods;
- browser preference storage adapts `localStorage` or equivalent browser facilities.

### `src/presentation/`

```text
main.js
components/
pages/
```

Owns rendering, navigation, DOM events, accessibility behavior, command palette, tables, code blocks, and page initialization.

---

## 5. Dynamic platform root

```text
platform/
├── .config/
├── apps/
├── core/
├── deploy/
├── docs/
├── postman/
├── scripts/
├── src/
├── tests/
├── Directory.Build.props
├── Directory.Packages.props
├── EntertainmentDocs.sln
└── README.md
```

### `.config/`

Contains the local .NET tool manifest. The repository pins `dotnet-ef` so migrations use a predictable tool version after `dotnet tool restore`.

### `Directory.Build.props`

Applies common MSBuild properties to descendant projects:

- target framework;
- nullable reference types;
- implicit usings;
- warnings as errors;
- globalization behavior.

### `Directory.Packages.props`

Central Package Management file. Package versions are declared once and project files reference package names without repeating versions.

### `EntertainmentDocs.sln`

Visual Studio solution index. It groups the 15 projects and maps Debug/Release configurations. It does not contain application logic.

---

## 6. Reusable core ownership

```text
platform/core/
├── FoundationKit.Domain/
├── FoundationKit.Application/
├── FoundationKit.Infrastructure/
├── FoundationKit.WebApi/
├── FoundationKit.Blazor/
├── Directory.Build.props
└── README.md
```

### `FoundationKit.Domain`

Owns framework-independent model primitives. It must remain usable by any product and must not know SQL Server, HTTP, ASP.NET Core, EF Core, Blazor, or EntertainmentDocs.

### `FoundationKit.Application`

Owns reusable use-case contracts and result vocabulary. It may depend inward on FoundationKit.Domain, but not outward on Infrastructure or WebApi.

### `FoundationKit.Infrastructure`

Owns reusable EF Core and event-dispatch adapters. It is allowed to reference base EF Core but must not select a relational provider or web host.

### `FoundationKit.WebApi`

Owns ASP.NET Core-specific transport conventions such as correlation IDs, security headers, ProblemDetails, and result mapping.

### `FoundationKit.Blazor`

Owns browser-side API result handling and asynchronous state. It does not own product routes, product contracts, or product pages.

### `core/Directory.Build.props`

Adds package metadata and imports the parent platform build settings. It ensures the core projects remain packable without losing the common `net8.0`, nullable, or warnings-as-errors configuration.

---

## 7. Product backend ownership

```text
platform/src/
├── EntertainmentDocs.Domain/
├── EntertainmentDocs.Application/
├── EntertainmentDocs.Contracts/
├── EntertainmentDocs.Infrastructure/
└── EntertainmentDocs.Api/
```

### `EntertainmentDocs.Domain`

Owns product meaning and invariants:

- document aggregate;
- document version entity;
- document status;
- GUID-specialized product base classes.

It must not contain EF attributes, route strings, JWT code, SQL, UI concerns, or dependency injection.

### `EntertainmentDocs.Application`

Owns use cases and ports:

- commands and queries;
- handlers;
- document errors;
- application DTOs;
- repository interface;
- current-user, clock, and unit-of-work aliases;
- handler registration.

It coordinates but does not implement SQL Server or HTTP.

### `EntertainmentDocs.Contracts`

Owns public transport shapes shared by API and Blazor clients:

- login request/response;
- document requests/responses;
- user requests/responses;
- legacy/simple error response.

Contracts are not domain entities and must not expose EF navigation properties or internal methods.

### `EntertainmentDocs.Infrastructure`

Owns technical implementation chosen by the product:

- SQL Server provider;
- `AppDbContext`;
- EF configurations and migrations;
- concrete document repository;
- ASP.NET Core Identity user and roles;
- token generation;
- seed behavior;
- system clock;
- Infrastructure DI.

### `EntertainmentDocs.Api`

Owns the HTTP host and composition root:

- service registration;
- middleware order;
- authentication and authorization configuration;
- rate limiting and CORS;
- endpoint mapping;
- development Swagger;
- startup migration choice;
- bootstrap seed invocation;
- HTTP-to-application adapters.

---

## 8. Product frontend ownership

```text
platform/apps/
├── EntertainmentDocs.Admin/
├── EntertainmentDocs.Client/
└── EntertainmentDocs.Ui/
```

### `EntertainmentDocs.Admin`

Authenticated administration application. It owns:

- browser authentication state;
- token storage abstraction and sessionStorage adapter;
- authenticated request factory;
- authentication, document, and user typed clients;
- administration pages;
- role-aware navigation;
- Admin layout and local app configuration.

### `EntertainmentDocs.Client`

Public read-only application. It owns:

- published-document typed client;
- catalog/search page;
- detail page;
- public layout and local app configuration.

### `EntertainmentDocs.Ui`

Shared Razor Class Library. It owns visual behavior reused by both apps:

- theme;
- page header;
- loading state;
- empty state;
- error state.

It must not know API endpoints or business rules.

---

## 9. Test ownership

```text
platform/tests/
├── FoundationKit.Tests/
└── EntertainmentDocs.Domain.Tests/
```

### `FoundationKit.Tests`

Owns reusable primitive tests and architecture dependency tests. A failure here may indicate that the core is no longer safe to reuse.

### `EntertainmentDocs.Domain.Tests`

Owns product-domain behavior tests. Current tests protect the review-before-publish invariant.

The Docker smoke test under `platform/scripts/` complements these unit tests by validating the deployed topology and HTTP workflow.

---

## 10. Operations ownership

### `platform/deploy/`

Owns container and reverse-proxy definitions:

- API Docker image;
- reusable Blazor publish/Nginx image;
- static portal image;
- test and deployment Compose definitions;
- Nginx single-origin gateway;
- Nginx SPA fallback;
- environment-variable example.

### `platform/scripts/`

Owns executable developer/CI workflows:

- prepare local SQL Server;
- open local services after Visual Studio startup;
- pack FoundationKit on Windows or Unix;
- start/stop test stack;
- run the end-to-end smoke test.

### `platform/postman/`

Owns importable REST-client artifacts. The collection and environment are executable documentation and must stay synchronized with Contracts and endpoints.

### `platform/docs/`

Owns focused architecture and operation guides. These files are shorter operational references; the current `docs/repository-reference/` directory is the deep codebase reference.

---

## 11. Change-routing guide

| Requested change | Primary owner | Usually also update |
|---|---|---|
| Change a document state rule | Product Domain | Domain tests, handler behavior, docs |
| Add a new use case | Product Application | API, contracts, frontend, Postman, tests |
| Add a reusable result type | FoundationKit.Application | core tests, package version, consumers |
| Change SQL column or index | Product Infrastructure | migration, snapshot, integration tests |
| Change HTTP body | Product Contracts | endpoint, clients, Postman, docs, tests |
| Change JWT validation | Product API/Infrastructure | auth tests, deployment settings, security docs |
| Add reusable API middleware | FoundationKit.WebApi | core docs/tests, product pipeline |
| Change Admin page behavior | Admin feature | typed client/contract when transport changes |
| Change shared visual component | EntertainmentDocs.Ui | both app builds, accessibility review |
| Change static API facts | root `src/domain` | page rendering/search only when structure changes |
| Change Docker routing | `platform/deploy` | smoke test, CI, operational docs |
| Add FoundationKit package dependency | core project + central package file | architecture tests, pack workflow |

---

## 12. Names that can be confusing

### Two `src` directories

- `/src` is JavaScript for the static portal.
- `/platform/src` is the .NET product backend.

### Two documentation systems

- the static portal documents an external Entertainment Services API;
- EntertainmentDocs is a dynamic platform capable of storing and publishing versioned documents.

### Two result families

- `FoundationKit.Application.Results.Result` represents server-side use-case outcomes;
- `FoundationKit.Blazor.Api.ApiResult` represents browser-side HTTP outcomes.

### Product and core abstractions with the same short name

Product interfaces such as `EntertainmentDocs.Application.Abstractions.IClock` extend FoundationKit equivalents. The product aliases give the product a stable local vocabulary and DI registration point while preserving the reusable contract.

---

## 13. Repository navigation principle

Start from behavior, not from framework:

1. locate the business capability;
2. read the Domain aggregate or policy;
3. read the Application command/query and handler;
4. read the port used by the handler;
5. read the Infrastructure implementation;
6. read the API endpoint and Contract;
7. read the typed frontend client and page;
8. read Postman and tests;
9. verify the deployment and CI path if the change affects runtime topology.

This sequence follows the dependency direction and prevents a reader from mistaking an adapter detail for the business source of truth.
