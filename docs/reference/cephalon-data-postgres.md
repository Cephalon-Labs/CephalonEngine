# Cephalon.Data.Postgres

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Data.Postgres)
## Namespaces

- `Cephalon.Data.Postgres.Configuration`
- `Cephalon.Data.Postgres.Registration`

<a id="namespace-cephalon-data-postgres-configuration"></a>

## Namespace Cephalon.Data.Postgres.Configuration

<a id="type-cephalon-data-postgres-configuration-postgresdataoptions"></a>

### `PostgresDataOptions`

Configuration options for the PostgreSQL data provider (`Engine:Data:Postgres`).

#### Declaration
```csharp
public sealed class PostgresDataOptions
```

#### Constructors

<a id="member-m-cephalon-data-postgres-configuration-postgresdataoptions-ctor"></a>

##### `PostgresDataOptions`

```csharp
PostgresDataOptions()
```

#### Fields

<a id="member-f-cephalon-data-postgres-configuration-postgresdataoptions-providerid"></a>

##### `ProviderId`

```csharp
const string ProviderId
```

Gets the canonical provider identifier emitted by the pack.

<a id="member-f-cephalon-data-postgres-configuration-postgresdataoptions-sectionpath"></a>

##### `SectionPath`

```csharp
const string SectionPath
```

Gets the configuration section path used by default for PostgreSQL data settings.

#### Properties

<a id="member-p-cephalon-data-postgres-configuration-postgresdataoptions-cdccaptures"></a>

##### `CdcCaptures`

```csharp
IList<PostgresLogicalReplicationCaptureOptions> CdcCaptures { get; }
```

Gets the provider-native PostgreSQL logical-replication captures that should be contributed to the active runtime.

<a id="member-p-cephalon-data-postgres-configuration-postgresdataoptions-connectionstring"></a>

##### `ConnectionString`

```csharp
string ConnectionString { get; set; }
```

Gets or sets the inline PostgreSQL connection string.

Remarks: Use either `ConnectionStringName` or `ConnectionString`.

<a id="member-p-cephalon-data-postgres-configuration-postgresdataoptions-connectionstringname"></a>

##### `ConnectionStringName`

```csharp
string ConnectionStringName { get; set; }
```

Gets or sets the root `ConnectionStrings` entry name to resolve for PostgreSQL.

Remarks: Use either `ConnectionStringName` or `ConnectionString`.

<a id="member-p-cephalon-data-postgres-configuration-postgresdataoptions-databasename"></a>

##### `DatabaseName`

```csharp
string DatabaseName { get; set; }
```

Gets or sets the operator-facing database name that owns the configured logical-replication captures.

<a id="type-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions"></a>

### `PostgresLogicalReplicationCaptureOptions`

Declares one provider-native PostgreSQL logical-replication capture for the active runtime.

#### Declaration
```csharp
public sealed class PostgresLogicalReplicationCaptureOptions
```

#### Constructors

<a id="member-m-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-ctor"></a>

##### `PostgresLogicalReplicationCaptureOptions`

```csharp
PostgresLogicalReplicationCaptureOptions()
```

#### Properties

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-channelid"></a>

##### `ChannelId`

```csharp
string ChannelId { get; set; }
```

Gets or sets the logical outbox channel that receives emitted publications.

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-createslotifmissing"></a>

##### `CreateSlotIfMissing`

```csharp
bool CreateSlotIfMissing { get; set; }
```

Gets or sets a value indicating whether the pack should create the logical replication slot when it does not exist yet.

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-description"></a>

##### `Description`

```csharp
string Description { get; set; }
```

Gets or sets the human-readable CDC capture description.

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; set; }
```

Gets or sets the operator-facing CDC capture name.

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-eventformat"></a>

##### `EventFormat`

```csharp
string EventFormat { get; set; }
```

Gets or sets the operator-facing event format projected on the CDC descriptor.

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

Gets or sets the stable CDC capture identifier.

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-initialposition"></a>

##### `InitialPosition`

```csharp
string InitialPosition { get; set; }
```

Gets or sets the initial position used when the logical replication slot must be created.

Remarks: Supported values are `slot-consistent-point` and `latest-available`.

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-maxawaittimeseconds"></a>

##### `MaxAwaitTimeSeconds`

```csharp
int MaxAwaitTimeSeconds { get; set; }
```

Gets or sets the maximum number of seconds to await committed WAL messages during one provider-native iteration.

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-maxchangesperread"></a>

##### `MaxChangesPerRead`

```csharp
int MaxChangesPerRead { get; set; }
```

Gets or sets the maximum number of committed logical-replication changes to stage during one iteration.

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-messagetype"></a>

##### `MessageType`

```csharp
string MessageType { get; set; }
```

Gets or sets the logical message type emitted for each captured change event.

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-metadata"></a>

##### `Metadata`

```csharp
IDictionary<string, string> Metadata { get; }
```

Gets arbitrary operator-facing metadata that should flow through the capture descriptor.

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-outboxid"></a>

##### `OutboxId`

```csharp
string OutboxId { get; set; }
```

Gets or sets the outbox identifier that receives emitted publications.

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-pollingintervalseconds"></a>

##### `PollingIntervalSeconds`

```csharp
int PollingIntervalSeconds { get; set; }
```

Gets or sets the polling interval, in seconds, between hosted-service iterations.

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-publicationname"></a>

##### `PublicationName`

```csharp
string PublicationName { get; set; }
```

Gets or sets the PostgreSQL publication that should emit the tracked table changes.

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-resourceids"></a>

##### `ResourceIds`

```csharp
IList<string> ResourceIds { get; }
```

Gets the resource identifiers observed by the capture.

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-slotname"></a>

##### `SlotName`

```csharp
string SlotName { get; set; }
```

Gets or sets the PostgreSQL logical replication slot used for durable progress.

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-sourceid"></a>

##### `SourceId`

```csharp
string SourceId { get; set; }
```

Gets or sets the logical source identifier when it should differ from the watched table path.

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-sourcemoduleid"></a>

##### `SourceModuleId`

```csharp
string SourceModuleId { get; set; }
```

Gets or sets the module identifier that owns the capture surface.

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-tablename"></a>

##### `TableName`

```csharp
string TableName { get; set; }
```

Gets or sets the table name of the tracked table.

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-tableschema"></a>

##### `TableSchema`

```csharp
string TableSchema { get; set; }
```

Gets or sets the schema name of the tracked table.

<a id="member-p-cephalon-data-postgres-configuration-postgreslogicalreplicationcaptureoptions-tags"></a>

##### `Tags`

```csharp
IList<string> Tags { get; }
```

Gets the descriptive tags associated with the capture.

<a id="namespace-cephalon-data-postgres-registration"></a>

## Namespace Cephalon.Data.Postgres.Registration

<a id="type-cephalon-data-postgres-registration-postgresdataenginebuilderextensions"></a>

### `PostgresDataEngineBuilderExtensions`

Registers the PostgreSQL logical-replication CDC companion pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class PostgresDataEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-data-postgres-registration-postgresdataenginebuilderextensions-addpostgresdata-cephalon-engine-composition-enginebuilder-system-action-cephalon-data-postgres-configuration-postgresdataoptions"></a>

##### `AddPostgresData`

```csharp
EngineBuilder AddPostgresData(this EngineBuilder builder, Action<PostgresDataOptions> configure)
```

Adds the PostgreSQL logical-replication CDC pack using an options callback that can bind from configuration.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: The callback that configures the host-owned PostgreSQL pack options, including `ConnectionStringName`, `ConnectionString`, and `DatabaseName`.

<a id="member-m-cephalon-data-postgres-registration-postgresdataenginebuilderextensions-addpostgresdata-cephalon-engine-composition-enginebuilder-system-string-system-string-system-action-cephalon-data-postgres-configuration-postgresdataoptions"></a>

##### `AddPostgresData`

```csharp
EngineBuilder AddPostgresData(this EngineBuilder builder, string connectionString, string databaseName, Action<PostgresDataOptions> configure)
```

Adds the PostgreSQL logical-replication CDC pack with the supplied connection string and database name.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `connectionString`: The PostgreSQL connection string.
- `databaseName`: The operator-facing database name.
- `configure`: An optional callback that configures the host-owned PostgreSQL pack options.
