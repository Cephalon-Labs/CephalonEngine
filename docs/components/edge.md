# Cephalon.Edge

`Cephalon.Edge` is the baseline technology pack for edge-native delivery scenarios.

## What it owns

- edge runtime options
- module and registration entry points for the `EdgeNativeDelivery` technology
- edge node descriptors, registries, and catalogs
- runtime-surface contribution for introspection

## Main surfaces

- `Configuration/EdgeRuntimeOptions.cs`
- `Modules/EdgeRuntimeModule.cs`
- `Registration/EdgeEngineBuilderExtensions.cs`
- `Services/EdgeNodeDescriptor.cs`
- `Services/EdgeNodeRegistry.cs`
- `Services/EdgeNodeCatalog.cs`
- `Services/IEdgeNodeContributor.cs`
- `Services/IEdgeNodeCatalog.cs`
- `Services/EdgeRuntimeSurfaceContributor.cs`

## Source structure

- `Configuration`
- `Modules`
- `Registration`
- `Services`

## How it fits

This pack lets Cephalon model edge topology and deployment concerns through the same technology
selection and introspection flow used by the other future-tech companions. Phase 13 cell traffic
automation can now target `edgeNodeIds` on the shared `ICellTrafficAutomationRuntimeCatalog`
without taking a direct dependency on `Cephalon.Edge`; operators can correlate those automation
answers with the `edge-nodes` technology surface when `EdgeNativeDelivery` is active.

## Related docs

- [Technology packs](../technology-packs.md)
- [Module authoring](../module-authoring.md)
