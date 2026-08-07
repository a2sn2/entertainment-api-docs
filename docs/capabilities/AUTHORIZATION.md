# FoundationKit.Authorization

`FoundationKit.Authorization` is the provider-neutral authorization capability above `FoundationKit.Identity`.

The capability separates **who the product says a subject is** from **what the product allows that subject to do**. FoundationKit supplies reusable permission and ownership evaluation mechanics; the consuming product owns its roles, permission IDs, business meaning, and persistence strategy.

## Current v1 surface

### Authorization subject

`IAuthorizationSubject` exposes only the identity facts required by the reusable evaluator:

- authenticated/not authenticated;
- current user ID;
- role membership check.

It does not expose HTTP, claims storage, cookies, ASP.NET Core Identity, EF entities, tenant records, or product profile data.

### Permission definitions

`PermissionDefinition` provides a bounded immutable descriptor for product-owned permission IDs.

Permission IDs are normalized to lower case and support letters, digits, `.`, `:`, `-`, and `_`. Examples:

```text
orders.read
orders.approve
finance:payments.release
```

FoundationKit does not ship business permissions such as `Administrator`, `FinanceManager`, or `athar.initiatives.review`. Those names belong to the consuming product.

### Role-to-permission mapping

`RolePermissionGrant` and `RolePermissionMap` map product-owned roles to product-owned permissions.

The map:

- validates and normalizes permission IDs;
- rejects empty grants;
- deduplicates roles and permissions;
- exposes read-only grant collections;
- fails closed for unknown permissions.

This v1 map is in-memory configuration. It does **not** require role/permission database tables. A later provider may load equivalent grants from configuration, SQL, an IdP, or another source without changing the permission vocabulary.

### Authorization evaluator

`IAuthorizationEvaluator` currently supports:

```csharp
bool HasPermission(string permission);

bool CanAccessOwnedResource(
    Guid ownerUserId,
    string privilegedPermission);
```

`RolePermissionAuthorizationEvaluator` grants a permission only when:

1. the subject is authenticated; and
2. at least one role assigned to that permission matches the current subject.

Ownership access is granted when the authenticated subject owns the resource or has the explicitly supplied bypass permission.

Unknown permissions fail closed.

## Athar consumer

Athar is the first real consumer.

Athar owns these permission IDs:

```text
athar.initiatives.read-all
athar.initiatives.review
athar.dashboard.read
```

Athar maps its own `Administrator` role to those permissions. FoundationKit does not know the role name or the permission names.

The application layer now asks semantic questions:

```text
Can this subject read all initiatives?
Can this subject review initiatives?
Can this subject read the dashboard?
Can this subject access this owned initiative?
```

instead of embedding `IsInRole("Administrator")` throughout business logic.

The existing ASP.NET Core `AtharAdministrator` HTTP policy remains as a coarse outer defense. Application-level permission checks remain the business authorization boundary. This is intentional defense in depth rather than duplicate ownership of the same policy.

## Dependency direction

```text
FoundationKit.Domain
        ↑
FoundationKit.Application
        ↑
FoundationKit.WebApi
        ↑
FoundationKit.Security
        ↑
FoundationKit.Identity
        ↑
FoundationKit.Authorization
```

No lower package depends on Authorization.

## Explicitly out of scope for v1

- product role names;
- product permission IDs;
- role/permission database schema;
- EF migrations;
- user-role persistence;
- ASP.NET Core policy registration helpers;
- tenant or organization scope;
- row-level security implementation;
- ABAC policy languages;
- external PDP/OPA/Cedar engines;
- role administration UI;
- entitlement synchronization from an IdP.

These remain consumers/providers until enough evidence exists for a reusable abstraction.

## Security properties

- unauthenticated subjects receive no role permissions;
- unknown permissions fail closed;
- owner access requires an authenticated matching user ID;
- privileged ownership bypass requires an explicit permission;
- permission grants are exposed through read-only collections;
- the reusable package does not create a universal administrator bypass.

## Maturity

Authorization remains `ReferenceOnly` in Capability Model v1 during this first extraction. Promotion requires additional consumer evidence and stable integration with later organization/multi-tenancy scopes.
