# Cephalon.Data.Debezium

`Cephalon.Data.Debezium` is the Debezium-managed external CDC companion pack for Cephalon. It proves that the shared `Cephalon.Data` CDC runtime story also fits managed Kafka Connect or Debezium-style connector topologies where Cephalon does not own the runner, does not fake a hosted execution, and still publishes truthful capture ownership, external runtime reporting, reporter-lease posture, connector or task lifecycle posture, and operator drill-downs on the existing shared `/engine/cdc-*`, `/engine/runtime-story`, and `snapshot` surfaces.

## What it owns

- contributes Debezium-managed capture descriptors through `DebeziumCaptureOptions` and keeps those descriptors on the shared `/engine/cdc-captures*` catalog with `provider = "debezium"` and `mode = "managed-connector"`
- contributes external execution runtimes through `DebeziumConnectorOptions` and keeps those runtimes on the shared `/engine/cdc-capture-runtimes*` catalog with `executionOwnership = external-managed`, `executionTopology = managed-connector`, and `acknowledgementMode = connector-offset-commit`
- wires the shared external-reporting sink automatically when Debezium connectors are configured, so hosts that already add `Cephalon.Data` do not also need to remember `EnableExternalCdcRuntimeReporting = true` just to accept managed connector reports
- normalizes connector, task, and reconciliation metadata from external Debezium reports into stable `debezium*` metadata on the existing shared capture and execution-runtime surfaces instead of inventing a Debezium-only lifecycle registry
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

That keeps the runtime honest. `Cephalon.Data.Debezium` does not pretend Cephalon runs Kafka Connect tasks or connector worker loops itself, so it does not contribute a fake execution graph or hosted execution. The actual managed connector stays out of process and reports observations back into the shared runtime story through `POST /engine/cdc-capture-runtimes/{executionRuntimeId}/reports`, while this pack normalizes connector or task lifecycle and reconciliation detail into additive shared metadata instead of inventing a second Debezium operator catalog.

The slice also stays intentionally scoped. This pack does not claim Kafka Connect provisioning, Debezium REST management, connector lifecycle orchestration, schema-registry management, or provider-native read/write persistence. It exists to project truthful Debezium-managed capture and runtime topology onto the shared Cephalon CDC surfaces.

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

## Not shipped in this slice

This pack intentionally still does not claim:

- Kafka Connect or Debezium REST API provisioning and reconciliation
- connector restart or pause management
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
