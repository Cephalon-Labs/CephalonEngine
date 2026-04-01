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

This pack lets Cephalon model edge topology and deployment concerns through the same technology selection and introspection flow used by the other future-tech companions.

## Related docs

- [Technology packs](../technology-packs.md)
- [Module authoring](../module-authoring.md)
