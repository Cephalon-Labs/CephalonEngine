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
| Package-scoped single-file support | `Cephalon.Diagnostics`, `Cephalon.Abstractions`, and `Cephalon.Scaffolding` claimed | These are package-local claims only. They prove selected clean-baseline packages can carry single-file analyzer posture without widening the global engine, host, sample, or generated-app support contract. |

## Source of truth

Cephalon now keeps this contract explicit in two layers:

- machine-readable manifest: `scripts/deployment-mode-support.json` (schema `1.5.0`; per-mode `requiredProjectProperties`, `requiredAnalyzerProperties`, `warningPatterns`, package-scoped `deploymentModeEligibility.supportedModes`, load-bearing `singleFile` publish-gate policy, plus `representativePublishTargets`, `expectedPublishOutputShape`, advisory `knownTransitiveHazards`, and lock-file-backed `knownTransitiveHazardAudit`)
- repo-native validation and reporting: `scripts/validate-dotnet-readiness.ps1` plus `scripts/validate-deployment-mode-claims.ps1` (`claim-validation-report.json`, `hazard-inventory.json`, and a human-readable `README.md`)

The broader framework-readiness story stays aligned through:

- [.NET 11 readiness](dotnet11-readiness.md)
- [Compatibility](compatibility.md)
- [Package publishing](package-publishing.md)
- [Trim / Native AOT / single-file hazard inventory](trim-aot-hazard-inventory.md) — per-package reflection / dynamic-code patterns observed in shipped `src/Cephalon.*` runtime code, classified by severity (`excluded-by-design`, `clean-baseline`, `low`, `medium`, `high`), feeding the manifest-backed `deploymentModeEligibility`, `knownTransitiveHazards`, and `knownTransitiveHazardAudit` fields below

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

The shipped harness now runs as part of `scripts/validate-release.ps1` through the opt-out `-SkipDeploymentModeClaims` switch, so framework readiness and deployment-mode claim truth are reportable in one release-validation flow. Release validation reads `publishProbePolicy.releaseValidationDeploymentModes` and `publishProbePolicy.releaseValidationSkipsPublish`; the current manifest says `releaseValidationMode: "single-file-publish-gate"`, `releaseValidationDeploymentModes: ["singleFile"]`, `releaseValidationSkipsPublish: false`, `gatedModes: ["singleFile"]`, and `nonOptOutGate: true`, so release validation invokes `scripts/validate-deployment-mode-claims.ps1 -DeploymentMode singleFile` without `-SkipPublish`. The harness still keeps trim and Native AOT out of this release gate through `auditOnlyModes: ["trim", "nativeAot"]`; those modes can be audited deliberately, but they do not block the default release path until their own support-promotion slices land. The harness reads its per-mode `requiredProjectProperties`, `requiredAnalyzerProperties`, and `warningPatterns` from `scripts/deployment-mode-support.json` schema `1.5.0` directly through `Get-DeploymentModeConfigFromManifest`, with the hardcoded `$Script:DeploymentModeConfigs` table kept as a fallback for legacy manifests; manifest edits to those fields take effect on the next harness run without code changes.

`ENG-524` hardens the audit-only lane without promoting a global support claim. Direct trim or Native AOT runs now report `PublishProbeGate: not-applicable` when the evaluated modes are only `publishProbePolicy.auditOnlyModes`, so a deliberate audit is not failed just because the release gate is scoped to `singleFile`. Compiler-only analyzer and source-generator references also remove host publish-mode globals through `CephalonCompilerOnlyProjectReferenceGlobalPropertiesToRemove`, and the excluded-by-design compiler-only projects localize `PublishTrimmed`, `PublishAot`, `PublishSingleFile`, `SelfContained`, `RuntimeIdentifier`, and `RuntimeIdentifiers` with `TreatAsLocalProperty`. This lets trim and Native AOT probes reach the real runtime hazards instead of failing first on `netstandard2.0` analyzer/source-generator drift. Current trim and Native AOT audits still report aggregate `not-claimed`; the remaining blocker evidence is in `Cephalon.Engine` dynamic package loading and Native AOT behavior-input JSON materialization that require later deliberate support-promotion slices.

`ENG-454` adds the first package-scoped claim: `Cephalon.Diagnostics` is declared as `clean-baseline` with `supportedModes: ["singleFile"]`, and its project file declares `PublishSingleFile=true` plus `EnableSingleFileAnalyzer=true`. `ENG-525` extends the same scoped claim pattern to the host-agnostic `Cephalon.Abstractions` contract package and the `Cephalon.Scaffolding` generation package after both remained clean-baseline in the hazard inventory. The global single-file row remains `not-claimed`; `scripts/validate-deployment-mode-claims.ps1` treats those project-property signals as scoped package truth instead of global support drift, and reports the package claims separately in `PackageClaimAudits`.

`ENG-526` reduces the active trim publish blockers without widening any support row. `Cephalon.Engine` now parses `cephalon.package.json` through a source-generated `PackageDefinitionFileJsonContext`, `AddFeatureFlagProvider<TProvider>()` preserves provider public constructors for DI under trimming, and module-owned behavior registration carries explicit trimming annotations for behavior interfaces, typed JSON inputs, and type-based compatibility overloads. A focused trim publish probe against `samples/Cephalon.Sample.ModularMonolith` now drops from seven `Cephalon.Engine` analyzer errors to the two remaining dynamic package-loading errors in `PackageAssemblyLoadContext.LoadFromAssemblyPath(...)`. A focused Native AOT probe drops to those two package-loading errors plus the still-real generic behavior input JSON fallback. Global trim / Native AOT / single-file rows remain `not-claimed`.

`ENG-455` makes the manifest-backed hazard inventory an emitted harness artifact instead of a hand-authored-doc-only truth. Every run now includes `HazardInventory` inside `claim-validation-report.json`, writes a sibling `hazard-inventory.json`, and summarizes package count, tier count, scoped-claim count, and transitive-hazard hint count in the report README. `ENG-456` then retired the low-tier REST wire-name enum-field reflection surface by replacing nine attribute-reflection maps with closed switch mappings. `ENG-457` moves source-generated behavior dispatch startup onto generated closed-generic execution-slot hints. `ENG-458` applies the same pattern to durable execution by emitting closed `DurableExecutionSlot` adapters for strategy execution and runtime-catalog metadata. `ENG-459` removes the MySQL CDC hosted-service duck-typed capture failure metadata reflection by projecting failures through a typed internal contract, while `Cephalon.Data.MySql` stays `high` because the separate third-party `SciSharp.MySQL.Replication.ReplicationClient` non-public adapter remains. `ENG-460` applies the same typed-contract shape to the remaining Postgres and Oracle CDC hosted-service failure metadata paths, removing the last four duck-typed CDC exception metadata hazards from the manifest. `ENG-461` removes the ten EventSourcing persisted type-name rows by routing provider append/read payloads through the shared `IEventTypeRegistry` instead of provider-local `Type.GetType(...)`. `ENG-462` removes the core `Cephalon.Data` command/query dispatch row by routing default read/write stores through registered closed-generic dispatch descriptors instead of runtime `MakeGenericMethod` dispatch. `ENG-463` narrows the remaining `Cephalon.Behaviors.Http` row by moving REST route mapping, projection application, and REST module ownership onto type-based contracts instead of `MethodInfo.MakeGenericMethod/Invoke`. `ENG-464` retires the follow-up `ResultModel<>` OpenAPI metadata and generated-profile carrier-method rows by moving source-generated REST profile hints into a module-initializer registry and by projecting result-envelope schemas through endpoint metadata descriptors. `ENG-465` then retires the `Cephalon.Behaviors` generated-carrier rows: source-generated behavior module hints register through `BehaviorGeneratedModuleRegistry`, and `BehaviorModule` consumes those descriptors without reflective `Register` / `GetExecutionSlots` / `GetTopologyDescriptors` / `GetBehaviorsNeedingRuntimeTopology` lookup. `ENG-466` removes the `Cephalon.Behaviors.Http` generated-profile assembly-scan fallback by requiring source-generated registry hints for `MapGeneratedProfiles(...)`. `ENG-467` removes `DurableExecutionSlot.ForType(...)`, the old `ReflectionSlots` cache, and durable runtime-catalog interface fallback. `ENG-468` then moves saga choreography runtime-catalog authoring/result-shape metadata into source-generated or explicitly registered `SagaChoreographyRuntimeSlot` registrations, so `Cephalon.Behaviors.Patterns` returns to clean-baseline absence from the active package table. `ENG-469` removes the `Cephalon.Behaviors` fallback assembly scan by making auto-registration consume `BehaviorGeneratedModuleRegistry` hints only and fail fast for explicit assemblies without generated hints. `ENG-470` removes the generated auto-registration static `ConfigureTopology(...)` reflection fallback: source generation now emits descriptors for supported fluent topology or unambiguous attribute-only declarations, and generated auto-registration fails fast when topology cannot be reduced to generated metadata. `ENG-471` removes the `BehaviorExecutionSlot.ForType(...)` open-generic fallback: dispatch startup now requires source-generated or explicitly registered closed execution slots, with typed fluent/module overloads for manual registrations. `ENG-472` then removes `BehaviorTypeRegistry` / `IBehaviorTypeRegistry` by moving runtime behavior implementation mapping to `BehaviorImplementationDescriptor` records contributed by source-generated or explicit registration, so `Cephalon.Behaviors` returns to clean-baseline absence from the active package table. `ENG-473` retires the `Cephalon.Behaviors.Http` `MapProfile<TBehavior>()` attribute/profile binding fallback by requiring generated or explicitly registered profile descriptors plus behavior-type hints; explicit verb mappings remain the no-profile direct path. `ENG-474` retires the follow-up input-shape fallback by making explicit profile binding validation consume source-generated or explicitly registered `BehaviorRestInputContractDescriptor` metadata instead of inspecting runtime behavior/input types. `ENG-475` then retires the remaining manual/profile route-contract fallback by making REST routes consume source-generated or explicitly registered `BehaviorContractDescriptor` metadata instead of resolving behavior interfaces, attributes, or structured-result wrappers from runtime types. `Cephalon.Behaviors.Http` leaves the active first-party hazard table. A real manifest run after `ENG-475` reports 6 package entries, 3 packages with known hazards, 9 known hazard entries, 3 `high` + 0 `medium` + 2 `excluded-by-design` + 1 `clean-baseline` tier counts, zero active `low` entries, 1 package-scoped `singleFile` claim, and transitive-hazard hint counts of `trim=4`, `nativeAot=8`, and `singleFile=2`. This is audit/readiness evidence; it does not widen any global support row.

`ENG-476` then retires the broader `Cephalon.Engine` module-discovery blocker by moving assembly-based module discovery onto generated `ModuleDiscoveryDescriptor` metadata registered by `Cephalon.Engine.SourceGen`. `ENG-477` follows by moving the remaining MySQL SciSharp reflective transport path out of `Cephalon.Data.MySql` and into the optional `Cephalon.Data.MySql.SciSharpReplication` adapter package, so the core MySQL package no longer carries that third-party reflection dependency. `ENG-478` makes the optional adapter's permanent `not-claimed` posture machine-checkable by declaring `IsTrimmable=false`, `IsAotCompatible=false`, `PublishTrimmed=false`, `PublishAot=false`, and `PublishSingleFile=false` in its project file, recording the same values in `deploymentModeEligibility.packages[].requiredProjectProperties`, and adding manifest Pester coverage that verifies package-level required properties against the owning csproj. `ENG-479` adds the lock-file-backed transitive hazard audit subset: `knownTransitiveHazardAudit` scans `src/**/packages.lock.json`, `samples/**/packages.lock.json`, and `benchmarks/**/packages.lock.json`, and the real harness inventory now reports `matched` for all 7 audited package-pattern entries across 116 lock files with 0 missing entries. `ENG-480` adds `publishProbePolicy` as the manifest-backed release-gate posture. `ENG-494` closes the remaining high-tier package decision sweep: `Cephalon.Data.MySql.SciSharpReplication` and `Cephalon.ReferenceDocs` stay as deliberate package-level `not-claimed` support boundaries backed by project properties, manifest `requiredProjectProperties`, component docs, and manifest tests. `ENG-510` promotes only the representative `singleFile` publish probe into a non-opt-out release gate: `validate-release.ps1` now runs the harness for `singleFile` without `-SkipPublish`, the generated report emits `PublishProbeGate`, and a failed, skipped, targetless, or warning-producing gated publish probe fails release validation even though global single-file support remains `not-claimed`. `ENG-516` carries that proof into the engine-completion scorecard by validating the generated `artifacts/deployment-mode-claims-release/claim-validation-report.json` against the manifest policy, target count, zero-warning/zero-error gate, package-claim verdicts, and hazard-inventory counts before release validation or `cephalon doctor --scorecard` can report the gate as healthy. `ENG-524` then keeps trim and Native AOT audit-only while removing false compiler-only publish drift from the probe path. `ENG-525` adds scoped single-file claims for `Cephalon.Abstractions` and `Cephalon.Scaffolding`. Trim and Native AOT remain audit-only. The current manifest has 8 package entries, 2 packages with known hazards, 14 known hazard entries, 2 `high` + 0 `medium` + 3 `excluded-by-design` + 3 `clean-baseline` tier counts, zero active `low` entries, 3 package-scoped `singleFile` claims, and transitive-hazard hint counts of `trim=4`, `nativeAot=8`, and `singleFile=2`. Remaining global deployment-mode promotion work is now a later deliberate slice that widens global support rows only if the intended package set, docs, workflow, and package guidance all agree.

`ENG-449` expanded `representativePublishTargets.projects` from the original ModularMonolith-only probe to five sample hosts:

- `samples/Cephalon.Sample.ModularMonolith/Cephalon.Sample.ModularMonolith.csproj`
- `samples/Cephalon.Sample.ModularVerticalSlice/Cephalon.Sample.ModularVerticalSlice.csproj`
- `samples/Cephalon.Sample.Microservice/Cephalon.Sample.Microservice.csproj`
- `samples/Cephalon.Sample.MicroserviceSuite/services/CatalogService/Cephalon.Sample.MicroserviceSuite.CatalogService.csproj`
- `samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj`

That expanded set is verified with `scripts/validate-deployment-mode-claims.ps1 -DeploymentMode singleFile` returning successful publish-probe targets with zero warnings and zero errors. Publish probes snapshot and restore `packages.lock.json` files around the probe so validation can restore the RID/linker graph it needs without leaving machine-specific lock-file churn in the worktree. The manifest still declares `not-claimed` for all three deployment modes; this is readiness evidence, not a support claim.

Manifest schema fields in `scripts/deployment-mode-support.json`:

- `validationStrategy`: `analyzer-only`, `publish-required`, or `full-flow`
- `publishProbePolicy`: release-validation publish-probe posture (`releaseValidationMode`, `releaseValidationDeploymentModes`, `releaseValidationSkipsPublish`, `gatedModes`, `auditOnlyModes`, `failureBlocksRelease`, `failOnWarnings`, `nonOptOutGate`, `gatePromotion`, and `promotionRequirements`) that explains which modes `scripts/validate-release.ps1` publishes by default and whether publish failures block the release
- per-package `deploymentModeEligibility`: `packageName`, `nugetId`, `claimAuditTier`, `supportedModes` per scoped claim, `requiredProjectProperties` per claim, `minimumAnalyzerPackVersion`, and `knownHazards: []` (reflection, native interop, dynamic dispatch, third-party transitive risk)
- `knownTransitiveHazardAudit`: lock-file glob set plus audited package patterns for the transitive-hazard subset that should remain visible in `hazard-inventory.json` even while broad `knownTransitiveHazards` lists stay advisory
- `representativePublishTargets.projects`: the small set of sample hosts the harness actually publishes during validation so the claim is anchored in real binary output
- `expectedPublishOutputShape`: shape constraints (single-file binary signature, allowed warning categories, allowed size bounds)

Harness phases:

1. **project-property audit** scans every `src/Cephalon.*/*.csproj` and reports which projects set the claimed deployment-mode properties and which do not
2. **analyzer phase** verifies the matching analyzer pack is enabled and at or above `minimumAnalyzerPackVersion` for projects that claim AOT or trim
3. **publish phase** runs `dotnet publish -c Release` with the requested mode against each `representativePublishTargets.projects` entry and captures exit code, warnings, and errors; output-shape expectations stay declared in the manifest until a later claim-promotion slice makes them load-bearing
4. **inventory phase** projects `deploymentModeEligibility.packages`, `knownTransitiveHazards`, and the lock-file-backed `knownTransitiveHazardAudit` subset into the manifest-backed `HazardInventory` read model
5. **policy phase** projects `publishProbePolicy` into `PublishProbePolicy` so release artifacts explain why release validation did or did not run publish probes
6. **gate phase** computes `PublishProbeGate` so non-opt-out gated modes fail the run when their publish probe is skipped, targetless, failing, or warning-producing; direct audit-only trim or Native AOT runs report `not-applicable` instead of borrowing the single-file release gate
7. **report phase** writes `artifacts/deployment-mode-claims-release/claim-validation-report.json`, `artifacts/deployment-mode-claims-release/hazard-inventory.json`, and a human-readable `README.md`
8. **scorecard phase** reads the generated `claim-validation-report.json` through `DeploymentModeEvidence.ClaimsReport*` fields so release validation and `cephalon doctor --scorecard` summarize the same publish-gate proof instead of trusting manifest policy alone

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
- compiler-only analyzer/source-generator projects accidentally inheriting host publish-mode globals during representative publish probes; `GlobalPropertiesToRemove` and `TreatAsLocalProperty` guard this path

When the harness grows new load-bearing phases, this section is rewritten in place to describe the actual validation flow, the report paths, and the workflow integration; cross-references in [`compatibility.md`](compatibility.md), [`engineering-standards.md`](engineering-standards.md), [`dotnet11-readiness.md`](dotnet11-readiness.md), and [`project-memory.md`](project-memory.md) are updated together in the same slice.

Until a mode is deliberately promoted with matching manifest, project-property, support docs, and release-validation truth, the global support contract above stays at `not-claimed` for trim, Native AOT, and single-file; analyzer-only or local-experiment signals do not widen the contract. A publish-probe gate is narrower than a support claim: today it proves the representative single-file sample-host publish path is release-blocking, not that every package or app shape is globally single-file supported. Package-scoped claims can move earlier, but only when the package is explicitly listed in `deploymentModeEligibility.packages`, starts from `clean-baseline`, carries the matching project properties, and passes the harness's `PackageClaimAudits` flow.

## What this guide does not mean

- it does not move Cephalon's shipping floor from `net10.0`
- it does not turn `.NET 11` readiness into a default-target migration
- it does not claim global trim, Native AOT, or single-file compatibility today
- it does not let support statements outrun what the readiness report and release-validation flow actually prove
