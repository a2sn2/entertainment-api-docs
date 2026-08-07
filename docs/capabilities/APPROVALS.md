# FoundationKit.Approvals

`FoundationKit.Approvals` is a provider-neutral reference capability for small approval decisions that compose FoundationKit authorization, workflow, and auditing primitives without owning product persistence or routing rules.

## Implemented v1 surface

The current package provides:

- strict `approve` / `reject` decision normalization through `ApprovalDecisions`;
- fail-closed rejection of unknown decision tokens;
- `ApprovalResolution`, which binds a normalized decision to a resolved `WorkflowTransition`;
- `ApprovalPolicy.HasDecisionPermission` for explicit authorization eligibility;
- `ApprovalPolicy.Evaluate` for permission-first maker-checker evaluation;
- `ApprovalEligibility` outcomes: `Allowed`, `PermissionDenied`, and `MakerCheckerViolation`;
- `ApprovalDecisionAudit.CreateRequest` for a bounded, provider-neutral audit intent.

The package depends on `FoundationKit.Workflow`, `FoundationKit.Authorization`, and `FoundationKit.Auditing`. It does not select a database, identity provider, ASP.NET Core policy, queue, scheduler, or audit sink.

## Maker-checker semantics

Maker-checker is intentionally small and explicit in v1. A checker must have the product-supplied permission and must not have the same normalized actor identifier as the maker. Permission denial is evaluated before maker-checker disclosure.

Consumers should still retain critical domain invariants in their aggregate or domain model. The reusable gate is an application-level guard, not a reason to remove defense-in-depth from the product domain.

## Athar consumer evidence

Athar's initiative review flow is the first real consumer:

1. the application verifies `athar.initiatives.review` before loading the initiative;
2. after loading, `ApprovalPolicy.Evaluate` enforces maker-checker using the initiative owner as maker and the current reviewer as checker;
3. `ApprovalDecisions` normalizes the requested approve/reject decision;
4. the decision resolves through Athar's existing `InitiativeWorkflow`;
5. the `Initiative` aggregate still re-checks self-review and transition validity before mutation;
6. Athar keeps its existing review persistence, product audit entry, concurrency handling, domain events, routes, and DTOs.

This preserves the existing non-disclosure order and domain invariant while removing reusable approval mechanics from product orchestration.

## Explicit non-goals

The current package does **not** implement:

- sequential or multi-stage approvals;
- parallel approvals;
- quorum, voting, or weighted approval rules;
- delegation, substitution, or escalation;
- dynamic approver discovery or organization routing;
- approval persistence tables or EF migrations;
- approval inbox/task UI;
- SLA timers, background jobs, notifications, or webhooks;
- arbitrary expressions or script execution.

Those concerns require additional product evidence and, where appropriate, separate FoundationKit capabilities rather than being implied by the v1 package.

## Maturity

Capability Model v1 marks Approvals as `ReferenceOnly`. That means the implemented v1 surface is real and tested, but one consumer and a deliberately narrow decision model are not evidence of broad production maturity.

`ReferenceOnly` is not a certification, deployment approval, or claim that advanced approval patterns are implemented.
