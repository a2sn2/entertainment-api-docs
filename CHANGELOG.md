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
- `FoundationKit Atlas`, a creative Arabic GitHub Pages portal that documents every Workbench and Athar Blazor route, core package, API surface, document, and operational proof.
- Pages manifest validation that compares documented UI routes with the actual Razor `@page` declarations.
- Detailed Arabic Visual Studio 2026 guide for SQL Server, User Secrets, startup projects, user/admin workflows, and troubleshooting.
- Capability Model v1 with dependency resolution, reusable profiles, project manifests, and a machine-readable capability graph protected by drift checks.
- `FoundationKit.Auditing` as the first extracted opt-in capability package with bounded provider-neutral audit contracts and sensitive-field rejection.
- FoundationKit Composer CLI v1 with strict manifest parsing, capability/profile discovery, dependency explanation, and fail-closed maturity validation.
- Capability Roadmap v1 and a shared Definition of Done for future reusable capabilities.
- `FoundationKit.Security` preview package with explicit trusted-proxy forwarding, reusable rate-limit partition keys, and shared `amr=mfa` authentication-assurance conventions.
- Athar adoption of `FoundationKit.Security` for trusted proxy handling, authentication/write partitioning, and administrator MFA authorization policy.
- `FoundationKit.Identity` reference capability package with reusable account policy, notification ports, security-event vocabulary, and explicit step-up requirements for sensitive account operations.
- Athar adoption of `FoundationKit.Identity` account policy and notification contracts while keeping ASP.NET Core Identity, Arabic product copy, token handling, and EF persistence in the product/adapters.
- `FoundationKit.Authorization` reference capability package with immutable permission descriptors, role-to-permission grants, authorization subjects, permission evaluation, and owner-or-privileged resource access.
- Athar adoption of semantic product permissions in `InitiativeManager`, replacing embedded administrator-role checks in business logic while retaining the existing coarse ASP.NET Core administrator policy.
- `FoundationKit.Workflow` first extraction with deterministic state/trigger transition definitions, fail-closed resolution, immutable transition records, and bounded Auditing integration.
- Athar adoption of a product-owned initiative review workflow for `submitted + approve/reject -> approved/rejected` while retaining aggregate validation, domain events, persistence, and concurrency.
- `FoundationKit.Approvals` reference capability with strict approve/reject decisions, permission-first maker-checker eligibility, Workflow resolution, and bounded approval audit intent.
- Athar adoption of `FoundationKit.Approvals` in the initiative review orchestration while retaining the aggregate self-review invariant, existing product persistence, audit entries, domain events, routes, DTOs, and concurrency behavior.
- `FoundationKit.Notifications` reference capability with bounded channel-neutral message/delivery contracts and sensitive-safe diagnostics.
- Athar account-security delivery split into an Identity/account formatting adapter and a provider-neutral notification boundary, keeping one-time tokens and Arabic product copy in Athar.
- `FoundationKit.Notifications.Smtp` reference provider package with validated SMTP transport options, provider-neutral delivery result mapping, caller-cancellation preservation, and a bounded observer that never receives recipient/body/token/credential/exception-object data.
- Athar adoption of the reusable SMTP provider while retaining product configuration keys, fail-closed production SMTP/TLS validation, secret ownership, and logging policy.

### Changed

- The repository now distinguishes reusable core, architecture Workbench, complete reference products, future real applications, and a dedicated static documentation portal.
- `FoundationKit.sln`, repository verification, CI, documentation, and package versions now include Athar.
- CI publishes and tests both the Workbench and Athar against real SQL Server containers.
- GitHub Pages now deploys the standalone Arabic repository atlas instead of presenting one product client as the entire repository.
- Reusable package output increases to thirteen NuGet packages plus thirteen symbol packages after extracting Auditing, Security, Identity, Authorization, Workflow, Approvals, Notifications, and the SMTP notification provider from product-specific concerns.
- Capability extraction guidance now requires a concrete consumer and a reusable independent boundary before creating a new package; Files/Documents, Jobs, Messaging, Idempotency, and Concurrency remain Planned/ReferenceOnly where current evidence is product-specific or incomplete.

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
