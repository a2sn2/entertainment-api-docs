# STEP-00 — Governance and evidence foundation

## Change identity

- Program: Global-grade hardening baseline
- Source audit baseline: `b9de00ba29928111637786f921c1c01249ddcada`
- Policies: Segregation of Duties, Secure SDLC, Change Management, Risk Management, plus evidence dependencies for all other policies.

## Implemented

- Added living `POLICY-IMPLEMENTATION-REGISTER.md`.
- Added repository `RISK-REGISTER.md`.
- Added `THREAT-MODEL.md` with trust boundaries and abuse cases.
- Added `SECURITY-DECISIONS.md` to prevent unapproved policy values from being invented.
- Added `CHANGE-AND-RELEASE-EVIDENCE.md`.
- Added `.github/CODEOWNERS` ownership routing.
- Expanded the Pull Request template with policy, risk, test, rollback, evidence, and independent-review fields.

## Findings affected

- `FK-GOV-001`: repository-side ownership and review evidence requirements implemented; independent reviewer enforcement remains an external GitHub setting and organizational decision.
- `FK-GOV-002`: still external configuration required.
- `FK-RISK-001`: repository risk/threat/evidence model implemented; final organizational risk acceptance authority remains external.
- `FK-CHG-001`: repository change/release evidence model implemented; required release authority remains external.

## Verification

Repository-file creation only in this step. Full repository verification is deferred to the integrated hardening PR CI so evidence is generated against the complete change set rather than a partial branch.

## Residual risk

- CODEOWNERS currently routes to the known repository owner and does not itself provide independent review.
- Protected `main`, reviewer count, release authority, and risk acceptance authority cannot be proven/configured by repository files alone.

## Next step

Application-security P0 controls and negative tests.