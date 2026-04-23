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
declared execution runtimes opt into `ObservationStaleAfterSeconds`,
`RejectOutOfOrderReports`, `ReporterLeaseSeconds`, `RejectConflictingReporterIds`, and declared
`EdgeNodeIds`, treats repeated `CdcCaptureRuntimeObservation.ReportId` values as idempotent
retries when the payload matches, rejects older reports when the runtime requires ordered
ingestion, rejects conflicting `ReporterId` values while an active reporter lease still exists,
rejects `EdgeNodeId` values that fall outside the declared runtime allow-list, and derives
`ObservationFreshness` plus execution-runtime-summary freshness from the configured stale window
even when the active runner lives out of process. The shared catalog also stamps
`metadata.cdcCaptureReportId`, `metadata.observationFreshnessState`,
`metadata.observationFreshUntilUtc`, `metadata.observationStaleAfterSeconds`,
`metadata.cdcCaptureReporterId`, `metadata.cdcCaptureReporterLeaseExpiresAtUtc`, and
`metadata.cdcCaptureEdgeNodeId`, while `CdcCaptureRuntimeState` plus
`CdcCaptureExecutionRuntimeSummary` now keep first-class `LastReporterId`, `ActiveReporterId`,
`ReporterLeaseExpiresAtUtc`, `ObservedEdgeNodeIds`, `LastEdgeNodeId`, and typed
`ReporterCoordination` so operators can read latest report identity, active owner, lease-expiry,
takeover history, degraded conflict posture, and edge provenance straight off
`/engine/cdc-captures/runtime*`, `/engine/cdc-capture-runtimes*`, and `snapshot` without
inventing a second watchdog registry or topology-coordination surface. That same shared
coordination answer now also derives participant-level `active`, `standby`, and `rejected`
reporter stories through `ReporterParticipants`, `HasStandbyReporters`, and
`HasRejectedReporters`, plus additive participant counters such as `ParticipantCount`,
`ActiveReporterCount`, `StandbyReporterCount`, and `RejectedReporterCount`, so accepted takeovers
keep previous owners visible as standby evidence and rejected conflicts stay visible without
collapsing the operator story back to one last-conflicting reporter field. It now also keeps
stable `TakeoverState` plus `DegradedReason` identifiers and the derived `RequiresTakeover` /
`HasCompletedTakeover` helpers, so the same shared `/engine/cdc-*` and `snapshot` surfaces can
distinguish an expired lease still awaiting failover, a rejected conflicting reporter, a
multi-capture multiple-active ambiguity, and a completed takeover without inventing a second
coordination registry. The shared runtime-state catalog now also clears stale rejected-conflict
evidence after later accepted reports and drops takeover-only standby participants once the
replacement reporter reaffirms its lease, while historical takeover fields remain queryable on the
same shared runtime story.

That same execution-runtime story now also carries additive operator rollups instead of forcing
operators to reopen one capture payload and re-aggregate the participant story manually.
`CdcCaptureExecutionRuntimeSummary.ReporterCoordinationRollup` now publishes stable breakdowns for
coordination states and degraded reasons together with distinct `ActiveReporterIds`,
`StandbyReporterIds`, `RejectedReporterIds`, and `DegradedCdcCaptureIds`. The same rollup flows
through `/engine/cdc-capture-runtimes*` and `snapshot.CdcCaptureExecutionRuntimes`, so runtime
summaries can answer active-versus-standby-versus-rejected posture directly without inventing a
second operator index.

That same shared execution-runtime story now also keeps declared-versus-reported coverage explicit.
`CdcCaptureExecutionRuntimeSummary.ReportingCoverage` now publishes stable
`not-bound` / `unreported` / `partially-reported` / `fully-reported` posture together with
declared and reported capture counts, still-unreported capture ids, and derived
`HasUnreportedDeclaredCaptures` / `HasFullCaptureCoverage` helpers. `ReportedCdcCaptureIds` now
only lists captures that have submitted real observations instead of descriptor-backed placeholder
state, and the same coverage truth flows through `/engine/cdc-capture-runtimes*` plus
`snapshot.CdcCaptureExecutionRuntimes` without inventing a second external-runtime coverage
registry.

That same shared execution-runtime story now also keeps operator remediation explicit. The shared
catalog now derives `CdcCaptureExecutionRuntimeSummary.Remediation` from resolved runtime capture
ownership instead of only the authored `CdcCaptureExecutionRuntimeOptions.CdcCaptureIds` list, so
coverage and remediation stay truthful even when capture ownership comes from authored
`ExecutionBinding` intent. The same additive remediation answer now publishes stable
`ready` / `attention` / `blocked` posture together with active remediation categories such as
`failed-cdc-captures`, `reporter-coordination-issues`, `stale-observations`, and
`unreported-cdc-captures`, while still surfacing the affected capture ids back onto
`/engine/cdc-capture-runtimes*` plus `snapshot.CdcCaptureExecutionRuntimes` instead of inventing a
second external-runtime remediation registry.

That same shared execution-runtime story now also keeps managed-connector governance explicit.
`CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorGovernance` publishes stable
`not-applicable` / `observe-only` / `future-control-plane` / `out-of-policy` posture together with
governance categories such as `missing-management-mode`, `missing-connect-cluster-id`,
`missing-connector-class`, `missing-source-provider-id`, and `future-control-plane-mode`, plus the
recommended action id, declared-versus-reported task ids, and latest connector lifecycle or
reconciliation context. The shared execution-runtime catalog derives that answer from merged
runtime metadata, so external managed connectors can stay on the existing `/engine/cdc-*`,
`/engine/runtime-story`, and `snapshot` surfaces instead of inventing a Debezium-only governance
registry.

That same shared execution-runtime story now also keeps managed-connector
desired-versus-observed drift explicit. `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorDrift`
publishes stable `not-applicable` / `unknown` / `in-sync` / `drifted` posture together with drift
categories such as `missing-task-baseline`, `reported-task-topology-unavailable`,
`reported-task-identity-unavailable`, `task-count-mismatch`, `missing-declared-task-reports`,
`unexpected-reported-tasks`, `connect-cluster-mismatch`, `connector-class-mismatch`, and
`source-provider-mismatch`, plus the recommended action id, declared-versus-reported connector
identity, declared-versus-reported task ids, and the latest lifecycle or reconciliation context.
The shared execution-runtime catalog derives that answer from merged runtime metadata, so external
managed connectors can keep desired-versus-observed drift on the existing `/engine/cdc-*`,
`/engine/runtime-story`, and `snapshot` surfaces instead of inventing a Debezium-only drift
registry.

That same shared execution-runtime story now also keeps managed-connector action planning
explicit. `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorActionPlan` publishes stable
`not-applicable` / `observe` / `waiting` / `action-required` / `blocked` posture together with
action-plan categories, ordered action ids, and the source remediation, governance, plus drift
state. The shared execution-runtime catalog derives that answer from the merged remediation,
governance, and desired-versus-observed drift truth, so operator follow-through can stay on the
existing `/engine/cdc-*`, `/engine/runtime-story`, and `snapshot` surfaces instead of inventing a
Debezium-only action-planning registry.

That same shared execution-runtime story now also keeps managed-connector write-path readiness
explicit. `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorWritePathReadiness` publishes
stable `not-applicable` / `deferred` / `not-ready` / `ready` / `blocked` posture together with
readiness categories, source coverage/remediation/governance/drift/action-plan state, and the
current primary action id. The shared execution-runtime catalog derives that answer from merged
reporting coverage, remediation, governance, desired-versus-observed drift, and action-planning
truth, so future write-path follow-through can stay on the existing `/engine/cdc-*`,
`/engine/runtime-story`, and `snapshot` surfaces instead of inventing a Debezium-only readiness
registry.

That same shared execution-runtime story now also keeps managed-connector preflight explicit.
`CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorPreflight` publishes stable
`not-applicable` / `deferred` / `not-ready` / `ready` / `blocked` posture together with
preflight categories, the intended operation id, source
coverage/remediation/governance/drift/action-plan/write-path-readiness state, and the current
primary action id. The shared execution-runtime catalog derives that answer from the already
shipped coverage, remediation, governance, desired-versus-observed drift, action-planning, and
write-path-readiness truth, so future connector-management preflight can stay on the existing
`/engine/cdc-*`, `/engine/runtime-story`, and `snapshot` surfaces instead of inventing a
Debezium-only preflight registry.

That same shared execution-runtime story now also keeps managed-connector dry-run explicit.
`CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorDryRun` publishes stable
`not-applicable` / `deferred` / `blocked` / `no-op` / `would-change` posture together with
dry-run categories, the intended operation id, source
coverage/remediation/governance/drift/action-plan/write-path-readiness/preflight state, the
current primary action id, and additive potential-change detail. The shared execution-runtime
catalog derives that answer from the already shipped coverage, remediation, governance,
desired-versus-observed drift, action-planning, write-path-readiness, and preflight truth, so
future connector-management dry-run can stay on the existing `/engine/cdc-*`,
`/engine/runtime-story`, and `snapshot` surfaces instead of inventing a Debezium-only dry-run
registry.

That same shared execution-runtime story now also keeps managed-connector execution intent
explicit. `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorExecutionIntent` publishes stable
`not-applicable` / `deferred` / `blocked` / `operator-action` / `requires-approval` /
`ready-to-execute` posture together with execution-intent categories, the intended operation id,
source coverage/remediation/governance/drift/action-plan/write-path-readiness/preflight/dry-run
state, the current primary action id, the confidence-source id, and additive potential-change
detail. The shared execution-runtime catalog derives that answer from the already shipped
coverage, remediation, governance, desired-versus-observed drift, action-planning,
write-path-readiness, preflight, and dry-run truth, so future connector-management follow-through
can stay on the existing `/engine/cdc-*`, `/engine/runtime-story`, and `snapshot` surfaces
instead of inventing a Debezium-only execution planner.

That same shared execution-runtime story now also keeps managed-connector execution approval and
safety-gating explicit. `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorExecutionApproval`
publishes stable `not-applicable` / `auto-blocked` / `policy-blocked` / `approval-required` /
`approval-ready` / `auto-eligible` posture together with execution-approval categories, the
intended operation id, source
coverage/remediation/governance/drift/action-plan/write-path-readiness/preflight/dry-run/execution-intent
state, the current primary action id, the safety-gating source id, and explicit-approval detail.
The shared execution-runtime catalog derives that answer from the already shipped coverage,
remediation, governance, desired-versus-observed drift, action-planning, write-path-readiness,
preflight, dry-run, and execution-intent truth, so future connector-management approval workflows
can stay on the existing `/engine/cdc-*`, `/engine/runtime-story`, and `snapshot` surfaces
instead of inventing a Debezium-only approval registry.

That same shared execution-runtime story now also keeps managed-connector write-path command
envelopes explicit. `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorCommandEnvelope`
publishes stable `not-applicable` / `blocked` / `operator-only` / `approval-gated` /
`engine-ready` posture together with command-envelope categories, the intended operation id,
source coverage/remediation/governance/drift/action-plan/write-path-readiness/preflight/dry-run/
execution-intent/execution-approval state, the current primary action id, source truth, target
connector identity, deterministic command fingerprints, and safety flags. The shared
execution-runtime catalog derives that answer from the already shipped execution-approval,
execution-intent, dry-run, preflight, write-path-readiness, desired-versus-observed drift,
governance, remediation, and coverage truth, so future connector-management execution work can
stay on the existing `/engine/cdc-*`, `/engine/runtime-story`, and `snapshot` surfaces instead of
inventing a Debezium-only execution registry.

That same shared execution-runtime story now also keeps managed-connector command issuance
explicit. `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorCommandIssuance` publishes stable
`not-applicable` / `blocked` / `operator-only` / `accepted` / `rejected` / `issued` posture
together with command-issuance categories, the intended operation id,
source coverage/remediation/governance/drift/action-plan/write-path-readiness/preflight/dry-run/
execution-intent/execution-approval/command-envelope state, issuance source truth, target
connector identity, deterministic command fingerprints, deterministic issuance fingerprints, and
safety flags. The shared execution-runtime catalog derives that answer from the already shipped
command-envelope, execution-approval, execution-intent, dry-run, preflight, write-path-readiness,
desired-versus-observed drift, governance, remediation, and coverage truth, so later provider
transport or execution-adapter work can stay on the existing `/engine/cdc-*`, `/engine/runtime-story`,
and `snapshot` surfaces instead of inventing a Debezium-only issuance registry.

That same shared execution-runtime story now also keeps managed-connector provider execution
adapters explicit. `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorExecutionAdapter`
publishes stable `not-applicable` / `blocked` / `operator-only` / `unavailable` / `ready`
posture together with execution-adapter categories, the intended operation id,
source coverage/remediation/governance/drift/action-plan/write-path-readiness/preflight/dry-run/
execution-intent/execution-approval/command-envelope/command-issuance state, adapter identity,
deterministic adapter fingerprints, and provider-ready safety flags. The shared execution-runtime
catalog derives that answer from the already shipped command-issuance, command-envelope,
execution-approval, execution-intent, dry-run, preflight, write-path-readiness,
desired-versus-observed drift, governance, remediation, and coverage truth, so provider execution
translation can stay on the existing `/engine/cdc-*`, `/engine/runtime-story`, and `snapshot`
surfaces instead of inventing a Debezium-only transport registry.

That same shared execution-runtime story now also keeps managed-connector command-execution outcomes
explicit. `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorCommandExecution` publishes stable
`not-applicable` / `unrecorded` / `blocked` / `operator-only` / `unavailable` / `no-op` /
`adapted` / `failed` posture together with the requested and resolved operation ids, source
execution-adapter and command-issuance state, deterministic command/execution fingerprints, and
latest recorded attempt metadata such as `AttemptId`, `RecordedAtUtc`, and `HasRecordedOutcome`.
The shared execution-runtime catalog now keeps that latest-outcome answer plus bounded recent
history on the same `/engine/cdc-*`, `/engine/runtime-story`, and `snapshot` surfaces, and that
same bounded history now feeds the later shared managed-connector command-journal answer instead
of inventing a Debezium-only durable journal.

That same shared execution-runtime story now also keeps managed-connector command-retry and
idempotency posture explicit. `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorCommandRetry`
publishes stable `not-applicable` / `not-needed` / `duplicate` / `cooldown` / `retry-blocked` /
`operator-only` / `retry-eligible` posture together with retry categories, the intended operation
id, source coverage/remediation/governance/drift/action-plan/write-path-readiness/preflight/
dry-run/execution-intent/execution-approval/command-envelope/command-issuance/execution-adapter/
latest-command-execution state, deterministic retry fingerprints, cooldown windows, and latest
attempt metadata. The shared execution-runtime catalog derives that answer from the existing
command-execution history plus the current command-issuance, execution-adapter, and safety posture,
so hosts can answer retry versus duplicate versus cooldown truth on the same `/engine/cdc-*`,
`/engine/runtime-story`, and `snapshot` surfaces instead of inventing a Debezium-only retry
registry.

That same shared execution-runtime story now also keeps managed-connector retry-execution policy
explicit. `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorRetryExecutionPolicy` publishes
stable `not-applicable` / `not-needed` / `cooldown` / `manual-approval` / `policy-blocked` /
`operator-only` / `background-retry-disabled` posture together with policy categories, the intended
operation id, source coverage/remediation/governance/drift/action-plan/write-path-readiness/
preflight/dry-run/execution-intent/execution-approval/command-envelope/command-issuance/
execution-adapter/latest-command-execution state, deterministic retry fingerprints, cooldown
windows, and latest attempt metadata. The shared execution-runtime catalog derives that answer from
the existing command-retry posture plus command-execution history, command-issuance,
execution-adapter, execution-approval, and command-envelope truth, so hosts can answer "retry now,
wait, require approval, or stay blocked by policy" on the same `/engine/cdc-*`,
`/engine/runtime-story`, and `snapshot` surfaces instead of inventing a Debezium-only retry-policy
registry.

That same shared execution-runtime story now also keeps managed-connector bounded command-journal
truth explicit. `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorCommandJournal` publishes
stable `not-applicable` / `empty` / `bounded` / `truncated` / `cooldown-active` /
`duplicate-evidence-present` / `insufficient-for-automation` posture together with journal
categories, the intended operation id, source coverage/remediation/governance/drift/action-plan/
write-path-readiness/preflight/dry-run/execution-intent/execution-approval/command-envelope/
command-issuance/execution-adapter/latest-command-execution/command-retry/retry-execution-policy
state, retained-versus-recorded history counts, deterministic fingerprint matching, cooldown
windows, and latest-versus-oldest retained attempt metadata. The shared execution-runtime catalog
derives that answer from the existing command-execution history, command-retry posture, and
retry-execution-policy posture, so hosts can answer whether the bounded journal is empty,
truncated, cooling down, duplicate-backed, or still insufficient for automation on the same
`/engine/cdc-*`, `/engine/runtime-story`, and `snapshot` surfaces instead of inventing a
Debezium-only durable journal registry.

That same shared execution-runtime story now also keeps managed-connector automatic background
retry execution explicit. `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorAutomaticRetryExecution`
publishes stable `not-applicable` / `disabled` / `blocked` / `eligible` / `completed` posture
together with automatic-retry categories, the intended operation id, source
coverage/remediation/governance/drift/action-plan/write-path-readiness/preflight/dry-run/
execution-intent/execution-approval/command-envelope/command-issuance/execution-adapter/
latest-command-execution/command-retry/retry-execution-policy/command-journal state, deterministic
command, retry, and execution fingerprints, latest automatic-attempt metadata, operator versus
automatic invocation-source truth, cooldown windows, and matching approval/destructive-allowance
reuse flags. The shared execution-runtime catalog derives that answer from the existing
retry-execution-policy posture plus command-journal, command-retry, and command-execution history
truth, and the shared data pack can optionally run one bounded in-process automatic retry lane
through `DataRuntimeOptions.EnableManagedConnectorAutomaticRetryExecution` and
`ManagedConnectorAutomaticRetryPollingIntervalSeconds` instead of inventing a Debezium-only retry
loop or second coordinator.

That same shared execution-runtime story now also keeps managed-connector automatic background
retry coordination explicit. `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorAutomaticRetryCoordination`
publishes stable `not-applicable` / `single-node` / `uncoordinated` / `lease-held` /
`lease-missing` / `conflicted` / `operator-only` posture together with coordination categories,
source truth, coordination-owner id, active-reporter id, reporter-lease timing, and
`CanExecuteOnCurrentNode`. The shared execution-runtime catalog derives that answer from the
existing execution ownership, execution topology, reporter-coordination, automatic-retry,
retry-execution-policy, and command-journal truth, and the shared data pack can optionally scope
that bounded retry lane to one host through `DataRuntimeOptions.ManagedConnectorAutomaticRetryCoordinationOwnerId`
so the hosted retry service and automatic command invocations do not invent a second
cross-node coordination registry.

That same shared execution-runtime story now also keeps managed-connector command-journal
durability explicit. `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorCommandJournalDurability`
publishes stable `not-applicable` / `in-memory-only` / `persisted` / `recovered` /
`recovery-failed` / `persistence-failed` posture together with durability categories, persistence
path, persisted-versus-recovered timestamps, retained-versus-recorded history counts, and additive
persisted or recovered history flags. The shared execution-runtime catalog derives that answer from
the existing command-journal, automatic-retry, and coordination truth plus the bounded history
store, and the shared data pack can optionally persist that same bounded retry evidence through
`DataRuntimeOptions.ManagedConnectorCommandJournalPersistencePath` so hosts can recover durable
command history after restart without inventing a Debezium-only persistence registry or second
command journal.

That same shared execution-runtime story now also keeps managed-connector distributed retry lease
and cross-node idempotency posture explicit.
`CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorDistributedRetryLease` publishes stable
`not-applicable` / `single-node` / `lease-held` / `lease-missing` / `lease-conflicted` /
`idempotent-safe` / `idempotency-risk` / `operator-only` posture together with lease categories,
coordination-owner and active-reporter identity, retry fingerprint and attempt counts, durable
journal evidence, and `CanExecuteAutomaticRetryOnCurrentNode`. The shared execution-runtime catalog
derives that answer from the existing automatic-retry coordination, automatic-retry execution,
retry-execution-policy, command-journal, command-journal durability, and retained command history
truth, and the shared data pack now gates both `ManagedConnectorAutomaticRetryHostedService` and
automatic command invocations through that richer cross-node answer instead of inventing a second
distributed retry coordinator.

That same shared execution-runtime story now also keeps managed-connector distributed retry
orchestration explicit.
`CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorDistributedRetryOrchestration` publishes
stable `not-applicable` / `disabled` / `operator-only` / `cooldown` / `blocked` / `scheduled` /
`completed` posture together with orchestration categories, scheduler id/kind, polling interval,
owner and reporter identity, retry fingerprint, cooldown windows, latest-attempt metadata,
durable-history evidence, and `CanScheduleAutomaticRetryOnCurrentNode`. The shared
execution-runtime catalog derives that answer from the existing automatic-retry execution,
automatic-retry coordination, retry-execution-policy, command-journal durability, and distributed
retry lease truth, and the shared data pack now gates both
`ManagedConnectorAutomaticRetryHostedService` and automatic command invocations through that one
orchestration answer instead of scattering scheduler checks across host code.

That same shared operator story is now directly queryable by reporter, edge node, coordination
state, degraded reason, remediation state, remediation category, managed-connector governance
state, managed-connector governance category, managed-connector drift state,
managed-connector drift category, managed-connector action-plan state, managed-connector action
id, managed-connector write-path-readiness state, managed-connector write-path-readiness
category, managed-connector preflight state, managed-connector preflight category,
managed-connector preflight operation, managed-connector dry-run state, managed-connector
dry-run category, managed-connector dry-run operation, managed-connector execution-intent state,
managed-connector execution-intent category, managed-connector execution-intent operation,
managed-connector execution-approval state, managed-connector execution-approval category,
managed-connector execution-approval operation, managed-connector command-envelope state,
managed-connector command-envelope category, managed-connector command-envelope operation,
managed-connector command-issuance state, managed-connector command-issuance category,
managed-connector command-issuance operation, managed-connector execution-adapter state,
managed-connector execution-adapter category, managed-connector execution-adapter operation,
managed-connector command-execution state, managed-connector command-execution operation,
managed-connector command-retry state, managed-connector command-retry category,
managed-connector command-retry operation, managed-connector retry-execution-policy state,
managed-connector retry-execution-policy category, managed-connector retry-execution-policy
operation, managed-connector command-journal state, managed-connector command-journal
category, managed-connector command-journal-durability state,
managed-connector command-journal-durability category, managed-connector automatic-retry state,
managed-connector automatic-retry category, managed-connector automatic-retry operation,
managed-connector automatic-retry-coordination state, managed-connector automatic-retry-coordination
category, managed-connector automatic-retry-coordination owner id, managed-connector
distributed-retry-orchestration state, managed-connector distributed-retry-orchestration category,
and managed-connector distributed-retry-orchestration owner id as well.
`ICdcCaptureRuntimeStateCatalog` plus `ICdcCaptureExecutionRuntimeCatalog` now expose
`GetByReporterId(...)`, `GetByEdgeNodeId(...)`, `GetByReporterCoordinationState(...)`,
`GetByReporterCoordinationIssueReason(...)`, `GetByRemediationState(...)`, and
`GetByRemediationCategory(...)`, `GetByManagedConnectorGovernanceState(...)`,
`GetByManagedConnectorGovernanceCategory(...)`, `GetByManagedConnectorDriftState(...)`, and
`GetByManagedConnectorDriftCategory(...)`, `GetByManagedConnectorActionPlanState(...)`, and
`GetByManagedConnectorActionId(...)`, `GetByManagedConnectorWritePathReadinessState(...)`, and
`GetByManagedConnectorWritePathReadinessCategory(...)`,
`GetByManagedConnectorPreflightState(...)`, `GetByManagedConnectorPreflightCategory(...)`, and
`GetByManagedConnectorPreflightOperationId(...)`,
`GetByManagedConnectorDryRunState(...)`, `GetByManagedConnectorDryRunCategory(...)`, and
`GetByManagedConnectorDryRunOperationId(...)`,
`GetByManagedConnectorExecutionIntentState(...)`,
`GetByManagedConnectorExecutionIntentCategory(...)`,
`GetByManagedConnectorExecutionIntentOperationId(...)`,
`GetByManagedConnectorExecutionApprovalState(...)`,
`GetByManagedConnectorExecutionApprovalCategory(...)`, and
`GetByManagedConnectorExecutionApprovalOperationId(...)`,
`GetByManagedConnectorCommandEnvelopeState(...)`,
`GetByManagedConnectorCommandEnvelopeCategory(...)`, and
`GetByManagedConnectorCommandEnvelopeOperationId(...)`,
`GetByManagedConnectorCommandIssuanceState(...)`,
`GetByManagedConnectorCommandIssuanceCategory(...)`, and
`GetByManagedConnectorCommandIssuanceOperationId(...)`,
`GetByManagedConnectorExecutionAdapterState(...)`,
`GetByManagedConnectorExecutionAdapterCategory(...)`, and
`GetByManagedConnectorExecutionAdapterOperationId(...)`,
`GetByManagedConnectorCommandExecutionState(...)`,
`GetByManagedConnectorCommandExecutionOperationId(...)`, and
`GetByManagedConnectorCommandRetryState(...)`,
`GetByManagedConnectorCommandRetryCategory(...)`, and
`GetByManagedConnectorCommandRetryOperationId(...)`,
`GetByManagedConnectorRetryExecutionPolicyState(...)`,
`GetByManagedConnectorRetryExecutionPolicyCategory(...)`, and
`GetByManagedConnectorRetryExecutionPolicyOperationId(...)`,
`GetByManagedConnectorCommandJournalState(...)`, and
`GetByManagedConnectorCommandJournalCategory(...)`,
`GetByManagedConnectorCommandJournalDurabilityState(...)`, and
`GetByManagedConnectorCommandJournalDurabilityCategory(...)`,
`GetByManagedConnectorDistributedRetryLeaseState(...)`,
`GetByManagedConnectorDistributedRetryLeaseCategory(...)`, and
`GetByManagedConnectorDistributedRetryLeaseOwnerId(...)`,
`GetByManagedConnectorAutomaticRetryExecutionState(...)`,
`GetByManagedConnectorAutomaticRetryExecutionCategory(...)`, and
`GetByManagedConnectorAutomaticRetryExecutionOperationId(...)`,
`GetByManagedConnectorAutomaticRetryCoordinationState(...)`,
`GetByManagedConnectorAutomaticRetryCoordinationCategory(...)`, and
`GetByManagedConnectorAutomaticRetryCoordinationOwnerId(...)`,
`GetByManagedConnectorDistributedRetryOrchestrationState(...)`,
`GetByManagedConnectorDistributedRetryOrchestrationCategory(...)`, and
`GetByManagedConnectorDistributedRetryOrchestrationOwnerId(...)`, plus
`GetManagedConnectorCommandExecutionHistory(...)`, so host code, provider packs, and tooling can
narrow the shared CDC runtime story without rebuilding a second coordination, remediation,
managed-connector governance, managed-connector drift, managed-connector action-planning,
managed-connector readiness, managed-connector preflight, managed-connector dry-run,
managed-connector execution-intent, managed-connector execution-approval, managed-connector
command-envelope, managed-connector command-issuance, managed-connector execution-adapter,
managed-connector command-execution, managed-connector command-retry,
managed-connector retry-execution-policy,
managed-connector command-journal,
managed-connector command-journal durability, or
managed-connector distributed retry lease,
managed-connector automatic background retry execution,
managed-connector automatic background retry coordination,
managed-connector distributed retry orchestration, or
managed-connector command-execution-history
index.
ASP.NET Core maps those same filters through
`/engine/cdc-captures/runtime/reporters/{reporterId}`,
`/engine/cdc-captures/runtime/edge-nodes/{edgeNodeId}`,
`/engine/cdc-captures/runtime/reporter-coordination/{coordinationState}`,
`/engine/cdc-captures/runtime/reporter-coordination/issues/{degradedReason}`,
`/engine/cdc-capture-runtimes/reporters/{reporterId}`,
`/engine/cdc-capture-runtimes/edge-nodes/{edgeNodeId}`,
`/engine/cdc-capture-runtimes/reporter-coordination/{coordinationState}`, and
`/engine/cdc-capture-runtimes/reporter-coordination/issues/{degradedReason}` together with
`/engine/cdc-capture-runtimes/remediation/{remediationState}` and
`/engine/cdc-capture-runtimes/remediation/categories/{remediationCategory}` plus
`/engine/cdc-capture-runtimes/governance/{governanceState}` and
`/engine/cdc-capture-runtimes/governance/categories/{governanceCategory}` plus
`/engine/cdc-capture-runtimes/drift/{driftState}` and
`/engine/cdc-capture-runtimes/drift/categories/{driftCategory}` plus
`/engine/cdc-capture-runtimes/action-plans/{actionPlanState}` and
`/engine/cdc-capture-runtimes/actions/{actionId}` plus
`/engine/cdc-capture-runtimes/write-path-readiness/{readinessState}` and
`/engine/cdc-capture-runtimes/write-path-readiness/categories/{readinessCategory}` plus
`/engine/cdc-capture-runtimes/preflight/{preflightState}`,
`/engine/cdc-capture-runtimes/preflight/categories/{preflightCategory}`,
`/engine/cdc-capture-runtimes/preflight/operations/{operationId}` plus
`/engine/cdc-capture-runtimes/dry-runs/{dryRunState}`,
`/engine/cdc-capture-runtimes/dry-runs/categories/{dryRunCategory}`,
`/engine/cdc-capture-runtimes/dry-runs/operations/{operationId}` plus
`/engine/cdc-capture-runtimes/execution-intents/{executionIntentState}`,
`/engine/cdc-capture-runtimes/execution-intents/categories/{executionIntentCategory}`,
`/engine/cdc-capture-runtimes/execution-intents/operations/{operationId}` plus
`/engine/cdc-capture-runtimes/execution-approvals/{executionApprovalState}`,
`/engine/cdc-capture-runtimes/execution-approvals/categories/{executionApprovalCategory}`, and
`/engine/cdc-capture-runtimes/execution-approvals/operations/{operationId}` plus
`/engine/cdc-capture-runtimes/command-envelopes/{commandState}`,
`/engine/cdc-capture-runtimes/command-envelopes/categories/{commandCategory}`, and
`/engine/cdc-capture-runtimes/command-envelopes/operations/{operationId}` plus
`/engine/cdc-capture-runtimes/command-issuances/{issuanceState}`,
`/engine/cdc-capture-runtimes/command-issuances/categories/{issuanceCategory}`, and
`/engine/cdc-capture-runtimes/command-issuances/operations/{operationId}` plus
`/engine/cdc-capture-runtimes/command-executions/{executionState}`,
`/engine/cdc-capture-runtimes/command-executions/operations/{operationId}`, and
`/engine/cdc-capture-runtimes/{executionRuntimeId}/command-executions` plus
`/engine/cdc-capture-runtimes/command-retries/{retryState}`,
`/engine/cdc-capture-runtimes/command-retries/categories/{retryCategory}`, and
`/engine/cdc-capture-runtimes/command-retries/operations/{operationId}` plus
`/engine/cdc-capture-runtimes/retry-execution-policies/{policyState}`,
`/engine/cdc-capture-runtimes/retry-execution-policies/categories/{policyCategory}`, and
`/engine/cdc-capture-runtimes/retry-execution-policies/operations/{operationId}` plus
`/engine/cdc-capture-runtimes/command-journals/{journalState}`,
`/engine/cdc-capture-runtimes/command-journals/categories/{journalCategory}`, and
`/engine/cdc-capture-runtimes/{executionRuntimeId}/command-journal` plus
`/engine/cdc-capture-runtimes/command-journal-durability/{durabilityState}`,
`/engine/cdc-capture-runtimes/command-journal-durability/categories/{durabilityCategory}`, and
`/engine/cdc-capture-runtimes/{executionRuntimeId}/command-journal-durability` plus
`/engine/cdc-capture-runtimes/distributed-retry-leases/{leaseState}`,
`/engine/cdc-capture-runtimes/distributed-retry-leases/categories/{leaseCategory}`,
`/engine/cdc-capture-runtimes/distributed-retry-leases/owners/{ownerId}`, and
`/engine/cdc-capture-runtimes/{executionRuntimeId}/distributed-retry-lease` plus
`/engine/cdc-capture-runtimes/automatic-retries/{automaticRetryState}`,
`/engine/cdc-capture-runtimes/automatic-retries/categories/{automaticRetryCategory}`, and
`/engine/cdc-capture-runtimes/automatic-retries/operations/{operationId}` plus
`/engine/cdc-capture-runtimes/automatic-retry-coordinations/{coordinationState}`,
`/engine/cdc-capture-runtimes/automatic-retry-coordinations/categories/{coordinationCategory}`,
`/engine/cdc-capture-runtimes/automatic-retry-coordinations/owners/{ownerId}` plus
`/engine/cdc-capture-runtimes/distributed-retry-orchestrations/{orchestrationState}`,
`/engine/cdc-capture-runtimes/distributed-retry-orchestrations/categories/{orchestrationCategory}`,
`/engine/cdc-capture-runtimes/distributed-retry-orchestrations/owners/{ownerId}`, and
`/engine/cdc-capture-runtimes/{executionRuntimeId}/distributed-retry-orchestration` so the live
host surface stays aligned with the same shared runtime-state and execution-runtime catalogs.

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

`Cephalon.Data.Postgres` now proves that the same runtime story also fits logical-replication
streaming on PostgreSQL. Its `postgresql-logical-replication-capture-pump` contributor keeps the
same `/engine/cdc-*`, `/engine/execution-graphs`, `/engine/hosted-executions`, and `snapshot`
surfaces while validating publication/table ownership, optionally creating the logical replication
slot, streaming bounded pgoutput batches, staging outbox messages, and acknowledging
slot-confirmed flush progress only after stage success. That gives the shared data baseline
provider-native CDC proofs across document, relational change-table, and relational
logical-streaming sources without inventing a second control plane beside `/engine/cdc-captures*`,
`/engine/cdc-captures/runtime*`, or `/engine/cdc-capture-runtimes*`.

`Cephalon.Data.Oracle` now proves that the same runtime story also fits redo-log style Oracle
capture through LogMiner. Its `oracle-logminer-capture-pump` contributor keeps the same
`/engine/cdc-*`, `/engine/execution-graphs`, `/engine/hosted-executions`, and `snapshot` surfaces
while resolving bounded SCN windows, selecting covering redo plus archive log files, starting one
committed-only LogMiner session per iteration, staging deterministic outbox messages, and
persisting durable `commitScn|changeScn|rsId|ssn` checkpoints only after stage success. That gives
the shared data baseline provider-native CDC proofs across document, relational change-table,
logical-streaming, binlog, and redo-log families without inventing a second control plane beside
`/engine/cdc-captures*`, `/engine/cdc-captures/runtime*`, or `/engine/cdc-capture-runtimes*`.

`Cephalon.Data.Debezium` now proves that the same runtime story also fits external managed
connectors without faking a Cephalon-hosted execution loop. Its managed-connector descriptors plus
shared report-sink bridge keep Debezium or Kafka Connect style lifecycle, reconciliation,
reporter-coordination truth, governance posture, desired-versus-observed drift posture, and
operator action-planning truth on the same `/engine/cdc-*`, `/engine/runtime-story`, and
`snapshot` surfaces, while the shared execution-runtime catalog now rolls that connector posture
back into typed runtime-level operator summaries, provider execution-adapter posture, latest
managed-connector command-execution outcome/history, additive command-retry/idempotency truth,
additive retry-execution-policy truth, additive bounded command-journal truth, and additive
automatic background retry plus automatic-retry-coordination plus distributed retry lease plus
distributed retry orchestration truth instead of inventing a Debezium-only status registry.

When the outbox path already reports downstream runtime truth, the same catalog can conservatively
merge that dispatch posture into `OutboxDispatchState` and the typed CDC publication answer. That
keeps `Cephalon.Data` honest: it now owns the shared in-process execution substrate plus the shared
reporting/catalog surface, while provider packs or modules still own the actual WAL,
change-stream, or source-specific capture semantics behind `ICdcCapture`.

The engine-owned database-topology baseline is now in place through `Engine:Databases`, `AppProfile.Databases`, `/engine/databases`, the resolved `/engine/database-roles` operator catalog, and the resolved `/engine/database-migrations` operator catalog. That baseline makes shared runtime tuning, `Write` / `Read` / `Outbox` / `History` roles, migration policy, requested versus resolved role truth, role-consumer metadata, provider-contributed live role health, logical migration-target state, and provider-added deploy-time command templates introspectable without turning `Cephalon.Data` itself into a provider-specific pack. The next follow-through is deeper provider consumption of those roles rather than inventing a second topology model inside each pack. See [Database topology](../database-topology.md).

## Related docs

- [Cephalon.Abstractions](abstractions.md)
- [Cephalon.Data.Debezium](data-debezium.md)
- [Cephalon.Data.EntityFramework](data-entityframework.md)
- [Cephalon.Data.MySql](data-mysql.md)
- [Cephalon.Data.Oracle](data-oracle.md)
- [Cephalon.Data.Postgres](data-postgres.md)
- [Cephalon.Data.SqlServer](data-sqlserver.md)
- [Cephalon.Engine](engine.md)
- [Cephalon.Ids.Sfid](ids-sfid.md)
- [Architecture](../architecture.md)
