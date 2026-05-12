# Cephalon.AspNetCore.Grpc

> **Maturity:** `M2` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.AspNetCore.Grpc` adds gRPC transport support to the ASP.NET Core host.

## What it owns

- gRPC service registration
- gRPC module contract for host modules
- gRPC route mapping
- Cephalon endpoint rate-limiting participation for the public gRPC route group
- gRPC-native resilience fault translation for direct module timeouts and open-circuit rejections
- shared proto contracts used by the sample/runtime surface

## Main surfaces

- `Hosting/GrpcTransportServiceCollectionExtensions.cs`
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

The adapter also registers a gRPC interceptor that keeps direct module resilience faults transport-native. `TimeoutException` and optional Polly `TimeoutRejectedException` faults become `DeadlineExceeded`, while optional Polly `BrokenCircuitException` faults become `Unavailable`; both carry stable Cephalon metadata trailers so callers and operators can distinguish resilience faults from arbitrary handler failures. Unmapped exceptions still flow through ASP.NET Core gRPC's normal `Unknown` behavior.

## Related docs

- [Architecture](../architecture.md)
- [App models](../app-models.md)
