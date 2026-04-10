# Cephalon.Audit.EntityFramework

`Cephalon.Audit.EntityFramework` is the first durable audit-history provider pack for Cephalon.

## What it owns

- registers a durable audit-history `DbContext` through companion-pack registration instead of host-specific startup code
- consumes `Engine:Audit:History` plus the selected `Engine:Databases` role named by `Engine:Audit:History:DatabaseRole`
- persists audit entries through `IAuditWriter` without pushing storage concerns back into modules
- exposes filtered, paged audit-history reads through `IAuditHistoryReader`
- exposes bounded audit-history export streams through `IAuditHistoryExporter`
- can run engine-owned retention passes through `Engine:Audit:History:Retention`
- publishes a durable audit-store descriptor through `/engine/audit-stores` and `/engine/snapshot`
- keeps queryable audit-history operator answers available through `/engine/audit-history` when the pack is active in an ASP.NET Core host
- keeps bounded audit-history export answers available through `/engine/audit-history/export` when the pack is active in an ASP.NET Core host and export is enabled
- can participate in startup schema apply when `Engine:Databases:Migrations:ApplyOnStartup` is enabled and the selected audit-history `DbContext` role is registered truthfully

## Main surfaces

- `Configuration/EntityFrameworkAuditHistoryOptions.cs`
- `EntityFrameworkAuditHistoryEntry.cs`
- `IEntityFrameworkAuditHistoryContext.cs`
- `Modeling/EntityFrameworkAuditHistoryModelBuilderExtensions.cs`
- `Registration/EntityFrameworkAuditHistoryEngineBuilderExtensions.cs`
- `Services/EntityFrameworkAuditHistoryReader.cs`
- `Services/EntityFrameworkAuditHistoryRetentionHostedService.cs`
- `Services/EntityFrameworkAuditHistoryWriter.cs`
- `Services/EntityFrameworkAuditHistoryStoreRuntimeContributor.cs`

## How it fits

This pack keeps `Cephalon.Audit` narrow. The host-agnostic audit pack still owns low-ceremony recording plus the optional in-memory baseline, while `Cephalon.Audit.EntityFramework` adds the first truthful durable write, read, and retention path on the relational golden path.

The pack is intentionally aligned with the engine-owned database-topology contract. Durable audit history is turned on through `Engine:Audit:History`, targets the logical `history` database role by default, and can be redirected to another engine-owned role through `Engine:Audit:History:DatabaseRole`. That selected role can now itself use the narrow `UseRole` contract, which means `History` can explicitly reuse the concrete `write` connection while keeping local schema or runtime overrides when a host wants that layout. The canonical provider id is `entity-framework`, while the pack also accepts the legacy `EntityFramework` alias during this POC phase. That keeps audit-history routing, startup schema apply, and operator-facing runtime answers aligned instead of inventing another storage section.

The pack now also owns the first filtered-page reader, NDJSON export stream, and retention baseline. ASP.NET Core hosts can expose operator-facing reads through `/engine/audit-history`, bounded NDJSON exports through `/engine/audit-history/export`, and application-facing modules such as the showcase sample can layer their own REST endpoints over the same `IAuditHistoryReader` plus `IAuditHistoryExporter` contracts. The export path is intentionally configuration-gated through `Engine:Audit:History:Export`, bounded by `MaxEntries`, and limited to NDJSON so nested audit payloads stay truthful without pretending Cephalon already ships a generic report engine. Replay UX, richer export formats, and non-relational audit-history providers remain later additive slices.

## Related docs

- [Cephalon.Audit](audit.md)
- [Cephalon.Data.EntityFramework](data-entityframework.md)
- [Cephalon.Engine](engine.md)
- [Database topology](../database-topology.md)
