# Cephalon.Data.MySql

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Data.MySql)
## Namespaces

- `Cephalon.Data.MySql.Configuration`
- `Cephalon.Data.MySql.Registration`

<a id="namespace-cephalon-data-mysql-configuration"></a>

## Namespace Cephalon.Data.MySql.Configuration

<a id="type-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions"></a>

### `MySqlBinlogCaptureOptions`

Declares one provider-native MySQL binlog capture for the active runtime.

#### Declaration
```csharp
public sealed class MySqlBinlogCaptureOptions
```

#### Constructors

<a id="member-m-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-ctor"></a>

##### `MySqlBinlogCaptureOptions`

```csharp
MySqlBinlogCaptureOptions()
```

#### Properties

<a id="member-p-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-channelid"></a>

##### `ChannelId`

```csharp
string ChannelId { get; set; }
```

Gets or sets the logical outbox channel that receives emitted publications.

<a id="member-p-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-description"></a>

##### `Description`

```csharp
string Description { get; set; }
```

Gets or sets the human-readable CDC capture description.

<a id="member-p-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

Gets or sets the operator-facing CDC capture name.

<a id="member-p-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-eventformat"></a>

##### `EventFormat`

```csharp
string EventFormat { get; set; }
```

Gets or sets the operator-facing event format projected on the CDC descriptor.

<a id="member-p-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-expectedsourceserveruuid"></a>

##### `ExpectedSourceServerUuid`

```csharp
string ExpectedSourceServerUuid { get; set; }
```

Gets or sets the expected MySQL source-server UUID when the capture should fail fast if the runtime connects to a different upstream.

Remarks: Leave this blank when the capture should observe source-server identity for diagnostics only. When set, the provider-native runner validates the live server UUID before it starts or resumes binlog consumption.

<a id="member-p-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

Gets or sets the stable CDC capture identifier.

<a id="member-p-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-initialposition"></a>

##### `InitialPosition`

```csharp
string InitialPosition { get; set; }
```

Gets or sets the initial position used when no durable checkpoint exists yet.

Remarks: Supported values are `latest-available` and `earliest-available`.

<a id="member-p-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-maxawaittimeseconds"></a>

##### `MaxAwaitTimeSeconds`

```csharp
int MaxAwaitTimeSeconds { get; set; }
```

Gets or sets the maximum number of seconds to await row events during one provider-native iteration.

<a id="member-p-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-maxchangesperread"></a>

##### `MaxChangesPerRead`

```csharp
int MaxChangesPerRead { get; set; }
```

Gets or sets the maximum number of captured row changes to stage during one provider-native iteration.

<a id="member-p-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-messagetype"></a>

##### `MessageType`

```csharp
string MessageType { get; set; }
```

Gets or sets the logical message type emitted for each captured change event.

<a id="member-p-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-metadata"></a>

##### `Metadata`

```csharp
IDictionary<string, string> Metadata { get; }
```

Gets arbitrary operator-facing metadata that should flow through the capture descriptor.

<a id="member-p-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-outboxid"></a>

##### `OutboxId`

```csharp
string OutboxId { get; set; }
```

Gets or sets the outbox identifier that receives emitted publications.

<a id="member-p-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-pollingintervalseconds"></a>

##### `PollingIntervalSeconds`

```csharp
int PollingIntervalSeconds { get; set; }
```

Gets or sets the polling interval, in seconds, between hosted-service iterations.

<a id="member-p-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-resourceids"></a>

##### `ResourceIds`

```csharp
IList<string> ResourceIds { get; }
```

Gets the resource identifiers observed by the capture.

<a id="member-p-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-serverid"></a>

##### `ServerId`

```csharp
int ServerId { get; set; }
```

Gets or sets the replication-client server identifier used for this capture connection.

Remarks: MySQL expects a stable positive server id per replication client. When multiple captures run concurrently, configure a distinct value for each capture.

<a id="member-p-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-sourceid"></a>

##### `SourceId`

```csharp
string SourceId { get; set; }
```

Gets or sets the logical source identifier when it should differ from the watched table path.

<a id="member-p-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; set; }
```

Gets or sets the module identifier that owns the capture surface.

<a id="member-p-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-tablename"></a>

##### `TableName`

```csharp
string TableName { get; set; }
```

Gets or sets the table name of the tracked table.

<a id="member-p-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-tableschema"></a>

##### `TableSchema`

```csharp
string TableSchema { get; set; }
```

Gets or sets the MySQL schema name of the tracked table.

Remarks: When this value is blank, the pack falls back to `DatabaseName`.

<a id="member-p-cephalon-data-mysql-configuration-mysqlbinlogcaptureoptions-tags"></a>

##### `Tags`

```csharp
IList<string> Tags { get; }
```

Gets the descriptive tags associated with the capture.

<a id="type-cephalon-data-mysql-configuration-mysqldataoptions"></a>

### `MySqlDataOptions`

Configuration options for the MySQL data provider (`Engine:Data:MySql`).

#### Declaration
```csharp
public sealed class MySqlDataOptions
```

#### Constructors

<a id="member-m-cephalon-data-mysql-configuration-mysqldataoptions-ctor"></a>

##### `MySqlDataOptions`

```csharp
MySqlDataOptions()
```

#### Fields

<a id="member-f-cephalon-data-mysql-configuration-mysqldataoptions-providerid"></a>

##### `ProviderId`

```csharp
const string ProviderId
```

Gets the canonical provider identifier emitted by the pack.

<a id="member-f-cephalon-data-mysql-configuration-mysqldataoptions-sectionpath"></a>

##### `SectionPath`

```csharp
const string SectionPath
```

Gets the configuration section path used by default for MySQL data settings.

#### Properties

<a id="member-p-cephalon-data-mysql-configuration-mysqldataoptions-cdccaptures"></a>

##### `CdcCaptures`

```csharp
IList<MySqlBinlogCaptureOptions> CdcCaptures { get; }
```

Gets the provider-native MySQL binlog captures that should be contributed to the active runtime.

<a id="member-p-cephalon-data-mysql-configuration-mysqldataoptions-checkpointtablename"></a>

##### `CheckpointTableName`

```csharp
string CheckpointTableName { get; set; }
```

Gets or sets the Cephalon-managed checkpoint table name used for durable MySQL binlog progress.

<a id="member-p-cephalon-data-mysql-configuration-mysqldataoptions-connectionstring"></a>

##### `ConnectionString`

```csharp
string ConnectionString { get; set; }
```

Gets or sets the inline MySQL connection string.

Remarks: Use either `ConnectionStringName` or `ConnectionString`.

<a id="member-p-cephalon-data-mysql-configuration-mysqldataoptions-connectionstringname"></a>

##### `ConnectionStringName`

```csharp
string ConnectionStringName { get; set; }
```

Gets or sets the root `ConnectionStrings` entry name to resolve for MySQL.

Remarks: Use either `ConnectionStringName` or `ConnectionString`.

<a id="member-p-cephalon-data-mysql-configuration-mysqldataoptions-databasename"></a>

##### `DatabaseName`

```csharp
string DatabaseName { get; set; }
```

Gets or sets the operator-facing database name that owns the configured binlog captures.

<a id="namespace-cephalon-data-mysql-registration"></a>

## Namespace Cephalon.Data.MySql.Registration

<a id="type-cephalon-data-mysql-registration-mysqldataenginebuilderextensions"></a>

### `MySqlDataEngineBuilderExtensions`

Registers the MySQL binlog CDC companion pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class MySqlDataEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-data-mysql-registration-mysqldataenginebuilderextensions-addmysqldata-cephalon-engine-composition-enginebuilder-system-action-cephalon-data-mysql-configuration-mysqldataoptions"></a>

##### `AddMySqlData`

```csharp
EngineBuilder AddMySqlData(this EngineBuilder builder, Action<MySqlDataOptions> configure)
```

Adds the MySQL binlog CDC pack using an options callback that can bind from configuration.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: The callback that configures the host-owned MySQL pack options, including `ConnectionStringName`, `ConnectionString`, and `DatabaseName`.

<a id="member-m-cephalon-data-mysql-registration-mysqldataenginebuilderextensions-addmysqldata-cephalon-engine-composition-enginebuilder-system-string-system-string-system-action-cephalon-data-mysql-configuration-mysqldataoptions"></a>

##### `AddMySqlData`

```csharp
EngineBuilder AddMySqlData(this EngineBuilder builder, string connectionString, string databaseName, Action<MySqlDataOptions> configure)
```

Adds the MySQL binlog CDC pack with the supplied connection string and database name.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `connectionString`: The MySQL connection string.
- `databaseName`: The operator-facing database name.
- `configure`: An optional callback that configures the host-owned MySQL pack options.
