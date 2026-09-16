# Host and deployment compatibility evidence

September 16, 2026. Tracking: [ENG-744 / #1437](https://github.com/Cephalon-Labs/CephalonEngine/issues/1437), [Phase 15](https://github.com/Cephalon-Labs/CephalonEngine/milestone/16), [Project 2](https://github.com/orgs/Cephalon-Labs/projects/2).

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

The [Host Compatibility workflow](../.github/workflows/host-compatibility.yml) runs both scripts on Windows/Linux and uploads JSON, Markdown and logs. Deployment validation retains all five representative publish targets. Global trim, Native AOT and single-file support remains `not-claimed`; the existing package-scoped single-file claims remain limited to Abstractions, Diagnostics and Scaffolding. See [deployment-mode support](deployment-mode-support.md).

## Validation infrastructure repair

A local test wrote all 109 packages and its PowerShell process exited, but `PackagePublishingTests.RunProcess` remained blocked in `TaskAwaiter.GetResult`. The exit timeout did not cover stream draining. The internal `ToolingProcess` helper now bounds process exit and both output streams together, retains partial diagnostics, limits cleanup to two seconds, and disables persistent build workers only in child environments. An orphan retaining a pipe causes an explicit timeout; the helper does not promise to kill descendants of an already-exited root. Test-owned orphan fixtures clean themselves up.

Four real-process regressions cover concurrent pipe pressure, nonzero exits, live-process timeout and inherited pipes after root exit. The normal budget remains ten minutes. A second stack sample found repeated Markdown anchor parsing in generated-reference validation; that audit now uses a per-run anchor cache while retaining every link/fragment assertion. No public runtime API or documentation assembly is added.

Microsoft documents [redirected-stream deadlock risks](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standardoutput?view=net-10.0) and [MSBuild server controls](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-server). These repairs address observed validation infrastructure, not an engine runtime failure.

## Planning and evidence

The initial 4 h estimate omitted the reproduced process hang and reference-audit work. ENG-744 is revised to **12 h**, adding **8 h** of engineering scope. ENG-742 rolls up **20 h**, ENG-731 **40 h**; parents are not additive. Remaining September leaf scope becomes **476 h**, plus ENG-532 **1 h** = **477 h** until acceptance closes. Revised Phase 15 scope is **216 h**, with **188 h** remaining plus ENG-532. Estimates are not elapsed time or delivery dates.

Local Tooling passed **383/383** tests in 3.51 minutes, including all package tests and the four process regressions. The generated-reference link audit passed in 0.50 seconds. Pester passed **263/263**, including thirteen runtime payload guards. The external generated host passed on Windows with SDK 10.0.401, manifest 2.0, two modules, eight capabilities and successful runtime startup. This local run used a dirty tree based on `56187325`; it is diagnostic evidence. Deployment receipts and corrected committed-source CI are being collected. ENG-744 remains open until its declared matrix is demonstrated; cancelled or running Release Validation jobs are not passing full-release evidence. Release CI now has explicit 60-minute shipping and 20-minute SDK-readiness limits; the dedicated host/deployment job has a 35-minute limit.

## Full release observation failures

Committed-source [Host Compatibility 35093289578](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/35093289578) passed both jobs on `5f99ee15bd64e7fb79a6bb197b5260c0999ea4d1`. Both receipts report a clean tree, SDK 10.0.401, manifest 2.0, two modules, eight capabilities, successful startup, fifteen assertions and three payload digests. Contract Compatibility also [passed both systems](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/35093289583).

| OS / RID | Generated REST host | Single-file publish gate | Global deployment claim |
| --- | --- | --- | --- |
| Ubuntu / linux-x64 | passed | 5 targets passed | not-claimed |
| Windows / win-x64 | passed | 5 targets passed | not-claimed |

Local Windows single-file validation also passed all five targets and every boundary/route/JSON audit. The maturity readback remains 107 packages (M0=1, M1=39, M2=51, M3=7, M4=9), zero drift; API readback remains 104 baselines, three packages with 397 pending additions and zero removals.

[Release Validation 35093289591](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/35093289591) on `5f99ee15` failed Linux hosting after 818 successes and Windows composition after 884 successes. Kubernetes cleanup was still `pending` after a fixed 1.4 s wait. The test now holds the cleanup source behind an explicit gate, verifies pending state, releases the gate, and waits for applied state on all three queried surfaces before checking the captured payloads.

The Oracle harness dequeues its single failure batch and reports Idle on the next empty read; the provider retry interval was one second despite a 600 s shared-loop interval. The failure-metadata fixture now uses a 600 s provider interval, preserving the first failure for the existing bounded assertion. Production retry behavior remains unchanged. These are test-fixture repairs, not live Kubernetes or Oracle integration evidence.

This adds **4 h** to ENG-744 (**16 h** total). Current parent rollups are ENG-742 **24 h** and ENG-731 **44 h**; revised September scope **548 h**; remaining **480 h + ENG-532 1 h = 481 h**. Phase 15 is **220 h**, with **192 h** remaining plus ENG-532. Earlier checkpoints above are historical. Full release acceptance remains open.

Repair validation: all four Kubernetes materializer hosting tests and all 885 composition tests passed locally. The eight planning tests passed with the revised estimate graph; GitHub guards matched 23 issues and Project items. The full hosting suite and renewed Release Validation supply the remaining acceptance evidence.
