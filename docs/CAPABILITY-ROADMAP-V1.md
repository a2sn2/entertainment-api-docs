# FoundationKit Capability Roadmap v1

This roadmap turns FoundationKit from a reusable core into a composable system-building foundation while keeping the kernel small and provider-neutral.

## Delivery rule

A capability moves through the following lifecycle:

1. **Planned** — vocabulary, boundary, and dependencies are defined.
2. **ReferenceOnly** — a real reference-level boundary/package or consumer proof exists, but broader adoption, compatibility, provider, or production evidence is still limited.
3. **Preview** — reusable package/contracts exist and pass repository quality/security gates, but compatibility or provider/adoption evidence is still evolving.
4. **Stable** — reusable contract is documented, independently composable, tested, packaged, and supported as part of the FoundationKit public surface.

No capability is promoted merely because a class or empty package exists.

## Phase A — Composition foundation

- [x] Capability vocabulary and typed catalog.
- [x] Dependency resolver with cycle/unknown-dependency protection.
- [x] Seven initial profiles.
- [x] Project-manifest model.
- [x] Machine-readable generated capability graph.
- [x] CI drift protection between compiled graph and exported JSON.
- [x] Strict manifest parsing/validation through current Composer tooling.
- [x] Composition dependency diagnostics through current Composer explain/validation flow.
- [ ] Capability compatibility/version metadata.

## Phase B — Governance and security foundations

- [x] Auditing reusable package extracted and packaged.
- [ ] Auditing provider/adoption proof and maturity promotion beyond `ReferenceOnly`.
- [x] Security reusable capability boundary.
- [x] Identity reusable capability boundary.
- [x] Authorization roles, permissions, ownership, and scoped access primitives.
- [x] Sensitive-action/step-up requirement contracts.
- [x] Maker-checker/four-eyes reusable policy primitive in Approvals v1.

## Phase C — Business process capabilities

- [x] Deterministic Workflow/state-transition kernel.
- [x] Approvals v1: single approve/reject decision, permission gate, maker-checker, workflow resolution, and audit intent.
- [ ] Advanced approvals: sequential, parallel, quorum/voting, delegation, escalation, and dynamic routing.
- [ ] Tasks/work items.
- [ ] SLA/business-hours/escalation capability.
- [ ] Timeline/activity stream.
- [ ] Comments/notes/mentions.
- [ ] Tags and favorites.

## Phase D — Communication and content

- [ ] Notifications abstraction.
- [ ] Notification templates and localization.
- [ ] SMTP provider capability integration beyond the existing product/reference adapter.
- [ ] File storage abstraction.
- [ ] Document metadata/versioning/classification.
- [ ] Local-development file provider.
- [ ] Object-storage provider boundary.
- [ ] Realtime abstraction.

## Phase E — Platform and organization

- [ ] Settings hierarchy.
- [ ] Feature management/feature flags.
- [ ] Organization/branch/department/team hierarchy.
- [ ] Multi-tenancy context and isolation contracts.
- [ ] Localization/culture/time-zone foundation.
- [ ] Numbering/sequences.
- [ ] Lifecycle/archive/soft-delete primitives.

## Phase F — Reliability and integration

- [ ] Background jobs abstraction.
- [ ] Messaging/integration events.
- [ ] Outbox/inbox contracts.
- [ ] Webhooks with signing/replay/retry contracts.
- [ ] Idempotency reusable package extraction beyond current reference behavior.
- [ ] Optimistic concurrency reusable package extraction beyond current reference behavior.
- [ ] Caching abstraction.
- [ ] External HTTP integration resilience conventions.

## Phase G — Search, reporting, privacy, finance

- [ ] Search abstraction.
- [ ] Reporting definitions and export boundaries.
- [ ] Import/export capability.
- [ ] Privacy/PII classification and masking hooks.
- [ ] Retention/anonymization contracts.
- [ ] Money/currency value model.
- [ ] Finance-oriented approval and audit composition profile improvements.

## Phase H — Providers

Providers remain outside business capabilities and are selected explicitly.

- [ ] SQL Server provider family where reusable provider code is justified.
- [ ] PostgreSQL provider family.
- [ ] Redis provider.
- [ ] SMTP provider family beyond the existing product/reference adapter.
- [ ] Object storage providers.
- [ ] Search providers.
- [ ] Messaging providers.
- [ ] Observability/OpenTelemetry provider wiring.

## Phase I — Project Composer

- [x] `FoundationKit.Composer` CLI project/reference tooling.
- [x] Capability/profile discovery.
- [x] Strict manifest validation.
- [x] Dependency explanation/current composition diagnostics.
- [ ] `foundationkit new` interactive composer.
- [ ] Deterministic project generation.
- [ ] Generated architecture/decision report.
- [ ] Workbench visual composer using the same capability graph.
- [ ] Golden-template tests proving generated projects build/test.

## Phase J — AI as an optional capability

AI is deliberately late so it cannot distort the core architecture.

- [ ] Provider-neutral chat model abstraction.
- [ ] Embedding abstraction.
- [ ] Retriever/vector-store abstraction.
- [ ] Prompt-template contracts.
- [ ] Tool/agent execution boundary.
- [ ] AI observability, redaction, rate/cost controls.
- [ ] Provider adapters only after those boundaries are stable.

## Definition of Done for each reusable capability

A capability is not considered complete until applicable items below are satisfied:

- explicit purpose and non-goals;
- dependency graph entry;
- no unnecessary dependency from the kernel back to the capability;
- provider-neutral public contracts;
- bounded and validated public inputs;
- security/privacy threat review;
- unit tests for success and failure paths;
- architecture tests where dependency boundaries matter;
- package included in Release build/pack when appropriate;
- generated catalog is synchronized;
- README/capability documentation;
- reference-consumer proof when runtime behavior is involved;
- CI, security scan, and CodeQL green;
- compatibility and migration impact documented.

## Current baseline

The repository currently has extracted reusable/reference packages for:

- Auditing;
- Security;
- Identity;
- Authorization;
- Workflow;
- Approvals v1.

Athar provides current consumer evidence for the security, identity, authorization, workflow, and narrow approval surfaces. The capability maturity values remain conservative and do not imply production certification.

The next recommended family after Approvals v1 is **Notifications + Files + Jobs/Messaging**, while advanced approval models remain a separate future expansion rather than being implied by the v1 package.