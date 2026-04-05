# Cephalon Benchmarking

`Cephalon.Benchmarks` is the repository performance suite built on BenchmarkDotNet.

It currently tracks ten hot paths:

- `Cephalon.Benchmarks.Composition`: configured-builder engine composition and manifest construction
- `Cephalon.Benchmarks.Composition`: strict trust-policy composition and capability filtering
- `Cephalon.Benchmarks.Composition`: phase-8 configured-builder composition with the shipped data, eventing, identity, tenancy, audit, and `Sfid` companion-pack baseline
- `Cephalon.Benchmarks.Runtime`: prepared-runtime initialize/start/stop lifecycle overhead
- `Cephalon.Benchmarks.Runtime`: phase-8 prepared-runtime initialize/start/stop lifecycle overhead with the shipped low-ceremony companion-pack baseline
- `Cephalon.Benchmarks.Runtime`: ASP.NET Core request logging with bounded request/response body capture and trace correlation
- `Cephalon.Benchmarks.Runtime`: ASP.NET Core request logging while oversized textual payloads are truncated to the configured capture limits
- `Cephalon.Benchmarks.Runtime`: ASP.NET Core request logging under concurrent request pressure with the shipped logging and redaction pipeline enabled
- `Cephalon.Benchmarks.Scaffolding`: blueprint-to-files scaffold generation
- `Cephalon.Benchmarks.Scaffolding`: phase-8 blueprint-to-files scaffold generation with structured `Engine:*` sections, additive pack hints, and starter-test conventions

The benchmark suite now also ships a guardrail catalog at `benchmarks/Cephalon.Benchmarks/guardrails/performance-guardrails.json`.

That catalog is the repository baseline for the current hot paths:

- `BuildRuntimeManifest`
- `BuildRuntimeManifestWithStrictTrustPolicy`
- `BuildPhase8RuntimeManifest`
- `InitializeStartStopRuntime`
- `InitializeStartStopPhase8Runtime`
- `HandleLoggedJsonRequest`
- `HandleTruncatedJsonRequest`
- `HandleConcurrentLoggedJsonRequest`
- `GenerateBlueprintScaffold`
- `GeneratePhase8BlueprintScaffold`

The composition and runtime baselines prepare configured builders, runtimes, and service providers outside the measured loop so the guardrails track `Build()` and lifecycle transition costs rather than one-time benchmark harness setup.
That baseline now also includes the stricter trust-policy composition path, the shipped phase-8 low-ceremony companion-pack path, the bounded-truncation HTTP logging path, and a concurrent logging throughput path so security hardening work stays measurable under both single-request and multi-request pressure.

## Run all benchmarks

```powershell
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks
```

## Run a focused benchmark

```powershell
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks -- --filter "*EngineBuilderBenchmarks*"
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks -- --filter "*EngineRuntimeBenchmarks*"
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks -- --filter "*AspNetCoreRequestLoggingBenchmarks*"
dotnet run -c Release --project benchmarks/Cephalon.Benchmarks -- --filter "*ScaffoldGeneratorBenchmarks*"
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
pwsh ./scripts/validate-release.ps1 -BenchmarkFilters "*EngineBuilderBenchmarks*" "*EngineRuntimeBenchmarks*" "*AspNetCoreRequestLoggingBenchmarks*" "*ScaffoldGeneratorBenchmarks*"
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
- keep at least one concurrent HTTP-host path benchmarked when request logging changes materially enough to affect shared-host throughput
- keep trust-policy and bounded-capture paths benchmarked when security-sensitive host behavior changes materially
