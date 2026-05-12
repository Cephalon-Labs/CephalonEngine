# Cephalon.Resilience

> **Maturity:** `M2` · **Ownership:** mixed: `application-managed` descriptors + `cephalon-managed` runtime — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Resilience` is the engine-managed resilience runtime companion pack for Cephalon.

## What it owns

- declarative resilience policy resolution from `ResilienceSettings` and `BehaviorExecutionResilienceOverrideSelection` descriptors into a strongly-typed policy catalog
- per-policy `Microsoft.Extensions.Resilience` (Polly v8) pipeline configuration covering retry, total/attempt timeout, circuit breaker, bulkhead, and rate limiting
- bounded process-local circuit-breaker state registry that captures opened/closed/half-opened transitions, last break duration, and last opened exception type for runtime introspection
- the default behavior resilience exception classifier that distinguishes transient transport/database/IO faults from non-retryable validation, security, and contract faults
- the shared resilience pipeline `ResiliencePropertyKey<T>` set used to flow behavior id, transport id, and behavior idempotency through `ResilienceContext`

## Main surfaces

- `Resilience/BehaviorResiliencePolicyResolver.cs` — turns `ResilienceSettings` plus per-behavior overrides into a `BehaviorResiliencePolicyCatalog`
- `Resilience/BehaviorCircuitBreakerStateRegistry.cs` — bounded process-local circuit-breaker runtime state per policy id
- `Resilience/DefaultBehaviorResilienceExceptionClassifier.cs` — default `IBehaviorResilienceExceptionClassifier` implementation
- `Resilience/BehaviorResilienceExecutionContextKeys.cs` — shared `ResiliencePropertyKey` definitions

## Key contracts (from `Cephalon.Abstractions.Resilience`)

| Type | Description |
|------|-------------|
| `IBehaviorResilienceExceptionClassifier` | Author-extension point for classifying exceptions as `Ignore` / `RetryAndTrip` / `TripOnly` |
| `IBehaviorResilienceRuntimeCatalog` | Read surface — enumerate resolved resilience policies, including effective Polly strategy values |
| `BehaviorResilienceRuntimeDescriptor` | Resolved per-policy runtime descriptor exposed through the runtime catalog |
| `BehaviorResilienceExceptionContext` | Per-classification context handed to `IBehaviorResilienceExceptionClassifier` |
| `BehaviorResilienceExceptionHandling` | Policy outcome enum: `Ignore`, `RetryAndTrip`, `TripOnly` |
| `BehaviorExecutionResilienceSelection` | Aggregated retry/timeout/circuit-breaker/bulkhead/rate-limit selection record |

## How it fits

This pack stays intentionally narrow. It owns the engine-managed resilience runtime that consumes the abstractions in `Cephalon.Abstractions.Resilience` plus the application-managed descriptors in `Cephalon.Abstractions.AppModel` (`ResilienceSettings`, `RetrySelection`, `TimeoutSelection`, `CircuitBreakerSelection`, `BulkheadSelection`, `RateLimitingSelection`, `BehaviorExecutionResilienceOverrideSelection`). It does not re-implement anything that `Microsoft.Extensions.Resilience` already covers — Polly v8 stays in-box.

Companion packs that need to enforce resilience inside their own dispatch pipeline can take a dependency on `Cephalon.Resilience` and reuse its policy catalog, circuit-breaker state registry, exception classifier, and execution-context keys without taking the full `Cephalon.Behaviors` runtime. `Cephalon.Behaviors` itself depends on this package and ships the behavior-coupled `BehaviorResilienceExecutionMiddleware` plus the `BehaviorIdempotencyResolver` and `BehaviorResilienceRuntimeCatalog` glue, since those types implement and consume the internal `Cephalon.Behaviors` middleware contract and behavior type registry respectively.

ASP.NET Core endpoint-level rate limiting is owned by `Cephalon.AspNetCore`, not this package. That host adapter projects the requested `RateLimiting` selection into `/engine/rate-limiting`, applies the policy to selected public HTTP transports, and keeps transport-native rejection semantics where the adapter owns them. The gRPC adapter now participates in that same endpoint policy and returns `ResourceExhausted` for rejected gRPC calls. It also enforces configured direct-module `Timeout`, `CircuitBreaker`, and `Bulkhead` policy from `Engine:Resilience` through its own interceptor, translates timeout/open-circuit/bulkhead outcomes into `DeadlineExceeded` / `Unavailable` / `ResourceExhausted` gRPC statuses with Cephalon metadata trailers, publishes `grpc-direct-module-resilience` runtime truth through `/engine/technology-surfaces` without requiring Wolverine or consumer interceptor code, and surfaces cumulative timeout-occurrence, circuit-open transition, and circuit-open rejection outcome counters there so operators can answer fault-rate questions without scraping handler exception logs. Behavior-dispatch resilience remains the cross-transport execution layer for behavior-owned retry, timeout, circuit-breaker, bulkhead, and rate-limiting semantics.
Behavior-dispatch bulkhead saturation remains owned by `Cephalon.Behaviors` plus the relevant
transport adapters; the generic behavior HTTP bindings surface it as protocol-native `429`
envelopes with `behavior_execution_rejected`.

The engine-level resilience runtime is tracked at `M2 — mixed application-managed descriptors plus cephalon-managed runtime` in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md). Promotion to `M3` is gated on an explicit operator surface (catalog routes, snapshot keys) landing inside this package.

## Maturity and ownership

- Maturity: `M2`
- Ownership: mixed — `application-managed` resilience descriptors (selected at app-profile level) plus `cephalon-managed` execution runtime over the in-box `Microsoft.Extensions.Resilience` integration

## Related docs

- [Cephalon.Abstractions](abstractions.md)
- [Cephalon.Behaviors](behaviors.md)
- [Engine surface maturity audit](../engine-surface-maturity-audit.md)
