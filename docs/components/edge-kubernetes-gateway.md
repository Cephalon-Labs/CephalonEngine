# Cephalon.Edge.KubernetesGateway

> **Maturity:** `M3` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Edge.KubernetesGateway` is the first provider-specific control-plane materializer pack for Cephalon cell traffic automation. It proves that a real gateway/control-plane family can stay outside `Cephalon.Engine` while still publishing truthful materialization answers on the shared runtime surfaces. The pack now supports configured intent, opt-in live observation, opt-in apply-and-reconcile, opt-in cleanup sweeps, and typed provider materialization conditions on that same shared runtime story.

## What it owns

- `KubernetesGatewayTrafficMaterializerOptions`, `KubernetesGatewayTrafficRouteOptions`, and `KubernetesGatewayTrafficObservationOptions` for declarative Kubernetes Gateway API projection plus opt-in live observation, apply-and-reconcile, or cleanup sweeps
- the `AddKubernetesGatewayTrafficMaterializer(...)` registration entry point for attaching the pack to an `EngineBuilder`
- a provider-specific `ICellTrafficAutomationProviderMaterializer` implementation for `providerId = "kubernetes-gateway"`
- deterministic projection of cell routes into `GatewayClass`, `Gateway`, `HTTPRoute`, `parentRefs`, and `backendRefs` intent metadata
- recurring live `Gateway` plus `HTTPRoute` observation in `observe-only` or `apply-and-reconcile` mode, including freshness, drift, and cleanup metadata
- controlled `HTTPRoute` apply semantics in `apply-and-reconcile` mode, including ownership labels and annotations, write-result metadata, and post-apply status reconciliation
- namespace-scoped cleanup sweeps in `apply-and-reconcile` mode that can delete transferred `HTTPRoute` resources or prune orphaned Cephalon-owned routes while truthfully reporting a `primary-only` cleanup strategy instead of inventing a second lifecycle registry
- the `kubernetes-gateway-traffic-materializations` technology surface under `cell-based-architecture`
- truthful operator metadata such as `providerRouteId`, `gatewayNamespace`, `gatewayName`, `controllerName`, `statusSource`, projected-or-observed Gateway API conditions, shared ownership/dependency/drift posture, lifecycle action, freshness windows, HTTPRoute write results, typed provider `MaterializationConditions`, and additive cleanup-sweep summaries such as `cleanupState`, `cleanupObservedAtUtc`, `cleanup.cleanupStrategy`, `cleanup.primaryCandidateCount`, `cleanup.dependencyCandidateCount`, `cleanup.lifecycleActions`, `providerMaterialization.conditionCount`, and `providerMaterialization.highestConditionSeverity`

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
`providerId = "kubernetes-gateway"` project into, observe, or reconcile Kubernetes Gateway API
control-plane truth?

The pack now has three truthful operating modes:

- default `configured-intent`, which reports `providerAction = projected-intent`, `observationMode = configured-intent`, `statusSource = configured-intent`, `ownershipState = requested`, `dependencyState = unknown`, `driftState = unknown`, and `lifecycleAction = project` while publishing deterministic `Gateway` plus `HTTPRoute` intent without claiming live cluster state or a successful apply; the shared provider materialization state stays `pending`
- opt-in `observe-only`, which reports `providerAction = observe-only`, `statusSource = gateway-api-status`, and the same shared ownership/dependency/drift vocabulary while reading live `Gateway` plus `HTTPRoute` status back into the same shared runtime catalog without claiming that Cephalon applied resources itself
- opt-in `apply-and-reconcile`, which reports `providerAction = apply-and-reconcile`, writes only owned `HTTPRoute` resources, keeps `Gateway` as a pre-provisioned dependency, and then merges observed `Gateway` plus `HTTPRoute` status back into the same shared runtime catalog while preserving write posture through `httpRouteWriteAction`
  and `lifecycleAction = create|replace|transfer`
- optional cleanup sweeps inside `apply-and-reconcile`, which report additive `cleanup*` metadata after namespace-scoped delete or prune passes while leaving the primary provider lifecycle answer grounded in the selected route's actual materialization state and truthfully publishing `cleanupStrategy = primary-only` with zero dependency-cleanup counts

What this proves is that one provider-specific pack can publish deterministic projected intent,
selected materializer ownership, live provider status, drift, freshness answers, a typed
condition taxonomy, and now a narrow owned-resource apply loop back onto the same shared cell
runtime story without inventing a second traffic registry or pushing Kubernetes assumptions into
the engine core.

When the pack owns an automation answer, operators can inspect the same route through:

- `/engine/cell-traffic-automations`
- `/engine/cell-traffic-automations/providers/kubernetes-gateway`
- `/engine/technology-surfaces/cell-based-architecture`
- `/engine/snapshot`

The technology surface entry lives under `surfaceId = "kubernetes-gateway-traffic-materializations"`
and carries one provider-facing projection per selected route, including the projected
`providerRouteId`, parent reference, backend reference, hostname list, controller identity, and
Gateway resource identity. In live modes the same entries also surface condition, drift,
freshness, ownership, dependency, lifecycle-action, write metadata, and additive cleanup-sweep
metadata such as `gatewayAcceptedCondition`, `httpRouteResolvedRefsCondition`, `driftState`,
`observationFreshUntilUtc`, `ownershipState`, `dependencyState`, `lifecycleAction`,
`httpRouteWriteAction`, `httpRouteAppliedGeneration`, `cleanupState`, `cleanup.cleanupStrategy`,
`cleanup.primaryCandidateCount`, `cleanup.dependencyCandidateCount`, and
`cleanup.lifecycleActions`. The shared automation answer for the same route now also carries
typed provider conditions through `CellTrafficAutomationRuntimeDescriptor.MaterializationConditions`
plus additive summaries such as `materialization.conditionCount`,
`materialization.highestConditionSeverity`, `providerMaterialization.conditionCategories`, and
`providerMaterialization.conditionBreakdown`.

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

Opt into apply-and-reconcile when the pack should manage owned `HTTPRoute` resources and then read
the resulting Gateway API status back into the shared runtime surfaces:

```csharp
engine.AddKubernetesGatewayTrafficMaterializer(options =>
{
    options.Observation.Mode = KubernetesGatewayTrafficObservationModes.ApplyAndReconcile;
    options.Observation.UseInClusterConfiguration = true;
    options.Observation.PollingIntervalSeconds = 30;
    options.Observation.StaleAfterSeconds = 90;
});
```

Enable cleanup sweeps when the same apply-and-reconcile loop should also delete transferred routes
or prune orphaned Cephalon-owned routes that no longer map to an active automation:

```csharp
engine.AddKubernetesGatewayTrafficMaterializer(options =>
{
    options.Observation.Mode = KubernetesGatewayTrafficObservationModes.ApplyAndReconcile;
    options.Observation.UseInClusterConfiguration = true;
    options.Observation.EnableCleanupSweep = true;
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

## Ownership and apply model

`apply-and-reconcile` is intentionally narrow:

- the pack writes only `HTTPRoute` resources
- `Gateway` remains a pre-provisioned dependency and is never created or updated by Cephalon
- the pack only replaces an existing `HTTPRoute` when ownership matches the current automation or when stale or incomplete Cephalon ownership metadata marks the route as an orphaned transfer candidate
- existing unmanaged resources and active foreign Cephalon owners fail with an explicit ownership-conflict posture instead of being hijacked silently
- owned routes carry `cephalon.io/managed-by = edge-kubernetes-gateway` plus route and automation annotations so later observation can verify ownership truthfully
- merged live observation keeps the last write lifecycle action (`create`, `replace`, or `transfer`) visible on the same shared runtime surfaces instead of collapsing every successful reconciliation back to `observe`
- optional cleanup sweeps run only when `EnableCleanupSweep` is true in `apply-and-reconcile` mode, scan the configured route namespaces, delete stale transferred `HTTPRoute` resources with `lifecycleAction = delete`, and prune orphaned Cephalon-owned routes with `lifecycleAction = prune`
- cleanup sweep summaries stay additive through `providerMaterialization.cleanup*` and the provider-specific technology surface so operators can inspect delete/prune posture without losing the selected route's primary materialization answer; those summaries now publish `cleanupStrategy = primary-only` plus primary-resource counts while dependency counts stay `0`

This pack intentionally does not yet claim:

- multi-route low-code generation beyond the explicitly configured route projections
- control-plane ownership outside `provider-managed` or `provider-and-edge-managed` routes
- controller-driven policy or dependency semantics beyond the shipped typed readiness, ownership,
  dependency, lifecycle, and observation condition taxonomy
- broader dependency-aware teardown beyond the current primary-only owned `HTTPRoute` sweep baseline; `Gateway` and backend dependencies remain observe-only

Those remain later follow-through so the current provider claim stays honest.

## Related docs

- [Cephalon.Edge](edge.md)
- [Cephalon.Engine](engine.md)
- [Technology packs](../technology-packs.md)
