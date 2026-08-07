# FoundationKit — Change and Release Evidence Model

## Definition of Ready

A material change is ready for implementation only when the PR/issue records:

- Change/Issue identifier.
- Business/engineering purpose.
- Affected components, users, data, integrations, and environments.
- Affected policies from the mandatory twelve-policy set.
- Threat/risk scenario and expected residual risk.
- Security/privacy/database/CI/CD impact.
- Positive and negative test plan.
- Migration/data-transfer impact.
- Rollback, rollforward, or compensating-action concept.
- Required independent reviewer/authority when known.

## Pull request evidence

Every material PR must state:

1. What changed and why.
2. Which policies/findings are affected.
3. New/reduced risks.
4. Authentication/authorization/PII/logging/crypto/dependency/database/CI/CD impact.
5. Positive, negative, integration, and security tests.
6. Migration and rollback/rollforward plan.
7. New dependency/source/license/SBOM impact.
8. Secret/PII handling.
9. Evidence links (workflow run, logs, artifacts, screenshots/exports when external).
10. Independent review result.
11. Remaining organizational decisions.

## Definition of Done

A code change can be called **implemented** after code and automated tests pass.

It can be called **verified** only after evidence is captured and mapped to the finding.

It can be called **production-approved** only after all applicable external gates and organizational approvals are complete.

Required closeout fields:

- Source commit.
- PR number.
- Independent reviewer evidence.
- Required CI results.
- Security gate results.
- Migration/data-integrity evidence.
- Artifact digest.
- Deployment/rollback plan if applicable.
- Post-deployment validation if applicable.
- Monitoring/alert impact.
- Open findings and residual risk disposition.
- Updated `POLICY-IMPLEMENTATION-REGISTER.md`.

## Release Security Passport template

```text
Release/Build ID:
Source commit/tag:
Change IDs:
Artifact names and SHA-256 digests:
SBOM reference:
Dependency/SCA result:
Secret scan result:
SAST result:
Container/IaC result:
Unit tests:
Integration tests:
E2E tests:
Negative security tests:
Coverage result:
DAST result (when applicable):
Migration plan/evidence:
Backup/restore evidence:
Open findings:
Risk acceptance evidence:
Independent approvals:
Deployment evidence:
Post-deployment validation:
Observability/alerts evidence:
Decision: Built | Verified | Production Approved | Rejected
```

## Emergency changes

Emergency process must not be used to bypass evidence permanently. At minimum record:

- incident/change reason;
- approving authority;
- exact source diff;
- tests run before/after;
- rollback/compensation;
- temporary exceptions;
- post-implementation review;
- retrospective risk and policy update.

The exact emergency authority and time limits remain organizational decisions.