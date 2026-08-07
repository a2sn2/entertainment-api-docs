# FoundationKit / Athar — Incident, Rollback, and Recovery Runbook

This runbook defines repository-visible technical actions. Incident severity, notification deadlines, named responders, RPO/RTO, legal notification, and production authority are organizational decisions.

## 1. Preserve evidence first

- Record incident/change ID, UTC start time, affected environment/build/commit, and reporter.
- Preserve relevant application/edge/database/CI logs and artifact digests.
- Do not paste passwords, tokens, MFA keys, cookies, or PII into issue comments.
- Freeze destructive cleanup until evidence requirements are understood.

## 2. Contain

Depending on scenario:

- revoke/rotate compromised GitHub, SQL, SMTP, certificate, Data Protection, or administrator credentials;
- disable exposed account/session or security-sensitive integration;
- stop Quick Tunnel/public demo exposure;
- block malicious ingress at approved edge/WAF/network layer;
- pause deployment/release automation;
- disable affected artifact/tag and record digest.

## 3. Decide rollback / rollforward / compensation

### Application-only change

Prefer redeploying a previously approved immutable artifact by digest when rollback is safe.

### Database/schema change

Do not assume binary rollback makes schema/data safe. Use the reviewed change record:

1. confirm backup/recovery evidence;
2. identify migration/rollforward/compensating path;
3. preserve data created after deployment;
4. execute through migration identity, not runtime identity;
5. validate schema and business invariants;
6. reconcile affected records.

### Identity/security incident

- rotate/revoke compromised authenticators;
- invalidate affected sessions/security stamps;
- review role changes and MFA disable/regeneration events;
- preserve audit evidence;
- notify users/authorities only according to approved incident/privacy procedure.

## 4. Backup/restore

Repository CI performs a synthetic isolated restore drill. Production recovery additionally requires:

- approved backup source and checksum/integrity evidence;
- encrypted/off-site/immutable storage where required;
- isolated restore target when possible;
- schema and application compatibility validation;
- account/initiative/review/audit integrity checks;
- post-restore reconciliation;
- measured recovery duration/data point compared with approved RTO/RPO once those values exist.

## 5. Validate service after recovery

Minimum technical validation:

- `/health/live` healthy;
- protected/internal readiness confirms dependencies;
- login/account lifecycle works;
- role authorization works;
- maker-checker self-review remains denied;
- CSRF negative test passes;
- representative user/admin workflow succeeds;
- audit/security events reach the approved sink;
- no pending unauthorized migrations;
- artifact digest matches approved release.

## 6. Closeout / PIR

Record:

- root cause;
- detection gap;
- containment and recovery timeline;
- exact commits/artifacts/config changes;
- data/PII/security impact;
- evidence preserved;
- residual risk;
- corrective/preventive actions;
- policy/risk/threat-model updates;
- post-implementation review and approval.

No incident is considered closed merely because the service starts again.