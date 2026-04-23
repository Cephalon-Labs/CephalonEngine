# Cephalon.Data.Debezium

`Cephalon.Data.Debezium` is the Debezium-managed external CDC companion pack for Cephalon. It proves that the shared `Cephalon.Data` CDC runtime story also fits managed Kafka Connect or Debezium-style connector topologies where Cephalon does not own the runner, does not fake a hosted execution, and still publishes truthful capture ownership, external runtime reporting, reporter-lease posture, connector or task lifecycle posture, managed-connector governance posture, desired-versus-observed managed-connector drift posture, managed-connector action-planning posture, managed-connector write-path readiness posture, managed-connector preflight posture, managed-connector dry-run posture, managed-connector execution-intent posture, managed-connector execution-approval posture, managed-connector command-envelope posture, and operator drill-downs on the existing shared `/engine/cdc-*`, `/engine/runtime-story`, and `snapshot` surfaces.

## What it owns

- contributes Debezium-managed capture descriptors through `DebeziumCaptureOptions` and keeps those descriptors on the shared `/engine/cdc-captures*` catalog with `provider = "debezium"` and `mode = "managed-connector"`
- contributes external execution runtimes through `DebeziumConnectorOptions` and keeps those runtimes on the shared `/engine/cdc-capture-runtimes*` catalog with `executionOwnership = external-managed`, `executionTopology = managed-connector`, and `acknowledgementMode = connector-offset-commit`
- wires the shared external-reporting sink automatically when Debezium connectors are configured, so hosts that already add `Cephalon.Data` do not also need to remember `EnableExternalCdcRuntimeReporting = true` just to accept managed connector reports
- normalizes connector, task, reconciliation, managed-connector governance, and desired-versus-observed drift metadata from external Debezium reports into stable `debezium*` plus shared `managedConnector*` metadata on the existing capture and execution-runtime surfaces so the shared `Cephalon.Data` catalog can also derive operator-facing action plans without inventing a Debezium-only lifecycle, governance, drift, or action registry
- preserves authored capture ownership through `CdcCaptureDescriptor.SourceModuleId` while surfacing `metadata.contributorModuleId = "debezium-data"` when the Debezium pack contributes descriptors on behalf of another module

## Main surfaces

- `Configuration/DebeziumDataOptions.cs`
- `Configuration/DebeziumConnectorOptions.cs`
- `Configuration/DebeziumCaptureOptions.cs`
- `Modules/DebeziumDataModule.cs`
- `Registration/DebeziumDataEngineBuilderExtensions.cs`
- `Services/DebeziumExecutionRuntimeContributor.cs`
- `Services/DebeziumExecutionRuntimeReportSink.cs`

## How it fits

This pack sits on top of `Cephalon.Data`, not in place of it. `Cephalon.Data` still owns the shared CDC descriptor catalog, capture-side execution binding, runtime-state catalog, execution-runtime catalog, operator drill-down routes, runtime story, and snapshot surfaces. `Cephalon.Data.Debezium` adds the managed-connector contribution layer that declares Debezium-owned captures and external execution runtimes on those shared surfaces without inventing a Debezium-specific registry.

That keeps the runtime honest. `Cephalon.Data.Debezium` does not pretend Cephalon runs Kafka Connect tasks or connector worker loops itself, so it does not contribute a fake execution graph or hosted execution. The actual managed connector stays out of process and reports observations back into the shared runtime story through `POST /engine/cdc-capture-runtimes/{executionRuntimeId}/reports`, while this pack normalizes connector or task lifecycle, reconciliation detail, and managed-connector governance or drift inputs into additive shared metadata so the engine-owned catalog can answer the operator action plan without inventing a second Debezium operator catalog.

The slice also stays intentionally scoped. This pack does not claim Kafka Connect provisioning, Debezium REST management, connector lifecycle orchestration, schema-registry management, or provider-native read/write persistence. It exists to project truthful Debezium-managed capture and runtime topology onto the shared Cephalon CDC surfaces, with non-`observe-only` management declarations remaining governance truth about future intent rather than a shipped write-path controller.

## Registration

```csharp
engine.AddData();

engine.AddDebeziumData(options =>
{
    var connector = new DebeziumConnectorOptions
    {
        Id = "inventory-debezium-connector",
        DisplayName = "Inventory Debezium Connector",
        Description = "Represents a Debezium-managed PostgreSQL connector.",
        ConnectClusterId = "connect-cluster-a",
        ConnectorClass = "io.debezium.connector.postgresql.PostgresConnector",
        SourceProviderId = "postgresql",
        TopicPrefix = "inventory",
        ManagementMode = "observe-only",
        ObservationStaleAfterSeconds = 180,
        ReporterLeaseSeconds = 120,
        RejectConflictingReporterIds = true,
        ExpectedTaskCount = 2
    };
    connector.EdgeNodeIds.Add("edge-bkk-01");
    connector.TaskIds.Add("0");
    connector.TaskIds.Add("1");
    connector.CdcCaptures.Add(new DebeziumCaptureOptions
    {
        Id = "inventory-customers-cdc",
        DisplayName = "Inventory Customers CDC",
        SourceModuleId = "inventory",
        OutboxId = "tenant-event-outbox",
        TopicName = "inventory.public.customers",
        SnapshotMode = "initial"
    });

    options.Connectors.Add(connector);
});
```

For configuration-driven hosts, prefer binding from `Engine:Data:Debezium`:

```json
{
  "Engine": {
    "Data": {
      "Debezium": {
        "Connectors": [
          {
            "Id": "inventory-debezium-connector",
            "DisplayName": "Inventory Debezium Connector",
            "ConnectClusterId": "connect-cluster-a",
            "ConnectorClass": "io.debezium.connector.postgresql.PostgresConnector",
            "SourceProviderId": "postgresql",
            "TopicPrefix": "inventory",
            "ManagementMode": "observe-only",
            "ObservationStaleAfterSeconds": 180,
            "ReporterLeaseSeconds": 120,
            "RejectConflictingReporterIds": true,
            "ExpectedTaskCount": 2,
            "EdgeNodeIds": ["edge-bkk-01"],
            "TaskIds": ["0", "1"],
            "CdcCaptures": [
              {
                "Id": "inventory-customers-cdc",
                "DisplayName": "Inventory Customers CDC",
                "SourceModuleId": "inventory",
                "OutboxId": "tenant-event-outbox",
                "TopicName": "inventory.public.customers",
                "SnapshotMode": "initial"
              }
            ]
          }
        ]
      }
    }
  }
}
```

`engine.AddData()` is still required because the shared `Cephalon.Data` pack owns the runtime-state catalog, execution-runtime catalog, and shared `/engine/cdc-*` surfaces. `Cephalon.Data.Debezium` removes the extra host ceremony for enabling external runtime reports once that shared data pack is present.

## Configuration options (`Engine:Data:Debezium`)

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Connectors` | `DebeziumConnectorOptions[]` | `[]` | Debezium-managed external connector runtimes that contribute captures and runtime ownership |

## Connector options (`Engine:Data:Debezium:Connectors[]`)

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Id` | `string` | required | Stable execution-runtime id for the managed connector |
| `DisplayName` | `string` | `Id` | Operator-facing connector name |
| `Description` | `string` | generated | Human-readable connector description |
| `ConnectClusterId` | `string` | empty | Operator-facing Kafka Connect or Debezium cluster identifier |
| `ConnectorClass` | `string` | empty | Debezium connector-class identifier |
| `SourceProviderId` | `string` | empty | Upstream provider family behind the connector, such as `postgresql` or `sqlserver` |
| `TopicPrefix` | `string` | empty | Debezium topic prefix |
| `ExecutionOwnership` | `string` | `external-managed` | Execution-ownership mode projected on the shared runtime descriptor |
| `ExecutionTopology` | `string` | `managed-connector` | Execution-topology classification projected on the shared runtime descriptor |
| `AcknowledgementMode` | `string` | `connector-offset-commit` | Operator-facing acknowledgement mode |
| `ManagementMode` | `string` | `observe-only` | Operator-facing connector lifecycle-management mode published on the shared runtime surfaces |
| `ObservationStaleAfterSeconds` | `int?` | `300` | Report-freshness window used to mark external observations stale |
| `RejectOutOfOrderReports` | `bool` | `false` | Whether the runtime rejects out-of-order reports |
| `ReporterLeaseSeconds` | `int?` | `120` | Reporter-lease window for active reporter ownership |
| `RejectConflictingReporterIds` | `bool` | `false` | Whether conflicting reporter ids are rejected while an active lease exists |
| `ExpectedTaskCount` | `int?` | `null` or `TaskIds.Length` | Expected Debezium task count when the connector should publish reconciliation expectations even if task ids are not declared individually |
| `TaskIds` | `string[]` | `[]` | Declared connector task identifiers |
| `EdgeNodeIds` | `string[]` | `[]` | Declared edge nodes that can originate observations |
| `CdcCaptures` | `DebeziumCaptureOptions[]` | `[]` | Shared CDC capture descriptors owned by the connector runtime |
| `Metadata` | `Dictionary<string,string>` | `{}` | Additional operator-facing runtime metadata |

## Capture options (`Engine:Data:Debezium:Connectors:CdcCaptures[]`)

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Id` | `string` | required | Stable CDC capture id |
| `DisplayName` | `string` | `Id` | Operator-facing capture name |
| `Description` | `string` | generated | Human-readable capture description |
| `SourceModuleId` | `string` | required | Module id that owns the capture surface |
| `SourceId` | `string` | derived | Logical upstream source id when it should differ from the derived connector/topic path |
| `OutboxId` | `string` | required | Logical outbox boundary that the external connector feeds |
| `TopicName` | `string` | empty | External topic name carrying the Debezium envelope |
| `Mode` | `string` | `managed-connector` | Operator-facing capture mode |
| `EventFormat` | `string` | `debezium-envelope` | Operator-facing event format |
| `SnapshotMode` | `string` | `connector-default` | Debezium snapshot posture published as metadata |
| `ResourceIds` | `string[]` | `[]` or `[TopicName]` | Logical resources observed by the capture |
| `Tags` | `string[]` | `["cdc","debezium","external-managed"]` | Operator-facing tags |
| `Metadata` | `Dictionary<string,string>` | `{}` | Additional operator-facing metadata |

## Managed connector runtime behavior

When Debezium connectors are configured:

- each connector publishes one external execution runtime through `/engine/cdc-capture-runtimes*` and `snapshot.CdcCaptureExecutionRuntimes`
- each contributed capture binds to that runtime through authored and requested `executionRuntimeId` so the shared runtime catalog resolves external ownership deterministically
- the shared `POST /engine/cdc-capture-runtimes/{executionRuntimeId}/reports` route becomes available without requiring `EnableExternalCdcRuntimeReporting = true` explicitly on `DataRuntimeOptions`
- later runtime reports can still surface reporter id, edge node id, stale observation posture, reporter-lease expiry, degraded coordination posture, connector or task lifecycle metadata, and operator drill-downs through the same `/engine/cdc-captures/runtime*`, `/engine/cdc-capture-runtimes*`, `/engine/runtime-story`, and `snapshot` surfaces already used by the rest of the shared CDC model

## Lifecycle and reconciliation hardening

The `ENG-165` follow-through keeps lifecycle truth additive over the shared report route instead of adding a Debezium-only status registry.

- connector declarations can now publish `ManagementMode`, `ExpectedTaskCount`, and declared `TaskIds` as stable runtime expectations on both capture and execution-runtime metadata
- runtime reports can now include raw Debezium-facing keys such as `connectorState`, `reportedTaskIds`, `activeTaskIds`, `failedTaskIds`, `pausedTaskIds`, `restartingTaskIds`, `taskStateSummary`, `rebalanceState`, `connectorGeneration`, and `workerId`
- the Debezium report sink now normalizes those raw values into stable `debeziumConnectorLifecycleState`, `debeziumTaskReconciliationState`, `debeziumReconciliationState`, `debeziumReconciliationReason`, and additive task-summary metadata on the shared capture runtime-state catalog
- the shared execution-runtime catalog now also promotes runtime-scoped Debezium reconciliation metadata back onto `/engine/cdc-capture-runtimes*` and `snapshot.CdcCaptureExecutionRuntimes`, so operators do not need to re-open one capture payload just to understand the connector's latest reported lifecycle posture
- that same shared execution-runtime catalog now also derives `ReporterCoordinationRollup` on `CdcCaptureExecutionRuntimeSummary`, so Debezium-managed runtimes can answer active versus standby versus rejected reporter posture, degraded-capture ids, and coordination-state or degraded-reason breakdowns directly through `/engine/cdc-capture-runtimes*` and `snapshot.CdcCaptureExecutionRuntimes` without a second Debezium rollup surface

## Managed-connector governance baseline

The `ENG-169` follow-through keeps managed-connector governance additive over the same shared
runtime surface instead of introducing a Debezium-only control-plane catalog.

- connector declarations and runtime reports now normalize shared `managedConnector*` metadata beside the existing `debezium*` metadata, including management mode, declared versus reported task ids, expected versus reported task counts, and connector reconciliation context
- `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorGovernance` now publishes stable `observe-only`, `future-control-plane`, and `out-of-policy` posture for Debezium-managed runtimes together with governance categories, recommended action ids, and the latest lifecycle or reconciliation context
- the shared execution-runtime catalog now derives that governance answer from merged runtime metadata, so missing `ManagementMode`, `ConnectClusterId`, `ConnectorClass`, or `SourceProviderId` becomes truthful `out-of-policy` posture on the existing `/engine/cdc-capture-runtimes*` and `snapshot.CdcCaptureExecutionRuntimes` surfaces
- ASP.NET Core now maps `/engine/cdc-capture-runtimes/governance/{governanceState}` plus `/engine/cdc-capture-runtimes/governance/categories/{governanceCategory}` on the same shared runtime route family, so operator drill-down stays aligned with the engine-owned catalog instead of a Debezium-only endpoint family
- non-`observe-only` values such as `apply-and-reconcile` currently surface as `future-control-plane` governance truth only; they do not mean Cephalon already owns Kafka Connect write paths

## Managed-connector drift baseline

The `ENG-170` follow-through keeps desired-versus-observed managed-connector drift additive over
that same shared runtime surface instead of introducing a Debezium-only drift registry.

- connector declarations and runtime reports now normalize shared declared-versus-reported
  `managedConnector*` identity metadata beside the existing `debezium*` metadata, including
  connector cluster, connector class, source provider, expected task count, declared task ids, and
  reported task topology
- `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorDrift` now publishes stable
  `not-applicable`, `unknown`, `in-sync`, and `drifted` posture for Debezium-managed runtimes
  together with drift categories, recommended action ids, declared-versus-reported connector
  identity, declared-versus-reported task ids, and the latest lifecycle or reconciliation context
- the shared execution-runtime catalog now derives that drift answer from merged runtime metadata,
  so missing task baselines, missing reported task topology, task-count mismatches, missing
  declared task ids, unexpected reported task ids, and connector-identity mismatches become
  truthful operator posture on the existing `/engine/cdc-capture-runtimes*` and
  `snapshot.CdcCaptureExecutionRuntimes` surfaces
- ASP.NET Core now maps `/engine/cdc-capture-runtimes/drift/{driftState}` plus
  `/engine/cdc-capture-runtimes/drift/categories/{driftCategory}` on the same shared runtime route
  family, so operator drill-down stays aligned with the engine-owned catalog instead of a
  Debezium-only endpoint family
- drift posture currently remains read-only operator truth; it does not mean Cephalon already owns
  Kafka Connect write paths or automatic connector mutation

## Managed-connector action-planning baseline

The `ENG-171` follow-through keeps managed-connector action planning additive over that same shared
runtime surface instead of introducing a Debezium-only operator planner.

- `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorActionPlan` now publishes stable
  `not-applicable`, `observe`, `waiting`, `action-required`, and `blocked` posture for
  Debezium-managed runtimes together with ordered action ids, action-plan categories, and the
  source remediation, governance, plus drift state
- the shared execution-runtime catalog now derives that action plan from the already-shipped
  remediation, governance, and drift truth, so stale observations, governance gaps, incomplete
  task baselines, missing runtime truth, observed drift, and future control-plane declarations can
  collapse into one operator-facing answer on the existing `/engine/cdc-capture-runtimes*` and
  `snapshot.CdcCaptureExecutionRuntimes` surfaces
- ASP.NET Core now maps `/engine/cdc-capture-runtimes/action-plans/{actionPlanState}` plus
  `/engine/cdc-capture-runtimes/actions/{actionId}` on the same shared runtime route family, so
  operator drill-down stays aligned with the engine-owned catalog instead of a Debezium-only
  endpoint family
- action planning currently remains read-only operator truth; it does not mean Cephalon already
  owns Kafka Connect write paths, automatic connector mutation, or managed-connector control-plane
  orchestration

## Managed-connector write-path readiness baseline

The `ENG-172` follow-through keeps managed-connector write-path readiness additive over that same
shared runtime surface instead of introducing a Debezium-only readiness planner.

- `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorWritePathReadiness` now publishes stable
  `not-applicable`, `deferred`, `not-ready`, `ready`, and `blocked` posture for
  Debezium-managed runtimes together with readiness categories, source
  coverage/remediation/governance/drift/action-plan state, and the current primary action id
- the shared execution-runtime catalog now derives that readiness answer from the already-shipped
  coverage, remediation, governance, drift, and action-planning truth, so observe-only,
  out-of-policy, incomplete-reporting, drifted, blocked, and future-control-plane declarations can
  collapse into one operator-facing answer on the existing `/engine/cdc-capture-runtimes*` and
  `snapshot.CdcCaptureExecutionRuntimes` surfaces
- ASP.NET Core now maps
  `/engine/cdc-capture-runtimes/write-path-readiness/{readinessState}` plus
  `/engine/cdc-capture-runtimes/write-path-readiness/categories/{readinessCategory}` on the same
  shared runtime route family, so operator drill-down stays aligned with the engine-owned catalog
  instead of a Debezium-only endpoint family
- write-path readiness currently remains read-only operator truth; it does not mean Cephalon
  already owns Kafka Connect write-path execution, automatic connector mutation, or
  managed-connector control-plane orchestration

## Managed-connector preflight baseline

The `ENG-173` follow-through keeps managed-connector preflight additive over that same shared
runtime surface instead of introducing a Debezium-only preflight planner.

- `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorPreflight` now publishes stable
  `not-applicable`, `deferred`, `not-ready`, `ready`, and `blocked` posture for
  Debezium-managed runtimes together with preflight categories, the intended operation id, source
  coverage/remediation/governance/drift/action-plan/write-path-readiness state, and the current
  primary action id
- the shared execution-runtime catalog now derives that preflight answer from the already-shipped
  coverage, remediation, governance, drift, action-planning, and write-path-readiness truth, so
  observe-only, out-of-policy, incomplete-reporting, drifted, blocked, and future-control-plane
  declarations can surface one consistent connector-management preflight answer without claiming
  Kafka Connect write paths or automatic connector mutation
- ASP.NET Core now maps `/engine/cdc-capture-runtimes/preflight/{preflightState}`,
  `/engine/cdc-capture-runtimes/preflight/categories/{preflightCategory}`, and
  `/engine/cdc-capture-runtimes/preflight/operations/{operationId}` on the same shared runtime
  route family, so operator drill-down stays aligned with the engine-owned catalog instead of a
  Debezium-only endpoint family
- preflight currently remains read-only operator truth; it does not mean Cephalon already owns
  Kafka Connect write-path execution, automatic connector mutation, or managed-connector
  control-plane orchestration

## Managed-connector dry-run baseline

The `ENG-174` follow-through keeps managed-connector dry-run additive over that same shared
runtime surface instead of introducing a Debezium-only dry-run planner.

- `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorDryRun` now publishes stable
  `not-applicable`, `deferred`, `blocked`, `no-op`, and `would-change` posture for
  Debezium-managed runtimes together with dry-run categories, the intended operation id, source
  coverage/remediation/governance/drift/action-plan/write-path-readiness/preflight state, the
  current primary action id, and additive potential-change detail
- the shared execution-runtime catalog now derives that dry-run answer from the already-shipped
  coverage, remediation, governance, drift, action-planning, write-path-readiness, and preflight
  truth, so observe-only, out-of-policy, incomplete-reporting, drifted, blocked,
  future-control-plane, and lifecycle-specific declarations can surface one consistent
  connector-management dry-run answer without claiming Kafka Connect write paths or automatic
  connector mutation
- ASP.NET Core now maps `/engine/cdc-capture-runtimes/dry-runs/{dryRunState}`,
  `/engine/cdc-capture-runtimes/dry-runs/categories/{dryRunCategory}`, and
  `/engine/cdc-capture-runtimes/dry-runs/operations/{operationId}` on the same shared runtime
  route family, so operator drill-down stays aligned with the engine-owned catalog instead of a
  Debezium-only endpoint family
- dry-run currently remains read-only operator truth; it does not mean Cephalon already owns
  Kafka Connect write-path execution, automatic connector mutation, or managed-connector
  control-plane orchestration

## Managed-connector execution-intent baseline

The `ENG-175` follow-through keeps managed-connector execution intent additive over that same
shared runtime surface instead of introducing a Debezium-only execution planner.

- `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorExecutionIntent` now publishes stable
  `not-applicable`, `deferred`, `blocked`, `operator-action`, `requires-approval`, and
  `ready-to-execute` posture for Debezium-managed runtimes together with execution-intent
  categories, the intended operation id, source
  coverage/remediation/governance/drift/action-plan/write-path-readiness/preflight/dry-run state,
  the current primary action id, the confidence-source id, and additive potential-change detail
- the shared execution-runtime catalog now derives that execution-intent answer from the
  already-shipped coverage, remediation, governance, drift, action-planning, write-path
  readiness, preflight, and dry-run truth, so observe-only, out-of-policy,
  incomplete-reporting, blocking-remediation, reconcile-drift, and lifecycle-specific
  declarations can surface one consistent connector-management execution-intent answer without
  claiming Kafka Connect write paths or automatic connector mutation
- reconcile paths that would still apply future control-plane changes remain operator-owned on the
  shared surface, while lifecycle operations such as `pause` can now surface approval-gated or
  ready-to-execute future engine lanes when shared preflight truth is satisfied
- ASP.NET Core now maps `/engine/cdc-capture-runtimes/execution-intents/{executionIntentState}`,
  `/engine/cdc-capture-runtimes/execution-intents/categories/{executionIntentCategory}`, and
  `/engine/cdc-capture-runtimes/execution-intents/operations/{operationId}` on the same shared
  runtime route family, so operator drill-down stays aligned with the engine-owned catalog
  instead of a Debezium-only endpoint family
- execution intent currently remains read-only operator truth; it does not mean Cephalon already
  owns Kafka Connect write-path execution, automatic connector mutation, or managed-connector
  control-plane orchestration

## Managed-connector execution-approval baseline

The `ENG-176` follow-through keeps managed-connector execution approval and safety-gating
additive over that same shared runtime surface instead of introducing a Debezium-only approval
registry.

- `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorExecutionApproval` now publishes stable
  `not-applicable`, `auto-blocked`, `policy-blocked`, `approval-required`, `approval-ready`, and
  `auto-eligible` posture for Debezium-managed runtimes together with execution-approval
  categories, the intended operation id, source
  coverage/remediation/governance/drift/action-plan/write-path-readiness/preflight/dry-run/execution-intent
  state, the current primary action id, the safety-gating source id, and explicit-approval detail
- the shared execution-runtime catalog now derives that execution-approval answer from the
  already-shipped coverage, remediation, governance, drift, action-planning, write-path
  readiness, preflight, dry-run, and execution-intent truth, so observe-only,
  out-of-policy, incomplete-reporting, blocking-remediation, lifecycle-specific, and destructive
  declarations can surface one consistent connector-management execution-approval answer without
  claiming Kafka Connect write paths or automatic connector mutation
- reconcile paths that still depend on a future control plane remain policy-blocked on the shared
  surface, lifecycle operations such as `pause` can now surface approval-ready truth, and
  destructive operations such as `delete` surface explicit approval-required truth before any
  future engine execution lane
- ASP.NET Core now maps `/engine/cdc-capture-runtimes/execution-approvals/{executionApprovalState}`,
  `/engine/cdc-capture-runtimes/execution-approvals/categories/{executionApprovalCategory}`, and
  `/engine/cdc-capture-runtimes/execution-approvals/operations/{operationId}` on the same shared
  runtime route family, so operator drill-down stays aligned with the engine-owned catalog
  instead of a Debezium-only endpoint family
- execution approval currently remains read-only operator truth; it does not mean Cephalon
  already owns Kafka Connect write-path execution, automatic connector mutation, or
  managed-connector control-plane orchestration

## Managed-connector write-path command-envelope baseline

The `ENG-177` follow-through keeps managed-connector write-path command envelopes additive over
that same shared runtime surface instead of introducing a Debezium-only execution registry.

- `CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorCommandEnvelope` now publishes stable
  `not-applicable`, `blocked`, `operator-only`, `approval-gated`, and `engine-ready` posture for
  Debezium-managed runtimes together with command-envelope categories, the intended operation id,
  source coverage/remediation/governance/drift/action-plan/write-path-readiness/preflight/dry-run/
  execution-intent/execution-approval state, the current primary action id, source truth, target
  connector identity, deterministic command fingerprints, and safety flags
- the shared execution-runtime catalog now derives that command-envelope answer from the
  already-shipped execution-approval, execution-intent, dry-run, preflight, write-path readiness,
  governance, drift, remediation, and coverage truth, so observe-only, out-of-policy,
  blocking-remediation, future-control-plane, approval-gated, and engine-ready declarations can
  surface one consistent connector-management command-envelope answer without claiming Kafka
  Connect write paths or automatic connector mutation
- reconcile paths that still depend on a future control plane now surface `operator-only`
  command-envelope truth, lifecycle operations such as `pause` can surface `approval-gated` or
  `engine-ready` envelopes, and destructive operations such as `delete` surface explicit
  destructive-envelope truth before any future engine execution lane
- ASP.NET Core now maps `/engine/cdc-capture-runtimes/command-envelopes/{commandState}`,
  `/engine/cdc-capture-runtimes/command-envelopes/categories/{commandCategory}`, and
  `/engine/cdc-capture-runtimes/command-envelopes/operations/{operationId}` on the same shared
  runtime route family, so operator drill-down stays aligned with the engine-owned catalog
  instead of a Debezium-only endpoint family
- command envelopes currently remain read-only operator truth; they do not mean Cephalon already
  owns Kafka Connect write-path execution, automatic connector mutation, or managed-connector
  control-plane orchestration

## Not shipped in this slice

This pack intentionally still does not claim:

- Kafka Connect or Debezium REST API provisioning and reconciliation
- connector restart or pause management
- managed-connector apply-and-reconcile ownership beyond the shared `future-control-plane` governance signal
- automatic managed-connector drift correction or write-path remediation
- per-task execution graphs or hosted executions inside Cephalon
- schema-registry management or event serialization policy outside the shared CDC runtime metadata
- provider-native read/write storage or outbox implementation

These remain later slices so the current provider claim stays truthful.

## Related docs

- [Cephalon.Data](data.md)
- [Cephalon.Data.MongoDB](data-mongodb.md)
- [Cephalon.Data.Postgres](data-postgres.md)
- [Cephalon.Data.MySql](data-mysql.md)
- [Cephalon.Data.Oracle](data-oracle.md)
