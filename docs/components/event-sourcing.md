# Cephalon.EventSourcing

> **Maturity:** `M1` · **Ownership:** `application-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.EventSourcing` is the runtime-neutral event-sourcing baseline for Cephalon.

## What it owns

- host-agnostic contracts for domain events, event stores, aggregate replay, snapshots, and event-stream catalogs
- low-ceremony registration through `AddEventSourcing(...)` for hosts that want one shared event-sourcing baseline
- stable event-type registration through `AddCephalonEventType<TEvent>(...)`, `AddCephalonEventTypeWithJsonTypeInfo<TEvent>(...)`, and `IEventTypeContributor`
- a merged `IEventStoreCatalog` built from `IEventStoreContributor` registrations
- a merged `IEventTypeRegistry` that maps stable persisted event names to serializer/deserializer descriptors
- aggregate hydration through `AggregateHydrator<TAggregate, TState>` on top of `IEventStore`
- a truthful `event-sourcing` runtime surface that reports active provider/store count, active provider ids, default provider, snapshot toggle state, and one sanitized runtime entry per contributed provider store

## Main surfaces

- `Configuration/EventSourcingOptions.cs`
- `Hosting/EventSourcingServiceCollectionExtensions.cs`
- `Registration/EventSourcingEngineBuilderExtensions.cs`
- `Services/AggregateHydrator.cs`
- `Services/EventTypeDescriptor.cs`
- `Services/EventTypeRegistry.cs`
- `Services/EventStreamCatalog.cs`
- `Services/EventStreamRegistry.cs`
- `Services/IEventTypeContributor.cs`
- `Services/IEventTypeRegistry.cs`
- `Runtime/EventSourcingRuntimeContributor.cs`

## Contracts overview

The host-agnostic contracts live under `Cephalon.Abstractions.EventSourcing`.

- `IDomainEvent` and `DomainEvent` define the minimum stream identity, version, and occurrence timestamp for persisted events
- `IEventStore` defines append, stream read, and current-version lookup
- `IAggregate<TState>` defines deterministic state transitions during replay
- `ISnapshotStore` is declared now so future providers can add snapshot support without changing the baseline contract
- `EventStreamDescriptor`, `IEventStoreContributor`, `IEventStoreRegistry`, and `IEventStoreCatalog` keep active event-stream answers introspectable
- `EventTypeDescriptor`, `IEventTypeContributor`, `IEventTypeRegistry`, and `EventTypeRegistry` keep event payload names, aliases, and serializers explicit so providers do not resolve persisted strings through `Type.GetType(...)`

## Event-type registry

Event stores persist the registry name, not a CLR `AssemblyQualifiedName`. Register every concrete event type before appending or reading that event stream:

```csharp
builder.Services.AddCephalonEventType<OrderPlaced>("orders.order-placed");
```

The default overload uses `System.Text.Json` generic serialization for low-ceremony hosts. Hosts that want source-generated JSON metadata for future trim / Native AOT work should register the same event through:

```csharp
builder.Services.AddCephalonEventTypeWithJsonTypeInfo(
    OrderJsonContext.Default.OrderPlaced,
    "orders.order-placed");
```

Descriptors automatically include the event type's `FullName` and historical `AssemblyQualifiedName` as aliases when those names differ from the stable registry name. That lets stores read legacy rows written by older Cephalon providers while new appends move to stable names.

Provider service-registration helpers wire the registry automatically. If a host constructs a provider store directly, use the constructor overload that accepts `IEventTypeRegistry` or the provider's registry-aware `Create(...)` factory; retained convenience constructors use the empty registry and are only suitable before appending or reading registered payloads.

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

builder.Services.AddCephalonEventType<OrderPlaced>("orders.order-placed");
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
