# Cephalon Engine Architecture Recommendations

Recommendations in this document reflect the repository state as of `April 20, 2026`.

Cross-references: `docs/architecture-inventory.md`, `docs/engine-roadmap.md`, `docs/engine-backlog.md`

## Purpose

This document catalogs architecture patterns and capabilities that should be added to CephalonEngine, with priority ranking, implementation approach, and rationale. Items are organized into three priority tiers and three planned phases.

## Priority 1 — High impact, low risk

These patterns address gaps that production microservice deployments encounter immediately. They should land first.

### Onion Architecture (pattern descriptor)

Current state: shipped. `BuiltInPatterns.cs` now includes `onion-architecture`, so the dependency-inversion taxonomy is complete across Clean, Hexagonal, and Onion. Remaining follow-through is guidance, not descriptor registration.

Recommendation: keep the descriptor stable and add adoption guidance only when a concrete sample or blueprint needs Onion-specific conventions beyond the shared architecture taxonomy.

Effort: trivial — taxonomy-only, no new runtime code.

### Circuit Breaker (resilience infrastructure)

Current state: the contract-first baseline is now shipped through `Engine:Resilience:CircuitBreaker`, `AppProfile.Resilience`, and `/engine/resilience`, and the behavior pipeline now also enforces circuit-breaker policy through `Cephalon.Behaviors`, `/engine/behavior-resilience`, and `snapshot.BehaviorResiliencePolicies`. The runtime publishes live circuit metadata such as current state, last-open timestamps, retry-after timing, and the last exception type that opened the circuit, and `Cephalon.Behaviors.Http` now translates open-circuit rejections into truthful REST `503` responses plus per-route OpenAPI metadata while the generic GraphQL, JSON-RPC, SSE, and WebSocket behavior HTTP adapters keep the same open-circuit truth in protocol-native envelopes.

Recommendation: keep the shared `Microsoft.Extensions.Resilience` (Polly v8) baseline in `Cephalon.Behaviors` and extend it through host-agnostic exception classification plus dependency-health-informed policy tuning, rather than adding a second parallel circuit-breaker abstraction too early.

Implementation outline:
- keep `Engine:Resilience:CircuitBreaker` as the requested configuration contract
- keep `IBehaviorResilienceExceptionClassifier` as the host-agnostic failure-classification seam
- keep live breaker truth in `/engine/behavior-resilience` and `snapshot.BehaviorResiliencePolicies`
- compose future health and upstream-signal inputs into the same runtime instead of inventing a second breaker stack
- add a first-class capability only when it becomes part of a broader resilience/runtime-governance story

Effort: medium.

### Retry, Timeout, Circuit Breaker, Bulkhead, and Behavior Rate Limiting (resilience suite)

Current state: the contract-first baseline is now shipped through `Engine:Resilience` with `Retry`, `Timeout`, `CircuitBreaker`, `Bulkhead`, and `RateLimiting` selections plus operator-facing introspection, and the current behavior-pipeline follow-through now enforces shared execution retry, timeout, circuit-breaker, bulkhead, and rate-limiting policies through `Cephalon.Behaviors`, `/engine/behavior-resilience`, and `snapshot.BehaviorResiliencePolicies`. `Engine:Resilience:BehaviorExecution:Overrides` now adds behavior- and transport-scoped override resolution with precedence `behavior+transport > behavior > transport > default`, including explicit disable answers that suppress inherited retry, timeout, circuit-breaker, bulkhead, or behavior-execution rate limiting for a narrower surface. The behavior contract layer now also exposes `BehaviorIdempotencyAttribute` plus `BehaviorIdempotencyMode`, the default classifier distinguishes retry-eligible transient failures only for explicitly idempotent behaviors, and `IBehaviorResilienceRuntimeCatalog.Resolve(...)` now surfaces behavior-specific retry-eligibility metadata plus effective retry settings. Automatic replay remains intentionally gated by that explicit idempotency contract instead of being inferred from transports or CQRS naming. The HTTP transport layer now also reuses one shared resilience-fault mapping so REST keeps truthful `429`/`503` answers while the generic GraphQL, JSON-RPC, SSE, and WebSocket behavior HTTP adapters keep rate-limiting, timeout, and open-circuit outcomes protocol-native.

Recommendation: keep ASP.NET Core middleware as the truthful baseline for public HTTP protection, keep the shared `Microsoft.Extensions.Resilience` baseline in `Cephalon.Behaviors` for cross-transport execution retry/timeout/circuit-breaker/bulkhead/rate-limiting behavior, treat behavior-authored idempotency as the retry gate, and use endpoint-scoped ASP.NET Core rate-limiting overrides when a REST route needs behavior-owned `429` semantics to surface instead of the host limiter.

Implementation outline:
- Keep `BehaviorIdempotencyAttribute` / `BehaviorIdempotencyMode` as the explicit replay-safety contract for behavior authors
- Keep `IBehaviorResilienceExceptionClassifier` as the host-agnostic retry/circuit-breaker decision seam
- Keep automatic retry execution in the shared `Cephalon.Behaviors` pipeline only when the effective behavior policy requests retry and the classifier reports a retryable transient fault for an explicitly idempotent behavior
- Per-behavior and per-transport resilience configuration through `Engine:Resilience:BehaviorExecution:Overrides`
- Capabilities: `resilience.retry`, `resilience.timeout`, `resilience.circuit-breaker`, `resilience.bulkhead`, `resilience.rate-limiting`

Effort: medium.

### Rate Limiting (API protection)

Current state: the contract-first baseline is now shipped through `Engine:Resilience:RateLimiting`,
`AppProfile.Resilience`, and `/engine/resilience`, and the first transport-native follow-through is
also shipped through ASP.NET Core middleware, `/engine/rate-limiting`, and
`snapshot.RateLimitingPolicies`. The current limiter covers public HTTP endpoints, intentionally
excludes operator/docs routes, and now supports endpoint-scoped override modeling through
`Engine:Resilience:RateLimiting:Overrides` with behavior-aware precedence across module-owned REST
routes and generic behavior HTTP bindings. The runtime metadata now also distinguishes long-lived
stream and connection surfaces through fields such as `transportKind`, `transportSemantics`, and
`enforcementMoment`, so GraphQL-SSE, GraphQL-WS, SSE, and WebSocket policies no longer collapse
into one generic endpoint label.

Recommendation: keep ASP.NET Core middleware as the truthful baseline for public HTTP protection, keep the shared behavior-dispatch pipeline as the truthful non-host follow-through for cross-transport execution limits, and coordinate the two through endpoint-scoped overrides so REST routes can intentionally expose either the host-owned or behavior-owned `429` answer without lying in runtime catalogs or OpenAPI. That same behavior-owned limiter truth should stay protocol-native for generic behavior HTTP adapters instead of being rewrapped into one generic failure contract.

Remaining follow-through:
- Broader non-REST transport-native resilience-envelope semantics beyond the current ASP.NET Core public-HTTP plus shared behavior-dispatch `429`/`503` baseline for rate limiting, timeout, and circuit breaker
- Capability: `resilience.rate-limiting`

Effort: medium for the remaining non-baseline work.

## Priority 2 — Strategic value

These patterns provide adoption acceleration and complete the distributed systems story.

### Strangler Fig (migration support)

Current state: the contract-first runtime baseline, the first configuration-driven policy overlay, the first engine-first ingress follow-through, and the first ASP.NET Core host-level cutover baseline are now shipped. `BuiltInPatterns.cs` still carries the `strangler-fig` descriptor, and `Cephalon.Abstractions` now also exports `IStranglerFigRouteContributor`, `IStranglerFigRouteRegistry`, `IStranglerFigRuntimeCatalog`, `IStranglerFigMigrationRuntimeCatalog`, `IStranglerFigIngressRuntimeCatalog`, `IStranglerFigRouter`, `StranglerFigIngressRuntimeDescriptor`, `StranglerFigMigrationRuntimeDescriptor`, `StranglerFigRequest`, `StranglerFigRouteDescriptor`, `StranglerFigRouteResolution`, and `StranglerFigTarget`. `Cephalon.Engine` now composes those routes into the authored runtime catalog and snapshot, applies `Engine:Migration:StranglerFig` defaults and per-route policy overlays deterministically, projects effective migration progress plus target-selection answers through `snapshot.StranglerFigRoutePolicies`, and also projects normalized ingress materialization answers through `snapshot.StranglerFigIngressRoutes`. ASP.NET Core now exposes `/engine/strangler-fig`, `/engine/strangler-fig/runtime`, `/engine/strangler-fig/ingress`, `/engine/strangler-fig/resolve`, `/engine/strangler-fig/cutover`, and `/engine/strangler-fig/cutover/resolve` as operator-facing surfaces. When `Engine:Migration:StranglerFig:AspNetCore` is enabled, the host-level cutover runtime now derives from that shared ingress truth: rooted local selected endpoints rewrite in-process, absolute HTTP or HTTPS selected endpoints can redirect or proxy, and opaque or unsupported selected endpoints fail truthfully with `502` without introducing a second routing truth outside the shared migration catalogs. Remaining work is provider-specific ingress or edge automation, not the first engine-owned ingress runtime.

Recommendation: keep the host-agnostic route-contribution, runtime-catalog, migration-policy catalog, ingress catalog, and request-resolution contracts stable, keep host-level cutover derived from those shared catalogs, and add provider-specific ingress or edge automation only when a concrete edge or host needs it.

Implementation outline:
- `PatternDescriptor` "strangler-fig" in `BuiltInPatterns.cs`
- `IStranglerFigRouteContributor` + `IStranglerFigRouteRegistry` — let modules contribute migration boundaries explicitly
- `IStranglerFigRuntimeCatalog` + `IStranglerFigMigrationRuntimeCatalog` + `IStranglerFigIngressRuntimeCatalog` + `IStranglerFigRouter` — publish the authored route catalog, effective migration-policy answers, normalized ingress materialization answers, and request ownership without leaking host APIs into abstractions
- `/engine/strangler-fig`, `/engine/strangler-fig/runtime`, `/engine/strangler-fig/ingress`, `/engine/strangler-fig/resolve`, `/engine/strangler-fig/cutover`, `/engine/strangler-fig/cutover/resolve`, `snapshot.StranglerFigRoutes`, `snapshot.StranglerFigRoutePolicies`, and `snapshot.StranglerFigIngressRoutes` — operator-facing runtime surface for the shipped baseline
- Configuration: `Engine:Migration:StranglerFig` for default target/progress overlays plus per-route overrides, and `Engine:Migration:StranglerFig:AspNetCore` for host cutover handling
- Capability: `migration.strangler-fig`

Effort: small-to-medium for provider-specific ingress or edge automation on top of the shipped engine-first ingress runtime.

### Anti-Corruption Layer (DDD integration boundary)

Current state: shipped as a taxonomy descriptor. `BuiltInPatterns.cs` now includes `anti-corruption-layer`, so the remaining work is guidance and interface conventions rather than descriptor registration.

Recommendation: keep the descriptor stable and add translator conventions only when a concrete integration slice needs them.

Implementation outline:
- `PatternDescriptor` "anti-corruption-layer" in `BuiltInPatterns.cs`
- `IAntiCorruptionTranslator<TExternal, TInternal>` interface convention
- Guidance in pattern descriptor for module boundary translation

Effort: small.

### Saga Choreography (event-driven saga variant)

Current state: the first host-agnostic choreography baseline, the first explicit eventing bridge,
the first higher-level authoring-helper follow-through, the first choreography
runtime/operator catalog follow-through, the first live choreography publication-state
follow-through, and the first module-backed capability-publication follow-through are now shipped.
`Cephalon.Behaviors.Patterns` exposes
`ChoreographySagaExecutionStrategy`, `ISagaChoreographyPublisher`,
`ISagaChoreographyStepResult`, `ISagaEventReactor<TEvent>`,
`ISagaEventReactor<TEvent, TOutput>`, `SagaChoreographyPublication`,
`SagaChoreographyStepResult`, `SagaChoreographyStepResult<TOutput>`, and an in-memory default
publisher so choreography steps can publish continuation or compensation work through typed helper
contracts without forcing a hard dependency on `Cephalon.Eventing`. `Cephalon.Abstractions` now
also exposes `SagaChoreographyRuntimeDescriptor`, `ISagaChoreographyRuntimeCatalog`,
`SagaChoreographyPublicationRuntimeState`, and
`ISagaChoreographyPublicationRuntimeStateCatalog`. `Cephalon.Behaviors.Patterns` derives the
static operator surface from shared behavior topology plus registered implementation types and now
also reports accepted or failed publication handoff observations directly from
`ChoreographySagaExecutionStrategy`, `Cephalon.Engine` projects those answers through both
`snapshot.SagaChoreographies` and `snapshot.SagaChoreographyPublicationStates`, and ASP.NET Core
now exposes `/engine/saga-choreographies` plus `/engine/saga-choreographies/runtime` with
behavior/module/transport/channel/correlation/failure/compensation drill-down routes.
`Cephalon.Behaviors` now also publishes
`behaviors.saga-choreography.runtime-catalog` and
`behaviors.saga-choreography.publication-state` through `/engine/capabilities` only when
`AddBehaviorPatterns()` actually registers the shared choreography runtime services, so capability
truth stays module-backed instead of inventing a synthetic engine or bridge source.
`Cephalon.Eventing.Behaviors` still provides the explicit `AddBehaviorEventingBridge()`
follow-through that stages those publications through the shared outbox-backed eventing publish
path when that path is actually available.

Recommendation: keep the host-agnostic choreography contracts stable, keep the bridge explicit
through a dedicated companion pack rather than collapsing the behavior-pattern layer into
`Cephalon.Eventing` or making choreography publication an ambient side effect of enabling the
eventing technology pack, keep module-backed capability publication derived from real pattern
activation instead of synthetic engine ownership, and keep strategy-owned publication handoff
observations separate from provider-specific publish or downstream dispatch truth.

Implementation outline:
- shipped baseline: `ChoreographySagaExecutionStrategy`, `ISagaChoreographyPublisher`,
  `SagaChoreographyPublication`, `SagaChoreographyStepResult`, `InMemorySagaChoreographyPublisher`,
  and capability `behaviors.saga-choreography`
- shipped follow-through: `Cephalon.Eventing.Behaviors`, `AddBehaviorEventingBridge()`, capability
  `eventing.behaviors.saga-choreography`, and the `saga-choreography-bridges` runtime surface for
  explicit handoff into the shared outbox-backed eventing publication runtime
- shipped authoring-helper follow-through: `ISagaChoreographyStepResult`,
  `ISagaEventReactor<TEvent>`, `ISagaEventReactor<TEvent, TOutput>`,
  `SagaChoreographyStepResult<TOutput>`, and typed JSON publication helpers on
  `SagaChoreographyPublication` so choreography authors can stay on the shared execution contract
  without inventing a second registry or hard-depending on `Cephalon.Eventing`
- shipped runtime/operator follow-through: `SagaChoreographyRuntimeDescriptor`,
  `ISagaChoreographyRuntimeCatalog`, `snapshot.SagaChoreographies`,
  `/engine/saga-choreographies`, `/engine/saga-choreographies/{behaviorId}`,
  `/engine/saga-choreographies/modules/{moduleId}`, and
  `/engine/saga-choreographies/transports/{transportId}` all stay derived from the same shared
  choreography behavior topology and registered implementation types
- shipped live publication-state follow-through: `SagaChoreographyPublicationRuntimeState`,
  `ISagaChoreographyPublicationRuntimeStateCatalog`,
  `snapshot.SagaChoreographyPublicationStates`,
  `/engine/saga-choreographies/runtime`,
  `/engine/saga-choreographies/runtime/behaviors/{behaviorId}`,
  `/engine/saga-choreographies/runtime/modules/{moduleId}`,
  `/engine/saga-choreographies/runtime/transports/{transportId}`,
  `/engine/saga-choreographies/runtime/channels/{channelId}`,
  `/engine/saga-choreographies/runtime/correlations/{correlationId}`,
  `/engine/saga-choreographies/runtime/compensations`, and
  `/engine/saga-choreographies/runtime/failures` keep live handoff observations on the shared
  choreography execution path without pretending to replace downstream eventing or broker dispatch
  truth
- shipped capability-publication follow-through:
  `behaviors.saga-choreography.runtime-catalog` and
  `behaviors.saga-choreography.publication-state` now publish through the `behaviors` module only
  when `AddBehaviorPatterns()` actually activates `ISagaChoreographyRuntimeCatalog` and
  `ISagaChoreographyPublicationRuntimeStateCatalog`, and their metadata points back to the pattern
  pack, shared service contracts, snapshot fields, and ASP.NET Core routes without claiming
  eventing-bridge ownership

Effort: medium for any later provider-specific downstream choreography follow-through.

### Backend for Frontend — BFF (explicit pattern)

Current state: the contract-first client-binding runtime baseline, the first client-aware REST filtering follow-through, and the first scope-specific REST documentation/materialization follow-through are now shipped. `BuiltInPatterns.cs` still carries the `backend-for-frontend` descriptor, and `Cephalon.Abstractions` now also exports `BackendForFrontendBehaviorFilterDescriptor`, `BackendForFrontendClientBindingDescriptor`, `IBackendForFrontendClientBindingContributor`, `IBackendForFrontendClientBindingRegistry`, `IBackendForFrontendRuntimeCatalog`, `BackendForFrontendRestEndpointRuntimeDescriptor`, `IBackendForFrontendRestRuntimeCatalog`, `BackendForFrontendRestDocumentRuntimeDescriptor`, and `IBackendForFrontendRestDocumentRuntimeCatalog`. `Cephalon.Engine` still composes host-added, module-contributed, and `Engine:BackendForFrontend:Bindings` client bindings into one runtime catalog, auto-selects the `backend-for-frontend` pattern when bindings exist, and projects the merged binding answer through `snapshot.BackendForFrontendBindings`, while ASP.NET Core now derives both client-aware REST runtime answers and scope-specific OpenAPI/Scalar document descriptors from that shared binding catalog plus `IRestEndpointRuntimeCatalog` and the normal host OpenAPI publication settings instead of inventing host-only registries. ASP.NET Core now exposes `/engine/backend-for-frontend` plus client, module, and transport drill-down routes for binding truth, `/engine/backend-for-frontend/rest-endpoints` plus binding, client, module, published-endpoint, and id drill-down routes for effective REST visibility per client binding, and `/engine/backend-for-frontend/rest-documents` plus binding, client, and id drill-down routes for the actual materialized document surfaces. Remaining work is broader non-REST transport-specific follow-through, not the core BFF REST runtime or documentation baseline.

Recommendation: keep the host-agnostic client-binding contracts and runtime catalogs stable, keep both client-aware REST filtering and scope-specific REST document materialization derived from `IBackendForFrontendRuntimeCatalog` plus `IRestEndpointRuntimeCatalog`, and only add transport-specific materialization when a concrete frontend surface cannot be expressed as a truthful projection of those shared runtime answers.

Implementation outline:
- `PatternDescriptor` "backend-for-frontend" in `BuiltInPatterns.cs` with aliases `["BackendForFrontend", "BFF"]`
- `BackendForFrontendClientBindingDescriptor`, `IBackendForFrontendClientBindingContributor`, `IBackendForFrontendClientBindingRegistry`, and `IBackendForFrontendRuntimeCatalog` for host-agnostic client-binding contribution and reads
- `Engine:BackendForFrontend:Bindings` for configuration-driven binding contribution without inventing a host-only registry
- `/engine/backend-for-frontend` plus `snapshot.BackendForFrontendBindings` for the shipped operator-facing runtime surface
- `BackendForFrontendRestEndpointRuntimeDescriptor`, `IBackendForFrontendRestRuntimeCatalog`, `/engine/backend-for-frontend/rest-endpoints`, and `snapshot.BackendForFrontendRestEndpoints` for the shipped client-aware REST follow-through derived from existing runtime catalogs
- `BackendForFrontendRestDocumentRuntimeDescriptor`, `IBackendForFrontendRestDocumentRuntimeCatalog`, `/engine/backend-for-frontend/rest-documents`, `snapshot.BackendForFrontendRestDocuments`, and scope-specific filtered OpenAPI plus Scalar routes under the configured host prefixes for the shipped documentation/materialization follow-through
- Non-REST transport-specific follow-through as later slices

Effort: small for the remaining non-REST transport-materialization follow-through.

### Feature Flags (progressive delivery)

Current state: the first contract-first feature-flag baseline is now shipped. `Cephalon.Abstractions`
now exports `FeatureFlagDescriptor`, `FeatureFlagProviderBindingDescriptor`,
`FeatureFlagProviderEvaluationResult`, `FeatureFlagTargetingDescriptor`,
`FeatureFlagEvaluationContext`, `FeatureFlagEvaluationResult`, `IFeatureToggle`,
`IFeatureFlagProvider`, `IFeatureFlagRuntimeCatalog`, `IFeatureFlagContributor`, and
`IFeatureFlagRegistry`. `Cephalon.Engine` now merges host-added, configuration-driven, and
module-contributed flags through `engine.AddFeatureFlag(...)`, `engine.AddFeatureFlags(...)`,
`Engine:Features`, and `IFeatureFlagContributor`, projects the merged catalog into
`snapshot.FeatureFlags`, and evaluates runtime answers through `IFeatureToggle`. The generic
external-provider bridge baseline is now also shipped there: `FeatureFlagDescriptor.ProviderBindings`,
`Engine:Features:Flags:*:ProviderBindings`, and `engine.AddFeatureFlagProvider(...)` let provider
companion packs contribute additional rollout gates through `IFeatureFlagProvider` without
replacing the Cephalon-owned descriptor catalog. ASP.NET Core now exposes `/engine/features` plus
enabled/disabled/module/id drill-down routes and `/engine/features/{featureFlagId}/evaluate`, with
provider-backed evaluation details now flowing through `ProviderResults` on the shared result.
The next shared-consumption follow-through is also now shipped: `BehaviorTopologyDescriptor`
carries ordered `RequiredFeatureFlagIds` plus `SourceModuleId`,
`IBehaviorTopologyBuilder` exposes `RequireFeatureFlag(...)` / `RequireFeatureFlags(...)`, the
shared `Cephalon.Behaviors` pipeline now evaluates those requirements through `IFeatureToggle`,
source-generated topology literals keep the same declarations build-time aligned, and the behavior
runtime surface now reports feature-gated behavior counts plus per-behavior ownership metadata.
`Cephalon.Behaviors.Http` then projects that same behavior-owned gate into REST helper execution
and JSON-RPC envelopes without turning transport middleware into the only source of rollout truth.

Recommendation: keep the current host-agnostic descriptor/catalog/evaluator baseline stable, keep
module ownership explicit by requiring module-contributed flags to stay
`FeatureFlagSourceKind.Module` with a matching `SourceModuleId`, keep behavior-owned execution
gates in shared behavior topology instead of duplicating that truth in host-only middleware, and
keep external-provider participation additive by letting providers further gate Cephalon-owned
flags through typed bindings instead of replacing the merged catalog with opaque remote state.

Remaining follow-through:
- provider-specific companion-pack integrations for LaunchDarkly, Azure App Configuration,
  Unleash, or similar providers on top of the shipped generic bridge contracts
- reconsider a dedicated `runtime.feature-flags` capability only if runtime capability provenance
  expands beyond the current module-owned model or a truthful module-backed publication path exists

Effort: medium.

## Priority 3 — Differentiators

These patterns would make CephalonEngine stand out among modern application frameworks.

### Cell-Based Architecture

Current state: the first explicit cell-boundary baseline, the first governed cell-route baseline, the first cell health-isolation baseline, the first configuration-driven traffic-automation baseline, provider-aware and edge-aware targeting, and the first provider-managed plus edge-runtime materialization follow-through are now shipped. `CellBoundaryDescriptor`, `ICellBoundaryContributor`, `ICellBoundaryRegistry`, `ICellBoundaryCatalog`, `CellRouteDescriptor`, `ICellRouteContributor`, `ICellRouteRegistry`, `ICellRouteCatalog`, `CellHealthIsolationDescriptor`, `ICellHealthIsolationContributor`, `ICellHealthIsolationRegistry`, `ICellHealthIsolationCatalog`, `CellTrafficAutomationRuntimeDescriptor`, `ICellTrafficAutomationRuntimeCatalog`, `/engine/cells`, `/engine/cell-routes`, `/engine/cell-health-isolations`, `/engine/cell-traffic-automations`, `snapshot.CellBoundaries`, `snapshot.CellRoutes`, `snapshot.CellHealthIsolations`, `snapshot.CellTrafficAutomations`, and the `cell-boundaries`, `cell-routes`, `cell-health-isolations`, plus `cell-traffic-automations` technology runtime surfaces now make module-owned blast-radius posture, cell-to-cell routing posture, cell health-isolation posture, effective automation policy, and selected provider or edge materializer reconciliation posture readable from one shared runtime truth.

Recommendation: keep cells as a technology/profile concern instead of a new blueprint, keep module ownership explicit at boundary, route, and health-isolation levels, keep deterministic automation overlays in `Engine:Cells:TrafficAutomation`, and keep provider or edge materialization additive through the shared automation catalog by letting concrete packs implement `ICellTrafficAutomationProviderMaterializer` or `ICellTrafficAutomationEdgeMaterializer` instead of inventing a second traffic registry.

Implementation outline:
- shipped baseline: built-in `TechnologyDescriptor` `cell-based-architecture`, `CellBoundaryDescriptor`, `ICellBoundaryContributor`, `ICellBoundaryCatalog`, `CellRouteDescriptor`, `ICellRouteContributor`, `ICellRouteCatalog`, `CellHealthIsolationDescriptor`, `ICellHealthIsolationContributor`, `ICellHealthIsolationCatalog`, `CellTrafficAutomationRuntimeDescriptor`, `ICellTrafficAutomationRuntimeCatalog`, `Engine:Cells:TrafficAutomation`, `/engine/cells`, `/engine/cell-routes`, `/engine/cell-health-isolations`, `/engine/cell-traffic-automations`, `snapshot.CellBoundaries`, `snapshot.CellRoutes`, `snapshot.CellHealthIsolations`, `snapshot.CellTrafficAutomations`, the `cell-boundaries`, `cell-routes`, `cell-health-isolations`, plus `cell-traffic-automations` technology runtime surfaces, additive `providerId` plus `edgeNodeIds` targeting, and selected `providerMaterializerId` plus `edgeMaterializerId` reconciliation posture on that same catalog
- later follow-through: richer multi-provider reconciliation and additional provider-specific control-plane materializers on top of the shipped provider-aware and edge-aware automation catalog
- later follow-through: module-to-cell mapping through configuration when explicit operator scenarios justify that extra layer

Effort: medium for the remaining follow-through.

### Data Mesh (domain-owned data products)

Current state: the first data-product baseline is now shipped. `Cephalon.Abstractions` now exposes
`IDataProduct<T>`, `DataProductDescriptor`, `IDataProductCatalog`,
`IDataProductContributor`, and `IDataProductRegistry`; `Cephalon.Engine` now
projects `snapshot.DataProducts`; and ASP.NET Core now exposes `/engine/data-products`
plus `/engine/data-products/{dataProductId}` so module-owned query surfaces are
inspectable through the same runtime truth as other engine catalogs.

Recommendation: keep the initial descriptor/runtime surface focused on module ownership,
domain identity, contract identity, and access mode so data mesh stays host-agnostic
without freezing provider or federation decisions too early.

Shipped baseline:
- `IDataProduct<T>` — module-owned, queryable data product
- `DataProductDescriptor` — operator-facing module/domain/contract/mode metadata
- data product catalog in runtime surface through `/engine/data-products` and `snapshot.DataProducts`

Later follow-through:
- self-serve data infrastructure via module capabilities
- capability convention: `data.data-product`
- provider-specific federation, freshness, and governance follow-through over the shipped catalog

Effort: medium for the remaining follow-through.

### Durable Execution (Temporal/Restate style)

Current state: the first durable-execution baseline, the first durable runtime/operator catalog follow-through, the first durable live-state/failure-posture follow-through, the first durable timer/signal coordination follow-through, and the first durable compensation-helper follow-through are now shipped. `IBehaviorTopologyBuilder.AsDurableExecution()` plus source-generated `durable-execution` literals let behaviors opt into replay explicitly, and `Cephalon.Behaviors.Patterns` now exports `IDurableExecution<TState>`, `IDurableExecution<TInput, TState, TOutput>`, `DurableExecutionState<TState>`, `DurableExecutionStepResult<TOutput>`, and `DurableExecutionStrategy`. The shared strategy replays state from `IEventStore`, validates sequential stream versions before append, returns truthful `200`, `202`, or `204` outcomes based on local output versus continuation-only work or pending timer/signal waits, and now also reports per-stream `started`, `succeeded`, `continuation-staged`, `waiting`, `completed`, and `failed` runtime observations. `Cephalon.Behaviors` now also exposes capability `behaviors.durable-execution` plus rule `ABT-006`, and the Kafka, RabbitMQ, and test behavior contexts can now flow `IEventStore` into the shared pipeline for non-default-host execution. `Cephalon.Abstractions` now also exports `DurableExecutionRuntimeDescriptor`, `IDurableExecutionRuntimeCatalog`, `DurableExecutionRuntimeState`, `IDurableExecutionRuntimeStateCatalog`, `DurableExecutionPendingTimer`, `DurableExecutionPendingSignal`, and `DurableExecutionCompensationAction`; `Cephalon.Behaviors.Patterns` derives the static catalog from shared behavior topology plus registered durable behavior types and reports per-stream live posture from the shared strategy; `Cephalon.Engine` projects both `snapshot.DurableExecutions` and `snapshot.DurableExecutionStates`; and ASP.NET Core now exposes `/engine/durable-executions` plus id/module/transport drill-down routes together with `/engine/durable-executions/runtime` plus stream, behavior, module, transport, pending-timer, pending-signal, and compensation-action drill-down routes.

Recommendation: keep the replay contract host-agnostic and `IEventStore`-backed, keep durable authoring explicit through `IDurableExecution` instead of hiding deterministic replay requirements behind generic behavior interfaces, and keep the operator/runtime catalog derived from shared behavior topology plus registered behavior types instead of adding a second workflow registry beside the behavior system.

Implementation outline:
- shipped baseline: `IBehaviorTopologyBuilder.AsDurableExecution()`, source-generated `durable-execution` literals, `IDurableExecution<TState>`, `IDurableExecution<TInput, TState, TOutput>`, `DurableExecutionState<TState>`, `DurableExecutionStepResult<TOutput>`, `DurableExecutionStrategy`, capability `behaviors.durable-execution`, and compatibility rule `ABT-006`
- shipped operator/runtime follow-through: `DurableExecutionRuntimeDescriptor`, `IDurableExecutionRuntimeCatalog`, `snapshot.DurableExecutions`, `/engine/durable-executions`, `/engine/durable-executions/{behaviorId}`, `/engine/durable-executions/modules/{moduleId}`, and `/engine/durable-executions/transports/{transportId}` all stay derived from the same shared durable behavior topology
- shipped live-state/failure follow-through: `DurableExecutionRuntimeState`, `IDurableExecutionRuntimeStateCatalog`, `snapshot.DurableExecutionStates`, `/engine/durable-executions/runtime`, and the stream/behavior/module/transport drill-down routes all stay derived from the same shared durable strategy and topology truth
- shipped timer/signal coordination follow-through: `DurableExecutionPendingTimer`, `DurableExecutionPendingSignal`, `DurableExecutionStepResult<TOutput>.PendingTimers`, `DurableExecutionStepResult<TOutput>.PendingSignals`, the durable `waiting` runtime posture, and the pending timer/signal filter routes all stay additive over the same shared runtime-state catalog instead of creating a second workflow registry
- shipped compensation-helper follow-through: `DurableExecutionCompensationAction`, `DurableExecutionStepResult<TOutput>.CompensationActions`, `DurableExecutionRuntimeState.CompensationActions`, and the compensation filter routes all stay additive over the same shared runtime-state catalog instead of becoming a second workflow engine
- later follow-through: any future auto-executing recovery should remain additive over the shared replay contract instead of becoming a second workflow engine hidden inside `Cephalon.Behaviors`

Effort: small-to-medium for the remaining follow-through.

### Change Data Capture — CDC (event-driven data sync)

Current state: the first CDC descriptor baseline, the first live runtime-state follow-through, the
first typed freshness/lag/publication follow-through, and the first shared hosted-execution
substrate are now shipped. `Cephalon.Abstractions` now exposes `ICdcCapture`,
`CdcCaptureExecutionResult`, `CdcCaptureDescriptor`, `ICdcCaptureCatalog`,
`ICdcCaptureContributor`, `ICdcCaptureRegistry`, `CdcCaptureRuntimeState`,
`ICdcCaptureRuntimeStateCatalog`, `CdcCaptureFreshnessStatus`, `CdcCaptureLagStatus`, and
`CdcCapturePublicationStatus`; `Cephalon.Engine` now projects both `snapshot.CdcCaptures` and
`snapshot.CdcCaptureStates` while validating referenced `sourceModuleId` and `outboxId` values
against the active runtime; `Cephalon.Data` now supplies the shared reporter/catalog path plus the
shared in-process CDC pump; and ASP.NET Core now exposes both `/engine/cdc-captures` and
`/engine/cdc-captures/runtime` while the broader runtime already exposes the same CDC execution
substrate through `/engine/execution-graphs`, `/engine/hosted-executions`, `/engine/runtime-story`,
and `/engine/snapshot`.

Recommendation: keep the shared CDC surface focused on module ownership,
provider/source/outbox identity, capture mode, event format, resource ids,
bounded batch results, operator-facing metadata, latest reported runtime posture,
and generic-host lifecycle truth so CDC stays host-agnostic and additive over
explicit outbox descriptors instead of pulling provider-specific source semantics
or topology-specific scheduling into the engine core too early.

Shipped baseline:
- `ICdcCapture` plus `CdcCaptureExecutionResult` — host-agnostic bounded CDC execution contract for provider-specific implementations
- `CdcCaptureDescriptor` — operator-facing module/provider/source/outbox/mode/event-format metadata with resource ids, tags, and free-form metadata
- CDC capture catalog in the runtime surface through `/engine/cdc-captures*` and `snapshot.CdcCaptures`
- `CdcCaptureRuntimeState` plus `ICdcCaptureRuntimeStateCatalog` — operator-facing live CDC posture with latest outcome, totals, checkpoints, typed freshness/lag/publication status, and optional linked `OutboxDispatchState`
- `CdcCaptureFreshnessStatus`, `CdcCaptureLagStatus`, and `CdcCapturePublicationStatus` — typed operator-facing freshness, lag, and publication-posture answers that provider packs can report without falling back to metadata-only semantics
- CDC runtime-state catalog in the runtime surface through `/engine/cdc-captures/runtime*` and `snapshot.CdcCaptureStates`
- shared `data.cdc.execution` capability plus the `data-cdc-capture-flow` execution graph and `data-cdc-capture-pump` hosted execution through `/engine/execution-graphs`, `/engine/hosted-executions`, `/engine/runtime-story`, and `snapshot.OperationalStory`
- build-time validation that rejects missing source-module ownership or missing outbox references before broken operator metadata can ship

Later follow-through:
- provider-specific capture implementations over the shipped `ICdcCapture` / `CdcCaptureExecutionResult` contract, such as PostgreSQL WAL or MongoDB change streams
- richer provider-native execution semantics and deeper source-specific posture beyond the shipped typed freshness/lag/publication baseline
- out-of-process or edge-aware CDC execution ownership only when a project truly needs topology beyond the shipped shared in-process host loop

Effort: medium for the remaining provider-specific follow-through.

## Planned phases

### Phase 11 — Resilience Foundation

Target: Sprint 36–37

Deliverables:
- Onion Architecture pattern descriptor — shipped
- Circuit Breaker behavior middleware and runtime catalog — shipped
- Retry/Timeout/Bulkhead resilience policies — shipped through the shared behavior pipeline, including idempotency-gated retry execution
- Rate Limiting middleware integration — shipped
- Anti-Corruption Layer pattern descriptor — shipped
- shared `Cephalon.Behaviors` resilience extension baseline

Exit criteria:
- a consumer app can configure per-behavior circuit breaker, retry, timeout, and rate-limit policies through `Engine:Resilience` without writing custom middleware
- health probes and circuit breakers compose together to prevent cascading failures

### Phase 12 — Migration and Advanced Coordination

Target: Sprint 38–39

Deliverables:
- Strangler Fig migration pattern
- Saga Choreography execution strategy
- Backend for Frontend explicit pattern
- Feature flags runtime baseline — shipped through `FeatureFlagDescriptor`,
  `FeatureFlagTargetingDescriptor`, `IFeatureToggle`, `IFeatureFlagRuntimeCatalog`,
  `IFeatureFlagContributor`, `Engine:Features`, `/engine/features`,
  `/engine/features/{featureFlagId}/evaluate`, and `snapshot.FeatureFlags` while provider
  integration and any future capability publication remain later follow-through
- Durable Execution foundations — shipped through `IBehaviorTopologyBuilder.AsDurableExecution()`,
  source-generated `durable-execution` literals, `IDurableExecution<TState>`,
  `IDurableExecution<TInput, TState, TOutput>`, `DurableExecutionState<TState>`,
  `DurableExecutionStepResult<TOutput>`, `DurableExecutionStrategy`,
  `behaviors.durable-execution`, and `ABT-006`; the first durable runtime/operator follow-through
  is now also shipped through `DurableExecutionRuntimeDescriptor`,
  `IDurableExecutionRuntimeCatalog`, `/engine/durable-executions`, and
  `snapshot.DurableExecutions`, while replay-progress/failure-posture and higher-level workflow
  helpers remain later

Exit criteria:
- a consumer app can migrate incrementally from a legacy system using the strangler fig router
- sagas can coordinate through events (choreography) in addition to state (orchestration)
- feature flags can gate behavior, module, transport, environment, tenant, subject, or tag-scoped
  availability through `IFeatureToggle` and `Engine:Features`
- durable execution workflows can replay and append through `IEventStore` across process restarts
  without a transport-specific workflow runner

### Phase 13 — Next-Generation Patterns

Target: Sprint 40–41

Deliverables:
- Cell-Based Architecture technology descriptor and boundary abstraction — shipped baseline
- Data Mesh data product abstraction — shipped baseline
- CDC capture abstraction plus typed runtime-state/freshness/lag/publication follow-through — shipped baseline
- provider-aware and edge-aware cell traffic automation plus the first provider-managed and edge-runtime materialization contracts over the shared automation catalog — shipped baseline

Exit criteria:
- modules can declare cell boundaries, governed cell routes, and cell health-isolation posture with explicit blast-radius isolation, configuration can overlay deterministic traffic automation including provider and edge targeting, provider-managed and edge-managed automation can publish selected materializer plus latest reconciliation posture on that same catalog, and operators can inspect the same answers through `/engine/cells`, `/engine/cell-routes`, `/engine/cell-health-isolations`, `/engine/cell-traffic-automations`, `/engine/technology-surfaces/cell-based-architecture`, and `/engine/snapshot`
- modules can expose queryable data products through the runtime catalog
- modules can declare CDC captures linked to an outbox through the runtime catalog today, operators can inspect latest capture/freshness/lag/post-publication posture through `/engine/cdc-captures`, `/engine/cdc-captures/runtime`, and `/engine/snapshot`, and later provider-specific execution can capture and publish those changes without inventing a second registry
