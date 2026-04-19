# Cephalon.Data

`Cephalon.Data` is the runtime-neutral data execution companion package for Cephalon.

## What it owns

- registers default `IReadStore` and `IWriteStore` implementations backed by the existing command/query handler abstractions
- keeps command/query dispatching out of hosts so ASP.NET Core and worker apps stay thin
- provides the first reusable bridge between phase-8 data contracts and future provider-specific packs such as `Cephalon.Data.EntityFramework`
- provides the first reusable CDC runtime-state reporting/catalog bridge over `ICdcCaptureCatalog` plus optional linked outbox dispatch truth

## Main surfaces

- `Configuration/DataRuntimeOptions.cs`
- `Registration/DataEngineBuilderExtensions.cs`
- `Services/CdcCaptureExecutionReport.cs`
- `Services/CdcCaptureRuntimeStateCatalog.cs`
- `Services/HandlerDispatchingReadStore.cs`
- `Services/HandlerDispatchingWriteStore.cs`
- `Services/ICdcCaptureRuntimeReporter.cs`

## How it fits

This package is intentionally the smallest honest first step for the broader data baseline. It does not claim relational persistence, Entity Framework integration, durable outbox behavior, or provider breadth yet. Instead, it turns the existing `ICommandHandler`, `IQueryHandler`, `IReadStore`, and `IWriteStore` contracts into a reusable runtime service so consumer apps can execute commands and queries through Cephalon-managed stores before a concrete database pack joins the picture.

That makes `Cephalon.Data` the runtime-neutral layer, while `Cephalon.Data.EntityFramework` now adds the first DbContext-backed companion-pack baseline on top of the same abstractions. The provider pack currently handles honest read/write DbContext registration, capability metadata, direct consumption of the engine-owned `Write` and optional `Read` database roles, explicit dependent `UseRole` references for `Outbox` and for `History` when a host deliberately aliases it to `write`, startup schema apply for those registered `DbContext` roles through a generic-host hosted service, opt-in Entity Framework-backed outbox and inbox paths, outbox descriptors that surface through `/engine/outboxes` and `/engine/snapshot`, inbox descriptors that surface through `/engine/inboxes` and `/engine/snapshot`, and event-driven technology-surface entries for staged outbox producers plus application-managed inbox stores when the eventing technology is active. Richer projection persistence, dedicated outbox execution, broader role graphs, subscription/runtime linkage, and fuller dispatch semantics remain open so the docs stay truthful about what is and is not shipped yet.

The same package now also owns the first runtime-neutral CDC live-state bridge. `AddData()` wires
`ICdcCaptureRuntimeReporter` plus `ICdcCaptureRuntimeStateCatalog` so provider packs, tests, or
host code can report `started`, `captured`, `idle`, or `failed` observations against the active
descriptor catalog without inventing a second runtime registry. The catalog projects every active
capture even before the first report arrives, preserves descriptor ownership fields such as
`sourceModuleId`, `provider`, `sourceId`, `outboxId`, `mode`, `eventFormat`, and `resourceIds`,
tracks totals plus latest checkpoint/change-id/error metadata, and can merge linked
`IEventDispatchRuntimeCatalog` truth into `OutboxDispatchState` when the outbox path already
reports downstream publication posture. That keeps `Cephalon.Data` honest: it now owns the shared
reporting/catalog surface, not provider-specific WAL or change-stream execution loops.

The engine-owned database-topology baseline is now in place through `Engine:Databases`, `AppProfile.Databases`, `/engine/databases`, the resolved `/engine/database-roles` operator catalog, and the resolved `/engine/database-migrations` operator catalog. That baseline makes shared runtime tuning, `Write` / `Read` / `Outbox` / `History` roles, migration policy, requested versus resolved role truth, role-consumer metadata, provider-contributed live role health, logical migration-target state, and provider-added deploy-time command templates introspectable without turning `Cephalon.Data` itself into a provider-specific pack. The next follow-through is deeper provider consumption of those roles rather than inventing a second topology model inside each pack. See [Database topology](../database-topology.md).

## Related docs

- [Cephalon.Abstractions](abstractions.md)
- [Cephalon.Data.EntityFramework](data-entityframework.md)
- [Cephalon.Engine](engine.md)
- [Cephalon.Ids.Sfid](ids-sfid.md)
- [Architecture](../architecture.md)
