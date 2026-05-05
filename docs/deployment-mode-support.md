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

## Source of truth

Cephalon now keeps this contract explicit in two layers:

- machine-readable manifest: `scripts/deployment-mode-support.json` (schema `1.1.0`; per-mode `requiredProjectProperties`, `requiredAnalyzerProperties`, `warningPatterns`, plus `representativePublishTargets`, `expectedPublishOutputShape`, `deploymentModeEligibility`, and `knownTransitiveHazards`)
- repo-native validation and reporting: `scripts/validate-dotnet-readiness.ps1`

The broader framework-readiness story stays aligned through:

- [.NET 11 readiness](dotnet11-readiness.md)
- [Compatibility](compatibility.md)
- [Package publishing](package-publishing.md)
- [Trim / Native AOT / single-file hazard inventory](trim-aot-hazard-inventory.md) — per-package reflection / dynamic-code patterns observed in shipped `src/Cephalon.*` runtime code, classified by severity (`excluded-by-design`, `clean-baseline`, `low`, `medium`, `high`), feeding the planned `deploymentModeEligibility` / `knownTransitiveHazards` manifest fields below

External adopters can read that same contract through:

- `cephalon doctor` for the machine-level shipping floor, readiness lane, and current trim / Native AOT / single-file posture
- `cephalon doctor --app-root <path>` for generated-app bootstrap plus generated host target-framework, generated self-hosted and hosted deployment assets, generated container deployment assets, generated Dockerfile baseline, and publish-mode claim posture against the same contract

## What the current statuses mean

- `not-claimed` means Cephalon does not currently publish trim, Native AOT, or single-file as supported deployment modes for external adopters.
- `not-claimed` does **not** block targeted local experiments, but those experiments do not become repo truth by themselves.
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

## Validation harness (initial slice shipped, follow-through pending)

Today the support claim is `not-claimed` across trim, Native AOT, and single-file. To move any of those to a real `claimed` state honestly, Cephalon needs a machine-checkable validation harness that proves the claim against actual `dotnet publish` results, not only against analyzer signals. The first slice of that harness now ships as `scripts/validate-deployment-mode-claims.ps1` with comprehensive Pester coverage at `tests/Cephalon.Tests.Scripts/validate-deployment-mode-claims.Tests.ps1`. The follow-through work (CI integration via `scripts/validate-release.ps1`, the manifest schema extension below, and an actual representative-publish-target list) is still planned.

The shipped harness now runs as part of `scripts/validate-release.ps1` through the opt-out `-SkipDeploymentModeClaims` switch (audit-only by default until `representativePublishTargets.projects` is populated), so framework readiness and deployment-mode claim truth are reportable in one release-validation flow. The harness also now reads its per-mode `requiredProjectProperties`, `requiredAnalyzerProperties`, and `warningPatterns` from `scripts/deployment-mode-support.json` schema `1.1.0` directly through `Get-DeploymentModeConfigFromManifest`, with the hardcoded `$Script:DeploymentModeConfigs` table kept as a fallback for legacy manifests; manifest edits to those fields take effect on the next harness run without code changes.

Planned manifest schema additions in `scripts/deployment-mode-support.json`:

- `validationStrategy`: `analyzer-only`, `publish-required`, or `full-flow`
- per-package `deploymentModeEligibility`: `packageName`, `nugetId`, `supportedModes` per claim, `requiredProjectProperties` per claim, `minimumAnalyzerPackVersion`, and `knownHazards: []` (reflection, native interop, dynamic dispatch, third-party transitive risk)
- `representativePublishTargets: []`: the small set of packages the harness actually publishes during validation so the claim is anchored in real binary output
- `expectedPublishOutputShape`: shape constraints (single-file binary signature, allowed warning categories, allowed size bounds)

Planned harness phases:

1. **project-property audit** scans every `src/Cephalon.*/*.csproj` and reports which projects set the claimed deployment-mode properties and which do not
2. **analyzer phase** verifies the matching analyzer pack is enabled and at or above `minimumAnalyzerPackVersion` for projects that claim AOT or trim
3. **publish phase** runs `dotnet publish -c Release` with the requested mode against each `representativePublishTargets` entry and captures exit code, warnings, errors, and binary artefacts
4. **report phase** writes `artifacts/deployment-mode-release/{mode}-claim-validation-report.json` plus a human-readable `README.md`

Aggregate verdicts the report emits:

- `claim-truthful` — manifest claims the mode, all targets publish cleanly, analyzers pass
- `claim-overstated` — manifest claims the mode, but publish or analyzer results show warnings or errors that contradict the claim
- `not-claimed` — manifest says `not-claimed` and the audit confirms no project sets the matching properties
- `mixed` — partial pass across multiple packages

Known risks the harness must report on rather than hide:

- transitive dependencies that emit `IL2026` / `IL3050` / NETSDK trim or AOT warnings even when the Cephalon package itself looks clean
- native interop in cloud-provider SDKs (`AWSSDK.*`, `Azure.*`, `Google.*`) that may break Native AOT in non-obvious ways
- reflection in source-generated boundaries and consumer code that invokes generated stubs reflectively
- benchmark and test utilities (`BenchmarkDotNet`, broad reflection) that should not poison the framework claim
- analyzer-pack version drift where a project claims AOT but uses an older analyzer that misses violations

When the harness ships, this section is rewritten in place to describe the actual validation flow, the report path, and the workflow integration; cross-references in [`compatibility.md`](compatibility.md), [`engineering-standards.md`](engineering-standards.md), [`dotnet11-readiness.md`](dotnet11-readiness.md), and [`project-memory.md`](project-memory.md) are updated together in the same slice.

Until the harness ships, the support contract above stays at `not-claimed` for trim, Native AOT, and single-file; analyzer-only or local-experiment signals do not widen the contract.

## What this guide does not mean

- it does not move Cephalon's shipping floor from `net10.0`
- it does not turn `.NET 11` readiness into a default-target migration
- it does not claim trim, Native AOT, or single-file compatibility today
- it does not let support statements outrun what the readiness report and release-validation flow actually prove
