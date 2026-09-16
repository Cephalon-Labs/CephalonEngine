# Cross-version contract consumers

The contract compatibility probe supplies executable evidence for selected `Cephalon.Abstractions` surfaces. It complements API baseline analysis with real NuGet restore, compilation and execution of unchanged consumer binaries. It does not certify the entire engine or a published release.

`scripts/contract-compatibility-support.json` names the immutable pre-coordination checkpoint, consumer fixture, target framework, scope and exclusions. The initial baseline is repository commit `11488f13f28f68f4bb1bc062dbf931f0d4e91d0e`; there is no released-package baseline asserted by this probe. Both source checkpoints are packed with the current shipping SDK. Their temporary `0.1.0-compat.baseline` and `0.1.0-compat.candidate` package versions prevent same-version NuGet cache reuse and are never published by this workflow.

## Run the probe

Install the SDK specified in `global.json`. Create a disposable baseline checkout outside the candidate directory, then run from the candidate checkout:

```powershell
git worktree add --detach ../cephalon-compat-baseline 11488f13f28f68f4bb1bc062dbf931f0d4e91d0e
pwsh ./scripts/validate-contract-compatibility.ps1 -BaselineSourceRoot ../cephalon-compat-baseline
```

The source mode requires a clean baseline at the exact declared commit. It checks the SDK selected by MSBuild for both projects against the candidate CLI SDK before packing. Packing can regenerate SDK-dependent baseline locks; this is a source-checkpoint comparison under the candidate toolchain, not a reconstruction of historical release bits. Reuse already produced artifacts with the explicit package mode:

```powershell
pwsh ./scripts/validate-contract-compatibility.ps1 `
  -BaselinePackagePath /packages/Cephalon.Abstractions.0.1.0-compat.baseline.nupkg `
  -CandidatePackagePath /packages/Cephalon.Abstractions.0.1.0-compat.candidate.nupkg `
  -OutputPath artifacts/contract-compatibility
```

Package mode accepts trusted local artifacts with distinct versions and the declared `net10.0` assembly. It records their identity and checksums; caller-supplied artifacts do not automatically become certified release baselines. The candidate must contain the declared new API and the baseline must predate it for the negative rollback case to be meaningful.

## Evidence and boundaries

| Scenario | What must hold |
| --- | --- |
| Baseline control | The original consumer builds from the baseline NuGet package and executes module discovery, module descriptors and blueprint JSON. |
| Old binary, new contracts | Only the contract DLL is replaced; the consumer SHA-256 stays unchanged and its loaded contract hash must match the candidate. |
| Old reader, candidate blueprint JSON | The baseline binary and contracts read the document emitted by the candidate. This covers `AppBlueprint`, not the runtime snapshot schema. |
| Common source recompiled | The same source restores/builds against the candidate package and reads baseline blueprint JSON. |
| Common-surface rollback | That recompiled consumer runs with the baseline contracts without recompilation. |
| New API control | A consumer compiled with `ReconciliationRequest` executes successfully with the candidate. |
| New API rollback rejected | Replacing its contracts with the baseline must produce the exact expected missing-type boundary and exit code. An unrelated crash is a failure, not passing evidence. |

Each run creates a unique directory with isolated NuGet storage, explicit package-source mapping, bounded subprocess execution, logs, JSON receipts and a Markdown summary. Ambient repository MSBuild settings are excluded from the consumer. The report includes package/assembly/consumer SHA-256 values, source revision and dirty-tree status, SDK, runtime and OS. A dirty candidate is local diagnostic evidence only. Input failure, timeout, unexpected exit or hash mismatch leaves a failed receipt; passing one scenario cannot mark the whole run passed.

The [Contract Compatibility workflow](../.github/workflows/contract-compatibility.yml) runs the same script on Windows and Linux with read-only repository permissions, an immutable baseline checkout and uploaded receipts. Runtime code and public APIs are unchanged by the probe. M0–M4 and deployment support declarations remain unchanged.

This is a bounded consumer proof, not a complete API-diff service. All-package API validation, runtime snapshots, host configuration, generated apps, provider migrations and trim/Native AOT/single-file claims remain outside this fixture. Newly added APIs are explicitly **not** safe to downgrade to an older contract assembly. ENG-744 retains the remaining compatibility matrix; ENG-742 and ENG-731 remain open until their broader acceptance is met.

Microsoft distinguishes [source, binary and behavioral compatibility](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/breaking-changes). [Package validation](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/package-validation/overview) and [API compatibility tools](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/overview) remain appropriate for full-surface release baselines; these executable consumers add behavioral evidence for the declared subset.

Tracking: [ENG-743 / #1436](https://github.com/Cephalon-Labs/CephalonEngine/issues/1436), [ENG-744 / #1437](https://github.com/Cephalon-Labs/CephalonEngine/issues/1437), [ENG-742 / #1435](https://github.com/Cephalon-Labs/CephalonEngine/issues/1435), [Phase 15](https://github.com/Cephalon-Labs/CephalonEngine/milestone/16), and [Project 2](https://github.com/orgs/Cephalon-Labs/projects/2). Engineering estimates are 8 h and 4 h inside the existing 12 h parent, not additional effort to sum again.

## September 16, 2026 validation checkpoint

Local source and explicit-artifact runs passed all seven scenarios on Windows x64 with SDK `10.0.401` and runtime `10.0.12`. Both source projects reported MSBuild SDK `10.0.401`. The script suite passed 250 tests, including nine artifact/process guard tests. This checkpoint used the implementation working tree; Windows/Linux CI receipts on the committed implementation are required before ENG-743 closes.
