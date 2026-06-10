# Cephalon.EventSourcing.EntityFramework

> **Maturity:** `M1` · **Ownership:** provider-managed · **Family:** `event-sourcing` · See [audit](../engine-surface-maturity-audit.md), [matrix](../conformance-matrix.md).

`Cephalon.EventSourcing.EntityFramework` is the first provider-backed event-store baseline for Cephalon event-sourcing workloads.

## What it owns

- an Entity Framework Core event-store provider registered through `AddCephalonEntityFrameworkEventSourcing<TContext>()`
- the `EntityFrameworkEventEntry` persistence model for append-only event rows
- model configuration for the `CephalonEvents` table and its indexes
- optimistic-version append semantics on top of `IEventStore`
- stream replay reads ordered by `StreamVersion`
- a truthful event-stream contribution that identifies the active provider as `entity-framework`

## Main surfaces

- `EntityFrameworkEventEntry.cs`
- `EntityFrameworkEventSourcingConfiguration.cs`
- `IEntityFrameworkEventContext.cs`
- `Hosting/EntityFrameworkEventSourcingServiceCollectionExtensions.cs`
- `Registration/EntityFrameworkEventSourcingEngineBuilderExtensions.cs`
- `Services/EntityFrameworkEventStore.cs`

## Provider usage

```csharp
builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddCephalonEventSourcing(options =>
{
    options.DefaultProvider = "entity-framework";
});

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
- the provider stores the CLR event type as `AssemblyQualifiedName` and serializes payloads through `System.Text.Json`

## Not shipped in this slice

This provider intentionally does not claim:

- snapshot persistence
- projection rebuild orchestration
- archival or retention management
- background replay workers
- transport/event-bus integration

It is a narrow append/read provider for the baseline `IEventStore` contract.

## Related docs

- [Cephalon.EventSourcing](event-sourcing.md)
- [Cephalon.Data.EntityFramework](data-entityframework.md)
- [Cephalon.Engine](engine.md)
- [Technology packs](../technology-packs.md)
