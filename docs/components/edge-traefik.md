# Cephalon.Edge.Traefik

`Cephalon.Edge.Traefik` is the second provider-specific control-plane materializer pack for Cephalon cell traffic automation. It proves that the shared provider-materializer seam is not overfit to Kubernetes Gateway API by projecting truthful Traefik `IngressRoute` intent and, when enabled, overlaying live Traefik CRD observation, ownership-aware `IngressRoute` apply-and-reconcile posture, typed provider materialization conditions, and additive cleanup sweeps for both primary routes and safe owned dependents back onto the same shared runtime surfaces without moving Traefik-specific assumptions into `Cephalon.Engine`.

## What it owns

- `TraefikTrafficMaterializerOptions`, `TraefikTrafficObservationModes`, `TraefikTrafficObservationOptions`, `TraefikIngressRouteOptions`, and `TraefikMiddlewareReferenceOptions` for declarative Traefik `IngressRoute` projection plus opt-in live observation, apply-and-reconcile execution, or cleanup sweeps
- the `AddTraefikTrafficMaterializer(...)` registration entry point for attaching the pack to an `EngineBuilder`
- a provider-specific `ICellTrafficAutomationProviderMaterializer` implementation for `providerId = "traefik"`
- deterministic projection of selected cell routes into Traefik `IngressRoute` intent, including entry points, match rules, middleware references, backend Service references, and TLS options
- opt-in observe-only polling over Traefik `IngressRoute`, `Middleware`, `TLSOption`, `Secret`, and backend `Service` resources so live ownership, dependency, drift, freshness, and cleanup posture can flow back into the shared runtime catalog
- opt-in `apply-and-reconcile` ownership over Traefik `IngressRoute` resources only, while treating referenced `Middleware`, `TLSOption`, `Secret`, and backend `Service` resources as pre-provisioned dependencies for steady-state writes
- namespace-scoped cleanup sweeps in `apply-and-reconcile` mode that can delete transferred `IngressRoute` resources, prune orphaned Cephalon-owned routes, and remove safe owned `Middleware` or `TLSOption` dependents that no longer map to active projections without inventing a second lifecycle registry
- the `traefik-ingressroute-traffic-materializations` technology surface under `cell-based-architecture`
- truthful operator metadata such as `providerRouteId`, `ingressRouteNamespace`, `ingressRouteName`, `entryPoints`, `matchRule`, `middlewareRefs`, `serviceRefs`, `tlsSecretName`, `tlsOptionsRef`, `statusSource`, `observationMode`, `ingressRouteWriteAction`, freshness metadata, typed provider `MaterializationConditions`, additive cleanup-sweep summaries such as `cleanupState`, `cleanupObservedAtUtc`, `cleanup.cleanupStrategy`, `cleanup.primaryCandidateCount`, `cleanup.dependencyCandidateCount`, the shared ownership/dependency/drift/lifecycle-action vocabulary, and additive condition summaries such as `providerMaterialization.conditionCount` plus `providerMaterialization.highestConditionSeverity`

## Main surfaces

- `Configuration/TraefikTrafficMaterializerOptions.cs`
- `Configuration/TraefikTrafficObservationModes.cs`
- `Configuration/TraefikTrafficObservationOptions.cs`
- `Configuration/TraefikIngressRouteOptions.cs`
- `Configuration/TraefikMiddlewareReferenceOptions.cs`
- `Modules/TraefikTrafficMaterializerModule.cs`
- `Registration/TraefikEngineBuilderExtensions.cs`
- `Services/TraefikTrafficAutomationMaterializer.cs`
- `Services/TraefikTrafficObservationHostedService.cs`
- `Services/TraefikTrafficObservationSource.cs`
- `Services/TraefikTrafficProjectionBuilder.cs`
- `Services/TraefikTrafficMaterializationRuntimeContributor.cs`

## How it fits

This pack sits on top of the shared Phase 13 cell-traffic contract instead of replacing it.
`Cephalon.Abstractions` still owns `ICellTrafficAutomationProviderMaterializer`,
`CellTrafficAutomationRuntimeDescriptor`, and the shared provider-materialization result and state
contracts. `Cephalon.Engine` still owns route ownership, health-isolation validation, deterministic
materializer selection, startup reconciliation, and the canonical `/engine/cell-traffic-automations*`
plus `snapshot.CellTrafficAutomations` truth. `Cephalon.Edge.Traefik` only answers one
provider-specific question: how should a `provider-managed` automation targeting
`providerId = "traefik"` project into Traefik `IngressRoute` intent and, when observation is
enabled, how should the pack read live CRD posture or owned `IngressRoute` write posture back into
that same shared truth?

This pack currently ships three truthful modes:

- default `configured-intent`, which reports `providerAction = projected-intent`,
  `observationMode = configured-intent`, `statusSource = configured-intent`,
  `ownershipState = requested`, `dependencyState = unknown`, `driftState = unknown`, and
  `lifecycleAction = project`, with
  `resourceState = projection-only` while publishing deterministic Traefik `IngressRoute` intent
  without claiming live cluster state or successful control-plane writes; the shared provider
  materialization state stays `pending`
- opt-in `observe-only`, which reports `providerAction = observe-only`,
  `observationMode = observe-only`, `statusSource = traefik-ingressroute-observation`, and
  `lifecycleAction = observe` while polling live Traefik CRDs and dependent Kubernetes resources so
  the shared provider materialization state can move to `applied`, `pending`, or `failed` based on
  observed route existence, dependency readiness, ownership, drift, and freshness instead of
  staying projection-only
- opt-in `apply-and-reconcile`, which reports `providerAction = apply-and-reconcile`,
  `observationMode = apply-and-reconcile`, and `statusSource = control-plane-apply` during owned
  write attempts before merging the resulting `ingressRouteWriteAction` together with live
  `statusSource = traefik-ingressroute-observation` posture; the pack only creates or replaces
  `IngressRoute` resources that Cephalon already owns, is creating from scratch, or can adopt from
  stale or incomplete Cephalon ownership metadata; existing unmanaged routes and active foreign
  owners stay blocked as ownership conflicts, while the merged provider answer keeps the last write
  lifecycle action (`create`, `replace`, or `transfer`) instead of collapsing every successful
  reconciliation back to `observe`
- optional cleanup sweeps inside `apply-and-reconcile`, which publish additive `cleanup*` metadata
  after namespace-scoped delete or prune passes while leaving the primary provider lifecycle answer
  grounded in the selected route's actual materialization state and truthfully publishing
  `cleanupStrategy = primary-and-owned-dependencies` plus primary/dependency cleanup breakdowns

What this proves is that a second provider family can publish selected materializer ownership,
provider-facing route identity, middleware and TLS intent, a typed condition taxonomy, and the
same requested or observed lifecycle truth back onto the shared automation catalog without
inventing a provider-local traffic registry.

When the pack owns an automation answer, operators can inspect the same route through:

- `/engine/cell-traffic-automations`
- `/engine/cell-traffic-automations/providers/traefik`
- `/engine/technology-surfaces/cell-based-architecture`
- `/engine/snapshot`

The technology surface entry lives under `surfaceId = "traefik-ingressroute-traffic-materializations"`
and carries one provider-facing projection per selected route, including the projected or observed
`providerRouteId`, entry points, route match, middleware references, backend service reference, TLS
intent, resource existence, dependency posture, `ingressRouteWriteAction`, freshness metadata, and
additive cleanup-sweep metadata such as `cleanupState`, `cleanup.cleanupStrategy`,
`cleanup.primaryCandidateCount`, `cleanup.dependencyCandidateCount`, and `cleanup.lifecycleActions`. The shared
automation answer for the same route now also carries typed provider conditions through
`CellTrafficAutomationRuntimeDescriptor.MaterializationConditions` plus additive summaries such as
`materialization.conditionCount`, `materialization.highestConditionSeverity`,
`providerMaterialization.conditionCategories`, and `providerMaterialization.conditionBreakdown`.

## Registration

```csharp
engine.AddTraefikTrafficMaterializer(options =>
{
    options.RouteNamespace = "edge-traefik";
    options.EntryPoints.Add("websecure");
    options.Observation.Mode = TraefikTrafficObservationModes.ObserveOnly;
    options.Observation.KubeConfigPath = "/etc/cephalon/traefik-kubeconfig";
    options.Observation.PollingIntervalSeconds = 30;

    options.Routes.Add(new TraefikIngressRouteOptions
    {
        RouteId = "orders-to-public-ingress",
        IngressRouteName = "orders-public-ingress",
        MatchRule = "Host(`orders.example.com`) && PathPrefix(`/orders`)",
        BackendNamespace = "orders-runtime",
        BackendServiceName = "orders-api",
        BackendPort = 8443,
        BackendWeight = 100,
        BackendScheme = "https",
        PassHostHeader = true,
        TlsSecretName = "orders-public-tls",
        TlsOptionsName = "strict-mtls",
        TlsOptionsNamespace = "edge-security"
    });

    options.Routes[0].Middlewares.Add(new TraefikMiddlewareReferenceOptions
    {
        Name = "secure-headers"
    });
    options.Routes[0].Middlewares.Add(new TraefikMiddlewareReferenceOptions
    {
        Name = "orders-rate-limit",
        Namespace = "edge-security"
    });
});
```

Use this pack alongside the shared cell baseline:

```csharp
engine.UseConfiguration(configuration);
engine.AddTraefikTrafficMaterializer(...);
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
            "ProviderId": "traefik"
          }
        ]
      }
    }
  }
}
```

The route must still exist on the shared `ICellRouteCatalog`, and the engine still decides whether
this pack is the selected provider materializer for that route.

Enable cleanup sweeps when the same apply-and-reconcile loop should also delete transferred routes
or prune orphaned Cephalon-owned routes that no longer map to an active automation:

```csharp
engine.AddTraefikTrafficMaterializer(options =>
{
    options.Observation.Mode = TraefikTrafficObservationModes.ApplyAndReconcile;
    options.Observation.KubeConfigPath = "/etc/cephalon/traefik-kubeconfig";
    options.Observation.EnableCleanupSweep = true;
    options.Observation.PollingIntervalSeconds = 30;
    options.Observation.StaleAfterSeconds = 90;
});
```

## Ownership and cleanup model

- the pack writes only `IngressRoute` resources
- `Middleware`, `TLSOption`, backend `Service`, and TLS `Secret` resources remain pre-provisioned dependencies for steady-state apply-and-reconcile; this pack never creates or replaces them
- the pack only replaces an existing `IngressRoute` when ownership matches the current automation or when stale or incomplete Cephalon ownership metadata marks the route as an orphaned transfer candidate
- existing unmanaged resources and active foreign Cephalon owners fail with an explicit ownership-conflict posture instead of being hijacked silently
- owned routes carry stable Cephalon ownership labels and annotations so later observation can verify ownership truthfully
- merged live observation keeps the last write lifecycle action (`create`, `replace`, or `transfer`) visible on the same shared runtime surfaces instead of collapsing every successful reconciliation back to `observe`
- optional cleanup sweeps run only when `EnableCleanupSweep` is true in `apply-and-reconcile` mode, scan the configured route namespaces, delete stale transferred `IngressRoute` resources with `lifecycleAction = delete`, and prune orphaned Cephalon-owned routes with `lifecycleAction = prune`
- when ownership metadata proves safe delete and no active projection still references them, the same cleanup sweeps can also remove Cephalon-managed `Middleware` and `TLSOption` resources that became orphaned or transfer candidates; backend `Service` and TLS `Secret` dependencies remain observe-only
- cleanup sweep summaries stay additive through `providerMaterialization.cleanup*` and `traefik-ingressroute-traffic-materializations` entries so operators can inspect delete/prune posture without losing the selected route's primary materialization answer; those summaries now publish `cleanupStrategy = primary-and-owned-dependencies` plus primary/dependency resource breakdowns

## Current limits

This pack intentionally does not yet claim:

- controller-driven success or policy semantics beyond the shipped typed readiness, ownership,
  dependency, lifecycle, and observation condition taxonomy plus the current CRD-existence and
  dependency checks
- `TraefikService`, parent `IngressRoute`, or richer multi-layer routing follow-through beyond the
  single route-rule and Service backend baseline
- broader dependency-aware teardown beyond the shipped `IngressRoute` plus safe owned `Middleware` and `TLSOption` cleanup-sweep baseline; backend `Service`, TLS `Secret`, and richer dependent-resource families remain later work

Those remain later follow-through so the current provider claim stays honest.

## Related docs

- [Cephalon.Edge](edge.md)
- [Cephalon.Edge.KubernetesGateway](edge-kubernetes-gateway.md)
- [Cephalon.Engine](engine.md)
- [Technology packs](../technology-packs.md)
