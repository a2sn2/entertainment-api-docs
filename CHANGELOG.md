# Changelog

All notable repository and package changes are documented here.

## [Unreleased]

### Added

- Official `FoundationKit.Workbench.Client` Blazor WebAssembly project using Razor Components and MudBlazor.
- Official `FoundationKit.Workbench.Contracts` project containing shared routes, requests, responses, runtime, health, and catalog contracts.
- Typed `WorkbenchApiClient` built on the reusable `FoundationKit.Blazor.ApiClientBase`.
- Swagger/OpenAPI documentation for the Workbench API.
- Postman collection using the same `BuildBriefRequest` JSON contract consumed by Blazor.
- Hosted Blazor WebAssembly delivery from the ASP.NET Core Workbench API.
- GitHub Pages workflow that publishes the Blazor client itself in explicit demo mode.
- Docker and CI verification that the hosted Blazor client, Swagger document, API, migrations, and SQL Server persistence operate together.

### Changed

- Renamed the executable Workbench project to `FoundationKit.Workbench.Api` while preserving its product domain, application logic, EF Core configuration, and migrations.
- Replaced the handwritten HTML/CSS/JavaScript frontend with Blazor WebAssembly and MudBlazor.
- Moved API transport models out of the API application layer into the shared Contracts project.
- Updated the solution, tests, Docker image, repository verification, documentation, and local Visual Studio instructions for the new architecture.
- GitHub Pages now publishes one frontend implementation instead of maintaining a separate JavaScript UI.

### Removed

- Legacy `site/` static Workbench implementation.
- API-local `BuildBriefRequest` and `BuildBriefResponse` duplicates.

## [0.1.0] - 2026-08-06

### Added

- Five reusable FoundationKit packages for Domain, Application, Infrastructure, WebApi, and Blazor.
- Provider-neutral EF Core persistence adapters and in-process domain-event dispatch.
- Result mapping, correlation IDs, security headers, typed API results, and asynchronous UI state.
- Package, symbol-package, architecture-test, documentation, and CI foundations.
- Local SQL Server Workbench consumer, canonical capability catalog, generated capability documentation, Docker launchers, and persistence smoke testing.

### Fixed

- Synchronous and asynchronous post-save domain-event interception.
- Event clearing before handler dispatch to prevent accidental redispatch after handler failure.
- Invalid JSON handling for successful typed HTTP responses.
