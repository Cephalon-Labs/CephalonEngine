# Deployment-mode support

This guide records the current Cephalon support contract for trimming, Native AOT, and single-file publishing.

## Current contract

| Concern | Current contract | Notes |
| --- | --- | --- |
| Stable shipping floor | `net10.0` | Cephalon still ships stable packages, templates, and samples on the current LTS baseline. |
| Higher-SDK readiness lane | `net11.0` assessment-only | `.NET 11` remains a readiness lane, not a supported default-target migration. |
| Trim | `not-claimed` | Trimming is not part of the current Cephalon support contract. |
| Native AOT | `not-claimed` | Native AOT is not part of the current Cephalon support contract. |
| Single-file | `not-claimed` | Single-file publishing is not part of the current Cephalon support contract. |
| Package-scoped single-file support | `Cephalon.Diagnostics` claimed | This is a package-local claim only. It proves the diagnostics companion package can carry single-file analyzer posture without widening the global engine, host, sample, or generated-app support contract. |

## Source of truth

Cephalon now keeps this contract explicit in two layers:

- machine-readable manifest: `scripts/deployment-mode-support.json` (schema `1.2.0`; per-mode `requiredProjectProperties`, `requiredAnalyzerProperties`, `warningPatterns`, package-scoped `deploymentModeEligibility.supportedModes`, plus `representativePublishTargets`, `expectedPublishOutputShape`, and `knownTransitiveHazards`)
- repo-native validation and reporting: `scripts/validate-dotnet-readiness.ps1` plus `scripts/validate-deployment-mode-claims.ps1` (`claim-validation-report.json`, `hazard-inventory.json`, and a human-readable `README.md`)

The broader framework-readiness story stays aligned through:

- [.NET 11 readiness](dotnet11-readiness.md)
- [Compatibility](compatibility.md)
- [Package publishing](package-publishing.md)
- [Trim / Native AOT / single-file hazard inventory](trim-aot-hazard-inventory.md) — per-package reflection / dynamic-code patterns observed in shipped `src/Cephalon.*` runtime code, classified by severity (`excluded-by-design`, `clean-baseline`, `low`, `medium`, `high`), feeding the manifest-backed `deploymentModeEligibility` / `knownTransitiveHazards` fields below

External adopters can read that same contract through:

- `cephalon doctor` for the machine-level shipping floor, readiness lane, and current trim / Native AOT / single-file posture
- `cephalon doctor --app-root <path>` for generated-app bootstrap plus generated host target-framework, generated self-hosted and hosted deployment assets, generated container deployment assets, generated Dockerfile baseline, and publish-mode claim posture against the same contract

## What the current statuses mean

- `not-claimed` means Cephalon does not currently publish trim, Native AOT, or single-file as supported deployment modes for external adopters.
- `not-claimed` does **not** block targeted local experiments, but those experiments do not become repo truth by themselves.
- package-scoped `supportedModes` claims are narrower: they apply only to the named package entry in `deploymentModeEligibility.packages` and do not promote the global mode row from `not-claimed`.
- analyzer-only signals remain useful readiness input, but they do not widen the support contract without matching validation, docs, and planning updates.

The reserved future `claimed` state should only be used once Cephalon intentionally enables and validates that deployment mode as part of the shipped framework story.

## How a support claim changes

For trim, Native AOT, or single-file support to become real Cephalon support statements, all of the following must move together:

- project properties that intentionally enable or declare the deployment mode
- `scripts/validate-dotnet-readiness.ps1`
- release-validation workflow coverage
- `docs/deployment-mode-support.md`
- `docs/compatibility.md`
- `docs/package-publishing.md`
- `docs/project-memory.md`
- `docs/engine-roadmap.md`
- `docs/engine-backlog.md`

## Validation harness (manifest-driven, claims still not claimed)

Today the support claim is `not-claimed` across trim, Native AOT, and single-file. Moving any of those to a real `claimed` state requires a machine-checkable validation harness that proves the claim against actual `dotnet publish` results, not only against analyzer signals. That harness now ships as `scripts/validate-deployment-mode-claims.ps1` with comprehensive Pester coverage at `tests/Cephalon.Tests.Scripts/validate-deployment-mode-claims.Tests.ps1`, manifest-schema coverage at `tests/Cephalon.Tests.Scripts/deployment-mode-support-manifest.Tests.ps1`, and a manifest-backed default publish-target list.

The shipped harness now runs as part of `scripts/validate-release.ps1` through the opt-out `-SkipDeploymentModeClaims` switch, so framework readiness and deployment-mode claim truth are reportable in one release-validation flow. Release validation still passes `-SkipPublish` deliberately, so CI remains audit-only until the project intentionally promotes the publish-probe lane to a non-opt-out gate. The harness reads its per-mode `requiredProjectProperties`, `requiredAnalyzerProperties`, and `warningPatterns` from `scripts/deployment-mode-support.json` schema `1.2.0` directly through `Get-DeploymentModeConfigFromManifest`, with the hardcoded `$Script:DeploymentModeConfigs` table kept as a fallback for legacy manifests; manifest edits to those fields take effect on the next harness run without code changes.

`ENG-454` adds the first package-scoped claim: `Cephalon.Diagnostics` is declared as `clean-baseline` with `supportedModes: ["singleFile"]`, and its project file declares `PublishSingleFile=true` plus `EnableSingleFileAnalyzer=true`. The global single-file row remains `not-claimed`; `scripts/validate-deployment-mode-claims.ps1` now treats that project-property signal as scoped package truth instead of global support drift, and reports the package claim separately in `PackageClaimAudits`.

`ENG-455` makes the manifest-backed hazard inventory an emitted harness artifact instead of a hand-authored-doc-only truth. Every run now includes `HazardInventory` inside `claim-validation-report.json`, writes a sibling `hazard-inventory.json`, and summarizes package count, tier count, scoped-claim count, and transitive-hazard hint count in the report README. `ENG-456` then retired the low-tier REST wire-name enum-field reflection surface by replacing nine attribute-reflection maps with closed switch mappings. `ENG-457` moves source-generated behavior dispatch startup onto generated closed-generic execution-slot hints, while keeping the `Cephalon.Behaviors` package in the `high` tier because carrier-method lookups and runtime-discovery fallback still exist. `ENG-458` applies the same pattern to durable execution: source-generated durable behaviors now register closed `DurableExecutionSlot` adapters for strategy execution and runtime-catalog metadata, while `Cephalon.Behaviors.Patterns` stays `high` because the manifest-listed `DurableExecutionSlot.ForType(...)` fallback still exists. A real manifest run after `ENG-457` reported 22 package entries, 19 packages with known hazards, 51 known hazard entries, 5 `high` + 14 `medium` + 2 `excluded-by-design` + 1 `clean-baseline` tier counts, zero active `low` entries, 1 package-scoped `singleFile` claim, and transitive-hazard hint counts of `trim=4`, `nativeAot=8`, and `singleFile=2`; `ENG-458` updates the `Cephalon.Behaviors.Patterns` hazard row in place, so those aggregate counts do not move. This is audit/readiness evidence; it does not widen any global support row.

`ENG-449` expanded `representativePublishTargets.projects` from the original ModularMonolith-only probe to five sample hosts:

- `samples/Cephalon.Sample.ModularMonolith/Cephalon.Sample.ModularMonolith.csproj`
- `samples/Cephalon.Sample.ModularVerticalSlice/Cephalon.Sample.ModularVerticalSlice.csproj`
- `samples/Cephalon.Sample.Microservice/Cephalon.Sample.Microservice.csproj`
- `samples/Cephalon.Sample.MicroserviceSuite/services/CatalogService/Cephalon.Sample.MicroserviceSuite.CatalogService.csproj`
- `samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj`

That expanded set is verified with `scripts/validate-deployment-mode-claims.ps1 -DeploymentMode singleFile` returning successful publish-probe targets with zero warnings and zero errors. Publish probes snapshot and restore `packages.lock.json` files around the probe so validation can restore the RID/linker graph it needs without leaving machine-specific lock-file churn in the worktree. The manifest still declares `not-claimed` for all three deployment modes; this is readiness evidence, not a support claim.

Manifest schema fields in `scripts/deployment-mode-support.json`:

- `validationStrategy`: `analyzer-only`, `publish-required`, or `full-flow`
- per-package `deploymentModeEligibility`: `packageName`, `nugetId`, `claimAuditTier`, `supportedModes` per scoped claim, `requiredProjectProperties` per claim, `minimumAnalyzerPackVersion`, and `knownHazards: []` (reflection, native interop, dynamic dispatch, third-party transitive risk)
- `representativePublishTargets.projects`: the small set of sample hosts the harness actually publishes during validation so the claim is anchored in real binary output
- `expectedPublishOutputShape`: shape constraints (single-file binary signature, allowed warning categories, allowed size bounds)

Harness phases:

1. **project-property audit** scans every `src/Cephalon.*/*.csproj` and reports which projects set the claimed deployment-mode properties and which do not
2. **analyzer phase** verifies the matching analyzer pack is enabled and at or above `minimumAnalyzerPackVersion` for projects that claim AOT or trim
3. **publish phase** runs `dotnet publish -c Release` with the requested mode against each `representativePublishTargets.projects` entry and captures exit code, warnings, and errors; output-shape expectations stay declared in the manifest until a later claim-promotion slice makes them load-bearing
4. **inventory phase** projects `deploymentModeEligibility.packages` and `knownTransitiveHazards` into the manifest-backed `HazardInventory` read model
5. **report phase** writes `artifacts/deployment-mode-claims-release/claim-validation-report.json`, `artifacts/deployment-mode-claims-release/hazard-inventory.json`, and a human-readable `README.md`

Aggregate verdicts the report emits:

- `claim-truthful` — manifest claims the mode, all targets publish cleanly, analyzers pass
- `claim-overstated` — manifest claims the mode, but publish or analyzer results show warnings or errors that contradict the claim
- `not-claimed` — manifest says `not-claimed` and the audit confirms no unscoped project sets the matching properties; scoped package claims are reported separately
- `mixed` — partial pass across multiple packages

Known risks the harness must report on rather than hide:

- transitive dependencies that emit `IL2026` / `IL3050` / NETSDK trim or AOT warnings even when the Cephalon package itself looks clean
- native interop in cloud-provider SDKs (`AWSSDK.*`, `Azure.*`, `Google.*`) that may break Native AOT in non-obvious ways
- reflection in source-generated boundaries and consumer code that invokes generated stubs reflectively
- benchmark and test utilities (`BenchmarkDotNet`, broad reflection) that should not poison the framework claim
- analyzer-pack version drift where a project claims AOT but uses an older analyzer that misses violations

When the harness grows new load-bearing phases, this section is rewritten in place to describe the actual validation flow, the report paths, and the workflow integration; cross-references in [`compatibility.md`](compatibility.md), [`engineering-standards.md`](engineering-standards.md), [`dotnet11-readiness.md`](dotnet11-readiness.md), and [`project-memory.md`](project-memory.md) are updated together in the same slice.

Until a mode is deliberately promoted with matching manifest, project-property, publish-probe, and release-validation truth, the global support contract above stays at `not-claimed` for trim, Native AOT, and single-file; analyzer-only or local-experiment signals do not widen the contract. Package-scoped claims can move earlier, but only when the package is explicitly listed in `deploymentModeEligibility.packages`, starts from `clean-baseline`, carries the matching project properties, and passes the harness's `PackageClaimAudits` flow.

## What this guide does not mean

- it does not move Cephalon's shipping floor from `net10.0`
- it does not turn `.NET 11` readiness into a default-target migration
- it does not claim global trim, Native AOT, or single-file compatibility today
- it does not let support statements outrun what the readiness report and release-validation flow actually prove
