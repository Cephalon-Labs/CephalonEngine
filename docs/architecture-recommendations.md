# Cephalon Engine Architecture Recommendations

Recommendations in this document reflect the repository state as of `April 19, 2026`.

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

Current state: the contract-first runtime baseline, the first configuration-driven policy overlay, and the first ASP.NET Core host-level cutover baseline are now shipped. `BuiltInPatterns.cs` still carries the `strangler-fig` descriptor, and `Cephalon.Abstractions` now also exports `IStranglerFigRouteContributor`, `IStranglerFigRouteRegistry`, `IStranglerFigRuntimeCatalog`, `IStranglerFigMigrationRuntimeCatalog`, `IStranglerFigRouter`, `StranglerFigMigrationRuntimeDescriptor`, `StranglerFigRequest`, `StranglerFigRouteDescriptor`, `StranglerFigRouteResolution`, and `StranglerFigTarget`. `Cephalon.Engine` now composes those routes into the runtime catalog and snapshot, applies `Engine:Migration:StranglerFig` defaults and per-route policy overlays deterministically, and projects effective migration progress plus target-selection answers through `snapshot.StranglerFigRoutePolicies`. ASP.NET Core now exposes `/engine/strangler-fig`, `/engine/strangler-fig/runtime`, `/engine/strangler-fig/resolve`, `/engine/strangler-fig/cutover`, and `/engine/strangler-fig/cutover/resolve` as operator-facing surfaces. When `Engine:Migration:StranglerFig:AspNetCore` is enabled, rooted local selected endpoints rewrite in-process, absolute HTTP or HTTPS selected endpoints can redirect or proxy, and unsupported selected endpoints fail truthfully with `502` without introducing a second routing truth outside the shared migration catalogs. Remaining work is broader traffic-manager or ingress follow-through plus BFF client-binding, not the first host cutover runtime.

Recommendation: keep the host-agnostic route-contribution, runtime-catalog, migration-policy catalog, and request-resolution contracts stable, keep host-level cutover derived from those shared catalogs, and add broader traffic-manager or ingress follow-through only when a concrete edge or host needs it.

Implementation outline:
- `PatternDescriptor` "strangler-fig" in `BuiltInPatterns.cs`
- `IStranglerFigRouteContributor` + `IStranglerFigRouteRegistry` — let modules contribute migration boundaries explicitly
- `IStranglerFigRuntimeCatalog` + `IStranglerFigMigrationRuntimeCatalog` + `IStranglerFigRouter` — publish the authored route catalog, effective migration-policy answers, and request ownership without leaking host APIs into abstractions
- `/engine/strangler-fig`, `/engine/strangler-fig/runtime`, `/engine/strangler-fig/resolve`, `/engine/strangler-fig/cutover`, `/engine/strangler-fig/cutover/resolve`, `snapshot.StranglerFigRoutes`, and `snapshot.StranglerFigRoutePolicies` — operator-facing runtime surface for the shipped baseline
- Configuration: `Engine:Migration:StranglerFig` for default target/progress overlays plus per-route overrides, and `Engine:Migration:StranglerFig:AspNetCore` for host cutover handling
- Capability: `migration.strangler-fig`

Effort: small-to-medium for the remaining traffic-manager, ingress, and BFF follow-through.

### Anti-Corruption Layer (DDD integration boundary)

Current state: shipped as a taxonomy descriptor. `BuiltInPatterns.cs` now includes `anti-corruption-layer`, so the remaining work is guidance and interface conventions rather than descriptor registration.

Recommendation: keep the descriptor stable and add translator conventions only when a concrete integration slice needs them.

Implementation outline:
- `PatternDescriptor` "anti-corruption-layer" in `BuiltInPatterns.cs`
- `IAntiCorruptionTranslator<TExternal, TInternal>` interface convention
- Guidance in pattern descriptor for module boundary translation

Effort: small.

### Saga Choreography (event-driven saga variant)

Current state: the first host-agnostic choreography baseline is now shipped. `Cephalon.Behaviors.Patterns`
now exposes `ChoreographySagaExecutionStrategy`, `ISagaChoreographyPublisher`,
`SagaChoreographyPublication`, `SagaChoreographyStepResult`, and an in-memory default publisher so
choreography steps can publish continuation or compensation work without forcing a hard dependency
on `Cephalon.Eventing`.

Recommendation: keep the host-agnostic choreography contracts stable, then add a dedicated
`Cephalon.Eventing` bridge that maps `ISagaChoreographyPublisher` onto the shared outbox-backed
eventing runtime instead of collapsing the behavior-pattern layer into a technology-pack
dependency.

Implementation outline:
- shipped baseline: `ChoreographySagaExecutionStrategy`, `ISagaChoreographyPublisher`,
  `SagaChoreographyPublication`, `SagaChoreographyStepResult`, `InMemorySagaChoreographyPublisher`,
  and capability `behaviors.saga-choreography`
- next follow-through: `Cephalon.Eventing` bridge that stages choreography publications through the
  same outbox-backed publication runtime used by the eventing technology pack
- optional later follow-through: higher-level authoring helpers such as `ISagaEventReactor<TEvent>`
  when a concrete module-authoring workflow benefits from them

Effort: medium.

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

Current state: no cell boundary concept exists. CephalonEngine's module system is already cell-shaped, but without explicit blast-radius isolation and cell-to-cell routing.

Recommendation: add technology descriptor and cell boundary abstraction for Netflix/Meta-scale deployments.

Implementation outline:
- `TechnologyDescriptor` "cell-based-architecture"
- `ICellBoundary` — defines a cell's blast radius
- Cell routing table and cell health isolation
- Module-to-cell mapping through configuration

Effort: large.

### Data Mesh (domain-owned data products)

Current state: the module system and polyglot persistence are a natural fit for data mesh, but there is no explicit data product concept.

Recommendation: add `IDataProduct<T>` abstraction where each module owns its queryable data product.

Implementation outline:
- `IDataProduct<T>` — module-owned, queryable data product
- Data product catalog in runtime surface
- Self-serve data infrastructure via module capabilities
- Capability: `data.data-product`

Effort: medium.

### Durable Execution (Temporal/Restate style)

Current state: the first durable-execution baseline is now shipped. `IBehaviorTopologyBuilder.AsDurableExecution()` plus source-generated `durable-execution` literals let behaviors opt into replay explicitly, and `Cephalon.Behaviors.Patterns` now exports `IDurableExecution<TState>`, `IDurableExecution<TInput, TState, TOutput>`, `DurableExecutionState<TState>`, `DurableExecutionStepResult<TOutput>`, and `DurableExecutionStrategy`. The shared strategy replays state from `IEventStore`, validates sequential stream versions before append, and returns truthful `200`, `202`, or `204` outcomes based on local output versus continuation-only work. `Cephalon.Behaviors` now also exposes capability `behaviors.durable-execution` plus rule `ABT-006`, and the Kafka, RabbitMQ, and test behavior contexts can now flow `IEventStore` into the shared pipeline for non-default-host execution.

Recommendation: keep the replay contract host-agnostic and `IEventStore`-backed, keep durable authoring explicit through `IDurableExecution` instead of hiding deterministic replay requirements behind generic behavior interfaces, and add richer operator/runtime surfaces only when they can stay derived from that same shared replay truth.

Implementation outline:
- shipped baseline: `IBehaviorTopologyBuilder.AsDurableExecution()`, source-generated `durable-execution` literals, `IDurableExecution<TState>`, `IDurableExecution<TInput, TState, TOutput>`, `DurableExecutionState<TState>`, `DurableExecutionStepResult<TOutput>`, `DurableExecutionStrategy`, capability `behaviors.durable-execution`, and compatibility rule `ABT-006`
- next follow-through: operator-facing runtime/catalog answers for active durable executions, replay progress, or failure posture when a concrete host/operator workflow needs them
- later follow-through: higher-level timers, signals, or compensation helpers only when they can remain additive over the shared replay contract instead of becoming a second workflow engine hidden inside `Cephalon.Behaviors`

Effort: medium for the remaining follow-through.

### Change Data Capture — CDC (event-driven data sync)

Current state: outbox pattern handles explicit event staging, but CDC captures all database changes automatically.

Recommendation: add CDC abstraction for cross-service data synchronization and legacy integration.

Implementation outline:
- `ICdcCapture` interface
- Debezium-compatible event format
- Integration with outbox for reliable publication
- Provider-specific implementations (PostgreSQL WAL, MongoDB change streams)
- Capability: `data.cdc`

Effort: large.

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
  `behaviors.durable-execution`, and `ABT-006` while richer operator/runtime surfaces remain later

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
- Cell-Based Architecture technology descriptor and boundary abstraction
- Data Mesh data product abstraction
- CDC capture abstraction

Exit criteria:
- modules can declare cell boundaries with explicit blast-radius isolation
- modules can expose queryable data products through the runtime catalog
- database changes can be captured and published through the outbox without explicit staging
