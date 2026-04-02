# Cephalon.Eventing

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Eventing)
## Namespaces

- `Cephalon.Eventing.Configuration`
- `Cephalon.Eventing.Registration`
- `Cephalon.Eventing.Services`

<a id="namespace-cephalon-eventing-configuration"></a>

## Namespace Cephalon.Eventing.Configuration

<a id="type-cephalon-eventing-configuration-eventingoptions"></a>

### `EventingOptions`

Configures the built-in eventing runtime pack.

Remarks: These options seed the host-owned part of the eventing runtime. Installed modules can still contribute additional channels through `IEventChannelContributor`.

#### Declaration
```csharp
public sealed class EventingOptions
```

#### Constructors

<a id="member-m-cephalon-eventing-configuration-eventingoptions-ctor"></a>

##### `EventingOptions`

```csharp
EventingOptions()
```

Creates eventing options with the default host-owned features enabled.

#### Properties

<a id="member-p-cephalon-eventing-configuration-eventingoptions-channels"></a>

##### `Channels`

```csharp
IList<EventChannelDescriptor> Channels { get; }
```

Gets the host-defined event channels that should be available to the eventing runtime.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-enablepublishing"></a>

##### `EnablePublishing`

```csharp
bool EnablePublishing { get; set; }
```

Gets or sets a value indicating whether publishing features are enabled.

<a id="member-p-cephalon-eventing-configuration-eventingoptions-enablesubscriptions"></a>

##### `EnableSubscriptions`

```csharp
bool EnableSubscriptions { get; set; }
```

Gets or sets a value indicating whether subscription features are enabled.

<a id="namespace-cephalon-eventing-registration"></a>

## Namespace Cephalon.Eventing.Registration

<a id="type-cephalon-eventing-registration-eventingenginebuilderextensions"></a>

### `EventingEngineBuilderExtensions`

Registers the built-in eventing runtime pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class EventingEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-eventing-registration-eventingenginebuilderextensions-addeventing-cephalon-engine-composition-enginebuilder-system-action-cephalon-eventing-configuration-eventingoptions"></a>

##### `AddEventing`

```csharp
EngineBuilder AddEventing(this EngineBuilder builder, Action<EventingOptions> configure)
```

Adds the eventing runtime pack to the engine.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: An optional callback that configures the host-owned eventing options.

<a id="namespace-cephalon-eventing-services"></a>

## Namespace Cephalon.Eventing.Services

<a id="type-cephalon-eventing-services-eventchanneldescriptor"></a>

### `EventChannelDescriptor`

Describes an event channel that can be surfaced through the eventing runtime pack.

#### Declaration
```csharp
public sealed class EventChannelDescriptor
```

#### Constructors

<a id="member-m-cephalon-eventing-services-eventchanneldescriptor-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `EventChannelDescriptor`

```csharp
EventChannelDescriptor(string id, string displayName, string description, IReadOnlyList<string> tags)
```

Creates a new event channel descriptor.

Parameters:
- `id`: The stable channel identifier.
- `displayName`: The operator-facing channel name.
- `description`: The human-readable description of the channel.
- `tags`: Optional tags that classify the channel.

#### Properties

<a id="member-p-cephalon-eventing-services-eventchanneldescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the channel.

<a id="member-p-cephalon-eventing-services-eventchanneldescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing display name for the channel.

<a id="member-p-cephalon-eventing-services-eventchanneldescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable channel identifier.

<a id="member-p-cephalon-eventing-services-eventchanneldescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the normalized tag set associated with the channel.

<a id="type-cephalon-eventing-services-ieventchannelcatalog"></a>

### `IEventChannelCatalog`

Exposes the merged set of event channels available to the active eventing runtime.

#### Declaration
```csharp
public interface IEventChannelCatalog
```

#### Properties

<a id="member-p-cephalon-eventing-services-ieventchannelcatalog-channels"></a>

##### `Channels`

```csharp
IReadOnlyList<EventChannelDescriptor> Channels { get; }
```

Gets the effective channel set after host options and module contributors have both been applied.

#### Methods

<a id="member-m-cephalon-eventing-services-ieventchannelcatalog-tryget-system-string-cephalon-eventing-services-eventchanneldescriptor"></a>

##### `TryGet`

```csharp
bool TryGet(string channelId, out EventChannelDescriptor channel)
```

Attempts to resolve an event channel descriptor by identifier.

Returns: `true` when the channel exists; otherwise `false`.

Parameters:
- `channelId`: The channel identifier to resolve.
- `channel`: When this method returns, contains the resolved channel if found.

<a id="type-cephalon-eventing-services-ieventchannelcontributor"></a>

### `IEventChannelContributor`

Allows a module to contribute event channels into the active eventing runtime pack.

#### Declaration
```csharp
public interface IEventChannelContributor
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventchannelcontributor-registerchannels-cephalon-eventing-services-ieventchannelregistry"></a>

##### `RegisterChannels`

```csharp
void RegisterChannels(IEventChannelRegistry channels)
```

Registers one or more event channel descriptors with the supplied registry.

Parameters:
- `channels`: The registry that collects contributed channel descriptors.

<a id="type-cephalon-eventing-services-ieventchannelregistry"></a>

### `IEventChannelRegistry`

Collects event channel descriptors contributed to the active eventing runtime pack.

#### Declaration
```csharp
public interface IEventChannelRegistry
```

#### Methods

<a id="member-m-cephalon-eventing-services-ieventchannelregistry-add-cephalon-eventing-services-eventchanneldescriptor"></a>

##### `Add`

```csharp
void Add(EventChannelDescriptor channel)
```

Adds an event channel descriptor to the registry.

Parameters:
- `channel`: The channel descriptor to contribute.
