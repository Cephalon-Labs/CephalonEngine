# Cephalon.Data

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Data)
## Namespaces

- `Cephalon.Data.Configuration`
- `Cephalon.Data.Registration`
- `Cephalon.Data.Services`

<a id="namespace-cephalon-data-configuration"></a>

## Namespace Cephalon.Data.Configuration

<a id="type-cephalon-data-configuration-cdccaptureexecutionruntimeoptions"></a>

### `CdcCaptureExecutionRuntimeOptions`

Configures one host-owned CDC execution runtime declaration for the runtime-neutral data pack.

Remarks: These options seed additional operator-facing execution-runtime surfaces. Installed modules and companion packs can still contribute runtimes through `ICdcCaptureExecutionRuntimeContributor`.

#### Declaration
```csharp
public sealed class CdcCaptureExecutionRuntimeOptions
```

#### Constructors

<a id="member-m-cephalon-data-configuration-cdccaptureexecutionruntimeoptions-ctor"></a>

##### `CdcCaptureExecutionRuntimeOptions`

```csharp
CdcCaptureExecutionRuntimeOptions()
```

Creates CDC execution runtime options with empty identity fields and a declared-runtime topology.

#### Properties

<a id="member-p-cephalon-data-configuration-cdccaptureexecutionruntimeoptions-acknowledgementmode"></a>

##### `AcknowledgementMode`

```csharp
string AcknowledgementMode { get; set; }
```

Gets or sets the operator-facing acknowledgement mode when the runtime reports one.

<a id="member-p-cephalon-data-configuration-cdccaptureexecutionruntimeoptions-cdccaptureids"></a>

##### `CdcCaptureIds`

```csharp
IList<string> CdcCaptureIds { get; }
```

Gets the CDC capture identifiers explicitly owned by the runtime when ownership is bounded to a known capture set.

<a id="member-p-cephalon-data-configuration-cdccaptureexecutionruntimeoptions-description"></a>

##### `Description`

```csharp
string Description { get; set; }
```

Gets or sets the human-readable execution-runtime description.

<a id="member-p-cephalon-data-configuration-cdccaptureexecutionruntimeoptions-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

Gets or sets the operator-facing execution-runtime name.

<a id="member-p-cephalon-data-configuration-cdccaptureexecutionruntimeoptions-edgenodeids"></a>

##### `EdgeNodeIds`

```csharp
IList<string> EdgeNodeIds { get; }
```

Gets the declared edge-node identifiers that can originate observations for the runtime.

<a id="member-p-cephalon-data-configuration-cdccaptureexecutionruntimeoptions-executiongraphid"></a>

##### `ExecutionGraphId`

```csharp
string ExecutionGraphId { get; set; }
```

Gets or sets the execution-graph identifier when the runtime maps to a Cephalon execution graph.

<a id="member-p-cephalon-data-configuration-cdccaptureexecutionruntimeoptions-executionownership"></a>

##### `ExecutionOwnership`

```csharp
string ExecutionOwnership { get; set; }
```

Gets or sets the operator-facing ownership mode for the runtime.

<a id="member-p-cephalon-data-configuration-cdccaptureexecutionruntimeoptions-executiontopology"></a>

##### `ExecutionTopology`

```csharp
string ExecutionTopology { get; set; }
```

Gets or sets the operator-facing topology classification for the runtime.

<a id="member-p-cephalon-data-configuration-cdccaptureexecutionruntimeoptions-hostedexecutionid"></a>

##### `HostedExecutionId`

```csharp
string HostedExecutionId { get; set; }
```

Gets or sets the hosted-execution identifier when the runtime maps to a Cephalon hosted execution.

<a id="member-p-cephalon-data-configuration-cdccaptureexecutionruntimeoptions-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

Gets or sets the stable execution-runtime identifier.

<a id="member-p-cephalon-data-configuration-cdccaptureexecutionruntimeoptions-metadata"></a>

##### `Metadata`

```csharp
IDictionary<string, string> Metadata { get; }
```

Gets arbitrary operator-facing metadata that should flow through the runtime declaration.

<a id="member-p-cephalon-data-configuration-cdccaptureexecutionruntimeoptions-observationstaleafterseconds"></a>

##### `ObservationStaleAfterSeconds`

```csharp
int? ObservationStaleAfterSeconds { get; set; }
```

Gets or sets the report-freshness window, in seconds, used to mark external runtime observations stale.

<a id="member-p-cephalon-data-configuration-cdccaptureexecutionruntimeoptions-rejectconflictingreporterids"></a>

##### `RejectConflictingReporterIds`

```csharp
bool RejectConflictingReporterIds { get; set; }
```

Gets or sets a value indicating whether the runtime should reject reports from conflicting reporter identities while an active lease still exists.

<a id="member-p-cephalon-data-configuration-cdccaptureexecutionruntimeoptions-rejectoutoforderreports"></a>

##### `RejectOutOfOrderReports`

```csharp
bool RejectOutOfOrderReports { get; set; }
```

Gets or sets a value indicating whether the runtime should reject out-of-order external reports.

<a id="member-p-cephalon-data-configuration-cdccaptureexecutionruntimeoptions-reporterleaseseconds"></a>

##### `ReporterLeaseSeconds`

```csharp
int? ReporterLeaseSeconds { get; set; }
```

Gets or sets the reporter-lease window, in seconds, used to keep one external reporter authoritative for the runtime.

<a id="type-cephalon-data-configuration-dataruntimeoptions"></a>

### `DataRuntimeOptions`

Describes the host-owned options for the runtime-neutral Cephalon data pack.

#### Declaration
```csharp
public sealed class DataRuntimeOptions
```

#### Constructors

<a id="member-m-cephalon-data-configuration-dataruntimeoptions-ctor"></a>

##### `DataRuntimeOptions`

```csharp
DataRuntimeOptions()
```

Initializes a new instance of the `DataRuntimeOptions` class.

#### Properties

<a id="member-p-cephalon-data-configuration-dataruntimeoptions-cdcexecutionruntimes"></a>

##### `CdcExecutionRuntimes`

```csharp
IList<CdcCaptureExecutionRuntimeOptions> CdcExecutionRuntimes { get; }
```

Gets the host-defined CDC execution runtimes that should be available to the active data runtime.

<a id="member-p-cephalon-data-configuration-dataruntimeoptions-cdcpollingintervalseconds"></a>

##### `CdcPollingIntervalSeconds`

```csharp
int CdcPollingIntervalSeconds { get; set; }
```

Gets or sets the polling interval, in seconds, used by the shared CDC hosted execution pump.

<a id="member-p-cephalon-data-configuration-dataruntimeoptions-enablecdcexecution"></a>

##### `EnableCdcExecution`

```csharp
bool EnableCdcExecution { get; set; }
```

Gets or sets a value indicating whether the pack should register the shared CDC hosted execution pump.

<a id="member-p-cephalon-data-configuration-dataruntimeoptions-enableexternalcdcruntimereporting"></a>

##### `EnableExternalCdcRuntimeReporting`

```csharp
bool EnableExternalCdcRuntimeReporting { get; set; }
```

Gets or sets a value indicating whether the pack should accept external CDC execution-runtime reports through the shared runtime-state catalog.

<a id="member-p-cephalon-data-configuration-dataruntimeoptions-enablemanagedconnectorautomaticretryexecution"></a>

##### `EnableManagedConnectorAutomaticRetryExecution`

```csharp
bool EnableManagedConnectorAutomaticRetryExecution { get; set; }
```

Gets or sets a value indicating whether the pack should run the shared automatic managed-connector background retry lane.

<a id="member-p-cephalon-data-configuration-dataruntimeoptions-managedconnectorautomaticretrycoordinationownerid"></a>

##### `ManagedConnectorAutomaticRetryCoordinationOwnerId`

```csharp
string ManagedConnectorAutomaticRetryCoordinationOwnerId { get; set; }
```

Gets or sets the host-owned local coordination owner identifier used to decide whether the current node can run automatic managed-connector retries.

<a id="member-p-cephalon-data-configuration-dataruntimeoptions-managedconnectorautomaticretrypollingintervalseconds"></a>

##### `ManagedConnectorAutomaticRetryPollingIntervalSeconds`

```csharp
int ManagedConnectorAutomaticRetryPollingIntervalSeconds { get; set; }
```

Gets or sets the polling interval, in seconds, used by the shared automatic managed-connector background retry lane.

<a id="member-p-cephalon-data-configuration-dataruntimeoptions-registerreadstore"></a>

##### `RegisterReadStore`

```csharp
bool RegisterReadStore { get; set; }
```

Gets or sets a value indicating whether the pack should register the default read-store dispatcher.

<a id="member-p-cephalon-data-configuration-dataruntimeoptions-registerwritestore"></a>

##### `RegisterWriteStore`

```csharp
bool RegisterWriteStore { get; set; }
```

Gets or sets a value indicating whether the pack should register the default write-store dispatcher.

<a id="namespace-cephalon-data-registration"></a>

## Namespace Cephalon.Data.Registration

<a id="type-cephalon-data-registration-dataenginebuilderextensions"></a>

### `DataEngineBuilderExtensions`

Registers the runtime-neutral data pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class DataEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-data-registration-dataenginebuilderextensions-adddata-cephalon-engine-composition-enginebuilder-system-action-cephalon-data-configuration-dataruntimeoptions"></a>

##### `AddData`

```csharp
EngineBuilder AddData(this EngineBuilder builder, Action<DataRuntimeOptions> configure)
```

Adds the data runtime pack to the engine.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: An optional callback that configures the host-owned data runtime options.

<a id="namespace-cephalon-data-services"></a>

## Namespace Cephalon.Data.Services

<a id="type-cephalon-data-services-cdccaptureexecutionreport"></a>

### `CdcCaptureExecutionReport`

Describes one reported runtime observation for a CDC capture.

#### Declaration
```csharp
public sealed class CdcCaptureExecutionReport
```

#### Constructors

<a id="member-m-cephalon-data-services-cdccaptureexecutionreport-ctor-system-string-system-string-system-datetimeoffset-system-string-system-int32-system-int32-system-string-system-string-system-string-cephalon-abstractions-data-cdccapturefreshnessstatus-cephalon-abstractions-data-cdccapturefreshnessstatus-cephalon-abstractions-data-cdccapturelagstatus-cephalon-abstractions-data-cdccapturepublicationstatus-system-collections-generic-ireadonlydictionary-system-string-system-string-system-string-system-string"></a>

##### `CdcCaptureExecutionReport`

```csharp
CdcCaptureExecutionReport(string cdcCaptureId, string outcome, DateTimeOffset observedAtUtc, string reportId, int capturedChangeCount, int producedMessageCount, string changeId, string checkpoint, string error, CdcCaptureFreshnessStatus freshness, CdcCaptureFreshnessStatus observationFreshness, CdcCaptureLagStatus lag, CdcCapturePublicationStatus publication, IReadOnlyDictionary<string, string> metadata, string reporterId, string edgeNodeId)
```

Creates a new CDC capture runtime observation.

Parameters:
- `cdcCaptureId`: The stable CDC capture identifier that produced the observation.
- `outcome`: The stable outcome identifier, such as `started`, `captured`, `idle`, or `failed`.
- `observedAtUtc`: The UTC timestamp when the observation occurred.
- `reportId`: The optional stable report identifier used to make repeated submissions idempotent.
- `capturedChangeCount`: The number of source changes observed by this report.
- `producedMessageCount`: The number of outbox messages produced by this report.
- `changeId`: The latest provider-facing change identifier when available.
- `checkpoint`: The latest provider-facing checkpoint or cursor when available.
- `error`: The operator-facing error summary when the observation represents a failure.
- `freshness`: An optional typed freshness answer reported by the active provider/runtime.
- `observationFreshness`: An optional report-freshness answer describing whether the observation itself is still current.
- `lag`: An optional typed lag answer reported by the active provider/runtime.
- `publication`: An optional typed publication-posture answer reported by the active provider/runtime.
- `metadata`: Optional operator-facing metadata captured alongside the observation.
- `reporterId`: The optional stable reporter identity that submitted the observation.
- `edgeNodeId`: The optional stable edge-node identifier that originated the observation.

#### Properties

<a id="member-p-cephalon-data-services-cdccaptureexecutionreport-capturedchangecount"></a>

##### `CapturedChangeCount`

```csharp
int CapturedChangeCount { get; }
```

Gets the number of source changes observed by this report.

<a id="member-p-cephalon-data-services-cdccaptureexecutionreport-cdccaptureid"></a>

##### `CdcCaptureId`

```csharp
string CdcCaptureId { get; }
```

Gets the stable CDC capture identifier that produced the observation.

<a id="member-p-cephalon-data-services-cdccaptureexecutionreport-changeid"></a>

##### `ChangeId`

```csharp
string ChangeId { get; }
```

Gets the latest provider-facing change identifier when one was reported.

<a id="member-p-cephalon-data-services-cdccaptureexecutionreport-checkpoint"></a>

##### `Checkpoint`

```csharp
string Checkpoint { get; }
```

Gets the latest provider-facing checkpoint or cursor when one was reported.

<a id="member-p-cephalon-data-services-cdccaptureexecutionreport-edgenodeid"></a>

##### `EdgeNodeId`

```csharp
string EdgeNodeId { get; }
```

Gets the stable edge-node identifier that originated the observation when one was supplied.

<a id="member-p-cephalon-data-services-cdccaptureexecutionreport-error"></a>

##### `Error`

```csharp
string Error { get; }
```

Gets the operator-facing error summary when the observation represents a failure.

<a id="member-p-cephalon-data-services-cdccaptureexecutionreport-freshness"></a>

##### `Freshness`

```csharp
CdcCaptureFreshnessStatus Freshness { get; }
```

Gets the typed freshness answer reported by the active provider/runtime when one was supplied.

<a id="member-p-cephalon-data-services-cdccaptureexecutionreport-lag"></a>

##### `Lag`

```csharp
CdcCaptureLagStatus Lag { get; }
```

Gets the typed lag answer reported by the active provider/runtime when one was supplied.

<a id="member-p-cephalon-data-services-cdccaptureexecutionreport-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata captured alongside the observation.

<a id="member-p-cephalon-data-services-cdccaptureexecutionreport-observationfreshness"></a>

##### `ObservationFreshness`

```csharp
CdcCaptureFreshnessStatus ObservationFreshness { get; }
```

Gets the typed report-freshness answer reported for the observation itself when one was supplied.

<a id="member-p-cephalon-data-services-cdccaptureexecutionreport-observedatutc"></a>

##### `ObservedAtUtc`

```csharp
DateTimeOffset ObservedAtUtc { get; }
```

Gets the UTC timestamp when the observation occurred.

<a id="member-p-cephalon-data-services-cdccaptureexecutionreport-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; }
```

Gets the stable outcome identifier for the observed CDC activity.

<a id="member-p-cephalon-data-services-cdccaptureexecutionreport-producedmessagecount"></a>

##### `ProducedMessageCount`

```csharp
int ProducedMessageCount { get; }
```

Gets the number of outbox messages produced by this report.

<a id="member-p-cephalon-data-services-cdccaptureexecutionreport-publication"></a>

##### `Publication`

```csharp
CdcCapturePublicationStatus Publication { get; }
```

Gets the typed publication-posture answer reported by the active provider/runtime when one was supplied.

<a id="member-p-cephalon-data-services-cdccaptureexecutionreport-reporterid"></a>

##### `ReporterId`

```csharp
string ReporterId { get; }
```

Gets the stable reporter identity that submitted the observation when one was supplied.

<a id="member-p-cephalon-data-services-cdccaptureexecutionreport-reportid"></a>

##### `ReportId`

```csharp
string ReportId { get; }
```

Gets the optional stable report identifier used to make repeated submissions idempotent.

<a id="type-cephalon-data-services-cdccaptureruntimeoutcomes"></a>

### `CdcCaptureRuntimeOutcomes`

Defines the stable outcome identifiers used when reporting CDC capture activity.

#### Declaration
```csharp
public static class CdcCaptureRuntimeOutcomes
```

#### Fields

<a id="member-f-cephalon-data-services-cdccaptureruntimeoutcomes-captured"></a>

##### `Captured`

```csharp
const string Captured
```

Gets the outcome identifier used when a capture observes one or more source changes.

<a id="member-f-cephalon-data-services-cdccaptureruntimeoutcomes-failed"></a>

##### `Failed`

```csharp
const string Failed
```

Gets the outcome identifier used when a capture fails.

<a id="member-f-cephalon-data-services-cdccaptureruntimeoutcomes-idle"></a>

##### `Idle`

```csharp
const string Idle
```

Gets the outcome identifier used when a capture polls successfully but finds no new changes.

<a id="member-f-cephalon-data-services-cdccaptureruntimeoutcomes-started"></a>

##### `Started`

```csharp
const string Started
```

Gets the outcome identifier used when a capture runtime starts or resumes work.

<a id="type-cephalon-data-services-icdccaptureexecutionruntimecontributor"></a>

### `ICdcCaptureExecutionRuntimeContributor`

Contributes one or more operator-facing CDC capture execution runtimes to the active data runtime.

#### Declaration
```csharp
public interface ICdcCaptureExecutionRuntimeContributor
```

#### Methods

<a id="member-m-cephalon-data-services-icdccaptureexecutionruntimecontributor-registerexecutionruntimes-cephalon-data-services-icdccaptureexecutionruntimeregistry"></a>

##### `RegisterExecutionRuntimes`

```csharp
void RegisterExecutionRuntimes(ICdcCaptureExecutionRuntimeRegistry executionRuntimes)
```

Registers one or more CDC capture execution runtime descriptors owned by the contributor.

Parameters:
- `executionRuntimes`: The execution-runtime registry receiving contributed descriptors.

<a id="type-cephalon-data-services-icdccaptureexecutionruntimeregistry"></a>

### `ICdcCaptureExecutionRuntimeRegistry`

Receives operator-facing CDC capture execution runtime descriptors contributed by active data packs.

#### Declaration
```csharp
public interface ICdcCaptureExecutionRuntimeRegistry
```

#### Methods

<a id="member-m-cephalon-data-services-icdccaptureexecutionruntimeregistry-add-cephalon-abstractions-data-cdccaptureexecutionruntimedescriptor"></a>

##### `Add`

```csharp
void Add(CdcCaptureExecutionRuntimeDescriptor executionRuntime)
```

Adds one CDC capture execution runtime to the current data-runtime composition.

Parameters:
- `executionRuntime`: The execution runtime to register.

<a id="type-cephalon-data-services-icdccaptureruntimereporter"></a>

### `ICdcCaptureRuntimeReporter`

Accepts operator-facing CDC runtime observations for active capture surfaces.

#### Declaration
```csharp
public interface ICdcCaptureRuntimeReporter
```

#### Methods

<a id="member-m-cephalon-data-services-icdccaptureruntimereporter-reportasync-cephalon-data-services-cdccaptureexecutionreport-system-threading-cancellationtoken"></a>

##### `ReportAsync`

```csharp
ValueTask ReportAsync(CdcCaptureExecutionReport report, CancellationToken cancellationToken)
```

Reports one CDC runtime observation for the active runtime.

Returns: A task that completes when the observation has been recorded.

Parameters:
- `report`: The CDC runtime observation to capture.
- `cancellationToken`: The token that cancels the operation.
