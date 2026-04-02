# Cephalon Benchmarking

`Cephalon.Benchmarks` is the repository performance suite built on BenchmarkDotNet.

It currently tracks seven hot paths:

- `Cephalon.Benchmarks.Composition`: configured-builder engine composition and manifest construction
- `Cephalon.Benchmarks.Composition`: strict trust-policy composition and capability filtering
- `Cephalon.Benchmarks.Runtime`: prepared-runtime initialize/start/stop lifecycle overhead
- `Cephalon.Benchmarks.Runtime`: ASP.NET Core request logging with bounded request/response body capture and trace correlation
- `Cephalon.Benchmarks.Runtime`: ASP.NET Core request logging while oversized textual payloads are truncated to the configured capture limits
- `Cephalon.Benchmarks.Runtime`: ASP.NET Core request logging under concurrent request pressure with the shipped logging and redaction pipeline enabled
- `Cephalon.Benchmarks.Scaffolding`: blueprint-to-files scaffold generation

The benchmark suite now also ships a guardrail catalog at `benchmarks/Cephalon.Benchmarks/guardrails/performance-guardrails.json`.

That catalog is the repository baseline for the current hot paths:

- `BuildRuntimeManifest`
- `BuildRuntimeManifestWithStrictTrustPolicy`
- `InitializeStartStopRuntime`
- `HandleLoggedJsonRequest`
- `HandleTruncatedJsonRequest`
- `HandleConcurrentLoggedJsonRequest`
- `GenerateBlueprintScaffold`

The composition and runtime baselines prepare configured builders, runtimes, and service providers outside the measured loop so the guardrails track `Build()` and lifecycle transition costs rather than one-time benchmark harness setup.
That baseline now also includes the stricter trust-policy composition path, the bounded-truncation HTTP logging path, and a concurrent logging throughput path so security hardening work stays measurable under both single-request and multi-request pressure.

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

For a repo-native validation pass that builds, runs the focused operational health/export convention suite, runs the broader test suite, runs the benchmark smoke suite, validates guardrails, and publishes release reference docs:

```powershell
.\scripts\validate-release.ps1
```

Useful switches:

```powershell
.\scripts\validate-release.ps1 -SkipBuild
.\scripts\validate-release.ps1 -SkipTests
.\scripts\validate-release.ps1 -SkipOperationalConventions
.\scripts\validate-release.ps1 -SkipReferenceDocs
.\scripts\validate-release.ps1 -BenchmarkFilters "*EngineBuilderBenchmarks*" "*EngineRuntimeBenchmarks*" "*AspNetCoreRequestLoggingBenchmarks*"
```

Run only the focused health/export convention suite:

```powershell
.\scripts\validate-operational-conventions.ps1
```

## CI validation

GitHub Actions runs the same flow through `.github/workflows/release-validation.yml`.

That workflow:

- uses `windows-latest`
- installs the SDK from `global.json`
- runs `.\scripts\validate-release.ps1`
- uploads `BenchmarkDotNet.Artifacts/results` as a workflow artifact
- uploads `artifacts/reference-docs-release` as a workflow artifact

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
