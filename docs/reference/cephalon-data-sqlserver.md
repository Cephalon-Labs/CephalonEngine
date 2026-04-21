# Cephalon.Data.SqlServer

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Data.SqlServer)
## Namespaces

- `Cephalon.Data.SqlServer.Configuration`
- `Cephalon.Data.SqlServer.Registration`

<a id="namespace-cephalon-data-sqlserver-configuration"></a>

## Namespace Cephalon.Data.SqlServer.Configuration

<a id="type-cephalon-data-sqlserver-configuration-sqlservercdccaptureoptions"></a>

### `SqlServerCdcCaptureOptions`

Declares one provider-native SQL Server CDC capture for the active runtime.

#### Declaration
```csharp
public sealed class SqlServerCdcCaptureOptions
```

#### Constructors

<a id="member-m-cephalon-data-sqlserver-configuration-sqlservercdccaptureoptions-ctor"></a>

##### `SqlServerCdcCaptureOptions`

```csharp
SqlServerCdcCaptureOptions()
```

#### Properties

<a id="member-p-cephalon-data-sqlserver-configuration-sqlservercdccaptureoptions-captureinstance"></a>

##### `CaptureInstance`

```csharp
string CaptureInstance { get; set; }
```

Gets or sets the SQL Server CDC capture-instance name.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlservercdccaptureoptions-channelid"></a>

##### `ChannelId`

```csharp
string ChannelId { get; set; }
```

Gets or sets the logical outbox channel that receives emitted publications.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlservercdccaptureoptions-description"></a>

##### `Description`

```csharp
string Description { get; set; }
```

Gets or sets the human-readable CDC capture description.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlservercdccaptureoptions-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

Gets or sets the operator-facing CDC capture name.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlservercdccaptureoptions-eventformat"></a>

##### `EventFormat`

```csharp
string EventFormat { get; set; }
```

Gets or sets the operator-facing event format projected on the CDC descriptor.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlservercdccaptureoptions-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

Gets or sets the stable CDC capture identifier.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlservercdccaptureoptions-initialposition"></a>

##### `InitialPosition`

```csharp
string InitialPosition { get; set; }
```

Gets or sets the initial position used when no durable checkpoint exists yet.

Remarks: Supported values are `latest-available` and `earliest-available`.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlservercdccaptureoptions-maxchangesperpoll"></a>

##### `MaxChangesPerPoll`

```csharp
int MaxChangesPerPoll { get; set; }
```

Gets or sets the maximum number of captured changes to stage during one polling iteration.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlservercdccaptureoptions-messagetype"></a>

##### `MessageType`

```csharp
string MessageType { get; set; }
```

Gets or sets the logical message type emitted for each captured change event.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlservercdccaptureoptions-metadata"></a>

##### `Metadata`

```csharp
IDictionary<string, string> Metadata { get; }
```

Gets arbitrary operator-facing metadata that should flow through the capture descriptor.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlservercdccaptureoptions-outboxid"></a>

##### `OutboxId`

```csharp
string OutboxId { get; set; }
```

Gets or sets the outbox identifier that receives emitted publications.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlservercdccaptureoptions-pollingintervalseconds"></a>

##### `PollingIntervalSeconds`

```csharp
int PollingIntervalSeconds { get; set; }
```

Gets or sets the polling interval, in seconds, for one provider-native SQL Server CDC iteration.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlservercdccaptureoptions-resourceids"></a>

##### `ResourceIds`

```csharp
IList<string> ResourceIds { get; }
```

Gets the resource identifiers observed by the capture.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlservercdccaptureoptions-sourceid"></a>

##### `SourceId`

```csharp
string SourceId { get; set; }
```

Gets or sets the logical source identifier when it should differ from the watched table path.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlservercdccaptureoptions-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; set; }
```

Gets or sets the module identifier that owns the capture surface.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlservercdccaptureoptions-tablename"></a>

##### `TableName`

```csharp
string TableName { get; set; }
```

Gets or sets the table name of the tracked table.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlservercdccaptureoptions-tableschema"></a>

##### `TableSchema`

```csharp
string TableSchema { get; set; }
```

Gets or sets the schema name of the tracked table.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlservercdccaptureoptions-tags"></a>

##### `Tags`

```csharp
IList<string> Tags { get; }
```

Gets the descriptive tags associated with the capture.

<a id="type-cephalon-data-sqlserver-configuration-sqlserverdataoptions"></a>

### `SqlServerDataOptions`

Configuration options for the SQL Server data provider (`Engine:Data:SqlServer`).

#### Declaration
```csharp
public sealed class SqlServerDataOptions
```

#### Constructors

<a id="member-m-cephalon-data-sqlserver-configuration-sqlserverdataoptions-ctor"></a>

##### `SqlServerDataOptions`

```csharp
SqlServerDataOptions()
```

#### Fields

<a id="member-f-cephalon-data-sqlserver-configuration-sqlserverdataoptions-providerid"></a>

##### `ProviderId`

```csharp
const string ProviderId
```

Gets the canonical provider identifier emitted by the pack.

<a id="member-f-cephalon-data-sqlserver-configuration-sqlserverdataoptions-sectionpath"></a>

##### `SectionPath`

```csharp
const string SectionPath
```

Gets the configuration section path used by default for SQL Server data settings.

#### Properties

<a id="member-p-cephalon-data-sqlserver-configuration-sqlserverdataoptions-cdccaptures"></a>

##### `CdcCaptures`

```csharp
IList<SqlServerCdcCaptureOptions> CdcCaptures { get; }
```

Gets the provider-native SQL Server CDC captures that should be contributed to the active runtime.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlserverdataoptions-checkpointtablename"></a>

##### `CheckpointTableName`

```csharp
string CheckpointTableName { get; set; }
```

Gets or sets the table name that stores Cephalon-managed SQL Server CDC checkpoints.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlserverdataoptions-checkpointtableschema"></a>

##### `CheckpointTableSchema`

```csharp
string CheckpointTableSchema { get; set; }
```

Gets or sets the schema that stores Cephalon-managed SQL Server CDC checkpoints.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlserverdataoptions-connectionstring"></a>

##### `ConnectionString`

```csharp
string ConnectionString { get; set; }
```

Gets or sets the inline SQL Server connection string.

Remarks: Use either `ConnectionStringName` or `ConnectionString`.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlserverdataoptions-connectionstringname"></a>

##### `ConnectionStringName`

```csharp
string ConnectionStringName { get; set; }
```

Gets or sets the root `ConnectionStrings` entry name to resolve for SQL Server.

Remarks: Use either `ConnectionStringName` or `ConnectionString`.

<a id="member-p-cephalon-data-sqlserver-configuration-sqlserverdataoptions-databasename"></a>

##### `DatabaseName`

```csharp
string DatabaseName { get; set; }
```

Gets or sets the operator-facing database name that owns the configured CDC captures.

<a id="namespace-cephalon-data-sqlserver-registration"></a>

## Namespace Cephalon.Data.SqlServer.Registration

<a id="type-cephalon-data-sqlserver-registration-sqlserverdataenginebuilderextensions"></a>

### `SqlServerDataEngineBuilderExtensions`

Registers the SQL Server CDC companion pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class SqlServerDataEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-data-sqlserver-registration-sqlserverdataenginebuilderextensions-addsqlserverdata-cephalon-engine-composition-enginebuilder-system-action-cephalon-data-sqlserver-configuration-sqlserverdataoptions"></a>

##### `AddSqlServerData`

```csharp
EngineBuilder AddSqlServerData(this EngineBuilder builder, Action<SqlServerDataOptions> configure)
```

Adds the SQL Server CDC pack using an options callback that can bind from configuration.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: The callback that configures the host-owned SQL Server pack options, including `ConnectionStringName`, `ConnectionString`, and `DatabaseName`.

<a id="member-m-cephalon-data-sqlserver-registration-sqlserverdataenginebuilderextensions-addsqlserverdata-cephalon-engine-composition-enginebuilder-system-string-system-string-system-action-cephalon-data-sqlserver-configuration-sqlserverdataoptions"></a>

##### `AddSqlServerData`

```csharp
EngineBuilder AddSqlServerData(this EngineBuilder builder, string connectionString, string databaseName, Action<SqlServerDataOptions> configure)
```

Adds the SQL Server CDC pack with the supplied connection string and database name.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `connectionString`: The SQL Server connection string.
- `databaseName`: The operator-facing database name.
- `configure`: An optional callback that configures the host-owned SQL Server pack options.
