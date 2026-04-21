# Cephalon.Data

`Cephalon.Data` is the runtime-neutral data execution companion package for Cephalon.

## What it owns

- registers default `IReadStore` and `IWriteStore` implementations backed by the existing command/query handler abstractions
- keeps command/query dispatching out of hosts so ASP.NET Core and worker apps stay thin
- provides the first reusable bridge between phase-8 data contracts and future provider-specific packs such as `Cephalon.Data.EntityFramework`
- provides the first reusable CDC runtime-state reporting/catalog bridge over `ICdcCaptureCatalog` plus optional linked outbox dispatch truth
- extends that same CDC bridge with typed freshness, lag, and publication-posture reporting
- provides an optional shared in-process CDC hosted-execution substrate that resolves active `ICdcCapture` plus `IOutbox` implementations, stages outbox publications, optionally acknowledges provider progress after stage success, and reports lifecycle truth through the existing execution/runtime-story surfaces
- provides an additive CDC execution-runtime catalog so shared, external-managed, provider-native, or other declared capture runners can publish one ownership/topology answer without replacing per-capture CDC truth
- provides an opt-in external CDC execution-runtime report sink so out-of-process runners can refresh the same runtime-state and execution-runtime summaries without inventing a second registry

## Main surfaces

- `Configuration/DataRuntimeOptions.cs`
- `Configuration/CdcCaptureExecutionRuntimeOptions.cs`
- `Registration/DataEngineBuilderExtensions.cs`
- `Services/CdcCaptureExecutionReport.cs`
- `Services/CdcCaptureExecutionRuntimeCatalog.cs`
- `Services/CdcCaptureExecutionRuntimeRegistry.cs`
- `Services/CdcCaptureHostedService.cs`
- `Services/CdcCaptureRuntimeStateCatalog.cs`
- `Services/ConfiguredCdcCaptureExecutionRuntimeContributor.cs`
- `Services/DataRuntimeIds.cs`
- `Services/HandlerDispatchingReadStore.cs`
- `Services/HandlerDispatchingWriteStore.cs`
- `Services/ICdcCaptureExecutionRuntimeContributor.cs`
- `Services/ICdcCaptureExecutionRuntimeRegistry.cs`
- `Services/ICdcCaptureRuntimeReporter.cs`
- `Services/SharedCdcCaptureExecutionRuntimeContributor.cs`

## How it fits

This package is intentionally the smallest honest first step for the broader data baseline. It does not claim relational persistence, Entity Framework integration, durable outbox behavior, or provider breadth yet. Instead, it turns the existing `ICommandHandler`, `IQueryHandler`, `IReadStore`, and `IWriteStore` contracts into a reusable runtime service so consumer apps can execute commands and queries through Cephalon-managed stores before a concrete database pack joins the picture.

That makes `Cephalon.Data` the runtime-neutral layer, while `Cephalon.Data.EntityFramework` now adds the first DbContext-backed companion-pack baseline on top of the same abstractions. The provider pack currently handles honest read/write DbContext registration, capability metadata, direct consumption of the engine-owned `Write` and optional `Read` database roles, explicit dependent `UseRole` references for `Outbox` and for `History` when a host deliberately aliases it to `write`, startup schema apply for those registered `DbContext` roles through a generic-host hosted service, opt-in Entity Framework-backed outbox and inbox paths, outbox descriptors that surface through `/engine/outboxes` and `/engine/snapshot`, inbox descriptors that surface through `/engine/inboxes` and `/engine/snapshot`, and event-driven technology-surface entries for staged outbox producers plus application-managed inbox stores when the eventing technology is active. Richer projection persistence, dedicated outbox execution, broader role graphs, subscription/runtime linkage, and fuller dispatch semantics remain open so the docs stay truthful about what is and is not shipped yet.

The same package now also owns the first runtime-neutral CDC live-state bridge and shared
in-process execution substrate. `AddData()` wires `ICdcCaptureRuntimeReporter` plus
`ICdcCaptureRuntimeStateCatalog` so provider packs, tests, or host code can report `started`,
`captured`, `idle`, or `failed` observations against the active descriptor catalog without
inventing a second runtime registry. The catalog projects every active capture even before the
first report arrives, preserves descriptor ownership fields such as `sourceModuleId`, `provider`,
`sourceId`, `outboxId`, `mode`, `eventFormat`, and `resourceIds`, tracks totals plus latest
checkpoint/change-id/error metadata, and can now also carry typed freshness windows, lag posture,
and pending-publication answers through `CdcCaptureExecutionReport`.

When `DataRuntimeOptions.EnableCdcExecution` is enabled, the same package now also registers a
shared `BackgroundService` pump that resolves active `ICdcCapture` implementations by
`CdcCaptureId`, reads one bounded `CdcCaptureExecutionResult`, stages the returned
`OutboxMessage` publications through the matching `IOutbox.OutboxId`, optionally calls
`ICdcCaptureAcknowledger` with one staged `CdcCaptureExecutionAcknowledgement` only after the
outbox handoff succeeds, and reports the resulting runtime posture back through the shared catalog.
That pump stays introspectable through the `data.cdc.execution` capability, the
`data-cdc-capture-flow` execution graph, the `data-cdc-capture-pump` hosted execution, and the
existing runtime-story/snapshot surfaces.

The same catalog family now also keeps capture-side execution ownership explicit. `CdcCaptureDescriptor`
and `CdcCaptureRuntimeState` both carry `ExecutionBinding`, so the authored/requested/effective
execution-runtime answer lives on the per-capture truth instead of only on the runtime side.
`CdcCaptureExecutionBoundCatalog` resolves that binding deterministically from authored capture
intent plus active execution-runtime claims, defaults unclaimed captures to the shared
`data-cdc-capture-pump` only when that runtime is active, rejects ambiguous competing claims, and
lets the shared pump execute only captures whose effective owner is the shared runtime.

The same shared runtime now also projects one operator-facing CDC execution-runtime answer through
`ICdcCaptureExecutionRuntimeCatalog`. The shipped `SharedCdcCaptureExecutionRuntimeContributor`
publishes `data-cdc-capture-pump` with first-class `executionOwnership = host-managed`,
`executionTopology = shared-in-process-polling`, `acknowledgementMode = post-stage-provider`,
linked `hostedExecutionId` plus `executionGraphId`, and `surface = shared-cdc-execution`, while
`DataRuntimeOptions.CdcExecutionRuntimes` plus
`ConfiguredCdcCaptureExecutionRuntimeContributor` now let hosts declare additional external,
managed, edge, or provider-native execution runtimes on that same catalog without pretending the
shared pump owns them. `CdcCaptureExecutionRuntimeCatalog` then folds the shared
`ICdcCaptureRuntimeStateCatalog` into one aggregate `CdcCaptureExecutionRuntimeSummary` per runtime
by reusing the same inverse execution-runtime lookup that capture and runtime-state surfaces already
publish. That same ownership/topology answer now flows through `/engine/cdc-capture-runtimes`,
`/engine/cdc-captures/execution-runtimes/{executionRuntimeId}`,
`/engine/cdc-captures/runtime/execution-runtimes/{executionRuntimeId}`, and
`snapshot.CdcCaptureExecutionRuntimes`, so additional provider-native or out-of-process runners can
project on the same truth instead of inventing a second host-only runner registry beside
`/engine/cdc-captures*`.

When `DataRuntimeOptions.EnableExternalCdcRuntimeReporting` is enabled, the same package now also
registers `ICdcCaptureExecutionRuntimeReportSink` on top of the shared
`CdcCaptureRuntimeStateCatalog`. That opt-in sink accepts `CdcCaptureRuntimeObservation` batches
for one `executionRuntimeId`, validates that every reported `cdcCaptureId` is effectively owned by
that runtime, stamps `metadata.cdcCaptureExecutionRuntimeId`, and then refreshes the existing
`/engine/cdc-captures/runtime*`, `/engine/cdc-capture-runtimes*`, and `snapshot` answers through
the same shared catalog instead of a second external-monitor path. That same seam now also lets
declared execution runtimes opt into `ObservationStaleAfterSeconds` and
`RejectOutOfOrderReports`, treats repeated `CdcCaptureRuntimeObservation.ReportId` values as
idempotent retries when the payload matches, rejects older reports when the runtime requires
ordered ingestion, and derives `ObservationFreshness` plus execution-runtime-summary freshness from
the configured stale window even when the active runner lives out of process. The shared catalog
also stamps `metadata.cdcCaptureReportId`, `metadata.observationFreshnessState`,
`metadata.observationFreshUntilUtc`, and `metadata.observationStaleAfterSeconds` so operators can
read latest report identity and expiry posture straight off `/engine/cdc-captures/runtime*`,
`/engine/cdc-capture-runtimes*`, and `snapshot` without inventing a second watchdog registry.

`Cephalon.Data.MongoDB` first proved that contract with a document-oriented provider-native runner.
Its `mongodb-change-stream-capture-pump` contributor publishes host-managed, provider-native,
provider-native-acknowledgement ownership through the shared execution-runtime catalog while the
provider pack keeps per-capture `sourceModuleId` truth, stages outbox messages, persists
resume-token checkpoints only after stage success, and reports live posture back through the same
shared CDC runtime-state catalog. The shared `data-cdc-capture-pump` remains additive and simply
ignores captures whose effective owner resolves to the MongoDB runtime.

`Cephalon.Data.SqlServer` now proves the same ownership and runtime model on a relational source.
Its `sqlserver-cdc-capture-pump` contributor keeps the same `/engine/cdc-*` truth model while
polling SQL Server CDC change tables, staging outbox messages, and durably persisting LSN-based
checkpoint tokens only after stage success. That gives the shared data baseline both a document and
a relational provider-native runner without adding a second control plane beside
`/engine/cdc-captures*`, `/engine/cdc-captures/runtime*`, or `/engine/cdc-capture-runtimes*`.

When the outbox path already reports downstream runtime truth, the same catalog can conservatively
merge that dispatch posture into `OutboxDispatchState` and the typed CDC publication answer. That
keeps `Cephalon.Data` honest: it now owns the shared in-process execution substrate plus the shared
reporting/catalog surface, while provider packs or modules still own the actual WAL,
change-stream, or source-specific capture semantics behind `ICdcCapture`.

The engine-owned database-topology baseline is now in place through `Engine:Databases`, `AppProfile.Databases`, `/engine/databases`, the resolved `/engine/database-roles` operator catalog, and the resolved `/engine/database-migrations` operator catalog. That baseline makes shared runtime tuning, `Write` / `Read` / `Outbox` / `History` roles, migration policy, requested versus resolved role truth, role-consumer metadata, provider-contributed live role health, logical migration-target state, and provider-added deploy-time command templates introspectable without turning `Cephalon.Data` itself into a provider-specific pack. The next follow-through is deeper provider consumption of those roles rather than inventing a second topology model inside each pack. See [Database topology](../database-topology.md).

## Related docs

- [Cephalon.Abstractions](abstractions.md)
- [Cephalon.Data.EntityFramework](data-entityframework.md)
- [Cephalon.Data.SqlServer](data-sqlserver.md)
- [Cephalon.Engine](engine.md)
- [Cephalon.Ids.Sfid](ids-sfid.md)
- [Architecture](../architecture.md)
