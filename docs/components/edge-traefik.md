# Cephalon.Edge.Traefik

`Cephalon.Edge.Traefik` is the second provider-specific control-plane materializer pack for Cephalon cell traffic automation. It proves that the shared provider-materializer seam is not overfit to Kubernetes Gateway API by projecting truthful Traefik IngressRoute intent back onto the same shared runtime surfaces without moving Traefik CRD assumptions into `Cephalon.Engine`.

## What it owns

- `TraefikTrafficMaterializerOptions`, `TraefikIngressRouteOptions`, and `TraefikMiddlewareReferenceOptions` for declarative Traefik IngressRoute projection
- the `AddTraefikTrafficMaterializer(...)` registration entry point for attaching the pack to an `EngineBuilder`
- a provider-specific `ICellTrafficAutomationProviderMaterializer` implementation for `providerId = "traefik"`
- deterministic projection of selected cell routes into Traefik `IngressRoute` intent, including entry points, match rules, middleware references, backend Service references, and TLS options
- the `traefik-ingressroute-traffic-materializations` technology surface under `cell-based-architecture`
- truthful operator metadata such as `providerRouteId`, `ingressRouteNamespace`, `ingressRouteName`, `entryPoints`, `matchRule`, `middlewareRefs`, `serviceRefs`, `tlsSecretName`, `tlsOptionsRef`, and `statusSource = configured-intent`

## Main surfaces

- `Configuration/TraefikTrafficMaterializerOptions.cs`
- `Configuration/TraefikIngressRouteOptions.cs`
- `Configuration/TraefikMiddlewareReferenceOptions.cs`
- `Modules/TraefikTrafficMaterializerModule.cs`
- `Registration/TraefikEngineBuilderExtensions.cs`
- `Services/TraefikTrafficAutomationMaterializer.cs`
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
`providerId = "traefik"` project into Traefik IngressRoute intent?

This pack currently ships one truthful mode:

- default `configured-intent`, which reports `providerAction = projected-intent`,
  `observationMode = configured-intent`, `statusSource = configured-intent`, and
  `resourceState = projection-only` while publishing deterministic Traefik `IngressRoute` intent
  without claiming live cluster state or successful control-plane writes; the shared provider
  materialization state stays `pending`

What this proves is that a second provider family can publish selected materializer ownership,
provider-facing route identity, middleware and TLS intent, and the same `pending` materialization
truth back onto the shared automation catalog without inventing a provider-local traffic registry.

When the pack owns an automation answer, operators can inspect the same route through:

- `/engine/cell-traffic-automations`
- `/engine/cell-traffic-automations/providers/traefik`
- `/engine/technology-surfaces/cell-based-architecture`
- `/engine/snapshot`

The technology surface entry lives under `surfaceId = "traefik-ingressroute-traffic-materializations"`
and carries one provider-facing projection per selected route, including the projected
`providerRouteId`, entry points, route match, middleware references, backend service reference, and
TLS intent.

## Registration

```csharp
engine.AddTraefikTrafficMaterializer(options =>
{
    options.RouteNamespace = "edge-traefik";
    options.EntryPoints.Add("websecure");

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

## Current limits

This pack intentionally does not yet claim:

- live Traefik reconciliation or status polling
- apply-and-reconcile ownership over Traefik CRDs
- `TraefikService`, parent `IngressRoute`, or richer multi-layer routing follow-through beyond the single projected route rule and Service backend baseline

Those remain later follow-through so the current provider claim stays honest.

## Related docs

- [Cephalon.Edge](edge.md)
- [Cephalon.Edge.KubernetesGateway](edge-kubernetes-gateway.md)
- [Cephalon.Engine](engine.md)
- [Technology packs](../technology-packs.md)
