# FoundationKit.Notifications

`FoundationKit.Notifications` is a small provider-neutral reference capability for bounded notification messages and delivery results. It deliberately separates reusable delivery contracts from product copy, account-security semantics, channel providers, persistence, and orchestration.

## Implemented v1 surface

The package provides:

- `NotificationMessage` with bounded destination, title, body, and purpose fields;
- normalization of surrounding whitespace;
- rejection of unsupported control characters in destination/title and unsafe purpose-code shapes;
- line breaks/tabs allowed only in the message body;
- a `ToString()` representation that exposes only the non-sensitive purpose and never the destination or body;
- `INotificationSender` as the channel/provider boundary;
- `NotificationDeliveryStatus` / `NotificationDeliveryResult` with `Delivered`, `NotConfigured`, and `Failed` outcomes.

The reusable result intentionally does not carry provider exceptions, recipient addresses, message bodies, credentials, or tokens.

## Security boundary

A notification body may legitimately contain a one-time confirmation/reset token. Therefore the package treats destination and body as sensitive operational data even though it does not classify or encrypt them itself.

Consumers and providers must not log `Destination` or `Body` by default. The v1 message's `ToString()` is deliberately safe for diagnostics, but this does not make the underlying properties non-sensitive.

The package does not own transport encryption, provider credentials, secrets management, retry policy, retention, or delivery-history storage.

## Consumer evidence

### Athar

Athar is the first real consumer. Its previous combined account-security SMTP sender is split into two responsibilities:

1. `AccountSecurityNotificationAdapter` implements the existing `FoundationKit.Identity.IAccountNotificationSender` contract. It owns Arabic product copy, confirmation/reset tokens, and account-security event wording.
2. `SmtpNotificationSender` implements `FoundationKit.Notifications.INotificationSender`. It owns SMTP transport, TLS/configuration usage, and conversion of the generic title/body/destination into `MailMessage`.

The adapter maps the generic delivery result back to the existing `bool` account-notification contract, preserving endpoint behavior. SMTP remains an Athar/provider concern rather than becoming a dependency of FoundationKit.Notifications.

### Madar

Madar v0.5 is the second independent product consumer. It uses the unchanged v1 contracts for operational case notifications after assignment, approval decision, and resolution.

Madar owns:

- Arabic product copy and purpose codes;
- recipient selection and Identity-backed destination lookup;
- when a notification is appropriate for a case event;
- bounded audit evidence containing purpose, target user ID, and delivery status only;
- the decision to commit the business operation before attempting transport delivery.

Madar currently composes the existing SMTP provider as an optional email transport. Empty provider configuration resolves to `NotConfigured`; provider failure resolves to `Failed`. Neither reusable result contains destination/body/provider exception details.

This second consumer demonstrates that the same provider-neutral message/result boundary works for both account-security mail (Athar) and operational case mail (Madar) without expanding the public API. It does not demonstrate multi-channel routing, durable delivery, or production notification orchestration.

## Explicit non-goals

Notifications v1 does **not** implement:

- templates or template persistence;
- localization/resource lookup;
- user notification preferences;
- channel routing or fallback;
- queues, retries, delayed delivery, or scheduling;
- bulk campaigns;
- delivery history or read/unread state;
- in-app inbox UI;
- SMS, push, webhook, or vendor SDK integrations;
- bounce/complaint processing;
- provider credentials or secret storage.

Those require additional consumer evidence and, where appropriate, separate capabilities such as Jobs, Messaging, Webhooks, Settings, or provider packages.

## Maturity

Capability Model v1 remains `ReferenceOnly`. The v1 contracts are implemented, packaged, tested, and now consumed by two independent products, but both current consumers use the same SMTP transport class and neither proves queues/retries, multi-channel routing, provider diversity, delivery-history semantics, or production operational certification.

`ReferenceOnly` is not a production approval, external certification, or claim that templates, preferences, queues, retries, or multiple channels are implemented.
