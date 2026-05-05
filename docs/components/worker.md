# Cephalon.Worker

> **Maturity:** `M4` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Worker` hosts the same Cephalon runtime inside the generic host without HTTP.

## What it owns

- worker-specific service registration
- project-level split-configuration loading through `AddCephalonProjectConfigurations()` and `AddCephalon(...)`
- lifecycle bridging from `IHost` into `IRuntime`
- background-host startup and shutdown coordination
- the internal `WorkerDiagnostics` activity-source declared against `CephalonActivitySources.Worker` and `CephalonMeters.Worker`; `RuntimeHostedService` emits one `worker.lifecycle.start` activity per `IHostedService.StartAsync` call and one `worker.lifecycle.stop` activity per `IHostedService.StopAsync` call with stable Cephalon-prefix tags (`cephalon.lifecycle.phase` carrying `start` / `stop`, `cephalon.blueprint` carrying the canonical kebab-case blueprint id, `cephalon.module.count` carrying the engaged module count), all routed through the `RedactionPipeline` resolved from DI so consumer-registered redaction filters scrub lifecycle-emission attributes uniformly with the AspNetCore middleware, engine-runtime emission, Cephalon.Agentics dispatcher emission, and Cephalon.Retrieval indexer / query emission sites; this is the sixth M1 redaction emission site shipped through `ENG-412` and integration-tested through `ENG-413`

## Main surfaces

- `Hosting/WorkerHostApplicationBuilderExtensions.cs`
- `Hosting/WorkerServiceCollectionExtensions.cs`
- `Hosting/RuntimeHostedService.cs`
- `Hosting/WorkerDiagnostics.cs`

## Source structure

- `Hosting`

## How it fits

This package proves that Cephalon is not tied to ASP.NET Core. Modules and runtime policy stay the same, while the hosting surface changes to fit background services, queue consumers, or agent workers.

It also owns the generic-host side of Cephalon's split-configuration convention so worker apps can group settings under `Configurations/Add*.json` and `Configurations/{group}/{Environment}.json` instead of a single large JSON file.

## Related docs

- [Architecture](../architecture.md)
- [Operations](../operations.md)
