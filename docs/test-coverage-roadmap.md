# Test Coverage Roadmap

Maturity: `M2` planning surface — this document is the canonical home for the layered-test posture, the gap-definition criteria, the prioritized recommendation list, and the test-flake quarantine queue. Recommendations are referenced by stable number from `docs/engine-backlog.md` cards and from `docs/sre-posture.md` so test-coverage slices can cite "recommendation #N" without copying the full rationale into every backlog entry.

Cross-references: [`engineering-standards.md`](engineering-standards.md), [`sre-posture.md`](sre-posture.md), [`benchmarking.md`](benchmarking.md), [`runtime-failure-policy.md`](runtime-failure-policy.md), [`operational-hardening-gap-inventory.md`](operational-hardening-gap-inventory.md), [`engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md), [`conformance-matrix.md`](conformance-matrix.md), [`engine-backlog.md`](engine-backlog.md).

## Why this document exists

Cephalon now ships dozens of `M1`-or-higher packages whose runtime contracts are exercised by a deliberately small set of layered test projects rather than per-package mirrored test assemblies. That layering is intentional — it keeps the test surface honest about which seams are actually composable and which are integration-only — but it also means that "test coverage" cannot be measured by counting test files next to source files. A claim is covered when the layered tests prove the claim, not when a test assembly carries the same name as the source pack.

This roadmap exists so the framework can answer four questions consistently:

1. Which test project is the right home for a new test? (See *Layered test posture* below.)
2. When does a missing test become a coverage *gap* worth a backlog card? (See *Gap definition criteria* below.)
3. What are the next-priority gaps right now? (See *Prioritized recommendations* below.)
4. Which tests are flaking on `main` and what is the quarantine deadline? (See *Test-flake quarantine queue* below.)

## Layered test posture

The repository ships five test projects under `tests/` plus one benchmark project under `benchmarks/`. Each layer answers a different question:

| Project | Layer | What it proves |
| --- | --- | --- |
| [`tests/Cephalon.Tests.Composition`](../tests/Cephalon.Tests.Composition) | Composition | The engine builder, module registries, runtime catalogs, and source-generator literals compose correctly under the public DI seams without booting a host. |
| [`tests/Cephalon.Tests.Hosting`](../tests/Cephalon.Tests.Hosting) | Hosting integration | A real `WebApplicationBuilder` / `HostApplicationBuilder` boots and the engine's published HTTP / hosted-service / runtime-catalog surfaces match the documented contract. |
| [`tests/Cephalon.Tests.Tooling`](../tests/Cephalon.Tests.Tooling) | Tooling and contract | The shipped tooling (reference-docs generator, package-surface assertions, manifest validators, public-API delta helpers) produces deterministic output on the current `src/` tree. |
| [`tests/Cephalon.Tests.Scripts`](../tests/Cephalon.Tests.Scripts) | Scripts | The PowerShell / bash scripts under `scripts/` are testable units rather than opaque automation; behavior changes flow through real assertions. |
| [`tests/Cephalon.Tests.Support`](../tests/Cephalon.Tests.Support) | Shared support | Reusable test modules (e.g. `IdentityAuthorizationTestModule`, `IdentityDecisionMatrixTestModule`) consumed across the four execution layers above. Not an execution layer itself. |
| [`benchmarks/Cephalon.Benchmarks`](../benchmarks/Cephalon.Benchmarks) | Performance | Per-package guardrail benchmarks tracked by the SLI catalog in [`sre-posture.md`](sre-posture.md). |

**Standing rule.** New dedicated test projects are added only when one source pack ships enough independent execution surface to justify a separate harness. Layered framework-level tests are the default home for new coverage; mirroring `src/` 1:1 with per-package test assemblies is explicitly *not* the discipline. Promoting a private test module to `Cephalon.Tests.Support` is a deliberate slice that ships when a second consumer appears, not preemptively.

## Gap definition criteria

A test-coverage gap exists when **any** of the following is true:

- (a) **No layered test exercises any of a pack's surfaces.** A pack ships an `M1`-or-higher claim but no composition, hosting, or tooling test mentions it.
- (b) **Layered tests exercise only the composition seam without the runtime path.** The DI registration is asserted but the runtime behavior — what the runtime catalog publishes, what the HTTP route returns, what the hosted service does on tick — is not.
- (c) **One or more of the pack's `M2`-or-higher claims have no test that proves the runtime contract.** The package row in [`engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md) declares a guarantee that no test asserts.
- (d) **A regression-prone hot path is not covered by a guardrail benchmark.** The path is named in [`sre-posture.md`](sre-posture.md) but no `benchmarks/` row asserts the SLI.

When a gap maps to an `M2`-or-higher claim, treat closing it as a Compatibility / Auditability / Reliability quality advance and name the dimension explicitly in the planning card.

## Prioritized recommendations

Recommendations are stable-numbered. When a recommendation ships, the row is annotated with the closing slice rather than removed — the number stays a permanent reference for downstream backlog cards.

### #1 — `Cephalon.AspNetCore.Grpc` streaming + error-mode coverage (high priority, **pending**)

`Cephalon.AspNetCore.Grpc` is at `M2 cephalon-managed`; the transport adapter routes incoming gRPC RPC calls through the engine's behavior-dispatch seam, but the server-streaming, client-streaming, and bidirectional-streaming paths have no end-to-end coverage at the hosting layer, and the canonical gRPC `Status` mapping (NOT_FOUND, INVALID_ARGUMENT, INTERNAL, UNAUTHENTICATED, PERMISSION_DENIED, UNAVAILABLE, RESOURCE_EXHAUSTED, FAILED_PRECONDITION, ABORTED, DEADLINE_EXCEEDED) is exercised only on the happy path.

Quality dimension: **Reliability** (transport contract).

### #2 — `Cephalon.AspNetCore.JsonRpc` error-response coverage (high priority, **shipped through `ENG-405` / [PR #919](https://github.com/Cephalon-Labs/CephalonEngine/pull/919)**)

`Cephalon.AspNetCore.JsonRpc` JSON-RPC 2.0 error envelopes (`-32700` parse error, `-32600` invalid request, `-32601` method not found, `-32602` invalid params, `-32603` internal error) are exercised end-to-end at the hosting layer through `tests/Cephalon.Tests.Hosting/JsonRpcErrorResponseHostingTests.cs`. The internal `JsonRpcErrorModesTestModule` declared inside the test file demonstrates the canonical error-envelope handling pattern as a reference implementation.

### #3 — `MetadataDrivenAuthorizationEvaluator` decision-matrix coverage (high priority, **shipped through `ENG-403` / [PR #917](https://github.com/Cephalon-Labs/CephalonEngine/pull/917)**)

The default `IAuthorizationEvaluator` decision matrix — every metadata-rule branch (`requiredRoles` ANY / ALL / empty, subject / resource / context attribute prefixes, `requireOwner`, `requireTenantMatch`, the no-rules policy, the policy-mode fallback, and the canceled-token throw path) — is pinned through `tests/Cephalon.Tests.Composition/Composition/MetadataDrivenAuthorizationDecisionMatrixTests.cs`. The reusable `IdentityDecisionMatrixTestModule` lives in `Cephalon.Tests.Support`.

### #4 — `IAuditActorAccessor` and `DefaultAuditRecorder` ambient-fallback coverage (high priority, **shipped through `ENG-404` / [PR #918](https://github.com/Cephalon-Labs/CephalonEngine/pull/918)**)

The `Cephalon.Audit` actor / correlation / tenant / entry-id fallback chain is exercised through `tests/Cephalon.Tests.Composition/Composition/AuditActorAndRecorderFallbackTests.cs`. Coverage includes the explicit-actor-wins / accessor-wins / system-actor-fallback chain, the `Activity.Current.TraceId` correlation fallback, the `ITenantContextAccessor.Current` tenant fallback, the `IIdGenerator` entry-id fallback, the `OccurredAtUtc` default-vs-explicit branches, and the writer-exception propagation path.

### #5 — Provider-native CDC integration scenarios for `Cephalon.Data.SqlServer`, `Cephalon.Data.Postgres`, and `Cephalon.Data.MongoDB` (medium priority, **pending**)

The shared CDC runtime catalog and the provider-native pumps (`sqlserver-cdc-capture-pump`, `postgresql-logical-replication-capture-pump`, `mongodb-change-stream-capture-pump`) are covered at the composition layer by `Cephalon.Tests.Composition`, but the integration scenario — *real provider running, real change stream, real outbox staging, real acknowledgement, real checkpoint commit* — is not yet end-to-end exercised. Closing the gap requires a `tests/Cephalon.Tests.Integration` (or per-provider `tests/Cephalon.Tests.Integration.Postgres`, etc.) project that opts into Testcontainers, plus a clear discipline for when integration tests run on `main` vs. only on `release` branches.

Quality dimension: **Reliability + Compatibility** (provider contract).

### #6 — `Cephalon.AspNetCore.GraphQL` transport-mapping coverage (medium priority, **gated**)

`Cephalon.AspNetCore.GraphQL` ships `M1` with route-mapping only today. When the package widens beyond route mapping (schema generation, query execution, error mapping), recommendation #6 promotes from gated to active and the test shape mirrors recommendation #1 (gRPC) and recommendation #2 (JSON-RPC) for the GraphQL canonical error envelope.

### #7 — Direct unit coverage for the four pre-existing `DebeziumDataCdcPackTests` recursion failures (low priority, **gated on quarantine resolution**)

Four `DebeziumDataCdcPackTests` failures observed during full `Cephalon.Tests.Composition` runs are tracked under the *Pre-existing test-flake watch* section of [`docs/releases/v0.1.0-preview-notes.md`](releases/v0.1.0-preview-notes.md) and under the quarantine queue below. The recursion lives in `Cephalon.Data.Services.CdcCaptureExecutionRuntimeCatalog.Enrich`. Closing this gap means either fixing the recursion in source or skipping the failing tests with a tracking comment per the *Test flake budget* rule in [`sre-posture.md`](sre-posture.md).

Quality dimension: **Reliability** (existing flake).

### #8 — `Cephalon.Diagnostics.Redaction` contract surface and 6-emission-site integration coverage (high priority, **shipped through `ENG-362` / `ENG-365` / `ENG-366` / `ENG-374` / `ENG-376` plus the Agentics + Retrieval + Worker emission slices `ENG-401` / `ENG-402` / `ENG-412` / `ENG-413` / `ENG-418`**)

The redaction surface has 46+ tests across the contract, starter filters, orchestration, DI extension, and the six M1 emission-site integrations:

- contract types and starter filters: `tests/Cephalon.Tests.Composition/Diagnostics/Redaction/KeyMatchRedactionFilterTests.cs` (8), `RegexRedactionFilterTests.cs` (10) — `ENG-362`
- orchestration helper: `RedactionPipelineTests.cs` (8) — `ENG-363`
- DI registration extension: `RedactionServiceCollectionExtensionsTests.cs` (7) — `ENG-364`
- AspNetCore middleware integration: `HttpRequestResponseLoggingMiddlewareRedactionTests.cs` (2) — `ENG-365`
- engine runtime module-phase integration: `EngineRuntimeRedactionTests.cs` (2) — `ENG-366`
- Wolverine dispatch integration: `WolverineEventingPackTests.cs` redaction tests (2) — `ENG-374` / `ENG-376`
- Agentics tool-dispatch integration: `tests/Cephalon.Tests.Hosting/AgenticsToolDispatchActivityTests.cs` (2) — `ENG-401`
- Retrieval knowledge-indexing + knowledge-query integration: `tests/Cephalon.Tests.Hosting/RetrievalKnowledgeIndexActivityTests.cs` (4: indexer track + indexer replace + query track from `ENG-402`, plus query replace from `ENG-418`) — `ENG-402` / `ENG-418`
- Worker lifecycle integration: `tests/Cephalon.Tests.Composition/Diagnostics/Redaction/WorkerLifecycleRedactionTests.cs` (2) — `ENG-413`

All six M1 emission sites now have symmetric per-site integration coverage that proves both the tracking-filter route (every emitted attribute value flows through the consumer-registered `RedactionPipeline`) and the replacement-filter route (a redacted value reaches the activity tag, not just the filter input). `ENG-418` closed the asymmetric coverage gap that left `KnowledgeQueryEngine` without a replacement-filter assertion even though `KnowledgeIndexer`, `AgentToolDispatcher`, and `RuntimeHostedService` already shipped paired track + replace tests. The shared `Redact(activity, key, value)` helper pattern with lazy `IServiceProvider.GetService<RedactionPipeline>()` resolution is contract-typed across every site so adding a seventh emission site is now a straight repetition rather than an open design question.

Quality dimension: **Security + Auditability + Compliance + Reliability** (engine-boundary redaction is part of the supply-chain contract for any consumer pipeline that exports telemetry beyond the trust boundary).

### #9 — `Cephalon.Resilience` engine-managed runtime coverage (medium priority, **shipped through `ENG-390` / [PR #907](https://github.com/Cephalon-Labs/CephalonEngine/pull/907)**)

The 19 resilience tests under `tests/Cephalon.Tests.Composition/Behaviors/Resilience/` (e.g. `BehaviorResilienceTests`, plus the hosting-integration tests in `Cephalon.Tests.Hosting`: `BehaviorResilienceHostingTests`, `BehaviorHttpTransportResilienceHostingTests`, `BehaviorResilienceRestHostingTests`) verify that the dedicated `Cephalon.Resilience` package's policy resolver, circuit-breaker state registry, exception classifier, and execution-context keys still satisfy the resilience descriptor + Polly v8 pipeline contract after extraction from `Cephalon.Behaviors`. The 4-of-7 file move preserved git rename history at 92-99% similarity; the residual 3 files (`BehaviorResilienceExecutionMiddleware`, `BehaviorIdempotencyResolver`, `BehaviorResilienceRuntimeCatalog`) stayed in `Cephalon.Behaviors` because moving them would have widened internal surface or created circular project references — coverage of those continues to live in the existing behavior-dispatch tests.

Quality dimension: **Reliability + Availability + Data Integrity** (the resilience surface is the engine's first-class retry / circuit-breaker / bulkhead / timeout / rate-limit primitive over `Microsoft.Extensions.Resilience`).

## Test-flake quarantine queue

When `engine.tests.flake-rate.7d` exceeds the target, the affected test enters a quarantine queue per the *Test flake budget* rule in [`sre-posture.md`](sre-posture.md): `[Skip]`-attribute the failing test with a tracking comment within 24 hours, then either fix or delete within 7 days.

| Test | Project | First observed | Quarantine action | Deadline |
| --- | --- | --- | --- | --- |
| `DebeziumDataCdcPackTests` (4 failures) | `Cephalon.Tests.Composition` | Pre-`v0.1.0-preview` | Documented under `releases/v0.1.0-preview-notes.md` *Pre-existing test-flake watch*; root cause is `Cephalon.Data.Services.CdcCaptureExecutionRuntimeCatalog.Enrich` recursion | Resolve through recommendation #7 before the GA release that follows `v0.1.0-preview` |

The queue is empty otherwise.

## Maintenance discipline

- **Adding a recommendation.** Append a new numbered row at the next-highest integer; do *not* renumber existing rows. Recommendations stay stable references for downstream cards.
- **Closing a recommendation.** Annotate the row with the closing slice (`shipped through ENG-NNN / PR #NNNN`) but keep the row in place. The number remains a permanent anchor.
- **Adjusting priority.** A high-priority recommendation that ages out without shipping should be demoted (with the demotion reason captured in the row) rather than silently re-prioritized.
- **Adding a quarantine row.** Open the row the moment the test is `[Skip]`-attributed. The deadline column is mandatory.

## Cross-references

- [Engineering standards](engineering-standards.md) — code-quality gates, library / API design, packaging
- [SRE posture](sre-posture.md) — SLI catalog, burn-rate signals, freeze threshold, test-flake budget
- [Benchmarking](benchmarking.md) — guardrail benchmarks consumed by recommendation-#5-style hot-path coverage decisions
- [Engine surface maturity audit](engine-surface-maturity-audit.md) — `M0`–`M4` plus ownership truth that drives the *gap-definition* criterion (c)
- [Conformance matrix](conformance-matrix.md) — per-package contract truth that drives the *gap-definition* criterion (b)
- [Engine backlog](engine-backlog.md) — backlog cards reference recommendations from this document by stable number
