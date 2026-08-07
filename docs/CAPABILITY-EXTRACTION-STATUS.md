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

After Settings and Feature Management are verified and merged, the repository package output is expected to be fifteen reusable FoundationKit NuGet packages plus fifteen symbol packages. The exact CI evidence must be recorded on the pull request before merge rather than asserted here in advance.

## Previously verified merge evidence

The earlier extraction sequence was merged through pull-request gates rather than direct edits to `main`:

- Workflow extraction — PR #59, merged to `main` at `9331460f...`.
- Approvals v1 — PR #60, merged to `main` at `745e33f8...`.
- Notifications v1 — PR #61, merged to `main` at `ef64a6d3...`.
- SMTP provider v1 — PR #62, merged to `main` at `d12f34fe...`.
- Consumer-driven extraction closure — PR #63, merged to `main` at `5141f572...`.

PR #63 verified the then-current 13-package baseline with CI `31218305535`, Security Scan `31218302115`, and CodeQL `31218304349`, including 152 automated tests, real SQL Server integration, Athar E2E/non-root checks, and isolated backup/restore verification.

These automated results are technical repository evidence. They are not independent organizational approval, Production Approval, ISO certification, or formal Segregation-of-Duties evidence.

## General-purpose continuation — Issue #64

Issue #64 reopens the cycle with a stricter goal: continue building broadly useful system capabilities until the next useful step requires a real owner/product/organizational decision.

The rule remains consumer-first:

1. a reusable capability must have an independently useful, provider-neutral boundary;
2. runtime behavior should be exercised by a real Workbench/Athar reference consumer where applicable;
3. package extraction must not fabricate product semantics just to reduce roadmap checkboxes;
4. maturity remains conservative until broader adoption/compatibility evidence exists.

### Settings v1

The reusable package provides bounded setting keys/values, opaque caller-defined scopes, deterministic most-specific-first resolution, deterministic source precedence, and an immutable in-memory reference source.

Workbench proves runtime use through `GET /api/platform-reference`, resolving `workbench.experience.default-culture` from global scope.

Important boundary: Settings is **not** a secret store and does not select persistence, encryption, KMS, tenant hierarchy, organization hierarchy, administration UI, or refresh policy.

### Feature Management v1

The reusable package provides bounded feature IDs and deterministic Boolean feature evaluation backed by Settings. An absent setting uses the feature definition's explicit default. An explicitly configured non-Boolean value fails closed to disabled rather than silently falling back to an enabled default.

Workbench proves runtime use through `GET /api/platform-reference`, and the SQL integration smoke flow asserts the settings-backed decision.

Important boundary: v1 does not implement percentage rollouts, user/segment targeting, experiments, schedules, vendor SDKs, or arbitrary rule execution.

## Current next autonomous candidate

**Localization / culture / time-zone foundation** is the next candidate that can be designed without selecting an external provider or inventing business hierarchy. Its scope should stay limited to bounded supported cultures, directionality, deterministic fallback, and explicit time-zone identifiers; resource storage/translation provider and user/tenant persistence remain consumer concerns.

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
