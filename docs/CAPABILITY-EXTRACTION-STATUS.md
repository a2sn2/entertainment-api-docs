# Capability Extraction Status

Status date: 2026-08-08.

This document records the current consumer-driven capability extraction status. It distinguishes reusable packages that actually exist from catalog vocabulary, product-specific reference behavior, and future roadmap ideas.

## Completed reusable/reference extractions

| Capability | Package / boundary | First real consumer | Current maturity |
|---|---|---|---|
| Auditing | `FoundationKit.Auditing` | Athar / reusable audit composition | ReferenceOnly |
| Security | `FoundationKit.Security` | Athar | Preview |
| Identity | `FoundationKit.Identity` | Athar | ReferenceOnly |
| Authorization | `FoundationKit.Authorization` | Athar | ReferenceOnly |
| Workflow | `FoundationKit.Workflow` | Athar initiative review | ReferenceOnly |
| Approvals v1 | `FoundationKit.Approvals` | Athar initiative review | ReferenceOnly |
| Notifications v1 | `FoundationKit.Notifications` | Athar account-security adapter | ReferenceOnly |
| SMTP provider v1 | `FoundationKit.Notifications.Smtp` | Athar account-security delivery | ReferenceOnly |
| Settings v1 | `FoundationKit.Settings` | Workbench platform reference | ReferenceOnly |
| Feature Management v1 | `FoundationKit.FeatureManagement` | Workbench platform reference | ReferenceOnly |
| Localization v1 | `FoundationKit.Localization` | Workbench platform reference | ReferenceOnly |
| Caching v1 | `FoundationKit.Caching` | Workbench embedded catalog read path | ReferenceOnly |

The reusable package output is currently **seventeen FoundationKit NuGet packages plus seventeen symbol packages**. Maturity remains capability-specific and must not be inferred from package existence alone.

## Verified merge evidence

The capability sequence is merged through pull-request gates rather than direct edits to `main`:

- Workflow extraction — PR #59, merged to `main` at `9331460f...`.
- Approvals v1 — PR #60, merged to `main` at `745e33f8...`.
- Notifications v1 — PR #61, merged to `main` at `ef64a6d3...`.
- SMTP provider v1 — PR #62, merged to `main` at `d12f34fe...`.
- Consumer-driven extraction closure — PR #63, merged to `main` at `5141f572...`.
- Settings + Feature Management — PR #65, merged to `main` at `72f15909...`.
- Localization v1 — PR #66, merged to `main` at `64660c48...`.
- Caching v1 — PR #67, merged to `main` at `9af7c2b1...`.

PR #67 verified its exact final source head `283b432f...` with CI `31240105512`, Security Scan `31240105492`, and CodeQL `31240105490`: 355 tracked text files scanned, 198 NuGet components in the SBOM, zero build warnings/errors, 190 automated tests, 17 NuGet + 17 symbol packages, Workbench SQL/cache-path assertions, Athar E2E/non-root, and isolated backup/restore verification.

These automated results are technical repository evidence. They are not independent organizational approval, Production Approval, ISO certification, or formal Segregation-of-Duties evidence.

## General-purpose continuation — Issue #64

Issue #64 established the rule for the general-purpose extraction cycle: build broadly useful system capabilities until the next useful step requires a real owner/product/organizational decision.

The rule remains consumer-first:

1. a reusable capability must have an independently useful, provider-neutral boundary;
2. runtime behavior should be exercised by a real Workbench/Athar reference consumer where applicable;
3. package extraction must not fabricate product semantics just to reduce roadmap checkboxes;
4. maturity remains conservative until broader adoption/compatibility evidence exists.

### Settings v1

The reusable package provides bounded setting keys/values, opaque caller-defined scopes, deterministic most-specific-first resolution, deterministic source precedence, and an immutable in-memory reference source.

Workbench proves runtime use through `GET /api/platform-reference`.

Important boundary: Settings is **not** a secret store and does not select persistence, encryption, KMS, tenant hierarchy, organization hierarchy, administration UI, or refresh policy.

### Feature Management v1

The reusable package provides bounded feature IDs and deterministic Boolean feature evaluation backed by Settings. An absent setting uses the feature definition's explicit default. An explicitly configured non-Boolean value fails closed to disabled rather than silently falling back to an enabled default.

Workbench proves runtime use through `GET /api/platform-reference`, and the SQL integration smoke flow asserts the settings-backed decision.

Important boundary: v1 does not implement percentage rollouts, user/segment targeting, experiments, schedules, vendor SDKs, or arbitrary rule execution.

### Localization v1

The reusable package provides canonical culture metadata, RTL/LTR directionality from BCL culture data, bounded supported-culture sets, deterministic exact/parent/default fallback, explicit invalid-request provenance, and a bounded opaque time-zone identifier.

Workbench supplies `ar-YE` and `UTC` through Settings and proves Localization through `GET /api/platform-reference`; the integration smoke flow asserts exact `ar-YE` resolution, `RightToLeft`, and `UTC` before exercising the existing SQL user/admin workflow.

Important boundary: Localization v1 does not select a translation/resource provider, persist user/tenant preferences, negotiate HTTP languages, perform OS-specific time-zone conversion, or invent Windows/IANA mappings.

### Caching v1

The reusable package provides a bounded normalized `CacheKey`, explicit positive finite TTL, explicit hit/miss results, provider-neutral get/set/remove operations, caller cancellation, DateTimeOffset overflow protection, and a bounded BCL-only in-memory reference provider.

Workbench is the first real consumer: `CatalogService` caches the existing embedded capability-catalog byte payload. `CatalogCachingTests` proves two gets and one set across two reads, while the SQL integration smoke flow reads `/api/catalog` twice before continuing the SQL user/admin workflow.

Important boundary: Caching v1 does not select Redis or another production cache provider, define distributed coherence/locking, serialize arbitrary objects, make cache state authoritative, provide tag invalidation/refresh-ahead/stale-while-revalidate, or classify which sensitive data a product may cache.

## Repository consistency closure

After Caching v1, the remaining repository-only task was a consistency sweep rather than another capability extraction. The sweep aligns the repository with the already implemented baseline:

- `tooling-cli` now represents the existing Composer v1 as `ReferenceOnly`, while interactive project generation remains explicitly future work;
- the root manager delegates packaging to canonical `scripts/pack.ps1` instead of maintaining a second five-package list;
- the human catalog and generated `FEATURES.md` describe all seventeen reusable package projects;
- the Atlas package surface mirrors the same seventeen source package projects and documents Composer as tooling, not a package generator;
- the root README describes the current composable baseline instead of the original five-package snapshot;
- repository verification now derives the reusable package set from `src/FoundationKit.*` and fails when the human catalog or Atlas package cards drift from it;
- CI parses the unified manager and canonical pack script in addition to the product/deployment PowerShell scripts.

This implementation is not treated as finally verified until the consistency pull request passes CI, Security Scan, CodeQL, generated-file checks, package output, and integration workflows on its exact final head.

## Current autonomous boundary

No additional reusable package is currently justified by both a provider-neutral boundary and a real independent consumer. After the consistency sweep is verified and merged, further runtime capability extraction should wait for real product semantics or provider choices rather than inventing them.

## Capabilities that still require stronger consumer evidence or owner semantics

### Files / Documents

Athar and Workbench currently have no upload, object-storage, document-versioning, classification, or reusable file-lifecycle consumer.

Status: **Planned — no extraction yet**.

### Background Jobs

There is no delayed, scheduled, recurring, retryable, or worker-hosted job behavior in the current reference consumers.

Status: **Planned — no extraction yet**.

### Messaging

FoundationKit.Infrastructure has an in-process domain-event dispatcher. That mechanism dispatches domain events to in-process handlers and is deliberately not presented as integration messaging, a broker abstraction, outbox/inbox, retry/dead-letter handling, or cross-service delivery.

Status: **Planned — do not rename the existing domain-event dispatcher into Messaging**.

### Idempotency

Athar has real owner-scoped `ClientRequestId` behavior plus a unique database constraint, but no reusable reservation/store/completion/replay contract is proven independently of Athar's initiative schema.

Status: **ReferenceOnly behavior — no package extraction yet**.

### Concurrency

Athar has real SQL Server `rowversion` plus HTTP 409 handling, but no reusable client-visible precondition/token contract or second consumer that justifies a separate package.

Status: **ReferenceOnly behavior — no package extraction yet**.

### Organization / Multi-Tenancy

FoundationKit can define vocabulary in the capability graph, but it must not invent organization hierarchy, tenant identity, tenant resolution, or data-isolation topology without an actual product requirement.

Status: **Planned — owner/product model required before runtime extraction**.

### Search / Reporting / Privacy / Retention / Money / Numbering

These remain catalog/roadmap vocabulary until a real consumer establishes their semantics and required provider/policy boundaries.

Status: **Planned — no extraction yet**.

## Governance boundary

This status tracks **repository capability extraction**, not Production Approval. Independent approval, branch/ruleset evidence, deployment-provider choices, legal/user-data retention, monitoring/SIEM, production secret/KMS operations, distributed-cache provider policy, and formal certification remain separate organizational/deployment controls under `docs/security/`.
