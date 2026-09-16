# September SRE validation follow-up

September 17, 2026. [ENG-745 / #1438](https://github.com/Cephalon-Labs/CephalonEngine/issues/1438) is the active 8 h validation-reliability slice inside [ENG-729 / #1422](https://github.com/Cephalon-Labs/CephalonEngine/issues/1422). [ENG-746 / #1439](https://github.com/Cephalon-Labs/CephalonEngine/issues/1439) retains 24 h of workload/SLO/resilience/telemetry evidence. The 32 h parent is non-additive; September implementation scope remains 548 h. No runtime API, production policy, package maturity or provider-ownership claim changes.

## Observed failures and boundaries

[Release Validation on `12f436ae`](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/35098689841) passed Linux shipping and SDK 11 readiness but failed one Windows composition test after 884 successes. `BehaviorDispatcherSkipsTimeoutWhenTransportSpecificOverrideDisablesDefaultTimeout` expected a timeout and instead completed its five-second simulated effect. Its real one-second timeout timer competed with the simulated delay on a loaded runner. Passing the preceding release checkpoint does not make this latest run green. The separate [host matrix](host-compatibility-2026-09.md) remains evidence for its named checkpoints and scope.

The preceding successful Windows release on `c4f8d288` took 1,881,965.5918 ms (31 m 22 s), exceeding the 30-minute wall-time target. Its 41 microbenchmark guardrails passed. These are separate measurements: a passed benchmark gate does not resolve the wall-time investigation or prove workload p95/p99 compliance.

Local stacks from the full hosting suite showed repeated OTLP flush waits during Showcase host disposal. The fixture inherited the sample's enabled logs/metrics/traces exporters at `localhost:4317`, without owning a collector. A baseline run of `ShowcaseSampleResolvesCanonicalShowcaseRoute` took 15 seconds. That sample profile is appropriate for its documented collector deployment; the HTTP/domain test fixture must own its external dependencies.

## Implementation

The four timeout/circuit/override composition tests use an explicitly advanced, one-shot `TimeProvider` through Polly's existing registry builder factory. The simulated effect signals entry and waits on a release gate or cancellation. Tests verify an armed timeout before advancing time, exact `TimeoutRejectedException`, disabled overrides with no armed timer, an unreleased effect surviving the default deadline, and ordinary caller cancellation after pipeline reuse. Circuit-open rejection must occur before the effect starts. Ten-second real-time watchdogs fail a broken test and clean up its token; they do not supply the expected timeout result. Production clocks and retry policies are unchanged.

The Showcase in-memory test profile disables only OTLP export of logs, metrics and traces. It retains engine diagnostic emission and normal host composition. A fixture guard asserts absence of the exporter SDK providers. `OpenTelemetryHostingTests` continues to exercise all three signals against its owned HTTP/protobuf capture server, alongside the existing disabled/default configuration guards. No exported-telemetry assertion is replaced with a catalog-only check.

These choices follow Polly's [timeout cancellation contract](https://www.pollydocs.org/strategies/timeout.html) and [testing guidance](https://www.pollydocs.org/advanced/testing.html), and preserve OpenTelemetry's [exporter boundary](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/README.md). The test clock covers this fixture's one-shot timers; it is not a general scheduler or a shipped engine service.

## Acceptance and retained work

Local Windows / SDK 10.0.401 validation passed 16 resilience tests in 320 ms and all 67 Showcase/OTLP tests in 23 s. In the saved TRX receipts, the same `ShowcaseSampleResolvesCanonicalShowcaseRoute` case took 15.7694063 s before and 0.2891534 s after fixture isolation. Three documentation-link/hub checks passed. These are local diagnostic measurements; full-suite and committed-source CI evidence is still required.

ENG-745 remains In Progress until focused regressions, full composition/hosting tests, documentation/planning checks and Windows/Linux release CI pass on committed source. Record before/after timings with runner and checkpoint; retain warnings and failed runs. Neither a local duration reduction nor a single passing CI run promotes a stable SLO or a seven-day flake-rate baseline.

ENG-746 stays open for repeatable workload/hardware measurements, fault/load recovery, backpressure/exhaustion/cancellation, telemetry cardinality/redaction/cost budgets, dashboard windows, and CI flake-rate assessment. [September follow-ups](architecture-review-2026-09-followups.md), [SRE posture](sre-posture.md), [backlog](engine-backlog.md) and [completion plan](framework-completion-plan.md) retain these gates. M0–M4 remains 1/39/51/7/9 across 107 packages, subject to the existing evidence-based promotion process.
