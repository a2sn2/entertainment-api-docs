# Capability Extraction Status

Status date: 2026-08-07.

This document records the current end of the consumer-driven capability extraction cycle. It distinguishes reusable packages that actually exist from catalog vocabulary, product-specific reference behavior, and future roadmap ideas.

## Completed extraction sequence

The current cycle completed these reusable/reference extractions on `main`:

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

The repository packages thirteen reusable FoundationKit projects plus thirteen symbol packages after the SMTP provider extraction.

## Recent merge evidence

The final extraction sequence was merged through reviewed pull-request gates rather than direct edits to `main`:

- Workflow extraction — PR #59, merged to `main` at `9331460f...`.
- Approvals v1 — PR #60, merged to `main` at `745e33f8...`.
- Notifications v1 — PR #61, merged to `main` at `ef64a6d3...`.
- SMTP provider v1 — PR #62, merged to `main` at `d12f34fe...`.

For PR #62, the final verified source head was `be5b3b18b318f5013604a79fc78aa0703eed1208` with:

- FoundationKit CI `31216921336` — success;
- Security Scan `31216921297` — success;
- CodeQL `31216921235` — success;
- Release build — zero warnings and zero errors;
- 152 automated tests passed;
- 13 NuGet packages and 13 symbol packages created;
- Workbench SQL Server integration passed;
- Athar readiness, non-root, Arabic/API surface, end-to-end workflow, and isolated backup/restore verification passed.

These automated results are technical repository evidence. They are not independent organizational approval, Production Approval, ISO certification, or formal Segregation-of-Duties evidence.

## Why the extraction cycle stops here

FoundationKit intentionally does not create a reusable package merely because a capability name exists in the catalog or roadmap. The next package must have both:

1. a concrete product/consumer requirement; and
2. an independently useful reusable boundary that is not shaped only around that one product.

The currently visible candidates do not yet meet both conditions.

### Files / Documents

Athar currently has no upload, object-storage, document-versioning, classification, or reusable file-lifecycle consumer. Creating `FoundationKit.Files` now would define storage semantics without implementation evidence.

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

## Reopening the cycle

A future extraction should reopen this cycle only when a concrete feature creates real evidence. Examples include:

- a product needs uploaded files and two storage providers are plausible;
- a background worker needs scheduled/retryable jobs;
- an external integration requires outbox/inbox or broker delivery;
- a second write workflow needs the same idempotency reservation/replay semantics;
- an API exposes reusable optimistic-concurrency preconditions across more than one aggregate/product.

At that point the capability should be designed from the consumer behavior, tested through the repository gates, documented with explicit non-goals, and only then moved beyond its current maturity.

## Governance boundary

This status document closes the current **repository capability extraction cycle** only. It does not close the separate organizational/production governance work tracked under `docs/security/`, including independent approval, branch/ruleset evidence, deployment-provider choices, retention/RPO/RTO decisions, monitoring/SIEM, secret/KMS operations, or formal certification activities.
