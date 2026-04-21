# Cephalon.Edge.KubernetesGateway

`Cephalon.Edge.KubernetesGateway` is the first provider-specific control-plane materializer pack for Cephalon cell traffic automation. It proves that a real gateway/control-plane family can stay outside `Cephalon.Engine` while still publishing truthful materialization answers on the shared runtime surfaces. The pack now supports both projected-intent and opt-in live observation modes on that same shared runtime story.

## What it owns

- `KubernetesGatewayTrafficMaterializerOptions`, `KubernetesGatewayTrafficRouteOptions`, and `KubernetesGatewayTrafficObservationOptions` for declarative Kubernetes Gateway API projection plus opt-in live observation
- the `AddKubernetesGatewayTrafficMaterializer(...)` registration entry point for attaching the pack to an `EngineBuilder`
- a provider-specific `ICellTrafficAutomationProviderMaterializer` implementation for `providerId = "kubernetes-gateway"`
- deterministic projection of cell routes into `GatewayClass`, `Gateway`, `HTTPRoute`, `parentRefs`, and `backendRefs` intent metadata
- recurring live `Gateway` plus `HTTPRoute` observation in `observe-only` mode, including freshness and drift metadata
- the `kubernetes-gateway-traffic-materializations` technology surface under `cell-based-architecture`
- truthful operator metadata such as `providerRouteId`, `gatewayNamespace`, `gatewayName`, `controllerName`, `statusSource`, projected-or-observed Gateway API conditions, drift posture, and freshness windows

## Main surfaces

- `Configuration/KubernetesGatewayTrafficMaterializerOptions.cs`
- `Configuration/KubernetesGatewayTrafficObservationModes.cs`
- `Configuration/KubernetesGatewayTrafficObservationOptions.cs`
- `Configuration/KubernetesGatewayTrafficRouteOptions.cs`
- `Modules/KubernetesGatewayTrafficMaterializerModule.cs`
- `Registration/KubernetesGatewayEngineBuilderExtensions.cs`
- `Services/KubernetesGatewayTrafficAutomationMaterializer.cs`
- `Services/KubernetesGatewayTrafficObservationHostedService.cs`
- `Services/KubernetesGatewayTrafficObservationSource.cs`
- `Services/KubernetesGatewayTrafficProjectionBuilder.cs`
- `Services/KubernetesGatewayTrafficMaterializationRuntimeContributor.cs`

## How it fits

This pack sits on top of the shared Phase 13 cell-traffic contract instead of replacing it.
`Cephalon.Abstractions` still owns `ICellTrafficAutomationProviderMaterializer`,
`ICellTrafficAutomationMaterializationReportSink`, `CellTrafficAutomationRuntimeDescriptor`, and the shared materialization result/state contracts.
`Cephalon.Engine` still owns route ownership, health-isolation validation, deterministic
materializer selection, startup reconciliation, live materialization merge-back, and the canonical `/engine/cell-traffic-automations*`
plus `snapshot.CellTrafficAutomations` truth. `Cephalon.Edge.KubernetesGateway` only answers one
provider-specific question: how should a `provider-managed` automation targeting
`providerId = "kubernetes-gateway"` project into or observe Kubernetes Gateway API control-plane truth?

The pack now has two truthful operating modes:

- default `configured-intent`, which reports `providerAction = projected-intent` and `statusSource = configured-intent` while publishing deterministic `Gateway` plus `HTTPRoute` intent without claiming live cluster state
- opt-in `observe-only`, which reports `providerAction = observe-only` and `statusSource = gateway-api-status` while reading live `Gateway` plus `HTTPRoute` status back into the same shared runtime catalog without claiming that Cephalon applied resources itself

What this proves is that one provider-specific pack can publish deterministic projected intent,
selected materializer ownership, and then layer live provider status, drift, and freshness answers
back onto the same shared cell runtime story without inventing a second traffic registry or
pushing Kubernetes assumptions into the engine core.

When the pack owns an automation answer, operators can inspect the same route through:

- `/engine/cell-traffic-automations`
- `/engine/cell-traffic-automations/providers/kubernetes-gateway`
- `/engine/technology-surfaces/cell-based-architecture`
- `/engine/snapshot`

The technology surface entry lives under `surfaceId = "kubernetes-gateway-traffic-materializations"`
and carries one provider-facing projection per selected route, including the projected
`providerRouteId`, parent reference, backend reference, hostname list, controller identity, and
Gateway resource identity. In `observe-only` mode the same entries also surface live condition,
drift, and freshness metadata such as `gatewayAcceptedCondition`,
`httpRouteResolvedRefsCondition`, `driftState`, and `observationFreshUntilUtc`.

## Registration

```csharp
engine.AddKubernetesGatewayTrafficMaterializer(options =>
{
    options.ControllerName = "cephalon.io/gateway-controller";
    options.GatewayClassName = "cephalon-public";
    options.GatewayNamespace = "edge-system";
    options.GatewayName = "public-gateway";
    options.ListenerName = "https";
    options.RouteNamespace = "edge-system";

    options.Routes.Add(new KubernetesGatewayTrafficRouteOptions
    {
        RouteId = "orders-to-public-ingress",
        HttpRouteName = "orders-public-ingress",
        BackendNamespace = "orders-runtime",
        BackendServiceName = "orders-api",
        BackendPort = 8443,
        BackendWeight = 100
    });
});
```

Opt into live observation when the pack should read Gateway API status from a cluster instead of
staying at projected intent:

```csharp
engine.AddKubernetesGatewayTrafficMaterializer(options =>
{
    options.Observation.Mode = KubernetesGatewayTrafficObservationModes.ObserveOnly;
    options.Observation.UseInClusterConfiguration = true;
    options.Observation.PollingIntervalSeconds = 30;
    options.Observation.StaleAfterSeconds = 90;
});
```

Use this pack alongside the shared cell baseline:

```csharp
engine.UseConfiguration(configuration);
engine.AddKubernetesGatewayTrafficMaterializer(...);
```

```json
{
  "Engine": {
    "Cells": {
      "TrafficAutomation": {
        "Routes": [
          {
            "RouteId": "orders-to-public-ingress",
            "AutomationMode": "automatic",
            "TriggerMode": "source-or-target-health",
            "ActionMode": "shed-load",
            "MaterializationMode": "provider-managed",
            "ProviderId": "kubernetes-gateway"
          }
        ]
      }
    }
  }
}
```

The route must still exist on the shared `ICellRouteCatalog`, and the engine still decides whether
this pack is the selected provider materializer for that route.

## Not shipped in this slice

This pack intentionally does not yet claim:

- live Kubernetes API writes or apply-and-reconcile loops
- multi-route low-code generation beyond the explicitly configured route projections
- control-plane ownership outside `provider-managed` or `provider-and-edge-managed` routes

Those remain later follow-through so the current provider claim stays honest.

## Related docs

- [Cephalon.Edge](edge.md)
- [Cephalon.Engine](engine.md)
- [Technology packs](../technology-packs.md)
