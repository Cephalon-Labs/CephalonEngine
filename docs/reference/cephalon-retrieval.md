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

<a id="type-cephalon-retrieval-services-iknowledgeindexcatalog"></a>

### `IKnowledgeIndexCatalog`

Exposes operator-facing index and query execution state for the active retrieval runtime.

#### Declaration
```csharp
public interface IKnowledgeIndexCatalog
```

#### Properties

<a id="member-p-cephalon-retrieval-services-iknowledgeindexcatalog-states"></a>

##### `States`

```csharp
IReadOnlyList<KnowledgeIndexState> States { get; }
```

Gets the latest index state reported for registered knowledge collections.

#### Methods

<a id="member-m-cephalon-retrieval-services-iknowledgeindexcatalog-getbycollectionid-system-string"></a>

##### `GetByCollectionId`

```csharp
KnowledgeIndexState GetByCollectionId(string collectionId)
```

Gets the latest index state for a collection when one exists.

Returns: The latest state for the collection, or `null` when no activity has been recorded.

Parameters:
- `collectionId`: The collection identifier to resolve.

<a id="member-m-cephalon-retrieval-services-iknowledgeindexcatalog-tryget-system-string-cephalon-retrieval-services-knowledgeindexstate"></a>

##### `TryGet`

```csharp
bool TryGet(string collectionId, out KnowledgeIndexState state)
```

Attempts to resolve the latest index state for a collection.

Returns: `true` when state exists; otherwise `false`.

Parameters:
- `collectionId`: The collection identifier to resolve.
- `state`: When this method returns, contains the state if one was recorded.

<a id="type-cephalon-retrieval-services-iknowledgeindexer"></a>

### `IKnowledgeIndexer`

Builds and replaces managed indexes for registered knowledge collections.

#### Declaration
```csharp
public interface IKnowledgeIndexer
```

#### Methods

<a id="member-m-cephalon-retrieval-services-iknowledgeindexer-indexasync-cephalon-retrieval-services-knowledgeindexingrequest-system-threading-cancellationtoken"></a>

##### `IndexAsync`

```csharp
ValueTask<KnowledgeIndexingResult> IndexAsync(KnowledgeIndexingRequest request, CancellationToken cancellationToken)
```

Indexes the documents supplied by providers for the requested collection.

Returns: The final indexing result recorded for operator introspection.

Parameters:
- `request`: The indexing request to execute.
- `cancellationToken`: A token that can cancel the indexing run.

<a id="type-cephalon-retrieval-services-iknowledgequeryengine"></a>

### `IKnowledgeQueryEngine`

Executes managed retrieval queries over indexed knowledge documents.

#### Declaration
```csharp
public interface IKnowledgeQueryEngine
```

#### Methods

<a id="member-m-cephalon-retrieval-services-iknowledgequeryengine-queryasync-cephalon-retrieval-services-knowledgequeryrequest-system-threading-cancellationtoken"></a>

##### `QueryAsync`

```csharp
ValueTask<KnowledgeQueryResult> QueryAsync(KnowledgeQueryRequest request, CancellationToken cancellationToken)
```

Queries the active managed index for a collection.

Returns: The query result, including any ranked matches found in the current index.

Parameters:
- `request`: The query request to execute.
- `cancellationToken`: A token that can cancel the query execution.

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

<a id="type-cephalon-retrieval-services-knowledgeindexfreshnessstates"></a>

### `KnowledgeIndexFreshnessStates`

Defines stable freshness state identifiers for managed retrieval indexes.

#### Declaration
```csharp
public static class KnowledgeIndexFreshnessStates
```

#### Fields

<a id="member-f-cephalon-retrieval-services-knowledgeindexfreshnessstates-failed"></a>

##### `Failed`

```csharp
const string Failed
```

The latest indexing run failed.

<a id="member-f-cephalon-retrieval-services-knowledgeindexfreshnessstates-fresh"></a>

##### `Fresh`

```csharp
const string Fresh
```

The latest successful index is within the configured freshness window.

<a id="member-f-cephalon-retrieval-services-knowledgeindexfreshnessstates-notindexed"></a>

##### `NotIndexed`

```csharp
const string NotIndexed
```

The collection has not been indexed.

<a id="member-f-cephalon-retrieval-services-knowledgeindexfreshnessstates-skipped"></a>

##### `Skipped`

```csharp
const string Skipped
```

The latest indexing run was skipped.

<a id="member-f-cephalon-retrieval-services-knowledgeindexfreshnessstates-stale"></a>

##### `Stale`

```csharp
const string Stale
```

The latest successful index is older than the configured freshness window.

<a id="type-cephalon-retrieval-services-knowledgeindexingoutcomes"></a>

### `KnowledgeIndexingOutcomes`

Defines stable outcome identifiers emitted by managed retrieval indexing.

#### Declaration
```csharp
public static class KnowledgeIndexingOutcomes
```

#### Fields

<a id="member-f-cephalon-retrieval-services-knowledgeindexingoutcomes-failed"></a>

##### `Failed`

```csharp
const string Failed
```

Indexing failed before a replacement index could be published.

<a id="member-f-cephalon-retrieval-services-knowledgeindexingoutcomes-skipped"></a>

##### `Skipped`

```csharp
const string Skipped
```

Indexing was skipped because required runtime inputs were not available.

<a id="member-f-cephalon-retrieval-services-knowledgeindexingoutcomes-started"></a>

##### `Started`

```csharp
const string Started
```

Indexing started for a collection.

<a id="member-f-cephalon-retrieval-services-knowledgeindexingoutcomes-succeeded"></a>

##### `Succeeded`

```csharp
const string Succeeded
```

Indexing completed successfully.

<a id="type-cephalon-retrieval-services-knowledgeindexingrequest"></a>

### `KnowledgeIndexingRequest`

Describes a managed indexing request for one knowledge collection.

#### Declaration
```csharp
public sealed class KnowledgeIndexingRequest
```

#### Constructors

<a id="member-m-cephalon-retrieval-services-knowledgeindexingrequest-ctor-system-string-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `KnowledgeIndexingRequest`

```csharp
KnowledgeIndexingRequest(string collectionId, string runId, string actorId, string correlationId, DateTimeOffset? requestedAtUtc, IReadOnlyDictionary<string, string> metadata)
```

Creates a managed indexing request.

Parameters:
- `collectionId`: The collection identifier to index.
- `runId`: The stable run identifier for this indexing attempt.
- `actorId`: The optional actor that requested indexing.
- `correlationId`: The optional correlation identifier for this indexing attempt.
- `requestedAtUtc`: The optional UTC timestamp when indexing was requested.
- `metadata`: Optional operator-facing metadata attached to the indexing request.

#### Properties

<a id="member-p-cephalon-retrieval-services-knowledgeindexingrequest-actorid"></a>

##### `ActorId`

```csharp
string ActorId { get; }
```

Gets the optional actor that requested indexing.

<a id="member-p-cephalon-retrieval-services-knowledgeindexingrequest-collectionid"></a>

##### `CollectionId`

```csharp
string CollectionId { get; }
```

Gets the collection identifier to index.

<a id="member-p-cephalon-retrieval-services-knowledgeindexingrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for this indexing attempt.

<a id="member-p-cephalon-retrieval-services-knowledgeindexingrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional operator-facing metadata attached to the indexing request.

<a id="member-p-cephalon-retrieval-services-knowledgeindexingrequest-requestedatutc"></a>

##### `RequestedAtUtc`

```csharp
DateTimeOffset RequestedAtUtc { get; }
```

Gets the UTC timestamp when indexing was requested.

<a id="member-p-cephalon-retrieval-services-knowledgeindexingrequest-runid"></a>

##### `RunId`

```csharp
string RunId { get; }
```

Gets the stable run identifier for this indexing attempt.

<a id="type-cephalon-retrieval-services-knowledgeindexingresult"></a>

### `KnowledgeIndexingResult`

Describes the result of a managed indexing attempt.

#### Declaration
```csharp
public sealed class KnowledgeIndexingResult
```

#### Constructors

<a id="member-m-cephalon-retrieval-services-knowledgeindexingresult-ctor-system-string-system-string-system-string-system-datetimeoffset-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-int32-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `KnowledgeIndexingResult`

```csharp
KnowledgeIndexingResult(string CollectionId, string RunId, string Outcome, DateTimeOffset ObservedAtUtc, DateTimeOffset? IndexedAtUtc, DateTimeOffset? SourceFreshnessUtc, int DocumentCount, string Error, IReadOnlyDictionary<string, string> Metadata)
```

Describes the result of a managed indexing attempt.

Parameters:
- `CollectionId`: The collection identifier that was indexed.
- `RunId`: The stable indexing run identifier.
- `Outcome`: The stable indexing outcome identifier.
- `ObservedAtUtc`: The UTC timestamp when the outcome was observed.
- `IndexedAtUtc`: The UTC timestamp when the replacement index was published.
- `SourceFreshnessUtc`: The newest source-document timestamp observed during indexing.
- `DocumentCount`: The number of documents stored in the replacement index.
- `Error`: The operator-facing error summary when indexing failed.
- `Metadata`: Optional operator-facing metadata captured with the result.

#### Properties

<a id="member-p-cephalon-retrieval-services-knowledgeindexingresult-collectionid"></a>

##### `CollectionId`

```csharp
string CollectionId { get; set; }
```

The collection identifier that was indexed.

<a id="member-p-cephalon-retrieval-services-knowledgeindexingresult-documentcount"></a>

##### `DocumentCount`

```csharp
int DocumentCount { get; set; }
```

The number of documents stored in the replacement index.

<a id="member-p-cephalon-retrieval-services-knowledgeindexingresult-error"></a>

##### `Error`

```csharp
string Error { get; set; }
```

The operator-facing error summary when indexing failed.

<a id="member-p-cephalon-retrieval-services-knowledgeindexingresult-indexedatutc"></a>

##### `IndexedAtUtc`

```csharp
DateTimeOffset? IndexedAtUtc { get; set; }
```

The UTC timestamp when the replacement index was published.

<a id="member-p-cephalon-retrieval-services-knowledgeindexingresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; set; }
```

Optional operator-facing metadata captured with the result.

<a id="member-p-cephalon-retrieval-services-knowledgeindexingresult-observedatutc"></a>

##### `ObservedAtUtc`

```csharp
DateTimeOffset ObservedAtUtc { get; set; }
```

The UTC timestamp when the outcome was observed.

<a id="member-p-cephalon-retrieval-services-knowledgeindexingresult-outcome"></a>

##### `Outcome`

```csharp
string Outcome { get; set; }
```

The stable indexing outcome identifier.

<a id="member-p-cephalon-retrieval-services-knowledgeindexingresult-runid"></a>

##### `RunId`

```csharp
string RunId { get; set; }
```

The stable indexing run identifier.

<a id="member-p-cephalon-retrieval-services-knowledgeindexingresult-sourcefreshnessutc"></a>

##### `SourceFreshnessUtc`

```csharp
DateTimeOffset? SourceFreshnessUtc { get; set; }
```

The newest source-document timestamp observed during indexing.

<a id="type-cephalon-retrieval-services-knowledgeindexstate"></a>

### `KnowledgeIndexState`

Describes the latest managed index and query execution state for one knowledge collection.

#### Declaration
```csharp
public sealed class KnowledgeIndexState
```

#### Constructors

<a id="member-m-cephalon-retrieval-services-knowledgeindexstate-ctor-system-string-system-string-system-string-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-nullable-system-datetimeoffset-system-int32-system-string-system-int32-system-int32-system-int32-system-int32-system-int32-system-nullable-system-datetimeoffset-system-string-system-int32-system-int32-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `KnowledgeIndexState`

```csharp
KnowledgeIndexState(string CollectionId, string LastRunId, string LastOutcome, DateTimeOffset? LastObservedAtUtc, DateTimeOffset? LastIndexedAtUtc, DateTimeOffset? SourceFreshnessUtc, int DocumentCount, string FreshnessState, int StartedCount, int SucceededCount, int FailedCount, int SkippedCount, int QueryCount, DateTimeOffset? LastQueriedAtUtc, string LastQueryFingerprint, int LastQueryLength, int LastQueryMatchedCount, string LastActorId, string LastCorrelationId, string LastError, IReadOnlyDictionary<string, string> Metadata)
```

Describes the latest managed index and query execution state for one knowledge collection.

Parameters:
- `CollectionId`: The collection identifier represented by this state.
- `LastRunId`: The latest indexing run identifier when one has been observed.
- `LastOutcome`: The latest indexing outcome when one has been observed.
- `LastObservedAtUtc`: The UTC timestamp when the latest indexing observation was recorded.
- `LastIndexedAtUtc`: The UTC timestamp when the latest replacement index was published.
- `SourceFreshnessUtc`: The newest source-document timestamp observed during the latest successful indexing run.
- `DocumentCount`: The number of documents currently stored in the managed index.
- `FreshnessState`: The operator-facing freshness state captured for the latest indexing observation.
- `StartedCount`: The number of indexing runs that have started.
- `SucceededCount`: The number of indexing runs that have completed successfully.
- `FailedCount`: The number of indexing runs that have failed.
- `SkippedCount`: The number of indexing runs that have been skipped.
- `QueryCount`: The number of managed queries executed against this collection.
- `LastQueriedAtUtc`: The UTC timestamp when the latest query was executed.
- `LastQueryFingerprint`: A non-reversible SHA-256 fingerprint of the latest query text.
- `LastQueryLength`: The character length of the latest query text.
- `LastQueryMatchedCount`: The number of matches returned by the latest query.
- `LastActorId`: The latest actor identifier when one was supplied by an indexing request.
- `LastCorrelationId`: The latest correlation identifier when one was supplied by an indexing request.
- `LastError`: The latest operator-facing error summary when indexing failed.
- `Metadata`: Optional operator-facing metadata captured with the latest indexing observation.

#### Properties

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-collectionid"></a>

##### `CollectionId`

```csharp
string CollectionId { get; set; }
```

The collection identifier represented by this state.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-documentcount"></a>

##### `DocumentCount`

```csharp
int DocumentCount { get; set; }
```

The number of documents currently stored in the managed index.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-failedcount"></a>

##### `FailedCount`

```csharp
int FailedCount { get; set; }
```

The number of indexing runs that have failed.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-freshnessstate"></a>

##### `FreshnessState`

```csharp
string FreshnessState { get; set; }
```

The operator-facing freshness state captured for the latest indexing observation.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-lastactorid"></a>

##### `LastActorId`

```csharp
string LastActorId { get; set; }
```

The latest actor identifier when one was supplied by an indexing request.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-lastcorrelationid"></a>

##### `LastCorrelationId`

```csharp
string LastCorrelationId { get; set; }
```

The latest correlation identifier when one was supplied by an indexing request.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-lasterror"></a>

##### `LastError`

```csharp
string LastError { get; set; }
```

The latest operator-facing error summary when indexing failed.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-lastindexedatutc"></a>

##### `LastIndexedAtUtc`

```csharp
DateTimeOffset? LastIndexedAtUtc { get; set; }
```

The UTC timestamp when the latest replacement index was published.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-lastobservedatutc"></a>

##### `LastObservedAtUtc`

```csharp
DateTimeOffset? LastObservedAtUtc { get; set; }
```

The UTC timestamp when the latest indexing observation was recorded.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-lastoutcome"></a>

##### `LastOutcome`

```csharp
string LastOutcome { get; set; }
```

The latest indexing outcome when one has been observed.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-lastqueriedatutc"></a>

##### `LastQueriedAtUtc`

```csharp
DateTimeOffset? LastQueriedAtUtc { get; set; }
```

The UTC timestamp when the latest query was executed.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-lastqueryfingerprint"></a>

##### `LastQueryFingerprint`

```csharp
string LastQueryFingerprint { get; set; }
```

A non-reversible SHA-256 fingerprint of the latest query text.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-lastquerylength"></a>

##### `LastQueryLength`

```csharp
int LastQueryLength { get; set; }
```

The character length of the latest query text.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-lastquerymatchedcount"></a>

##### `LastQueryMatchedCount`

```csharp
int LastQueryMatchedCount { get; set; }
```

The number of matches returned by the latest query.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-lastrunid"></a>

##### `LastRunId`

```csharp
string LastRunId { get; set; }
```

The latest indexing run identifier when one has been observed.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; set; }
```

Optional operator-facing metadata captured with the latest indexing observation.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-querycount"></a>

##### `QueryCount`

```csharp
int QueryCount { get; set; }
```

The number of managed queries executed against this collection.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-skippedcount"></a>

##### `SkippedCount`

```csharp
int SkippedCount { get; set; }
```

The number of indexing runs that have been skipped.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-sourcefreshnessutc"></a>

##### `SourceFreshnessUtc`

```csharp
DateTimeOffset? SourceFreshnessUtc { get; set; }
```

The newest source-document timestamp observed during the latest successful indexing run.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-startedcount"></a>

##### `StartedCount`

```csharp
int StartedCount { get; set; }
```

The number of indexing runs that have started.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-succeededcount"></a>

##### `SucceededCount`

```csharp
int SucceededCount { get; set; }
```

The number of indexing runs that have completed successfully.

<a id="member-p-cephalon-retrieval-services-knowledgeindexstate-totalindexruns"></a>

##### `TotalIndexRuns`

```csharp
int TotalIndexRuns { get; }
```

Gets the total number of indexing observations recorded for this collection.

<a id="type-cephalon-retrieval-services-knowledgequerymatch"></a>

### `KnowledgeQueryMatch`

Describes one ranked document match returned by the managed query engine.

#### Declaration
```csharp
public sealed class KnowledgeQueryMatch
```

#### Constructors

<a id="member-m-cephalon-retrieval-services-knowledgequerymatch-ctor-system-string-system-string-system-string-system-string-system-int32-system-uri-system-collections-generic-ireadonlylist-system-string-system-nullable-system-datetimeoffset-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `KnowledgeQueryMatch`

```csharp
KnowledgeQueryMatch(string CollectionId, string DocumentId, string Title, string ContentSnippet, int Score, Uri Uri, IReadOnlyList<string> Tags, DateTimeOffset? LastModifiedAtUtc, IReadOnlyDictionary<string, string> Metadata)
```

Describes one ranked document match returned by the managed query engine.

Parameters:
- `CollectionId`: The collection identifier that produced the match.
- `DocumentId`: The matched document identifier.
- `Title`: The matched document title.
- `ContentSnippet`: A short content snippet around the matched text.
- `Score`: The lexical relevance score assigned by the managed query engine.
- `Uri`: The optional source document URI.
- `Tags`: The normalized tags attached to the matched document.
- `LastModifiedAtUtc`: The UTC timestamp when the matched source document was last modified.
- `Metadata`: The operator-facing metadata attached to the matched document.

#### Properties

<a id="member-p-cephalon-retrieval-services-knowledgequerymatch-collectionid"></a>

##### `CollectionId`

```csharp
string CollectionId { get; set; }
```

The collection identifier that produced the match.

<a id="member-p-cephalon-retrieval-services-knowledgequerymatch-contentsnippet"></a>

##### `ContentSnippet`

```csharp
string ContentSnippet { get; set; }
```

A short content snippet around the matched text.

<a id="member-p-cephalon-retrieval-services-knowledgequerymatch-documentid"></a>

##### `DocumentId`

```csharp
string DocumentId { get; set; }
```

The matched document identifier.

<a id="member-p-cephalon-retrieval-services-knowledgequerymatch-lastmodifiedatutc"></a>

##### `LastModifiedAtUtc`

```csharp
DateTimeOffset? LastModifiedAtUtc { get; set; }
```

The UTC timestamp when the matched source document was last modified.

<a id="member-p-cephalon-retrieval-services-knowledgequerymatch-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; set; }
```

The operator-facing metadata attached to the matched document.

<a id="member-p-cephalon-retrieval-services-knowledgequerymatch-score"></a>

##### `Score`

```csharp
int Score { get; set; }
```

The lexical relevance score assigned by the managed query engine.

<a id="member-p-cephalon-retrieval-services-knowledgequerymatch-tags"></a>

##### `Tags`

```csharp
IReadOnlyList<string> Tags { get; set; }
```

The normalized tags attached to the matched document.

<a id="member-p-cephalon-retrieval-services-knowledgequerymatch-title"></a>

##### `Title`

```csharp
string Title { get; set; }
```

The matched document title.

<a id="member-p-cephalon-retrieval-services-knowledgequerymatch-uri"></a>

##### `Uri`

```csharp
Uri Uri { get; set; }
```

The optional source document URI.

<a id="type-cephalon-retrieval-services-knowledgequeryrequest"></a>

### `KnowledgeQueryRequest`

Describes a managed retrieval query request for one knowledge collection.

#### Declaration
```csharp
public sealed class KnowledgeQueryRequest
```

#### Constructors

<a id="member-m-cephalon-retrieval-services-knowledgequeryrequest-ctor-system-string-system-string-system-nullable-system-int32-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `KnowledgeQueryRequest`

```csharp
KnowledgeQueryRequest(string collectionId, string queryText, int? maxResults, string actorId, string correlationId, IReadOnlyDictionary<string, string> metadata)
```

Creates a managed retrieval query request.

Parameters:
- `collectionId`: The collection identifier to query.
- `queryText`: The text to search for.
- `maxResults`: The optional maximum number of matches to return. When omitted, the runtime default is used.
- `actorId`: The optional actor that requested the query.
- `correlationId`: The optional correlation identifier for the query.
- `metadata`: Optional request metadata used by provider-specific query engines.

#### Properties

<a id="member-p-cephalon-retrieval-services-knowledgequeryrequest-actorid"></a>

##### `ActorId`

```csharp
string ActorId { get; }
```

Gets the optional actor that requested the query.

<a id="member-p-cephalon-retrieval-services-knowledgequeryrequest-collectionid"></a>

##### `CollectionId`

```csharp
string CollectionId { get; }
```

Gets the collection identifier to query.

<a id="member-p-cephalon-retrieval-services-knowledgequeryrequest-correlationid"></a>

##### `CorrelationId`

```csharp
string CorrelationId { get; }
```

Gets the optional correlation identifier for the query.

<a id="member-p-cephalon-retrieval-services-knowledgequeryrequest-maxresults"></a>

##### `MaxResults`

```csharp
int? MaxResults { get; }
```

Gets the optional maximum number of matches to return.

<a id="member-p-cephalon-retrieval-services-knowledgequeryrequest-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets optional request metadata used by provider-specific query engines.

<a id="member-p-cephalon-retrieval-services-knowledgequeryrequest-querytext"></a>

##### `QueryText`

```csharp
string QueryText { get; }
```

Gets the text to search for.

<a id="type-cephalon-retrieval-services-knowledgequeryresult"></a>

### `KnowledgeQueryResult`

Describes the result of a managed retrieval query.

#### Declaration
```csharp
public sealed class KnowledgeQueryResult
```

#### Constructors

<a id="member-m-cephalon-retrieval-services-knowledgequeryresult-ctor-system-string-system-string-system-datetimeoffset-system-collections-generic-ireadonlylist-cephalon-retrieval-services-knowledgequerymatch-system-int32-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `KnowledgeQueryResult`

```csharp
KnowledgeQueryResult(string CollectionId, string QueryText, DateTimeOffset QueriedAtUtc, IReadOnlyList<KnowledgeQueryMatch> Matches, int TotalIndexedDocuments, IReadOnlyDictionary<string, string> Metadata)
```

Describes the result of a managed retrieval query.

Parameters:
- `CollectionId`: The collection identifier that was queried.
- `QueryText`: The query text supplied by the caller.
- `QueriedAtUtc`: The UTC timestamp when the query executed.
- `Matches`: The ranked matches returned by the managed query engine.
- `TotalIndexedDocuments`: The number of indexed documents available at query time.
- `Metadata`: Optional operator-facing metadata captured with the query result.

#### Properties

<a id="member-p-cephalon-retrieval-services-knowledgequeryresult-collectionid"></a>

##### `CollectionId`

```csharp
string CollectionId { get; set; }
```

The collection identifier that was queried.

<a id="member-p-cephalon-retrieval-services-knowledgequeryresult-hasmatches"></a>

##### `HasMatches`

```csharp
bool HasMatches { get; }
```

Gets a value indicating whether the query returned at least one match.

<a id="member-p-cephalon-retrieval-services-knowledgequeryresult-matches"></a>

##### `Matches`

```csharp
IReadOnlyList<KnowledgeQueryMatch> Matches { get; set; }
```

The ranked matches returned by the managed query engine.

<a id="member-p-cephalon-retrieval-services-knowledgequeryresult-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; set; }
```

Optional operator-facing metadata captured with the query result.

<a id="member-p-cephalon-retrieval-services-knowledgequeryresult-queriedatutc"></a>

##### `QueriedAtUtc`

```csharp
DateTimeOffset QueriedAtUtc { get; set; }
```

The UTC timestamp when the query executed.

<a id="member-p-cephalon-retrieval-services-knowledgequeryresult-querytext"></a>

##### `QueryText`

```csharp
string QueryText { get; set; }
```

The query text supplied by the caller.

<a id="member-p-cephalon-retrieval-services-knowledgequeryresult-totalindexeddocuments"></a>

##### `TotalIndexedDocuments`

```csharp
int TotalIndexedDocuments { get; set; }
```

The number of indexed documents available at query time.
