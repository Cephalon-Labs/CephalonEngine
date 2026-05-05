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

<a id="member-m-cephalon-eventsourcing-hosting-eventsourcingservicecollectionextensions-addcephaloneventtype-1-microsoft-extensions-dependencyinjection-iservicecollection-system-string-system-string"></a>

##### `AddCephalonEventType`

```csharp
IServiceCollection AddCephalonEventType<TEvent>(this IServiceCollection services, string name, string[] aliases)
```

Registers a domain-event type with the Cephalon event-type registry.

Returns: The same service collection for fluent registration.

Type parameters:
- `TEvent`: The concrete domain-event type.

Parameters:
- `services`: The service collection to extend.
- `name`: An optional stable persisted name. Defaults to the event type's full name.
- `aliases`: Optional legacy names that should resolve to this event type.

<a id="member-m-cephalon-eventsourcing-hosting-eventsourcingservicecollectionextensions-addcephaloneventtyperegistry-microsoft-extensions-dependencyinjection-iservicecollection"></a>

##### `AddCephalonEventTypeRegistry`

```csharp
IServiceCollection AddCephalonEventTypeRegistry(this IServiceCollection services)
```

Adds the shared Cephalon event-type registry if it has not already been registered.

Returns: The same service collection for fluent registration.

Parameters:
- `services`: The service collection to extend.

<a id="member-m-cephalon-eventsourcing-hosting-eventsourcingservicecollectionextensions-addcephaloneventtypewithjsontypeinfo-1-microsoft-extensions-dependencyinjection-iservicecollection-system-text-json-serialization-metadata-jsontypeinfo-0-system-string-system-string"></a>

##### `AddCephalonEventTypeWithJsonTypeInfo`

```csharp
IServiceCollection AddCephalonEventTypeWithJsonTypeInfo<TEvent>(this IServiceCollection services, JsonTypeInfo<TEvent> jsonTypeInfo, string name, string[] aliases)
```

Registers a domain-event type with the Cephalon event-type registry using source-generated JSON metadata.

Returns: The same service collection for fluent registration.

Type parameters:
- `TEvent`: The concrete domain-event type.

Parameters:
- `services`: The service collection to extend.
- `jsonTypeInfo`: The source-generated JSON type information for the event payload.
- `name`: An optional stable persisted name. Defaults to the event type's full name.
- `aliases`: Optional legacy names that should resolve to this event type.

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

<a id="type-cephalon-eventsourcing-services-eventtypedescriptor"></a>

### `EventTypeDescriptor`

Describes one domain-event payload type known to the Cephalon event-type registry.

Remarks: The descriptor carries both the persisted event-type name and the serialization delegates for that type. This keeps provider event stores away from string-to-type reflection and gives future source generators a closed descriptor shape to emit.

#### Declaration
```csharp
public sealed class EventTypeDescriptor
```

#### Constructors

<a id="member-m-cephalon-eventsourcing-services-eventtypedescriptor-ctor-system-type-system-string-system-func-cephalon-abstractions-eventsourcing-idomainevent-system-string-system-func-system-string-cephalon-abstractions-eventsourcing-idomainevent-system-collections-generic-ienumerable-system-string"></a>

##### `EventTypeDescriptor`

```csharp
EventTypeDescriptor(Type eventType, string name, Func<IDomainEvent, string> serialize, Func<string, IDomainEvent> deserialize, IEnumerable<string> aliases)
```

Initializes a new instance of the `EventTypeDescriptor` class.

Parameters:
- `eventType`: The concrete domain-event CLR type.
- `name`: The stable persisted event-type name.
- `serialize`: The payload serializer for this event type.
- `deserialize`: The payload deserializer for this event type.
- `aliases`: Optional legacy names that should resolve to this event type.

#### Properties

<a id="member-p-cephalon-eventsourcing-services-eventtypedescriptor-aliases"></a>

##### `Aliases`

```csharp
IReadOnlyList<string> Aliases { get; }
```

Gets the legacy or alternate names that resolve to this event type.

<a id="member-p-cephalon-eventsourcing-services-eventtypedescriptor-eventtype"></a>

##### `EventType`

```csharp
Type EventType { get; }
```

Gets the concrete domain-event CLR type.

<a id="member-p-cephalon-eventsourcing-services-eventtypedescriptor-name"></a>

##### `Name`

```csharp
string Name { get; }
```

Gets the stable event-type name persisted by event stores.

#### Methods

<a id="member-m-cephalon-eventsourcing-services-eventtypedescriptor-create-1-system-string-system-collections-generic-ienumerable-system-string"></a>

##### `Create`

```csharp
EventTypeDescriptor Create<TEvent>(string name, IEnumerable<string> aliases)
```

Creates a descriptor for an event type using the default `System.Text.Json` generic serializer.

Returns: A descriptor for `TEvent`.

Type parameters:
- `TEvent`: The concrete domain-event type.

Parameters:
- `name`: An optional stable persisted name. Defaults to the event type's full name.
- `aliases`: Optional legacy names that should resolve to this event type.

<a id="member-m-cephalon-eventsourcing-services-eventtypedescriptor-createwithjsontypeinfo-1-system-text-json-serialization-metadata-jsontypeinfo-0-system-string-system-collections-generic-ienumerable-system-string"></a>

##### `CreateWithJsonTypeInfo`

```csharp
EventTypeDescriptor CreateWithJsonTypeInfo<TEvent>(JsonTypeInfo<TEvent> jsonTypeInfo, string name, IEnumerable<string> aliases)
```

Creates a descriptor for an event type using source-generated `JsonTypeInfo<T>` metadata.

Returns: A descriptor for `TEvent`.

Type parameters:
- `TEvent`: The concrete domain-event type.

Parameters:
- `jsonTypeInfo`: The source-generated JSON type information for the event payload.
- `name`: An optional stable persisted name. Defaults to the event type's full name.
- `aliases`: Optional legacy names that should resolve to this event type.

<a id="member-m-cephalon-eventsourcing-services-eventtypedescriptor-deserialize-system-string"></a>

##### `Deserialize`

```csharp
IDomainEvent Deserialize(string payload)
```

Deserializes a payload using this descriptor.

Returns: The deserialized domain event, or `null` when the payload cannot be deserialized.

Parameters:
- `payload`: The serialized payload.

<a id="member-m-cephalon-eventsourcing-services-eventtypedescriptor-serialize-cephalon-abstractions-eventsourcing-idomainevent"></a>

##### `Serialize`

```csharp
string Serialize(IDomainEvent evt)
```

Serializes a domain-event instance using this descriptor.

Returns: The serialized payload.

Parameters:
- `evt`: The event instance to serialize.

<a id="type-cephalon-eventsourcing-services-eventtyperegistry"></a>

### `EventTypeRegistry`

Default merged implementation of `IEventTypeRegistry`.

#### Declaration
```csharp
public sealed class EventTypeRegistry
```

#### Constructors

<a id="member-m-cephalon-eventsourcing-services-eventtyperegistry-ctor-system-collections-generic-ienumerable-cephalon-eventsourcing-services-ieventtypecontributor"></a>

##### `EventTypeRegistry`

```csharp
EventTypeRegistry(IEnumerable<IEventTypeContributor> contributors)
```

Initializes a new instance of the `EventTypeRegistry` class.

Parameters:
- `contributors`: The contributors whose descriptors should be merged.

#### Properties

<a id="member-p-cephalon-eventsourcing-services-eventtyperegistry-all"></a>

##### `All`

```csharp
IReadOnlyList<EventTypeDescriptor> All { get; }
```

<a id="member-p-cephalon-eventsourcing-services-eventtyperegistry-empty"></a>

##### `Empty`

```csharp
EventTypeRegistry Empty { get; }
```

Gets an empty event-type registry for direct provider construction scenarios.

#### Methods

<a id="member-m-cephalon-eventsourcing-services-eventtyperegistry-deserialize-system-string-system-string"></a>

##### `Deserialize`

```csharp
IDomainEvent Deserialize(string eventTypeName, string payload)
```

<a id="member-m-cephalon-eventsourcing-services-eventtyperegistry-getname-cephalon-abstractions-eventsourcing-idomainevent"></a>

##### `GetName`

```csharp
string GetName(IDomainEvent evt)
```

<a id="member-m-cephalon-eventsourcing-services-eventtyperegistry-serialize-cephalon-abstractions-eventsourcing-idomainevent"></a>

##### `Serialize`

```csharp
string Serialize(IDomainEvent evt)
```

<a id="member-m-cephalon-eventsourcing-services-eventtyperegistry-tryfindbyname-system-string-cephalon-eventsourcing-services-eventtypedescriptor"></a>

##### `TryFindByName`

```csharp
bool TryFindByName(string eventTypeName, out EventTypeDescriptor descriptor)
```

<a id="member-m-cephalon-eventsourcing-services-eventtyperegistry-tryfindbytype-system-type-cephalon-eventsourcing-services-eventtypedescriptor"></a>

##### `TryFindByType`

```csharp
bool TryFindByType(Type eventType, out EventTypeDescriptor descriptor)
```

<a id="type-cephalon-eventsourcing-services-ieventtypecontributor"></a>

### `IEventTypeContributor`

Contributes known domain-event type descriptors to the Cephalon event-type registry.

Remarks: Applications, modules, and future source generators use this contract to provide the closed set of domain-event payloads that an event store can serialize and deserialize without resolving CLR type names from persisted data.

#### Declaration
```csharp
public interface IEventTypeContributor
```

#### Methods

<a id="member-m-cephalon-eventsourcing-services-ieventtypecontributor-contribute"></a>

##### `Contribute`

```csharp
IReadOnlyList<EventTypeDescriptor> Contribute()
```

Returns the event-type descriptors contributed by this component.

Returns: The descriptors that should be merged into the runtime event-type registry.

<a id="type-cephalon-eventsourcing-services-ieventtyperegistry"></a>

### `IEventTypeRegistry`

Resolves, serializes, and deserializes the domain-event types known to a Cephalon runtime.

Remarks: Provider event stores use this registry instead of resolving persisted CLR type names with `Type.GetType`. The registry is intentionally closed and host-owned so trimming and Native AOT lanes can replace runtime type-name discovery with generated descriptors.

#### Declaration
```csharp
public interface IEventTypeRegistry
```

#### Properties

<a id="member-p-cephalon-eventsourcing-services-ieventtyperegistry-all"></a>

##### `All`

```csharp
IReadOnlyList<EventTypeDescriptor> All { get; }
```

Gets all event-type descriptors known to this registry.

#### Methods

<a id="member-m-cephalon-eventsourcing-services-ieventtyperegistry-deserialize-system-string-system-string"></a>

##### `Deserialize`

```csharp
IDomainEvent Deserialize(string eventTypeName, string payload)
```

Deserializes a persisted payload using the descriptor registered for the event-type name.

Returns: The rehydrated domain event.

Parameters:
- `eventTypeName`: The event-type name read from the event store.
- `payload`: The serialized event payload.

<a id="member-m-cephalon-eventsourcing-services-ieventtyperegistry-getname-cephalon-abstractions-eventsourcing-idomainevent"></a>

##### `GetName`

```csharp
string GetName(IDomainEvent evt)
```

Gets the persisted event-type name for a domain-event instance.

Returns: The stable event-type name that should be persisted with the payload.

Parameters:
- `evt`: The domain event being appended.

<a id="member-m-cephalon-eventsourcing-services-ieventtyperegistry-serialize-cephalon-abstractions-eventsourcing-idomainevent"></a>

##### `Serialize`

```csharp
string Serialize(IDomainEvent evt)
```

Serializes a domain-event payload using the descriptor registered for its concrete event type.

Returns: The serialized event payload.

Parameters:
- `evt`: The domain event to serialize.

<a id="member-m-cephalon-eventsourcing-services-ieventtyperegistry-tryfindbyname-system-string-cephalon-eventsourcing-services-eventtypedescriptor"></a>

##### `TryFindByName`

```csharp
bool TryFindByName(string eventTypeName, out EventTypeDescriptor descriptor)
```

Attempts to find a descriptor by its persisted event-type name or one of its aliases.

Returns: `true` when the registry contains a matching descriptor.

Parameters:
- `eventTypeName`: The persisted event-type name to resolve.
- `descriptor`: The resolved descriptor, when one exists.

<a id="member-m-cephalon-eventsourcing-services-ieventtyperegistry-tryfindbytype-system-type-cephalon-eventsourcing-services-eventtypedescriptor"></a>

##### `TryFindByType`

```csharp
bool TryFindByType(Type eventType, out EventTypeDescriptor descriptor)
```

Attempts to find a descriptor by the concrete event CLR type.

Returns: `true` when the registry contains a matching descriptor.

Parameters:
- `eventType`: The concrete event type to resolve.
- `descriptor`: The resolved descriptor, when one exists.
