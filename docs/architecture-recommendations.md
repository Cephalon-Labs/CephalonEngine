# Cephalon Engine Architecture Recommendations

Recommendations in this document reflect the repository state as of `April 13, 2026`.

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

Current state: the contract-first runtime baseline plus the first configuration-driven policy overlay are now shipped. `BuiltInPatterns.cs` still carries the `strangler-fig` descriptor, and `Cephalon.Abstractions` now also exports `IStranglerFigRouteContributor`, `IStranglerFigRouteRegistry`, `IStranglerFigRuntimeCatalog`, `IStranglerFigMigrationRuntimeCatalog`, `IStranglerFigRouter`, `StranglerFigMigrationRuntimeDescriptor`, `StranglerFigRequest`, `StranglerFigRouteDescriptor`, `StranglerFigRouteResolution`, and `StranglerFigTarget`. `Cephalon.Engine` now composes those routes into the runtime catalog and snapshot, applies `Engine:Migration:StranglerFig` defaults and per-route policy overlays deterministically, and projects effective migration progress plus target-selection answers through `snapshot.StranglerFigRoutePolicies`. ASP.NET Core now exposes `/engine/strangler-fig`, `/engine/strangler-fig/runtime`, and `/engine/strangler-fig/resolve` as operator-facing surfaces. Remaining work is concrete host-level cutover or proxy behavior.

Recommendation: keep the host-agnostic route-contribution, runtime-catalog, migration-policy catalog, and request-resolution contracts stable, then add host-specific proxy or traffic-manager follow-through only when a concrete host needs real cutover behavior.

Implementation outline:
- `PatternDescriptor` "strangler-fig" in `BuiltInPatterns.cs`
- `IStranglerFigRouteContributor` + `IStranglerFigRouteRegistry` — let modules contribute migration boundaries explicitly
- `IStranglerFigRuntimeCatalog` + `IStranglerFigMigrationRuntimeCatalog` + `IStranglerFigRouter` — publish the authored route catalog, effective migration-policy answers, and request ownership without leaking host APIs into abstractions
- `/engine/strangler-fig`, `/engine/strangler-fig/runtime`, `/engine/strangler-fig/resolve`, `snapshot.StranglerFigRoutes`, and `snapshot.StranglerFigRoutePolicies` — operator-facing runtime surface for the shipped baseline
- Configuration: `Engine:Migration:StranglerFig` section with default target/progress overlays plus per-route overrides
- Capability: `migration.strangler-fig`

Effort: small-to-medium for the remaining host cutover and traffic-manager follow-through.

### Anti-Corruption Layer (DDD integration boundary)

Current state: shipped as a taxonomy descriptor. `BuiltInPatterns.cs` now includes `anti-corruption-layer`, so the remaining work is guidance and interface conventions rather than descriptor registration.

Recommendation: keep the descriptor stable and add translator conventions only when a concrete integration slice needs them.

Implementation outline:
- `PatternDescriptor` "anti-corruption-layer" in `BuiltInPatterns.cs`
- `IAntiCorruptionTranslator<TExternal, TInternal>` interface convention
- Guidance in pattern descriptor for module boundary translation

Effort: small.

### Saga Choreography (event-driven saga variant)

Current state: saga implementation is orchestration-based only (`SagaExecutionStrategy` with state store). Choreography-based sagas where each service reacts to events are equally important for loosely-coupled systems.

Recommendation: add a choreography saga execution strategy that leverages existing eventing infrastructure.

Implementation outline:
- `ChoreographySagaExecutionStrategy` — event-reaction-based coordination
- `ISagaEventReactor<TEvent>` interface
- Compensation event publishing
- Works with existing outbox for reliable event publication
- Capability: `behaviors.saga-choreography`

Effort: medium.

### Backend for Frontend — BFF (explicit pattern)

Current state: shipped as a taxonomy descriptor. `BuiltInPatterns.cs` now includes `backend-for-frontend`, so the remaining work is explicit client-binding configuration and filtering guidance rather than descriptor registration.

Recommendation: keep the descriptor stable and add per-client transport binding plus behavior-filtering follow-through only when a concrete frontend surface needs it.

Implementation outline:
- `PatternDescriptor` "backend-for-frontend" in `BuiltInPatterns.cs` with aliases `["BackendForFrontend", "BFF"]`
- Per-client transport binding configuration
- Client-aware behavior filtering

Effort: small for the remaining non-descriptor work.

### Feature Flags (progressive delivery)

Current state: no feature flag infrastructure exists. Essential for trunk-based development and progressive rollout.

Recommendation: add `IFeatureToggle` abstraction with per-behavior, per-module, and per-tenant evaluation.

Implementation outline:
- `IFeatureToggle` abstraction in `Cephalon.Abstractions`
- In-memory default implementation
- Integration points for external providers (LaunchDarkly, Azure App Configuration, Unleash)
- Per-behavior and per-module feature evaluation middleware
- Configuration: `Engine:Features` section
- Capability: `runtime.feature-flags`

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

Current state: Process Manager is close but lacks replay semantics and automatic failure recovery. Durable execution provides long-running workflows that survive process restarts.

Recommendation: add durable execution infrastructure on top of the existing process-manager and event-sourcing foundations.

Implementation outline:
- `IDurableExecution<TState>` — workflow definition with replay semantics
- Execution journal for deterministic replay
- Integration with existing `IEventStore` for persistence
- Capability: `behaviors.durable-execution`

Effort: large.

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

Target: Sprint 36–37

Deliverables:
- Strangler Fig migration pattern
- Saga Choreography execution strategy
- Backend for Frontend explicit pattern
- Feature Flags abstraction and middleware
- Durable Execution foundations

Exit criteria:
- a consumer app can migrate incrementally from a legacy system using the strangler fig router
- sagas can coordinate through events (choreography) in addition to state (orchestration)
- feature flags can gate behavior availability per tenant

### Phase 13 — Next-Generation Patterns

Target: Sprint 38–39

Deliverables:
- Cell-Based Architecture technology descriptor and boundary abstraction
- Data Mesh data product abstraction
- CDC capture abstraction

Exit criteria:
- modules can declare cell boundaries with explicit blast-radius isolation
- modules can expose queryable data products through the runtime catalog
- database changes can be captured and published through the outbox without explicit staging
