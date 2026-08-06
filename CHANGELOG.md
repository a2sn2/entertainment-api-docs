# Changelog

All notable repository and package changes are documented here.

## [Unreleased]

### Added

- Explicit User Full Stack reference path from SQL Server and domain logic through contracts, API, typed client, Blazor, and user UI/UX.
- Explicit Admin Full Stack reference path from SQL-backed work queue and review audit through domain transition, contracts, API, typed client, Blazor, and admin UI/UX.
- `CreateUserRequest`, `UserRequestResponse`, `AdminReviewRequest`, `AdminQueueItemResponse`, and `AdminReviewResponse` contracts grouped by audience.
- `CreateUserRequestUseCase` and `ReviewUserRequestUseCase` application flows.
- Dedicated `/api/user` and `/api/admin` endpoint groups.
- `/user` and `/admin` MudBlazor portals plus an architecture landing page.
- `AdminReview` aggregate, review audit persistence, and `BuildBriefReviewed` domain event.
- Request lifecycle states: submitted, approved, and rejected.
- `AdminReviews` table and request-status migration.
- `docs/DUAL-FULL-STACK.md` as the first-minute architecture guide.
- Postman folders and CI smoke coverage for the full user-create → admin-review → user-status workflow.
- Temporary per-run SQL Server test credentials in CI instead of a committed fixed test value.

### Changed

- Reframed the Workbench from one generic build-brief flow into two complete, connected vertical slices.
- Split transport contracts into Shared, User, Admin, and Workflow namespaces.
- Updated README, architecture, operations, navigation, repository verification, Swagger descriptions, tests, and CI around the two-stack model.
- Admin review now inserts an audit record and changes the linked user-request status through one unit of work.
- GitHub Pages now presents both portal experiences in explicit non-persistent demo mode.

### Removed

- Ambiguous `BuildBriefRequest` and `BuildBriefResponse` transport contracts that did not identify whether behavior belonged to the user or admin stack.

## [0.1.0] - 2026-08-06

### Added

- Five reusable FoundationKit packages for Domain, Application, Infrastructure, WebApi, and Blazor.
- Provider-neutral EF Core persistence adapters and in-process domain-event dispatch.
- Result mapping, correlation IDs, security headers, typed API results, and asynchronous UI state.
- Package, symbol-package, architecture-test, documentation, and CI foundations.
- Local SQL Server Workbench consumer, canonical capability catalog, generated capability documentation, Docker launchers, and persistence smoke testing.
- Blazor WebAssembly + MudBlazor client, shared API contracts, Swagger, Postman, and GitHub Pages deployment.

### Fixed

- Synchronous and asynchronous post-save domain-event interception.
- Event clearing before handler dispatch to prevent accidental redispatch after handler failure.
- Invalid JSON handling for successful typed HTTP responses.
