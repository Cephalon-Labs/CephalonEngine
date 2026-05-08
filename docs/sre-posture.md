# Cephalon engine SRE posture

This document is the engine-level Site Reliability Engineering (SRE) posture for the Cephalon engine itself. It is *not* a prescription for consumer applications that adopt Cephalon — those teams own their own SLOs against their own user journeys. Instead, this page declares the reliability semantics the engine commits to as a framework: cold-start time, dispatch latency, allocation discipline, build/restore/release-validation wall time, and test-suite flake rate.

Cross-references: [`engineering-standards.md`](engineering-standards.md), [`benchmarking.md`](benchmarking.md), [`runtime-failure-policy.md`](runtime-failure-policy.md), [`operational-hardening-gap-inventory.md`](operational-hardening-gap-inventory.md), [`test-coverage-roadmap.md`](test-coverage-roadmap.md), [`engine-completion-scorecard.md`](engine-completion-scorecard.md), [`../scripts/sre-posture-support.json`](../scripts/sre-posture-support.json), [`../scripts/sre-stable-baselines.json`](../scripts/sre-stable-baselines.json), [`project-memory.md`](project-memory.md), [`planning-governance.md`](planning-governance.md).

## Why the engine itself has SLOs

Cephalon ships as a reusable .NET engine. Consumer teams take a binary dependency on it and inherit its operational characteristics. That makes the engine's own reliability semantics part of the public contract, not an internal concern:

- a regression in `BehaviorDispatcher` p99 latency degrades every consumer that routes requests through behaviors
- a regression in cold-start time degrades every serverless and AOT scenario that depends on Cephalon
- a regression in `dotnet restore` wall time, in `scripts/validate-release.ps1` wall time, or in test-suite flake rate slows down every contributor and every consumer pipeline that builds against the engine

Treating these as SLIs (Service Level Indicators), declaring SLOs (Service Level Objectives) over them, and applying an error-budget policy when the SLOs burn is how the engine stays trustworthy across `.NET 10` LTS, the `.NET 11` readiness lane, and the durable `.NET 12+` migration lanes that follow.

## SLI catalogue (initial draft)

These are the SLIs the engine commits to measure. SLO targets are listed below; refine them after the next benchmark guardrail run produces a clean baseline.

### Hot-path latency SLIs

- **`engine.behavior.dispatch.latency.p95`** — wall-clock time from `BehaviorDispatcher` invocation to behavior return, measured from `BenchmarkInProcessShortRunConfig` runs over representative behaviors
- **`engine.behavior.dispatch.latency.p99`** — same instrumentation, p99
- **`engine.aspnetcore.minimal-api.cold-start.p95`** — wall-clock time from `WebApplication.Build()` to first request handled, measured from a representative Cephalon-on-ASP.NET-Core sample
- **`engine.worker.cold-start.p95`** — wall-clock time from `WorkerHostApplicationBuilder.Build()` to first hosted-service `StartAsync` return, measured from a representative `Cephalon.Worker` sample

### Allocation discipline SLIs

- **`engine.behavior.dispatch.alloc.bytes-per-op`** — `MemoryDiagnoser` allocation per dispatched behavior, measured from BenchmarkDotNet runs over representative behaviors
- **`engine.aspnetcore.request.alloc.bytes-per-op`** — allocation per request through the Cephalon ASP.NET Core mapping layer (excluding consumer-owned business logic)

### Build / release-validation wall-time SLIs

- **`engine.dotnet.restore.wall-time.lock-mode`** — wall-clock time of `dotnet restore` with `RestoreLockedMode=true` from a clean `.dotnet/` cache
- **`engine.validate-release.wall-time`** — wall-clock time of `scripts/validate-release.ps1` end-to-end, including build, tests, benchmarks, reference-doc generation, deployment-mode claim audit, and `.NET 11` readiness check
- **`engine.reference-docs.wall-time`** — wall-clock time of `Cephalon.ReferenceDocs` generation across the shipped public-package surface

### Test-suite flake SLI

- **`engine.tests.flake-rate.7d`** — fraction of CI runs in the last 7 days where a `tests/Cephalon.Tests.*` project failed and a re-run on the same commit passed; computed from CI metadata, not from inferring transient infrastructure failures

### Deployment-mode claim truth SLI

- **`engine.deployment-mode-claims.truthful-fraction`** — fraction of declared deployment-mode support claims (`net10.0` published smoke, Windows Service, IIS, Azure App Service, container image, Azure Container Apps, Kubernetes, Linux `systemd`) for which `scripts/validate-deployment-mode-claims.ps1` returns a `claim-truthful` verdict. The representative `singleFile` publish probe is now a release-blocking `PublishProbeGate`, but global trim / Native AOT / single-file support rows remain `not-claimed`; while no deployment mode is intentionally claimed globally, this SLI reports nonnumeric support-claim posture rather than counting the gated publish probe as a support claim

## SLO targets (initial draft)

These are starting targets, not load-bearing budgets. They will be revised after the next benchmark guardrail run produces a clean baseline and after the deployment-mode support-claim lane moves from explicit `not-claimed` rows to real `claim-truthful` support statements.

| SLI | Initial SLO target | Window |
| --- | --- | --- |
| `engine.behavior.dispatch.latency.p95` | ≤ 50 µs over the representative behavior set | 30-day rolling against shipped `main` |
| `engine.behavior.dispatch.latency.p99` | ≤ 200 µs over the representative behavior set | 30-day rolling against shipped `main` |
| `engine.aspnetcore.minimal-api.cold-start.p95` | ≤ 800 ms on the JIT path; AOT path tracked separately once `claim-truthful` | 30-day rolling against shipped `main` |
| `engine.worker.cold-start.p95` | ≤ 500 ms on the JIT path | 30-day rolling against shipped `main` |
| `engine.behavior.dispatch.alloc.bytes-per-op` | no regression beyond the current `BenchmarkInProcessShortRunConfig` baseline; tighten to a hard cap once the baseline is stable | per-PR comparison |
| `engine.aspnetcore.request.alloc.bytes-per-op` | no regression beyond the current baseline | per-PR comparison |
| `engine.dotnet.restore.wall-time.lock-mode` | ≤ 90 s on the canonical reference-build agent | 30-day rolling |
| `engine.validate-release.wall-time` | ≤ 25 minutes on the canonical reference-build agent | 30-day rolling |
| `engine.reference-docs.wall-time` | ≤ 5 minutes on the canonical reference-build agent | 30-day rolling |
| `engine.tests.flake-rate.7d` | ≤ 0.5% | 7-day rolling |
| `engine.deployment-mode-claims.truthful-fraction` | 100% of declared claims are `claim-truthful`; `not-claimed` rows never count against this SLI | per release |

The SLO targets above are still engine-owned orientation, not consumer-application SLOs. `scripts/sre-posture-support.json` now separates stable-baseline posture from guardrail coverage: an SLI may be `guardrail-catalog-mapped` because it points at an existing entry in `benchmarks/Cephalon.Benchmarks/guardrails/performance-guardrails.json`, and it becomes `stable-baseline-published` only when `scripts/sre-stable-baselines.json` carries matching evidence. The current stable-baseline subset is deliberately narrow: the three BehaviorDispatcher latency/allocation SLIs and the ASP.NET Core request-allocation SLI are published from the May 8, 2026 focused BenchmarkDotNet run, and `engine.deployment-mode-claims.truthful-fraction` is published from the release-validation deployment-mode claims report. The ASP.NET Core and Worker cold-start SLIs, restore/release/reference-docs wall-time SLIs, and test flake-rate SLI remain `pending-stable-baseline`.

## Error-budget policy

Cephalon's error-budget policy is intentionally simple, because the engine has a single shipped branch and a small set of canonical SLIs:

- **Burn-rate signal.** When an SLI is burning faster than the published target, open a follow-through row in the current `architecture-review-YYYY-MM-followups.md` tracker so the burn becomes visible alongside the rest of the monthly review state. The follow-through row should name the SLI, the observed burn-rate, and the package family it most directly affects.
- **Freeze threshold.** When a hot-path latency or allocation SLI burns more than 25% of its monthly budget within a 7-day window, freeze new feature merges on the affected package family until the budget recovers. Bug fixes, security fixes, and reliability fixes are explicitly allowed during a freeze; they are the reason the freeze exists.
- **Recovery proof.** A frozen family un-freezes when the SLI returns inside its target for a contiguous 7-day window and a benchmark or validation pass demonstrates the recovery on `main`. The recovery is recorded in the same follow-through tracker row that opened the burn.
- **Standing exemptions.** Cold-start, restore wall-time, and reference-docs wall-time SLIs do not trigger a freeze on their own; they trigger an investigation row instead. The intent is to keep the framework moving forward when these SLIs burn for tooling reasons (Aspire upgrades, NuGet feed health, GitHub Actions runner availability) rather than engine regressions.
- **Test flake budget.** When `engine.tests.flake-rate.7d` exceeds the target, the affected test project enters a quarantine queue (the failing test is `[Skip]`-attributed with a tracking comment within 24 hours, and either fixed or deleted within 7 days). The quarantine row is tracked in `docs/test-coverage-roadmap.md` and in the monthly follow-through tracker.

## Instrumentation alignment

The engine's first-class telemetry contract stays `System.Diagnostics.ActivitySource`, `System.Diagnostics.Metrics.Meter`, and `Microsoft.Extensions.Logging.ILogger`. SLI emission must follow that contract:

- latency SLIs are emitted as histograms on a stable `Meter` named per the per-package diagnostic-id range
- allocation SLIs are computed from `MemoryDiagnoser` benchmark output rather than runtime instrumentation, so they do not pay an instrumentation cost on the hot path
- attribute names on emitted spans / metrics / logs follow [OpenTelemetry semantic conventions](https://opentelemetry.io/docs/specs/semconv/) and stay decoupled from the OTel instrumentation-stability proposal so the engine can evolve attribute names additively as semconv stabilizes
- attribute cardinality is part of the contract; SLI emission must avoid unbounded high-cardinality dimensions (full request URLs, raw user input, full SQL strings, full tenant identifiers)

OTLP export configuration is the responsibility of host adapters and observability companion packs, not the engine core. SLI emission is exporter-agnostic; the same telemetry can fan out to the Aspire dashboard, Grafana, Datadog, Honeycomb, or a future agentic observer without engine changes.

## Engineer-facing SLI surfaces

The SLI catalogue above is meant to be reachable from three operator-facing surfaces:

- **`cephalon doctor`** — surfaces the latest local benchmark and validation wall-time signals so a contributor can see SLI health on the local box before pushing
- **`/engine/snapshot`** — projects relevant runtime SLI metadata (counters, latest dispatch outcomes, latest cold-start posture) so a deployed Cephalon instance can answer "is the engine itself behaving" without log archaeology
- **`scripts/validate-release.ps1`** output — publishes the engine-completion scorecard artifact and prints the `SrePostureEvidence` target-declared, pending-baseline, stable-baseline, stable-baseline manifest, and guardrail-coverage counts so release reviewers can see which benchmark SLIs have stable evidence and which rows remain pending
- **`scripts/sre-posture-support.json`** — keeps the SLI target list, source-document links, release-validation summary mode, baseline-status posture, guardrail-coverage status, stable-baseline manifest reference, and guardrail references machine-readable for the scorecard
- **`scripts/sre-stable-baselines.json`** — records the benchmark-backed stable-baseline rows for the four promoted benchmark SLIs, the release-validation claims-report baseline for the deployment-mode claim-truth SLI, the captured commit/time/tooling metadata, and the six SLI ids that intentionally remain pending

The intent is that SLI signal flows through the existing engine introspection surface rather than through a parallel dashboard. Consumer SRE teams can compose engine SLI signal into their own SLO targets through OTLP export, without the engine forcing a dashboard topology on them.

## Out of scope for this posture

This posture is deliberately narrow. The following are *not* covered here and are owned elsewhere:

- consumer-application SLOs (owned by the consumer team's SRE / on-call rotation against their own user journeys)
- specific provider/companion-pack SLOs (each companion pack with `M2`-or-higher claims may declare its own SLI/SLO over its execution path; those declarations live in the matching `docs/components/*.md` page, not here)
- distributed-system SLOs across multiple Cephalon instances (cell health isolation, multi-region availability, and edge-runtime traffic posture remain owned by `docs/architecture.md`, the cell-based architecture surface, and the relevant deployment guides)
- security incident response and vulnerability handling cadence (owned by `docs/engineering-standards.md` security/supply-chain section and, eventually, by the EU CRA Article 13 vulnerability-handling response flow)

When a future slice promotes a companion pack from `M2` into a multi-region or distributed-execution `M3`, that pack should declare its own SLI/SLO additively in its component doc and cross-reference back here.

## Refresh cadence

Refresh this document in place when:

- a new SLI is introduced through a benchmark, validation harness, or runtime-introspection surface
- an existing SLO target is tightened or relaxed after a baseline pass produces new evidence
- an SLI's `baselineStatus` or the `scripts/sre-stable-baselines.json` manifest changes
- an SLI's `guardrailCoverageStatus` or `guardrailReferences` changes in `scripts/sre-posture-support.json`
- the freeze threshold or recovery-proof rule changes (record the change as an in-place edit; the durable record of the change lives in commits, planning cards, and the matching architecture-review snapshot)
- a related framework reference (OpenTelemetry semantic conventions, Google SRE Workbook, the EU regulatory framework) publishes a new revision that materially affects the alignment

Do not append a dated change log inside this document. Long-range standards rarely benefit from change history living inside the standards page; the durable history belongs in commits, planning cards, and architecture-review snapshots.
