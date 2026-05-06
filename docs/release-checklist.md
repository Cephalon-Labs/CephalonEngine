# Cephalon release checklist

This checklist consolidates everything a release manager runs through when cutting a Cephalon release tag (`v*.*.*`). It is the operational counterpart to the publish-side contract declared in [`package-publishing.md`](package-publishing.md), the supply-chain plan in [`supply-chain-uplift-plan.md`](supply-chain-uplift-plan.md), and the SRE error-budget posture in [`sre-posture.md`](sre-posture.md). Run it top-to-bottom; each section names the artefact that proves the step succeeded so a release pass is self-evidencing.

For per-release tracking, copy [`release-checklist-template.md`](release-checklist-template.md) into a working artefact (GitHub Release draft, Linear / Jira issue, or `docs/releases/<tag>-checklist.md`) and fill in the tickboxes there; this rule book itself is never edited per release.

Per-release release-notes drafts live alongside the per-release checklist in `docs/releases/<tag>-notes.md`. The notes draft is the consolidated summary of substantive shipping arcs across the sprint window (engine-quality dimensions advanced, ENG range citations, compatibility posture, supply-chain posture, known gaps and limitations) that the release manager hands to the GitHub Release body when the tag is cut. The first such draft lives at [`docs/releases/v0.1.0-preview-notes.md`](releases/v0.1.0-preview-notes.md).

Cross-references: [`package-publishing.md`](package-publishing.md), [`supply-chain-uplift-plan.md`](supply-chain-uplift-plan.md), [`engineering-standards.md`](engineering-standards.md), [`engine-completion-scorecard.md`](engine-completion-scorecard.md), [`compatibility.md`](compatibility.md), [`sre-posture.md`](sre-posture.md), [`dotnet11-readiness.md`](dotnet11-readiness.md), [`deployment-mode-support.md`](deployment-mode-support.md), [`engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md), [`runtime-contract-index.md`](runtime-contract-index.md), [`conformance-matrix.md`](conformance-matrix.md), [`planning-governance.md`](planning-governance.md), [`releases/v0.1.0-preview-notes.md`](releases/v0.1.0-preview-notes.md).

## Pre-flight: working-tree truth

- [ ] Repository is on the intended release branch (typically `master`); no uncommitted changes outside intentional release prep
- [ ] [`global.json`](../global.json) SDK pin (`10.0.201`) matches the SDK installed on the release-build machine; the release pipeline pins this through `actions/setup-dotnet@v4` with `global-json-file: global.json`
- [ ] `Directory.Packages.props` package versions reflect the deliberate dependency snapshot for this release; no in-flight version bumps left over from earlier work
- [ ] [`packages.lock.json`](../) files (one per project) are in sync with `Directory.Packages.props`; run `dotnet restore CephalonEngine.slnx` to refresh, then `dotnet restore --locked-mode CephalonEngine.slnx` to verify (per the `ENG-321` contract)

## Public-API contract review

- [ ] Review every `PublicAPI.Unshipped.txt` entry that is about to graduate into `PublicAPI.Shipped.txt`; intentional additions ship, accidental additions are reverted before tagging
- [ ] Run [`scripts/summarise-public-api-deltas.ps1`](../scripts/summarise-public-api-deltas.ps1) (or read the `public-api-delta-*` artefact emitted by `scripts/validate-release.ps1`) for the full additive / removal delta across all 101 packages; confirm the markdown report matches the release-notes intent
- [ ] Removals are recorded with the `*REMOVED*` prefix in `PublicAPI.Unshipped.txt` and follow the deprecation discipline in [`compatibility.md`](compatibility.md): an `[Obsolete]` major before a removal, never silent
- [ ] When the engine cuts its first stable GA (post-`0.1.0-preview`), enable `<EnablePackageValidation>true</EnablePackageValidation>` with a `<PackageValidationBaselineVersion>` per stable package so binary-breaking changes fail packaging time on the next release

## Validation pipeline

- [ ] Run [`scripts/validate-release.ps1`](../scripts/validate-release.ps1) end-to-end on a clean workspace; it runs in order:
    1. **Restore** in `--locked-mode` (per `ENG-321`)
    2. **Build** the solution with `--no-restore`
    3. **Tests** for `Cephalon.Tests.Composition` / `.Hosting` / `.Tooling` (Pester suite for `tests/Cephalon.Tests.Scripts` runs separately in CI)
    4. **`.NET 11` readiness** through `scripts/validate-dotnet-readiness.ps1`
    5. **Deployment-mode claim** audit through `scripts/validate-deployment-mode-claims.ps1` (release validation still passes `-SkipPublish` while `publishProbePolicy.releaseValidationMode` is `audit-only`; explicit publish-probe runs use the manifest's staged `representativePublishTargets.projects` list)
    6. **Engine completion scorecard artifact** through `scripts/publish-engine-completion-scorecard.ps1`, including evidence-source references and per-package GA readiness rows; optionally run `cephalon doctor --scorecard artifacts/engine-completion-scorecard-release/engine-completion-scorecard.json` to get the local CLI summary over the generated artifact
    7. **Operational health and export conventions**
    8. **Phase-8 architecture, runtime, and starter conventions**
    9. **Benchmark smoke suite** + benchmark guardrail validation
    10. **Reference-doc publishing** through `scripts/publish-reference-docs.ps1`
    11. **Package artefact publishing** through `scripts/publish-package-artifacts.ps1`
    12. **Public-API delta summary** through `scripts/summarise-public-api-deltas.ps1` (per `ENG-352`)
- [ ] Every step finishes with `0 Warning(s), 0 Error(s)`; benchmark guardrails report no regression beyond the configured allowance
- [ ] The release-validation GitHub Actions workflow ([`release-validation.yml`](../.github/workflows/release-validation.yml)) passes on both `windows-latest` and `ubuntu-latest`; the `dotnet11-readiness` job passes against the `.NET 11` SDK preview

## Conformance and maturity truth

- [ ] [`docs/engine-completion-scorecard.md`](engine-completion-scorecard.md) reviewed as the release-readiness roll-up, and the generated `artifacts/engine-completion-scorecard-release/engine-completion-scorecard.json` artifact matches it, validates evidence-source references, and carries per-package GA readiness rows from [`conformance-matrix.md`](conformance-matrix.md); no gate was promoted there without the owning source document changing first; the optional `cephalon doctor --scorecard artifacts/engine-completion-scorecard-release/engine-completion-scorecard.json` summary is treated as artifact readback only
- [ ] [`docs/engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md) Current-audit table reflects shipped state: every shipped `Cephalon.*` package has a row with current maturity, ownership mode, and "next proof needed" populated; recently-promoted packages have their maturity bumped in the audit before the release notes or scorecard mention the promotion
- [ ] [`docs/conformance-matrix.md`](conformance-matrix.md) per-family tables and the *Family summary at a glance* match the audit; no row reports a maturity that disagrees with the audit
- [ ] [`docs/runtime-contract-index.md`](runtime-contract-index.md) `/engine/*` route catalog reflects every shipped route; recently-added routes (e.g. `/engine/diagnostics-conventions` from `ENG-351`) are present
- [ ] [`docs/components/README.md`](components/README.md) lists every shipped component page; new packages added during the release window have a matching `docs/components/<package>.md` page

## Deployment-mode claim truthfulness

- [ ] [`scripts/deployment-mode-support.json`](../scripts/deployment-mode-support.json) `deploymentModes.{trim,nativeAot,singleFile}.status` accurately reflects what the engine claims; `not-claimed` until the claim is actually proven by `scripts/validate-deployment-mode-claims.ps1` returning `claim-truthful` (per the contract in [`deployment-mode-support.md`](deployment-mode-support.md))
- [ ] The harness's publish-probe lane (activated in `ENG-331`) reports the expected verdict for the manifest-declared `representativePublishTargets.projects`; `claim-validation-report.json` includes `PublishProbePolicy` matching `publishProbePolicy`, and the `engine.deployment-mode-claims.truthful-fraction` SLI in [`sre-posture.md`](sre-posture.md) is in the expected state (`audit-only` until trim / AOT / single-file claim flips)
- [ ] [`docs/dotnet11-readiness.md`](dotnet11-readiness.md) reflects current `.NET 11` preview window observation; `.NET 10` LTS remains the shipping floor

## Signed release pipeline (tag-triggered)

The signed release pipeline at [`.github/workflows/publish-release.yml`](../.github/workflows/publish-release.yml) is the authoritative path for publishing `Cephalon.*` packages to nuget.org (per `ENG-324`). It runs on `v*.*.*` tag push.

- [ ] On nuget.org: trusted-publishing policy is in place pointing at `Cephalon-Labs/CephalonEngine`, `.github/workflows/publish-release.yml`, and the `v*.*.*` tag pattern; `Cephalon.*` prefix is reserved when the release approaches GA; `NUGET_USER` repository secret is set
- [ ] **Dry-run first**: trigger the workflow via `workflow_dispatch` with `dry_run: true` to exercise build + sign + attest stages without pushing to nuget.org; verify the artefact bundle (`packages-release`, `sboms-release`, `signatures-release`, `release-manifest`) is uploaded and the per-step output is clean
- [ ] **Tag the release** (`git tag v0.X.Y` then `git push origin v0.X.Y`); the workflow runs end-to-end with the `dry_run` flag absent, producing:
    - SLSA v1.1 build provenance per `.nupkg` via `actions/attest-build-provenance@v2`
    - Sigstore Cosign keyless signature + Rekor transparency log entry per `.nupkg` (signature `.sig` + certificate `.pem` under `artifacts/signatures-release/`)
    - CycloneDX SBOM per `Cephalon.*` project (`artifacts/sboms-release/<package-id>/<package-id>.cdx.json`)
    - Release manifest (`artifacts/release-bundle/release-manifest.json`) with SHA-256 + size for every package, SBOM, and signature artefact
    - NuGet trusted-publishing login + `dotnet nuget push` of `.nupkg` and `.snupkg` to nuget.org
- [ ] Verify on nuget.org: published packages show the expected version, Source Link metadata, and signature; the SBOM artefacts are downloadable from the GitHub Actions run; the SLSA provenance attestation is visible in the GitHub UI

## Documentation and planning truth

- [ ] [`docs/engine-roadmap.md`](engine-roadmap.md) reflects shipped phase / sprint progress; in-flight items have honest status
- [ ] [`docs/engine-backlog.md`](engine-backlog.md) cards for the just-shipped slice are marked `Status: done` with an Issue reference and Estimate; `Sprint history and next 4 sprints` section names the closing sprint
- [ ] [`docs/project-memory.md`](project-memory.md) collaboration agreements still hold; any decisions made during the release window that affect "how we work" are recorded here
- [ ] When a tag is cut, the matching GitHub Release is published with a body that links the public-API delta artefact + the SLSA provenance + the SBOM bundle; release notes name the engine-quality dimensions advanced (Performance / Security / Compatibility / etc. per [`engineering-standards.md`](engineering-standards.md)). When a per-release `docs/releases/<tag>-notes.md` draft exists for the tag (e.g. [`releases/v0.1.0-preview-notes.md`](releases/v0.1.0-preview-notes.md)), the release manager copies that draft into the GitHub Release body with any final timing edits and adds the SLSA provenance / Sigstore signature / SBOM bundle URLs once the pipeline produces them.

## Post-release follow-through

- [ ] Architecture review for the release month is published as `docs/architecture-review-YYYY-MM.md` per the cadence rule in [`planning-governance.md`](planning-governance.md); the review extends rather than replaces the previous month's review
- [ ] If the release introduced a maturity-audit row change (new package, promoted maturity, retired package), [`docs/engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md) is updated in the same slice; the conformance matrix row is updated to match
- [ ] If the release contains a deprecation, the `[Obsolete]` annotation is in place at least one major version before the removal; the obsoletion message names the replacement and a `DiagnosticId` per [`compatibility.md`](compatibility.md)

## SRE follow-through

- [ ] Compare the release pass against the SLI catalogue in [`sre-posture.md`](sre-posture.md); any SLI that burned its budget enters the architecture-review-YYYY-MM follow-through tracker per the error-budget freeze policy
- [ ] If a hot-path latency or allocation SLI burned more than 25% of its monthly budget within a 7-day window, freeze new feature merges on the affected family until the budget recovers
- [ ] Test flake rate (`engine.tests.flake-rate.7d`) is within target; quarantine any newly-introduced flaky test within 24 hours

## Refresh cadence for this checklist

Refresh this document in place when:

- a new validation step lands in `scripts/validate-release.ps1` (extend the validation pipeline section)
- a new release-pipeline artefact lands in `.github/workflows/publish-release.yml` (extend the signed release pipeline section)
- a new shipped `Cephalon.*` family adds a documentation surface that release managers need to verify (extend the conformance / docs sections)
- the EU regulatory framework or SLSA / Sigstore / SBOM specs publish a revision that materially affects the post-release evidence requirements

Do not append a dated change log inside this document. Long-running operational checklists rarely benefit from change history living inside the checklist; the durable history belongs in commits, planning cards, and architecture-review snapshots.
