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

After Localization is verified and merged, the reusable package output is expected to be sixteen FoundationKit NuGet packages plus sixteen symbol packages. The exact current-head CI evidence belongs in PR #66 before merge rather than being asserted here in advance.

## Verified merge evidence

The capability sequence is merged through pull-request gates rather than direct edits to `main`:

- Workflow extraction — PR #59, merged to `main` at `9331460f...`.
- Approvals v1 — PR #60, merged to `main` at `745e33f8...`.
- Notifications v1 — PR #61, merged to `main` at `ef64a6d3...`.
- SMTP provider v1 — PR #62, merged to `main` at `d12f34fe...`.
- Consumer-driven extraction closure — PR #63, merged to `main` at `5141f572...`.
- Settings + Feature Management — PR #65, merged to `main` at `72f15909...`.

PR #65 verified its exact final source head with CI `31221346528`, Security Scan `31221346353`, and CodeQL `31221346445`: 342 tracked text files scanned, 198 NuGet components in the SBOM, zero build warnings/errors, 167 automated tests, 15 NuGet + 15 symbol packages, Workbench SQL/runtime capability assertions, Athar E2E/non-root, and isolated backup/restore verification.

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

## Current next autonomous candidate

**Caching v1** is the next capability that can be extracted without an owner/provider decision. The reusable boundary can define bounded keys, bounded byte payloads, explicit TTL, cache miss semantics, remove operations, and an in-memory reference provider using BCL time primitives. Workbench can prove the boundary on an existing read path without choosing Redis or distributed consistency semantics.

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

## Governance boundary

This status tracks **repository capability extraction**, not Production Approval. Independent approval, branch/ruleset evidence, deployment-provider choices, legal/user-data retention, monitoring/SIEM, production secret/KMS operations, and formal certification remain separate organizational/deployment controls under `docs/security/`.
