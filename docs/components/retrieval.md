# Cephalon.Retrieval

`Cephalon.Retrieval` is the baseline technology pack for knowledge retrieval workloads.

## What it owns

- retrieval options
- module and registration entry points for the `KnowledgeRetrieval` technology
- knowledge collection descriptors, registries, and catalogs
- knowledge document provider contracts for module-owned source material
- one Cephalon-managed lexical indexing and query execution baseline
- index state, freshness, query counters, and runtime-surface contribution for introspection

## Main surfaces

- `Configuration/RetrievalOptions.cs`
- `Modules/RetrievalModule.cs`
- `Registration/RetrievalEngineBuilderExtensions.cs`
- `Services/KnowledgeCollectionDescriptor.cs`
- `Services/KnowledgeCollectionRegistry.cs`
- `Services/KnowledgeCatalog.cs`
- `Services/IKnowledgeCollectionContributor.cs`
- `Services/IKnowledgeCatalog.cs`
- `Services/IKnowledgeDocumentProvider.cs`
- `Services/IKnowledgeIndexer.cs`
- `Services/IKnowledgeQueryEngine.cs`
- `Services/IKnowledgeIndexCatalog.cs`
- `Services/KnowledgeDocument.cs`
- `Services/KnowledgeIndexingRequest.cs`
- `Services/KnowledgeIndexingResult.cs`
- `Services/KnowledgeQueryRequest.cs`
- `Services/KnowledgeQueryResult.cs`
- `Services/KnowledgeIndexState.cs`
- `Services/RetrievalRuntimeSurfaceContributor.cs`

## Source structure

- `Configuration`
- `Modules`
- `Registration`
- `Services`

## How it fits

This pack provides the retrieval-side companion to `Cephalon.Agentics`. It models knowledge collections as a first-class selectable technology surface instead of embedding retrieval assumptions into the engine.

When the `KnowledgeRetrieval` technology is selected, the pack can now index documents from registered `IKnowledgeDocumentProvider` services into an in-process lexical index and execute bounded lexical queries through `IKnowledgeQueryEngine`. Runtime surfaces report `indexingOwnership`, `queryOwnership`, provider readiness, latest indexing outcome, document count, query count, and freshness state so operators can distinguish an indexed collection from one that is still `awaiting-provider` or `awaiting-index`.

This is intentionally a narrow managed proof. It is useful for low-ceremony documentation, runbook, and module-owned knowledge search, but it is not yet a vector database, distributed index, durable search cluster, embedding pipeline, reranker, or provider-specific semantic search adapter. Those should land as companion packs or later retrieval slices when they own those paths explicitly.

## Related docs

- [Technology packs](../technology-packs.md)
- [Module authoring](../module-authoring.md)
