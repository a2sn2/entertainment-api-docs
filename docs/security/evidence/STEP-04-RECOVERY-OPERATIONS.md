# STEP-04 — Recovery, containers, logging, and incident operations

## Policies

- Data Backup Policy.
- Logging and Monitoring Policy.
- Malware Protection Policy.
- Cryptography and Key Management Policy.
- Change Management Policy.
- Risk Management Policy.

## Implemented

- SQL backup commands continue using `CHECKSUM` and now label local backups development-only recovery conveniences.
- Windows local backup copies receive restricted ACLs.
- Added `scripts/verify-athar-restore.sh`, gated by `ATHAR_ALLOW_RESTORE_DRILL=true`, which:
  - creates a synthetic CI backup with `COPY_ONLY, CHECKSUM`;
  - runs `RESTORE VERIFYONLY WITH CHECKSUM`;
  - determines logical SQL files;
  - restores to isolated `AtharRestoreDrill` database;
  - queries Identity, initiatives, reviews, and audit tables;
  - drops isolated recovery resources afterward.
- CI integration path invokes the restore drill after Athar E2E.
- Athar runtime image declares non-root `USER app`.
- Development Compose adds SQL/app health checks, `no-new-privileges`, and drops all Linux capabilities from the app container.
- Production contract adds read-only root filesystem and tmpfs expectation.
- Added structured security event/alert catalog and rules prohibiting secret/token/free-text leakage.
- Added incident/rollback/recovery runbook.

## Findings affected

- `FK-BACK-001`: isolated restore-drill implementation added; becomes Verified only when CI evidence passes.
- `FK-BACK-002`: local ACL restriction improved; production encryption/off-site/immutability/retention remain external decisions.
- `FK-DOCK-001`: non-root/capability/no-new-privileges controls implemented; integrated runtime verification pending.
- `FK-DOCK-002`: immutable base-image digests remain pending approved update process.
- `FK-LOG-001`: event catalog/runbook implemented; central sink/alerts/retention remain external configuration required.
- `FK-AUD-001`: external append-only/tamper-resistant sink remains open.
- `FK-AUD-002`: structured event target documented; database audit schema enrichment remains open.
- `FK-OPS-001`: repository incident/rollback/recovery runbook implemented; organizational responders/authorities remain external.

## Organizational decisions intentionally left open

RPO, RTO, backup/log retention, off-site/immutable technology, SIEM provider, alert thresholds, incident severity/authority, and legal notification rules.