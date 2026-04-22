# Cephalon.Data.Oracle

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Data.Oracle)
## Namespaces

- `Cephalon.Data.Oracle.Configuration`
- `Cephalon.Data.Oracle.Registration`

<a id="namespace-cephalon-data-oracle-configuration"></a>

## Namespace Cephalon.Data.Oracle.Configuration

<a id="type-cephalon-data-oracle-configuration-oracledataoptions"></a>

### `OracleDataOptions`

Configuration options for the Oracle data provider (`Engine:Data:Oracle`).

#### Declaration
```csharp
public sealed class OracleDataOptions
```

#### Constructors

<a id="member-m-cephalon-data-oracle-configuration-oracledataoptions-ctor"></a>

##### `OracleDataOptions`

```csharp
OracleDataOptions()
```

#### Fields

<a id="member-f-cephalon-data-oracle-configuration-oracledataoptions-providerid"></a>

##### `ProviderId`

```csharp
const string ProviderId
```

Gets the canonical provider identifier emitted by the pack.

<a id="member-f-cephalon-data-oracle-configuration-oracledataoptions-sectionpath"></a>

##### `SectionPath`

```csharp
const string SectionPath
```

Gets the configuration section path used by default for Oracle data settings.

#### Properties

<a id="member-p-cephalon-data-oracle-configuration-oracledataoptions-cdccaptures"></a>

##### `CdcCaptures`

```csharp
IList<OracleLogMinerCaptureOptions> CdcCaptures { get; }
```

Gets the provider-native Oracle LogMiner captures that should be contributed to the active runtime.

<a id="member-p-cephalon-data-oracle-configuration-oracledataoptions-checkpointtablename"></a>

##### `CheckpointTableName`

```csharp
string CheckpointTableName { get; set; }
```

Gets or sets the Cephalon-managed checkpoint table name used for durable Oracle LogMiner progress.

<a id="member-p-cephalon-data-oracle-configuration-oracledataoptions-connectionstring"></a>

##### `ConnectionString`

```csharp
string ConnectionString { get; set; }
```

Gets or sets the inline Oracle connection string.

Remarks: Use either `ConnectionStringName` or `ConnectionString`.

<a id="member-p-cephalon-data-oracle-configuration-oracledataoptions-connectionstringname"></a>

##### `ConnectionStringName`

```csharp
string ConnectionStringName { get; set; }
```

Gets or sets the root `ConnectionStrings` entry name to resolve for Oracle.

Remarks: Use either `ConnectionStringName` or `ConnectionString`.

<a id="member-p-cephalon-data-oracle-configuration-oracledataoptions-databasename"></a>

##### `DatabaseName`

```csharp
string DatabaseName { get; set; }
```

Gets or sets the operator-facing database name that owns the configured LogMiner captures.

<a id="type-cephalon-data-oracle-configuration-oraclelogminercaptureoptions"></a>

### `OracleLogMinerCaptureOptions`

Declares one provider-native Oracle LogMiner capture for the active runtime.

#### Declaration
```csharp
public sealed class OracleLogMinerCaptureOptions
```

#### Constructors

<a id="member-m-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-ctor"></a>

##### `OracleLogMinerCaptureOptions`

```csharp
OracleLogMinerCaptureOptions()
```

#### Properties

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-channelid"></a>

##### `ChannelId`

```csharp
string ChannelId { get; set; }
```

Gets or sets the logical outbox channel that receives emitted publications.

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-description"></a>

##### `Description`

```csharp
string Description { get; set; }
```

Gets or sets the human-readable CDC capture description.

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

Gets or sets the operator-facing CDC capture name.

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-eventformat"></a>

##### `EventFormat`

```csharp
string EventFormat { get; set; }
```

Gets or sets the operator-facing event format projected on the CDC descriptor.

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-expecteddatabaseid"></a>

##### `ExpectedDatabaseId`

```csharp
decimal? ExpectedDatabaseId { get; set; }
```

Gets or sets the expected Oracle database identifier when the capture should fail fast if the runtime connects to a different upstream.

Remarks: Leave this unset when the capture should observe Oracle database identity for diagnostics only. When set, the provider-native runner validates the live `DBID` before it starts or resumes LogMiner execution.

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-expecteddatabaseuniquename"></a>

##### `ExpectedDatabaseUniqueName`

```csharp
string ExpectedDatabaseUniqueName { get; set; }
```

Gets or sets the expected Oracle database unique name when the capture should fail fast if the runtime connects to a different upstream.

Remarks: Leave this blank when the capture should observe Oracle database identity for diagnostics only. When set, the provider-native runner validates the live `DB_UNIQUE_NAME` before it starts or resumes LogMiner execution.

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

Gets or sets the stable CDC capture identifier.

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-initialposition"></a>

##### `InitialPosition`

```csharp
string InitialPosition { get; set; }
```

Gets or sets the initial position used when no durable checkpoint exists yet.

Remarks: Supported values are `latest-available` and `earliest-available`.

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-maxawaittimeseconds"></a>

##### `MaxAwaitTimeSeconds`

```csharp
int MaxAwaitTimeSeconds { get; set; }
```

Gets or sets the maximum number of seconds to await committed redo during one provider-native iteration.

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-maxchangesperread"></a>

##### `MaxChangesPerRead`

```csharp
int MaxChangesPerRead { get; set; }
```

Gets or sets the maximum number of captured row changes to stage during one provider-native iteration.

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-messagetype"></a>

##### `MessageType`

```csharp
string MessageType { get; set; }
```

Gets or sets the logical message type emitted for each captured change event.

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-metadata"></a>

##### `Metadata`

```csharp
IDictionary<string, string> Metadata { get; }
```

Gets arbitrary operator-facing metadata that should flow through the capture descriptor.

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-outboxid"></a>

##### `OutboxId`

```csharp
string OutboxId { get; set; }
```

Gets or sets the outbox identifier that receives emitted publications.

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-pollingintervalseconds"></a>

##### `PollingIntervalSeconds`

```csharp
int PollingIntervalSeconds { get; set; }
```

Gets or sets the polling interval, in seconds, between hosted-service iterations.

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-resourceids"></a>

##### `ResourceIds`

```csharp
IList<string> ResourceIds { get; }
```

Gets the resource identifiers observed by the capture.

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-resumefromearliestavailablescnifcheckpointunavailable"></a>

##### `ResumeFromEarliestAvailableScnIfCheckpointUnavailable`

```csharp
bool ResumeFromEarliestAvailableScnIfCheckpointUnavailable { get; set; }
```

Gets or sets a value indicating whether the provider-native runner should reseed from the earliest retained SCN when a durable checkpoint is older than the retained archive-log window.

Remarks: The default is `false` so Oracle LogMiner fails fast instead of silently skipping the gap between the durable checkpoint and the earliest retained archive-log SCN.

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-sourceid"></a>

##### `SourceId`

```csharp
string SourceId { get; set; }
```

Gets or sets the logical source identifier when it should differ from the watched table path.

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; set; }
```

Gets or sets the module identifier that owns the capture surface.

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-tablename"></a>

##### `TableName`

```csharp
string TableName { get; set; }
```

Gets or sets the table name of the tracked table.

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-tableschema"></a>

##### `TableSchema`

```csharp
string TableSchema { get; set; }
```

Gets or sets the Oracle schema name of the tracked table.

<a id="member-p-cephalon-data-oracle-configuration-oraclelogminercaptureoptions-tags"></a>

##### `Tags`

```csharp
IList<string> Tags { get; }
```

Gets the descriptive tags associated with the capture.

<a id="namespace-cephalon-data-oracle-registration"></a>

## Namespace Cephalon.Data.Oracle.Registration

<a id="type-cephalon-data-oracle-registration-oracledataenginebuilderextensions"></a>

### `OracleDataEngineBuilderExtensions`

Registers the Oracle LogMiner CDC companion pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class OracleDataEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-data-oracle-registration-oracledataenginebuilderextensions-addoracledata-cephalon-engine-composition-enginebuilder-system-action-cephalon-data-oracle-configuration-oracledataoptions"></a>

##### `AddOracleData`

```csharp
EngineBuilder AddOracleData(this EngineBuilder builder, Action<OracleDataOptions> configure)
```

Adds the Oracle LogMiner CDC pack using an options callback that can bind from configuration.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: The callback that configures the host-owned Oracle pack options, including `ConnectionStringName`, `ConnectionString`, and `DatabaseName`.

<a id="member-m-cephalon-data-oracle-registration-oracledataenginebuilderextensions-addoracledata-cephalon-engine-composition-enginebuilder-system-string-system-string-system-action-cephalon-data-oracle-configuration-oracledataoptions"></a>

##### `AddOracleData`

```csharp
EngineBuilder AddOracleData(this EngineBuilder builder, string connectionString, string databaseName, Action<OracleDataOptions> configure)
```

Adds the Oracle LogMiner CDC pack with the supplied connection string and database name.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `connectionString`: The Oracle connection string.
- `databaseName`: The operator-facing Oracle database name.
- `configure`: An optional callback that configures the host-owned Oracle pack options.
