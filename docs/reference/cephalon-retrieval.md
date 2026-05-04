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

Creates retrieval options with the default host-owned features enabled.

#### Properties

<a id="member-p-cephalon-retrieval-configuration-retrievaloptions-backgroundreindexcollectionids"></a>

##### `BackgroundReindexCollectionIds`

```csharp
IList<string> BackgroundReindexCollectionIds { get; }
```

Gets the optional collection ids included in background reindexing. When empty, every registered collection is included.

<a id="member-p-cephalon-retrieval-configuration-retrievaloptions-backgroundreindexinitialdelayseconds"></a>

##### `BackgroundReindexInitialDelaySeconds`

```csharp
int BackgroundReindexInitialDelaySeconds { get; set; }
```

Gets or sets the startup delay, in seconds, before the first background reindex run.

<a id="member-p-cephalon-retrieval-configuration-retrievaloptions-backgroundreindexintervalseconds"></a>

##### `BackgroundReindexIntervalSeconds`

```csharp
int BackgroundReindexIntervalSeconds { get; set; }
```

Gets or sets the interval, in seconds, between background reindex runs. Values less than one disable repeated runs after the optional startup run.

<a id="member-p-cephalon-retrieval-configuration-retrievaloptions-collections"></a>

##### `Collections`

```csharp
IList<KnowledgeCollectionDescriptor> Collections { get; }
```

Gets the host-defined knowledge collections that should be available to the retrieval runtime.

<a id="member-p-cephalon-retrieval-configuration-retrievaloptions-defaultquerylimit"></a>

##### `DefaultQueryLimit`

```csharp
int DefaultQueryLimit { get; set; }
```

Gets or sets the default maximum number of matches returned when a query request does not choose one explicitly.

<a id="member-p-cephalon-retrieval-configuration-retrievaloptions-enablebackgroundreindexing"></a>

##### `EnableBackgroundReindexing`

```csharp
bool EnableBackgroundReindexing { get; set; }
```

Gets or sets a value indicating whether Cephalon should run the opt-in background reindex scheduler.

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

<a id="member-p-cephalon-retrieval-configuration-retrievaloptions-freshnessstaleafterseconds"></a>

##### `FreshnessStaleAfterSeconds`

```csharp
int FreshnessStaleAfterSeconds { get; set; }
```

Gets or sets the number of seconds after which the latest successful index is considered stale for operator reporting.

<a id="member-p-cephalon-retrieval-configuration-retrievaloptions-maximumquerylimit"></a>

##### `MaximumQueryLimit`

```csharp
int MaximumQueryLimit { get; set; }
```

Gets or sets the upper bound applied to query result limits.

<a id="member-p-cephalon-retrieval-configuration-retrievaloptions-runbackgroundreindexonstartup"></a>

##### `RunBackgroundReindexOnStartup`

```csharp
bool RunBackgroundReindexOnStartup { get; set; }
```

Gets or sets a value indicating whether the scheduler should run once when the host starts.

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

<a id="member-m-cephalon-retrieval-registration-retrievalenginebuilderextensions-addretrieval-cephalon-engine-composition-enginebuilder-system-action-cephalon-retrieval-configuration-retrievaloptions"></a>

##### `AddRetrieval`

```csharp
EngineBuilder AddRetrieval(this EngineBuilder builder, Action<RetrievalOptions> configure)
```

Adds the retrieval runtime pack to the engine.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: An optional callback that configures the host-owned retrieval options.

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

<a id="type-cephalon-retrieval-services-iknowledgedocumentprovider"></a>

### `IKnowledgeDocumentProvider`

Loads documents for one knowledge collection so the retrieval runtime can build a managed index.

#### Declaration
```csharp
public interface IKnowledgeDocumentProvider
```

#### Properties

<a id="member-p-cephalon-retrieval-services-iknowledgedocumentprovider-collectionid"></a>

##### `CollectionId`

```csharp
string CollectionId { get; }
```

Gets the collection identifier served by this provider.

#### Methods

<a id="member-m-cephalon-retrieval-services-iknowledgedocumentprovider-loaddocumentsasync-cephalon-retrieval-services-knowledgedocumentprovidercontext-system-threading-cancellationtoken"></a>

##### `LoadDocumentsAsync`

```csharp
ValueTask<IReadOnlyList<KnowledgeDocument>> LoadDocumentsAsync(KnowledgeDocumentProviderContext context, CancellationToken cancellationToken)
```

Loads the current document set for the requested collection.

Returns: The current document set that should replace the collection index.

Parameters:
- `context`: The provider context for the current indexing request.
- `cancellationToken`: A token that can cancel the document load.

<a id="type-cephalon-retrieval-services-knowledgecollectiondescriptor"></a>

### `KnowledgeCollectionDescriptor`

Describes a knowledge collection that can be surfaced through the retrieval runtime pack.

#### Declaration
```csharp
public sealed class KnowledgeCollectionDescriptor
```

#### Constructors

<a id="member-m-cephalon-retrieval-services-knowledgecollectiondescriptor-ctor-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `KnowledgeCollectionDescriptor`

```csharp
KnowledgeCollectionDescriptor(string id, string displayName, string description, IReadOnlyList<string> tags)
```

Creates a new knowledge collection descriptor.

Parameters:
- `id`: The stable collection identifier.
- `displayName`: The operator-facing collection name.
- `description`: The human-readable description of the collection.
- `tags`: Optional tags that classify the collection.

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

<a id="type-cephalon-retrieval-services-knowledgedocument"></a>

### `KnowledgeDocument`

Describes one source document that can be indexed by the managed retrieval runtime.

#### Declaration
```csharp
public sealed class KnowledgeDocument
```

#### Constructors

<a id="member-m-cephalon-retrieval-services-knowledgedocument-ctor-system-string-system-string-system-string-system-uri-system-collections-generic-ireadonlylist-system-string-system-nullable-system-datetimeoffset-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `KnowledgeDocument`

```csharp
KnowledgeDocument(string id, string title, string content, Uri uri, IReadOnlyList<string> tags, DateTimeOffset? lastModifiedAtUtc, IReadOnlyDictionary<string, string> metadata)
```

Creates a knowledge document for managed indexing.

Parameters:
- `id`: The stable document identifier within its collection.
- `title`: The human-readable document title.
- `content`: The searchable document content.
- `uri`: An optional document URI for operator drill-down.
- `tags`: Optional tags that classify the document.
- `lastModifiedAtUtc`: The UTC timestamp when the source document was last modified.
- `metadata`: Optional operator-facing metadata attached to the document.

#### Properties

<a id="member-p-cephalon-retrieval-services-knowledgedocument-content"></a>

##### `Content`

```csharp
string Content { get; }
```

Gets the searchable document content.

<a id="member-p-cephalon-retrieval-services-knowledgedocument-id"></a>

##### `Id`

```csharp
string Id { get; }
```

Gets the stable document identifier within its collection.

<a id="member-p-cephalon-retrieval-services-knowledgedocument-lastmodifiedatutc"></a>

##### `LastModifiedAtUtc`

```csharp
DateTimeOffset? LastModifiedAtUtc { get; }
```

Gets the UTC timestamp when the source document was last modified.

<a id="member-p-cephalon-retrieval-services-knowledgedocument-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata attached to the document.

<a id="member-p-cephalon-retrieval-services-knowledgedocument-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; }
```

Gets the normalized tags that classify the document.

<a id="member-p-cephalon-retrieval-services-knowledgedocument-title"></a>

##### `Title`

```csharp
string Title { get; }
```

Gets the human-readable document title.

<a id="member-p-cephalon-retrieval-services-knowledgedocument-uri"></a>

##### `Uri`

```csharp
Uri Uri { get; }
```

Gets an optional document URI for operator drill-down.

<a id="type-cephalon-retrieval-services-knowledgedocumentprovidercontext"></a>

### `KnowledgeDocumentProviderContext`

Provides request context to a knowledge document provider during managed indexing.

#### Declaration
```csharp
public sealed class KnowledgeDocumentProviderContext
```

#### Constructors

<a id="member-m-cephalon-retrieval-services-knowledgedocumentprovidercontext-ctor-cephalon-retrieval-services-knowledgecollectiondescriptor-system-string-system-datetimeoffset-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `KnowledgeDocumentProviderContext`

```csharp
KnowledgeDocumentProviderContext(KnowledgeCollectionDescriptor collection, string runId, DateTimeOffset requestedAtUtc, string actorId, string correlationId, IReadOnlyDictionary<string, string> metadata)
```

Creates provider context for a managed indexing request.

Parameters:
- `collection`: The collection being indexed.
- `runId`: The stable indexing run identifier.
- `requestedAtUtc`: The UTC timestamp when indexing was requested.
- `actorId`: The optional actor that requested indexing.
- `correlationId`: The optional correlation identifier for the indexing run.
- `metadata`: Optional operator-facing metadata attached to the indexing request.

#### Properties

<a id="member-p-cephalon-retrieval-services-knowledgedocumentprovidercontext-actorid"></a>

##### `ActorId`

```csharp
string ActorId { get; }
```

Gets the optional actor that requested indexing.

<a id="member-p-cephalon-retrieval-services-knowledgedocumentprovidercontext-collection"></a>

##### `Collection`

```csharp
KnowledgeCollectionDescriptor Collection { get; }
```

Gets the collection being indexed.

<a id="member-p-cephalon-retrieval-services-knowledgedocumentprovidercontext-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for the indexing run.

<a id="member-p-cephalon-retrieval-services-knowledgedocumentprovidercontext-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata attached to the indexing request.

<a id="member-p-cephalon-retrieval-services-knowledgedocumentprovidercontext-requestedatutc"></a>

##### `RequestedAtUtc`

```csharp
DateTimeOffset RequestedAtUtc { get; }
```

Gets the UTC timestamp when indexing was requested.

<a id="member-p-cephalon-retrieval-services-knowledgedocumentprovidercontext-runid"></a>

##### `RunId`

```csharp
string RunId { get; }
```

Gets the stable indexing run identifier.

<a id="type-cephalon-retrieval-services-retrievaldiagnostics"></a>

### `RetrievalDiagnostics`

Defines the stable activity source, meter, activity, counter, and tag names emitted by the retrieval companion runtime. Names are sourced from `Retrieval` and `Retrieval` so the retrieval pack and observability companion packs share one canonical name set with the rest of the engine.

#### Declaration
```csharp
public static class RetrievalDiagnostics
```

#### Fields

<a id="member-f-cephalon-retrieval-services-retrievaldiagnostics-activitysourcename"></a>

##### `ActivitySourceName`

```csharp
const string ActivitySourceName
```

Gets the stable activity-source name emitted by the retrieval runtime.

<a id="member-f-cephalon-retrieval-services-retrievaldiagnostics-actoridtag"></a>

##### `ActorIdTag`

```csharp
const string ActorIdTag
```

Stable Cephalon-prefix tag carrying the optional actor identifier emitted on the activity.

<a id="member-f-cephalon-retrieval-services-retrievaldiagnostics-collectionidtag"></a>

##### `CollectionIdTag`

```csharp
const string CollectionIdTag
```

Stable Cephalon-prefix tag carrying the knowledge-collection identifier emitted on the activity.

<a id="member-f-cephalon-retrieval-services-retrievaldiagnostics-correlationidtag"></a>

##### `CorrelationIdTag`

```csharp
const string CorrelationIdTag
```

Stable Cephalon-prefix tag carrying the optional correlation identifier emitted on the activity.

<a id="member-f-cephalon-retrieval-services-retrievaldiagnostics-documentcounttag"></a>

##### `DocumentCountTag`

```csharp
const string DocumentCountTag
```

Stable Cephalon-prefix tag carrying the document count of the replacement index when one was published.

<a id="member-f-cephalon-retrieval-services-retrievaldiagnostics-indexeridtag"></a>

##### `IndexerIdTag`

```csharp
const string IndexerIdTag
```

Stable Cephalon-prefix tag carrying the indexer identifier responsible for the run.

<a id="member-f-cephalon-retrieval-services-retrievaldiagnostics-indexingoutcometag"></a>

##### `IndexingOutcomeTag`

```csharp
const string IndexingOutcomeTag
```

Stable Cephalon-prefix tag carrying the terminal indexing outcome emitted on the activity (started, succeeded, failed, or skipped).

<a id="member-f-cephalon-retrieval-services-retrievaldiagnostics-knowledgeindexactivityname"></a>

##### `KnowledgeIndexActivityName`

```csharp
const string KnowledgeIndexActivityName
```

Gets the stable activity name emitted around one managed knowledge-index run.

<a id="member-f-cephalon-retrieval-services-retrievaldiagnostics-knowledgeindexcountername"></a>

##### `KnowledgeIndexCounterName`

```csharp
const string KnowledgeIndexCounterName
```

Gets the stable counter name for completed knowledge-index runs.

<a id="member-f-cephalon-retrieval-services-retrievaldiagnostics-knowledgequeryactivityname"></a>

##### `KnowledgeQueryActivityName`

```csharp
const string KnowledgeQueryActivityName
```

Gets the stable activity name emitted around one managed knowledge-query.

<a id="member-f-cephalon-retrieval-services-retrievaldiagnostics-knowledgequerycountername"></a>

##### `KnowledgeQueryCounterName`

```csharp
const string KnowledgeQueryCounterName
```

Gets the stable counter name for completed knowledge-queries.

<a id="member-f-cephalon-retrieval-services-retrievaldiagnostics-matchcounttag"></a>

##### `MatchCountTag`

```csharp
const string MatchCountTag
```

Stable Cephalon-prefix tag carrying the count of matches returned by the query.

<a id="member-f-cephalon-retrieval-services-retrievaldiagnostics-metername"></a>

##### `MeterName`

```csharp
const string MeterName
```

Gets the stable meter name emitted by the retrieval runtime.

<a id="member-f-cephalon-retrieval-services-retrievaldiagnostics-providercounttag"></a>

##### `ProviderCountTag`

```csharp
const string ProviderCountTag
```

Stable Cephalon-prefix tag carrying the provider count consulted during the indexing run.

<a id="member-f-cephalon-retrieval-services-retrievaldiagnostics-queryengineidtag"></a>

##### `QueryEngineIdTag`

```csharp
const string QueryEngineIdTag
```

Stable Cephalon-prefix tag carrying the query-engine identifier responsible for the query.

<a id="member-f-cephalon-retrieval-services-retrievaldiagnostics-querylengthtag"></a>

##### `QueryLengthTag`

```csharp
const string QueryLengthTag
```

Stable Cephalon-prefix tag carrying the requested-or-effective query length emitted on the query activity. Query text itself is not emitted because retrieval queries can carry user content that should never reach exporters in the clear.

<a id="member-f-cephalon-retrieval-services-retrievaldiagnostics-querylimittag"></a>

##### `QueryLimitTag`

```csharp
const string QueryLimitTag
```

Stable Cephalon-prefix tag carrying the resolved query result limit (after default and maximum-limit clamps) emitted on the query activity.

<a id="member-f-cephalon-retrieval-services-retrievaldiagnostics-queryoutcometag"></a>

##### `QueryOutcomeTag`

```csharp
const string QueryOutcomeTag
```

Stable Cephalon-prefix tag carrying the terminal query outcome emitted on the activity (succeeded or failed). Unlike indexing, queries do not have a skipped or started state on the runtime path.

<a id="member-f-cephalon-retrieval-services-retrievaldiagnostics-runidtag"></a>

##### `RunIdTag`

```csharp
const string RunIdTag
```

Stable Cephalon-prefix tag carrying the knowledge-index run identifier emitted on the activity.
