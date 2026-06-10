# Cephalon.EventSourcing

> **Maturity:** `M1` · **Ownership:** application-managed · **Family:** `event-sourcing` · See [audit](../engine-surface-maturity-audit.md), [matrix](../conformance-matrix.md).

`Cephalon.EventSourcing` is the runtime-neutral event-sourcing baseline for Cephalon.

See also: [Engine surface maturity audit](../engine-surface-maturity-audit.md), [Conformance matrix](../conformance-matrix.md), and [Runtime contract index](../runtime-contract-index.md) for the per-package adoption-truth, maturity (`M1`, application-managed), ownership, and the `IEventStoreCatalog` / `snapshot.*` runtime-key inventory that the event-sourcing baseline projects against. [Engineering standards](../engineering-standards.md) records the library-design and code-quality baseline the contract layer ships against; [Long-range engine direction](../long-range-direction.md) frames why event-sourcing stays a host-agnostic baseline with a separate snapshot contract that provider packages (`Cephalon.EventSourcing.EntityFramework`, `.MongoDb`, `.Redis`, `.Nats`, and the wider catalog) plug into additively, rather than fusing stream and snapshot persistence into a single provider.

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
- [Engine surface maturity audit](../engine-surface-maturity-audit.md)
- [Conformance matrix](../conformance-matrix.md)
- [Runtime contract index](../runtime-contract-index.md)
- [Long-range engine direction](../long-range-direction.md)
- [Engineering standards](../engineering-standards.md)
- [Architecture review (May 2026)](../architecture-review-2026-05.md)
- [Cephalon.EventSourcing.EntityFramework](event-sourcing-entityframework.md)
- [Cephalon.Eventing](eventing.md)
- [Technology packs](../technology-packs.md)
