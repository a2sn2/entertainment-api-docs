# FoundationKit / Athar — Threat Model

## Scope

This model covers the reusable FoundationKit packages, Workbench reference consumer, Athar full-stack reference product, SQL Server, local launchers, Docker, GitHub Actions, artifacts, GitHub Pages, and temporary Cloudflare Quick Tunnel exposure.

It is an engineering threat model, not a legal/privacy impact assessment.

## Trust boundaries

```text
Developer workstation
    |
    | source / secrets / local SQL
    v
GitHub repository -----------------------> GitHub Actions / artifacts
    |                                            |
    |                                            v
    |                                      package/container outputs
    |
    +--> GitHub Pages (static Atlas/demo only)

Internet user
    |
    | HTTPS (production edge or temporary Quick Tunnel)
    v
Athar ASP.NET Core host
    |
    | cookie auth + CSRF + authorization
    v
Application / Domain
    |
    | EF Core
    v
SQL Server
```

Every arrow crossing a process, host, identity, network, or data store is a trust boundary and needs an explicit control.

## Protected assets

- Source code and branch history.
- CI workflow definitions and credentials.
- NuGet/container artifacts and provenance.
- User identities, password hashes, authenticator/recovery material.
- Session and antiforgery cookies/tokens.
- Initiative data, ownership, review decisions, and audit records.
- SQL credentials and application secrets.
- Backups and restore media.
- Data Protection keys.
- Logs, correlation IDs, and monitoring evidence.
- Administrative role assignments and release authority.

## Principal threat scenarios

### T-01 — Unauthorized source/release change

**Threat:** A maintainer, compromised token, or malicious contributor changes sensitive code/workflows without independent review.

**Controls:** PR workflow, CI, CODEOWNERS, protected `main`, immutable action references, release evidence.

**Repository status:** CODEOWNERS/evidence can be implemented in code; required independent review remains an external GitHub configuration/organizational decision.

### T-02 — Supply-chain compromise

**Threat:** Vulnerable/malicious NuGet package, Action, container base, or downloaded tool enters build/runtime.

**Controls:** dependency audit, lock strategy, pinned Actions/digests, SBOM, secret scanning, container scanning, approved registries, signed/attested artifacts.

### T-03 — Credential stuffing / brute force / auth DoS

**Threat:** Automated login attempts compromise accounts or a global limiter blocks legitimate users.

**Controls:** Identity lockout, partitioned per-IP/account rate limits, MFA for protected roles, breached-password screening, alerts.

### T-04 — Broken object authorization

**Threat:** A user reads another user's initiative by manipulating IDs.

**Controls:** ownership checks and negative tests. Administrators are authorized separately.

### T-05 — Maker-checker conflict

**Threat:** An administrator reviews an initiative they own.

**Control:** Domain invariant `reviewerUserId != OwnerUserId`, plus API/E2E negative test and audit event.

### T-06 — CSRF/session abuse

**Threat:** A browser is tricked into state-changing requests using an authenticated cookie.

**Controls:** SameSite cookie, antiforgery token on writes, secure cookie in non-development, token refresh after auth lifecycle changes, negative tests.

### T-07 — Schema/data corruption during deployment

**Threat:** Application startup automatically applies an unsafe migration with runtime credentials.

**Controls:** production startup migration disabled by default, controlled migration identity/process, backup/rollback/rollforward evidence, post-migration validation.

### T-08 — PII or secret leakage

**Threat:** Sensitive fields appear in logs, audit free text, local env files, backups, CI artifacts, browser storage, or public demo data.

**Controls:** structured audit, data inventory, redaction, secret scanning, no-real-data demo rule, encrypted/restricted backup, least-data DTOs.

### T-09 — Database interception / untrusted certificate

**Threat:** Production SQL traffic is intercepted when encryption/certificate validation is disabled.

**Controls:** production configuration validator requiring encrypted, validated transport; separate development topology clearly marked insecure-for-production.

### T-10 — Audit tampering / insufficient evidence

**Threat:** The same application identity can alter audit records, or evidence lacks actor/result/correlation context.

**Controls:** structured audit schema, restricted writer, external append-only/central sink, retention/access controls, security alerts.

### T-11 — Backup failure discovered during incident

**Threat:** A `.bak` exists but is corrupt, incomplete, inaccessible, or cannot recover the service in time.

**Controls:** `RESTORE VERIFYONLY`, isolated restore drill, application validation, scheduled recovery test, RPO/RTO decisions, encrypted/off-site/immutable copies.

### T-12 — Public development tunnel exposure

**Threat:** Quick Tunnel makes a development application and test database reachable from the Internet, possibly with seeded administrator credentials.

**Controls:** tunnel refuses non-development/production-sensitive use, explicit demo-only confirmation, no real data, short lifetime, current app readiness check, MFA before sensitive public use.

### T-13 — Container breakout / unnecessary privilege

**Threat:** A compromised app process runs with root-equivalent privilege or extra Linux capabilities.

**Controls:** non-root user, no-new-privileges, drop capabilities where compatible, read-only filesystem where compatible, minimal image, resource limits, image scan.

### T-14 — Workbench mistaken for secure product

**Threat:** The intentionally unauthenticated Workbench is exposed on LAN/Internet and receives real data.

**Controls:** loopback-only default, explicit insecure-sample warning for network exposure, documentation/banner, no real data.

## Abuse-case verification matrix

| Abuse case | Required evidence |
|---|---|
| User requests another user's initiative ID | 404/forbidden behavior test without data leakage |
| Admin reviews own initiative | deterministic domain/API rejection test |
| POST without antiforgery token | 400 security problem response |
| Reused valid client request ID | idempotent response, no duplicate row |
| Parallel review or stale rowversion | one valid transition, conflict handling |
| Repeated bad login | account/IP limiter behavior and lockout evidence |
| Production startup with wildcard host / insecure DB / startup migration | configuration validation fails closed |
| Production app requests `/swagger` | not exposed anonymously by default |
| External `/health/ready` | minimal result without SQL implementation details |
| Quick Tunnel under non-development environment | launcher refuses |
| Restore drill | backup verified, restored to isolated DB, application/integrity checks pass |

## Residual/environment risks

These cannot be closed solely by repository code:

- final MFA policy and recovery authority;
- production ingress/WAF/network segmentation;
- SIEM/log retention and alert ownership;
- KMS/Vault/HSM/certificate authority;
- protected-branch reviewer count and release authority;
- RPO/RTO and backup retention;
- legal/privacy retention and data-subject obligations;
- penetration test and load test acceptance;
- immutable/off-site backup service;
- production database account provisioning.

They remain explicit production gates, not implied successes.