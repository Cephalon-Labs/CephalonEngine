# Cephalon.Retrieval

`Cephalon.Retrieval` is the baseline technology pack for knowledge retrieval workloads.

## What it owns

- retrieval options
- module and registration entry points for the `KnowledgeRetrieval` technology
- knowledge collection descriptors, registries, and catalogs
- runtime-surface contribution for introspection

## Main surfaces

- `Configuration/RetrievalOptions.cs`
- `Modules/RetrievalModule.cs`
- `Registration/RetrievalEngineBuilderExtensions.cs`
- `Services/KnowledgeCollectionDescriptor.cs`
- `Services/KnowledgeCollectionRegistry.cs`
- `Services/KnowledgeCatalog.cs`
- `Services/IKnowledgeCollectionContributor.cs`
- `Services/IKnowledgeCatalog.cs`
- `Services/RetrievalRuntimeSurfaceContributor.cs`

## Source structure

- `Configuration`
- `Modules`
- `Registration`
- `Services`

## How it fits

This pack provides the retrieval-side companion to `Cephalon.Agentics`. It models knowledge collections as a first-class selectable technology surface instead of embedding retrieval assumptions into the engine.

## Related docs

- [Technology packs](../technology-packs.md)
- [Module authoring](../module-authoring.md)
