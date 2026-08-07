# FoundationKit Capability Model v1

## Purpose

FoundationKit is evolving from a reusable core into a **composable system-building foundation**. The design goal is not to place every feature inside the kernel. The goal is to keep a small, stable kernel and expose reusable capabilities that a project can opt into deliberately.

The model in this document is the first machine-oriented contract for that direction.

## Core rules

1. **Kernel stays small.** Product features do not move into `FoundationKit.Domain` or other core packages merely because many products may need them.
2. **Everything beyond the kernel is opt-in.** A project should be able to use FoundationKit without taking identity, workflow, files, multi-tenancy, AI, or another unrelated concern.
3. **Capabilities declare dependencies.** Selecting `approvals`, for example, can pull the workflow/audit/authorization contracts it requires.
4. **Providers are separate from capabilities.** SQL Server, Redis, SMTP, cloud services, search engines, message brokers, and AI vendors are adapters rather than business-core dependencies.
5. **Tooling consumes the same graph.** Future CLI and Workbench composers must use the same capability IDs and dependency rules instead of maintaining a second hidden model.
6. **Maturity is explicit.** A capability listed in the catalog is not automatically implemented or production-ready.
7. **Profiles are starting points, not frameworks inside the framework.** A project can start from a profile, include more capabilities, and remove independent capabilities.
8. **A required dependency cannot be excluded.** Composition must fail rather than silently generate an invalid project.

## Capability kinds

| Kind | Meaning |
|---|---|
| `Kernel` | Stable primitives every composition starts from. |
| `Optional` | Reusable capability that a project selects only when needed. |
| `Provider` | Technology/vendor adapter that implements a capability boundary. |
| `Tooling` | CLI, Workbench, generators, analyzers, or other developer experience. |

## Maturity states

| Maturity | Meaning |
|---|---|
| `Stable` | Reusable FoundationKit contract/implementation is part of the current supported core. |
| `Preview` | Reusable direction exists but is still being hardened or broadened. |
| `ReferenceOnly` | A real reusable boundary or package is implemented and/or proven by a reference consumer, but adoption, compatibility, provider, or production evidence is still too limited for `Preview` or `Stable`. |
| `Planned` | Defined in the capability graph so dependencies and future composition remain coherent; implementation must not be claimed yet. |

This distinction is mandatory. A profile containing a planned capability describes a **target system composition**, not a claim that the feature can already be generated. `ReferenceOnly` likewise does not mean production approval; it means the stated reference-level surface is real and must be described without implying broader unimplemented behavior.

## Current catalog groups

### Foundation and experience

- `kernel`
- `validation`
- `web-api`
- `blazor`
- `localization`

### Identity and security

- `security`
- `identity`
- `authorization`
- `privacy`

### Governance and process

- `auditing`
- `workflow`
- `approvals`
- `tasks`
- `retention`

### Platform and organization

- `settings`
- `feature-management`
- `organization`
- `multi-tenancy`

### Communication and integration

- `notifications`
- `messaging`
- `webhooks`
- `realtime`

### Data and content

- `files`
- `documents`
- `caching`
- `search`
- `reporting`

### Reliability and operations

- `observability`
- `jobs`
- `idempotency`
- `concurrency`

### Business building blocks

- `money`
- `numbering`

### Intelligence

- `ai`

### Initial provider/tooling identities

- `provider-sqlserver`
- `provider-redis`
- `provider-smtp`
- `tooling-cli`
- `tooling-workbench`

The catalog will grow only when a capability has a clear boundary, dependency model, ownership, tests, and documentation.

## Profiles

FoundationKit v1 defines seven composition profiles:

| Profile | Intent |
|---|---|
| `minimal` | Small API/service baseline. |
| `standard` | General business-system baseline. |
| `enterprise` | Organization/process/approval/automation baseline. |
| `fintech` | Enterprise baseline plus finance/privacy/retention-oriented controls. |
| `saas` | Tenant/feature/integration/search-oriented baseline. |
| `internal-business` | Line-of-business systems with organization, workflow, tasks and reporting. |
| `public-portal` | Externally facing portal baseline. |

Profiles are deliberately editable through includes/excludes. They are not hard-coded product templates.

## Dependency examples

### Approvals

```text
approvals
  -> workflow
      -> auditing
  -> authorization
      -> identity
          -> security
              -> web-api
                  -> kernel
```

### Feature Management

```text
feature-management
  -> settings
      -> kernel
```

### Localization

```text
localization
  -> kernel
```

### Documents

```text
documents
  -> files
      -> authorization
  -> auditing
```

### Redis provider

```text
provider-redis
  -> caching
      -> kernel
```

### SMTP provider

```text
provider-smtp
  -> notifications
      -> kernel
```

The resolver returns dependencies before dependants and rejects unknown IDs or cycles.

## Project manifest direction

A future project composer will consume a manifest shaped like this:

```json
{
  "name": "MySystem",
  "profile": "enterprise",
  "includeCapabilities": ["documents", "search"],
  "excludeCapabilities": ["localization"],
  "providers": ["provider-sqlserver"]
}
```

The same manifest must eventually drive:

- CLI generation;
- Workbench visual composition;
- package/project selection;
- provider wiring;
- generated architecture documentation;
- capability/maturity warnings;
- tests that prove the generated composition is valid.

## Implementation sequence

The capability catalog is not permission to create dozens of empty packages. Extraction should be vertical, consumer-driven, and evidence-driven.

Current sequence status:

1. Capability model, resolver, profiles, and manifest contract — **implemented**.
2. Composer validation and machine-readable catalog export — **implemented at reference/tooling level**.
3. Auditing, Security, Identity, and Authorization reusable boundaries — **extracted with conservative maturity levels**.
4. Workflow and the narrow Approvals v1 decision/maker-checker surface — **extracted as `ReferenceOnly`**.
5. Notifications bounded message/delivery contracts — **extracted as `ReferenceOnly` with Athar consumer evidence**.
6. SMTP notification provider v1 — **extracted as reusable `FoundationKit.Notifications.Smtp` and consumed by Athar; maturity remains `ReferenceOnly`**.
7. Settings v1 and Feature Management v1 — **extracted as reusable packages with Workbench runtime evidence; both remain `ReferenceOnly`**.
8. Localization v1 — **extracted as `FoundationKit.Localization` with Workbench runtime proof for canonical culture resolution, RTL/LTR directionality, and opaque time-zone identity; maturity remains `ReferenceOnly`**.
9. Issue #64 — **continues the general-purpose capability baseline using the same consumer-first stop rule**.
10. Caching v1 — **next autonomous provider-neutral candidate; Redis/distributed-provider semantics remain separate**.
11. Files/Documents, Jobs/Messaging, Organization/Multi-Tenancy, Search/Reporting/Privacy/Retention, and finance building blocks — **remain planned until consumer evidence and/or required product semantics justify extraction**.
12. Idempotency and Concurrency — **retain current Athar reference behavior, but no separate reusable package is claimed yet**.
13. Provider-family expansion — **planned beyond the current SQL Server reference behavior and SMTP provider v1**.
14. CLI and visual Workbench composer expansion — **planned beyond current reference tooling**.
15. AI abstractions — **planned only after provider-neutral boundaries and observability rules are established**.

Advanced approvals such as sequential, parallel, quorum, delegation, escalation, and dynamic approver routing remain future work even though the narrow v1 capability is implemented. Notification templates, preferences, queues, retry orchestration, delivery history, and additional channels likewise remain future work beyond the reference v1 boundary. The extracted SMTP provider is a narrow transport adapter; it does not imply those higher-level notification capabilities.

Settings v1 deliberately does not become a secret store, Feature Management v1 deliberately does not become a percentage-rollout/experimentation engine, and Localization v1 deliberately does not become a translation store or OS-specific time-zone conversion provider. These capabilities express reusable deterministic boundaries while leaving provider, organizational, and product policies to consuming systems.

Each extraction must preserve the dependency direction and current security baseline. A new package should not be created merely to reduce the number of planned items: it must have a concrete consumer and an independently useful boundary.

See `docs/CAPABILITY-EXTRACTION-STATUS.md` for the current extraction status and the evidence that distinguishes extracted packages from product-specific reference behavior.

## Non-goals of v1

The catalog does **not** claim that every item is implemented, production-ready, or available as a NuGet package. It establishes shared vocabulary and dependency rules, while each capability's maturity and dedicated documentation state what is actually implemented.
