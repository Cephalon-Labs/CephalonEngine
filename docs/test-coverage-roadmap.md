# Test Coverage Roadmap

Maturity: `M2` planning surface — this document is the canonical home for the layered-test posture, the gap-definition criteria, the prioritized recommendation list, and the test-flake quarantine queue. Recommendations are referenced by stable number from `docs/engine-backlog.md` cards and from `docs/sre-posture.md` so test-coverage slices can cite "recommendation #N" without copying the full rationale into every backlog entry. `scripts/publish-engine-completion-scorecard.ps1` reads this page into `TestCoverageEvidence`, and release validation plus `cephalon doctor --scorecard` fail closed when active recommendation gaps or open quarantine entries are present.

Cross-references: [`engineering-standards.md`](engineering-standards.md), [`sre-posture.md`](sre-posture.md), [`benchmarking.md`](benchmarking.md), [`runtime-failure-policy.md`](runtime-failure-policy.md), [`operational-hardening-gap-inventory.md`](operational-hardening-gap-inventory.md), [`engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md), [`conformance-matrix.md`](conformance-matrix.md), [`engine-completion-scorecard.md`](engine-completion-scorecard.md), [`engine-backlog.md`](engine-backlog.md).

## Why this document exists

Cephalon now ships dozens of `M1`-or-higher packages whose runtime contracts are exercised by a deliberately small set of layered test projects rather than per-package mirrored test assemblies. That layering is intentional — it keeps the test surface honest about which seams are actually composable and which are integration-only — but it also means that "test coverage" cannot be measured by counting test files next to source files. A claim is covered when the layered tests prove the claim, not when a test assembly carries the same name as the source pack.

This roadmap exists so the framework can answer four questions consistently:

1. Which test project is the right home for a new test? (See *Layered test posture* below.)
2. When does a missing test become a coverage *gap* worth a backlog card? (See *Gap definition criteria* below.)
3. What are the next-priority gaps right now? (See *Prioritized recommendations* below.)
4. Which tests are flaking on `main` and what is the quarantine deadline? (See *Test-flake quarantine queue* below.)

The generated scorecard treats this page as a read model source, not a replacement source of truth: layered project links must resolve, the four gap criteria must remain declared, recommendation counts must match the numbered sections, and the quarantine queue must be explicitly `empty` before release readback can pass.

## Layered test posture

The repository ships six .NET test projects under `tests/`, one Pester-based scripts suite under `tests/Cephalon.Tests.Scripts`, and one benchmark project under `benchmarks/`. Each layer answers a different question:

| Project | Layer | What it proves |
| --- | --- | --- |
| [`tests/Cephalon.Tests.Composition`](../tests/Cephalon.Tests.Composition) | Composition | The engine builder, module registries, runtime catalogs, and source-generator literals compose correctly under the public DI seams without booting a host. |
| [`tests/Cephalon.Tests.Hosting`](../tests/Cephalon.Tests.Hosting) | Hosting integration | A real `WebApplicationBuilder` / `HostApplicationBuilder` boots and the engine's published HTTP / hosted-service / runtime-catalog surfaces match the documented contract. |
| [`tests/Cephalon.Tests.Tooling`](../tests/Cephalon.Tests.Tooling) | Tooling and contract | The shipped tooling (reference-docs generator, package-surface assertions, manifest validators, public-API delta helpers) produces deterministic output on the current `src/` tree. |
| [`tests/Cephalon.Tests.CdcIntegration`](../tests/Cephalon.Tests.CdcIntegration) | CDC provider integration | Provider-native CDC paths run against real data-system runtimes while the default lane remains deterministic through explicit external-service gates. |
| [`tests/Cephalon.Tests.ProviderIntegration`](../tests/Cephalon.Tests.ProviderIntegration) | Provider integration | Provider-backed data, eventing, event-sourcing, and companion-pack surfaces that need a real infrastructure runtime. Disposable runtimes that the repo owns, currently MongoDB data through the shared `EphemeralMongo` replica-set runner, can run in the default lane; externally managed providers such as Redis, NATS, and SMTP relay delivery stay discovered by default and run only when an explicit provider gate is enabled. |
| [`tests/Cephalon.Tests.Scripts`](../tests/Cephalon.Tests.Scripts) | Scripts | The PowerShell / bash scripts under `scripts/` are testable units rather than opaque automation; behavior changes flow through real assertions, including release-readiness manifest drift such as dependency-health provider rows diverging from their source-derived provider manifest, release-validation scorecard readback failing when dependency-health provider-manifest evidence is missing, and `TestCoverageEvidence` failing when layered project links, active-gap counts, or the quarantine queue drift. |
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

### #1 — `Cephalon.AspNetCore.Grpc` streaming + error-mode coverage (high priority, **shipped through `ENG-408` / [PR #922](https://github.com/Cephalon-Labs/CephalonEngine/pull/922)**)

`Cephalon.AspNetCore.Grpc` canonical gRPC `Status` mapping (NOT_FOUND, INVALID_ARGUMENT, UNAUTHENTICATED, PERMISSION_DENIED, UNAVAILABLE, RESOURCE_EXHAUSTED, FAILED_PRECONDITION, ABORTED) plus the unhandled-exception → UNKNOWN contract are exercised end-to-end at the hosting layer through `tests/Cephalon.Tests.Hosting/GrpcTransportErrorAndStreamingHostingTests.cs`. The same file covers server-streaming happy path, server-streaming with a server-thrown `RpcException(InvalidArgument)` mid-stream, server-streaming with client-side cancellation mid-stream, bidirectional-streaming happy path, and bidirectional-streaming with a server-thrown `RpcException(Internal)` after the first reply. The internal `GrpcStreamingAndErrorModesTestModule` declared inside the test file demonstrates the canonical pattern of routing scenarios through gRPC `Metadata` headers, kept as a reference implementation alongside the existing `DiscoveryGrpcService` in `Cephalon.Tests.Support`.

Unary client-cancellation and unary deadline-expiry coverage for `SayHello` are intentionally deferred: under `Microsoft.AspNetCore.TestHost` the in-memory request pipe does not propagate the client-side cancellation token to the server-side `ServerCallContext` reliably for unary calls, so the canonical cancellation contract is exercised through the streaming variants instead. The test module reserves the `delay` scenario for future coverage if a real-host harness lands.

Quality dimension: **Reliability** (transport contract).

### #2 — `Cephalon.AspNetCore.JsonRpc` error-response coverage (high priority, **shipped through `ENG-405` / [PR #919](https://github.com/Cephalon-Labs/CephalonEngine/pull/919)**)

`Cephalon.AspNetCore.JsonRpc` JSON-RPC 2.0 error envelopes (`-32700` parse error, `-32600` invalid request, `-32601` method not found, `-32602` invalid params, `-32603` internal error) are exercised end-to-end at the hosting layer through `tests/Cephalon.Tests.Hosting/JsonRpcErrorResponseHostingTests.cs`. The internal `JsonRpcErrorModesTestModule` declared inside the test file demonstrates the canonical error-envelope handling pattern as a reference implementation.

### #3 — `MetadataDrivenAuthorizationEvaluator` decision-matrix coverage (high priority, **shipped through `ENG-403` / [PR #917](https://github.com/Cephalon-Labs/CephalonEngine/pull/917)**)

The default `IAuthorizationEvaluator` decision matrix — every metadata-rule branch (`requiredRoles` ANY / ALL / empty, subject / resource / context attribute prefixes, `requireOwner`, `requireTenantMatch`, the no-rules policy, the policy-mode fallback, and the canceled-token throw path) — is pinned through `tests/Cephalon.Tests.Composition/Composition/MetadataDrivenAuthorizationDecisionMatrixTests.cs`. The reusable `IdentityDecisionMatrixTestModule` lives in `Cephalon.Tests.Support`.

### #4 — `IAuditActorAccessor` and `DefaultAuditRecorder` ambient-fallback coverage (high priority, **shipped through `ENG-404` / [PR #918](https://github.com/Cephalon-Labs/CephalonEngine/pull/918)**)

The `Cephalon.Audit` actor / correlation / tenant / entry-id fallback chain is exercised through `tests/Cephalon.Tests.Composition/Composition/AuditActorAndRecorderFallbackTests.cs`. Coverage includes the explicit-actor-wins / accessor-wins / system-actor-fallback chain, the `Activity.Current.TraceId` correlation fallback, the `ITenantContextAccessor.Current` tenant fallback, the `IIdGenerator` entry-id fallback, the `OccurredAtUtc` default-vs-explicit branches, and the writer-exception propagation path.

### #5 — Provider-native CDC integration scenarios for `Cephalon.Data.SqlServer`, `Cephalon.Data.Postgres`, `Cephalon.Data.MongoDB`, `Cephalon.Data.MySql`, and `Cephalon.Data.Oracle` (medium priority, **shipped through `ENG-487` / [PR #1091](https://github.com/Cephalon-Labs/CephalonEngine/pull/1091), `ENG-490` / issue #1096, `ENG-491` / issue #1098, `ENG-492` / issue #1100, `ENG-498` / issue #1113, and `ENG-499` / issue #1114**)

The shared CDC runtime catalog and the provider-native pumps (`sqlserver-cdc-capture-pump`, `postgresql-logical-replication-capture-pump`, `mongodb-change-stream-capture-pump`, `mysql-binlog-capture-pump`, `oracle-logminer-capture-pump`) are covered at the composition layer by `Cephalon.Tests.Composition`. `ENG-487` adds the first dedicated CDC integration lane, `tests/Cephalon.Tests.CdcIntegration`, and proves the MongoDB path against a disposable single-node replica set through `EphemeralMongo7`: real provider running, real change stream, real outbox staging, real runtime-state reporting, real execution-runtime aggregation, and real checkpoint commit. `ENG-490` adds the external-service gate for the same test project so SQL Server/Postgres/MySQL/Oracle live tests can be discovered but skipped by default, then opt into either pre-provisioned connection strings or Testcontainers-backed disposable services. `ENG-491` adds the first relational live CDC scenario on that gate: SQL Server database/table CDC is enabled against a live service, the provider-native runner stages the outbox message, runtime-state plus execution-runtime aggregation report through the shared catalog, and the SQL checkpoint table is verified. `ENG-492` closes the provider-specific Postgres gap by creating a live schema/table/publication/slot path, streaming a real logical-replication change through `Cephalon.Data.Postgres`, staging the outbox message, reporting shared runtime-state plus execution-runtime aggregation, and comparing the Cephalon checkpoint token with the slot's `confirmed_flush_lsn`. `ENG-498` extends the same external-service gate to MySQL and proves the current `Cephalon.Data.MySql.SciSharpReplication` adapter against a live row-based binlog source, including outbox staging, shared runtime-state plus execution-runtime aggregation, provider-native binlog lifecycle metadata, staged row payload truth, and durable `binlogFile|position` checkpoint persistence. `ENG-499` extends the same external-service gate to Oracle and proves the current LogMiner runner against a live `ARCHIVELOG` source, including outbox staging, shared runtime-state plus execution-runtime aggregation, LogMiner payload/header truth, archive-log lifecycle metadata, and durable `commitScn|changeScn|rsId|ssn` checkpoint persistence.

Quality dimension: **Reliability + Compatibility** (provider contract).

### #6 — `Cephalon.AspNetCore.GraphQL` transport-mapping coverage (medium priority, **gated**)

`Cephalon.AspNetCore.GraphQL` ships `M1` with route-mapping only today. When the package widens beyond route mapping (schema generation, query execution, error mapping), recommendation #6 promotes from gated to active and the test shape mirrors recommendation #1 (gRPC) and recommendation #2 (JSON-RPC) for the GraphQL canonical error envelope.

### #7 — Direct unit coverage for the four pre-existing `DebeziumDataCdcPackTests` recursion failures (low priority, **shipped through `ENG-488` / [PR #1093](https://github.com/Cephalon-Labs/CephalonEngine/pull/1093)**)

Four `DebeziumDataCdcPackTests` failures observed during full `Cephalon.Tests.Composition` runs were tracked under the *Pre-existing test-flake watch* section of [`docs/releases/v0.1.0-preview-notes.md`](releases/v0.1.0-preview-notes.md) and under the quarantine queue below. `ENG-488` fixes the shared `Cephalon.Data.Services.CdcCaptureExecutionRuntimeCatalog` hot path by pre-indexing capture ownership and reusing a versioned runtime snapshot for repeated managed-connector filter projections. The regression test `AddDebeziumData_ExecutionRuntimeFilterSnapshotRefreshesAfterRuntimeReports` proves repeated drift filters stay bounded and refresh after later runtime reports instead of skipping the failing surface.

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

### #10 — CDC execution-runtime catalog hot-path guardrail (medium priority, **shipped through `ENG-489` / [PR #1095](https://github.com/Cephalon-Labs/CephalonEngine/pull/1095)**)

`ENG-489` turns the ENG-488 shared snapshot fix into a benchmark-governed hot path. `CdcExecutionRuntimeCatalogBenchmarks` builds 24 Debezium-managed external runtimes with 48 captures through public engine composition APIs, reports live external observations, warms the shared execution-runtime snapshot, and measures runtime enumeration plus repeated managed-connector drift, dry-run, command-issuance, and compact multi-selector operator drill-down filters. The guardrail catalog now has dedicated caps for those five CDC projections, so future snapshot invalidation, external-runtime report projection, or managed-connector drill-down changes cannot silently reintroduce the filter-heavy recursion pressure that caused the pre-existing Debezium flake watch.

Quality dimension: **Performance + Reliability + Maintainability + Auditability** (benchmark guardrail over the CDC operator hot path).

### #11 — Provider-backed data + event-sourcing live canaries (medium priority, **shipped through `ENG-501`, `ENG-511`, `ENG-512`, `ENG-513`, and `ENG-514`**)

`ENG-501` adds `tests/Cephalon.Tests.ProviderIntegration` as the dedicated opt-in provider-integration lane for non-CDC provider behavior that must execute against real infrastructure. The first canary, `RedisProviderIntegrationTests.RedisProvider_StagesOutboxInboxDispatchAndEventStreamAgainstLiveRedis`, proves `Cephalon.Data.Redis` and `Cephalon.EventSourcing.Redis` against one live Redis runtime: engine registration, manifest capabilities, outbox and inbox descriptors, event-stream descriptor projection, Redis Hash / Sorted Set / Set / Stream persistence, idempotent outbox and inbox writes, dispatch-store success reporting, ordered event replay, optimistic-concurrency rejection, provider-durable Redis snapshot save/load, snapshot-assisted managed replay, projection rebuild, and runtime-surface durable-snapshot readback.

The default project run remains deterministic and Docker-free. Live execution is gated by `CEPHALON_PROVIDER_EXTERNAL_SERVICES=1` plus either `CEPHALON_PROVIDER_TESTCONTAINERS=1` or provider-specific pre-provisioned service settings; the `CEPHALON_PROVIDER_INTEGRATION` and `CEPHALON_REDIS_CONNECTION_STRING` aliases are accepted for developer ergonomics.

`ENG-511` promotes the MongoDB data-provider row from composition-only evidence to a default-running live proof. `MongoDbProviderIntegrationTests.MongoDbProvider_StagesOutboxInboxAndDispatchAgainstDisposableReplicaSet` starts a disposable single-node replica set through `Cephalon.Tests.Support.MongoDbReplicaSetRunner`, composes `Cephalon.Engine`, `Cephalon.Data.MongoDB`, and `Cephalon.Eventing`, then proves manifest capabilities, outbox and inbox descriptors, `event-driven-integration` runtime surfaces, idempotent MongoDB collection writes, dispatch-store pending/success transitions, and inbox exactly-once receipts without requiring Docker or a developer-managed MongoDB instance.

`ENG-512` promotes Cassandra, ClickHouse, Elasticsearch, NATS, Neo4j, OpenSearch, and Qdrant from composition-only data-provider evidence to opt-in live proof rows. `LiveDataProviderIntegrationTests` composes `Cephalon.Engine`, `Cephalon.Eventing`, and each real provider pack, then proves manifest capabilities, outbox/inbox descriptors, `event-driven-integration` runtime surfaces, idempotent outbox/inbox writes, and provider persistence. Cassandra, Elasticsearch, NATS, Neo4j, OpenSearch, and Qdrant also prove `IEventDispatchStore` pending/success transitions; ClickHouse deliberately proves the explicit `unsupported` dispatch policy so the analytics-store pack does not overclaim mutable dispatch ownership. Cassandra was also hardened so the first live provider operation creates the configured keyspace before connecting when the service account has permission. `ENG-513` closes the disposable-runtime follow-through: all seven lanes now resolve either pre-provisioned settings or `CEPHALON_PROVIDER_TESTCONTAINERS=1` into provider-specific Testcontainers services while default CI remains Docker-free. `ENG-711` extends the NATS lane beyond data-provider proof: `NatsProviderIntegrationTests.NatsProvider_ReplaysEventStreamSnapshotsAgainstLiveJetStream` composes `Cephalon.EventSourcing.Nats` against a real JetStream KV bucket and proves append/read, provider-durable snapshot save/load, snapshot-assisted replay, projection rebuild, final snapshot saveback, stale snapshot rejection, optimistic-concurrency rejection, and runtime metadata readback.

`ENG-514` adds the execution lane around those disposable services. `scripts/run-provider-live-testcontainers.ps1` is now the shared entry point for Docker preflight, locked restore, provider selection, per-provider test filters, and result directories. `.github/workflows/provider-live-testcontainers.yml` runs the same script on `ubuntu-latest` as a scheduled/manual matrix over Cassandra, ClickHouse, Elasticsearch, NATS, Neo4j, OpenSearch, Qdrant, Redis, and SMTP, keeping release validation Docker-free while still giving release managers a real Docker-capable provider proof path. `tests/Cephalon.Tests.Scripts/run-provider-live-testcontainers.Tests.ps1` guards the script matrix and workflow contract so the CI lane cannot drift from the provider test methods.

The SMTP delivery live proof extends that same lane for outbound provider handoff. `SmtpDeliveryProviderIntegrationTests.SmtpDelivery_DispatchesInvitationThroughLiveRelay` composes `Cephalon.MultiTenancy.Governance.SmtpDelivery`, dispatches a tenant invitation through the real governance dispatcher and SMTP sender, sends to a live MailHog-compatible relay, reads the accepted message back through the relay API, and verifies recipients, subject/body rendering, deterministic provider message id, Cephalon context headers, safe metadata, and sanitized runtime-surface truth. It remains skipped in default runs unless `CEPHALON_PROVIDER_EXTERNAL_SERVICES=1` plus either `CEPHALON_PROVIDER_TESTCONTAINERS=1` or `CEPHALON_PROVIDER_SMTP_HOST` / `CEPHALON_PROVIDER_SMTP_API_URI` are configured.

The closeout validation also hardened the surrounding provider/runtime suite: relational provider-native CDC composition and hosting tests assert capture totals and checkpoints while tolerating the legitimate captured-to-idle transition, Tooling child-process tests run process-spawning cases non-parallel and drain stdout/stderr with bounded timeouts, and the focused provider lane keeps opt-in live providers skipped by default until their gate is enabled. The dependency-health provider family now adds a deterministic managed-probe live runtime proof that starts one host and reports all eighteen `Cephalon.Observability.*Dependencies` providers as healthy. The current provider-integration scorecard readback is `33` rows / `33` live proofs / `0` composition-only rows / `104` runtime contracts.

Quality dimension: **Reliability + Compatibility + Auditability + Data Integrity** (provider contract).

### #12 — Surface-maturity and additive operator-section contract coverage (high priority, **shipped through `ENG-712` and `ENG-713`**)

`ENG-712` adds a Pester lane that reconciles all source projects, component documents, exact conformance rows, dependency-health provider rows, and maturity/backlog baseline dates. It proves a clean repository report, a deliberate badge/matrix mismatch that fails closed while leaving an inspectable artifact, and release-validation/workflow artifact wiring.

`ENG-713` adds composition tests for package-owned versioned snapshot sections, deterministic entry ordering, and duplicate section-id rejection. Hosting tests prove the dependency-health section through ASP.NET Core JSON, Worker snapshots, all eighteen failure reports, and all eighteen live-success probes. Provider reports also prove observation time, probe duration, and consecutive-failure count without changing the existing positional record constructor.

Quality dimension: **Reliability + Availability + Maintainability + Compatibility + Auditability** (maturity and operator-contract evidence).

## September validation repair

[ENG-745](sre-validation-2026-09.md) repairs the observed Windows behavior-timeout race with an explicit test clock and release/cancellation gates, and isolates Showcase HTTP/domain tests from an absent OTLP collector. The dedicated OpenTelemetry capture-server integration tests retain real exporter coverage. No test is skipped; the failing assertion is repaired within the active slice. Broader flake-rate/SLO acceptance remains ENG-746.

## Test-flake quarantine queue

When `engine.tests.flake-rate.7d` exceeds the target, the affected test enters a quarantine queue per the *Test flake budget* rule in [`sre-posture.md`](sre-posture.md): `[Skip]`-attribute the failing test with a tracking comment within 24 hours, then either fix or delete within 7 days.

| Test | Project | First observed | Quarantine action | Deadline |
| --- | --- | --- | --- | --- |
| Provider-native CDC hosting/composition timing plus Tooling child-process lanes | `Cephalon.Tests.Hosting`, `Cephalon.Tests.Composition`, `Cephalon.Tests.Tooling` | `May 7, 2026` full-suite validation | Resolved through `ENG-500` / `ENG-501`: provider-native CDC tests now assert observed totals/checkpoints across captured-to-idle transitions, CDC runtime waits use deterministic suite-level windows, the startup-failure policy test uses a 30-second restart-backoff assertion window, and Tooling child-process tests drain stdout/stderr with bounded timeouts while process-spawning cases run non-parallel | Closed in the same regression-closeout slice |
| `DebeziumDataCdcPackTests` (4 failures) | `Cephalon.Tests.Composition` | Pre-`v0.1.0-preview` | Resolved through `ENG-488`: shared execution-runtime filters now reuse a versioned snapshot over indexed capture ownership, and the Debezium regression refreshes after later reports | Closed before the GA release that follows `v0.1.0-preview` |

The queue is empty.

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
