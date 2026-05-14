# Package Publishing

This document describes the repo-native flow for producing the NuGet and template artifacts Cephalon intentionally ships.

## Scope

The release package-artifact baseline currently includes:

- shipped `src/Cephalon.*` packages
- `templates/Cephalon.TemplatePack`
- `samples/Cephalon.ReferenceModule.Operations`

The baseline intentionally excludes:

- benchmarks and playground hosts
- sample application hosts
- sample-only shared libraries such as the `MicroserviceSuite` governance and shared-foundation projects

`Cephalon.Cli` is now included as a dedicated `.NET tool` package, not as a generic library-style nupkg.

## Shared package metadata

Packable Cephalon packages now inherit shared NuGet metadata from `Directory.Build.props`:

- `Authors`
- `PackageLicenseExpression`
- `PackageProjectUrl`
- `RepositoryUrl`
- `RepositoryType`
- `PublishRepositoryUrl`
- a shared `PACKAGE.md` readme when a project does not provide a package-specific readme already

`Cephalon.TemplatePack` keeps its own package-specific `PACKAGE.md`.
`Cephalon.Cli` now also keeps its own package-specific `PACKAGE.md` so the tool install surface can document the stable `cephalon` command directly.

## Publish flow

Publish the intended release package set:

```powershell
pwsh ./scripts/publish-package-artifacts.ps1
```

Skip the build when the repository has already been compiled:

```powershell
pwsh ./scripts/publish-package-artifacts.ps1 -SkipBuild
```

Choose a custom output directory:

```powershell
pwsh ./scripts/publish-package-artifacts.ps1 -OutputPath artifacts/packages-preview
```

Seed the local package feed emitted by a generated app:

```powershell
$generatedRoot = (Resolve-Path ./Acme.Store).Path
pwsh ./scripts/publish-package-artifacts.ps1 -OutputPath (Join-Path $generatedRoot '.cephalon/packages')
```

That path aligns with the `NuGet.config` emitted by `cephalon new` and the app-focused `dotnet new` starters, so `dotnet build` and `docker compose up --build` can restore Cephalon packages without editing the generated host first.

The publish script validates the produced artifacts before it reports success. The validation reads the actual `.nupkg` and `.snupkg` files, not only project metadata, and checks:

- `Cephalon.*` package id prefix, version, authorship, license, project URL, repository URL/type, and source commit metadata
- NuGet search tags, including the `cephalon` family tag
- declared package readme plus the matching file inside the package
- package type for the CLI tool and template pack
- `.snupkg` pairing for runtime/tool packages with managed `lib/` or `tools/` output

Run the validator directly against an existing artifact directory:

```powershell
pwsh ./scripts/validate-package-metadata.ps1 -PackageArtifactsPath artifacts/packages-release
```

Install the packaged CLI tool from a locally published artifact:

```powershell
dotnet tool install --tool-path .\.tools\cephalon Cephalon.Cli `
  --add-source .\artifacts\packages-release `
  --ignore-failed-sources `
  --no-cache `
  --prerelease

.\.tools\cephalon\cephalon --help
```

## External module package staging

Published module `.nupkg` artifacts are distribution packages, not the final directory shape that `Engine:Discovery:PackageDirectories` loads directly.

Stage a published module package into a loadable package directory with the CLI:

```powershell
cephalon package stage `
  --package ./artifacts/packages-release/Cephalon.ReferenceModule.Operations.1.0.0.nupkg `
  --output ./plugins/reference-operations
```

If you are working inside this repository before installing the tool package, run the same flow with:

```powershell
dotnet run --project ./src/Cephalon.Cli -- package stage `
  --package ./artifacts/packages-release/Cephalon.ReferenceModule.Operations.1.0.0.nupkg `
  --output ./plugins/reference-operations
```

That command stages the selected `lib/<tfm>` surface plus `cephalon.package.json` into a directory the engine can load through `Engine:Discovery:PackageDirectories` or `Engine:Discovery:Packages:ManifestPath`.

Module packages that expect `Engine:Discovery:Packages`, `Engine:Discovery:PackageDirectories`, or
`Engine:Discovery:Assemblies` to find their modules must build with `Cephalon.Engine.SourceGen`
as an analyzer. The staged runtime assembly carries generated `ModuleDiscoveryDescriptor`
metadata in a module initializer, so the engine can load modules without scanning assembly types
or using reflective constructors. Packages that need custom factories should register modules
explicitly through module/package registration code instead of relying on assembly discovery.
Behavior-backed REST module packages that use `MapProfile<TBehavior>()` must also build with
`Cephalon.Behaviors.SourceGen` as an analyzer so REST profile, behavior-type, input-contract,
and output-contract descriptors are generated before the package is staged.

Generated module starters also include `Cephalon.Analyzers` as a private analyzer package. Keep it
when publishing consumer modules unless the package deliberately owns a different analyzer policy;
it brings the curated Cephalon banned-symbol and quality baseline into the build without becoming
part of the runtime package dependency graph.

For the full author -> publish -> trust -> load -> inspect walkthrough, see [External package lifecycle](external-package-lifecycle.md).

For the scenario-driven external replay of that same staged-package path, use `pwsh ./scripts/validate-out-of-tree-package-adoption.ps1`. It publishes the generated app package closure, including `Cephalon.Analyzers`, `Cephalon.Behaviors.SourceGen`, `Cephalon.Diagnostics`, and `Cephalon.Resilience`, scaffolds a fresh app outside the repository, stages `Cephalon.ReferenceModule.Operations` through `cephalon package stage`, patches `Engine:Discovery:PackageDirectories` plus `Engine:PackagePolicy` and `Engine:Trust`, reruns `cephalon doctor --app-root`, validates `/engine/packages`, `/engine/package-policy`, `/engine/trust-policy`, `/engine/snapshot`, and `/api/operations/status`, and writes `artifacts/adoption-smoke/out-of-tree-package-adoption.json` unless `-ReportPath` overrides it. The release-readiness scorecard validates this replay and execution-report contract through `scripts/adoption-smoke-support.json` before emitting `AdoptionSmokeEvidence`.

For the matching higher-assurance detached-signature replay, use `pwsh ./scripts/validate-signed-package-governance.ps1`. It repacks `Cephalon.ReferenceModule.Operations` with a deterministic detached signature, stages the signed `.nupkg`, patches stricter `Engine:PackagePolicy` plus `Engine:Trust:TrustedSignaturePublicKeys`, validates the same runtime/package surfaces, and then proves a tampered signed package is denied when signature verification is required.

For the matching certificate-chain trust replay, use `pwsh ./scripts/validate-signed-package-certificate-chain-governance.ps1`. It keeps the same signed-package path, but patches `Engine:Trust:TrustedSignatureCertificates` plus `Engine:Trust:TrustedSignatureCertificateAuthorities` and proves the runtime/package surfaces expose `trusted-certificate-chain` verification plus the signing `certificateThumbprint`.

## Output

The publish script writes package artifacts to `artifacts/packages-release/` by default:

- `.nupkg` package files for the intended release-pack surface, including the `Cephalon.Cli` tool package
- `.snupkg` symbol packages for runtime/tool packages with managed `lib/` or `tools/` output
- `package-artifacts-manifest.json` with the packed project list, package kinds, package file sizes, SHA-256 checksums, and top-level source repository/revision provenance hints
- `package-artifacts.sha256` with one checksum line per produced package artifact for consumers that want file verification without parsing JSON
- `package-metadata-validation.json` with per-package readme/tag/license/repository/source/package-type/symbol-pairing verdicts

The JSON manifest now carries:

- `SourceRepository` and `SourceRevision` for the release source that produced the package set
- `PackageKind` per packed project such as `library`, `dotnet-tool`, `template-pack`, or `reference-module`
- `PackageFiles` entries with `Path`, `FileName`, `SizeBytes`, and `Sha256`

The metadata-validation JSON uses schema `1.0.0` and reports `PackageCount`, `FailedPackageCount`, `SymbolRequiredPackageCount`, `SymbolPackageCount`, and per-package check verdicts. The validator fails closed when the package family loses required metadata or a runtime/tool package omits its `.snupkg` pair. Template/analyzer packages that intentionally ship no runtime/tool output are validated for metadata and package type/readme posture without requiring a symbol package.

## Release validation

`pwsh ./scripts/validate-release.ps1` now includes package-artifact publishing by default alongside:

- solution build
- test execution
- framework-readiness audit output
- NuGet vulnerability audit output through `scripts/validate-nuget-vulnerability-audit.ps1`
- operational convention validation
- benchmark smoke coverage and guardrails
- reference-doc publishing

The GitHub Actions release-validation workflow now proves this package-publishing path on both Windows and Ubuntu through the same repo-native script entry point. The current Ubuntu leg uses `-SkipBenchmarks` while benchmark guardrails remain Windows-baselined.

The same workflow also carries a dedicated `.NET 11` readiness lane through `scripts/validate-dotnet-readiness.ps1`. That lane exists to assess future-SDK compatibility without changing the stable `net10.0` shipping floor. When package publication quality or support claims need to be assessed under a higher SDK explicitly, use the readiness script directly so the output report, `scripts/deployment-mode-support.json`, [deployment-mode support](deployment-mode-support.md), and the package-publishing docs stay truthful about what was actually validated.

Package-scoped deployment-mode support claims are allowed to move before the global engine support rows, but only through the same manifest-backed path. Published packages such as `Cephalon.Diagnostics`, `Cephalon.Abstractions`, and `Cephalon.Scaffolding` can claim a scoped mode only when `scripts/deployment-mode-support.json` lists the package under `deploymentModeEligibility.packages`, records the scoped `supportedModes`, names the required project properties, and the validation harness reports the package claim separately from the global trim / Native AOT / single-file posture. The same `requiredProjectProperties` field also records permanent package-level `not-claimed` postures for high-tier packages such as `Cephalon.ReferenceDocs` and `Cephalon.Data.MySql.SciSharpReplication`; the manifest Pester suite verifies those entries against the owning project files so publishing metadata cannot drift from the actual package posture. The same harness also emits `PublishProbePolicy`, `PublishProbeGate`, and `hazard-inventory.json`, so release managers can see that the representative `singleFile` publish probe is now release-blocking, trim / Native AOT remain audit-only, and tier counts, scoped-claim counts, known transitive hazard hints, dynamic route boundary annotation audit status, core/full-common/full-operator request-delegate proof, operator response JSON contract proof, non-operator endpoint proof, framework endpoint boundary proof, and the lock-file-backed transitive-hazard audit status stay visible before any package-support claim is promoted; a failed dynamic route boundary annotation audit, route-delegate audit, response-contract audit, non-operator endpoint audit, or framework endpoint boundary audit fails the harness instead of becoming advisory-only output.

## Signed release pipeline (tag-triggered)

`pwsh ./scripts/publish-package-artifacts.ps1` and the release-validation workflow produce the package set, but they do not publish to nuget.org. A separate tag-triggered GitHub Actions workflow at [`.github/workflows/publish-release.yml`](../.github/workflows/publish-release.yml) wraps the validated package set with the supply-chain hardening Cephalon commits to under [`docs/supply-chain-uplift-plan.md`](supply-chain-uplift-plan.md).

The signed release pipeline runs on a `v*.*.*` tag push (and supports `workflow_dispatch` for dry-runs). Before a release manager claims this lane, run the repo-owned probe:

```powershell
pwsh ./scripts/invoke-signed-release-dry-run.ps1 -RequireRunCreated
```

The probe writes `artifacts/signed-release-dry-run/signed-release-dry-run-readiness.json` plus the paste-ready release-manager handoff at `artifacts/signed-release-dry-run/signed-release-dry-run-handoff.md`. A passing claim requires `Status = submitted`, `RunCreated = true`, and a workflow `RunUrl`; if GitHub returns `Actions has been disabled for this user`, the report and handoff record `BlockerClass = dispatch-identity-actions-disabled`, `DispatchActor`, `DispatchIdentityStatus`, `DispatchCommand`, `DispatchAttempted`, and `RequiredReleaseManagerAction` so the signed-release proof remains partial until an Actions-enabled release-manager identity reruns the probe. Script-level Pester coverage reads `scripts/supply-chain-release-support.json` `signedReleaseDryRun.requiredReportFields` and validates those required fields against both the generated report object, persisted JSON, and handoff Markdown before the dry-run report contract can drift. Tooling documentation coverage derives the same required field list and `handoffOutputPath` from that manifest and requires this guide to name every field release managers need.

The pipeline performs, in order:

1. checkout, .NET SDK setup, Pester install
2. on a real tag push only, fail-closed external-policy preflight through `scripts/validate-supply-chain-external-policy-preflight.ps1 -RequireAll`
3. full release validation through `scripts/validate-release.ps1` (locked-mode restore, vulnerability audit, build, tests, readiness, deployment-mode audit, benchmarks, reference docs, package artefacts)
4. CycloneDX SBOM generation per `Cephalon.*` project, written to `artifacts/sboms-release/<package-id>/<package-id>.cdx.json`
5. Sigstore Cosign keyless signing of every `.nupkg`, with the resulting `.sig` and `.pem` written under `artifacts/signatures-release/`; the OIDC identity is the GitHub Actions workflow run, the transparency log is Rekor
6. SLSA v1.1 build provenance attestation through `actions/attest-build-provenance` against every `.nupkg` subject path
7. release manifest (`artifacts/release-bundle/release-manifest.json`) with SHA-256 + size for every package, SBOM, signature, NuGet vulnerability audit, and external-policy preflight artefact
8. NuGet trusted-publishing login (federated OIDC token exchange) followed by `dotnet nuget push` for `.nupkg` and `.snupkg` files; this stage runs only on a real tag push, never on `workflow_dispatch` dry-runs

Per-package nuget.org configuration must be in place before the first push:

- configure trusted publishing on nuget.org for the `Cephalon-Labs` account (settings → trusted publishing → add policy) pointing at the `Cephalon-Labs/CephalonEngine` repository, the `.github/workflows/publish-release.yml` workflow file, and the `v*.*.*` tag pattern
- reserve the `Cephalon.*` prefix on nuget.org once a stable GA cut is coming so prefix protection lines up with the first `1.0.0` release
- ensure the `NUGET_USER` repository secret is set so the trusted-publishing login step can resolve the publishing account
- set `CEPHALON_NUGET_TRUSTED_PUBLISHING_POLICY_CONFIRMED=true` and `CEPHALON_NUGET_PREFIX_RESERVATION_CONFIRMED=true` as protected repository variables only after the release manager has confirmed the matching nuget.org policies

The pipeline is intentionally additive over `release-validation.yml`. The per-PR validation gate stays unchanged; this workflow only runs on a tag and only pushes when run from a tag.

The preflight writes `artifacts/supply-chain-external-policy/external-policy-preflight.json` and verifies only facts the workflow can truthfully know: `NUGET_USER` is present and the two release-manager confirmation variables are set. It does not claim to independently inspect nuget.org policy state.

The release-readiness scorecard validates the workflow-facing portion of this posture through [`scripts/supply-chain-release-support.json`](../scripts/supply-chain-release-support.json) before emitting `SupplyChainEvidence`. That generated evidence records which release-provenance items are already workflow-ready (locked release validation, package checksums, package metadata validation, NuGet vulnerability audit, CycloneDX SBOM, Sigstore/Cosign signatures, SLSA provenance, Rekor transparency, and release-bundle checksums), which items still require external nuget.org or repository policy (trusted-publishing policy, `Cephalon.*` prefix reservation, and `NUGET_USER`), which fail-closed preflight checks must pass before a real tag can log in to NuGet, and the `SignedReleaseDryRun` status/proof/blocker/identity/action/command/output readback for `workflow_dispatch dry_run=true`. `scripts/invoke-signed-release-dry-run.ps1` is the release-manager probe for that separate dry-run evidence; its generated report must name the dry-run workflow URL before the package-publishing scorecard prose can move beyond partial signed-release proof.

## Maintenance rules

- keep the intended packable surface explicit; do not rely on solution-wide `dotnet pack` defaults
- keep benchmarks, playgrounds, and sample-only libraries out of the release package set unless they are deliberately promoted
- keep shared package metadata and any package-specific readmes aligned with the actual release surface
- keep `scripts/validate-package-metadata.ps1`, package tags, package readmes, symbol output, and the publish-artifact manifest aligned whenever package metadata changes
- keep `scripts/validate-nuget-vulnerability-audit.ps1`, release-validation output, workflow artifact upload, and supply-chain evidence aligned whenever the vulnerability-audit contract changes
- keep the stable `cephalon` tool command name aligned across CLI packaging, docs, and validation coverage
- keep release checksum/provenance metadata aligned with the actual repository source revision and package file set
- keep the published-module staging flow aligned with the CLI package-stage command and external package lifecycle guide
- keep package-publishing docs, the publish script, and release-validation automation aligned when the package boundary changes
- do not let package support or global or package-scoped deployment-mode claims exceed what `scripts/deployment-mode-support.json`, `scripts/validate-deployment-mode-claims.ps1`, `validate-release.ps1`, `validate-dotnet-readiness.ps1`, and the manifest-backed `publishProbePolicy` actually prove together
