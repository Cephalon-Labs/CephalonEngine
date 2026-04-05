# Cephalon.Data.EntityFramework

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Data.EntityFramework)
## Namespaces

- `Cephalon.Data.EntityFramework.Configuration`
- `Cephalon.Data.EntityFramework.Modeling`
- `Cephalon.Data.EntityFramework.Registration`

<a id="namespace-cephalon-data-entityframework-configuration"></a>

## Namespace Cephalon.Data.EntityFramework.Configuration

<a id="type-cephalon-data-entityframework-configuration-entityframeworkdataoptions"></a>

### `EntityFrameworkDataOptions`

Describes the host-owned options for the Entity Framework Core data companion pack.

#### Declaration
```csharp
public sealed class EntityFrameworkDataOptions
```

#### Constructors

<a id="member-m-cephalon-data-entityframework-configuration-entityframeworkdataoptions-ctor-system-type-system-type"></a>

##### `EntityFrameworkDataOptions`

```csharp
EntityFrameworkDataOptions(Type readDbContextType, Type writeDbContextType)
```

Initializes a new instance of the `EntityFrameworkDataOptions` class.

Parameters:
- `readDbContextType`: The read-side `DbContext` type.
- `writeDbContextType`: The write-side `DbContext` type.

#### Fields

<a id="member-f-cephalon-data-entityframework-configuration-entityframeworkdataoptions-providerid"></a>

##### `ProviderId`

```csharp
const string ProviderId
```

Gets the canonical provider identifier emitted by the pack.

#### Properties

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdataoptions-enablesfididentifiers"></a>

##### `EnableSfidIdentifiers`

```csharp
bool EnableSfidIdentifiers { get; set; }
```

Gets or sets a value indicating whether the pack should enable official `Sfid.EntityFramework` conventions and key generation.

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdataoptions-readdbcontexttype"></a>

##### `ReadDbContextType`

```csharp
Type ReadDbContextType { get; }
```

Gets the read-side `DbContext` type.

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdataoptions-registerdbcontextcapabilities"></a>

##### `RegisterDbContextCapabilities`

```csharp
bool RegisterDbContextCapabilities { get; set; }
```

Gets or sets a value indicating whether the pack should publish read/write `DbContext` role capabilities.

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdataoptions-registerinbox"></a>

##### `RegisterInbox`

```csharp
bool RegisterInbox { get; set; }
```

Gets or sets a value indicating whether the pack should register the Entity Framework-backed inbox implementation.

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdataoptions-registeroutbox"></a>

##### `RegisterOutbox`

```csharp
bool RegisterOutbox { get; set; }
```

Gets or sets a value indicating whether the pack should register the Entity Framework-backed outbox implementation.

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdataoptions-registerprovidercapability"></a>

##### `RegisterProviderCapability`

```csharp
bool RegisterProviderCapability { get; set; }
```

Gets or sets a value indicating whether the pack should publish the provider capability.

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdataoptions-usesreadwritesplit"></a>

##### `UsesReadWriteSplit`

```csharp
bool UsesReadWriteSplit { get; }
```

Gets a value indicating whether distinct read and write `DbContext` types were selected.

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdataoptions-writedbcontexttype"></a>

##### `WriteDbContextType`

```csharp
Type WriteDbContextType { get; }
```

Gets the write-side `DbContext` type.

<a id="namespace-cephalon-data-entityframework-modeling"></a>

## Namespace Cephalon.Data.EntityFramework.Modeling

<a id="type-cephalon-data-entityframework-modeling-entityframeworkinboxentry"></a>

### `EntityFrameworkInboxEntry`

Represents one processed inbound message row stored through the Entity Framework data companion pack.

#### Declaration
```csharp
public sealed class EntityFrameworkInboxEntry
```

#### Constructors

<a id="member-m-cephalon-data-entityframework-modeling-entityframeworkinboxentry-ctor"></a>

##### `EntityFrameworkInboxEntry`

```csharp
EntityFrameworkInboxEntry()
```

Initializes a new instance of the `EntityFrameworkInboxEntry` class.

#### Properties

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkinboxentry-channelid"></a>

##### `ChannelId`

```csharp
string ChannelId { get; set; }
```

Gets or sets the logical channel or source identifier.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkinboxentry-contenttype"></a>

##### `ContentType`

```csharp
string ContentType { get; set; }
```

Gets or sets the payload content type when one is known.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkinboxentry-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; set; }
```

Gets or sets the correlation identifier associated with the message.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkinboxentry-headersjson"></a>

##### `HeadersJson`

```csharp
string HeadersJson { get; set; }
```

Gets or sets the serialized message headers payload.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkinboxentry-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

Gets or sets the stable inbound message identifier.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkinboxentry-messagetype"></a>

##### `MessageType`

```csharp
string MessageType { get; set; }
```

Gets or sets the logical message type identifier.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkinboxentry-metadatajson"></a>

##### `MetadataJson`

```csharp
string MetadataJson { get; set; }
```

Gets or sets the serialized message metadata payload.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkinboxentry-payload"></a>

##### `Payload`

```csharp
string Payload { get; set; }
```

Gets or sets the serialized payload that was received.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkinboxentry-processedatutc"></a>

##### `ProcessedAtUtc`

```csharp
DateTimeOffset ProcessedAtUtc { get; set; }
```

Gets or sets the time at which the inbox row was marked as processed.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkinboxentry-receivedatutc"></a>

##### `ReceivedAtUtc`

```csharp
DateTimeOffset ReceivedAtUtc { get; set; }
```

Gets or sets the time at which the message was received.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkinboxentry-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; set; }
```

Gets or sets the tenant identifier associated with the message.

<a id="type-cephalon-data-entityframework-modeling-entityframeworkmodelbuilderextensions"></a>

### `EntityFrameworkModelBuilderExtensions`

Configures shared Cephalon Entity Framework Core model slices.

#### Declaration
```csharp
public static class EntityFrameworkModelBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-data-entityframework-modeling-entityframeworkmodelbuilderextensions-configurecephaloninbox-microsoft-entityframeworkcore-modelbuilder-system-string"></a>

##### `ConfigureCephalonInbox`

```csharp
ModelBuilder ConfigureCephalonInbox(this ModelBuilder modelBuilder, string tableName)
```

Adds the Cephalon inbox entity mapping to the supplied model.

Returns: The same model builder for fluent configuration.

Parameters:
- `modelBuilder`: The model builder to extend.
- `tableName`: The table name that should hold processed inbox rows.

<a id="member-m-cephalon-data-entityframework-modeling-entityframeworkmodelbuilderextensions-configurecephalonoutbox-microsoft-entityframeworkcore-modelbuilder-system-string"></a>

##### `ConfigureCephalonOutbox`

```csharp
ModelBuilder ConfigureCephalonOutbox(this ModelBuilder modelBuilder, string tableName)
```

Adds the Cephalon outbox entity mapping to the supplied model.

Returns: The same model builder for fluent configuration.

Parameters:
- `modelBuilder`: The model builder to extend.
- `tableName`: The table name that should hold durable outbox rows.

<a id="type-cephalon-data-entityframework-modeling-entityframeworkoutboxentry"></a>

### `EntityFrameworkOutboxEntry`

Represents one durable outbox row stored through the Entity Framework data companion pack.

#### Declaration
```csharp
public sealed class EntityFrameworkOutboxEntry
```

#### Constructors

<a id="member-m-cephalon-data-entityframework-modeling-entityframeworkoutboxentry-ctor"></a>

##### `EntityFrameworkOutboxEntry`

```csharp
EntityFrameworkOutboxEntry()
```

Initializes a new instance of the `EntityFrameworkOutboxEntry` class.

#### Properties

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkoutboxentry-channelid"></a>

##### `ChannelId`

```csharp
string ChannelId { get; set; }
```

Gets or sets the logical channel or destination identifier.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkoutboxentry-contenttype"></a>

##### `ContentType`

```csharp
string ContentType { get; set; }
```

Gets or sets the payload content type when one is known.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkoutboxentry-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; set; }
```

Gets or sets the correlation identifier associated with the message.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkoutboxentry-createdatutc"></a>

##### `CreatedAtUtc`

```csharp
DateTimeOffset CreatedAtUtc { get; set; }
```

Gets or sets the time at which the outbox row was created.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkoutboxentry-dispatchattemptcount"></a>

##### `DispatchAttemptCount`

```csharp
int DispatchAttemptCount { get; set; }
```

Gets or sets the current dispatch-attempt count.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkoutboxentry-dispatchedatutc"></a>

##### `DispatchedAtUtc`

```csharp
DateTimeOffset? DispatchedAtUtc { get; set; }
```

Gets or sets the time at which the outbox row was dispatched, when known.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkoutboxentry-headersjson"></a>

##### `HeadersJson`

```csharp
string HeadersJson { get; set; }
```

Gets or sets the serialized message headers payload.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkoutboxentry-id"></a>

##### `Id`

```csharp
string Id { get; set; }
```

Gets or sets the stable outbox message identifier.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkoutboxentry-messagetype"></a>

##### `MessageType`

```csharp
string MessageType { get; set; }
```

Gets or sets the logical message type identifier.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkoutboxentry-metadatajson"></a>

##### `MetadataJson`

```csharp
string MetadataJson { get; set; }
```

Gets or sets the serialized message metadata payload.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkoutboxentry-nextattemptatutc"></a>

##### `NextAttemptAtUtc`

```csharp
DateTimeOffset? NextAttemptAtUtc { get; set; }
```

Gets or sets the time at which the outbox row becomes eligible for the next dispatch attempt, when delayed retry is in effect.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkoutboxentry-occurredatutc"></a>

##### `OccurredAtUtc`

```csharp
DateTimeOffset OccurredAtUtc { get; set; }
```

Gets or sets the time at which the message became visible to the outbox.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkoutboxentry-payload"></a>

##### `Payload`

```csharp
string Payload { get; set; }
```

Gets or sets the serialized payload that should be delivered later.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkoutboxentry-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; set; }
```

Gets or sets the tenant identifier associated with the message.

<a id="type-cephalon-data-entityframework-modeling-ientityframeworkinboxcontext"></a>

### `IEntityFrameworkInboxContext`

Declares the write-side Entity Framework Core surface required by the Cephalon inbox implementation.

#### Declaration
```csharp
public interface IEntityFrameworkInboxContext
```

#### Properties

<a id="member-p-cephalon-data-entityframework-modeling-ientityframeworkinboxcontext-inboxmessages"></a>

##### `InboxMessages`

```csharp
DbSet<EntityFrameworkInboxEntry> InboxMessages { get; }
```

Gets the processed-message rows tracked by the current write-side `DbContext`.

<a id="type-cephalon-data-entityframework-modeling-ientityframeworkoutboxcontext"></a>

### `IEntityFrameworkOutboxContext`

Declares the write-side Entity Framework Core surface required by the Cephalon outbox implementation.

#### Declaration
```csharp
public interface IEntityFrameworkOutboxContext
```

#### Properties

<a id="member-p-cephalon-data-entityframework-modeling-ientityframeworkoutboxcontext-outboxmessages"></a>

##### `OutboxMessages`

```csharp
DbSet<EntityFrameworkOutboxEntry> OutboxMessages { get; }
```

Gets the outbox rows staged by the current write-side `DbContext`.

<a id="namespace-cephalon-data-entityframework-registration"></a>

## Namespace Cephalon.Data.EntityFramework.Registration

<a id="type-cephalon-data-entityframework-registration-entityframeworkdataenginebuilderextensions"></a>

### `EntityFrameworkDataEngineBuilderExtensions`

Registers the Entity Framework Core data companion pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class EntityFrameworkDataEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-data-entityframework-registration-entityframeworkdataenginebuilderextensions-addentityframeworkdata-1-cephalon-engine-composition-enginebuilder-system-action-microsoft-entityframeworkcore-dbcontextoptionsbuilder-system-action-cephalon-data-entityframework-configuration-entityframeworkdataoptions"></a>

##### `AddEntityFrameworkData`

```csharp
EngineBuilder AddEntityFrameworkData<TDbContext>(this EngineBuilder builder, Action<DbContextOptionsBuilder> configureDbContext, Action<EntityFrameworkDataOptions> configure)
```

Adds the Entity Framework Core data pack with one shared `DbContext` type for both read and write workloads.

Remarks: Pair this pack with `AddData()` when you want Cephalon-managed `IReadStore` and `IWriteStore` dispatching on top of the registered `DbContext` services.

Returns: The same engine builder for fluent composition.

Type parameters:
- `TDbContext`: The shared `DbContext` type.

Parameters:
- `builder`: The engine builder to extend.
- `configureDbContext`: The callback that configures the shared Entity Framework Core `DbContext`.
- `configure`: An optional callback that configures the host-owned Entity Framework pack options.

<a id="member-m-cephalon-data-entityframework-registration-entityframeworkdataenginebuilderextensions-addentityframeworkdata-2-cephalon-engine-composition-enginebuilder-system-action-microsoft-entityframeworkcore-dbcontextoptionsbuilder-system-action-microsoft-entityframeworkcore-dbcontextoptionsbuilder-system-action-cephalon-data-entityframework-configuration-entityframeworkdataoptions"></a>

##### `AddEntityFrameworkData`

```csharp
EngineBuilder AddEntityFrameworkData<TReadDbContext, TWriteDbContext>(this EngineBuilder builder, Action<DbContextOptionsBuilder> configureReadDbContext, Action<DbContextOptionsBuilder> configureWriteDbContext, Action<EntityFrameworkDataOptions> configure)
```

Adds the Entity Framework Core data pack with explicit read and write `DbContext` types.

Remarks: Pair this pack with `AddData()` when you want Cephalon-managed `IReadStore` and `IWriteStore` dispatching on top of the registered `DbContext` services.

Returns: The same engine builder for fluent composition.

Type parameters:
- `TReadDbContext`: The read-side `DbContext` type.
- `TWriteDbContext`: The write-side `DbContext` type.

Parameters:
- `builder`: The engine builder to extend.
- `configureReadDbContext`: The callback that configures the read-side Entity Framework Core `DbContext`.
- `configureWriteDbContext`: The callback that configures the write-side Entity Framework Core `DbContext`.
- `configure`: An optional callback that configures the host-owned Entity Framework pack options.
