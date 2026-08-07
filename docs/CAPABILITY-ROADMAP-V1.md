# FoundationKit Capability Roadmap v1

This roadmap turns FoundationKit from a reusable core into a composable system-building foundation while keeping the kernel small and provider-neutral.

## Delivery rule

A capability moves through the following lifecycle:

1. **Planned** — vocabulary, boundary, and dependencies are defined.
2. **ReferenceOnly** — behavior is proven in a reference consumer, or an extraction is still under adoption proof.
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
- [ ] Manifest JSON schema and strict parser/validator.
- [ ] Composition diagnostics (`why selected`, dependency path, incompatible choices).
- [ ] Capability compatibility/version metadata.

## Phase B — Governance and security foundations

- [x] Auditing reusable package extracted and packaged.
- [ ] Auditing provider/adoption proof and maturity promotion.
- [ ] Security reusable capability boundary.
- [ ] Identity reusable capability boundary.
- [ ] Authorization: roles, permissions, policies, ownership, scoped access.
- [ ] Sensitive-action/step-up authorization contracts.
- [ ] Maker-checker/four-eyes reusable policy primitive.

## Phase C — Business process capabilities

- [ ] Workflow/state transition engine.
- [ ] Approval engine: single, sequential, parallel, quorum, maker-checker.
- [ ] Tasks/work items.
- [ ] SLA/business-hours/escalation capability.
- [ ] Timeline/activity stream.
- [ ] Comments/notes/mentions.
- [ ] Tags and favorites.

## Phase D — Communication and content

- [ ] Notifications abstraction.
- [ ] Notification templates and localization.
- [ ] SMTP provider.
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
- [ ] Idempotency reusable package.
- [ ] Optimistic concurrency reusable package.
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
- [ ] SMTP provider.
- [ ] Object storage providers.
- [ ] Search providers.
- [ ] Messaging providers.
- [ ] Observability/OpenTelemetry provider wiring.

## Phase I — Project Composer

- [ ] `foundationkit` CLI project.
- [ ] `foundationkit capabilities list`.
- [ ] `foundationkit profiles list`.
- [ ] `foundationkit validate <manifest>`.
- [ ] `foundationkit explain <manifest>`.
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

After the initial implementation sequence:

- Capability Model v1 is merged.
- Machine-readable capability graph and drift validation are merged.
- `FoundationKit.Auditing` is the first extracted opt-in capability package.

The next recommended family is **Security → Identity → Authorization**, followed by **Workflow → Approvals**, because many higher-level business capabilities depend on those boundaries.
