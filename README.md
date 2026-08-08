# FoundationKit for .NET

**FoundationKit** is a composable .NET foundation for building business systems without turning the reusable core into one giant application.

The repository separates reusable foundation code from its consumers deliberately:

```text
Reusable FoundationKit packages
        ↓
Optional capabilities and provider adapters
        ↓
Consumers
├── Workbench — executable architecture/reference consumer
├── Athar — complete Arabic reference product
└── Madar — first real product under apps/ with a working v0.1 vertical slice
```

The current reusable output is **17 NuGet packages + 17 symbol packages**. Package existence does not mean every capability is `Stable`; maturity is tracked explicitly in the capability model.

> FoundationKit has a verified automated repository baseline for the documented scope. Production approval, organizational compliance, provider operations, and formal certification remain deployment- and organization-specific.

---

## Repository map

```text
foundationkit-dotnet/
├─ src/                         reusable FoundationKit packages
├─ samples/                     FoundationKit Workbench
├─ examples/Athar/              complete Arabic reference product
├─ apps/Madar/                  first real product, v0.1 in development
├─ tools/
│  ├─ FoundationKit.CatalogGenerator
│  └─ FoundationKit.Composer
├─ tests/                       core, Workbench, Athar, and Madar tests
├─ catalog/                     human and machine capability catalogs
├─ docs/                        architecture, capability, security, and runbooks
├─ deploy/                      Docker Compose definitions
├─ postman/                     API collections
├─ scripts/                     verification, packaging, smoke, and security scripts
├─ site/                        FoundationKit Atlas GitHub Pages portal
├─ FoundationKit.sln
└─ foundationkit.ps1            unified Windows repository manager
```

---

## The 17 reusable packages

The five base packages remain the architectural foundation. The remaining packages are opt-in capabilities/adapters and must be selected deliberately by a consuming product.

| Package | Purpose |
|---|---|
| `FoundationKit.Domain` | entities, aggregate roots, value objects, domain events |
| `FoundationKit.Application` | use-case contracts, results, validation, pagination, persistence ports, capability model |
| `FoundationKit.Infrastructure` | provider-neutral EF Core adapters and in-process domain-event dispatch |
| `FoundationKit.WebApi` | HTTP result mapping, Problem Details, correlation, baseline response headers |
| `FoundationKit.Blazor` | typed API results, resilient response parsing, reusable UI state |
| `FoundationKit.Auditing` | provider-neutral audit recording contracts |
| `FoundationKit.Security` | trusted-proxy, rate-limit partition, and MFA-assurance conventions |
| `FoundationKit.Identity` | account policy, notification ports, and sensitive-operation step-up requirements |
| `FoundationKit.Authorization` | permission, role-grant, subject, and ownership evaluation primitives |
| `FoundationKit.Workflow` | deterministic state/trigger transition definitions |
| `FoundationKit.Approvals` | narrow approve/reject + permission + maker-checker composition |
| `FoundationKit.Notifications` | channel-neutral message and delivery contracts |
| `FoundationKit.Notifications.Smtp` | narrow SMTP transport adapter |
| `FoundationKit.Settings` | bounded hierarchical setting resolution |
| `FoundationKit.FeatureManagement` | settings-backed Boolean feature decisions |
| `FoundationKit.Localization` | culture metadata, RTL/LTR, fallback, opaque time-zone identity |
| `FoundationKit.Caching` | bounded byte-cache contracts and an in-memory reference provider |

Canonical package contracts are documented in [`docs/PACKAGES.md`](docs/PACKAGES.md). The human-readable implemented surface is generated into [`docs/FEATURES.md`](docs/FEATURES.md).

### Capability maturity

Capability maturity is not inferred from the presence of a project or class. The machine contract uses:

- `Stable`
- `Preview`
- `ReferenceOnly`
- `Planned`

The source of truth is:

```text
src/FoundationKit.Application/Capabilities/CapabilityModel.cs
```

and its generated machine-readable form:

```text
catalog/foundationkit.capabilities.json
```

The lifecycle and stop rules are documented in:

- [`docs/CAPABILITY-MODEL-V1.md`](docs/CAPABILITY-MODEL-V1.md)
- [`docs/CAPABILITY-ROADMAP-V1.md`](docs/CAPABILITY-ROADMAP-V1.md)
- [`docs/CAPABILITY-EXTRACTION-STATUS.md`](docs/CAPABILITY-EXTRACTION-STATUS.md)

---

## Dependency rule

The reusable foundation keeps dependency direction explicit:

```text
Domain
  ↑
Application
  ↑
Infrastructure

Application / Domain
  ↑
WebApi or Blazor consumers
```

Optional capabilities compose around these boundaries. A lower-level package must not gain a dependency merely because a higher-level feature needs convenience.

Provider decisions also stay separate:

```text
Capability contract ≠ provider selection
```

Examples:

- `FoundationKit.Caching` does not force Redis.
- `FoundationKit.Notifications` does not force SMTP.
- `FoundationKit.Localization` does not force a translation store or OS-specific time-zone mapping.
- `FoundationKit.Settings` is not a secret store.
- `FoundationKit.Infrastructure` does not own a product DbContext or migrations.

---

## Workbench

`FoundationKit.Workbench` is the executable architecture/reference consumer. It demonstrates two connected vertical slices:

```text
User Full Stack
SQL Server → Domain → Use Case → Contracts → API → Blazor UI

Admin Full Stack
SQL Server → Domain → Use Case → Contracts → API → Blazor UI
```

They meet through a shared request lifecycle:

```text
submitted → approved | rejected
```

Workbench also provides real consumer evidence for reusable platform capabilities:

- Settings
- Feature Management
- Localization
- Caching

Caching, for example, is exercised on the existing embedded capability-catalog read path rather than through a synthetic cache-only endpoint.

Read [`docs/WORKBENCH.md`](docs/WORKBENCH.md) and [`docs/DUAL-FULL-STACK.md`](docs/DUAL-FULL-STACK.md).

---

## Athar

`examples/Athar` is a complete Arabic reference product rather than another generic layer.

It demonstrates real product ownership of concerns such as:

- ASP.NET Core Identity and account lifecycle;
- authentication cookies and anti-CSRF;
- authorization and product permissions;
- MFA/security-sensitive operations;
- initiatives and administrative review;
- maker-checker behavior;
- SQL Server migrations and persistence;
- idempotency and optimistic concurrency reference behavior;
- audit records;
- notification and SMTP-provider consumption;
- Arabic Blazor UX;
- Docker, health/readiness, backup/restore, and E2E verification.

Athar keeps product rules, Arabic copy, database schema, migrations, secrets, and deployment configuration outside the reusable packages.

Read [`examples/Athar/README.md`](examples/Athar/README.md).

---

## Madar

`apps/Madar` is the first real product developed under the repository's `apps/` boundary. It is an operational case-management and orchestration product intended to validate FoundationKit against a product domain that is materially different from Athar.

Madar now has a repository-verified first vertical slice across Identity, authorization, SQL Server, API, Blazor, and auditing:

```text
Authenticate
  ↓
Create Case
  ↓
Persist + List / View
  ↓
Assign Operator
  ↓
new → assigned → in-progress → resolved → closed
  ↓
Persisted Audit Timeline
```

The product reuses FoundationKit Domain/Application/Infrastructure/WebApi/Blazor primitives together with Security, Authorization, Auditing, and Workflow contracts while keeping its ASP.NET Core Identity model, permissions, SQL schema/migrations, case queries, API endpoints, audit sink, Docker topology, and Arabic UI inside `apps/Madar`.

Pull-request CI publishes Madar and verifies a real SQL Server workflow covering anonymous access rejection, anti-CSRF login, case creation, assignment, operator-scoped visibility, progression, resolution, supervisor/administrator close, and persisted audit history.

This v0.1 slice does **not** claim SLA/escalation, configurable workflow design, files/documents, advanced search/reporting, external channels, organization hierarchy, multi-tenancy, background jobs, or production deployment approval.

Read [`apps/Madar/README.md`](apps/Madar/README.md) and track the initial product slice in GitHub issue #71.

---

## FoundationKit Composer v1

The current Composer is **real reference tooling**, but it does **not** generate a project yet.

Supported commands:

```powershell
dotnet run --project tools/FoundationKit.Composer -- capabilities
dotnet run --project tools/FoundationKit.Composer -- profiles
dotnet run --project tools/FoundationKit.Composer -- validate path/to/manifest.json
dotnet run --project tools/FoundationKit.Composer -- validate path/to/manifest.json --require-stable
dotnet run --project tools/FoundationKit.Composer -- explain path/to/manifest.json
```

Current responsibilities:

- capability/profile discovery;
- strict project-manifest parsing;
- composition validation;
- fail-closed maturity validation with `--require-stable`;
- dependency explanation.

Not implemented yet:

```text
foundationkit new
interactive project generation
deterministic project scaffolding
provider wiring generation
visual Workbench composer
```

Read [`docs/COMPOSER-CLI-V1.md`](docs/COMPOSER-CLI-V1.md).

---

## Windows unified manager

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 doctor
```

Useful commands:

```powershell
.\foundationkit.ps1 start -Target Athar -Mode Auto
.\foundationkit.ps1 start -Target Workbench -Mode Auto
.\foundationkit.ps1 start -Target All -Mode Auto
.\foundationkit.ps1 status -Target All
.\foundationkit.ps1 logs -Target All
.\foundationkit.ps1 stop -Target All
.\foundationkit.ps1 verify
.\foundationkit.ps1 pack
.\foundationkit.ps1 production-check
```

`doctor` checks the required commands, availability of a .NET 8 SDK, visible local SQL Server services on Windows, the main local ports, Git state, and running application health where available.

Workbench and Athar local credential/state files live under ignored `.local/` paths. The Windows manager restricts credential files to the current Windows account and refuses to continue if the ACL cannot be applied.

`pack` delegates to the canonical `scripts/pack.ps1` path. Package discovery/count validation therefore has one source of truth rather than a second hard-coded list in the manager.

`Auto` uses Docker when Docker Desktop is ready and otherwise uses local .NET/SQL Server where supported.

For the exact first-run sequence, SQL Server instance overrides, port map, and failure diagnostics, read [`docs/LOCAL-RUN-WINDOWS-AR.md`](docs/LOCAL-RUN-WINDOWS-AR.md). Madar currently uses its dedicated development/test Compose path documented in [`apps/Madar/README.md`](apps/Madar/README.md); unified-manager Madar support is not claimed yet.

---

## Build, test, and package

### Build

```bash
dotnet restore FoundationKit.sln
dotnet build FoundationKit.sln --configuration Release --no-restore
```

### Test

```bash
dotnet test FoundationKit.sln --configuration Release --no-build
```

### Verify generated capability metadata

```bash
dotnet run \
  --project tools/FoundationKit.CatalogGenerator \
  --configuration Release \
  --no-build \
  -- --check
```

### Package all reusable projects

Linux/macOS:

```bash
./scripts/pack.sh Release artifacts/packages
```

Windows:

```powershell
.\scripts\pack.ps1 -Configuration Release -Output artifacts/packages
```

Current invariant:

```text
17 .nupkg
17 .snupkg
```

The scripts discover `src/FoundationKit.*/*.csproj` and fail if the expected reusable package set drifts.

---

## SQL Server and migrations

FoundationKit does not centralize product schemas in the reusable packages.

The consuming application owns:

- its `DbContext`;
- relational provider selection;
- entity configurations;
- migrations;
- migration review;
- transactions;
- concurrency policy;
- production migration execution policy.

Workbench migrations live under:

```text
samples/FoundationKit.Workbench/Infrastructure/Migrations/
```

Athar migrations live under its product infrastructure project. Madar migrations now live under:

```text
apps/Madar/Madar.Infrastructure/Migrations/
```

**EF migrations are the schema source of truth.** Documentation must not be treated as a substitute for migration/model inspection.

---

## Catalogs: two different contracts

FoundationKit intentionally has two catalogs with different responsibilities.

### Human implemented-package catalog

```text
catalog/foundationkit.catalog.json
```

It drives the human `FEATURES.md` reference and the embedded Workbench `/api/catalog` surface. It lists implemented public behavior only.

### Composition capability graph

```text
catalog/foundationkit.capabilities.json
```

It is generated from the compiled Capability Model and carries:

- capability IDs;
- dependencies;
- kinds;
- maturity;
- composition profiles.

Do not infer `Stable` from the human catalog; maturity belongs to the composition capability graph.

---

## Automated verification

Pull-request CI verifies the repository as one system, including applicable stages such as:

- tracked-source secret scanning;
- tracked-repository hygiene checks that reject local/generated/sensitive artifacts;
- repository boundary checks;
- JSON and Atlas validation;
- container hardening checks for repository-owned application containers;
- NuGet vulnerability audit;
- CycloneDX dependency SBOM generation;
- Release build with analyzers;
- generated capability/catalog drift checks;
- unit and architecture tests, including Madar Domain/Application behavior;
- Workbench, Athar, and Madar publish;
- all reusable NuGet + symbol packages;
- artifact SHA-256 evidence including Madar publish output;
- Workbench SQL Server workflow;
- Athar readiness, non-root, Arabic/API surface, E2E workflow, and isolated backup/restore;
- Madar non-root runtime, Blazor/API surface, SQL Server migration, authentication/authorization, case lifecycle, and audit-timeline E2E workflow;
- Trivy repository/container scanning;
- black-box negative security tests;
- CodeQL for C# and JavaScript/TypeScript.

Exact evidence belongs to the pull request/head that produced it. A green historical run is not proof for a newer security- or behavior-relevant head.

---

## Security and production boundary

Repository automation can verify code and test evidence; it cannot invent deployment or organizational controls.

FoundationKit does **not** claim by repository existence alone:

- Production Approval;
- ISO/IEC 27001 certification;
- independent Segregation-of-Duties approval;
- a production KMS/Vault/SMTP/SIEM provider;
- production cloud/network architecture;
- legal retention periods;
- product-specific PII classification;
- production backup/RPO/RTO evidence for every deployment;
- production penetration/load acceptance.

Start with:

- [`docs/PRODUCTION-READINESS-AR.md`](docs/PRODUCTION-READINESS-AR.md)
- [`docs/security/CURRENT-SECURITY-STATUS.md`](docs/security/CURRENT-SECURITY-STATUS.md)
- [`docs/security/SECURITY-DECISIONS.md`](docs/security/SECURITY-DECISIONS.md)
- [`docs/security/POLICY-IMPLEMENTATION-REGISTER.md`](docs/security/POLICY-IMPLEMENTATION-REGISTER.md)

---

## Current autonomous stop boundary

The general-purpose reusable baseline has reached a deliberate consumer/policy boundary. New packages are **not** created merely to reduce roadmap checkboxes.

The following areas need a real product/provider decision or stronger consumer evidence before reusable runtime extraction:

- Files / Documents and storage/document lifecycle;
- Background Jobs and a real delayed/scheduled work consumer;
- Messaging / outbox / inbox / broker semantics;
- reusable Idempotency beyond Athar's product-specific behavior;
- reusable Concurrency beyond Athar's SQL Server `rowversion` behavior;
- Organization / Multi-Tenancy hierarchy and isolation topology;
- Search / Reporting;
- Privacy / Retention and legal/product semantics;
- Money / Numbering and finance semantics;
- Redis/object storage/messaging/search/observability provider families;
- advanced approval routing;
- project generation and visual composition;
- AI abstractions after real provider-neutral consumer requirements exist.

That stop rule is intentional: FoundationKit should be broadly useful without silently embedding one company's hierarchy, one product's policy, or one vendor's infrastructure. Madar is now a concrete second product-domain consumer that can provide evidence for future extraction decisions.

---

## Documentation index

Start here:

1. [`docs/LOCAL-RUN-WINDOWS-AR.md`](docs/LOCAL-RUN-WINDOWS-AR.md) — Windows first run and diagnostics.
2. [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
3. [`docs/PACKAGES.md`](docs/PACKAGES.md)
4. [`docs/FEATURES.md`](docs/FEATURES.md)
5. [`docs/CAPABILITY-MODEL-V1.md`](docs/CAPABILITY-MODEL-V1.md)
6. [`docs/CAPABILITY-ROADMAP-V1.md`](docs/CAPABILITY-ROADMAP-V1.md)
7. [`docs/CAPABILITY-EXTRACTION-STATUS.md`](docs/CAPABILITY-EXTRACTION-STATUS.md)
8. [`docs/COMPOSER-CLI-V1.md`](docs/COMPOSER-CLI-V1.md)
9. [`docs/WORKBENCH.md`](docs/WORKBENCH.md)
10. [`docs/DUAL-FULL-STACK.md`](docs/DUAL-FULL-STACK.md)
11. [`docs/VISUAL-STUDIO-2026-AR.md`](docs/VISUAL-STUDIO-2026-AR.md)
12. [`docs/ADDING-A-PROJECT-AR.md`](docs/ADDING-A-PROJECT-AR.md)
13. [`docs/PRODUCTION-READINESS-AR.md`](docs/PRODUCTION-READINESS-AR.md)
14. [`examples/Athar/README.md`](examples/Athar/README.md)
15. [`apps/Madar/README.md`](apps/Madar/README.md)

The GitHub Pages Atlas is generated from `site/portal-manifest.json` and provides a navigable view of the same repository surfaces.

---

## Versioning

Current package version:

```text
0.1.0
```

The current repository is still evolving. Capability maturity and compatibility expectations should be read from the capability model and changelog rather than inferred from semantic version alone.

See [`CHANGELOG.md`](CHANGELOG.md).
