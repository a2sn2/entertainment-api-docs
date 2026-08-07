# FoundationKit — Current Security and Policy Status

**Living reference. Update this file after every security-relevant change.**

- Source audit baseline: `b9de00ba29928111637786f921c1c01249ddcada` (2026-08-07).
- Current hardening change: `FK-HARDEN-2026-08-07`, PR #34.
- Integrated technical evidence: `evidence/STEP-05-INTEGRATED-VERIFICATION.md`, verified source commit `2e8f78eb98ce02a5fb4f8b02db720391f9c77f98`.
- Audit conclusion at baseline: `Strong Engineering Baseline — Security, Governance, Recovery and Production Evidence Required`.
- This file records repository implementation status only. It is not an ISO/IEC 27001 certificate, Statement of Applicability, legal opinion, residual-risk acceptance, or production approval.

## Status vocabulary

- `Implemented` — repository control exists in source/configuration.
- `Automated evidence added` — an automated test/gate exists; a specific successful run must still be cited before marking Verified.
- `Verified` — a recorded automated/manual evidence run passed against the stated commit and scope.
- `External configuration required` — repository can express/enforce the contract but production platform evidence is outside the repository.
- `Organizational decision required` — a value/scope/authority must not be invented by code.
- `Open` — control remains to be implemented or deliberately designed for a later production topology.

## Baseline findings → current state

| Finding | Current state | Control/evidence |
|---|---|---|
| FK-GOV-001 independent review | **External governance required** | CODEOWNERS + PR evidence template added; PR #34 intentionally must not self-satisfy independent approval |
| FK-GOV-002 protected main | **External configuration required** | protected branch/ruleset and required checks must be evidenced in GitHub settings |
| FK-RISK-001 risk/threat model | **Implemented** | `RISK-REGISTER.md`, `THREAT-MODEL.md`, `SECURITY-DECISIONS.md`; upstream no-fix image risk tracked as `R-FK-016` |
| FK-SDLC-001 security gates | **Verified** | CI, CodeQL, Trivy, NuGet audit, secret gate, SBOM, package/integrity evidence and negative suite recorded in STEP-05 |
| FK-SUP-001 dependency control | **Verified baseline / further hardening available** | NuGet audit, central/transitive security floors, SBOM, Trivy, and weekly Dependabot for NuGet/Actions/Docker; lock files/source mapping remain optional next-hardening items |
| FK-SUP-002 Actions mutable tags | **Verified for hardening-touched workflows** | checkout/setup/upload/Pages/CodeQL/Trivy references are pinned to immutable commit SHAs and the workflows passed |
| FK-REL-001 release integrity | **Verified baseline / external signing decision remains** | CycloneDX SBOM + SHA-256 package/publish evidence passed; signing/attestation identity/authority remains external/organizational |
| FK-TEST-001 security test depth | **Verified for current automated suite** | unit + black-box CSRF/BOLA/account-enumeration/maker-checker abuse tests passed; final ASVS target/coverage threshold remains organizational |
| FK-AUTH-001 account lifecycle | **Implemented capability / production configuration pending** | confirmation, reset/recovery, password change, TOTP MFA, recovery codes; final MFA scope and production SMTP are external/organizational |
| FK-AUTH-002 password standard/blocklist | **Organizational decision required** | existing baseline remains; final password/breached-password requirements not invented |
| FK-AUTH-003 admin seed | **Verified production fail-closed baseline** | production rejects seed; development seed refuses silent promotion; solution/tests/build passed |
| FK-SEC-001 local password disclosure | **Verified in official CI/launcher scope** | no normal password echo, local credential protection, generated CI credentials are masked; Windows launcher checks passed |
| FK-APP-001 Swagger all environments | **Verified baseline** | Swagger/UI Development-only; production restriction covered by configuration tests/build |
| FK-APP-002 rate limiter global | **Verified for current automated abuse scope** | auth partitions by IP; writes by authenticated user/IP fallback; black-box suite passed |
| FK-APP-003 admin self-review | **Verified** | domain maker-checker rule + unit/black-box tests passed |
| FK-APP-004 negative security tests | **Verified for current suite** | CSRF/authz/BOLA/account enumeration/maker-checker suite passed; broader ASVS matrix remains a future organizationally-scoped expansion |
| FK-APP-005 readiness disclosure | **Verified** | integrated smoke verified public readiness reports status and does not expose SQL implementation detail |
| FK-APP-006 CSP/cache hardening | **Open / app-specific design required** | baseline headers remain; a Blazor-compatible CSP/cache policy needs explicit compatibility testing before enforcement |
| FK-APP-007 AllowedHosts wildcard | **Verified production fail-closed baseline** | non-development rejects wildcard/blank host list; security configuration tests passed |
| FK-DATA-001 DB transport | **Implemented production contract / external certificate evidence required** | production rejects disabled encryption/certificate trust bypass; Development remains intentionally local/insecure-for-production |
| FK-DATA-002 runtime `sa` | **Implemented production contract / external provisioning required** | production validator rejects `sa`; DBA/platform must provision separate runtime/migration principals |
| FK-DB-001 startup migrations | **Verified production fail-closed baseline** | Development opt-in only; production requires controlled deployment migration step |
| FK-DB-002 migration rollback/restore | **Verified restore baseline / per-change schema recovery remains** | real CHECKSUM backup, VERIFYONLY, isolated restore, schema-qualified table validation and cleanup passed; destructive schema rollback/rollforward stays change-specific |
| FK-AUD-001 tamper evidence | **External configuration required** | central append-only/restricted sink still required; DB audit alone is not claimed tamper-evident |
| FK-AUD-002 structured audit | **Partially implemented/design documented** | structured logging and security event catalog improved; database audit schema enrichment/central correlation remains future work |
| FK-LOG-001 central observability | **External configuration required** | event/alert catalog + incident runbook; SIEM/log retention/alert routing require platform evidence |
| FK-PII-001 privacy lifecycle | **Partially implemented** | PII inventory/minimization rules added; retention/deletion/legal/privacy-notice decisions remain organizational/legal |
| FK-CRY-001 crypto inventory/vault/rotation | **Partially implemented** | crypto/secrets inventory + production fail-closed contracts; Vault/KMS/CA/rotation provider evidence external |
| FK-CRY-002 Data Protection keys | **Implemented capability / external material required** | durable file persistence + X.509 protection; certificate/key lifecycle external |
| FK-BACK-001 restore evidence | **Verified** | isolated CHECKSUM backup, VERIFYONLY, real restore, schema-qualified core-table validation and cleanup passed in CI run `31184047139` |
| FK-BACK-002 encrypted/off-site backup | **External configuration required** | local dev backups owner-restricted; production encryption/off-site/immutability/retention pending platform/organizational decision |
| FK-DOCK-001 container hardening | **Verified baseline** | Athar non-root runtime assertion passed; Workbench also runs as `app`; Compose hardening and container-policy checks passed |
| FK-DOCK-002 mutable image tags | **Partially mitigated / production pinning still open** | weekly Docker Dependabot + Trivy gates added; final production digest pin/update promotion process remains to be established |
| FK-SUP-003 upstream unfixed image CVEs | **Tracked residual technical risk** | fixable HIGH/CRITICAL image findings block CI; complete SARIF retains unfixed findings; `R-FK-016` governs monitoring and reassessment |
| FK-TUN-001 Development Quick Tunnel | **Accepted demo-only boundary, not production** | random temporary tunnel remains for synthetic demos; no real/sensitive data; production ingress is separate |
| FK-WB-001 unauthenticated Workbench | **Sample-only boundary** | must remain controlled/local; never a production/public-data service |
| FK-CHG-001 formal change evidence | **Implemented repository process** | expanded PR template + change/release/evidence model; approval authority remains organizational |
| FK-INC-001 vulnerability reporting | **Open/organizational** | private reporting channel and SLA still require repository/org configuration |
| FK-OPS-001 incident/BCP runbooks | **Implemented repository runbook / external ownership pending** | incident/rollback/recovery/PIR runbook; named responders/authorities/RTO/RPO external |

## Integrated verification evidence

`docs/security/evidence/STEP-05-INTEGRATED-VERIFICATION.md` records successful evidence against source commit `2e8f78eb98ce02a5fb4f8b02db720391f9c77f98`:

- FoundationKit CI run `31184047139` — build/test/publish/pack/security evidence and Workbench/Athar SQL integration including real restore drill: success.
- FoundationKit Security Scan run `31184044933` — Trivy gates/SARIF and black-box negative security tests: success.
- FoundationKit CodeQL run `31184045528` — C# and JavaScript/TypeScript: success.
- FoundationKit Windows Launcher Check run `31184044965` — Windows PowerShell 5.1 parser/smoke validation: success.

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

## Final repository classification

The repository-side technical hardening represented by `FK-HARDEN-2026-08-07` is **Verified for the explicitly automated scope recorded in STEP-05**.

That statement must not be rewritten as `Production Approved`. Production approval still requires independent review/protected-branch evidence and the external platform/organizational controls listed in STEP-05.
