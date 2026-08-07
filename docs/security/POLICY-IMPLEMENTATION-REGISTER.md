# FoundationKit — Policy Implementation Register

> **Canonical finding-level source of truth** for repository security, governance, recovery, and production evidence.
>
> Repository: `a2sn2/foundationkit-dotnet`  
> Baseline before this hardening program: `b9de00ba29928111637786f921c1c01249ddcada`  
> Program branch: `hardening/global-grade-baseline`  
> Source assessment: `FoundationKit_ISO27001_Repository_Audit_AR.md` (2026-08-07)  
> Independent PR #34 review: blocking findings `PR34-REV-01`, `PR34-APP-01`, `PR34-AUTH-01`, `PR34-AUTH-02`, `PR34-CRY-01`, `PR34-EVID-01`, `PR34-EVID-02`; recommended `PR34-AUTH-03`, `PR34-PASS-01`, `PR34-LOG-01`, `PR34-SBOM-01`.

`CURRENT-SECURITY-STATUS.md` is an executive view and MUST remain consistent with this register. When they conflict, this register controls finding-level status.

## Status model

- `Open` — confirmed repository gap with no implemented control.
- `Implemented / verification pending` — control exists in source/configuration but the latest affected source head has not yet completed the required evidence run.
- `Verified` — reproducible evidence demonstrates the control in the explicitly stated repository scope.
- `Partially Satisfied` — useful control exists but the finding is not fully satisfied in repository/deployment scope.
- `Pending Organizational Decision` — a value/scope/authority must not be invented in code.
- `External Configuration Required` — completion depends on GitHub/hosting/DBA/KMS/SIEM/backup or another external platform.
- `Residual Risk Tracked` — the risk is recorded and monitored; this does not mean accepted.
- `Residual Risk Accepted` — requires named authority, date, rationale, and evidence; never inferred.

A green build is not production approval. `Verified` always names its scope; organizational/external requirements remain separate.

## Mandatory policy set

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

## Findings register

| ID | Sev. | Finding / risk | Policies | Current status | Implemented control / evidence / residual action |
|---|---:|---|---|---|---|
| FK-GOV-001 / PR34-REV-01 | High | Independent review / maker-checker for repository changes | SoD, Change, SDLC | **External Configuration Required** | CODEOWNERS + PR evidence model exist. PR #34 still requires a real independent GitHub `APPROVE`; self-approval is not accepted. |
| FK-GOV-002 | High | Protected `main` / required checks evidence | SoD, Change | **External Configuration Required** | Repository workflows exist; GitHub branch/ruleset configuration must be evidenced externally before governance closure. |
| FK-RISK-001 | High | Repository risk/threat model absent | Risk, SDLC, AppSec | **Verified baseline** | `RISK-REGISTER.md`, `THREAT-MODEL.md`, `SECURITY-DECISIONS.md`; residual/external risks remain explicitly tracked. |
| FK-SDLC-001 | High | Security CI gates absent | SDLC, Malware, AppSec | **Verified baseline** | Secret scan, NuGet audit, CodeQL, Trivy, negative suite, SBOM/integrity evidence, build/test/publish/pack; STEP-05 records prior successful runs. Post-review affected code is re-verified before merge. |
| FK-SUP-001 | High | Dependency governance weak | Malware, SDLC | **Verified baseline / further hardening available** | Central package floors, NuGet audit, Trivy, baseline CycloneDX inventory, Dependabot. Lock files/source mapping remain optional next-hardening items or registry decisions. |
| FK-SUP-002 | Medium | Mutable GitHub Action references | SDLC, Malware | **Verified for hardening-touched workflows** | Security-sensitive workflow actions touched by the program are SHA-pinned and have passed. |
| FK-REL-001 / PR34-SBOM-01 | High | Release integrity/provenance incomplete | SDLC, Malware, Crypto | **Partially Satisfied** | CycloneDX **dependency inventory / baseline SBOM** + SHA-256 package/publish manifests exist. This is not claimed as complete provenance/signing/attestation; signing authority remains external/organizational. |
| FK-TEST-001 | High | Security-negative coverage weak | SDLC, AppSec | **Implemented / verification pending** | Unit + black-box authz/CSRF/BOLA/enumeration/maker-checker plus new MFA step-up and runtime 429 tests. Latest affected head must pass before status returns to Verified. |
| FK-AUTH-001 / PR34-AUTH-01 / PR34-AUTH-02 | High | Account lifecycle / MFA lifecycle incomplete | Password, AppSec | **Implemented / verification pending** | Confirmation/reset/change password, TOTP/recovery login, full password+fresh-MFA step-up for disable/recovery rotation, security notifications for password/MFA lifecycle. Production SMTP/config and final MFA scope remain external/organizational. |
| FK-AUTH-002 / PR34-PASS-01 | Medium–High | Password standard not organization-approved; no compromised-password blocklist | Password, Risk | **Pending Organizational Decision** | Password length/composition values are now configurable and must be explicitly supplied in Production; DTO/UI no longer impose enterprise values. Final standard and compromised-password source/capability remain an explicit organizational decision before Production Approved. |
| FK-AUTH-003 | High | Admin seed can create/promote administrator | Password, SoD, Change | **Verified production fail-closed baseline** | Production rejects seed; Development seed refuses silent promotion; prior automated evidence passed. |
| FK-SEC-001 | Medium | Local/admin credential disclosure | Password, Crypto, Logging | **Verified launcher/CI baseline** | Official launchers avoid routine password echo; generated CI credentials are masked; local credential protection exists. |
| FK-APP-001 | High | Swagger exposed outside Development | AppSec, Data Transfer | **Verified baseline** | Swagger/UI Development-only; configuration/build tests previously passed. |
| FK-APP-002 / PR34-APP-01 / PR34-EVID-02 | High | Rate limiting/proxy identity can collapse clients or be spoofed; prior evidence overclaim | Password, AppSec, Data Transfer, Risk | **Implemented / verification pending** | Explicit trusted-proxy allow-list; `ForwardedHeaders` before HTTPS/rate limiting; untrusted headers ignored in tests; auth partition per effective client IP; write partition per user/IP; black-box suite now requires real HTTP `429`. Final production ingress IP/topology remains deployment evidence. |
| FK-APP-003 | High | Administrator can review own initiative | SoD, AppSec | **Verified** | Domain maker-checker rule + unit/black-box test passed in prior evidence. |
| FK-APP-004 | Medium | Negative application-security coverage weak | AppSec, SDLC | **Implemented / verification pending** | Current suite covers authz, CSRF, BOLA, enumeration, maker-checker, MFA step-up, runtime 429. Broader ASVS target remains an organizational scope decision. |
| FK-APP-005 | Medium | Readiness leaks implementation detail | AppSec, Logging | **Verified** | Public readiness returns status only; prior integration evidence passed. |
| FK-APP-006 | Medium | App-specific CSP/cache policy incomplete | AppSec | **Open / design required** | Baseline headers remain. Blazor-compatible CSP/cache policy requires explicit compatibility/security testing; no false claim of completion. |
| FK-APP-007 / PR34-AUTH-03 | Medium | Production settings can silently fall back to permissive defaults | AppSec, Password, Change | **Implemented / verification pending** | Production requires explicit AllowedHosts plus explicit true/false decisions for confirmed email, admin MFA and reverse-proxy mode; password policy values are also explicit. |
| FK-DATA-001 | High | DB transport encryption/cert validation | Data Transfer, Crypto | **Implemented production contract / external certificate evidence** | Production rejects disabled encryption and `TrustServerCertificate=True`; deployment must prove trusted server certificate/route. |
| FK-DATA-002 | High | Runtime SQL `sa` / privilege separation | SoD, AppSec, Change | **Implemented production contract / external provisioning** | Production rejects `sa`; platform/DBA must provision separate least-privilege runtime/migration identities. |
| FK-DB-001 | High | Startup migrations in Production | Change, SDLC, SoD | **Verified production fail-closed baseline** | Production rejects automatic migration/role seeding; reviewed deployment migration step required. |
| FK-DB-002 | High | Migration/recovery evidence | Change, Backup | **Verified restore baseline / change-specific schema action remains** | Real CHECKSUM backup, VERIFYONLY, isolated restore and schema-qualified table checks passed. Destructive schema changes still require per-change rollback/rollforward plan. |
| FK-AUD-001 | High | Audit shares application DB trust boundary | Logging, AppSec | **External Configuration Required** | DB audit is useful but not claimed tamper-evident; central append-only/restricted sink required. |
| FK-AUD-002 / PR34-LOG-01 | Medium | Structured security audit/event coverage incomplete | Logging, AppSec | **Partially Satisfied** | Event schema/catalog is explicitly a **target contract**, not proof every event is emitted. Runtime emission/DB audit enrichment/central correlation remain work. |
| FK-LOG-001 | High | Central observability/alerting/retention absent | Logging | **External Configuration Required** | Catalog/runbook exist; SIEM, retention, alert routing and on-call evidence require platform/organizational implementation. |
| FK-PII-001 | High | PII lifecycle/retention/deletion incomplete | PII | **Partially Satisfied** | PII inventory/minimization exists; legal basis, privacy notice, retention/deletion decisions remain organizational/legal. |
| FK-CRY-001 / PR34-CRY-01 | High | Secret/crypto transport/lifecycle incomplete | Crypto, Data Transfer | **Implemented repository baseline / external provider evidence** | Crypto inventory, SQL TLS checks, SMTP TLS fail-closed, secret contracts. Vault/KMS/CA/rotation provider and operational evidence remain external. |
| FK-CRY-002 | High | Data Protection keys not durable/protected | Crypto, Password | **Implemented capability / external material required** | Durable file persistence + X.509 protection capability; certificate/key storage/rotation lifecycle is external. |
| FK-BACK-001 | Critical | Backup exists without proven restore | Backup, Risk | **Verified** | CHECKSUM backup, VERIFYONLY, real isolated restore, core-table validation and cleanup passed in CI evidence. |
| FK-BACK-002 | High | Production backup encryption/off-site/retention | Backup, Crypto, PII | **External Configuration Required** | Local dev backups restricted; production encrypted/off-site/immutable storage and retention require deployment/organizational evidence. |
| FK-DOCK-001 | High | Container hardening weak | Malware, AppSec, SDLC | **Verified baseline** | Non-root app runtime, capability/no-new-privilege controls, health checks; prior integration assertion passed. |
| FK-DOCK-002 | Medium | Mutable image tags/digests | Malware, SDLC | **Partially Satisfied** | Dependabot + Trivy gates; production digest pin/update/promotion process remains open. |
| FK-SUP-003 | High where applicable | Upstream unfixed image CVEs | Malware, Risk | **Residual Risk Tracked** | Fixable HIGH/CRITICAL findings block CI; unfixed findings remain visible in SARIF and `R-FK-016`; no implicit acceptance. |
| FK-TUN-001 | High if public | Development Quick Tunnel exposed publicly | Data Transfer, AppSec, PII | **Accepted demo-only boundary** | Temporary random tunnel is synthetic-demo only; not a production ingress and must not carry real/sensitive data. |
| FK-WB-001 | High if exposed | Workbench intentionally lacks auth | AppSec, Password | **Sample-only boundary** | Controlled/local reference only; not a production/public-data service. |
| FK-CHG-001 / PR34-EVID-01 | High | Change/evidence chain inconsistent | Change, Risk, SDLC | **Implemented / verification pending** | This canonical register is synchronized with `CURRENT-SECURITY-STATUS`; PR/evidence/runbooks exist. Post-review evidence is recorded before merge. |
| FK-INC-001 | Medium | Vulnerability reporting channel/SLA | Risk, Malware, Logging | **Pending Organizational Decision / External Configuration** | Private reporting channel, response ownership and SLA require repository/org decision/configuration. |
| FK-OPS-001 | High | Incident/rollback/recovery operations incomplete | Logging, Backup, Risk | **Implemented repository runbook / external ownership pending** | Incident/rollback/recovery/PIR runbook exists; named responders, authorities, RPO/RTO remain organizational. |

## PR #34 review-closure matrix

| Review ID | Repository action | Current state |
|---|---|---|
| PR34-REV-01 | Independent GitHub review + protected branch evidence | **External blocker remains** |
| PR34-APP-01 | Trusted forwarded headers, explicit proxy IP allow-list, ordering before HTTPS/rate limiting, trusted/untrusted proxy tests | **Implemented; latest CI pending** |
| PR34-AUTH-01 | Password + fresh TOTP/recovery proof for MFA disable/recovery rotation | **Implemented; black-box verification pending** |
| PR34-AUTH-02 | Independent email security notifications for password/MFA lifecycle | **Implemented capability; delivery provider remains production configuration** |
| PR34-CRY-01 | Production SMTP TLS fail-closed | **Implemented; configuration test pending** |
| PR34-EVID-01 | Canonical register synchronized; executive status derived from it | **Implemented by this update** |
| PR34-EVID-02 | Real runtime 429 black-box assertion added | **Implemented; security workflow pending** |
| PR34-AUTH-03 | Explicit Production decisions required instead of silent false fallback | **Implemented; tests pending** |
| PR34-PASS-01 | Password values configurable + explicit in Production; DTO/UI hard-coded enterprise policy removed | **Implemented capability; final standard/blocklist still organizational** |
| PR34-LOG-01 | Event catalog explicitly labeled target contract; runtime coverage remains partial | **Corrected evidence claim** |
| PR34-SBOM-01 | SBOM terminology narrowed to dependency inventory/baseline SBOM; no full-provenance claim | **Corrected evidence claim** |

## Organizational decisions intentionally not invented

The repository MUST NOT silently invent:

- required independent reviewer count or approval authority;
- final MFA scope/factor requirements;
- final password parameters and compromised-password source;
- ASVS target level/applicability;
- RPO/RTO;
- log/PII/backup retention periods;
- vulnerability remediation SLA;
- exception validity duration/authority;
- release/residual-risk acceptance authority;
- production Vault/KMS/CA/SIEM/backup provider;
- production hosting/network topology.

Production configuration now fails closed where the repository can require **an explicit decision** without choosing the decision itself.

## Verification rule

A finding moves from `Implemented / verification pending` to `Verified` only when this register points to reproducible evidence for the affected source head (test/workflow/commit/manual record). External/organizational status is never converted to Verified merely because CI is green.
