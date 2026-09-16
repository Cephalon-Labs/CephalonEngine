# SDK and API compatibility repair — September 16, 2026

ENG-731 remains the 32 h compatibility parent. Native children ENG-739 (8 h), ENG-740 (8 h), ENG-741 (4 h), and ENG-742 (12 h) split that existing estimate without adding it twice. The first three are Sprint 16 repairs; the remaining consumer and deployment matrix stays open. Estimates are engineering hours, not measured time spent.

## Changes and rationale

The Windows/Linux release jobs at [run 35085576422](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/35085576422) failed locked restore: `latestFeature` selected a newer SDK whose implicit ILLink requirement was 10.0.12, while three locks retained 10.0.11. `global.json` now requires SDK 10.0.401 exactly and disables prereleases. Abstractions, Diagnostics and Scaffolding locks were regenerated with that SDK; other dependency versions and net10.0 targets are unchanged. The Abstractions lock additionally records its direct API analyzer. The separate .NET 11 readiness lane is still an assessment, not shipping support.

Install SDK 10.0.401 from the [official download catalog](https://dotnet.microsoft.com/en-us/download/dotnet/10.0). Microsoft documents [exact SDK selection for locked dependency graphs](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json). A missing pinned SDK must fail clearly instead of silently changing the graph. For this delivery a repository-local official Windows x64 SDK archive was SHA-512 verified against Microsoft release metadata; validation commands set PATH and DOTNET_ROOT to that installation. System-wide SDK registrations were not changed.

Abstractions now directly references `Microsoft.CodeAnalysis.PublicApiAnalyzers` with private assets. Its old analyzer-meta-package project reference emitted the meta-package assembly without loading the analyzers in that package's dependencies. The NuGet consumer adoption path remains separate. The exact exported-type test now includes the eight previously delivered coordination contracts; the old whitelist caused a second failure in the latest readiness run. Enabling enforcement exposed 118 undeclared and three stale diagnostics. Of these, three were paired static-signature transcription errors; the other **115** existing signatures are recorded in **Unshipped**, not silently promoted to a released baseline.

`ModuleDiscoveryRegistry.Register`, `GetDescriptors` and `TryGetDescriptors` were already static in their introducing commit [2f3b3661](https://github.com/Cephalon-Labs/CephalonEngine/commit/2f3b3661). Their shipped baseline lines omitted `static`; only those three baseline annotations are corrected. Runtime signatures and source implementations do not change. This source-history reconciliation does not substitute for external binary-consumer upgrade proof, which remains ENG-742.

The in-process event scheduling hosting test now waits for the publication catalog's terminal `succeeded` report. Its handler probe can complete before subscription and publication reporting; observing that probe alone caused the .NET 11 failure. A second case uses the public subscription middleware seam to hold completion after the handler returns, proves the catalog still says `accepted`, then releases the gate and verifies terminal state. Existing pending/accepted and completed introspection assertions remain.

## Validation record

Receipts are local ignored files under `artifacts/compatibility-2026-09/`; they are diagnostic evidence, not published release artifacts.

| Check | Result |
| --- | --- |
| Stable SDK / lock graph | SDK 10.0.401 / runtime 10.0.12; solution locked restore passed after reviewing exactly three changed lock files. |
| Abstractions analyzer | Fresh Release build passed with zero warnings/errors. A temporary untracked public type fails with RS0016; removing the injected probe restores a zero-warning/error build. The probe is outside source and is never packaged. |
| API baseline report | 104 package baselines, 3 pending packages, 397 additive entries, zero removal entries. The 115-entry increase records prior source, not new runtime APIs. |
| Documentation | 4 focused documentation contract tests passed; edited Markdown local-link target scan passed. |
| Script tests | 241 Pester tests passed across all 15 script suites in an isolated checkout of the delivery plus the corrected planning regression test; zero failures/skips. The earlier focused 36-test result is included in this suite. |
| Event scheduling / SDK readiness | Event hosting: 24 passed, including both scheduling cases. SDK readiness: 4 passed; package surface: 227 passed. These local tests target net10.0; the .NET 11 CI result remains separate. |
| GitHub planning | 22 open issues match 22 tracked ENG rows with zero warnings; all child estimates are included in the 32 h parent. Project validation passed for all 22 items and all five required fields. |
| Package maturity | 107 packages; M0=1 / M1=39 / M2=51 / M3=7 / M4=9; drift=0. No promotion. |

## Remaining release gates

ENG-742 owns complete Windows/Linux consumer upgrade/downgrade, old binary/snapshot readers, configuration/manifest compatibility, generated apps, RC assessment, and selected manifest-backed deployment claims. A local restore or green focused test is not a full release validation, provider proof, or blanket trim/Native AOT/single-file claim. ENG-532's external signed-release dry run remains independent. Durable coordination, authorization and family pilots also remain open under ENG-721–726.

Tracking: [parent #1424](https://github.com/Cephalon-Labs/CephalonEngine/issues/1424), [SDK #1432](https://github.com/Cephalon-Labs/CephalonEngine/issues/1432), [API #1433](https://github.com/Cephalon-Labs/CephalonEngine/issues/1433), [scheduling #1434](https://github.com/Cephalon-Labs/CephalonEngine/issues/1434), [matrix #1435](https://github.com/Cephalon-Labs/CephalonEngine/issues/1435), [Phase 15 milestone](https://github.com/Cephalon-Labs/CephalonEngine/milestone/16), [Project 2](https://github.com/orgs/Cephalon-Labs/projects/2). Commit and CI links are recorded in the issue closeout after push.

## Post-push script-gate closeout

[Run 35087164368](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/35087164368) reached 240/241 Pester tests on both Windows and Linux, then stopped at an obsolete planning assertion that still expected ENG-719/720 to be open. The parser was correct. The regression test now checks the six delivered September tasks, the original 536 h implementation estimate, the 32 h compatibility child/parent equality, and 18 unfinished leaf tasks totaling 476 h (477 h including ENG-532). All 241 script tests then passed in an isolated local checkout. The original artifact-heavy local run was stopped after the clean-checkout run completed; it is not counted as a passing run. This follow-up is validation/closeout within ENG-739/740's existing estimates, with no runtime or parser changes. A fresh CI run is required; the first run is not reported as green.
