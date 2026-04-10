# Cephalon.Data

`Cephalon.Data` is the runtime-neutral data execution companion package for Cephalon.

## What it owns

- registers default `IReadStore` and `IWriteStore` implementations backed by the existing command/query handler abstractions
- keeps command/query dispatching out of hosts so ASP.NET Core and worker apps stay thin
- provides the first reusable bridge between phase-8 data contracts and future provider-specific packs such as `Cephalon.Data.EntityFramework`

## Main surfaces

- `Configuration/DataRuntimeOptions.cs`
- `Registration/DataEngineBuilderExtensions.cs`
- `Services/HandlerDispatchingReadStore.cs`
- `Services/HandlerDispatchingWriteStore.cs`

## How it fits

This package is intentionally the smallest honest first step for the broader data baseline. It does not claim relational persistence, Entity Framework integration, durable outbox behavior, or provider breadth yet. Instead, it turns the existing `ICommandHandler`, `IQueryHandler`, `IReadStore`, and `IWriteStore` contracts into a reusable runtime service so consumer apps can execute commands and queries through Cephalon-managed stores before a concrete database pack joins the picture.

That makes `Cephalon.Data` the runtime-neutral layer, while `Cephalon.Data.EntityFramework` now adds the first DbContext-backed companion-pack baseline on top of the same abstractions. The provider pack currently handles honest read/write DbContext registration, capability metadata, opt-in Entity Framework-backed outbox and inbox paths, outbox descriptors that surface through `/engine/outboxes` and `/engine/snapshot`, inbox descriptors that surface through `/engine/inboxes` and `/engine/snapshot`, and event-driven technology-surface entries for staged outbox producers plus application-managed inbox stores when the eventing technology is active. Richer projection persistence, subscription/runtime linkage, and fuller dispatch semantics remain open so the docs stay truthful about what is and is not shipped yet.

The engine-owned database-topology baseline is now in place through `Engine:Databases`, `AppProfile.Databases`, and `/engine/databases`. That baseline makes shared runtime tuning, `Write` / `Read` / `Outbox` / `History` roles, and migration policy introspectable without turning `Cephalon.Data` itself into a provider-specific pack. The next follow-through is deeper provider consumption of those roles rather than inventing a second topology model inside each pack. See [Database topology](../database-topology.md).

## Related docs

- [Cephalon.Abstractions](abstractions.md)
- [Cephalon.Data.EntityFramework](data-entityframework.md)
- [Cephalon.Engine](engine.md)
- [Cephalon.Ids.Sfid](ids-sfid.md)
- [Architecture](../architecture.md)
