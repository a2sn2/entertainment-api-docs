## Change identity

- Change/Issue ID:
- Owner:
- Business / engineering purpose:

## Summary

Describe exactly what changed and why. Identify whether the change affects reusable core, Workbench, Athar, catalog, Pages/demo, CI/CD, deployment, or documentation.

## Policy and risk impact

Select every affected policy. Do not change these names or numbers.

- [ ] 1. Segregation of Duties Policy
- [ ] 2. Data Transfer Policy
- [ ] 3. Password Management Policy
- [ ] 4. Logging and Monitoring Policy
- [ ] 5. Data Backup Policy
- [ ] 6. Personally Identifiable Information Protection Policy
- [ ] 7. Secure Software Development Life Cycle Policy
- [ ] 8. Malware Protection Policy
- [ ] 9. Cryptography and Key Management Policy
- [ ] 10. Application Security Policy
- [ ] 11. Change Management Policy
- [ ] 12. Risk Management Policy

Describe:

- Threat/risk scenario:
- Risk reduced or introduced:
- Affected assets/data/users/integrations:
- Related finding/risk IDs:
- Residual risk after this change:
- Organizational decision still required (if any):

## Security and privacy impact

State `None` only after reviewing each item.

- Authentication / MFA / recovery:
- Authorization / maker-checker / object ownership:
- PII / logs / audit:
- Cryptography / keys / secrets / certificates:
- Dependencies / packages / containers / Actions:
- Database / migrations / backup / restore:
- Network / public exposure / data transfer:
- CI/CD / artifacts / provenance:

## Compatibility and data impact

Describe public API, behavior, package, schema, migration, data-integrity, or repository-layout impact.

## Verification plan and evidence

Positive tests:

- 

Negative/security tests:

- 

Required repository checks:

- [ ] `bash scripts/verify-repository.sh`
- [ ] JSON / Pages / JavaScript validation when affected
- [ ] `dotnet restore FoundationKit.sln`
- [ ] `dotnet build FoundationKit.sln --configuration Release --no-restore`
- [ ] `dotnet test FoundationKit.sln --configuration Release --no-build`
- [ ] `dotnet run --project tools/FoundationKit.CatalogGenerator --configuration Release --no-build -- --check`
- [ ] `bash scripts/pack.sh Release artifacts/packages`
- [ ] Workbench SQL Server smoke when affected
- [ ] Athar SQL Server / E2E smoke when affected
- [ ] PowerShell 5.1 parser/smoke when launchers are affected
- [ ] Dependency vulnerability audit when packages change
- [ ] Secret/security scan when source/workflows/configuration change
- [ ] Container scan / runtime hardening verification when images/deploy change
- [ ] SBOM/dependency inventory updated for releasable artifacts

Evidence links / workflow runs / artifact digests:

- 

## Migration, rollback, and recovery

- Migration required: Yes / No
- Pre-change backup required: Yes / No
- Rollback / rollforward / compensating action:
- Data-integrity validation:
- Restore/recovery impact:

## Documentation and living reference

- [ ] Relevant README/docs updated
- [ ] `docs/security/POLICY-IMPLEMENTATION-REGISTER.md` updated
- [ ] Risk/threat model updated when the trust boundary or risk changed
- [ ] `CHANGELOG.md` updated when behavior changed

## Independent review and closeout

- Independent reviewer:
- Review evidence:
- Open findings:
- Risk acceptance evidence (only when formally approved):
- Final state: Built / Implemented / Verified / Production Approved / Rejected

> A green build is not production approval. Do not mark a policy satisfied without reproducible evidence.