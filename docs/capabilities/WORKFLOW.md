# FoundationKit.Workflow

`FoundationKit.Workflow` is the deterministic state-transition capability above `FoundationKit.Auditing`.

Its v1 scope is intentionally small: a product defines states, triggers, and transitions; FoundationKit validates that the transition graph is deterministic, resolves allowed transitions, fails closed for unknown transitions, and can produce a bounded audit request for a successful transition.

It is not a BPMN engine, scheduler, workflow database, visual designer, or approval system.

## Current v1 surface

### Transition definitions

`WorkflowTransitionDefinition` contains:

- transition ID;
- source state;
- trigger;
- destination state.

Identifiers are normalized to lower case, bounded to 160 characters, and support letters, digits, `.`, `:`, `-`, and `_`.

### Workflow definition

`WorkflowDefinition` owns an immutable transition collection.

Construction fails when:

- no transitions are supplied;
- transition IDs are duplicated;
- more than one transition uses the same `fromState + trigger` pair.

This makes resolution deterministic.

Runtime resolution is explicit:

```csharp
if (workflow.TryResolve(currentState, trigger, out var transition))
{
    // apply transition.ToState in the product aggregate/application logic
}
```

Unknown state/trigger pairs return `false`; FoundationKit never invents a fallback destination.

### Transition record

`WorkflowTransition` is the resolved immutable transition contract containing:

- workflow ID;
- transition ID;
- from state;
- trigger;
- to state.

Persistence and history storage are consumer/provider responsibilities.

### Audit integration

`WorkflowTransitionAudit.CreateRequest(...)` maps a successful transition to the existing `FoundationKit.Auditing.AuditRequest` contract using bounded metadata:

- `workflow_id`;
- `transition_id`;
- `from_state`;
- `trigger`;
- `to_state`.

The workflow package therefore composes with Auditing without choosing an audit sink, database, SIEM, retention policy, or product-specific actor model.

## Athar consumer

Athar is the first real consumer.

`InitiativeWorkflow` is product-owned and defines exactly two transitions:

```text
submitted + approve -> approved
submitted + reject  -> rejected
```

`Initiative.Review` now asks the workflow definition to resolve the transition instead of assigning the destination status through a product-local ternary expression.

Athar still owns:

- the state strings;
- trigger strings;
- self-review rule;
- review-note validation;
- aggregate mutation;
- domain events;
- persistence and concurrency;
- API authorization.

The existing externally visible behavior is preserved: invalid decisions remain validation failures and a second review remains a conflict.

## Dependency direction

```text
FoundationKit.Domain/Application
          ↑
FoundationKit.Auditing
          ↑
FoundationKit.Workflow
```

Workflow does not depend on Security, Identity, Authorization, EF, ASP.NET Core, or Athar. Architecture tests enforce that boundary.

## Explicitly out of scope for v1

- database workflow instances;
- EF migrations or schema;
- background timers/schedulers;
- escalation jobs;
- BPMN parsing/execution;
- visual workflow designer;
- dynamic expression languages;
- arbitrary script execution;
- HTTP callbacks/webhooks;
- human task assignment;
- approval quorum/maker-checker logic;
- tenant/organization scoping;
- workflow-version migration.

Those are separate capabilities or future adapters. In particular, `FoundationKit.Approvals` will build on Workflow + Authorization + Auditing rather than being embedded in this package.

## Security and reliability properties

- invalid identifiers are rejected at construction/input boundaries;
- ambiguous state/trigger transitions are rejected;
- unknown transitions fail closed;
- transition collections are read-only to consumers;
- no arbitrary code or expressions are executed from workflow definitions;
- no persistence or external I/O is performed by the transition resolver;
- audit integration uses the existing bounded audit metadata contract.

## Maturity

This is the first repository-backed extraction with Athar as a real consumer. The API should remain conservatively classified until additional consumers and the Approvals capability validate the shape; implementation existence alone is not treated as proof of broad production maturity.
