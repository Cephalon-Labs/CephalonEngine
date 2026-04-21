# Cephalon Technology Packs

Technology packs are the runtime companion pattern for future-facing workloads in Cephalon.

They sit between pure metadata and full application blueprints:

- `Blueprints` define project shape
- `Patterns` define supporting design behavior
- `Technologies` define workload posture
- `Technology packs` provide reusable runtime primitives for those technologies

## Why this exists

Technology profiles should not stop at documentation or scaffold hints. When a workload becomes important enough to need reusable services, capabilities, or conventions, it should move into a companion package instead of forcing changes into the engine core.

That keeps the engine extensible without making every new trend a built-in subsystem.
Some profiles can also start as engine-owned contract baselines before a dedicated companion pack
exists. The new `cell-based-architecture` profile now follows that path: the engine ships
`CellBoundaryDescriptor`, `CellRouteDescriptor`, `CellHealthIsolationDescriptor`,
`CellTrafficAutomationRuntimeDescriptor`, `/engine/cells`, `/engine/cell-routes`,
`/engine/cell-health-isolations`, `/engine/cell-traffic-automations`, and the
`cell-boundaries`, `cell-routes`, `cell-health-isolations`, plus `cell-traffic-automations`
technology runtime surfaces today. That engine-owned baseline now also carries additive
`providerId` plus `edgeNodeIds` targeting on the shared automation catalog. The shared
materialization seam is now also explicit through `CellTrafficAutomationMaterializationResult`,
`CellTrafficAutomationMaterializationStates`, `ICellTrafficAutomationProviderMaterializer`, and
`ICellTrafficAutomationEdgeMaterializer`; `Cephalon.Edge` now ships the first concrete
edge-runtime materializer while the provider-named result/state types stay available as
compatibility helpers over the same contract. Future service-mesh, gateway, or provider companion
packs can reconcile provider-owned or edge-owned traffic posture back onto the same shared
automation catalog instead of publishing a second materialization registry. The first concrete
provider-specific control-plane follow-through is now also shipped through
`Cephalon.Edge.KubernetesGateway`, which projects Kubernetes Gateway API `Gateway` plus `HTTPRoute`
intent back onto the same shared automation catalog and publishes that view through the
`kubernetes-gateway-traffic-materializations` technology surface. That same pack now also supports
opt-in live `observe-only` Gateway API polling plus `apply-and-reconcile` ownership-aware
`HTTPRoute` writes so projected intent, write posture, and observed control-plane status can stay
on one runtime truth instead of spawning a second provider-local view.

## Shipped baseline packs

Current baseline packages:

- `Cephalon.Agentics`
  - runtime services and capability activation for `AgenticWorkloads`
  - registers `IAgentToolCatalog` when the profile is selected
- `Cephalon.Eventing`
  - runtime services and capability activation for `EventDrivenIntegration`
  - registers `IEventChannelCatalog` when the profile is selected
- `Cephalon.Eventing.Wolverine`
  - official first-class adapter path for managed dispatch over `EventDrivenIntegration`
  - projects runtime truth for the current Wolverine-backed outbox and dispatch loop without turning Wolverine into an engine-core dependency
- `Cephalon.Retrieval`
  - runtime services and capability activation for `KnowledgeRetrieval`
  - registers `IKnowledgeCatalog` when the profile is selected
- `Cephalon.Edge`
  - runtime services and capability activation for `EdgeNativeDelivery`
  - registers `IEdgeNodeCatalog` when the profile is selected
- `Cephalon.Edge.KubernetesGateway`
  - first provider-specific control-plane materializer over the shared `cell-based-architecture` traffic-automation baseline
  - projects Kubernetes Gateway API intent and can now overlay live Gateway API observation plus owned `HTTPRoute` apply-and-reconcile without moving cluster-specific ownership or reconcile policy into `Cephalon.Engine`
- `Cephalon.Edge.Traefik`
  - second provider-specific control-plane materializer over the shared `cell-based-architecture` traffic-automation baseline
  - projects deterministic Traefik IngressRoute intent, middleware references, backend Service references, and TLS posture back onto the same shared automation catalog without moving Traefik CRD semantics into `Cephalon.Engine`

These packages are also used as scaffold hints for the matching built-in technology profiles.
The phase-8 data packs are companion packages rather than technology packs, but they can now enrich `EventDrivenIntegration` truth by projecting staged outbox producers and application-managed inbox stores into the eventing runtime surfaces when both baselines are active.

## Runtime pattern

The expected layering is:

1. select technology profiles through `Engine:Technologies`
2. install companion packages that understand those profiles
3. register the companion package in startup
4. let module/package runtime behavior activate only when the technology is actually selected
5. let installed modules contribute pack-specific descriptors through the pack's contributor services instead of pushing every descriptor into host startup

The shared cell traffic-materialization seam now also carries one stable lifecycle vocabulary that
provider and edge packs can reuse instead of inventing provider-local status taxonomies:
`CellTrafficAutomationOwnershipStates`, `CellTrafficAutomationDependencyStates`,
`CellTrafficAutomationDriftStates`, and `CellTrafficAutomationLifecycleActions`. The shared runtime
catalog projects those values back onto `providerMaterialization.*`, `edgeMaterialization.*`, and
derived `materialization.*` metadata so requested, observed, conflicted, drifted, or dependency-missing
posture stays comparable across `Cephalon.Edge`, `Cephalon.Edge.KubernetesGateway`, and
`Cephalon.Edge.Traefik`.

Example:

```csharp
builder.AddCephalon(engine =>
{
    engine.AddAgentics(options =>
    {
        options.Tools.Add(new AgentToolDescriptor(
            id: "planner",
            displayName: "Planner",
            description: "Builds agent plans.",
            capabilityKeys: ["workflow.approval.request", "workflow.approval.record"],
            executionGraphId: "approval-flow",
            hostedExecutionId: "approval-pump"));
    });

    engine.AddRetrieval(options =>
    {
        options.Collections.Add(new KnowledgeCollectionDescriptor(
            id: "docs",
            displayName: "Docs",
            description: "Knowledge base for retrieval."));
    });

    engine.AddEventing(options =>
    {
        options.Channels.Add(new EventChannelDescriptor(
            id: "orders",
            displayName: "Orders",
            description: "Integration events for the order domain."));
    });

    engine.AddEdge(options =>
    {
        options.Nodes.Add(new EdgeNodeDescriptor(
            id: "storefront-edge",
            displayName: "Storefront Edge",
            description: "Regional node serving intermittently connected experiences."));
    });
});
```

```json
{
  "Engine": {
    "Technologies": ["AgenticWorkloads", "EventDrivenIntegration", "KnowledgeRetrieval", "EdgeNativeDelivery"]
  }
}
```

## Authoring pattern

A technology pack should usually contain:

- `Configuration/`
  - options or defaults for the workload
- `Services/`
  - runtime contracts the app can consume
- `Modules/`
  - one or more modules that bridge the pack into Cephalon
- `Registration/`
  - `EngineBuilder` extensions for easy startup registration

Recommended contracts to use:

- `ITechnologyContributor`
  - when the pack adds new technology descriptors to the runtime catalog
- `ITechnologyServiceContributor`
  - when services should only activate for selected technologies
- `ITechnologyCapabilityContributor`
  - when capabilities should only activate for selected technologies

Shipped pack-specific extension points:

- `Cephalon.Agentics`
  - `IAgentToolContributor` and `IAgentToolRegistry`
- `Cephalon.Retrieval`
  - `IKnowledgeCollectionContributor` and `IKnowledgeCollectionRegistry`
- `Cephalon.Eventing`
  - `IEventChannelContributor` and `IEventChannelRegistry`
- `Cephalon.Edge`
  - `IEdgeNodeContributor` and `IEdgeNodeRegistry`

Those contributor interfaces are the preferred way for installed modules to add descriptors into a selected technology pack. Project-level code can still replace the final catalog service through DI when it needs full control.

For `Cephalon.Agentics`, `AgentToolDescriptor` can now also link back to:

- published capability keys through `capabilityKeys`
- one execution graph through `executionGraphId`
- one hosted execution through `hostedExecutionId`

That keeps AI-facing tool metadata anchored in the same module, capability, execution-graph, hosted-execution, and runtime-story contracts the engine already exposes.

Runtime introspection contract:

- `ITechnologyRuntimeContributor`
  - used by packs to project their active runtime surface into a transport-neutral snapshot
- `ITechnologyRuntimeCatalog`
  - host-agnostic abstraction for reading the merged runtime surface set in code
- `IRuntimeIntrospectionSnapshotProvider`
  - engine-level abstraction for reading one operator-facing snapshot that combines the runtime manifest, runtime status, and active technology-pack surfaces
- `GET /engine/technology-surfaces`
  - returns the active pack surfaces and the merged entries visible to the runtime after host options and module contributors have both been applied; agentic tools now also surface linked capability keys plus live execution-graph and hosted-execution state when those links are declared
- `GET /engine/snapshot`
  - returns the broader runtime introspection snapshot when operators need manifest, runtime status, and technology-pack surfaces in one payload

## Guardrails

- keep packs additive; do not mutate engine core behavior globally
- prefer activating services/capabilities from `TechnologySelection` instead of branching in hosts
- prefer pack-specific contributor services over hardcoded host-owned descriptor lists when extending a shipped pack
- prefer `ITechnologyRuntimeContributor` when a pack needs an operator-facing runtime snapshot instead of inventing host-specific ad-hoc endpoints
- keep built-in technology profiles lightweight; move reusable runtime behavior into packs
- only create a new blueprint when project shape changes materially
- only add a new built-in technology profile when validation, guidance, or scaffold/package hints are distinct enough to justify it
