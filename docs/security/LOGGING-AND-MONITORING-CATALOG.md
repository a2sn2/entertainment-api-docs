# FoundationKit / Athar — Logging and Monitoring Event Catalog

This is the repository-side **target event contract**, not evidence that every listed event is already emitted by the current application. The implemented event set must be verified from source/tests and the policy register. Selection of SIEM/observability vendor, retention, alert thresholds, on-call ownership, and escalation times are organizational decisions.

## Implementation status rule

- An event listed here is a required/desired security telemetry contract.
- It becomes `Implemented` only when source code emits it using the approved structured schema.
- It becomes `Verified` only when a test or captured runtime evidence demonstrates emission without prohibited sensitive data.
- Events not yet implemented remain design requirements; this catalog must never be cited by itself as proof of runtime logging coverage.
- Central sink, retention, alert routing, deletion restriction, and operational response remain `External Configuration Required` until deployment evidence exists.

## Event schema

Security-relevant events should be structured and contain only fields needed for detection/investigation:

```text
timestampUtc
service
category
eventName
result
correlationId
actorUserId (when authenticated)
actorRole (when needed)
targetType
targetId
sourceIp (when approved for security telemetry)
reasonCode
```

Never include:

- passwords/password hashes;
- email-confirmation/reset tokens;
- antiforgery tokens/cookies;
- MFA authenticator keys/recovery codes;
- SQL/SMTP credentials;
- full user-provided initiative/review free text in security logs.

## Minimum security events

| Event | Result values | Minimum fields | Detection purpose |
|---|---|---|---|
| `auth.login` | success/failure/locked/not-allowed/mfa-required | correlation, masked/account identifier strategy, source, reason | brute force/credential stuffing/account abuse |
| `auth.mfa.login` | success/failure/locked/recovery-code | correlation, actor/account, source | second-factor abuse |
| `auth.email.confirmation.requested` | accepted/delivery-failed | correlation; do not disclose account existence publicly | delivery/abuse monitoring |
| `auth.email.confirmed` | success/failure | actor/account, correlation | account lifecycle evidence |
| `auth.password.reset.requested` | accepted/delivery-failed | correlation | abuse/delivery health |
| `auth.password.reset` | success/failure | actor/account, correlation | credential lifecycle |
| `auth.password.changed` | success/failure | actor, correlation | sensitive action |
| `auth.mfa.enabled` | success/failure | actor, correlation | sensitive action |
| `auth.mfa.disabled` | success/failure | actor, correlation | high-value alert candidate |
| `auth.mfa.recovery_codes.regenerated` | success/failure | actor, correlation | sensitive action |
| `authorization.denied` | denied | actor/source, policy, target class | BOLA/privilege abuse |
| `initiative.created` | success/idempotent/failure | actor, initiative ID, correlation | business/audit evidence |
| `initiative.reviewed` | approved/rejected/denied-self-review/conflict | actor, target, correlation, decision | maker-checker/business assurance |
| `role.changed` | added/removed/refused | actor, target user, role, correlation | privilege escalation |
| `database.schema.validation` | success/pending-migrations/failure | service, migration identifiers | deployment/change assurance |
| `database.migration` | started/success/failure | build/change ID, migration ID | controlled change evidence |
| `backup.created` | success/failure | backup ID, checksum/evidence ref | recovery assurance |
| `restore.drill` | success/failure | backup ID, isolated DB ID, validation evidence | recovery assurance |
| `security.config.validation` | success/failure | reason code, environment | fail-closed production assurance |
| `tunnel.public_exposure` | started/stopped/refused | environment, target URL classification; no secrets | demo/public exposure audit |

## Alert candidates

Thresholds are deliberately not set here. Organizational monitoring must decide them based on risk/capacity.

- repeated failed/locked administrator login;
- repeated invalid MFA attempts;
- MFA disabled for an administrator;
- role elevation to Administrator;
- maker-checker self-review attempt;
- abnormal authorization-denied rate;
- secret/security scanner failure on protected branch;
- production security-configuration failure;
- database pending migration in production startup;
- restore drill failure;
- audit export/sink interruption;
- public-tunnel use outside approved demo context.

## Integrity/access requirements

Repository audit rows are useful product evidence but are not sufficient as the sole tamper-evident security record because application/database trust is shared. Production must export security events/audit evidence to an access-controlled central sink with restricted deletion and a retention policy.

## Verification

- CI/unit tests must assert sensitive token values are never returned by request-email/reset endpoints.
- Security event code must use structured fields, not string concatenation with secrets/free text.
- The policy register identifies which catalog events are actually implemented/verified.
- Central sink/retention/alert routing is `External Configuration Required` until environment evidence exists.
