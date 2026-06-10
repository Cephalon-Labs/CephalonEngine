# Cephalon.AspNetCore.Grpc

> **Maturity:** `M2` · **Ownership:** cephalon-managed · **Family:** `transport-adapter` · See [audit](../engine-surface-maturity-audit.md), [matrix](../conformance-matrix.md).

`Cephalon.AspNetCore.Grpc` adds gRPC transport support to the ASP.NET Core host.

## What it owns

- gRPC service registration
- gRPC module contract for host modules
- gRPC route mapping
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

## Related docs

- [Architecture](../architecture.md)
- [App models](../app-models.md)
