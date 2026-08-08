# Madar Changelog

This product changelog records Madar-specific behavior. Repository-wide reusable FoundationKit changes remain documented in the root `CHANGELOG.md`.

## [Unreleased] — v0.5 operational case notifications

### Added

- Reuse of `FoundationKit.Notifications` for bounded provider-neutral notification messages/results and `FoundationKit.Notifications.Smtp` for the current optional email transport.
- Product-owned Arabic notification copy for assignment, approval decision, and resolution events.
- Identity-backed notification destination resolution without exposing recipient addresses through API contracts.
- Optional SMTP configuration under `Madar:Notifications:Smtp`; an empty host/from-address is treated as `NotConfigured` by the existing provider contract.
- `madar.case.notification-delivery` audit evidence containing only purpose, target user ID, and bounded delivery status; destination and body are deliberately excluded.
- Notification delivery occurs only after the corresponding business transaction is saved, so SMTP `Failed` / `NotConfigured` outcomes do not roll back assignment, approval decision, or resolution.
- Unit coverage for delivered, not-configured, failed, and audit-privacy behavior.

### Deliberately deferred

- background jobs, outbox delivery, retry/backoff, or delayed scheduling;
- templates, preferences, recipient groups, fallback channels, or in-app inbox;
- SMS, push, WhatsApp, or webhook providers;
- SLA reminder scheduling;
- changes to the public `FoundationKit.Notifications` API merely for Madar convenience.

## [v0.4] — sensitive-case approval gate

### Added

- Product-owned `CaseApproval` persistence with requester/reviewer identities, pending/approved/rejected state, bounded decision notes, timestamps, and SQL Server rowversion.
- Sensitive-case policy: `access-request` and `compliance-case` require the latest approval to be approved before `in-progress → resolved`.
- Existing `FoundationKit.Approvals` reuse for permission-first decision eligibility, maker-checker enforcement, strict approve/reject normalization, and workflow-backed decision resolution.
- New `madar.cases.approve` product permission granted to Supervisor and Administrator in the current role model.
- Authenticated approval history/request/decision API under `/api/cases/{caseId}/approvals`, with anti-CSRF and write rate limiting on writes.
- `madar.CaseApprovals` SQL table with deterministic case-history index, requester/reviewer foreign keys, and migration/snapshot coverage.
- `madar.case.approval-requested` and `madar.case.approval-decided` audit actions with bounded metadata; decision notes remain product data and are excluded from audit attributes.
- Arabic approval panel on the existing case-details route, including request, maker-checker decision, status, notes, and history.
- Unit coverage for permission-first maker-checker behavior, rejection/re-request behavior, domain defense-in-depth, and audit-note exclusion.
- SQL smoke coverage proving resolution is blocked before approval, a different authorized actor approves, the same case then resolves/closes, approval history persists, and decision notes do not leak into the audit timeline.

### Deliberately deferred

- multi-stage, parallel, or quorum approvals;
- dynamic approver routing/delegation;
- approval SLA/background scheduling;
- files/attachments;
- edit/delete/versioning of approval records;
- organization hierarchy/multi-tenancy;
- changes to the public `FoundationKit.Approvals` API merely for Madar convenience.

## [v0.3] — case collaboration

- Product-owned append-only `CaseComment` model with case/author IDs, plain-text body, creation time, and SQL Server rowversion.
- Body validation that trims input and accepts only 1..2000 characters.
- Existing case read-scope reused for comment list/add access: creator, current assignee, or role with `madar.cases.read-all`.
- Inaccessible/missing parent cases use the existing not-found masking rule.
- `GET /api/cases/{caseId}/comments` and protected `POST /api/cases/{caseId}/comments`.
- `madar.CaseComments` SQL table with case/author foreign keys and deterministic `(CaseId, CreatedUtc, Id)` ordering/indexing.
- `madar.case.comment-added` audit action containing bounded metadata only; comment text is deliberately excluded from audit attributes.
- Typed Blazor API support and Arabic comments panel on the existing case-details route.
- SQL smoke coverage proving assigned-operator add/list, body availability only through the authorized comments API, body absence from the audit timeline, and comment history after case closure.
- No edit/delete/version history, private-note tiers, mentions/watchers, notifications, attachments, rich text, reactions/moderation, or reusable comments package.

## [v0.2] — SLA deadlines and bounded escalation

- Product-configured SLA duration by priority with absolute target snapshot at case creation.
- `not-applicable`, `active`, `met`, and `breached` semantics with exact-target boundary behavior.
- Persistent first breach and escalation timestamps.
- Late-resolution breach materialization.
- Authorized, bounded, idempotent `POST /api/cases/sla/evaluate` scheduler seam.
- SLA SQL migration, Arabic API/UI evidence, and real SQL smoke coverage.
- No production scheduler/provider or reusable `FoundationKit.Jobs` extraction.

## [v0.1.1] — operational readiness closure

- Database-backed readiness endpoint and bounded startup retry/schema validation.
- Protected local Madar Docker launcher and unified Windows manager integration.
- Atlas/Pages coverage, Madar container vulnerability gate/SARIF, and Windows PowerShell verification.

## [v0.1] — first runtime vertical slice

- ASP.NET Core Identity authentication/authorization.
- SQL Server case persistence.
- Case create/list/view/assign and deterministic `new → assigned → in-progress → resolved → closed` lifecycle.
- Persistent audit timeline.
- Arabic API/Blazor flow, Docker/SQL end-to-end verification, and optimistic-concurrency handling.

## Evidence rule

A changelog entry describes repository behavior only. Exact CI/security evidence belongs to the exact PR head that produced it. Repository evidence is not Production Approval, independent Segregation-of-Duties approval, ISO/IEC 27001 certification, or a production infrastructure/security attestation.