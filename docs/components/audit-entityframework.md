# Cephalon.Audit.EntityFramework

`Cephalon.Audit.EntityFramework` is the first durable audit-history provider pack for Cephalon.

## What it owns

- registers a durable audit-history `DbContext` through companion-pack registration instead of host-specific startup code
- consumes `Engine:Audit:History` plus the selected `Engine:Databases` role named by `Engine:Audit:History:DatabaseRole`
- persists audit entries through `IAuditWriter` without pushing storage concerns back into modules
- publishes a durable audit-store descriptor through `/engine/audit-stores` and `/engine/snapshot`
- can participate in startup schema apply when `Engine:Databases:Migrations:ApplyOnStartup` is enabled and the selected audit-history `DbContext` role is registered truthfully

## Main surfaces

- `Configuration/EntityFrameworkAuditHistoryOptions.cs`
- `EntityFrameworkAuditHistoryEntry.cs`
- `IEntityFrameworkAuditHistoryContext.cs`
- `Modeling/EntityFrameworkAuditHistoryModelBuilderExtensions.cs`
- `Registration/EntityFrameworkAuditHistoryEngineBuilderExtensions.cs`
- `Services/EntityFrameworkAuditHistoryWriter.cs`
- `Services/EntityFrameworkAuditHistoryStoreRuntimeContributor.cs`

## How it fits

This pack keeps `Cephalon.Audit` narrow. The host-agnostic audit pack still owns low-ceremony recording plus the optional in-memory baseline, while `Cephalon.Audit.EntityFramework` adds the first truthful durable write path on the relational golden path.

The pack is intentionally aligned with the engine-owned database-topology contract. Durable audit history is turned on through `Engine:Audit:History`, targets the logical `history` database role by default, and can be redirected to another engine-owned role through `Engine:Audit:History:DatabaseRole`. The canonical provider id is `entity-framework`, while the pack also accepts the legacy `EntityFramework` alias during this POC phase. That keeps audit-history routing, startup schema apply, and operator-facing runtime answers aligned instead of inventing another storage section.

This pack currently owns the durable write path only. Retention, replay/query UX, export pipelines, and non-relational audit-history providers remain later additive slices.

## Related docs

- [Cephalon.Audit](audit.md)
- [Cephalon.Data.EntityFramework](data-entityframework.md)
- [Cephalon.Engine](engine.md)
- [Database topology](../database-topology.md)
