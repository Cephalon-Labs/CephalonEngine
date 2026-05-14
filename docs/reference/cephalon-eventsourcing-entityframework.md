# Cephalon.EventSourcing.EntityFramework

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.EventSourcing.EntityFramework)
## Namespaces

- `Cephalon.EventSourcing.EntityFramework`
- `Cephalon.EventSourcing.EntityFramework.Hosting`
- `Cephalon.EventSourcing.EntityFramework.Registration`

<a id="namespace-cephalon-eventsourcing-entityframework"></a>

## Namespace Cephalon.EventSourcing.EntityFramework

<a id="type-cephalon-eventsourcing-entityframework-entityframeworkevententry"></a>

### `EntityFrameworkEventEntry`

Represents one persisted domain event row stored by the Entity Framework event-store provider.

#### Declaration
```csharp
public sealed class EntityFrameworkEventEntry
```

#### Constructors

<a id="member-m-cephalon-eventsourcing-entityframework-entityframeworkevententry-ctor"></a>

##### `EntityFrameworkEventEntry`

```csharp
EntityFrameworkEventEntry()
```

Initializes a new instance of the `EntityFrameworkEventEntry` class.

#### Properties

<a id="member-p-cephalon-eventsourcing-entityframework-entityframeworkevententry-appendedatutc"></a>

##### `AppendedAtUtc`

```csharp
DateTime AppendedAtUtc { get; set; }
```

Gets or sets the UTC time at which the event was appended to the store.

<a id="member-p-cephalon-eventsourcing-entityframework-entityframeworkevententry-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; set; }
```

Gets or sets the correlation identifier associated with the event when known.

<a id="member-p-cephalon-eventsourcing-entityframework-entityframeworkevententry-eventtype"></a>

##### `EventType`

```csharp
string EventType { get; set; }
```

Gets or sets the stable Cephalon event-type registry name.

<a id="member-p-cephalon-eventsourcing-entityframework-entityframeworkevententry-id"></a>

##### `Id`

```csharp
long Id { get; set; }
```

Gets or sets the database-assigned row identifier.

<a id="member-p-cephalon-eventsourcing-entityframework-entityframeworkevententry-occurredatutc"></a>

##### `OccurredAtUtc`

```csharp
DateTime OccurredAtUtc { get; set; }
```

Gets or sets the UTC time at which the domain event occurred.

<a id="member-p-cephalon-eventsourcing-entityframework-entityframeworkevententry-payload"></a>

##### `Payload`

```csharp
string Payload { get; set; }
```

Gets or sets the serialized event payload.

<a id="member-p-cephalon-eventsourcing-entityframework-entityframeworkevententry-streamid"></a>

##### `StreamId`

```csharp
string StreamId { get; set; }
```

Gets or sets the stable logical stream identifier.

<a id="member-p-cephalon-eventsourcing-entityframework-entityframeworkevententry-streamversion"></a>

##### `StreamVersion`

```csharp
long StreamVersion { get; set; }
```

Gets or sets the zero-based optimistic stream version for the event.

<a id="member-p-cephalon-eventsourcing-entityframework-entityframeworkevententry-tenantid"></a>

##### `TenantId`

```csharp
string TenantId { get; set; }
```

Gets or sets the tenant identifier associated with the event when known.

<a id="type-cephalon-eventsourcing-entityframework-entityframeworkeventsnapshotentry"></a>

### `EntityFrameworkEventSnapshotEntry`

Represents one persisted aggregate snapshot row stored by the Entity Framework event-sourcing provider.

#### Declaration
```csharp
public sealed class EntityFrameworkEventSnapshotEntry
```

#### Constructors

<a id="member-m-cephalon-eventsourcing-entityframework-entityframeworkeventsnapshotentry-ctor"></a>

##### `EntityFrameworkEventSnapshotEntry`

```csharp
EntityFrameworkEventSnapshotEntry()
```

Initializes a new instance of the `EntityFrameworkEventSnapshotEntry` class.

#### Properties

<a id="member-p-cephalon-eventsourcing-entityframework-entityframeworkeventsnapshotentry-id"></a>

##### `Id`

```csharp
long Id { get; set; }
```

Gets or sets the database-assigned row identifier.

<a id="member-p-cephalon-eventsourcing-entityframework-entityframeworkeventsnapshotentry-payload"></a>

##### `Payload`

```csharp
string Payload { get; set; }
```

Gets or sets the serialized aggregate state payload.

<a id="member-p-cephalon-eventsourcing-entityframework-entityframeworkeventsnapshotentry-savedatutc"></a>

##### `SavedAtUtc`

```csharp
DateTime SavedAtUtc { get; set; }
```

Gets or sets the UTC time at which the snapshot was saved.

<a id="member-p-cephalon-eventsourcing-entityframework-entityframeworkeventsnapshotentry-statetype"></a>

##### `StateType`

```csharp
string StateType { get; set; }
```

Gets or sets the aggregate state type key represented by the snapshot payload.

<a id="member-p-cephalon-eventsourcing-entityframework-entityframeworkeventsnapshotentry-streamid"></a>

##### `StreamId`

```csharp
string StreamId { get; set; }
```

Gets or sets the stable logical stream identifier represented by the snapshot.

<a id="member-p-cephalon-eventsourcing-entityframework-entityframeworkeventsnapshotentry-streamversion"></a>

##### `StreamVersion`

```csharp
long StreamVersion { get; set; }
```

Gets or sets the zero-based stream version represented by the snapshot.

<a id="type-cephalon-eventsourcing-entityframework-entityframeworkeventsourcingconfiguration"></a>

### `EntityFrameworkEventSourcingConfiguration`

Applies the Cephalon event-store schema to an Entity Framework model.

#### Declaration
```csharp
public static class EntityFrameworkEventSourcingConfiguration
```

#### Methods

<a id="member-m-cephalon-eventsourcing-entityframework-entityframeworkeventsourcingconfiguration-configurecephalonevents-microsoft-entityframeworkcore-modelbuilder"></a>

##### `ConfigureCephalonEvents`

```csharp
void ConfigureCephalonEvents(ModelBuilder modelBuilder)
```

Configures the `CephalonEvents` table and indexes required by the Entity Framework event-store provider.

Parameters:
- `modelBuilder`: The model builder to extend.

<a id="type-cephalon-eventsourcing-entityframework-ientityframeworkeventcontext"></a>

### `IEntityFrameworkEventContext`

Represents the minimum Entity Framework context contract required by the Cephalon event-store provider.

#### Declaration
```csharp
public interface IEntityFrameworkEventContext
```

#### Properties

<a id="member-p-cephalon-eventsourcing-entityframework-ientityframeworkeventcontext-events"></a>

##### `Events`

```csharp
DbSet<EntityFrameworkEventEntry> Events { get; }
```

Gets the event rows persisted by the active event-store context.

<a id="namespace-cephalon-eventsourcing-entityframework-hosting"></a>

## Namespace Cephalon.EventSourcing.EntityFramework.Hosting

<a id="type-cephalon-eventsourcing-entityframework-hosting-entityframeworkeventsourcingservicecollectionextensions"></a>

### `EntityFrameworkEventSourcingServiceCollectionExtensions`

Registers the Entity Framework event-store provider used by Cephalon hosts.

#### Declaration
```csharp
public static class EntityFrameworkEventSourcingServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-eventsourcing-entityframework-hosting-entityframeworkeventsourcingservicecollectionextensions-addcephalonentityframeworkeventsourcing-1-microsoft-extensions-dependencyinjection-iservicecollection"></a>

##### `AddCephalonEntityFrameworkEventSourcing`

```csharp
IServiceCollection AddCephalonEntityFrameworkEventSourcing<TContext>(this IServiceCollection services)
```

Adds the Entity Framework event-store provider to the service collection.

Returns: The same service collection for fluent registration.

Type parameters:
- `TContext`: The DbContext type that persists event rows.

Parameters:
- `services`: The service collection to extend.

<a id="namespace-cephalon-eventsourcing-entityframework-registration"></a>

## Namespace Cephalon.EventSourcing.EntityFramework.Registration

<a id="type-cephalon-eventsourcing-entityframework-registration-entityframeworkeventsourcingenginebuilderextensions"></a>

### `EntityFrameworkEventSourcingEngineBuilderExtensions`

Registers the Entity Framework event-store provider with an `EngineBuilder`.

#### Declaration
```csharp
public static class EntityFrameworkEventSourcingEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-eventsourcing-entityframework-registration-entityframeworkeventsourcingenginebuilderextensions-addentityframeworkeventsourcing-1-cephalon-engine-composition-enginebuilder"></a>

##### `AddEntityFrameworkEventSourcing`

```csharp
EngineBuilder AddEntityFrameworkEventSourcing<TContext>(this EngineBuilder builder)
```

Adds the Entity Framework event-store provider to the engine.

Returns: The same engine builder for fluent composition.

Type parameters:
- `TContext`: The DbContext type that persists event rows.

Parameters:
- `builder`: The engine builder to extend.
