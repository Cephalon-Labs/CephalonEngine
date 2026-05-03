# Cephalon.EventSourcing

> **Maturity:** `M1` · **Ownership:** `application-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.EventSourcing` is the runtime-neutral event-sourcing baseline for Cephalon.

## What it owns

- host-agnostic contracts for domain events, event stores, aggregate replay, snapshots, and event-stream catalogs
- low-ceremony registration through `AddEventSourcing(...)` for hosts that want one shared event-sourcing baseline
- a merged `IEventStoreCatalog` built from `IEventStoreContributor` registrations
- aggregate hydration through `AggregateHydrator<TAggregate, TState>` on top of `IEventStore`
- a truthful `event-sourcing` runtime surface that reports active stream count, default provider, and snapshot toggle state

## Main surfaces

- `Configuration/EventSourcingOptions.cs`
- `Hosting/EventSourcingServiceCollectionExtensions.cs`
- `Registration/EventSourcingEngineBuilderExtensions.cs`
- `Services/AggregateHydrator.cs`
- `Services/EventStreamCatalog.cs`
- `Services/EventStreamRegistry.cs`

## Contracts overview

The host-agnostic contracts live under `Cephalon.Abstractions.EventSourcing`.

- `IDomainEvent` and `DomainEvent` define the minimum stream identity, version, and occurrence timestamp for persisted events
- `IEventStore` defines append, stream read, and current-version lookup
- `IAggregate<TState>` defines deterministic state transitions during replay
- `ISnapshotStore` is declared now so future providers can add snapshot support without changing the baseline contract
- `EventStreamDescriptor`, `IEventStoreContributor`, `IEventStoreRegistry`, and `IEventStoreCatalog` keep active event-stream answers introspectable

## Entity Framework provider usage

The first provider-backed follow-through is [`Cephalon.EventSourcing.EntityFramework`](event-sourcing-entityframework.md).

Typical host wiring is:

```csharp
builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddCephalonEventSourcing(options =>
{
    options.DefaultProvider = "entity-framework";
});

builder.Services.AddCephalonEntityFrameworkEventSourcing<OrdersDbContext>();
```

Your `DbContext` implements `IEntityFrameworkEventContext` and calls `EntityFrameworkEventSourcingConfiguration.ConfigureCephalonEvents(modelBuilder)` during model creation.

## Concurrency semantics

- `expectedVersion == -1` means the stream must not exist yet
- append operations fail with `EventStreamConcurrencyException` when the current persisted version does not match the caller's expected version
- appended events must already carry the sequential stream versions the caller intends to persist
- stream replay reads events ordered by `StreamVersion` ascending

## Hydration example

```csharp
var hydrator = new AggregateHydrator<OrderAggregate, OrderState>();
var (state, version) = await hydrator.HydrateAsync(eventStore, streamId, cancellationToken: ct);
```

`AggregateHydrator` does not own snapshots, projection rebuilds, or background execution. It only replays events from `IEventStore` through the aggregate's `Apply(...)` contract.

## Not shipped in this slice

This baseline intentionally does not claim:

- snapshot persistence or snapshot-assisted replay
- projection rebuild orchestration
- stream archival, retention, or compaction
- hosted background projection runners
- event-subscription dispatch, saga orchestration, or durable consumer semantics

Those remain later slices until Cephalon can ship them truthfully.

## Related docs

- [Cephalon.Abstractions](abstractions.md)
- [Cephalon.Engine](engine.md)
- [Cephalon.EventSourcing.EntityFramework](event-sourcing-entityframework.md)
- [Cephalon.Eventing](eventing.md)
- [Technology packs](../technology-packs.md)
