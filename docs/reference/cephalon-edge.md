# Cephalon.Edge

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Edge)
## Namespaces

- `Cephalon.Edge.Configuration`
- `Cephalon.Edge.Registration`
- `Cephalon.Edge.Services`

<a id="namespace-cephalon-edge-configuration"></a>

## Namespace Cephalon.Edge.Configuration

<a id="type-cephalon-edge-configuration-edgeruntimeoptions"></a>

### `EdgeRuntimeOptions`

Configures the built-in edge runtime pack.

Remarks: These options seed the host-owned part of the edge runtime. Installed modules can still contribute additional nodes through `IEdgeNodeContributor`.

#### Declaration
```csharp
public sealed class EdgeRuntimeOptions
```

#### Constructors

<a id="member-m-cephalon-edge-configuration-edgeruntimeoptions-ctor"></a>

##### `EdgeRuntimeOptions`

```csharp
EdgeRuntimeOptions()
```

Creates edge runtime options with the default host-owned features enabled.

#### Properties

<a id="member-p-cephalon-edge-configuration-edgeruntimeoptions-enableofflinemode"></a>

##### `EnableOfflineMode`

```csharp
bool EnableOfflineMode { get; set; }
```

Gets or sets a value indicating whether offline mode features are enabled.

<a id="member-p-cephalon-edge-configuration-edgeruntimeoptions-enablesynchronization"></a>

##### `EnableSynchronization`

```csharp
bool EnableSynchronization { get; set; }
```

Gets or sets a value indicating whether synchronization features are enabled.

<a id="member-p-cephalon-edge-configuration-edgeruntimeoptions-nodes"></a>

##### `Nodes`

```csharp
IList<EdgeNodeDescriptor> Nodes { get; }
```

Gets the host-defined edge nodes that should be available to the edge runtime.

<a id="namespace-cephalon-edge-registration"></a>

## Namespace Cephalon.Edge.Registration

<a id="type-cephalon-edge-registration-edgeenginebuilderextensions"></a>

### `EdgeEngineBuilderExtensions`

Registers the built-in edge runtime pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class EdgeEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-edge-registration-edgeenginebuilderextensions-addedge-cephalon-engine-composition-enginebuilder-system-action-cephalon-edge-configuration-edgeruntimeoptions"></a>

##### `AddEdge`

```csharp
EngineBuilder AddEdge(this EngineBuilder builder, Action<EdgeRuntimeOptions> configure)
```

Adds the edge runtime pack to the engine.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: An optional callback that configures the host-owned edge runtime options.

<a id="namespace-cephalon-edge-services"></a>

## Namespace Cephalon.Edge.Services

<a id="type-cephalon-edge-services-edgenodedescriptor"></a>

### `EdgeNodeDescriptor`

Describes an edge node that can be surfaced through the edge runtime pack.

#### Declaration
```csharp
public sealed class EdgeNodeDescriptor
```

#### Constructors

<a id="member-m-cephalon-edge-services-edgenodedescriptor-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `EdgeNodeDescriptor`

```csharp
EdgeNodeDescriptor(string id, string displayName, string description, IReadOnlyList<string> tags)
```

Creates a new edge node descriptor.

Parameters:
- `id`: The stable node identifier.
- `displayName`: The operator-facing node name.
- `description`: The human-readable description of the node.
- `tags`: Optional tags that classify the node.

#### Properties

<a id="member-p-cephalon-edge-services-edgenodedescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the node.

<a id="member-p-cephalon-edge-services-edgenodedescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing display name for the node.

<a id="member-p-cephalon-edge-services-edgenodedescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable node identifier.

<a id="member-p-cephalon-edge-services-edgenodedescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the normalized tag set associated with the node.

<a id="type-cephalon-edge-services-iedgenodecatalog"></a>

### `IEdgeNodeCatalog`

Exposes the merged set of edge nodes available to the active edge runtime.

#### Declaration
```csharp
public interface IEdgeNodeCatalog
```

#### Properties

<a id="member-p-cephalon-edge-services-iedgenodecatalog-nodes"></a>

##### `Nodes`

```csharp
IReadOnlyList<EdgeNodeDescriptor> Nodes { get; }
```

Gets the effective node set after host options and module contributors have both been applied.

#### Methods

<a id="member-m-cephalon-edge-services-iedgenodecatalog-tryget-system-string-cephalon-edge-services-edgenodedescriptor"></a>

##### `TryGet`

```csharp
bool TryGet(string nodeId, out EdgeNodeDescriptor node)
```

Attempts to resolve an edge node descriptor by identifier.

Returns: `true` when the node exists; otherwise `false`.

Parameters:
- `nodeId`: The node identifier to resolve.
- `node`: When this method returns, contains the resolved node if found.

<a id="type-cephalon-edge-services-iedgenodecontributor"></a>

### `IEdgeNodeContributor`

Allows a module to contribute edge nodes into the active edge runtime pack.

#### Declaration
```csharp
public interface IEdgeNodeContributor
```

#### Methods

<a id="member-m-cephalon-edge-services-iedgenodecontributor-registernodes-cephalon-edge-services-iedgenoderegistry"></a>

##### `RegisterNodes`

```csharp
void RegisterNodes(IEdgeNodeRegistry nodes)
```

Registers one or more edge node descriptors with the supplied registry.

Parameters:
- `nodes`: The registry that collects contributed node descriptors.

<a id="type-cephalon-edge-services-iedgenoderegistry"></a>

### `IEdgeNodeRegistry`

Collects edge node descriptors contributed to the active edge runtime pack.

#### Declaration
```csharp
public interface IEdgeNodeRegistry
```

#### Methods

<a id="member-m-cephalon-edge-services-iedgenoderegistry-add-cephalon-edge-services-edgenodedescriptor"></a>

##### `Add`

```csharp
void Add(EdgeNodeDescriptor node)
```

Adds an edge node descriptor to the registry.

Parameters:
- `node`: The node descriptor to contribute.
