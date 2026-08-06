# Repository Boundaries

## Repository identity

The repository is becoming **FoundationKit for .NET**. Its planned GitHub repository name is `foundationkit-dotnet`.

The repository hosts four different concerns, each with explicit ownership:

| Concern | Responsibility | Current location |
|---|---|---|
| FoundationKit Core | Reusable technical packages | `platform/core/` |
| Showcase | Runnable interactive public experience | repository root |
| EntertainmentDocs | First validated reference consumer | `platform/src`, `platform/apps`, product tests and deploy assets |
| Future products | Independent consumers of packages | separate repositories |

## Core boundary

The core may expose stable, provider-neutral technical abstractions and adapters. It may not own product terminology, workflows, contracts, routes, policies, migrations, or UI.

## Reference-consumer boundary

EntertainmentDocs may depend on FoundationKit. FoundationKit may never depend on EntertainmentDocs.

```text
FoundationKit  ←  EntertainmentDocs
```

The reverse direction is forbidden.

## Showcase boundary

The Showcase explains the repository and helps visitors shape an idea. It is not a runtime host for FoundationKit and is not proof that a business product can be generated from a text prompt.

It must remain:

- browser-only unless a deliberate backend is introduced;
- free of product secrets;
- explicit about public versus private contact paths;
- independent from EntertainmentDocs runtime and database.

## Schema ownership

EntertainmentDocs EF Core migrations remain the database schema source of truth. Repository renaming or navigation changes do not modify schema.

## Change classification

Every change must state whether it affects:

- reusable package behavior;
- product behavior;
- API contracts;
- database schema;
- frontend behavior;
- deployment or operations;
- documentation only.

Do not mix broad file relocation with functional changes unless the migration itself is the reviewed purpose of the pull request.
