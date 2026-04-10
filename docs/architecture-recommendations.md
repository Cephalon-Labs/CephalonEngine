# Cephalon Engine Architecture Recommendations

Recommendations in this document reflect the repository state as of `April 11, 2026`.

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

Current state: the contract-first baseline is now shipped through `Engine:Resilience:CircuitBreaker`, `AppProfile.Resilience`, and `/engine/resilience`, but no runtime circuit-breaker state machine or behavior-pipeline enforcement exists yet. Health checks still tell you something is down; the engine does not yet stop calling it.

Recommendation: integrate `Microsoft.Extensions.Resilience` (Polly v8) or build a lightweight `ICircuitBreakerPolicy` abstraction.

Implementation outline:
- `Cephalon.Abstractions/Resilience/ICircuitBreaker.cs` — interface with open/half-open/closed semantics
- `Cephalon.Behaviors/Resilience/CircuitBreakerBehaviorMiddleware.cs` — behavior pipeline middleware
- `PatternDescriptor` "circuit-breaker" in `BuiltInPatterns.cs`
- Configuration: `Engine:Resilience:CircuitBreaker` section
- Integrates with existing `IDependencyHealthContributor`
- Capability: `resilience.circuit-breaker`

Effort: medium.

### Retry with Backoff, Timeout, and Bulkhead (resilience suite)

Current state: the contract-first baseline is now shipped through `Engine:Resilience` with `Retry`, `Timeout`, and `Bulkhead` selections plus operator-facing introspection, but no runtime enforcement or behavior-level override path exists yet.

Recommendation: create `Cephalon.Resilience` companion package or add resilience middleware to `Cephalon.Behaviors`.

Implementation outline:
- `IResiliencePolicy` abstraction covering retry, timeout, and bulkhead
- Integration with `Microsoft.Extensions.Resilience` (Polly v8) as the default implementation
- Per-behavior resilience configuration through `Engine:Resilience` or behavior-level metadata
- Capabilities: `resilience.retry`, `resilience.timeout`, `resilience.bulkhead`

Effort: medium.

### Rate Limiting (API protection)

Current state: the contract-first baseline is now shipped through `Engine:Resilience:RateLimiting`,
`AppProfile.Resilience`, and `/engine/resilience`, and the first transport-native follow-through is
also shipped through ASP.NET Core middleware, `/engine/rate-limiting`, and
`snapshot.RateLimitingPolicies`. The current limiter covers public HTTP endpoints, intentionally
excludes operator/docs routes, and now supports endpoint-scoped override modeling through
`Engine:Resilience:RateLimiting:Overrides` with behavior-aware precedence across module-owned REST
routes and generic behavior HTTP bindings.

Recommendation: keep ASP.NET Core middleware as the truthful baseline for public HTTP protection, and
add finer per-behavior or transport-native override models on top of that surface instead of jumping
straight to a generic resilience runtime catalog or a behavior pipeline that does not exist yet.

Remaining follow-through:
- Transport-native semantics for long-lived connections and non-route HTTP surfaces beyond the initial request gate
- Behavior-pipeline resilience coordination when retry/timeout/bulkhead middleware exists
- Capability: `resilience.rate-limiting`

Effort: medium for the remaining non-baseline work.

## Priority 2 — Strategic value

These patterns provide adoption acceleration and complete the distributed systems story.

### Strangler Fig (migration support)

Current state: no explicit migration pattern exists. Many enterprises adopt CephalonEngine incrementally from legacy systems.

Recommendation: provide explicit routing rules for gradual migration from old systems to Cephalon behaviors.

Implementation outline:
- `PatternDescriptor` "strangler-fig" in `BuiltInPatterns.cs`
- `IStranglerFigRouter` — routes requests between legacy and new system
- Migration progress tracking via runtime surface
- Configuration: `Engine:Migration:StranglerFig` section
- Capability: `migration.strangler-fig`

Effort: medium.

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

Current state: multi-transport support already enables BFF implicitly (REST for web, gRPC for mobile, GraphQL for rich clients). Making it an explicit pattern with dedicated configuration would add clarity.

Recommendation: register as a pattern descriptor with per-client transport binding configuration.

Implementation outline:
- `PatternDescriptor` "backend-for-frontend" in `BuiltInPatterns.cs` with aliases `["BackendForFrontend", "BFF"]`
- Per-client transport binding configuration
- Client-aware behavior filtering

Effort: small.

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
- Circuit Breaker abstraction and behavior middleware
- Retry/Timeout/Bulkhead resilience policies
- Rate Limiting middleware integration — shipped
- Anti-Corruption Layer pattern descriptor — shipped
- `Cephalon.Resilience` companion package or `Cephalon.Behaviors` resilience extension

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
