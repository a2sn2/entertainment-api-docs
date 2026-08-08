# Madar Changelog

This product changelog records Madar-specific behavior. Repository-wide reusable FoundationKit changes remain documented in the root `CHANGELOG.md`.

## [Unreleased] — v0.3 case collaboration

### Added

- Product-owned append-only `CaseComment` model with case/author IDs, plain-text body, creation time, and SQL Server rowversion.
- Body validation that trims input and accepts only 1..2000 characters.
- Existing case read-scope reused for comment list/add access: creator, current assignee, or role with `madar.cases.read-all`.
- Inaccessible/missing parent cases use the existing not-found masking rule.
- `GET /api/cases/{caseId}/comments` and protected `POST /api/cases/{caseId}/comments`.
- `madar.CaseComments` SQL table with case/author foreign keys and deterministic `(CaseId, CreatedUtc, Id)` ordering/indexing.
- `madar.case.comment-added` audit action containing bounded metadata only; comment text is deliberately excluded from audit attributes.
- Typed Blazor API support and Arabic comments panel on the existing case-details route.
- SQL smoke coverage proving assigned-operator add/list, body availability only through the authorized comments API, body absence from the audit timeline, and comment history after case closure.

### Deliberately deferred

- edit/delete/version history;
- private/internal-note visibility tiers;
- mentions/watchers/subscriptions;
- notifications;
- files/attachments;
- rich text/HTML;
- reactions/moderation;
- reusable collaboration/comments package.

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
