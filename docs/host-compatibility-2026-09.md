# Host and deployment compatibility evidence

September 16, 2026. Tracking: [ENG-744 / #1437](https://github.com/Cephalon-Labs/CephalonEngine/issues/1437), [Phase 15](https://github.com/Cephalon-Labs/CephalonEngine/milestone/16), [Project 2](https://github.com/orgs/Cephalon-Labs/projects/2).

Selected acceptance is complete for ENG-744 and parents ENG-742/731. Evidence is scoped to the checkpoints and OS/RIDs below; it does not certify every package or a published release.

## Scope and acceptance

This work extends the [cross-version contract consumers](contract-compatibility.md) with a generated application outside the repository and the existing deployment claim gate. Shipping remains `net10.0`; SDK 11 readiness stays separate. Package maturity and ownership declarations do not change.

The generated-app smoke publishes local packages, installs the CLI, runs doctor, generates a modular monolith, restores/builds it, and starts its built host DLL. Its disposable parent workspace uses the repository SDK pin; generated application files remain intact. Reports record commit, dirty-tree flag, repository and actual generated-host SDK, OS and RID. Dirty-tree runs are local diagnostic evidence; committed-source Windows/Linux CI supplies the reproducible checkpoint.

HTTP 200 alone is insufficient. The smoke verifies manifest schema `2.0`, engine version, module identities/versions and configured discovery assemblies, capability source-module mappings, and blueprint/pattern/transport/technology selections against `Configurations/AddEngine.AppModel.json`. The snapshot must agree with `/engine` and prove successful startup. JSON object property ordering and additional snapshot surfaces are accepted; manifest array ordering remains deterministic. Saved manifest, snapshot and configuration files have SHA-256 digests in the receipt.

Generated-app report schema `1.1.0` adds required `RuntimeContract` and `Toolchain` fields in `scripts/adoption-smoke-support.json`. Other adoption scenarios retain their own report versions.

The configuration comparison uses canonical engine identifiers, including `shared-foundation-pattern`; template input aliases are not serialized manifest IDs. A real external-app run exposed this distinction in the initial assertion fixture. Payload files are saved before assertions so failed receipts retain the data needed to diagnose drift.

## Reproduce

Use PowerShell 7.4 or later and the SDK in `global.json`. Run these serially with repository builds: the generated-app smoke temporarily restores against an isolated NuGet cache and restores repository assets during cleanup.

```powershell
pwsh ./scripts/validate-generated-app-adoption.ps1 -ReportPath artifacts/host-compatibility/generated-app.json
pwsh ./scripts/validate-deployment-mode-claims.ps1 -DeploymentMode singleFile -OutputPath artifacts/host-compatibility/deployment
```

The historical-reader check builds a standalone consumer against `Cephalon.Engine` at the same immutable source checkpoint used by the contract-consumer probe. Use a clean disposable baseline checkout; SDK-dependent baseline locks may be regenerated during the build.

```powershell
pwsh ./scripts/validate-snapshot-reader-compatibility.ps1 -BaselineSourceRoot ../cephalon-compat-baseline -GeneratedAppReportPath artifacts/host-compatibility/generated-app.json
```

It verifies the input receipt's revision, SDK and payload hashes, then reads manifest/snapshot v2 through the historical typed DTOs. The reader and loaded Engine assembly hashes must remain unchanged while reading the real payload and a payload with additive fields. An unsupported manifest version must produce the exact expected rejection. This supplies source-checkpoint wire evidence for the selected generated REST host, not all historical versions or released Engine packages. It does not test binary replacement of the full Engine assembly; that differs from reading JSON through historical contracts.

Local historical-reader validation passed all three scenarios against the clean Windows host receipt from `c4f8d288`, using a diagnostic working tree. The loaded historical Engine identifies `0.1.0-preview+11488f13f28f68f4bb1bc062dbf931f0d4e91d0e`; the producer identifies `0.1.0-preview+c4f8d2883d219e4224e04b02351a96840cfb79a4`. [CI on `4d7bfae9`](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/35096866388) passed all three scenarios on Windows/Linux with clean source and matching hashes.

The [Host Compatibility workflow](../.github/workflows/host-compatibility.yml) runs these scripts on Windows/Linux with separate baseline/candidate checkouts and uploads JSON, Markdown and logs. Deployment validation retains all five representative publish targets. Global trim, Native AOT and single-file support remains `not-claimed`; the existing package-scoped single-file claims remain limited to Abstractions, Diagnostics and Scaffolding. See [deployment-mode support](deployment-mode-support.md).

## Validation infrastructure repair

A local test wrote all 109 packages and its PowerShell process exited, but `PackagePublishingTests.RunProcess` remained blocked in `TaskAwaiter.GetResult`. The exit timeout did not cover stream draining. The internal `ToolingProcess` helper now bounds process exit and both output streams together, retains partial diagnostics, limits cleanup to two seconds, and disables persistent build workers only in child environments. An orphan retaining a pipe causes an explicit timeout; the helper does not promise to kill descendants of an already-exited root. Test-owned orphan fixtures clean themselves up.

Four real-process regressions cover concurrent pipe pressure, nonzero exits, live-process timeout and inherited pipes after root exit. The normal budget remains ten minutes. A second stack sample found repeated Markdown anchor parsing in generated-reference validation; that audit now uses a per-run anchor cache while retaining every link/fragment assertion. No public runtime API or documentation assembly is added.

Microsoft documents [redirected-stream deadlock risks](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standardoutput?view=net-10.0) and [MSBuild server controls](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-server). These repairs address observed validation infrastructure, not an engine runtime failure.

## Planning and evidence

ENG-744 is **16 h**: the original 4 h matrix, 8 h for process supervision/reference-audit repair, and 4 h for the lifecycle observation repairs below. ENG-742 rolls up **24 h**, ENG-731 **44 h**; parents are not additive. Revised September implementation scope is **548 h**. Remaining leaf scope is **464 h**, plus ENG-532 **1 h** = **465 h**. Phase 15 scope is **220 h**, with **176 h** remaining plus ENG-532. Estimates are not elapsed time or delivery dates.

Local Tooling passed **383/383** tests in 3.51 minutes, including all package tests and the four process regressions. The generated-reference link audit passed in 0.50 seconds. Pester passed **263/263**, including thirteen runtime payload guards. The external generated host passed on Windows with SDK 10.0.401, manifest 2.0, two modules, eight capabilities and successful runtime startup. That local run used a dirty tree based on `56187325`; committed-source receipts are recorded below. [Release Validation on `c4f8d288`](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/35095044308) passed both shipping jobs and SDK 11 readiness. Windows includes benchmark execution/guardrails; Linux uses the existing `-SkipBenchmarks` lane. Cancelled or earlier failed runs are not passing evidence. Release CI has 60-minute shipping and 20-minute SDK-readiness limits; the dedicated host/deployment job has a 35-minute limit.

## Committed-source evidence and repaired failures

Shipping CI passed all **2,087** core tests (885 composition, 819 hosting, 383 tooling) and **263** Pester tests per OS. Windows executed **41** benchmarks and all 41 guardrails passed. Its full release script took **1,881,965.5918 ms (31 m 22 s)** against the existing 30-minute SRE target, so timing evidence correctly remains `investigate` while the release gate passes. [ENG-729 / #1422](https://github.com/Cephalon-Labs/CephalonEngine/issues/1422) retains that timing investigation within its existing 32 h SLO-evidence scope; compatibility completion does not certify the wall-time objective.

Committed-source [Host Compatibility 35096866388](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/35096866388) passed both jobs on `4d7bfae949c24b1ba166324e4bec988525be4620`, including historical typed readers. Both receipts report a clean tree, SDK 10.0.401, manifest 2.0, two modules, eight capabilities, successful startup, fifteen assertions and three payload digests. Contract Compatibility also [passed both systems](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/35096870309).

| OS / RID | Generated REST host | Single-file publish gate | Global deployment claim |
| --- | --- | --- | --- |
| Ubuntu / linux-x64 | passed | 5 targets passed | not-claimed |
| Windows / win-x64 | passed | 5 targets passed | not-claimed |

Local Windows single-file validation also passed all five targets and every boundary/route/JSON audit. The maturity readback remains 107 packages (M0=1, M1=39, M2=51, M3=7, M4=9), zero drift; API readback remains 104 baselines, three packages with 397 pending additions and zero removals.

[Release Validation 35093289591](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/35093289591) on `5f99ee15` failed Linux hosting after 818 successes and Windows composition after 884 successes. Kubernetes cleanup was still `pending` after a fixed 1.4 s wait. The test now holds the cleanup source behind an explicit gate, verifies pending state, releases the gate, and waits for applied state on all three queried surfaces before checking the captured payloads.

The Oracle harness dequeues its single failure batch and reports Idle on the next empty read; the provider retry interval was one second despite a 600 s shared-loop interval. The failure-metadata fixture now uses a 600 s provider interval, preserving the first failure for the existing bounded assertion. Production retry behavior remains unchanged. These are test-fixture repairs, not live Kubernetes or Oracle integration evidence.

Repair validation: all 885 composition tests and all 819 hosting tests passed locally, including the four Kubernetes materializer tests. The full hosting run took 9 minutes locally; stack samples showed sample-host OpenTelemetry flush during disposal, and the run completed without failures. The eight planning tests passed with the revised estimate graph; before closeout, GitHub guards matched all 23 open issues and Project items. SDK 11 RC1 CI on `c4f8d288` passed 885 composition, 819 hosting and 374 selected tooling tests (2,078 total), all still targeting `net10.0`. Full shipping Release Validation passed on that same checkpoint. The later `4d7bfae9` changes only compatibility harnesses/workflow and documentation; `git diff c4f8d288 4d7bfae9 -- src Directory.Build.props Directory.Packages.props global.json` is empty. The named runs together close the declared matrix without widening its exclusions.
