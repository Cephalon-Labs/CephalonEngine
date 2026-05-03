# Cephalon.Worker

> **Maturity:** `M4` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Worker` hosts the same Cephalon runtime inside the generic host without HTTP.

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
- [Operations](../operations.md)
