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

Caching v1 is implemented on PR #67 and must not be treated as merged until that PR's exact final head passes every required repository gate. When verified, the reusable package output is expected to be seventeen FoundationKit NuGet packages plus seventeen symbol packages.

## Verified merge evidence

The capability sequence is merged through pull-request gates rather than direct edits to `main`:

- Workflow extraction — PR #59, merged to `main` at `9331460f...`.
- Approvals v1 — PR #60, merged to `main` at `745e33f8...`.
- Notifications v1 — PR #61, merged to `main` at `ef64a6d3...`.
- SMTP provider v1 — PR #62, merged to `main` at `d12f34fe...`.
- Consumer-driven extraction closure — PR #63, merged to `main` at `5141f572...`.
- Settings + Feature Management — PR #65, merged to `main` at `72f15909...`.
- Localization v1 — PR #66, merged to `main` at `64660c48...`; its final-head CI `31223314887`, Security Scan `31223315171`, and CodeQL `31223315164` all succeeded.

PR #65 verified its exact final source head with CI `31221346528`, Security Scan `31221346353`, and CodeQL `31221346445`, including zero build warnings/errors, Workbench SQL/runtime capability assertions, Athar E2E/non-root, and isolated backup/restore verification.

These automated results are technical repository evidence. They are not independent organizational approval, Production Approval, ISO certification, or formal Segregation-of-Duties evidence.

## General-purpose continuation — Issue #64

Issue #64 continues the cycle with a stricter goal: build broadly useful system capabilities until the next useful step requires a real owner/product/organizational decision.

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

The reusable package provides a bounded normalized `CacheKey`, explicit positive finite TTL, explicit hit/miss results, provider-neutral get/set/remove operations, caller cancellation, and a bounded BCL-only in-memory reference provider. The reference provider copies values defensively, removes expired entries, and uses deterministic earliest-expiry eviction when its configured capacity is reached.

Workbench is the first real consumer: `CatalogService` caches the existing embedded capability-catalog byte payload. The SQL integration smoke flow reads `/api/catalog` twice so the live host exercises the initial miss/fill path followed by the cache-hit path before continuing the existing SQL workflow.

Important boundary: Caching v1 does not select Redis or another production cache provider, define distributed coherence/locking, serialize arbitrary objects, make cache state authoritative, provide tag invalidation/refresh-ahead/stale-while-revalidate, or classify which sensitive data a product may cache.

## Current autonomous boundary

After Caching v1, no additional package is currently justified by both a reusable provider-neutral boundary and a real independent consumer. The remaining autonomous work is a repository consistency sweep; further capability extraction should wait for real product semantics or provider choices.

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

Athar has real reference behavior:

- an owner-scoped `ClientRequestId` is checked before initiative creation;
- the database has a unique `(OwnerUserId, ClientRequestId)` constraint as the final duplicate-write guard;
- integration/smoke coverage exercises the behavior.

What is not yet proven is a reusable reservation/store/completion/replay contract that another consumer can use independently of Athar's initiative schema.

Status: **ReferenceOnly behavior — no package extraction yet**.

### Concurrency

Athar has real reference behavior:

- SQL Server `rowversion` is configured as an EF concurrency token;
- concurrent update conflicts are translated to HTTP 409 behavior.

What is not yet proven is a reusable client-visible precondition/token contract, provider-neutral comparison primitive, or second consumer that justifies a separate package.

Status: **ReferenceOnly behavior — no package extraction yet**.

### Organization / Multi-Tenancy

FoundationKit can define vocabulary in the capability graph, but it must not invent organization hierarchy, tenant identity, tenant resolution, or data-isolation topology without an actual product requirement.

Status: **Planned — owner/product model required before runtime extraction**.

### Search / Reporting / Privacy / Retention / Money / Numbering

These remain catalog/roadmap vocabulary until a real consumer establishes their semantics and their required provider/policy boundaries.

Status: **Planned — no extraction yet**.

## Governance boundary

This status tracks **repository capability extraction**, not Production Approval. Independent approval, branch/ruleset evidence, deployment-provider choices, legal/user-data retention, monitoring/SIEM, production secret/KMS operations, distributed-cache provider policy, and formal certification remain separate organizational/deployment controls under `docs/security/`.
