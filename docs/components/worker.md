# Cephalon.Worker

`Cephalon.Worker` hosts the same Cephalon runtime inside the generic host without HTTP.

See also: [Engine surface maturity audit](../engine-surface-maturity-audit.md), [Conformance matrix](../conformance-matrix.md), and [Runtime contract index](../runtime-contract-index.md) for the per-package adoption-truth, maturity, ownership, and `I*Catalog` interface inventory that the worker host adapter projects against. [Engineering standards](../engineering-standards.md) records the library-design and code-quality baseline; [Long-range engine direction](../long-range-direction.md) frames why worker hosting stays a thin generic-host adapter over the same host-agnostic engine that `Cephalon.AspNetCore` adapts for HTTP, instead of forking into a worker-only runtime.

## What it owns

- worker-specific service registration
- project-level split-configuration loading through `AddCephalonProjectConfigurations()` and `AddCephalon(...)`
- lifecycle bridging from `IHost` into `IRuntime`
- background-host startup and shutdown coordination

## Main surfaces

- `Hosting/WorkerHostApplicationBuilderExtensions.cs`
- `Hosting/WorkerServiceCollectionExtensions.cs`
- `Hosting/RuntimeHostedService.cs`

## Source structure

- `Hosting`

## How it fits

This package proves that Cephalon is not tied to ASP.NET Core. Modules and runtime policy stay the same, while the hosting surface changes to fit background services, queue consumers, or agent workers.

It also owns the generic-host side of Cephalon's split-configuration convention so worker apps can group settings under `Configurations/Add*.json` and `Configurations/{group}/{Environment}.json` instead of a single large JSON file.

## Related docs

- [Architecture](../architecture.md)
- [Engine surface maturity audit](../engine-surface-maturity-audit.md)
- [Conformance matrix](../conformance-matrix.md)
- [Runtime contract index](../runtime-contract-index.md)
- [Long-range engine direction](../long-range-direction.md)
- [Engineering standards](../engineering-standards.md)
- [Architecture review (May 2026)](../architecture-review-2026-05.md)
- [Operations](../operations.md)
