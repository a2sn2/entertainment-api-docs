# FoundationKit.Notifications.Smtp

`FoundationKit.Notifications.Smtp` is the reusable SMTP provider adapter for `FoundationKit.Notifications`. It translates the provider-neutral `NotificationMessage` contract into `System.Net.Mail` transport behavior without owning account semantics, product copy, persistence, queues, or secrets management.

## Implemented v1 surface

The provider package supplies:

- `SmtpNotificationOptions`;
- `SmtpNotificationOptionsValidator`;
- `SmtpNotificationSender : INotificationSender`;
- `ISmtpNotificationObserver` for bounded operational diagnostics.

The sender snapshots validated options when it is constructed. Missing host/from configuration returns `NotConfigured`; supported SMTP/format/operation failures return `Failed`; a caller cancellation token is preserved as cancellation rather than converted to failure.

## Dependency boundary

The provider depends on `FoundationKit.Notifications` and the .NET SMTP transport APIs. It does not depend on:

- FoundationKit Identity, Authorization, Workflow, Approvals, Auditing, WebApi, Infrastructure, or Blazor;
- EF Core or ASP.NET Core;
- Athar product assemblies;
- Microsoft logging/options abstractions.

This keeps the reusable Notifications capability provider-neutral while allowing a product to opt into SMTP explicitly.

## Configuration ownership

`SmtpNotificationOptions` contains transport values only:

- host;
- port;
- TLS enablement flag;
- optional username/password;
- from address.

The provider validates structural constraints such as the TCP port range and control characters. It deliberately does **not** decide organizational production policy such as which relay is approved, whether a deployment is allowed to send without authentication, secret rotation, certificate trust, or whether TLS is mandatory for a particular environment.

Those decisions remain deployment/product policy. Athar's production validator continues to require its existing fail-closed SMTP/TLS configuration.

## Safe diagnostics

`ISmtpNotificationObserver` receives only:

- notification purpose; and
- provider exception **type name** for failed delivery.

It never receives destination, message body, token, password, SMTP username/password, or provider exception object. A consuming application can implement the observer with its own logging/telemetry policy without forcing a logging dependency into the provider package.

## Athar consumer evidence

Athar maps its existing `AccountSecurityDeliveryOptions` into `SmtpNotificationOptions` and injects `SmtpNotificationSender` behind `INotificationSender`.

Athar still owns:

- the `AccountSecurity` configuration section and production validation;
- Arabic product wording;
- confirmation/reset tokens;
- Identity lifecycle meaning;
- logging via `AtharSmtpNotificationObserver`;
- endpoint behavior and delivery-success mapping.

This means extracting SMTP does not move account semantics or product configuration into the provider.

## Explicit non-goals

The provider does not implement:

- SMTP queues or retries;
- delayed/scheduled sending;
- templates or localization;
- multi-provider routing/fallback;
- credentials or secret persistence;
- credential/certificate rotation;
- bounce or complaint processing;
- delivery history;
- bulk campaigns;
- SMS, push, webhook, or non-SMTP channels.

Those concerns belong to higher-level Notifications/Jobs/Messaging/provider work and are not implied by this package.

## Maturity

The Capability Model already marks `provider-smtp` as `ReferenceOnly`. This package provides concrete reusable implementation and Athar consumer evidence for that existing catalog entry; it does not promote the provider to `Preview` or `Stable`.

`ReferenceOnly` is not Production Approval, external certification, or evidence that a deployment's SMTP relay/security controls are compliant.