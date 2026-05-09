# Cephalon release checklist — per-release template

Copy this file to a release-tracking artefact for each cut release (e.g. paste into a GitHub Release draft, a Linear / Jira issue, or a per-release doc such as `docs/releases/v0.1.0-preview-checklist.md`). Fill in the metadata header, tick the boxes as steps complete, and link the matching artefact URLs (CI run, PR, GitHub Release page) inline so the record stands on its own.

The durable rule book lives in [`release-checklist.md`](release-checklist.md). This file is the per-release working copy — the rule book never gets a tickbox, but every release does.

---

## Release metadata

| Field | Value |
| --- | --- |
| Release tag | `v?.?.?` |
| Release type | preview / rc / stable |
| Release manager | `@?` |
| Release date | YYYY-MM-DD |
| Release notes URL | _to be filled when GitHub Release publishes_ |
| Previous shipped tag | `v?.?.?` (or "first release") |
| Architecture review month | `architecture-review-YYYY-MM.md` |
| `global.json` SDK pin at release | `10.0.201` (current) |
| Shipping target framework | `net10.0` (current) |

---

## 1. Pre-flight: working-tree truth

- [ ] Repository on `master` (or intended release branch); no uncommitted changes
- [ ] [`global.json`](../global.json) SDK pin matches release-build machine SDK
- [ ] `Directory.Packages.props` reflects deliberate dependency snapshot for this release
- [ ] `packages.lock.json` files in sync (`dotnet restore` then `dotnet restore --locked-mode` both pass)

## 2. Public-API contract review

- [ ] Reviewed every `PublicAPI.Unshipped.txt` entry that will graduate into `PublicAPI.Shipped.txt`
- [ ] Ran [`scripts/summarise-public-api-deltas.ps1`](../scripts/summarise-public-api-deltas.ps1) (or read the `public-api-delta-*` workflow artefact); markdown and JSON reports match release-notes intent
- [ ] Removals follow `[Obsolete]` deprecation discipline from [`compatibility.md`](compatibility.md) and are resolved before the default `scripts/validate-release.ps1` run because it passes `-FailOnRemovals`
- [ ] (At GA only) `<EnablePackageValidation>true</EnablePackageValidation>` + `<PackageValidationBaselineVersion>` are configured per stable package

**Public-API delta summary:** _link to `public-api-delta-*` Markdown + JSON artefacts or paste counts here (packages with changes / total additions / total removals)_

## 3. Validation pipeline

- [ ] [`scripts/validate-release.ps1`](../scripts/validate-release.ps1) end-to-end on a clean workspace
- [ ] Restore (locked-mode) ✅
- [ ] Build (Release) ✅
- [ ] Tests (Composition + Hosting + Tooling) ✅
- [ ] `.NET 11` readiness contract ✅
- [ ] Deployment-mode claim audit + gated `singleFile` publish probe ✅
- [ ] NuGet vulnerability audit (`artifacts/nuget-vulnerability-audit-release/nuget-vulnerability-audit.json`) ✅
- [ ] Engine completion scorecard artifact + source references + per-package GA rows + deployment-mode evidence including generated claims-report gate/target/warning/error/package-claim readback + adoption-smoke evidence + provider-integration evidence including dependency-health provider-manifest release-validation/doctor readback + SRE posture evidence including guardrail coverage counts, stable-baseline manifest readback, and pending-baseline blocker rows + supply-chain release evidence including package metadata validation, vulnerability audit, and external-policy preflight count/status + public API compatibility evidence + optional `cephalon doctor --scorecard artifacts/engine-completion-scorecard-release/engine-completion-scorecard.json` readback with deployment-mode claims-report, provider integration, dependency-health provider-manifest, SRE, supply-chain preflight, and public API counts ✅
- [ ] Operational health + export conventions ✅
- [ ] Phase-8 architecture / runtime / starter conventions ✅
- [ ] Benchmark smoke + guardrails ✅
- [ ] Reference-doc publishing ✅
- [ ] Package artefact publishing + `package-metadata-validation.json` ✅
- [ ] Public-API delta summary with removal gate ✅
- [ ] GitHub Actions [`release-validation.yml`](../.github/workflows/release-validation.yml) green on `windows-latest` + `ubuntu-latest` + `dotnet11-readiness`
- [ ] GitHub Actions [`provider-live-testcontainers.yml`](../.github/workflows/provider-live-testcontainers.yml) green for Cassandra / ClickHouse / Elasticsearch / NATS / Neo4j / OpenSearch / Qdrant, or release-manager exception recorded

**Workflow run URL:** _link to the green CI run_

**Provider live workflow run URL:** _link to the green provider Testcontainers run or exception note_

## 4. Conformance and maturity truth

- [ ] [`docs/engine-completion-scorecard.md`](engine-completion-scorecard.md) reviewed; generated scorecard artifact, source references, per-package GA rows, deployment-mode evidence plus generated claims-report gate/target/warning/error/package-claim readback, adoption-smoke evidence, provider-integration evidence with dependency-health provider-manifest release-validation/doctor readback, SRE posture evidence including benchmark guardrail coverage mappings plus stable-baseline manifest rows/measurements/pending ids and pending-baseline blocker rows, supply-chain release evidence including package metadata validation, vulnerability audit, and external-policy preflight count/status, public API compatibility evidence, release-validation and optional `cephalon doctor --scorecard` summaries with deployment-mode claims-report, provider integration, dependency-health provider-manifest, SRE, supply-chain preflight, and public API counts, and gate posture match the owning source docs
- [ ] [`docs/engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md) Current-audit table reflects shipped state
- [ ] [`docs/conformance-matrix.md`](conformance-matrix.md) per-family tables + Family summary at a glance match the audit
- [ ] [`docs/runtime-contract-index.md`](runtime-contract-index.md) `/engine/*` route catalog reflects every shipped route
- [ ] [`docs/components/README.md`](components/README.md) lists every shipped component page

## 5. Deployment-mode claim truthfulness

- [ ] [`scripts/deployment-mode-support.json`](../scripts/deployment-mode-support.json) claim states accurately reflect engine reality
- [ ] Harness publish-probe reports expected verdict and `PublishProbeGate` for the manifest-declared `representativePublishTargets.projects`, and generated scorecard `DeploymentModeEvidence.ClaimsReport*` readback matches that report
- [ ] `engine.deployment-mode-claims.truthful-fraction` SLI in expected state
- [ ] [`docs/dotnet11-readiness.md`](dotnet11-readiness.md) reflects current `.NET 11` preview window observation

## 6. Signed release pipeline (tag-triggered)

- [ ] nuget.org trusted-publishing policy in place (repo + workflow + tag pattern)
- [ ] (At GA only) `Cephalon.*` prefix reserved on nuget.org
- [ ] `NUGET_USER` repository secret configured
- [ ] `CEPHALON_NUGET_TRUSTED_PUBLISHING_POLICY_CONFIRMED=true` and `CEPHALON_NUGET_PREFIX_RESERVATION_CONFIRMED=true` protected repository variables configured after the release manager confirms nuget.org policy state
- [ ] **Dry-run pass** via [`publish-release.yml`](../.github/workflows/publish-release.yml) `workflow_dispatch` with `dry_run: true`
- [ ] **Tag pushed** (`git tag v?.?.?` + `git push origin v?.?.?`); workflow runs end-to-end
- [ ] External-policy preflight report (`artifacts/supply-chain-external-policy/external-policy-preflight.json`) reports `passed` before NuGet login ✅
- [ ] SLSA v1.1 build provenance attestation generated per `.nupkg` ✅
- [ ] Sigstore Cosign keyless signature + Rekor entry per `.nupkg` ✅
- [ ] CycloneDX SBOM per `Cephalon.*` project ✅
- [ ] NuGet vulnerability audit included in the release bundle ✅
- [ ] Release manifest (`release-manifest.json`) emitted ✅
- [ ] NuGet trusted-publishing login + push to nuget.org succeeded ✅
- [ ] Verified on nuget.org: published version + Source Link + signature visible

**Tag URL:** _link to the GitHub tag page_
**Workflow run URL:** _link to the publish-release run_

## 7. Documentation and planning truth

- [ ] [`docs/engine-roadmap.md`](engine-roadmap.md) reflects shipped phase / sprint progress
- [ ] [`docs/engine-backlog.md`](engine-backlog.md) cards for the just-shipped slice marked `Status: done` with Issue ref + Estimate; `Sprint history and next 4 sprints` updated
- [ ] [`docs/project-memory.md`](project-memory.md) collaboration agreements and standing decisions still hold
- [ ] GitHub Release published with body linking PublicAPI delta + SLSA provenance + SBOM bundle; engine-quality dimensions named per [`engineering-standards.md`](engineering-standards.md)

**GitHub Release URL:** _link to the published release_

## 8. Post-release follow-through

- [ ] Architecture review for the release month exists as `docs/architecture-review-YYYY-MM.md`
- [ ] Maturity-audit row changes (new package, promoted maturity, retired package) recorded in [`engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md)
- [ ] Deprecations carry `[Obsolete]` annotation at least one major before removal, with replacement + `DiagnosticId`

## 9. SRE follow-through

- [ ] Compared release pass against SLI catalogue in [`sre-posture.md`](sre-posture.md)
- [ ] Confirmed SRE guardrail coverage counts, stable-baseline row/measurement counts, pending baseline ids, pending-baseline blocker rows, and guardrail references in generated `SrePostureEvidence`
- [ ] No SLI burned more than 25% of monthly budget in 7-day window (or freeze in place on affected family)
- [ ] Test flake rate within target from `artifacts/sre-ci-flake-rate/ci-flake-rate.json`; new flaky tests quarantined within 24 hours

**SLI status snapshot:** _paste relevant SLI readings or link to dashboard_

---

## Notes / blockers / decisions

_Free-form section: capture any release-day decisions, deviations from the rule book, or carry-over items for the next release._

---

## Sign-off

- [ ] Release manager confirmed all sections passed: `@?` on YYYY-MM-DD
- [ ] Release announcement posted (channel: ___)
- [ ] Next release window scheduled (target: ___)
