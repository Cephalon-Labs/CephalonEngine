# Cephalon.Data.EntityFramework

`Cephalon.Data.EntityFramework` is the first provider-backed data companion pack for Cephalon.

## What it owns

- registers Entity Framework Core `DbContext` services through companion-pack registration instead of host-specific startup code
- supports either one shared `DbContext` or distinct read/write `DbContext` types for CQRS-oriented workloads
- exposes an opt-in Entity Framework-backed inbox that persists processed inbound messages through the active write-side `DbContext`
- exposes an opt-in Entity Framework-backed outbox that persists staged `OutboxMessage` rows through the active write-side `DbContext`
- exposes an adapter-neutral Entity Framework-backed `IEventDispatchStore` for pending staged outbox rows when the eventing package is present
- can enable official `Sfid.EntityFramework` conventions and save-time key assignment for typed `Sfid` identifiers
- publishes capability metadata that makes the active provider and read/write `DbContext` roles introspectable
- publishes an operator-facing inbox descriptor through `/engine/inboxes` and `/engine/snapshot` when the inbox path is enabled
- publishes an operator-facing outbox descriptor through `/engine/outboxes` and `/engine/snapshot` when the outbox path is enabled
- projects that same inbox through the `event-driven-integration` technology surface as `inbox-stores` when the eventing technology is active
- projects that same outbox through the `event-driven-integration` technology surface as `outbox-producers` when the eventing technology is active
- enables an outbox-backed `IEventPublisher` handoff path when the eventing technology is active and a truthful staged-publication path is available

## Main surfaces

- `Configuration/EntityFrameworkDataOptions.cs`
- `Modeling/EntityFrameworkInboxEntry.cs`
- `Modeling/IEntityFrameworkInboxContext.cs`
- `Modeling/EntityFrameworkOutboxEntry.cs`
- `Modeling/IEntityFrameworkOutboxContext.cs`
- `Modeling/EntityFrameworkModelBuilderExtensions.cs`
- `Registration/EntityFrameworkDataEngineBuilderExtensions.cs`
- `Services/EntityFrameworkInboxRuntimeSurfaceContributor.cs`
- `Services/EntityFrameworkEventDispatchStore.cs`
- `Services/EntityFrameworkOutboxRuntimeSurfaceContributor.cs`

## How it fits

This pack sits on top of `Cephalon.Data`, not in place of it. `Cephalon.Data` still owns the runtime-neutral `IReadStore` / `IWriteStore` dispatching surface, while `Cephalon.Data.EntityFramework` supplies the first concrete relational baseline that query and command handlers can depend on through standard Entity Framework Core `DbContext` injection.

The current slice is intentionally honest and narrow: it proves optional `DbContext` registration, CQRS-style read/write role separation, runtime capability metadata, an opt-in inbox persistence path where the write-side `DbContext` explicitly implements `IEntityFrameworkInboxContext` and maps the shared processed-message entity with `ConfigureCephalonInbox()`, an opt-in outbox persistence path where the same write-side `DbContext` can implement `IEntityFrameworkOutboxContext` and map the shared outbox entity with `ConfigureCephalonOutbox()`, operator-facing inbox and outbox descriptors that make staged-only idempotency and delivery truth visible through the runtime introspection surface, an `inbox-stores` technology surface entry that describes application-managed idempotency stores when `EventDrivenIntegration` is active, an `outbox-producers` technology surface entry under the same technology selection, subscription metadata that can now say an application-managed inbox store is available without pretending the eventing pack dispatches through it, a staged-only outbox-backed `IEventPublisher` handoff path, an adapter-neutral Entity Framework-backed `IEventDispatchStore` that can read pending staged rows and persist `dispatch_attempt_count`, `dispatched_at_utc`, and `next_attempt_at_utc` follow-through for later adapter-owned dispatch loops, and optional `Sfid.EntityFramework` conventions plus save-time key assignment when `Cephalon.Ids.Sfid` is active. Full subscription execution, broker-owned retries, and richer projection pipelines are still later slices.

The current recommendation is not to turn `DbContext` inheritance into the primary engine contract. The next follow-through should make database roles and migration policy engine-visible first, then add any optional Entity Framework convenience base classes only as thin developer-experience helpers. See [Database topology direction](../database-topology.md).

The next recommended follow-through is an engine-owned database-topology contract, not a mandatory `ReadDbContextBase` / `WriteDbContextBase` inheritance model. `Cephalon.Data.EntityFramework` should stay usable with plain EF Core `DbContext` types while future engine work adds role-aware `Engine:Databases` configuration, migration targeting, and optional durable audit-history follow-through on top. See [Database topology direction](../database-topology.md).

## Related docs

- [Cephalon.Data](data.md)
- [Cephalon.Ids.Sfid](ids-sfid.md)
- [Cephalon.Engine](engine.md)
- [Database topology direction](../database-topology.md)
- [Architecture](../architecture.md)
