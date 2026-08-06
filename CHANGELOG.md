# Changelog

All notable repository and package changes are documented here.

## [Unreleased]

### Added

- Complete Arabic `Athar` production-reference product under `examples/Athar`.
- Six explicit Athar projects: Domain, Application, Infrastructure, Contracts, API, and Blazor Client.
- Arabic user experience for registration, login, initiative creation, status tracking, and administration.
- ASP.NET Core Identity, User/Administrator roles, secure cookie authentication, password policy, and lockout.
- Anti-CSRF endpoint and validation filter for all write operations.
- Authentication and write rate-limiting policies.
- Idempotent initiative creation using a unique `ClientRequestId` per owner.
- Optimistic concurrency through SQL Server `rowversion`.
- Initiative review records and audit entries.
- SQL Server Identity and product schemas with a complete initial migration and model snapshot.
- Live and ready health endpoints, startup migration retry, Swagger, Postman, Docker, and end-to-end CI smoke testing.
- Generic `EntityDto<TId>` and `AuditedEntityDto<TId>` application models.
- Blazor-oriented `ViewModelBase` and `ListViewModel<T>` for MVVM-style state ownership.
- Arabic production-readiness gate and new-project guide.
- Reserved `apps/` boundary for future real products.

### Changed

- The repository now distinguishes reusable core, architecture Workbench, complete reference products, and future real applications.
- `FoundationKit.sln`, repository verification, CI, documentation, and package versions now include Athar.
- CI publishes and tests both the Workbench and Athar against real SQL Server containers.

## [0.1.0] - 2026-08-06

### Added

- Five reusable FoundationKit packages for Domain, Application, Infrastructure, WebApi, and Blazor.
- Provider-neutral EF Core persistence adapters and in-process domain-event dispatch.
- Result mapping, correlation IDs, security headers, typed API results, and asynchronous UI state.
- Package, symbol-package, architecture-test, documentation, and CI foundations.
- Local SQL Server Workbench consumer, canonical capability catalog, generated capability documentation, Docker launchers, and persistence smoke testing.
- Blazor WebAssembly + MudBlazor client, shared API contracts, Swagger, Postman, and GitHub Pages deployment.
- Explicit User Full Stack and Admin Full Stack reference paths connected through a SQL-backed review workflow.

### Fixed

- Synchronous and asynchronous post-save domain-event interception.
- Event clearing before handler dispatch to prevent accidental redispatch after handler failure.
- Invalid JSON handling for successful typed HTTP responses.
