# .NET 11 Readiness

This guide records the current Cephalon truth for future-framework assessment without silently changing the shipping baseline.

## Current repo truth

- stable Cephalon packages still ship on `net10.0`
- `global.json` pins `.NET SDK 10.0.401` with `rollForward: disable` and `allowPrerelease: false`
- the template-pack package, analyzer meta-package, and source-generator surfaces remain the intentional `netstandard2.0` exceptions
- `.NET 11` is currently a readiness lane, not a default-target migration
- trim, Native AOT, and single-file support remain explicit global `not-claimed` support statements tracked through [Deployment-mode support](deployment-mode-support.md) and `scripts/deployment-mode-support.json`; package-scoped claims such as `Cephalon.Diagnostics` single-file support are narrower manifest entries and do not change the global support rows, and `publishProbePolicy` now makes the representative `singleFile` publish probe release-blocking without promoting global single-file support

As of **September 16, 2026**, the official [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) and [.NET 11 download page](https://dotnet.microsoft.com/en-us/download/dotnet/11.0) show:

- .NET 11 RC1 / SDK 11.0.100-rc.1, released September 8, 2026; Microsoft lists RC1 as go-live with support through October 13, 2026.
- .NET 10.0.12 is the current listed patch, released September 8; .NET 10 LTS ends November 14, 2028.
- Cephalon still ships net10.0 with repository SDK 10.0.401. Upstream go-live status does not establish Cephalon .NET 11 support.
- ENG-739 updates the stable SDK and its three SDK-dependent ILLink lock entries to 10.0.12. ENG-731 remains open for consumer/OS/deployment proof under ENG-742. This servicing change does not claim .NET 11 support. See [compatibility repair evidence](compatibility-repair-2026-09.md).

This supersedes July 7 Preview 5 as current external truth. The older preview links below remain historical research references. Recheck upstream patch/RC/GA state at the next readiness run and before each release.

Official sources:

- [.NET 11 Preview 1 announcement](https://devblogs.microsoft.com/dotnet/dotnet-11-preview-1/)
- [.NET 11 Preview 2 announcement](https://devblogs.microsoft.com/dotnet/dotnet-11-preview-2/)
- [.NET 11 Preview 3 announcement](https://devblogs.microsoft.com/dotnet/dotnet-11-preview-3/)
- [.NET 11 Preview 4 announcement](https://devblogs.microsoft.com/dotnet/dotnet-11-preview-4/)
- [.NET 11 Preview 5 announcement](https://github.com/dotnet/core/discussions/10445)
- [What's new in .NET 11 (Microsoft Learn)](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/overview)
- [What's new in the SDK and tooling for .NET 11](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/sdk)
- [What's new in C# 15](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15)
- [.NET 11 download page](https://dotnet.microsoft.com/en-us/download/dotnet/11.0)
- [.NET 10 download page](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [.NET 11 release-notes folder (`dotnet/core`)](https://github.com/dotnet/core/tree/main/release-notes/11.0/preview/)
- [.NET 11 Preview 4 release-notes folder (`dotnet/core`)](https://github.com/dotnet/core/tree/main/release-notes/11.0/preview/preview4)
- [.NET 11 Preview 5 release-notes folder (`dotnet/core`)](https://github.com/dotnet/core/tree/main/release-notes/11.0/preview/preview5)
- [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy)
- [`actions/setup-dotnet` version-channel guidance](https://github.com/actions/setup-dotnet)

## Why Cephalon uses a readiness lane

Cephalon is a framework, so a framework-baseline change is not the same thing as “the latest SDK happens to compile the repo once.”

The readiness lane exists to keep these truths separate:

- shipping floor: the baseline Cephalon packages, templates, samples, and docs intentionally support today
- readiness surface: the newer SDK/runtime family that Cephalon is actively assessing for future adoption
- support claims: deployment-mode promises such as trimming, Native AOT, and single-file, which require explicit proof before they become repo truth

That split lets Cephalon learn early from preview analyzers and runtime changes without forcing adopters onto a preview-only baseline.

## Repo-native validation flow

Cephalon now ships `scripts/validate-dotnet-readiness.ps1`.

The script:

- records both the repo-selected SDK (`global.json`-aware) and the readiness SDK selected from a temporary working directory outside the repo root
- audits project target frameworks across `src/`, `tests/`, `samples/`, `templates/`, and `benchmarks/`
- confirms the shipped source baseline still stays on `net10.0`, with only the documented `netstandard2.0` exceptions
- confirms starter template project files still align with the stable `net10.0` floor
- fails if the repo starts using legacy `TargetFrameworkVersion`
- records the current trim / Native AOT / single-file claim status from the manifest-backed deployment-mode support contract
- records package-scoped deployment-mode claims separately so clean-baseline package proofs do not look like global support drift
- keeps deployment-mode claim status separate from the richer `scripts/validate-deployment-mode-claims.ps1` artifact set, where `PublishProbePolicy` explains release-validation gate posture, `PublishProbeGate` records the gated single-file publish result, and `HazardInventory` / `hazard-inventory.json` exposes tier counts, scoped claims, transitive-hazard hints, and the lock-file-backed `knownTransitiveHazardAudit` subset
- keeps generated-app readiness checks discoverable through `cephalon doctor --app-root <path>` so template, scaffold, package-feed, target-framework, and deployment-mode posture checks stay visible to adopters
- keeps source-generated, registry-backed, and typed-contract remediation evidence separate from support promotion: `Cephalon.Behaviors` generated execution-slot hints now reduce source-generated dispatch startup reflection, its generated module registry avoids generated-carrier lookup, generated-hint-only auto-registration removes the fallback assembly scan, generated topology descriptors plus fail-fast unsupported topology remove runtime static `ConfigureTopology(...)` invocation from generated auto-registration, source-generated or explicitly registered closed execution slots replace `BehaviorExecutionSlot.ForType(...)` dispatch fallback, explicit `JsonTypeInfo<TInput>` dispatch slots let hosts avoid reflection-based input materialization, and `BehaviorImplementationDescriptor` records replace mutable runtime type-registry joins; `Cephalon.Behaviors.Patterns` generated `DurableExecutionSlot` registrations plus the registered-slot fail-fast path now remove durable-execution open-generic adapter reflection, and generated or explicit `SagaChoreographyRuntimeSlot` registrations now remove saga choreography runtime-catalog shape inspection; the provider-native CDC hosted services now remove duck-typed failure metadata reflection through typed internal contracts; `Cephalon.Data.MySql` now keeps the SciSharp binlog transport in the optional `Cephalon.Data.MySql.SciSharpReplication` adapter instead of the core package, and that adapter now declares a machine-checkable permanent `not-claimed` posture through matching csproj and manifest `requiredProjectProperties`; the EventSourcing provider family now removes persisted type-name reflection through `IEventTypeRegistry`; core `Cephalon.Data` command/query dispatch now avoids runtime `MakeGenericMethod` through registered closed-generic dispatch descriptors; `Cephalon.Behaviors.Http` now avoids REST route/projection/module ownership reflection, generated-profile carrier-method lookup, generated-profile assembly-scan fallback, runtime attribute/profile fallback for `MapProfile<TBehavior>()`, input-shape inspection, manual/profile route-contract reflection, and `ResultModel<>` runtime construction through type-based route contracts, a source-generated REST profile registry, generated or explicitly registered profile descriptors, descriptor-backed input contracts, descriptor-backed endpoint contracts, fail-fast generated-profile mapping, and descriptor-based result-envelope OpenAPI metadata; `Cephalon.Engine` module discovery now avoids assembly scanning and `Activator.CreateInstance(...)` through generated `ModuleDiscoveryDescriptor` metadata from `Cephalon.Engine.SourceGen`, package manifest JSON parsing now uses source-generated `PackageDefinitionFileJsonContext`, feature-flag provider registration now preserves public constructors for DI under trimming, module-owned behavior registration now carries explicit trim/AOT annotations plus `JsonTypeInfo<TInput>` overloads, and dynamic package loading is isolated behind an explicit Native AOT fail-fast package boundary; `Cephalon.AspNetCore` has also moved OpenAPI/security/health JSON/config paths away from reflection-based binders and generic JsonNode materialization, and `MapCephalon()` now exposes the full operator route layer as an explicit dynamic Minimal API boundary through trim/AOT annotations so package-local analyzer builds pass without promoting adapter-level Native AOT. Global trim / Native AOT / single-file rows stay `not-claimed`; the three high-tier packages are deliberate package-level support boundaries or dynamic-route boundaries, and the current non-opt-out gate proves only the representative single-file publish path.
- can optionally build, test, publish reference docs, and publish package artifacts under the readiness SDK without editing `global.json`
- accepts `-TestFilter` for readiness-only workflow lanes that must keep broad build/test coverage while excluding known SDK-preview package/template roundtrips from becoming false shipping-baseline failures

Example audit-only run:

```powershell
pwsh ./scripts/validate-dotnet-readiness.ps1 `
  -Configuration Release `
  -SkipBuild `
  -SkipTests `
  -SkipReferenceDocs `
  -SkipPackages
```

Example higher-SDK readiness run:

```powershell
pwsh ./scripts/validate-dotnet-readiness.ps1 -Configuration Release
```

The script writes:

- `artifacts/dotnet-readiness-release/README.md`
- `artifacts/dotnet-readiness-release/dotnet-readiness-report.json`

## Release-validation contract

`pwsh ./scripts/validate-release.ps1` now emits the `.NET readiness` report as part of the normal release-validation flow.

That baseline report stays on the shipping toolchain and exists to keep framework, docs, package, and claim metadata honest.

The release-validation workflow uses the repository SDK roll-forward lane (`10.0.301` / runtime `10.0.9` as of the July 7, 2026 refresh), so project-level `packages.lock.json` files must be refreshed when SDK-carried packages such as `Microsoft.NET.ILLink.Tasks` move with that lane. A locked restore failure here is a release-readiness signal, not a CI-cache defect.

GitHub Actions now adds a dedicated `.NET 11` readiness job that:

- installs the current `11.0.x` SDK channel
- runs the readiness script from a temporary working directory so the repo root `global.json` does not force SDK 10 selection
- applies a negative test filter for SDK-preview package/template `dotnet pack` and install roundtrips (`TemplatePackTests`, `PackagePublishingTests`, and the CLI package-staging roundtrip) while the normal `net10.0` release-validation lane remains the package-shipping authority
- uploads a dedicated `dotnet-readiness-sdk11` artifact

The result is deliberate:

- normal release validation keeps proving the stable shipping floor
- the dedicated readiness job proves the future SDK path without pretending the repo has already migrated

## Claim policy for trimming, Native AOT, and single-file

Cephalon does **not** currently treat trim, Native AOT, or single-file support as shipped repo truth.

For those claims to become real support statements, Cephalon must update all of the following together:

- `scripts/deployment-mode-support.json`
- [Deployment-mode support](deployment-mode-support.md)
- project properties that actually enable or declare the deployment mode
- `scripts/validate-dotnet-readiness.ps1`
- `publishProbePolicy`, `PublishProbeGate`, and release-validation deployment-mode selection
- release-validation workflow coverage
- compatibility docs
- package-publishing docs
- project memory
- backlog and roadmap tracking

Analyzer-only settings are useful readiness signals, but they are not support claims by themselves.

Package-scoped claims follow the same rule at a narrower boundary: the package must be listed in `deploymentModeEligibility.packages`, the scoped `supportedModes` entry must match explicit project properties, and the validation harness must report the package claim separately from global support posture. The current scoped `singleFile` set is `Cephalon.Diagnostics`, `Cephalon.Abstractions`, and `Cephalon.Scaffolding`; that set still does not change the global trim, Native AOT, or single-file support rows.

The machine-readable contract exists so future support claims cannot drift away from what the readiness report and the human-facing docs say.

## What this slice intentionally does not do

- it does not move Cephalon's default target framework from `net10.0` to `net11.0`
- it does not retarget the template-pack starters
- it does not claim trim, Native AOT, or single-file compatibility yet
- it does not treat `.NET 11` preview builds as a production dependency baseline

## Next likely follow-through

After this Preview 5 readiness refresh, the next framework-focused work should be deliberate and explicit:

- analyzer drift review as Preview 6, RCs, and GA arrive
- package-surface review for Microsoft and ecosystem package compatibility under `.NET 11`
- truthful deployment-mode validation before any trim / AOT / single-file claims are added on top of the shipped support-contract manifest
- the shipped `scripts/validate-deployment-mode-claims.ps1` harness is the machine-checkable proof gate for trim/AOT/single-file support claims; until it reports `claim-truthful` for an intentionally promoted support set, the global support contract stays `not-claimed`. Its emitted report now includes `PublishProbePolicy`, `PublishProbeGate`, and a lock-file-backed transitive-hazard audit subset; the current gate is release-blocking for representative `singleFile` publish only and does not promote support by itself. See [Deployment-mode support](deployment-mode-support.md) for the harness scope and emitted inventory artifact.
- an eventual baseline-migration lane once `.NET 11` is stable enough for Cephalon's package, tooling, and template defaults
