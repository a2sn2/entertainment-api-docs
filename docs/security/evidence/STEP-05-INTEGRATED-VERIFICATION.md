# STEP-05 — Integrated repository verification

## Evidence identity

- Change: `FK-HARDEN-2026-08-07` / PR #34.
- Verified source commit: `2e8f78eb98ce02a5fb4f8b02db720391f9c77f98`.
- Verification date: 2026-08-07.
- Result: **Repository-side technical controls Verified for the automated scope below.**
- This evidence is **not** production approval, an ISO/IEC 27001 certification, or residual-risk acceptance.

## Successful workflow evidence

### FoundationKit CI — run `31184047139`

Both jobs completed successfully.

`Build, security checks, test, examples, portal, and pack` verified:

- tracked-source secret scan;
- repository boundary checks;
- JSON, GitHub Pages, and JavaScript validation;
- container-hardening policy validation;
- PowerShell parser validation;
- NuGet restore/vulnerability audit;
- CycloneDX SBOM generation;
- Release solution build;
- generated capability-document verification;
- full solution tests;
- Workbench and Athar publish;
- reusable package creation;
- SHA-256 package/publish integrity evidence;
- test/security/package artifact publication.

`Workbench and Athar SQL Server integration` verified:

- Workbench container build/start/readiness;
- Workbench portal, MudBlazor, Swagger, and SQL workflow smoke test;
- Athar container build/start/readiness;
- Athar non-root runtime assertion;
- Arabic client, MudBlazor, API surface, and minimal readiness behavior;
- Athar end-to-end account/initiative workflow;
- isolated Athar backup/restore drill using `BACKUP ... CHECKSUM`, `RESTORE VERIFYONLY`, real restore to `AtharRestoreDrill`, schema-qualified core-table queries, and cleanup;
- teardown of integration stacks and volumes.

### FoundationKit Security Scan — run `31184044933`

Both jobs completed successfully.

`Athar black-box negative security tests` verified the automated abuse suite and destroyed the isolated stack after execution.

`Trivy repository and Athar container` verified:

- repository vulnerability/secret/misconfiguration HIGH/CRITICAL gate;
- Athar image build;
- blocking HIGH/CRITICAL image gate for findings with upstream/vendor fixes;
- complete HIGH/CRITICAL SARIF generation including unfixed findings;
- SARIF upload to GitHub code scanning.

Unfixed upstream/base-image findings are not treated as absent. They remain tracked as `R-FK-016`; the repository blocks fixable HIGH/CRITICAL findings while retaining complete evidence for currently unfixed findings.

### FoundationKit CodeQL — run `31184045528`

- `CodeQL csharp`: success.
- `CodeQL javascript-typescript`: success.

### FoundationKit Windows Launcher Check — run `31184044965`

Windows PowerShell 5.1 validation completed successfully, including:

- ASCII-only launcher validation;
- PowerShell 5.1 parsing;
- unified help smoke test;
- unified doctor smoke test;
- Athar Native status smoke test.

## Credential and cleanup evidence

- Generated CI SQL/admin passwords are masked before export to the job environment.
- No generated test password is recorded in this evidence.
- Isolated security and integration stacks are destroyed after execution.
- The restore-drill database and synthetic backup file are removed by the drill cleanup path.

## Findings moved to Verified within repository scope

The evidence above supports repository verification of the implemented automated scope for:

- secure SDLC gates and source/build/test/package evidence;
- current negative application-security suite;
- maker-checker self-review prevention;
- current rate-limit/authentication-abuse paths covered by the suite;
- container non-root/runtime-hardening assertions covered by CI;
- isolated backup integrity and real restore capability;
- PowerShell 5.1 launcher compatibility;
- CodeQL C#/JavaScript analysis;
- fixable HIGH/CRITICAL repository/container vulnerability gates;
- dependency/SBOM/integrity evidence generation.

## External and organizational gates still required before Production Approved

The following cannot be satisfied honestly by repository code or CI alone and remain outside this verification:

- independent reviewer approval and protected-`main`/required-check evidence;
- named release and residual-risk acceptance authorities;
- production domain, trusted TLS certificate, ingress/network architecture, and firewall controls;
- production SMTP/recovery delivery configuration;
- external Vault/KMS/CA and Data Protection certificate lifecycle;
- central SIEM/log sink, retention, alert routing, and tamper-resistant audit evidence;
- least-privilege non-`sa` production runtime and migration SQL principals provisioned by the platform/DBA;
- encrypted, off-site, immutable production backup storage and approved retention;
- organizational RPO/RTO, retention, MFA scope, password/blocklist standard, ASVS target, vulnerability SLA, and exception/risk-acceptance rules;
- production penetration/load acceptance appropriate to the final deployment topology;
- final privacy/legal notices and retention/deletion decisions.

## Final classification

**Repository technical baseline: Verified for the stated automated scope.**

**Production approval: Not granted by this evidence.**

A subsequent security-relevant commit invalidates the source-commit identity above until the required checks pass again and new evidence is recorded.
