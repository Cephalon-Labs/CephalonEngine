# Cephalon.Edge

> **Maturity:** `M2` · **Ownership:** cephalon-managed · **Family:** `edge` · See [audit](../engine-surface-maturity-audit.md), [matrix](../conformance-matrix.md).

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
- `Services/EdgeTrafficAutomationMaterializer.cs`
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
answers with the `edge-nodes` technology surface when `EdgeNativeDelivery` is active. The pack now
also contributes the first concrete `ICellTrafficAutomationEdgeMaterializer` through
`EdgeTrafficAutomationMaterializer`, so `edge-managed` or `provider-and-edge-managed` routes can
reconcile targeted `edgeNodeIds` back into the shared `ICellTrafficAutomationRuntimeCatalog`,
`snapshot.CellTrafficAutomations`, and the `cell-traffic-automations` technology surface while
keeping edge-runtime implementation details inside `Cephalon.Edge`. That materializer now also
publishes an explicit low-priority fallback posture so more specific edge companion packs can
coexist through the shared highest-priority selection model instead of replacing the shared catalog.
The first provider-specific control-plane follow-through now also ships separately through
`Cephalon.Edge.KubernetesGateway`, which uses the same shared provider-materializer seam to project
Kubernetes Gateway API intent for `provider-managed` routes and can now also overlay live
Gateway/HTTPRoute observation in `observe-only` mode plus owned `HTTPRoute` apply-and-reconcile
behavior in `apply-and-reconcile` mode while leaving generic edge-node ownership, edge-runtime
fallback, and `EdgeNativeDelivery` activation inside this baseline pack. A second provider-specific
control-plane sibling now also ships through `Cephalon.Edge.Traefik`, which keeps Traefik
IngressRoute intent on that same shared provider-materializer seam, can now also overlay live
observe-only `IngressRoute` plus dependency posture back onto the shared runtime catalog, and proves
the abstraction is not bound to Kubernetes Gateway API alone. The shared automation story now also uses one stable
lifecycle vocabulary across generic edge-runtime and provider-specific control-plane packs:
`ownershipState`, `dependencyState`, `driftState`, and `lifecycleAction` now stay on the same
`/engine/cell-traffic-automations*`, `snapshot.CellTrafficAutomations`, and provider-specific
technology surfaces instead of forcing each pack to invent its own lifecycle taxonomy.

## Related docs

- [Cephalon.Edge.KubernetesGateway](edge-kubernetes-gateway.md)
- [Cephalon.Edge.Traefik](edge-traefik.md)
- [Technology packs](../technology-packs.md)
- [Module authoring](../module-authoring.md)
