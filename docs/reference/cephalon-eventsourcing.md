# Cephalon.EventSourcing

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.EventSourcing)
## Namespaces

- `Cephalon.EventSourcing.Configuration`
- `Cephalon.EventSourcing.Hosting`
- `Cephalon.EventSourcing.Registration`
- `Cephalon.EventSourcing.Services`

<a id="namespace-cephalon-eventsourcing-configuration"></a>

## Namespace Cephalon.EventSourcing.Configuration

<a id="type-cephalon-eventsourcing-configuration-eventsourcingoptions"></a>

### `EventSourcingOptions`

Describes the host-owned options for the runtime-neutral Cephalon event-sourcing pack.

#### Declaration
```csharp
public sealed class EventSourcingOptions
```

#### Constructors

<a id="member-m-cephalon-eventsourcing-configuration-eventsourcingoptions-ctor"></a>

##### `EventSourcingOptions`

```csharp
EventSourcingOptions()
```

Initializes a new instance of the `EventSourcingOptions` class.

#### Fields

<a id="member-f-cephalon-eventsourcing-configuration-eventsourcingoptions-sectionname"></a>

##### `SectionName`

```csharp
const string SectionName
```

The configuration section that owns the host-level event-sourcing settings.

#### Properties

<a id="member-p-cephalon-eventsourcing-configuration-eventsourcingoptions-defaultprovider"></a>

##### `DefaultProvider`

```csharp
string DefaultProvider { get; set; }
```

Gets or sets the default event-store provider identifier.

<a id="member-p-cephalon-eventsourcing-configuration-eventsourcingoptions-enablesnapshots"></a>

##### `EnableSnapshots`

```csharp
bool EnableSnapshots { get; set; }
```

Gets or sets a value indicating whether snapshot-aware paths are enabled.

<a id="namespace-cephalon-eventsourcing-hosting"></a>

## Namespace Cephalon.EventSourcing.Hosting

<a id="type-cephalon-eventsourcing-hosting-eventsourcingservicecollectionextensions"></a>

### `EventSourcingServiceCollectionExtensions`

Registers the runtime-neutral event-sourcing services used by Cephalon hosts.

#### Declaration
```csharp
public static class EventSourcingServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-eventsourcing-hosting-eventsourcingservicecollectionextensions-addcephaloneventsourcing-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-eventsourcing-configuration-eventsourcingoptions"></a>

##### `AddCephalonEventSourcing`

```csharp
IServiceCollection AddCephalonEventSourcing(this IServiceCollection services, Action<EventSourcingOptions> configure)
```

Adds the Cephalon event-sourcing baseline services to the service collection.

Returns: The same service collection for fluent registration.

Parameters:
- `services`: The service collection to extend.
- `configure`: An optional callback that configures the host-owned event-sourcing options.

<a id="namespace-cephalon-eventsourcing-registration"></a>

## Namespace Cephalon.EventSourcing.Registration

<a id="type-cephalon-eventsourcing-registration-eventsourcingenginebuilderextensions"></a>

### `EventSourcingEngineBuilderExtensions`

Registers the runtime-neutral Cephalon event-sourcing companion pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class EventSourcingEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-eventsourcing-registration-eventsourcingenginebuilderextensions-addeventsourcing-cephalon-engine-composition-enginebuilder-system-action-cephalon-eventsourcing-configuration-eventsourcingoptions"></a>

##### `AddEventSourcing`

```csharp
EngineBuilder AddEventSourcing(this EngineBuilder builder, Action<EventSourcingOptions> configure)
```

Adds the Cephalon event-sourcing companion pack to the engine.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: An optional callback that configures host-owned event-sourcing options.

<a id="namespace-cephalon-eventsourcing-services"></a>

## Namespace Cephalon.EventSourcing.Services

<a id="type-cephalon-eventsourcing-services-aggregatehydrator-taggregate-tstate"></a>

### `AggregateHydrator<TAggregate, TState>`

Rehydrates aggregate state by replaying domain events from an event store.

#### Declaration
```csharp
public sealed class AggregateHydrator<TAggregate, TState>
```

#### Constructors

<a id="member-m-cephalon-eventsourcing-services-aggregatehydrator-2-ctor"></a>

##### `AggregateHydrator<TAggregate, TState>`

```csharp
AggregateHydrator<TAggregate, TState>()
```

Initializes a new instance of the `AggregateHydrator<T1, T2>` class.

#### Methods

<a id="member-m-cephalon-eventsourcing-services-aggregatehydrator-2-hydrateasync-cephalon-abstractions-eventsourcing-ieventstore-system-string-system-int64-system-threading-cancellationtoken"></a>

##### `HydrateAsync`

```csharp
Task<ValueTuple<TState, long>> HydrateAsync(IEventStore eventStore, string streamId, long fromVersion, CancellationToken cancellationToken)
```

Rehydrates one aggregate state by replaying the requested event stream.

Returns: The rehydrated aggregate state and the latest version applied.

Parameters:
- `eventStore`: The event store to read from.
- `streamId`: The stable stream identifier.
- `fromVersion`: The first version to replay. The default is `0`.
- `cancellationToken`: The token that cancels the operation.

<a id="type-cephalon-eventsourcing-services-eventstreamcatalog"></a>

### `EventStreamCatalog`

Exposes the merged set of event-stream descriptors contributed to the active runtime.

#### Declaration
```csharp
public sealed class EventStreamCatalog
```

#### Constructors

<a id="member-m-cephalon-eventsourcing-services-eventstreamcatalog-ctor-system-collections-generic-ienumerable-cephalon-abstractions-eventsourcing-ieventstorecontributor"></a>

##### `EventStreamCatalog`

```csharp
EventStreamCatalog(IEnumerable<IEventStoreContributor> contributors)
```

Initializes a new instance of the `EventStreamCatalog` class.

Parameters:
- `contributors`: The contributors that project event-stream descriptors.

#### Properties

<a id="member-p-cephalon-eventsourcing-services-eventstreamcatalog-all"></a>

##### `All`

```csharp
IReadOnlyList<EventStreamDescriptor> All { get; }
```

Gets all event-stream descriptors contributed to the current runtime.

#### Methods

<a id="member-m-cephalon-eventsourcing-services-eventstreamcatalog-findbyid-system-string"></a>

##### `FindById`

```csharp
EventStreamDescriptor FindById(string id)
```

Finds one event-stream descriptor by its stable identifier.

Returns: The matching descriptor, or `null` when none match.

Parameters:
- `id`: The event-stream identifier to resolve.

<a id="member-m-cephalon-eventsourcing-services-eventstreamcatalog-getbyprovider-system-string"></a>

##### `GetByProvider`

```csharp
IReadOnlyList<EventStreamDescriptor> GetByProvider(string provider)
```

Gets the event-stream descriptors backed by the requested provider identifier.

Returns: The matching descriptors, or an empty list when the provider contributes none.

Parameters:
- `provider`: The provider identifier to filter by.

<a id="type-cephalon-eventsourcing-services-eventstreamregistry"></a>

### `EventStreamRegistry`

Collects event-stream descriptors registered by the active host and companion packs.

#### Declaration
```csharp
public sealed class EventStreamRegistry
```

#### Constructors

<a id="member-m-cephalon-eventsourcing-services-eventstreamregistry-ctor"></a>

##### `EventStreamRegistry`

```csharp
EventStreamRegistry()
```

Initializes a new instance of the `EventStreamRegistry` class.

#### Methods

<a id="member-m-cephalon-eventsourcing-services-eventstreamregistry-contribute"></a>

##### `Contribute`

```csharp
IReadOnlyList<EventStreamDescriptor> Contribute()
```

Returns the descriptors that have been registered with the current registry instance.

Returns: The registered descriptors.

<a id="member-m-cephalon-eventsourcing-services-eventstreamregistry-register-cephalon-abstractions-eventsourcing-eventstreamdescriptor"></a>

##### `Register`

```csharp
void Register(EventStreamDescriptor descriptor)
```

Registers one event-stream descriptor with the registry.

Parameters:
- `descriptor`: The descriptor to register.
