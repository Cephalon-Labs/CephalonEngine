# Cephalon.Retrieval

`Cephalon.Retrieval` is the baseline technology pack for knowledge retrieval workloads.

## What it owns

- retrieval options
- module and registration entry points for the `KnowledgeRetrieval` technology
- knowledge collection descriptors, registries, and catalogs
- knowledge document provider contracts for module-owned source material
- one Cephalon-managed lexical indexing and query execution baseline
- index state implementation, freshness calculation, manual reindex execution behind the abstraction-level `IKnowledgeIndexer`, bounded query execution behind the abstraction-level `IKnowledgeQueryEngine`, opt-in background reindex scheduling, query counters, and runtime-surface contribution for introspection

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
- `Services/KnowledgeDocument.cs`
- `Services/KnowledgeBackgroundReindexHostedService.cs`
- `Services/KnowledgeQueryEngine.cs`
- `Services/RetrievalRuntimeSurfaceContributor.cs`

## Source structure

- `Configuration`
- `Modules`
- `Registration`
- `Services`

## How it fits

This pack provides the retrieval-side companion to `Cephalon.Agentics`. It models knowledge collections as a first-class selectable technology surface instead of embedding retrieval assumptions into the engine.

When the `KnowledgeRetrieval` technology is selected, the pack can now index documents from registered `IKnowledgeDocumentProvider` services into an in-process lexical index, manually reindex a collection through the abstraction-level `IKnowledgeIndexer`, opt into an in-process background reindex scheduler, and execute bounded lexical queries through the abstraction-level `IKnowledgeQueryEngine`. Runtime surfaces report `indexingOwnership`, `queryOwnership`, `backgroundReindexingOwnership`, `backgroundReindexingScheduled`, provider readiness, latest indexing outcome, document count, query count, freshness state, and background scheduler scope/timing metadata so operators can distinguish an indexed collection from one that is still `awaiting-provider`, `awaiting-index`, or intentionally not scheduled.

The read-side index-state contract, manual reindex command seam, and bounded query command seam now live in
`Cephalon.Abstractions.Retrieval` as `IKnowledgeIndexCatalog`, `KnowledgeIndexState`,
`KnowledgeIndexingOutcomes`, `KnowledgeIndexFreshnessStates`, `IKnowledgeIndexer`,
`KnowledgeIndexingRequest`, `KnowledgeIndexingResult`, `IKnowledgeQueryEngine`,
`KnowledgeQueryRequest`, `KnowledgeQueryResult`, and `KnowledgeQueryMatch`. `Cephalon.Retrieval`
remains the implementation owner for the managed lexical index and query loop, while
`Cephalon.Engine` and host adapters can expose `/engine/knowledge-indexes`,
`/engine/knowledge-indexes/{collectionId}`, `POST /engine/knowledge-indexes/{collectionId}/queries`,
`POST /engine/knowledge-indexes/{collectionId}/reindex`, and `snapshot.KnowledgeIndexes` without
taking a direct dependency on this implementation package.

Background scheduling is opt-in through `RetrievalOptions.EnableBackgroundReindexing`. When enabled with ingestion, `Cephalon.Retrieval` registers a generic-host `IHostedService` that can reindex all registered collections or the configured `BackgroundReindexCollectionIds`, optionally run once on startup, and repeat at `BackgroundReindexIntervalSeconds`. Scheduled runs use the same `IKnowledgeIndexer` path as manual reindexing and record safe scheduler metadata such as trigger, scheduler id, iteration id, collection scope, and interval.

This is intentionally a narrow managed proof. It is useful for low-ceremony documentation, runbook, module-owned knowledge search, host-agnostic bounded query execution, manual operator reindexing, and opt-in in-process freshness scheduling, but it is not yet a vector database, distributed index, durable search cluster, embedding pipeline, reranker, distributed scheduler coordinator, leader-election mechanism, or provider-specific semantic search adapter. Those should land as companion packs or later retrieval slices when they own those paths explicitly.

## Related docs

- [Technology packs](../technology-packs.md)
- [Module authoring](../module-authoring.md)
