# Cephalon engine SRE posture

September 27 collector continuation: ENG-750 is active with ENG-751 (3 h, collector correctness) and ENG-752 (13 h, remaining workload/statistical evidence). Parent estimates remain non-additive: ENG-750 16 h, ENG-746 24 h, ENG-729 50 h. September scope remains **566 h**; **18 unfinished leaves / 448 h**, plus ENG-532 **1 h** = **449 h**. Phase 15 remains 238 h total / 160 h remaining plus ENG-532. [Collector contract and evidence](ci-flake-collector.md) describe the implementation under validation; no stable SLO or maturity promotion.

Earlier September 27 acceptance: ENG-745 (8 h), ENG-747 (10 h) and ENG-748 (8 h) are complete on `201e92a8`. ENG-729 remains 50 h with 26 h delivered and ENG-746 retaining 24 h, non-additive. September scope is **566 h**; **17 unfinished leaves / 456 h**, plus ENG-532 **1 h** = **457 h**. Phase 15 is 238 h total / 168 h remaining plus ENG-532. [SRE evidence](sre-validation-2026-09.md). No stable SLO, maturity or support promotion; earlier checkpoints preserve history.

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

- **`engine.behavior.dispatch.latency.p95`** — desired p95 from `BehaviorDispatcher` invocation to behavior return; the current `BenchmarkInProcessShortRunConfig` export supplies a mean baseline proxy, not a measured p95
- **`engine.behavior.dispatch.latency.p99`** — desired p99 over the same boundary; the current mean proxy does not establish that percentile
- **`engine.aspnetcore.minimal-api.cold-start.p95`** — desired build-to-first-request p95 for the minimal API host with `Engine:AspNetCore:OperatorSurface:Mode=core`; the existing in-process benchmark includes construction, start, request, stop and disposal and reports a lifecycle mean proxy
- **`engine.worker.cold-start.p95`** — desired build-to-start-completion p95 for `Cephalon.Worker`; the existing in-process benchmark includes construction, start, stop and disposal and reports a lifecycle mean proxy

### Allocation discipline SLIs

- **`engine.behavior.dispatch.alloc.bytes-per-op`** — `MemoryDiagnoser` allocation per dispatched behavior, measured from BenchmarkDotNet runs over representative behaviors
- **`engine.aspnetcore.request.alloc.bytes-per-op`** — allocation per request through the Cephalon ASP.NET Core mapping layer (excluding consumer-owned business logic)

### Build / release-validation wall-time SLIs

- **`engine.dotnet.restore.wall-time.lock-mode`** — wall-clock time of `dotnet restore` with `RestoreLockedMode=true` using existing runner caches; the script does not clear caches, so cache-hit/miss evidence must be retained separately for cold-restore claims
- **`engine.validate-release.wall-time`** — wall-clock time of `scripts/validate-release.ps1` end-to-end, including build, tests, benchmarks, reference-doc generation, deployment-mode claim audit, and `.NET 11` readiness check
- **`engine.reference-docs.wall-time`** — wall-clock time of `Cephalon.ReferenceDocs` generation across the shipped public-package surface

### Test-suite flake SLI

- **`engine.tests.flake-rate.7d`** — intended fraction of equivalent CI test executions that fail and then pass on retry. The current collector is an observed run-level test-step recovery proxy; it cannot establish test identity or statistical confidence. [Collection, denominator and promotion contract](ci-flake-collector.md)

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
| `engine.validate-release.wall-time` | ≤ 30 minutes on the canonical reference-build agent | 30-day rolling |
| `engine.reference-docs.wall-time` | ≤ 5 minutes on the canonical reference-build agent | 30-day rolling |
| `engine.tests.flake-rate.7d` | ≤ 0.5% | 7-day rolling |
| `engine.deployment-mode-claims.truthful-fraction` | 100% of declared claims are `claim-truthful`; `not-claimed` rows never count against this SLI | per release |

The SLO targets above are still engine-owned orientation, not consumer-application SLOs. `scripts/sre-posture-support.json` now separates stable-baseline posture from guardrail coverage: an SLI may be `guardrail-catalog-mapped` because it points at an existing entry in `benchmarks/Cephalon.Benchmarks/guardrails/performance-guardrails.json`, and it becomes `stable-baseline-published` only when `scripts/sre-stable-baselines.json` carries matching evidence. The current stable-baseline subset is deliberately narrow: the three BehaviorDispatcher latency/allocation SLIs, the ASP.NET Core request-allocation SLI, `engine.aspnetcore.minimal-api.cold-start.p95`, and `engine.worker.cold-start.p95` are published from May 8, 2026 BenchmarkDotNet evidence. Both cold-start rows use `benchmark-mean-baseline-proxy` evidence because the current short-run export does not emit percentile columns; the ASP.NET Core row measures the opt-in core operator surface at about `46.949 ms` against the 800 ms SLO target, and the Worker row remains far below the 500 ms SLO target. `engine.deployment-mode-claims.truthful-fraction` is published from the release-validation deployment-mode claims report, and `engine.dotnet.restore.wall-time.lock-mode`, `engine.reference-docs.wall-time`, plus `engine.validate-release.wall-time` are published from release-validation timing evidence. The full validate-release target is now 30 minutes after the May 9, 2026 release run measured about 1,669.848 seconds with the current benchmark, package, reference-doc, scorecard, and public-API gates enabled. The original flake-rate checkpoint had no GitHub Actions history. September now has completed and failed runs, including the Windows timeout-test regression tracked by ENG-745; the flake-rate remains `pending-stable-baseline` until ENG-746 assesses the collector window and promotion conditions. Historical missing-history evidence must not be read as the current run count. `scripts/measure-ci-flake-rate.ps1` records `artifacts/sre-ci-flake-rate/ci-flake-rate.json` in the release-validation workflow, and its `-OutputPath` accepts either a report directory or an explicit `.json` report path for local release-manager reruns; the report now also records repository Actions permission readiness, target workflow active-state readiness, and `workflow_dispatch` readiness, so the pending row can distinguish "Actions/workflows are ready" from "completed run history is still missing". Schema 2.0.0 always sets `PromotionAllowed=false`; `ReadyForStatisticalReview` distinguishes a clean descriptive observation from the separate test-level and statistical evidence required before publishing a stable baseline. `-RequirePromotion` therefore writes evidence and fails. [Schema migration and evidence](ci-flake-collector.md). The September candidate and its failed/canceled-run denominator are recorded in the [SRE follow-up](sre-validation-2026-09.md#flake-rate-collector-readback-retained-for-eng-746).

## Error-budget policy

Cephalon's error-budget policy is intentionally simple, because the engine has a single shipped branch and a small set of canonical SLIs:

- **Burn-rate signal.** When an SLI is burning faster than the published target, open a follow-through row in the current `architecture-review-YYYY-MM-followups.md` tracker so the burn becomes visible alongside the rest of the monthly review state. The follow-through row should name the SLI, the observed burn-rate, and the package family it most directly affects.
- **Freeze threshold.** When a hot-path latency or allocation SLI burns more than 25% of its monthly budget within a 7-day window, freeze new feature merges on the affected package family until the budget recovers. Bug fixes, security fixes, and reliability fixes are explicitly allowed during a freeze; they are the reason the freeze exists.
- **Recovery proof.** A frozen family un-freezes when the SLI returns inside its target for a contiguous 7-day window and a benchmark or validation pass demonstrates the recovery on `main`. The recovery is recorded in the same follow-through tracker row that opened the burn.
- **Standing exemptions.** Cold-start, restore wall-time, reference-docs wall-time, and full release-validation wall-time SLIs do not trigger a freeze on their own; they trigger an investigation row instead. Release-validation timing reports therefore write `status = investigate`, `targetExceeded = true`, and the excess milliseconds when these wall-time targets burn. The intent is to keep the framework moving forward when these SLIs burn for tooling reasons (Aspire upgrades, NuGet feed health, GitHub Actions runner availability) rather than engine regressions.
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
- **`scripts/validate-release.ps1`** output — publishes the engine-completion scorecard artifact and prints the `SrePostureEvidence` target-declared, pending-baseline, stable-baseline, stable-baseline manifest, pending-baseline blocker, and guardrail-coverage counts so release reviewers can see which benchmark SLIs have stable evidence and why remaining rows are blocked
- **`scripts/measure-ci-flake-rate.ps1`** output — queries GitHub Actions run and attempt metadata, writes `artifacts/sre-ci-flake-rate/ci-flake-rate.json` by default, accepts either an output directory or explicit `.json` report path through `-OutputPath`, records repository Actions/workflow/dispatch readiness beside run-history availability, and reports collection/attribution blockers without automatically promoting the SLI; see the [schema 2.0.0 contract](ci-flake-collector.md)
- **`scripts/sre-posture-support.json`** — keeps the SLI target list, source-document links, release-validation summary mode, baseline-status posture, guardrail-coverage status, stable-baseline manifest reference, CI flake-rate collector, Actions/workflow readiness posture, and guardrail references machine-readable for the scorecard
- **`scripts/sre-stable-baselines.json`** — records the benchmark-backed stable-baseline rows for the four promoted hot-path benchmark SLIs, the ASP.NET Core core-operator-surface cold-start mean-baseline proxy, the Worker cold-start mean-baseline proxy, the release-validation claims-report baseline for the deployment-mode claim-truth SLI, the restore locked-mode, reference-docs, and full validate-release wall-time baselines, the captured commit/time/tooling metadata, and the remaining pending flake-rate SLI id with collector-backed Actions/workflow readiness plus missing-history blocker evidence

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

## September validation follow-up

[ENG-745 / ENG-746 / ENG-747 / ENG-748](sre-validation-2026-09.md) split ENG-729's revised 50 h into 8 h validation reliability, 24 h remaining SLO/load/telemetry work, 10 h provider CDC/bootstrap evidence and 8 h host-clock publication scheduling. ENG-745/747/748 are complete; ENG-746 and the parent remain open. [September follow-ups](architecture-review-2026-09-followups.md) record the 31 m 22 s Windows full-release overrun against 30 minutes, the later timeout-test failure, MongoDB startup deadline and MySQL observation race. Stable baselines and target thresholds remain unchanged pending repeatable proof.

[ENG-750 proof sequence](sre-evidence-2026-09-27.md#eng-750-proof-sequence-planned-not-executed) assigns the remaining 16 h across collector correctness, declared workloads, recovery, telemetry, repeatability and publication. These planned proofs are not existing percentile or production SLO claims.
