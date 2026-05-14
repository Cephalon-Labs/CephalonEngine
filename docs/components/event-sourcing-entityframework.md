# Cephalon.EventSourcing.EntityFramework

> **Maturity:** `M2` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.EventSourcing.EntityFramework` is the first provider-backed event-store baseline for Cephalon event-sourcing workloads.

## What it owns

- an Entity Framework Core event-store provider registered through `AddCephalonEntityFrameworkEventSourcing<TContext>()`
- the `EntityFrameworkEventEntry` persistence model for append-only event rows
- the `EntityFrameworkEventSnapshotEntry` persistence model for provider-durable replay snapshots
- model configuration for the `CephalonEvents` and `CephalonEventSnapshots` tables and their indexes
- optimistic-version append semantics on top of `IEventStore`
- stream replay reads ordered by `StreamVersion`
- provider-durable snapshot save/load semantics on top of `ISnapshotStore`
- a truthful event-stream contribution that identifies the active provider as `entity-framework`, exposes `snapshotStorage = CephalonEventSnapshots`, and marks `snapshotLifecycle = provider-durable` through `IEventStoreCatalog` and the `event-sourcing` runtime surface

## Main surfaces

- `EntityFrameworkEventEntry.cs`
- `EntityFrameworkEventSnapshotEntry.cs`
- `EntityFrameworkEventSourcingConfiguration.cs`
- `IEntityFrameworkEventContext.cs`
- `Hosting/EntityFrameworkEventSourcingServiceCollectionExtensions.cs`
- `Registration/EntityFrameworkEventSourcingEngineBuilderExtensions.cs`
- `Services/EntityFrameworkEventStore.cs`
- `Services/EntityFrameworkEventStoreContributor.cs`
- `Services/EntityFrameworkSnapshotStore.cs`

## Provider usage

```csharp
builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddCephalonEventSourcing(options =>
{
    options.DefaultProvider = "entity-framework";
});

builder.Services.AddCephalonEventType<OrderPlaced>("orders.order-placed");
builder.Services.AddCephalonEntityFrameworkEventSourcing<OrdersDbContext>();
```

`OrdersDbContext` must implement `IEntityFrameworkEventContext`:

```csharp
public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options)
    : DbContext(options), IEntityFrameworkEventContext
{
    public DbSet<EntityFrameworkEventEntry> Events => Set<EntityFrameworkEventEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        EntityFrameworkEventSourcingConfiguration.ConfigureCephalonEvents(modelBuilder);
    }
}
```

## Concurrency semantics

- `GetVersionAsync(streamId)` returns `-1` when the stream does not exist yet
- `AppendAsync(..., expectedVersion: -1)` requires a new stream with no persisted events
- `AppendAsync` throws `EventStreamConcurrencyException` when the persisted version does not match `expectedVersion`
- appended events must already carry the exact sequential versions being persisted
- the provider stores the stable Cephalon event-type registry name and serializes payloads through the registered event-type descriptor
- descriptors include legacy `AssemblyQualifiedName` aliases by default so older rows can still be read after hosts register the concrete event type

## Durable snapshot semantics

`ENG-708` promotes the Entity Framework provider to the first provider-managed durable snapshot proof for the EventSourcing family.

- `AddCephalonEntityFrameworkEventSourcing<TContext>()` registers `ISnapshotStore` with the same `DbContext` used by the event store
- `ConfigureCephalonEvents(modelBuilder)` maps `CephalonEventSnapshots` with one latest snapshot per `StreamId` plus state type
- saving an older snapshot version over a newer persisted snapshot fails instead of rolling state backward
- the core `IEventStreamReplayWorker` can load the EF snapshot, replay only remaining events, save the new final state, and expose provider-durable snapshot evidence through `/engine/technology-surfaces` and `/engine/snapshot`
- consumer aggregate, projection, and replay code stays on the provider-neutral `IEventStore`, `ISnapshotStore`, and `IEventStreamReplayWorker` contracts

## Not shipped in this slice

This provider intentionally does not claim:

- projection rebuild orchestration
- archival or retention management
- background replay workers
- transport/event-bus integration

It owns append/read event storage plus the durable latest-snapshot store for the baseline EventSourcing contracts. Broader projection orchestration, archival, retention, distributed replay, and background runners remain later provider or engine slices.

## Related docs

- [Cephalon.EventSourcing](event-sourcing.md)
- [Cephalon.Data.EntityFramework](data-entityframework.md)
- [Cephalon.Engine](engine.md)
- [Technology packs](../technology-packs.md)
