# STEP-01 — Athar application-security P0 controls

## Policies

- Segregation of Duties Policy.
- Password Management Policy.
- Secure Software Development Life Cycle Policy.
- Application Security Policy.
- Change Management Policy.
- Risk Management Policy.

## Implemented controls

1. **Maker-checker rule** — `Initiative.Review` rejects `reviewerUserId == OwnerUserId` with `Athar.SelfReviewNotAllowed`.
2. **Swagger exposure** — Swagger/UI now execute only in `Development`.
3. **Rate-limit partitioning** — authentication traffic is partitioned by remote IP; authenticated writes are partitioned by user ID with IP fallback. Identity lockout remains the per-account control.
4. **Production startup guard** — non-development startup rejects wildcard `AllowedHosts`, `AdminSeed`, automatic migrations, and automatic role seeding.
5. **Database-change separation** — `DatabaseStartupOptions` now explicitly controls startup migration and role seeding; production defaults are off, development defaults are explicit in `appsettings.Development.json`.
6. **Admin seed escalation prevention** — seeding refuses to promote an existing non-administrator account automatically.
7. **Readiness minimization** — public readiness no longer reports the SQL Server implementation detail.
8. **Development topology labeling** — Athar Compose is explicitly labeled development/test and opts into migration/role automation intentionally.

## Tests added

- Owner cannot review own initiative and initiative remains submitted.
- Development permits local convenience settings.
- Production rejects wildcard hosts.
- Production rejects admin seed.
- Production rejects automatic migrations.
- Production rejects automatic role seeding.
- Authentication rate partition differs by remote IP.
- Authenticated write partition uses user identity.

## Findings affected

- `FK-APP-001`: Implemented, integrated CI pending.
- `FK-APP-002`: Implemented baseline partitioning, integrated/E2E rate-limit evidence pending.
- `FK-APP-003`: Implemented with domain regression test, integrated CI pending.
- `FK-APP-004`: Improved; broader negative suite remains open.
- `FK-APP-005`: Implemented, integrated smoke evidence pending.
- `FK-APP-007`: Implemented production fail-closed configuration guard, CI pending.
- `FK-AUTH-003`: Partially implemented; admin seed is development-only and silent promotion is refused.
- `FK-DB-001`: Implemented production fail-closed startup rule; controlled migration runbook still required.

## Residual risk

- MFA/email confirmation/recovery are not addressed in this step.
- Production DB transport and least-privilege principals are not addressed in this step.
- Current rate values remain existing development values; no new organizational policy values were invented.
- Full integrated verification is intentionally deferred to the hardening PR CI.