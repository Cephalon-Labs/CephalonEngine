# Cephalon.AspNetCore.Grpc

> **Maturity:** `M2` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.AspNetCore.Grpc` adds gRPC transport support to the ASP.NET Core host.

## What it owns

- gRPC service registration
- gRPC module contract for host modules
- gRPC route mapping
- Cephalon endpoint rate-limiting participation for the public gRPC route group
- host-enforced direct-module timeout, circuit-breaker, and bulkhead handling from `Engine:Resilience`
- gRPC-native resilience fault envelopes for direct module timeouts, open-circuit rejections, and bulkhead rejections
- `grpc-direct-module-resilience` runtime truth through `/engine/technology-surfaces`
- shared proto contracts used by the sample/runtime surface

## Main surfaces

- `Hosting/GrpcTransportServiceCollectionExtensions.cs`
- `Hosting/CephalonGrpcDirectModuleResilienceOptions.cs`
- `Hosting/CephalonGrpcDirectModuleCircuitBreakerState.cs`
- `Hosting/CephalonGrpcDirectModuleBulkheadState.cs`
- `Hosting/CephalonGrpcDirectModuleResilienceRuntimeContributor.cs`
- `Modules/IGrpcModule.cs`
- `Routing/GrpcTransportRouteMapper.cs`
- `Protos/discovery.proto`

## Source structure

- `Hosting`
- `Modules`
- `Protos`
- `Routing`

## How it fits

Use this package when a host selects the `Grpc` transport. Unary, server-streaming, and duplex-streaming support all live behind this adapter package rather than the engine core.

When `Engine:Resilience:RateLimiting:Enabled=true`, the adapter applies the effective Cephalon ASP.NET Core endpoint policy to the configured gRPC prefix. `/engine/rate-limiting` reports the covered `grpc` transport id with request-response semantics, and rejected gRPC calls receive a gRPC-native `ResourceExhausted` status instead of a JSON ProblemDetails or result-envelope payload. Operator and documentation routes remain excluded by the shared ASP.NET Core host policy.

The adapter also registers a gRPC interceptor that applies the configured direct-module `Engine:Resilience` timeout, circuit-breaker, and bulkhead policy without requiring Wolverine, Polly package references, or consumer-owned interceptor code. When `Engine:Resilience:Timeout` is enabled, direct `IGrpcModule` handlers are bounded by the resolved execution timeout and rejected calls become `DeadlineExceeded`. When `Engine:Resilience:CircuitBreaker` is enabled, the adapter tracks process-local transient failures for the direct gRPC endpoint group, rejects open-circuit calls as `Unavailable`, and publishes the live breaker posture through `/engine/technology-surfaces` as `grpc-direct-module-resilience`. When `Engine:Resilience:Bulkhead` is enabled, the adapter gates direct gRPC module calls with a bounded process-local concurrency limiter, honors `MaxConcurrentExecutions`, honors `MaxQueuedActions` only as an explicit bounded queue, and otherwise rejects at request entry as `ResourceExhausted` for predictable hot-path latency.

Direct module resilience faults stay transport-native. Host-enforced timeouts plus `TimeoutException` and optional Polly `TimeoutRejectedException` faults become `DeadlineExceeded`, host-enforced open-circuit rejections plus optional Polly `BrokenCircuitException` faults become `Unavailable`, and full bulkheads become `ResourceExhausted`; all three carry stable Cephalon metadata trailers so callers and operators can distinguish resilience faults from arbitrary handler failures. Unmapped exceptions still flow through ASP.NET Core gRPC's normal `Unknown` behavior.

## Related docs

- [Architecture](../architecture.md)
- [App models](../app-models.md)
