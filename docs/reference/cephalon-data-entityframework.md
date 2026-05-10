# Cephalon.Data.EntityFramework

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Data.EntityFramework)
## Namespaces

- `Cephalon.Data.EntityFramework.Configuration`
- `Cephalon.Data.EntityFramework.Modeling`
- `Cephalon.Data.EntityFramework.Registration`
- `Cephalon.Data.EntityFramework.Services`

<a id="namespace-cephalon-data-entityframework-configuration"></a>

## Namespace Cephalon.Data.EntityFramework.Configuration

<a id="type-cephalon-data-entityframework-configuration-entityframeworkdatabaserolecontext"></a>

### `EntityFrameworkDatabaseRoleContext`

Describes one resolved `Engine:Databases` role as consumed by the Entity Framework pack.

#### Declaration
```csharp
public sealed class EntityFrameworkDatabaseRoleContext
```

#### Constructors

<a id="member-m-cephalon-data-entityframework-configuration-entityframeworkdatabaserolecontext-ctor-system-string-system-string-cephalon-abstractions-appmodel-databasetargetselection-cephalon-abstractions-appmodel-databaseruntimeselection-system-string"></a>

##### `EntityFrameworkDatabaseRoleContext`

```csharp
EntityFrameworkDatabaseRoleContext(string requestedRoleId, string resolvedRoleId, DatabaseTargetSelection target, DatabaseRuntimeSelection runtime, string connectionString)
```

Initializes a new instance of the `EntityFrameworkDatabaseRoleContext` class.

Parameters:
- `requestedRoleId`: The logical role the caller requested, such as `write` or `read`.
- `resolvedRoleId`: The logical role that ultimately supplied the effective database target.
- `target`: The effective database target metadata after applying any configured role reference.
- `runtime`: The merged runtime settings for the role.
- `connectionString`: The resolved connection string for the role.

#### Properties

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdatabaserolecontext-connectionstring"></a>

##### `ConnectionString`

```csharp
string ConnectionString { get; }
```

Gets the resolved connection string for the selected role.

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdatabaserolecontext-connectionstringname"></a>

##### `ConnectionStringName`

```csharp
string ConnectionStringName { get; }
```

Gets the selected named connection-string reference, if one was declared.

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdatabaserolecontext-isfallback"></a>

##### `IsFallback`

```csharp
bool IsFallback { get; }
```

Gets a value indicating whether the requested role resolved through another configured role.

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdatabaserolecontext-provider"></a>

##### `Provider`

```csharp
string Provider { get; }
```

Gets the selected provider identifier, if one was declared.

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdatabaserolecontext-requestedroleid"></a>

##### `RequestedRoleId`

```csharp
string RequestedRoleId { get; }
```

Gets the logical database role requested by the caller.

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdatabaserolecontext-resolvedroleid"></a>

##### `ResolvedRoleId`

```csharp
string ResolvedRoleId { get; }
```

Gets the logical database role that supplied the effective target.

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdatabaserolecontext-role"></a>

##### `Role`

```csharp
string Role { get; }
```

Gets the convenience role identifier used by most host callbacks.

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdatabaserolecontext-runtime"></a>

##### `Runtime`

```csharp
DatabaseRuntimeSelection Runtime { get; }
```

Gets the merged runtime settings for the selected role.

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdatabaserolecontext-schema"></a>

##### `Schema`

```csharp
string Schema { get; }
```

Gets the selected schema override, if one was declared.

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdatabaserolecontext-target"></a>

##### `Target`

```csharp
DatabaseTargetSelection Target { get; }
```

Gets the effective database target metadata after applying any configured role reference.

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdatabaserolecontext-usesnamedconnectionstring"></a>

##### `UsesNamedConnectionString`

```csharp
bool UsesNamedConnectionString { get; }
```

Gets a value indicating whether the role resolved through a named connection string.

<a id="type-cephalon-data-entityframework-configuration-entityframeworkdatabaseroleresolver"></a>

### `EntityFrameworkDatabaseRoleResolver`

Resolves `Engine:Databases` role selections into Entity Framework-specific role contexts.

#### Declaration
```csharp
public static class EntityFrameworkDatabaseRoleResolver
```

#### Methods

<a id="member-m-cephalon-data-entityframework-configuration-entityframeworkdatabaseroleresolver-resolvehistory-system-iserviceprovider"></a>

##### `ResolveHistory`

```csharp
EntityFrameworkDatabaseRoleContext ResolveHistory(IServiceProvider serviceProvider)
```

Resolves the audit-history database role.

Returns: The resolved Entity Framework database-role context.

Parameters:
- `serviceProvider`: The current service provider.

<a id="member-m-cephalon-data-entityframework-configuration-entityframeworkdatabaseroleresolver-resolveoutbox-system-iserviceprovider"></a>

##### `ResolveOutbox`

```csharp
EntityFrameworkDatabaseRoleContext ResolveOutbox(IServiceProvider serviceProvider)
```

Resolves the outbox database role, falling back to the write role when a dedicated outbox role is not configured.

Returns: The resolved Entity Framework database-role context.

Parameters:
- `serviceProvider`: The current service provider.

<a id="member-m-cephalon-data-entityframework-configuration-entityframeworkdatabaseroleresolver-resolveread-system-iserviceprovider"></a>

##### `ResolveRead`

```csharp
EntityFrameworkDatabaseRoleContext ResolveRead(IServiceProvider serviceProvider)
```

Resolves the read database role.

Returns: The resolved Entity Framework database-role context.

Parameters:
- `serviceProvider`: The current service provider.

<a id="member-m-cephalon-data-entityframework-configuration-entityframeworkdatabaseroleresolver-resolverole-system-iserviceprovider-system-string"></a>

##### `ResolveRole`

```csharp
EntityFrameworkDatabaseRoleContext ResolveRole(IServiceProvider serviceProvider, string requestedRoleId)
```

Resolves an arbitrary supported database role from `Engine:Databases`.

Returns: The resolved Entity Framework database-role context.

Parameters:
- `serviceProvider`: The current service provider.
- `requestedRoleId`: The logical database role identifier to resolve.

<a id="member-m-cephalon-data-entityframework-configuration-entityframeworkdatabaseroleresolver-resolvesharedwrite-system-iserviceprovider"></a>

##### `ResolveSharedWrite`

```csharp
EntityFrameworkDatabaseRoleContext ResolveSharedWrite(IServiceProvider serviceProvider)
```

Resolves the shared write role used when one `DbContext` type serves both reads and writes.

Returns: The resolved Entity Framework database-role context.

Parameters:
- `serviceProvider`: The current service provider.

<a id="member-m-cephalon-data-entityframework-configuration-entityframeworkdatabaseroleresolver-resolvewrite-system-iserviceprovider"></a>

##### `ResolveWrite`

```csharp
EntityFrameworkDatabaseRoleContext ResolveWrite(IServiceProvider serviceProvider)
```

Resolves the write database role.

Returns: The resolved Entity Framework database-role context.

Parameters:
- `serviceProvider`: The current service provider.

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

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdataoptions-registerprojections"></a>

##### `RegisterProjections`

```csharp
bool RegisterProjections { get; set; }
```

Gets or sets a value indicating whether the pack should register Entity Framework-backed projection infrastructure.

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdataoptions-registerprovidercapability"></a>

##### `RegisterProviderCapability`

```csharp
bool RegisterProviderCapability { get; set; }
```

Gets or sets a value indicating whether the pack should publish the provider capability.

<a id="member-p-cephalon-data-entityframework-configuration-entityframeworkdataoptions-usesenginedatabasetopology"></a>

##### `UsesEngineDatabaseTopology`

```csharp
bool UsesEngineDatabaseTopology { get; set; }
```

Gets or sets a value indicating whether the pack resolves read/write roles from `Engine:Databases`.

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

<a id="type-cephalon-data-entityframework-modeling-entityframeworkeventdispatchremediationcommandentry"></a>

### `EntityFrameworkEventDispatchRemediationCommandEntry`

Represents one durable event-dispatch remediation command result stored through Entity Framework Core.

#### Declaration
```csharp
public sealed class EntityFrameworkEventDispatchRemediationCommandEntry
```

#### Constructors

<a id="member-m-cephalon-data-entityframework-modeling-entityframeworkeventdispatchremediationcommandentry-ctor"></a>

##### `EntityFrameworkEventDispatchRemediationCommandEntry`

```csharp
EntityFrameworkEventDispatchRemediationCommandEntry()
```

Initializes a new instance of the `EntityFrameworkEventDispatchRemediationCommandEntry` class.

#### Properties

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkeventdispatchremediationcommandentry-actorid"></a>

##### `ActorId`

```csharp
string ActorId { get; set; }
```

Gets or sets the operator actor identifier when it was supplied with the command.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkeventdispatchremediationcommandentry-channelid"></a>

##### `ChannelId`

```csharp
string ChannelId { get; set; }
```

Gets or sets the event channel identifier associated with the targeted staged event.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkeventdispatchremediationcommandentry-commandid"></a>

##### `CommandId`

```csharp
string CommandId { get; set; }
```

Gets or sets the stable remediation command identifier.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkeventdispatchremediationcommandentry-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; set; }
```

Gets or sets the operator correlation identifier when it was supplied with the command.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkeventdispatchremediationcommandentry-dispatchoutcome"></a>

##### `DispatchOutcome`

```csharp
string DispatchOutcome { get; set; }
```

Gets or sets the dispatch-store outcome applied by the accepted command.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkeventdispatchremediationcommandentry-error"></a>

##### `Error`

```csharp
string Error { get; set; }
```

Gets or sets the operator-facing error summary when the command was rejected.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkeventdispatchremediationcommandentry-messageid"></a>

##### `MessageId`

```csharp
string MessageId { get; set; }
```

Gets or sets the staged event message identifier targeted by the command.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkeventdispatchremediationcommandentry-metadatajson"></a>

##### `MetadataJson`

```csharp
string MetadataJson { get; set; }
```

Gets or sets the serialized command metadata payload.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkeventdispatchremediationcommandentry-observedatutc"></a>

##### `ObservedAtUtc`

```csharp
DateTimeOffset ObservedAtUtc { get; set; }
```

Gets or sets the UTC timestamp when the command result was observed.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkeventdispatchremediationcommandentry-operationid"></a>

##### `OperationId`

```csharp
string OperationId { get; set; }
```

Gets or sets the remediation operation identifier requested by the operator.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkeventdispatchremediationcommandentry-outboxid"></a>

##### `OutboxId`

```csharp
string OutboxId { get; set; }
```

Gets or sets the outbox identifier that owned the targeted staged event.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkeventdispatchremediationcommandentry-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; set; }
```

Gets or sets the stable command outcome identifier.

<a id="member-p-cephalon-data-entityframework-modeling-entityframeworkeventdispatchremediationcommandentry-reason"></a>

##### `Reason`

```csharp
string Reason { get; set; }
```

Gets or sets the operator-facing command reason when it was supplied.

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

<a id="member-m-cephalon-data-entityframework-modeling-entityframeworkmodelbuilderextensions-configurecephaloneventdispatchremediationcommandjournal-microsoft-entityframeworkcore-modelbuilder-system-string"></a>

##### `ConfigureCephalonEventDispatchRemediationCommandJournal`

```csharp
ModelBuilder ConfigureCephalonEventDispatchRemediationCommandJournal(this ModelBuilder modelBuilder, string tableName)
```

Adds the Cephalon event-dispatch remediation command journal entity mapping to the supplied model.

Returns: The same model builder for fluent configuration.

Parameters:
- `modelBuilder`: The model builder to extend.
- `tableName`: The table name that should hold durable remediation command rows.

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

<a id="type-cephalon-data-entityframework-modeling-ientityframeworkeventdispatchremediationcommandjournalcontext"></a>

### `IEntityFrameworkEventDispatchRemediationCommandJournalContext`

Declares the write-side Entity Framework Core surface required by the durable event-dispatch remediation command journal.

#### Declaration
```csharp
public interface IEntityFrameworkEventDispatchRemediationCommandJournalContext
```

#### Properties

<a id="member-p-cephalon-data-entityframework-modeling-ientityframeworkeventdispatchremediationcommandjournalcontext-eventdispatchremediationcommandjournalentries"></a>

##### `EventDispatchRemediationCommandJournalEntries`

```csharp
DbSet<EntityFrameworkEventDispatchRemediationCommandEntry> EventDispatchRemediationCommandJournalEntries { get; }
```

Gets the durable remediation command journal rows owned by the current write-side `DbContext`.

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

<a id="member-m-cephalon-data-entityframework-registration-entityframeworkdataenginebuilderextensions-addentityframeworkdata-1-cephalon-engine-composition-enginebuilder-system-action-cephalon-data-entityframework-configuration-entityframeworkdatabaserolecontext-microsoft-entityframeworkcore-dbcontextoptionsbuilder-system-action-cephalon-data-entityframework-configuration-entityframeworkdataoptions"></a>

##### `AddEntityFrameworkData`

```csharp
EngineBuilder AddEntityFrameworkData<TDbContext>(this EngineBuilder builder, Action<EntityFrameworkDatabaseRoleContext, DbContextOptionsBuilder> configureDbContext, Action<EntityFrameworkDataOptions> configure)
```

Adds the Entity Framework Core data pack with one shared `DbContext` type configured from the engine-owned `Engine:Databases` topology.

Remarks: This overload keeps the physical connection topology inside `Engine:Databases` while leaving the provider package selection with the consuming host or provider companion pack.

Returns: The same engine builder for fluent composition.

Type parameters:
- `TDbContext`: The shared `DbContext` type.

Parameters:
- `builder`: The engine builder to extend.
- `configureDbContext`: The callback that selects the EF Core provider for the resolved role and applies provider-specific tuning such as retries.
- `configure`: An optional callback that configures the host-owned Entity Framework pack options.

<a id="member-m-cephalon-data-entityframework-registration-entityframeworkdataenginebuilderextensions-addentityframeworkdata-2-cephalon-engine-composition-enginebuilder-system-action-cephalon-data-entityframework-configuration-entityframeworkdatabaserolecontext-microsoft-entityframeworkcore-dbcontextoptionsbuilder-system-action-cephalon-data-entityframework-configuration-entityframeworkdataoptions"></a>

##### `AddEntityFrameworkData`

```csharp
EngineBuilder AddEntityFrameworkData<TReadDbContext, TWriteDbContext>(this EngineBuilder builder, Action<EntityFrameworkDatabaseRoleContext, DbContextOptionsBuilder> configureDbContext, Action<EntityFrameworkDataOptions> configure)
```

Adds the Entity Framework Core data pack with distinct read and write `DbContext` types configured from the engine-owned `Engine:Databases` topology.

Returns: The same engine builder for fluent composition.

Type parameters:
- `TReadDbContext`: The read-side `DbContext` type.
- `TWriteDbContext`: The write-side `DbContext` type.

Parameters:
- `builder`: The engine builder to extend.
- `configureDbContext`: The callback that selects the EF Core provider for each resolved role and applies provider-specific tuning such as retries.
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

<a id="namespace-cephalon-data-entityframework-services"></a>

## Namespace Cephalon.Data.EntityFramework.Services

<a id="type-cephalon-data-entityframework-services-entityframeworkdatabasemigrationhostedservice"></a>

### `EntityFrameworkDatabaseMigrationHostedService`

Applies startup schema changes for Entity Framework Core database-role targets selected through `Engine:Databases`.

#### Declaration
```csharp
public sealed class EntityFrameworkDatabaseMigrationHostedService
```

#### Constructors

<a id="member-m-cephalon-data-entityframework-services-entityframeworkdatabasemigrationhostedservice-ctor-system-iserviceprovider-cephalon-abstractions-appmodel-appprofile-system-collections-generic-ienumerable-cephalon-data-entityframework-services-entityframeworkdatabasemigrationregistration"></a>

##### `EntityFrameworkDatabaseMigrationHostedService`

```csharp
EntityFrameworkDatabaseMigrationHostedService(IServiceProvider serviceProvider, AppProfile appProfile, IEnumerable<EntityFrameworkDatabaseMigrationRegistration> registrations)
```

Applies startup schema changes for Entity Framework Core database-role targets selected through `Engine:Databases`.

#### Methods

<a id="member-m-cephalon-data-entityframework-services-entityframeworkdatabasemigrationhostedservice-startasync-system-threading-cancellationtoken"></a>

##### `StartAsync`

```csharp
Task StartAsync(CancellationToken cancellationToken)
```

Applies the configured startup migration policy for every targeted Entity Framework database role.

Parameters:
- `cancellationToken`: The token that cancels startup migration execution.

<a id="member-m-cephalon-data-entityframework-services-entityframeworkdatabasemigrationhostedservice-stopasync-system-threading-cancellationtoken"></a>

##### `StopAsync`

```csharp
Task StopAsync(CancellationToken cancellationToken)
```

Stops the hosted service. Entity Framework startup migration execution is synchronous during startup, so there is no background work to drain.

Returns: A completed task.

Parameters:
- `cancellationToken`: The token that cancels shutdown.

<a id="type-cephalon-data-entityframework-services-entityframeworkdatabasemigrationregistration"></a>

### `EntityFrameworkDatabaseMigrationRegistration`

Describes one Entity Framework Core `DbContext` type that can satisfy one or more logical `Engine:Databases` migration targets.

#### Declaration
```csharp
public sealed class EntityFrameworkDatabaseMigrationRegistration
```

#### Constructors

<a id="member-m-cephalon-data-entityframework-services-entityframeworkdatabasemigrationregistration-ctor-system-type-system-collections-generic-ireadonlylist-system-string"></a>

##### `EntityFrameworkDatabaseMigrationRegistration`

```csharp
EntityFrameworkDatabaseMigrationRegistration(Type dbContextType, IReadOnlyList<string> targetRoleIds)
```

Initializes a new instance of the `EntityFrameworkDatabaseMigrationRegistration` class.

Parameters:
- `dbContextType`: The `DbContext` type that can apply schema changes.
- `targetRoleIds`: The logical migration targets satisfied by the context.

#### Properties

<a id="member-p-cephalon-data-entityframework-services-entityframeworkdatabasemigrationregistration-dbcontexttype"></a>

##### `DbContextType`

```csharp
Type DbContextType { get; }
```

Gets the `DbContext` type that can apply schema changes.

<a id="member-p-cephalon-data-entityframework-services-entityframeworkdatabasemigrationregistration-targetroleids"></a>

##### `TargetRoleIds`

```csharp
IReadOnlyList<string> TargetRoleIds { get; }
```

Gets the logical migration targets satisfied by the context.
