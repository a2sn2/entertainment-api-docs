# FoundationKit — Policy Implementation Register

> Living reference for repository security, governance, recovery, and production evidence.
>
> Repository: `a2sn2/foundationkit-dotnet`
> Baseline before this hardening program: `b9de00ba29928111637786f921c1c01249ddcada`
> Program branch: `hardening/global-grade-baseline`
> Source assessment: `FoundationKit_ISO27001_Repository_Audit_AR.md` (2026-08-07)

## Status model

- `Open` — confirmed gap, no accepted control yet.
- `In Progress` — implementation or evidence is being built.
- `Implemented` — code/configuration exists but final independent evidence is not complete.
- `Verified` — automated/manual evidence demonstrates the control in the reviewed scope.
- `Pending Organizational Decision` — implementation depends on a value/authority that must not be invented in code.
- `External Configuration Required` — cannot be completed only through repository files.
- `Residual Risk Accepted` — requires named authority and dated evidence; never inferred.

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

## Program rule

Every hardening step MUST update this register with:

`Finding → Policies → Risk → Control → Tests → Evidence → Result → Residual Risk → Next Action`.

A green build is not equivalent to production approval.

## Findings register

| ID | Severity | Finding | Primary policies | Status | Evidence / next action |
|---|---|---|---|---|---|
| FK-GOV-001 | High | Independent review not evidenced for prior PR | SoD, Change, SDLC | Open | Add CODEOWNERS/PR evidence requirements; repository setting still needs independent reviewer configuration. |
| FK-GOV-002 | High | Branch protection not evidenced | SoD, Change | External Configuration Required | Configure protected `main` after organizational review-count decision. |
| FK-RISK-001 | High | No repository risk register/threat model/acceptance workflow | Risk, SDLC, AppSec | In Progress | `RISK-REGISTER.md`, `THREAT-MODEL.md`, and evidence templates added in this program. |
| FK-SDLC-001 | High | Security CI gates missing | SDLC, Malware, AppSec | Open | Add native dependency audit, secret scan, security test lane, SBOM/provenance evidence where supportable. |
| FK-SUP-001 | High | No dependency locks/source mapping/automation | Malware, SDLC | Open | Introduce NuGet audit and lock strategy without breaking current build; source mapping remains a registry decision. |
| FK-SUP-002 | Medium | GitHub Actions use mutable tags | SDLC, Malware | Open | Pin known actions to immutable SHAs where verifiable. |
| FK-REL-001 | High | No SBOM/signing/attestation/provenance | SDLC, Malware, Crypto | Open | Add generated dependency inventory/SBOM artifact; signing authority remains organizational. |
| FK-TEST-001 | High | No coverage/security-negative gate | SDLC, AppSec | Open | Add targeted negative security tests and test result evidence. |
| FK-AUTH-001 | High | MFA/email confirmation/reset/recovery absent | Password, AppSec | Open | Build production-capable identity lifecycle without assuming unapproved factors/timeouts. |
| FK-AUTH-002 | Medium | Password constants not policy-approved; no compromised-password screening | Password, Risk | Pending Organizational Decision | Keep capability configurable; do not invent enterprise values. |
| FK-AUTH-003 | High | Admin seed may create/promote administrator automatically | Password, SoD, Change | Open | Restrict seed to explicit development/test use and forbid silent promotion. |
| FK-SEC-001 | Medium | Local admin password may be persisted/printed | Password, Crypto, Logging | Open | Remove routine credential echo; mask and restrict local secret storage. |
| FK-APP-001 | High | Swagger exposed in all environments | AppSec, Data Transfer | Open | Development-only by default; explicit protected exposure only when configured. |
| FK-APP-002 | High | Rate limiting not partitioned by account/IP | Password, AppSec, Risk | Open | Introduce partitioned policies and regression tests. |
| FK-APP-003 | High | Administrator can review own initiative | SoD, AppSec | Open | Enforce reviewer != owner in domain and tests. |
| FK-APP-004 | Medium | Negative security test coverage is weak | AppSec, SDLC | Open | Add CSRF/auth/self-review/rate-limit/health exposure tests where practical. |
| FK-APP-005 | Medium | Public readiness reveals database implementation detail | AppSec, Logging | Open | Return minimal public readiness response. |
| FK-APP-006 | Medium | Header baseline lacks app-specific CSP/cache policy | AppSec | Open | Add safe cache policy now; CSP requires Blazor compatibility validation. |
| FK-APP-007 | Medium | `AllowedHosts=*` lacks production guard | AppSec, Change | Open | Add production startup validation and explicit allow-list configuration. |
| FK-DATA-001 | High | Development DB transport disables certificate validation/encryption | Data Transfer, Crypto | Open | Keep clearly development-only topology; add production template with encrypted connection requirements. |
| FK-DATA-002 | High | Runtime DB identity uses `sa` in development topology | SoD, AppSec, Change | Open | Add production least-privilege topology guidance/template; runtime/migration split. |
| FK-DB-001 | High | Migrations auto-run at application startup | Change, SDLC, SoD | Open | Make startup migration environment/config controlled; production defaults to off. |
| FK-DB-002 | High | Migration rollback/recovery evidence absent | Change, Backup | Open | Add migration/recovery runbook and isolated verification path. |
| FK-AUD-001 | High | Audit shares application DB trust boundary and is not tamper-evident | Logging, AppSec | Open | Improve structured audit schema; immutable external sink remains environment control. |
| FK-AUD-002 | Medium | Audit lacks correlation/result/before-after metadata | Logging, AppSec | Open | Extend audit context in a controlled migration. |
| FK-LOG-001 | High | No central observability/alerting/retention evidence | Logging | External Configuration Required | Repository adds event catalog and OpenTelemetry-ready guidance; sink/retention require environment decision. |
| FK-PII-001 | High | No PII inventory/retention/deletion/privacy lifecycle | PII | Open | Add repository PII inventory and retention-decision register; legal values remain organizational. |
| FK-CRY-001 | High | No crypto/secret/certificate inventory/rotation | Crypto | Open | Add inventory and production configuration gates; vault choice remains environment decision. |
| FK-CRY-002 | High | Data Protection key persistence/protection not defined | Crypto, Password | Open | Add configurable persistent key path and production validation; key-encryption mechanism requires certificate/key decision. |
| FK-BACK-001 | Critical | Backup exists but restore is not proven | Backup, Risk | Open | Add isolated restore verification workflow/script and evidence instructions. |
| FK-BACK-002 | High | Local backups lack production encryption/retention/offsite guarantees | Backup, Crypto, PII | Open | Mark local backup as development-only; document production gates. |
| FK-DOCK-001 | High | API container lacks explicit non-root/hardening controls | Malware, AppSec, SDLC | Open | Run as non-root and add safe Compose hardening compatible with the app. |
| FK-DOCK-002 | Medium | Mutable image tags | Malware, SDLC | Pending Organizational Decision | Digest pinning requires approved update process and verified digests. |
| FK-TUN-001 | High if public | Quick Tunnel can expose development app publicly | Data Transfer, AppSec, PII | Open | Add explicit demo-safety checks/warnings and production environment refusal. |
| FK-WB-001 | High if exposed | Workbench intentionally has no auth | AppSec, Password | Open | Enforce loopback-only default and explicit insecure sample exposure warning. |
| FK-CHG-001 | High | No formal change/rollback/PIR evidence chain | Change, Risk | In Progress | PR and release evidence templates added in this program. |
| FK-INC-001 | Medium | Vulnerability reporting channel/SLA not mature | Risk, Malware, Logging | Pending Organizational Decision | Improve `SECURITY.md`; response SLA requires owner approval. |
| FK-OPS-001 | High | Incident/rollback/recovery runbooks incomplete | Logging, Backup, Risk | Open | Add operational runbooks and production gate matrix. |

## Organizational decisions intentionally not invented

The repository MUST NOT silently invent the following:

- required reviewer count;
- final MFA scope/factor policy;
- password length/composition/breach-screening source beyond currently documented development defaults;
- ASVS target level;
- RPO/RTO;
- log/PII/backup retention periods;
- vulnerability remediation SLA;
- exception validity duration;
- release authority or risk acceptance authority;
- production secret manager/KMS/certificate provider;
- production hosting/network architecture.

See `SECURITY-DECISIONS.md`.

## Verification rule

A finding moves to `Verified` only when this register points to reproducible evidence (test name, workflow run, commit/PR, command output, or external configuration record).