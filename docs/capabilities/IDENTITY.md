# FoundationKit.Identity

`FoundationKit.Identity` is the provider-neutral account-lifecycle capability above `FoundationKit.Security`.

Its first extraction deliberately does **not** turn FoundationKit into an identity provider and does not move ASP.NET Core Identity, EF stores, SMTP, product copy, or user tables into the reusable kernel.

## Current v1 surface

### Account security policy

`AccountSecurityOptions` owns the reusable decisions currently shared by account consumers:

- require confirmed email;
- require MFA for administrator/privileged policy composition;
- password required length;
- digit/lowercase/uppercase/non-alphanumeric requirements.

`AccountSecurityOptionsValidator` validates the supported structural range without selecting an organizational password standard. The consuming product remains responsible for its approved policy values.

Athar keeps the existing `AccountSecurity` configuration section, but the policy type is now owned by FoundationKit rather than the product.

### Notification port

`IAccountNotificationSender` is the delivery boundary for:

- email-confirmation tokens;
- password-reset tokens;
- independent account-security notifications.

`AccountSecurityNotification` currently covers:

- password changed;
- password reset;
- MFA enabled;
- MFA disabled;
- recovery codes regenerated.

FoundationKit.Identity does not implement SMTP and does not format product messages. Athar's `SmtpAccountNotificationSender` remains an adapter and retains the Arabic product copy and fail-closed production TLS configuration.

Tokens and destination addresses are intentionally passed directly to the delivery port and must not be logged by adapters.

### Step-up policy

`IdentityStepUpPolicy` expresses reusable factor requirements for sensitive account operations:

| Operation | Required factors |
|---|---|
| Change password | Password |
| Setup MFA | Password |
| Disable MFA | Password + MFA |
| Regenerate recovery codes | Password + MFA |

This is policy vocabulary, not a verifier. The consuming identity adapter still decides how a password, authenticator code, recovery code, passkey, or external IdP assertion is verified.

The existing Athar implementation continues to perform fresh password + MFA verification before disabling MFA or regenerating recovery codes and continues to send independent security notifications.

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
```

No lower package depends on Identity.

## Explicitly out of scope for v1

- user/entity persistence;
- ASP.NET Core Identity stores;
- database schema or EF migrations;
- OAuth/OIDC server implementation;
- external identity providers;
- session persistence;
- token generation algorithms;
- authenticator enrollment implementation;
- SMTP/SMS/push providers;
- tenant membership and authorization;
- product-specific email wording.

These boundaries keep Identity reusable while allowing later adapters to integrate ASP.NET Core Identity, external IdPs, or other account systems.

## Consumer evidence

Athar is the first real consumer:

- its account policy is bound through `FoundationKit.Identity.AccountSecurityOptions`;
- its existing endpoints resolve the FoundationKit `IAccountNotificationSender` port;
- its SMTP implementation remains in `Athar.Infrastructure`;
- its account-security event names are supplied by FoundationKit.Identity;
- no database migration or endpoint contract changes are required for this extraction.

## Maturity

The capability remains `ReferenceOnly` in Capability Model v1 during this first extraction. Promotion to `Preview` should follow only after the API survives broader consumer/adaptor evidence rather than being inferred from one product.
