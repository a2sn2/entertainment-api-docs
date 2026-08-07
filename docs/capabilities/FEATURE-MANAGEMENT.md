# FoundationKit.FeatureManagement

## Purpose

`FoundationKit.FeatureManagement` provides a small deterministic feature-enable/disable boundary on top of `FoundationKit.Settings`. It allows products to use explicit scoped configuration without turning FoundationKit into a targeting, experimentation, or rollout platform.

Current maturity: **ReferenceOnly**.

## Public surface

- `FeatureId` — normalized bounded feature identifiers.
- `FeatureDefinition` — feature ID, safe default state, and optional description.
- `FeatureEvaluationContext` — settings-resolution context used for evaluation.
- `IFeatureEvaluator` — asynchronous provider-neutral evaluation port.
- `SettingBackedFeatureEvaluator` — reference evaluator using `features.<feature-id>.enabled`.
- `FeatureDecision` / `FeatureDecisionSource` — explicit result and provenance (`Default`, `Setting`, or `InvalidSetting`).

## Evaluation semantics

For each feature:

1. resolve `features.<id>.enabled` through `ISettingReader`;
2. when no value exists, use the feature definition's explicit default;
3. when the value is a valid Boolean, use it and record the matched setting scope;
4. when a value exists but is not a valid Boolean, fail closed to **disabled** and return `InvalidSetting`.

An invalid explicit configuration therefore never falls back to an enabled default.

## Safety boundary

- feature IDs use a bounded safe identifier shape;
- settings inherit FoundationKit.Settings scope and value validation;
- `FeatureDecision.ToString()` omits the matched scope identifier;
- evaluation does not execute expressions, scripts, templates, or arbitrary code;
- no product/user data is copied into the feature definition by the reusable capability.

## Explicit non-goals

v1 does not implement:

- percentage or random rollouts;
- user/segment targeting rules;
- experiments or A/B testing;
- schedules or automatic activation windows;
- remote feature-management vendors;
- feature persistence or administration UI;
- audit/change-approval policy;
- realtime refresh or distributed cache invalidation.

Those concerns require concrete product/provider requirements and are not implied by `ReferenceOnly` maturity.

## Workbench consumer evidence

Workbench registers `workbench.catalog-preview` through the Settings reference source. `GET /api/platform-reference` evaluates the feature using `SettingBackedFeatureEvaluator`, and the SQL Server integration smoke flow asserts that the decision is enabled, setting-backed, and resolved from global scope.

The reference adoption changes no database schema, migrations, authentication, authorization, or existing user/admin workflow.

## Dependency direction

`FoundationKit.FeatureManagement -> FoundationKit.Settings`.

The package does not depend on Domain, Application, Infrastructure, WebApi, Blazor, Identity, Authorization, EF Core, ASP.NET Core, Athar, or Workbench product assemblies.
