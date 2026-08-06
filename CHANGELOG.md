# Changelog

All notable changes to FoundationKit are documented here.

## [Unreleased]

### Changed

- Focused the repository exclusively on the five reusable FoundationKit packages.
- Moved package projects to the root `src` directory and tests to `tests`.
- Replaced product-oriented workflows with one core build, test, verification, and packaging workflow.
- Defined the in-process domain-event delivery contract.
- Added synchronous EF Core save interception.
- Converted successful responses with invalid JSON into typed Blazor API failures.

### Removed

- Product-specific applications, contracts, database schema, migrations, deployment files, API collections, static showcase assets, and historical product documentation.

## [0.1.0]

- Initial reusable package baseline.
