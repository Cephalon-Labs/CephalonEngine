# Cephalon.Retrieval

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Retrieval)
## Namespaces

- `Cephalon.Retrieval.Configuration`
- `Cephalon.Retrieval.Registration`
- `Cephalon.Retrieval.Services`

<a id="namespace-cephalon-retrieval-configuration"></a>

## Namespace Cephalon.Retrieval.Configuration

<a id="type-cephalon-retrieval-configuration-retrievaloptions"></a>

### `RetrievalOptions`

Configures the built-in retrieval runtime pack.

Remarks: These options seed the host-owned part of the retrieval runtime. Installed modules can still contribute additional knowledge collections through `IKnowledgeCollectionContributor`.

#### Declaration
```csharp
public sealed class RetrievalOptions
```

#### Constructors

<a id="member-m-cephalon-retrieval-configuration-retrievaloptions-ctor"></a>

##### `RetrievalOptions`

```csharp
RetrievalOptions()
```

#### Properties

<a id="member-p-cephalon-retrieval-configuration-retrievaloptions-collections"></a>

##### `Collections`

```csharp
IList<KnowledgeCollectionDescriptor> Collections { get; }
```

Gets the host-defined knowledge collections that should be available to the retrieval runtime.

<a id="member-p-cephalon-retrieval-configuration-retrievaloptions-enableingestion"></a>

##### `EnableIngestion`

```csharp
bool EnableIngestion { get; set; }
```

Gets or sets a value indicating whether ingestion features are enabled.

<a id="member-p-cephalon-retrieval-configuration-retrievaloptions-enablequerying"></a>

##### `EnableQuerying`

```csharp
bool EnableQuerying { get; set; }
```

Gets or sets a value indicating whether query features are enabled.

<a id="namespace-cephalon-retrieval-registration"></a>

## Namespace Cephalon.Retrieval.Registration

<a id="type-cephalon-retrieval-registration-retrievalenginebuilderextensions"></a>

### `RetrievalEngineBuilderExtensions`

Registers the built-in retrieval runtime pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class RetrievalEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-retrieval-registration-retrievalenginebuilderextensions-addretrieval-cephalon-engine-composition-enginebuilder-system-action-1-cephalon-retrieval-configuration-retrievaloptions"></a>

##### `AddRetrieval`

```csharp
EngineBuilder AddRetrieval(this EngineBuilder builder, Action<RetrievalOptions> configure)
```

<a id="namespace-cephalon-retrieval-services"></a>

## Namespace Cephalon.Retrieval.Services

<a id="type-cephalon-retrieval-services-iknowledgecatalog"></a>

### `IKnowledgeCatalog`

Exposes the merged set of knowledge collections available to the active retrieval runtime.

#### Declaration
```csharp
public interface IKnowledgeCatalog
```

#### Properties

<a id="member-p-cephalon-retrieval-services-iknowledgecatalog-collections"></a>

##### `Collections`

```csharp
IReadOnlyList<KnowledgeCollectionDescriptor> Collections { get; }
```

Gets the effective collection set after host options and module contributors have both been applied.

#### Methods

<a id="member-m-cephalon-retrieval-services-iknowledgecatalog-tryget-system-string-cephalon-retrieval-services-knowledgecollectiondescriptor"></a>

##### `TryGet`

```csharp
bool TryGet(string collectionId, out KnowledgeCollectionDescriptor collection)
```

Attempts to resolve a knowledge collection descriptor by identifier.

Returns: `true` when the collection exists; otherwise `false`.

Parameters:
- `collectionId`: The collection identifier to resolve.
- `collection`: When this method returns, contains the resolved collection if found.

<a id="type-cephalon-retrieval-services-iknowledgecollectioncontributor"></a>

### `IKnowledgeCollectionContributor`

Allows a module to contribute knowledge collections into the active retrieval runtime pack.

#### Declaration
```csharp
public interface IKnowledgeCollectionContributor
```

#### Methods

<a id="member-m-cephalon-retrieval-services-iknowledgecollectioncontributor-registercollections-cephalon-retrieval-services-iknowledgecollectionregistry"></a>

##### `RegisterCollections`

```csharp
void RegisterCollections(IKnowledgeCollectionRegistry collections)
```

Registers one or more knowledge collection descriptors with the supplied registry.

Parameters:
- `collections`: The registry that collects contributed collection descriptors.

<a id="type-cephalon-retrieval-services-iknowledgecollectionregistry"></a>

### `IKnowledgeCollectionRegistry`

Collects knowledge collection descriptors contributed to the active retrieval runtime pack.

#### Declaration
```csharp
public interface IKnowledgeCollectionRegistry
```

#### Methods

<a id="member-m-cephalon-retrieval-services-iknowledgecollectionregistry-add-cephalon-retrieval-services-knowledgecollectiondescriptor"></a>

##### `Add`

```csharp
void Add(KnowledgeCollectionDescriptor collection)
```

Adds a knowledge collection descriptor to the registry.

Parameters:
- `collection`: The collection descriptor to contribute.

<a id="type-cephalon-retrieval-services-knowledgecollectiondescriptor"></a>

### `KnowledgeCollectionDescriptor`

Describes a knowledge collection that can be surfaced through the retrieval runtime pack.

#### Declaration
```csharp
public sealed class KnowledgeCollectionDescriptor
```

#### Constructors

<a id="member-m-cephalon-retrieval-services-knowledgecollectiondescriptor-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-1-system-string"></a>

##### `KnowledgeCollectionDescriptor`

```csharp
KnowledgeCollectionDescriptor(string id, string displayName, string description, IReadOnlyList<string> tags)
```

#### Properties

<a id="member-p-cephalon-retrieval-services-knowledgecollectiondescriptor-description"></a>

##### `Description`

```csharp
string Description { get; }
```

Gets the human-readable description of the collection.

<a id="member-p-cephalon-retrieval-services-knowledgecollectiondescriptor-displayname"></a>

##### `DisplayName`

```csharp
string DisplayName { get; }
```

Gets the operator-facing display name for the collection.

<a id="member-p-cephalon-retrieval-services-knowledgecollectiondescriptor-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable collection identifier.

<a id="member-p-cephalon-retrieval-services-knowledgecollectiondescriptor-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the normalized tag set associated with the collection.
