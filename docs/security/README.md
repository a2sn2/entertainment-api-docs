# FoundationKit Security, Policy, and Production Evidence

This directory is the living repository-side reference for security/governance implementation. It complements, but does not replace, organizational policy documents, GitHub settings evidence, production platform evidence, or external audit/certification.

## Living status

- [`POLICY-IMPLEMENTATION-REGISTER.md`](POLICY-IMPLEMENTATION-REGISTER.md) — policy/finding/control/evidence register.
- [`RISK-REGISTER.md`](RISK-REGISTER.md) — repository-visible risks and treatments.
- [`THREAT-MODEL.md`](THREAT-MODEL.md) — assets, trust boundaries, abuse cases, mitigations, residual risk.
- [`SECURITY-DECISIONS.md`](SECURITY-DECISIONS.md) — values/authorities that must be decided organizationally instead of invented in code.

## Change and release assurance

- [`CHANGE-AND-RELEASE-EVIDENCE.md`](CHANGE-AND-RELEASE-EVIDENCE.md) — Definition of Ready/Done and release evidence package.
- [`INCIDENT-RECOVERY-RUNBOOK.md`](INCIDENT-RECOVERY-RUNBOOK.md) — technical incident, rollback, rollforward, restore, and PIR path.
- `.github/pull_request_template.md` — mandatory per-change policy/risk/evidence checklist.
- `.github/CODEOWNERS` — ownership routing; independent review still requires protected-branch/reviewer configuration.

## Data / privacy / cryptography / observability

- [`PII-DATA-INVENTORY.md`](PII-DATA-INVENTORY.md)
- [`CRYPTO-AND-SECRETS-INVENTORY.md`](CRYPTO-AND-SECRETS-INVENTORY.md)
- [`LOGGING-AND-MONITORING-CATALOG.md`](LOGGING-AND-MONITORING-CATALOG.md)

## Evidence trail

`evidence/` contains step-by-step implementation evidence. Each file states what was implemented, what was tested, findings affected, and residual/external gaps. A control is not marked satisfied solely because source code exists.

## Automated security gates

- `scripts/security/scan-repository.py` — deterministic high-confidence tracked-source secret gate.
- `.github/workflows/codeql.yml` — CodeQL C# and JavaScript/TypeScript SAST.
- `.github/workflows/security-scan.yml` — Trivy repository, secret, misconfiguration, and container scanning plus black-box negative Athar security tests.
- `scripts/security/generate-sbom.py` — CycloneDX dependency inventory from the resolved NuGet graph.
- `scripts/security/check-container-hardening.py` — repository-controlled Athar container policy checks.
- `scripts/security/negative-athar.sh` — CSRF/authz/BOLA/account-enumeration/maker-checker abuse cases.
- `scripts/verify-athar-restore.sh` — isolated SQL backup/restore drill with checksum and table validation.

## Production boundary

Repository controls deliberately fail closed where a safe decision is universal (for example production must not use `sa`, wildcard hosts, startup schema changes, unvalidated SQL TLS, or persistent seed admin escalation). Values that depend on organization, risk appetite, regulation, hosting, or operations remain explicit decisions, including MFA scope/factor requirements, password parameters, reviewer count, ASVS target, RPO/RTO, retention, vulnerability SLA, release/risk authority, KMS/Vault/CA provider, central monitoring provider, and production network architecture.

See `../PRODUCTION-READINESS-AR.md` and `../../deploy/athar-production.example.yml` for the deployment boundary.