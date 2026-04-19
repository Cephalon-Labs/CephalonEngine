# Cephalon.Data

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Data)
## Namespaces

- `Cephalon.Data.Configuration`
- `Cephalon.Data.Registration`
- `Cephalon.Data.Services`

<a id="namespace-cephalon-data-configuration"></a>

## Namespace Cephalon.Data.Configuration

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

<a id="member-m-cephalon-data-services-cdccaptureexecutionreport-ctor-system-string-system-string-system-datetimeoffset-system-int32-system-int32-system-string-system-string-system-string-cephalon-abstractions-data-cdccapturefreshnessstatus-cephalon-abstractions-data-cdccapturelagstatus-cephalon-abstractions-data-cdccapturepublicationstatus-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `CdcCaptureExecutionReport`

```csharp
CdcCaptureExecutionReport(string cdcCaptureId, string outcome, DateTimeOffset observedAtUtc, int capturedChangeCount, int producedMessageCount, string changeId, string checkpoint, string error, CdcCaptureFreshnessStatus freshness, CdcCaptureLagStatus lag, CdcCapturePublicationStatus publication, IReadOnlyDictionary<string, string> metadata)
```

Creates a new CDC capture runtime observation.

Parameters:
- `cdcCaptureId`: The stable CDC capture identifier that produced the observation.
- `outcome`: The stable outcome identifier, such as `started`, `captured`, `idle`, or `failed`.
- `observedAtUtc`: The UTC timestamp when the observation occurred.
- `capturedChangeCount`: The number of source changes observed by this report.
- `producedMessageCount`: The number of outbox messages produced by this report.
- `changeId`: The latest provider-facing change identifier when available.
- `checkpoint`: The latest provider-facing checkpoint or cursor when available.
- `error`: The operator-facing error summary when the observation represents a failure.
- `freshness`: An optional typed freshness answer reported by the active provider/runtime.
- `lag`: An optional typed lag answer reported by the active provider/runtime.
- `publication`: An optional typed publication-posture answer reported by the active provider/runtime.
- `metadata`: Optional operator-facing metadata captured alongside the observation.

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
