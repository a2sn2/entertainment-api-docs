# FoundationKit — Current Security and Policy Status

**Living executive reference. `POLICY-IMPLEMENTATION-REGISTER.md` is the canonical finding-level source of truth and this file MUST remain consistent with it.**

- Source audit baseline: `b9de00ba29928111637786f921c1c01249ddcada` (2026-08-07).
- Current hardening change: `FK-HARDEN-2026-08-07`, PR #34.
- Prior integrated technical evidence: `evidence/STEP-05-INTEGRATED-VERIFICATION.md`.
- Independent read-only review of PR #34 produced blocking follow-up findings; repository-side closures are implemented and awaiting the latest affected-head evidence before merge.
- This file is not an ISO/IEC 27001 certificate, Statement of Applicability, legal opinion, residual-risk acceptance, or Production Approval.

## Status vocabulary

- `Implemented / verification pending` — repository control exists but latest affected source evidence is not complete.
- `Verified` — reproducible evidence passed for the explicitly stated repository scope.
- `Partially Satisfied` — useful controls exist but the finding remains incomplete.
- `External Configuration Required` — completion depends on GitHub/deployment/platform controls outside repository code.
- `Organizational Decision Required` — a value/scope/authority must not be invented by code.
- `Open` — repository/design work remains.

## PR #34 independent-review closure status

| Review finding | Current state | Repository closure |
|---|---|---|
| PR34-REV-01 independent approval | **External blocker remains** | CODEOWNERS/evidence process exists, but PR #34 still needs an independent GitHub `APPROVE`; the author cannot self-satisfy SoD. |
| PR34-APP-01 reverse proxy / forwarded headers | **Implemented / verification pending** | Explicit reverse-proxy decision; exact trusted proxy IP allow-list; no trust-all forwarded headers; middleware runs before HTTPS redirection/rate limiting; trusted/untrusted header tests added. |
| PR34-AUTH-01 MFA full re-authentication | **Implemented / verification pending** | MFA disable and recovery-code rotation require current password + fresh TOTP or recovery factor; black-box negative/positive path added. |
| PR34-AUTH-02 independent security notifications | **Implemented capability** | Email security notifications added for password reset/change, MFA enable/disable, and recovery-code regeneration; production delivery still depends on approved SMTP/provider configuration. |
| PR34-CRY-01 SMTP TLS fail-closed | **Implemented / verification pending** | Production startup rejects `SmtpEnableSsl != true`; username/password are not forced because an approved trusted relay may not use Basic Auth. |
| PR34-EVID-01 evidence-register inconsistency | **Closed in repository** | `POLICY-IMPLEMENTATION-REGISTER.md` is now canonical and synchronized; this file is the derived executive view. |
| PR34-EVID-02 rate-limit evidence overclaim | **Implemented / verification pending** | Security suite now proves actual middleware rejection with HTTP `429`; proxy/partition unit tests cover trusted/untrusted forwarded headers. |
| PR34-AUTH-03 silent security defaults | **Implemented / verification pending** | Production requires explicit decisions for confirmed email, administrator MFA, reverse-proxy mode, and password-policy values instead of accepting missing values as false/default. |
| PR34-PASS-01 hard-coded password standard | **Improved; final standard remains organizational** | Password policy is configurable; production requires explicit values; transport DTO/UI no longer impose an enterprise value. Compromised-password source/final policy remain an organizational decision. |
| PR34-LOG-01 event catalog vs runtime coverage | **Evidence claim corrected** | Logging catalog explicitly describes a target contract. Current implementation remains partial; central SIEM/retention/alerts remain external. |
| PR34-SBOM-01 SBOM/provenance terminology | **Evidence claim corrected** | Current artifact is called a CycloneDX dependency inventory/baseline SBOM; complete signing/provenance/attestation is not claimed. |

## Baseline findings — executive view

| Area | Current state | Notes |
|---|---|---|
| Independent review / protected `main` | **External Configuration Required** | Independent approval and protected-branch/required-check evidence remain before governance closure. |
| Risk/threat model | **Verified baseline** | Risk register, threat model, decision register and tracked residual technical risks exist. |
| Secure SDLC / malware gates | **Verified baseline; post-review re-verification pending** | Secret scan, NuGet audit, CodeQL, Trivy, dependency inventory/SBOM, integrity evidence, build/test/publish/pack. |
| Dependency/supply-chain baseline | **Verified baseline / partial provenance** | Dependabot + vulnerability gates + SHA-pinned security-sensitive Actions. Full artifact signing/provenance remains external/next-hardening. |
| Account lifecycle / MFA | **Implemented / verification pending** | Confirmation/reset/change, TOTP/recovery login, full MFA step-up for sensitive factor changes and independent notifications. Final MFA scope/provider remains organizational/external. |
| Password standard | **Organizational Decision Required** | Values are configurable and explicit in Production; final length/composition/blocklist policy is deliberately not invented. |
| Admin seed / local credential handling | **Verified baseline** | Production seed rejected; official launcher/CI credential exposure controls exist. |
| Swagger / readiness / AllowedHosts | **Verified baseline** | Swagger Development-only, minimal readiness, explicit Production host allow-list. |
| Rate limiting / proxy trust | **Implemented / verification pending** | Trusted proxy handling + effective client IP partitions + runtime 429 test added. Final ingress topology remains deployment evidence. |
| Maker-checker | **Verified** | Administrator cannot review own initiative; unit/black-box evidence exists. |
| CSP/cache policy | **Open / design required** | Requires Blazor-compatible design and compatibility/security testing. |
| SQL transport / identity | **Repository production contract implemented** | Encrypted validated SQL transport and non-`sa` runtime enforced by Production validator; actual cert/principal provisioning is external. |
| Controlled migrations / restore | **Verified restore baseline** | Startup migration privilege blocked in Production; real CHECKSUM/VERIFYONLY/restore validation exists. Per-change destructive schema strategy remains change-specific. |
| Audit / structured security logging | **Partially Satisfied** | Product audit and event target catalog exist; append-only central sink/schema enrichment/correlation remain incomplete/external. |
| Central observability | **External Configuration Required** | SIEM/log retention/alert routing/on-call evidence requires deployment. |
| PII lifecycle | **Partially Satisfied** | Inventory/minimization exist; retention/deletion/legal notices remain organizational/legal. |
| Crypto/Data Protection | **Repository capability implemented** | DP persistence + X.509 protection and transport checks exist; Vault/KMS/CA/rotation evidence remains external. |
| Production backup service | **External Configuration Required** | Restore drill verified; encrypted off-site immutable storage/retention remain deployment decisions. |
| Container hardening | **Verified baseline** | Non-root and runtime hardening assertions exist; immutable production digest promotion remains partially open. |
| Quick Tunnel | **Demo-only boundary** | Synthetic demonstrations only; not production ingress and not for sensitive data. |
| Workbench | **Sample-only boundary** | Controlled/local reference; not a public production service. |
| Incident/change runbooks | **Repository process implemented** | Organizational owners, approval authorities, SLA, RPO/RTO remain external decisions. |

## Prior integrated verification evidence

`docs/security/evidence/STEP-05-INTEGRATED-VERIFICATION.md` records the prior successful technical baseline, including:

- CI build/test/publish/pack + Workbench/Athar SQL integration and real restore drill;
- Security Scan / Trivy / negative tests;
- CodeQL C# and JavaScript/TypeScript;
- Windows PowerShell 5.1 launcher checks.

The independent review changed security-relevant source after that evidence. Therefore the affected findings above correctly remain `Implemented / verification pending` until the new head completes all required workflows and a post-review evidence record is added.

## Mandatory policy set

All twelve approved policies remain in scope without renaming:

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

## Decisions deliberately unresolved

No repository change may invent final values for independent reviewer count/authority, MFA scope/factors, password parameters/compromised-password source, ASVS target, RPO/RTO, retention, vulnerability SLA, exception authority/duration, release/residual-risk acceptance authority, production Vault/KMS/CA/SIEM/backup provider, or hosting/network topology.

See `SECURITY-DECISIONS.md` and the canonical policy register.

## Final classification rule

Repository-side technical controls may be called `Verified` only for the scope backed by successful evidence against the affected source head. They must never be rewritten as `ISO Certified` or `Production Approved`. Production approval additionally requires the external platform, organizational decisions, independent approval, and protected-branch evidence recorded in the policy register.
