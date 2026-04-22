# Cephalon.Data.Debezium

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Data.Debezium)
## Namespaces

- `Cephalon.Data.Debezium.Configuration`
- `Cephalon.Data.Debezium.Registration`

<a id="namespace-cephalon-data-debezium-configuration"></a>

## Namespace Cephalon.Data.Debezium.Configuration

<a id="type-cephalon-data-debezium-configuration-debeziumcaptureoptions"></a>

### `DebeziumCaptureOptions`

Declares one Debezium-managed CDC capture that should publish truth through the shared Cephalon CDC runtime surfaces.

#### Declaration
```csharp
public sealed class DebeziumCaptureOptions
```

#### Constructors

<a id="member-m-cephalon-data-debezium-configuration-debeziumcaptureoptions-ctor"></a>

##### `DebeziumCaptureOptions`

```csharp
DebeziumCaptureOptions()
```

#### Properties

<a id="member-p-cephalon-data-debezium-configuration-debeziumcaptureoptions-description"></a>

##### `Description`

```csharp
string Description { get; set; }
```

Gets or sets the human-readable CDC capture description.

<a id="member-p-cephalon-data-debezium-configuration-debeziumcaptureoptions-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

Gets or sets the operator-facing CDC capture name.

<a id="member-p-cephalon-data-debezium-configuration-debeziumcaptureoptions-eventformat"></a>

##### `EventFormat`

```csharp
string EventFormat { get; set; }
```

Gets or sets the operator-facing event format projected on the shared descriptor.

<a id="member-p-cephalon-data-debezium-configuration-debeziumcaptureoptions-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

Gets or sets the stable CDC capture identifier.

<a id="member-p-cephalon-data-debezium-configuration-debeziumcaptureoptions-metadata"></a>

##### `Metadata`

```csharp
IDictionary<string, string> Metadata { get; }
```

Gets arbitrary operator-facing metadata that should flow through the capture descriptor.

<a id="member-p-cephalon-data-debezium-configuration-debeziumcaptureoptions-mode"></a>

##### `Mode`

```csharp
string Mode { get; set; }
```

Gets or sets the operator-facing capture mode projected on the shared descriptor.

<a id="member-p-cephalon-data-debezium-configuration-debeziumcaptureoptions-outboxid"></a>

##### `OutboxId`

```csharp
string OutboxId { get; set; }
```

Gets or sets the outbox identifier that the external managed connector logically feeds.

<a id="member-p-cephalon-data-debezium-configuration-debeziumcaptureoptions-resourceids"></a>

##### `ResourceIds`

```csharp
IList<string> ResourceIds { get; }
```

Gets the resource identifiers observed by the capture, such as tables, topics, or collections.

<a id="member-p-cephalon-data-debezium-configuration-debeziumcaptureoptions-snapshotmode"></a>

##### `SnapshotMode`

```csharp
string SnapshotMode { get; set; }
```

Gets or sets the Debezium snapshot mode when the pack should publish it as operator-facing metadata.

<a id="member-p-cephalon-data-debezium-configuration-debeziumcaptureoptions-sourceid"></a>

##### `SourceId`

```csharp
string SourceId { get; set; }
```

Gets or sets the logical source identifier when it should differ from the derived connector or topic path.

<a id="member-p-cephalon-data-debezium-configuration-debeziumcaptureoptions-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; set; }
```

Gets or sets the module identifier that owns the capture surface.

<a id="member-p-cephalon-data-debezium-configuration-debeziumcaptureoptions-tags"></a>

##### `Tags`

```csharp
IList<string> Tags { get; }
```

Gets the descriptive tags associated with the capture.

<a id="member-p-cephalon-data-debezium-configuration-debeziumcaptureoptions-topicname"></a>

##### `TopicName`

```csharp
string TopicName { get; set; }
```

Gets or sets the external topic name that carries the Debezium envelope for this capture.

<a id="type-cephalon-data-debezium-configuration-debeziumconnectoroptions"></a>

### `DebeziumConnectorOptions`

Declares one external Debezium-managed connector runtime for the active Cephalon data runtime.

#### Declaration
```csharp
public sealed class DebeziumConnectorOptions
```

#### Constructors

<a id="member-m-cephalon-data-debezium-configuration-debeziumconnectoroptions-ctor"></a>

##### `DebeziumConnectorOptions`

```csharp
DebeziumConnectorOptions()
```

#### Properties

<a id="member-p-cephalon-data-debezium-configuration-debeziumconnectoroptions-acknowledgementmode"></a>

##### `AcknowledgementMode`

```csharp
string AcknowledgementMode { get; set; }
```

Gets or sets the acknowledgement mode published for the connector runtime.

<a id="member-p-cephalon-data-debezium-configuration-debeziumconnectoroptions-cdccaptures"></a>

##### `CdcCaptures`

```csharp
IList<DebeziumCaptureOptions> CdcCaptures { get; }
```

Gets the Debezium-managed CDC captures that should bind to this connector runtime.

<a id="member-p-cephalon-data-debezium-configuration-debeziumconnectoroptions-connectclusterid"></a>

##### `ConnectClusterId`

```csharp
string ConnectClusterId { get; set; }
```

Gets or sets the operator-facing Kafka Connect or Debezium cluster identifier that owns the connector.

<a id="member-p-cephalon-data-debezium-configuration-debeziumconnectoroptions-connectorclass"></a>

##### `ConnectorClass`

```csharp
string ConnectorClass { get; set; }
```

Gets or sets the Debezium connector-class identifier when the runtime should publish it on shared operator surfaces.

<a id="member-p-cephalon-data-debezium-configuration-debeziumconnectoroptions-description"></a>

##### `Description`

```csharp
string Description { get; set; }
```

Gets or sets the human-readable connector description.

<a id="member-p-cephalon-data-debezium-configuration-debeziumconnectoroptions-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

Gets or sets the operator-facing connector name.

<a id="member-p-cephalon-data-debezium-configuration-debeziumconnectoroptions-edgenodeids"></a>

##### `EdgeNodeIds`

```csharp
IList<string> EdgeNodeIds { get; }
```

Gets the declared edge-node identifiers that can originate observations for the managed connector runtime.

<a id="member-p-cephalon-data-debezium-configuration-debeziumconnectoroptions-executionownership"></a>

##### `ExecutionOwnership`

```csharp
string ExecutionOwnership { get; set; }
```

Gets or sets the execution-ownership mode published for the connector runtime.

<a id="member-p-cephalon-data-debezium-configuration-debeziumconnectoroptions-executiontopology"></a>

##### `ExecutionTopology`

```csharp
string ExecutionTopology { get; set; }
```

Gets or sets the execution-topology classification published for the connector runtime.

<a id="member-p-cephalon-data-debezium-configuration-debeziumconnectoroptions-expectedtaskcount"></a>

##### `ExpectedTaskCount`

```csharp
int? ExpectedTaskCount { get; set; }
```

Gets or sets the expected Debezium task count when the connector should publish that expectation even if task ids are not declared individually.

<a id="member-p-cephalon-data-debezium-configuration-debeziumconnectoroptions-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

Gets or sets the stable execution-runtime identifier for the managed connector.

<a id="member-p-cephalon-data-debezium-configuration-debeziumconnectoroptions-managementmode"></a>

##### `ManagementMode`

```csharp
string ManagementMode { get; set; }
```

Gets or sets the operator-facing lifecycle-management mode published for the connector runtime.

<a id="member-p-cephalon-data-debezium-configuration-debeziumconnectoroptions-metadata"></a>

##### `Metadata`

```csharp
IDictionary<string, string> Metadata { get; }
```

Gets arbitrary operator-facing metadata that should flow through the execution-runtime descriptor.

<a id="member-p-cephalon-data-debezium-configuration-debeziumconnectoroptions-observationstaleafterseconds"></a>

##### `ObservationStaleAfterSeconds`

```csharp
int? ObservationStaleAfterSeconds { get; set; }
```

Gets or sets the report-freshness window, in seconds, used to mark connector observations stale.

<a id="member-p-cephalon-data-debezium-configuration-debeziumconnectoroptions-rejectconflictingreporterids"></a>

##### `RejectConflictingReporterIds`

```csharp
bool RejectConflictingReporterIds { get; set; }
```

Gets or sets a value indicating whether the connector rejects conflicting reporter identities while an active lease still exists.

<a id="member-p-cephalon-data-debezium-configuration-debeziumconnectoroptions-rejectoutoforderreports"></a>

##### `RejectOutOfOrderReports`

```csharp
bool RejectOutOfOrderReports { get; set; }
```

Gets or sets a value indicating whether the connector rejects out-of-order external reports.

<a id="member-p-cephalon-data-debezium-configuration-debeziumconnectoroptions-reporterleaseseconds"></a>

##### `ReporterLeaseSeconds`

```csharp
int? ReporterLeaseSeconds { get; set; }
```

Gets or sets the reporter-lease window, in seconds, used to keep one reporter authoritative for the connector.

<a id="member-p-cephalon-data-debezium-configuration-debeziumconnectoroptions-sourceproviderid"></a>

##### `SourceProviderId`

```csharp
string SourceProviderId { get; set; }
```

Gets or sets the upstream provider identifier behind the managed connector, such as `postgresql` or `sqlserver`.

<a id="member-p-cephalon-data-debezium-configuration-debeziumconnectoroptions-taskids"></a>

##### `TaskIds`

```csharp
IList<string> TaskIds { get; }
```

Gets the declared task identifiers that belong to the managed connector runtime.

<a id="member-p-cephalon-data-debezium-configuration-debeziumconnectoroptions-topicprefix"></a>

##### `TopicPrefix`

```csharp
string TopicPrefix { get; set; }
```

Gets or sets the Debezium topic prefix when the connector fans out into one or more topics.

<a id="type-cephalon-data-debezium-configuration-debeziumdataoptions"></a>

### `DebeziumDataOptions`

Configuration options for the Debezium-managed external CDC pack (`Engine:Data:Debezium`).

#### Declaration
```csharp
public sealed class DebeziumDataOptions
```

#### Constructors

<a id="member-m-cephalon-data-debezium-configuration-debeziumdataoptions-ctor"></a>

##### `DebeziumDataOptions`

```csharp
DebeziumDataOptions()
```

#### Fields

<a id="member-f-cephalon-data-debezium-configuration-debeziumdataoptions-providerid"></a>

##### `ProviderId`

```csharp
const string ProviderId
```

Gets the canonical provider identifier emitted by the pack.

<a id="member-f-cephalon-data-debezium-configuration-debeziumdataoptions-sectionpath"></a>

##### `SectionPath`

```csharp
const string SectionPath
```

Gets the default configuration section path for Debezium data settings.

#### Properties

<a id="member-p-cephalon-data-debezium-configuration-debeziumdataoptions-connectors"></a>

##### `Connectors`

```csharp
IList<DebeziumConnectorOptions> Connectors { get; }
```

Gets the Debezium-managed connector runtimes that should contribute captures and external execution ownership to the active runtime.

<a id="namespace-cephalon-data-debezium-registration"></a>

## Namespace Cephalon.Data.Debezium.Registration

<a id="type-cephalon-data-debezium-registration-debeziumdataenginebuilderextensions"></a>

### `DebeziumDataEngineBuilderExtensions`

Registers the Debezium-managed external CDC companion pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class DebeziumDataEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-data-debezium-registration-debeziumdataenginebuilderextensions-adddebeziumdata-cephalon-engine-composition-enginebuilder-system-action-cephalon-data-debezium-configuration-debeziumdataoptions"></a>

##### `AddDebeziumData`

```csharp
EngineBuilder AddDebeziumData(this EngineBuilder builder, Action<DebeziumDataOptions> configure)
```

Adds the Debezium-managed external CDC pack using an options callback that can bind from configuration.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: The callback that configures the host-owned Debezium pack options.
