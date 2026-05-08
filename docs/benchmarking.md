# Cephalon Benchmarking

`Cephalon.Benchmarks` is the repository performance suite built on BenchmarkDotNet.

It currently tracks the following benchmark lanes across composition, runtime, ASP.NET Core hosting, REST publication, scaffolding, and hot-path engine services:

- `Cephalon.Benchmarks.Composition`: configured-builder engine composition and manifest construction
- `Cephalon.Benchmarks.Composition`: strict trust-policy composition and capability filtering
- `Cephalon.Benchmarks.Composition`: phase-8 configured-builder composition with the shipped data, eventing, identity, tenancy, audit, and `Sfid` companion-pack baseline
- `Cephalon.Benchmarks.Runtime`: prepared-runtime initialize/start/stop lifecycle overhead
- `Cephalon.Benchmarks.Runtime`: phase-8 prepared-runtime initialize/start/stop lifecycle overhead with the shipped low-ceremony companion-pack baseline
- `Cephalon.Benchmarks.Runtime`: ASP.NET Core request logging with bounded request/response body capture and trace correlation
- `Cephalon.Benchmarks.Runtime`: ASP.NET Core request logging while oversized textual payloads are truncated to the configured capture limits
- `Cephalon.Benchmarks.Runtime`: ASP.NET Core request logging under concurrent request pressure with the shipped logging and redaction pipeline enabled
- `Cephalon.Benchmarks.Runtime`: ASP.NET Core minimal API cold start with the opt-in core operator surface, plus generic-host Worker cold start
- `Cephalon.Benchmarks.Runtime`: engine-first REST projection, governance, and runtime-catalog materialization across generated/profile/DSL precedence, grouped generated module ownership, authoring policy, suppression, override, and preserved implicit query fallback
- `Cephalon.Benchmarks.Scaffolding`: blueprint-to-files scaffold generation
- `Cephalon.Benchmarks.Scaffolding`: phase-8 blueprint-to-files scaffold generation with structured `Engine:*` sections, additive pack hints, and starter-test conventions
- `Cephalon.Benchmarks.HotPath`: CDC execution-runtime catalog projections over Debezium-managed external runtimes, including repeated managed-connector operator filters against the shared versioned runtime snapshot

The benchmark suite now also ships a guardrail catalog at `benchmarks/Cephalon.Benchmarks/guardrails/performance-guardrails.json`.

That catalog is the repository baseline for the currently shipped benchmark methods:

- `EngineBuilderBenchmarks`: `BuildRuntimeManifest`, `BuildRuntimeManifestWithStrictTrustPolicy`, `BuildPhase8RuntimeManifest`
- `EngineRuntimeBenchmarks`: `InitializeStartStopRuntime`, `InitializeStartStopPhase8Runtime`
- `AspNetCoreRequestLoggingBenchmarks`: `HandleLoggedJsonRequest`, `HandleTruncatedJsonRequest`, `HandleConcurrentLoggedJsonRequest`
- `ColdStartBenchmarks`: `BuildStartHandleFirstRequestAspNetCore`, `BuildStartWorkerHost`
- `RestEndpointProjectionGovernanceBenchmarks`: `BuildMapGovernedRestCatalogs`
- `ScaffoldGeneratorBenchmarks`: `GenerateBlueprintScaffold`, `GeneratePhase8BlueprintScaffold`
- `DataDispatchBenchmarks`: `DispatchQuery`, `DispatchCommand`, `DispatchCommandWithResult`
- `BehaviorDispatchBenchmarks`: `DispatchBehavior`
- `AuthorizationEvaluationBenchmarks`: `EvaluateRbacAllow`, `EvaluateRbacDeny`
- `TenantResolutionBenchmarks`: `ResolveByTenantId`, `ResolveByHostName`, `ResolveDefaultTenant`
- `EventSourcingBenchmarks`: `AppendSingleEvent`, `ReadStream`, `GetStreamVersion`
- `OutboxStagingBenchmarks`: `StageOutboxMessage`
- `CdcExecutionRuntimeCatalogBenchmarks`: `EnumerateRuntimes`, `FilterManagedConnectorDriftState`, `FilterManagedConnectorDryRunState`, `FilterManagedConnectorCommandIssuanceState`, `FilterManagedConnectorOperatorSelectors`

The composition and runtime baselines prepare configured builders, runtimes, and service providers outside the measured loop so the guardrails track `Build()` and lifecycle transition costs rather than one-time benchmark harness setup.
That baseline now also includes the stricter trust-policy composition path, the shipped phase-8 low-ceremony companion-pack path, the bounded-truncation HTTP logging path, the engine-first REST projection/governance startup path, a concurrent logging throughput path, and the cold-start path for ASP.NET Core minimal API hosts that opt into `Engine:AspNetCore:OperatorSurface:Mode=core`, so security and startup hardening work stays measurable under both request and host-start pressure. The SRE manifest maps the ASP.NET Core request-allocation SLI to the three `AspNetCoreRequestLoggingBenchmarks` guardrail entries, so request logging allocation regressions are scorecard-visible.

The stable SRE baseline manifest lives at `scripts/sre-stable-baselines.json`. It records the May 8, 2026 BenchmarkDotNet evidence for the four hot-path SLI rows that have enough evidence to promote to `stable-baseline-published`: `engine.behavior.dispatch.latency.p95`, `engine.behavior.dispatch.latency.p99`, `engine.behavior.dispatch.alloc.bytes-per-op`, and `engine.aspnetcore.request.alloc.bytes-per-op`. The same manifest also records `engine.aspnetcore.minimal-api.cold-start.p95` from `Cephalon.Benchmarks.Runtime.ColdStartBenchmarks-report.csv` as a `benchmark-mean-baseline-proxy` over `BuildStartHandleFirstRequestAspNetCore`, because the benchmark's core operator surface measured about `46.949 ms` against the 800 ms SLO target while the current short-run export does not emit percentile columns. It also records `engine.worker.cold-start.p95` from the same report as a `benchmark-mean-baseline-proxy` over `BuildStartWorkerHost`, because the measured mean remains far below the 500 ms SLO target. The manifest also records the release-validation claims-report baseline for `engine.deployment-mode-claims.truthful-fraction` plus release-validation timing baselines for `engine.dotnet.restore.wall-time.lock-mode`, `engine.reference-docs.wall-time`, and `engine.validate-release.wall-time`; only the flake-rate SLI remains pending because GitHub Actions has no run history to compute a 7-day baseline, and it carries blocker evidence in the same manifest.
The hot-path data baseline now also includes the CDC execution-runtime catalog path that powers filter-heavy managed-connector operator surfaces, so the versioned snapshot and capture-ownership indexes remain benchmark-governed after the ENG-488 runtime fix.
The shipped local smoke suite now uses a shared in-process short-run BenchmarkDotNet config across every benchmark class so mirrored worktree artifacts such as repo-local `.build/*` copies do not break benchmark project resolution during `validate-release`.

## Run all benchmarks

```powershell
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks
```

## Run a focused benchmark

```powershell
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks -- --filter "*EngineBuilderBenchmarks*"
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks -- --filter "*EngineRuntimeBenchmarks*"
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks -- --filter "*AspNetCoreRequestLoggingBenchmarks*"
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks -- --filter "*RestEndpointProjectionGovernanceBenchmarks*"
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks -- --filter "*ScaffoldGeneratorBenchmarks*"
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks -- --filter "*DataDispatchBenchmarks*"
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks -- --filter "*BehaviorDispatchBenchmarks*"
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks -- --filter "*BehaviorDispatchBenchmarks*" "*AspNetCoreRequestLoggingBenchmarks*"
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks -- --filter "*AuthorizationEvaluationBenchmarks*"
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks -- --filter "*TenantResolutionBenchmarks*"
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks -- --filter "*EventSourcingBenchmarks*"
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks -- --filter "*OutboxStagingBenchmarks*"
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks -- --filter "*CdcExecutionRuntimeCatalogBenchmarks*"
```

The phase-8 composition, runtime, and scaffolding scenarios live in the same benchmark classes as the earlier baselines, so those filters cover both the original and phase-8 paths.

## Validate guardrails against the latest reports

Run the benchmarks first so `BenchmarkDotNet.Artifacts/results` is fresh, then validate:

```powershell
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks -- --validate-guardrails
```

Optional overrides:

```powershell
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks -- --validate-guardrails --results D:\custom\results --guardrails D:\custom\guardrails.json
```

## Run the release validation flow

For a repo-native validation pass that builds, runs the focused operational health/export convention suite, runs the focused phase-8 convention suite, runs the broader test suite, runs the benchmark smoke suite, validates guardrails, and publishes release reference docs:

```powershell
pwsh ./scripts/validate-release.ps1
```

Useful switches:

```powershell
pwsh ./scripts/validate-release.ps1 -SkipBuild
pwsh ./scripts/validate-release.ps1 -SkipTests
pwsh ./scripts/validate-release.ps1 -SkipOperationalConventions
pwsh ./scripts/validate-release.ps1 -SkipPhase8Conventions
pwsh ./scripts/validate-release.ps1 -SkipReferenceDocs
pwsh ./scripts/validate-release.ps1 -BenchmarkFilters "*EngineBuilderBenchmarks*" "*EngineRuntimeBenchmarks*" "*AspNetCoreRequestLoggingBenchmarks*" "*RestEndpointProjectionGovernanceBenchmarks*" "*ScaffoldGeneratorBenchmarks*" "*DataDispatchBenchmarks*" "*BehaviorDispatchBenchmarks*" "*AuthorizationEvaluationBenchmarks*" "*TenantResolutionBenchmarks*" "*EventSourcingBenchmarks*" "*OutboxStagingBenchmarks*" "*CdcExecutionRuntimeCatalogBenchmarks*"
```

Run only the focused health/export convention suite:

```powershell
pwsh ./scripts/validate-operational-conventions.ps1
```

Run only the focused phase-8 convention suite:

```powershell
pwsh ./scripts/validate-phase8-conventions.ps1
```

## CI validation

GitHub Actions runs the same script-led flow through `.github/workflows/release-validation.yml`.

That workflow:

- uses `windows-latest` for the full benchmark and guardrail path
- uses `ubuntu-latest` for the same repo-native validation entry point with `-SkipBenchmarks` until guardrails are made OS-neutral
- installs the SDK from `global.json`
- runs `pwsh ./scripts/validate-release.ps1`
- uploads Windows benchmark output plus per-OS reference-doc and package artifacts as workflow artifacts

Treat `scripts/validate-release.ps1` as the source of truth. If the local release-validation flow changes, keep the workflow aligned instead of duplicating logic in YAML.

## Benchmark rules

- use public engine APIs and realistic blueprint/configuration inputs
- keep module graphs non-trivial enough to exercise dependency ordering
- avoid benchmark-only shortcuts that skip the contracts shipped to users
- prefer adding a focused benchmark before large composition/runtime/scaffolding refactors
- update the guardrail catalog deliberately when benchmark scenarios change materially
- use the guardrails to catch regressions, not to chase machine-specific micro-noise
- keep HTTP-host hot paths covered when request logging, body capture, or trace-correlation behavior changes materially
- keep at least one end-to-end REST projection/governance startup path benchmarked when module-owned REST authoring, grouped generation, or runtime catalog truth changes materially
- keep at least one concurrent HTTP-host path benchmarked when request logging changes materially enough to affect shared-host throughput
- keep trust-policy and bounded-capture paths benchmarked when security-sensitive host behavior changes materially
- keep CDC execution-runtime catalog operator filters benchmarked when runtime snapshot invalidation, external-runtime report projection, or managed-connector drill-down behavior changes materially
