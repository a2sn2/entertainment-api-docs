# FoundationKit — Current Security and Policy Status

**Living reference. Update this file after every security-relevant change.**

- Source audit baseline: `b9de00ba29928111637786f921c1c01249ddcada` (2026-08-07).
- Current hardening change: `FK-HARDEN-2026-08-07`, PR #34.
- Audit conclusion at baseline: `Strong Engineering Baseline — Security, Governance, Recovery and Production Evidence Required`.
- This file records repository implementation status only. It is not an ISO/IEC 27001 certificate, Statement of Applicability, legal opinion, or production approval.

## Status vocabulary

- `Implemented` — repository control exists in source/configuration.
- `Automated evidence added` — an automated test/gate exists; a specific successful run must still be cited before marking Verified.
- `Verified` — a recorded automated/manual evidence run passed against the stated commit.
- `External configuration required` — repository can express/enforce the contract but production platform evidence is outside the repository.
- `Organizational decision required` — a value/scope/authority must not be invented by code.
- `Open` — control remains to be implemented.

## Baseline findings → current state

| Finding | Current state | Control/evidence |
|---|---|---|
| FK-GOV-001 independent review | **External governance required** | CODEOWNERS + PR evidence template added; PR #34 intentionally must not self-satisfy independent approval |
| FK-GOV-002 protected main | **External configuration required** | protected branch/ruleset and required checks must be evidenced in GitHub settings |
| FK-RISK-001 risk/threat model | **Implemented** | `RISK-REGISTER.md`, `THREAT-MODEL.md`, `SECURITY-DECISIONS.md` |
| FK-SDLC-001 security gates | **Implemented / automated evidence added** | CodeQL, Trivy, NuGet audit, secret gate, SBOM, container scan, negative black-box tests |
| FK-SUP-001 dependency control | **Partially implemented** | NuGet audit + central versions; lock files/source mapping/update automation remain open |
| FK-SUP-002 Actions mutable tags | **Implemented for hardening-touched workflows** | checkout/setup/upload/Pages/CodeQL/Trivy pinned to immutable commit SHAs |
| FK-REL-001 release integrity | **Partially implemented** | CycloneDX SBOM + SHA-256 evidence; signing/attestation identity/authority remains external decision |
| FK-TEST-001 security test depth | **Improved / automated evidence added** | domain security tests + black-box CSRF/BOLA/account-enumeration/maker-checker tests; coverage threshold decision remains open |
| FK-AUTH-001 account lifecycle | **Implemented capability** | confirmation, reset/recovery, password change, TOTP MFA, recovery codes; final MFA scope and production SMTP are external/organizational |
| FK-AUTH-002 password standard/blocklist | **Organizational decision required** | existing baseline remains; final password/breached-password requirements not invented |
| FK-AUTH-003 admin seed | **Implemented production fail-closed control** | production rejects seed; development seed refuses silent promotion of existing user |
| FK-SEC-001 local password disclosure | **Implemented in official launchers** | no normal password echo; local credential files owner-restricted; unified-manager legacy credential-display path requires continued review |
| FK-APP-001 Swagger all environments | **Implemented** | Swagger/UI Development-only |
| FK-APP-002 rate limiter global | **Implemented baseline** | auth partitions by IP; writes by authenticated user/IP fallback; integrated abuse evidence still required |
| FK-APP-003 admin self-review | **Implemented** | domain maker-checker rule + unit/black-box tests |
| FK-APP-004 negative security tests | **Improved** | CSRF/authz/BOLA/account enumeration/maker-checker negative suite; broader ASVS matrix remains pending decision |
| FK-APP-005 readiness disclosure | **Implemented** | public readiness only reports status |
| FK-APP-006 CSP/cache hardening | **Open / app-specific design required** | baseline headers remain; Blazor-compatible CSP/cache policy needs explicit compatibility test |
| FK-APP-007 AllowedHosts wildcard | **Implemented production fail-closed control** | non-development rejects wildcard/blank host list |
| FK-DATA-001 DB transport | **Implemented production contract** | production rejects disabled encryption/certificate trust bypass; Development remains intentionally local/insecure-for-production |
| FK-DATA-002 runtime `sa` | **Implemented production contract / external provisioning required** | production validator rejects `sa`; DBA/platform must provision separate runtime/migration principals |
| FK-DB-001 startup migrations | **Implemented production fail-closed control** | Development opt-in only; production requires controlled deployment migration step |
| FK-DB-002 migration rollback/restore | **Partially implemented** | recovery runbook + DB restore drill; schema-specific rollforward/compensation remains per change |
| FK-AUD-001 tamper evidence | **External configuration required** | central append-only/restricted sink still required; DB audit alone is not claimed tamper-evident |
| FK-AUD-002 structured audit | **Partially implemented/design documented** | security event catalog added; database audit schema enrichment remains open |
| FK-LOG-001 central observability | **External configuration required** | event/alert catalog + incident runbook; SIEM/log retention/alert routing require platform evidence |
| FK-PII-001 privacy lifecycle | **Partially implemented** | PII inventory/minimization rules added; retention/deletion/legal/privacy-notice decisions remain organizational/legal |
| FK-CRY-001 crypto inventory/vault/rotation | **Partially implemented** | crypto/secrets inventory + production fail-closed contracts; Vault/KMS/CA/rotation provider evidence external |
| FK-CRY-002 Data Protection keys | **Implemented capability / external material required** | durable file persistence + X.509 protection; certificate/key lifecycle external |
| FK-BACK-001 restore evidence | **Automated evidence added** | isolated CHECKSUM backup, VERIFYONLY, restore, core-table validation, cleanup |
| FK-BACK-002 encrypted/off-site backup | **External configuration required** | local dev backups owner-restricted; production encryption/off-site/immutability/retention pending platform/organizational decision |
| FK-DOCK-001 container hardening | **Implemented baseline** | non-root app, dropped capabilities, no-new-privileges, health checks; production example adds read-only rootfs/tmpfs |
| FK-DOCK-002 mutable image tags | **Open** | immutable digest/update process must be established and then pinned |
| FK-TUN-001 Development Quick Tunnel | **Accepted demo-only boundary, not production** | random temporary tunnel remains for synthetic demos; no real/sensitive data; production ingress is separate |
| FK-WB-001 unauthenticated Workbench | **Sample-only boundary** | must remain controlled/local; never a production/public-data service |
| FK-CHG-001 formal change evidence | **Implemented repository process** | expanded PR template + change/release evidence model; approval authority remains organizational |
| FK-INC-001 vulnerability reporting | **Open/organizational** | private reporting channel and SLA still require repository/org configuration |
| FK-OPS-001 incident/BCP runbooks | **Implemented repository runbook / external ownership pending** | incident/rollback/recovery/PIR runbook; named responders/authorities/RTO/RPO external |

## Policies impacted by this hardening program

All twelve approved policies are tracked without renaming:

1. Segregation of Duties Policy.
2. Data Transfer Policy.
3. Password Management Policy.
4. Logging and Monitoring Policy.
5. Data Backup Policy.
6. Personally Identifiable Information Protection Policy.
7. Secure Software Development Life Cycle Policy.
8. Malware Protection Policy.
9. Cryptography and Key Management Policy.
10. Application Security Policy.
11. Change Management Policy.
12. Risk Management Policy.

## Decisions that remain deliberately unresolved

No repository change may invent final values for:

- independent reviewer count/approval authority;
- final MFA scope/factor requirements;
- final password parameters and compromised-password source;
- ASVS target level/applicability;
- RPO/RTO;
- data/log/backup retention;
- vulnerability remediation SLA;
- security-exception duration/authority;
- release and residual-risk acceptance authority;
- KMS/Vault/CA/SIEM/backup provider;
- production hosting/network topology.

See `SECURITY-DECISIONS.md`.

## Verification rule

A control above moves from `Implemented`/`Automated evidence added` to `Verified` only after a successful run/evidence ID is recorded in `evidence/STEP-05-INTEGRATED-VERIFICATION.md`. A green build by itself is never equivalent to production approval.