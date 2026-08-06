# Changelog

All notable repository and package changes are documented here.

## [Unreleased]

### Added

- Local `FoundationKit.Workbench` ASP.NET Core sample that consumes the reusable packages and persists project briefs in SQL Server.
- Workbench-owned EF Core migration for the `BuildBriefs` schema.
- Docker Compose topology and one-command PowerShell/Bash launchers that open the Workbench automatically.
- Creative shared discovery UI for implemented capabilities, project ideas, adoption guidance, and project-brief generation.
- Static GitHub Pages demo that uses the same UI and clearly operates without backend execution or persistence.
- Canonical `catalog/foundationkit.catalog.json` source for packages, implemented capabilities, ideas, adoption steps, and contact metadata.
- Catalog validation and generated `docs/FEATURES.md` workflow.
- Workbench unit tests and a CI smoke test against a real SQL Server container.

### Changed

- Repository boundary verification now permits explicit `samples`, `site`, `catalog`, `tools`, and `deploy` roles while still preventing provider coupling or migrations inside reusable packages.
- README, architecture, package, contributing, and security documentation now distinguish core packages, the local Workbench, and the static Pages demo.

## [0.1.0] - 2026-08-06

### Added

- Five reusable FoundationKit packages for Domain, Application, Infrastructure, WebApi, and Blazor.
- Provider-neutral EF Core persistence adapters and in-process domain-event dispatch.
- Result mapping, correlation IDs, security headers, typed API results, and asynchronous UI state.
- Package, symbol-package, architecture-test, documentation, and CI foundations.

### Fixed

- Synchronous and asynchronous post-save domain-event interception.
- Event clearing before handler dispatch to prevent accidental redispatch after handler failure.
- Invalid JSON handling for successful typed HTTP responses.
