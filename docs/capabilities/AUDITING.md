# FoundationKit.Auditing

`FoundationKit.Auditing` is the first extracted optional capability in the FoundationKit capability model.

It is intentionally separate from the kernel. A project that does not need reusable audit recording does not reference the package.

## Purpose

The package provides provider-neutral primitives for recording business/security audit events without deciding where those events are stored.

The consuming product owns the sink implementation, persistence technology, retention policy, access controls, SIEM integration, and legal/compliance decisions.

## Public model

- `AuditRequest` — caller-owned description of the action being recorded.
- `AuditContext` / `IAuditContextAccessor` — actor, correlation, tenant, and source context supplied by the host.
- `AuditEvent` — normalized immutable event written to a sink.
- `AuditOutcome` — `Succeeded`, `Failed`, or `Denied`.
- `IAuditSink` — provider-neutral persistence/export boundary.
- `IAuditRecorder` / `AuditRecorder` — stamps the current context and UTC time then writes exactly one event to the sink.

## Example

```csharp
var auditEvent = await recorder.RecordAsync(
    new AuditRequest(
        Action: "customer.updated",
        SubjectType: "customer",
        SubjectId: customerId.ToString(),
        Outcome: AuditOutcome.Succeeded,
        ReasonCode: "profile-maintenance",
        Attributes: new Dictionary<string, string>
        {
            ["branch"] = "sanaa"
        }),
    cancellationToken);
```

The product supplies an `IAuditSink` implementation, for example SQL, an append-only store, a central security platform, or a fan-out adapter.

## Security-by-default boundaries

The reusable event model deliberately does **not** accept arbitrary request/response bodies or before/after object graphs.

It also:

- bounds action, subject, identifier, reason, and attribute lengths;
- limits the number of attributes per event;
- rejects control characters in identifier/attribute values;
- copies attributes before exposing them to prevent later caller mutation;
- rejects common sensitive attribute names such as password, token, authorization, cookie, secret, connection string, OTP/TOTP, recovery code, and private key variants.

These controls reduce accidental leakage. They are not a substitute for a product-specific data-classification policy. Callers must still avoid placing PII, credentials, payment data, or unapproved free text in audit attributes.

## Provider boundary

The package contains no SQL Server, Redis, cloud, SIEM, or logging-provider dependency.

A provider should implement:

```csharp
public interface IAuditSink
{
    ValueTask WriteAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken = default);
}
```

Future provider packages may add database, OpenTelemetry, SIEM, or message-bus adapters without changing the auditing contract.

## Failure semantics

`AuditRecorder` does not silently swallow sink failures. Whether an audited business operation should fail closed, retry, use an outbox, or continue with degraded audit availability is a product/risk decision and must be made by the consuming workflow.

## What this package does not claim

It does not by itself provide:

- tamper-proof or immutable storage;
- centralized SIEM retention;
- regulatory retention periods;
- cryptographic event signing;
- legal non-repudiation;
- production alerting;
- a full change-data-capture system.

Those belong to provider, deployment, governance, or future capability layers.
