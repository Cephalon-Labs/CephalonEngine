# Database Topology

This page describes the shipped `Engine:Databases` baseline in Cephalon and the current follow-through that deepens provider integration, migration orchestration, and durable history storage.

## What is shipped now

Cephalon now owns a first database-topology contract at the engine layer.

That baseline currently includes:

- `Engine:Databases` as the physical-topology section under `Engine`
- shared runtime tuning through `Engine:Databases:Runtime`
- first-class role slots for `Write`, `Read`, `Outbox`, and `History`
- explicit dependent role references through `UseRole` for `Outbox` and `History`
- nested migration policy through `Engine:Databases:Migrations`
- projection into `EngineSettings`
- projection into `AppProfile.Databases`
- validation in the app-profile selection pipeline
- introspection through `/engine/databases`, `/engine/database-roles`, `/engine/database-migrations`, `/engine/app-model`, and `/engine/snapshot`

This means Cephalon now has one engine-owned answer for database topology instead of leaving every host or provider pack to invent its own shape.

The first provider follow-through is also now in place: `Cephalon.Data.EntityFramework` consumes the engine-owned `Write` and optional `Read` roles directly, projects those choices into the `data-management/database-roles` runtime surface, and can apply startup schema creation or migrations for the registered relational `DbContext` roles through a generic-host hosted service when `Engine:Databases:Migrations:ApplyOnStartup` is enabled.

The first durable audit-history follow-through is also now shipped: `Cephalon.Audit.EntityFramework` consumes `Engine:Audit:History` plus the selected engine-owned database role named by `Engine:Audit:History:DatabaseRole`, persists audit rows through a dedicated EF Core `DbContext`, publishes a durable audit-store descriptor through `/engine/audit-stores` and `/engine/snapshot`, exposes filtered-page reads through `IAuditHistoryReader`, exposes bounded NDJSON export streams through `IAuditHistoryExporter`, and can run engine-owned retention passes through `Engine:Audit:History:Retention`.

The next runtime follow-through is now also shipped: `Cephalon.Abstractions` exposes `IDatabaseRoleCatalog`, the engine now projects a resolved `DatabaseRoles` set into `/engine/snapshot`, and ASP.NET Core hosts now expose `/engine/database-roles` plus `/engine/database-roles/{databaseRoleId}`. That catalog answers requested versus resolved roles, `UseRole` truth, connection mode, provider, schema, merged runtime tuning, operator-facing consumers, co-located role references, audit-history metadata, and provider-contributed live runtime health without forcing provider packs or hosts to invent their own runtime topology story.

The next migration follow-through is now also shipped: `Cephalon.Abstractions` exposes `IDatabaseMigrationCatalog`, the engine now projects `DatabaseMigrations` into `/engine/snapshot`, and ASP.NET Core hosts now expose `/engine/database-migrations` plus `/engine/database-migrations/{databaseMigrationId}`. That catalog keeps logical migration targets, requested versus resolved roles, provider ownership, startup/manual execution mode, runtime status (`planned`, `running`, `succeeded`, `failed`, or `unsupported`), and provider-added deploy-time command templates explicit instead of leaving migration truth buried in hosted-service logs or host-only wiring.

## Current shipped shape

The shipped baseline is intentionally narrow and relational-first.

The current shape is:

- `Runtime`
- `Write`
- `Read`
- `Outbox`
- `History`
- `Migrations`

`Write` and `Read` currently stay concrete root targets. `Outbox` and `History` can either define their own provider plus `ConnectionStringName` / `ConnectionString`, or explicitly reuse the concrete `write` target through `UseRole` while still layering local `Schema` and `Runtime` overrides.

That is not yet the final long-term shape, but it is now a truthful engine contract that also owns the first durable audit-history route set. The audit-history path defaults to the `History` role, but it can now target any supported engine-owned role through `Engine:Audit:History:DatabaseRole`, expose operator answers through `/engine/audit-history`, expose bounded NDJSON exports through `/engine/audit-history/export`, and drive retention through `Engine:Audit:History:Retention`.

## Current validation rules

The shipped baseline currently validates:

- `Read` requires the `cqrs` pattern
- `Outbox` requires the `outbox` pattern
- `Read` cannot be configured when `Engine:Data:ReadWriteSplit` is explicitly disabled
- `Outbox` cannot be configured when `Engine:Data:Outbox:Enabled` is explicitly disabled
- `Write` and `Read` cannot use `UseRole`
- `Outbox` and `History` can use `UseRole`, but only to reference the concrete `write` role in the current contract
- `UseRole` cannot be combined with `Provider`, `ConnectionStringName`, or `ConnectionString`
- `ExitAfterApply` requires `ApplyOnStartup = true`
- migration targets must be one of `Write`, `Read`, `Outbox`, or `History`
- migration targets must reference a configured role
- a database target cannot set both `ConnectionStringName` and `ConnectionString`
- durable audit-history export cannot be enabled unless durable audit history is enabled
- durable audit-history export `MaxEntries` must be greater than zero when supplied
- durable audit-history retention requires a positive `MaxAgeDays` value when enabled
- durable audit-history retention must either run on startup or define a recurring `RunIntervalMinutes` value

## Introspection surface

The new baseline is visible through:

- `/engine/databases`
- `/engine/database-roles`
- `/engine/database-roles/{databaseRoleId}`
- `/engine/database-migrations`
- `/engine/database-migrations/{databaseMigrationId}`
- `/engine/app-model`
- `/engine/audit-stores`
- `/engine/audit-history`
- `/engine/audit-history/export`
- `/engine/snapshot`
- `/api/v1/showcase/system/database-topology` in the showcase sample

`/engine/databases` remains the raw engine-owned topology answer. `/engine/database-roles` is the resolved operator catalog for active roles and their runtime metadata, including provider-contributed health and migration-pressure signals when a pack can supply them. `/engine/database-migrations` is the resolved operator catalog for logical migration targets, their execution state, and any provider-added deploy-time command templates. Together they keep database-topology choices explicit and operator-visible even before deeper provider packs consume every part of the contract.

The showcase sample now also exposes `/api/v1/showcase/system/database-topology` as an adoption-quality operator projection that combines the engine-owned role and migration catalogs with sample-specific read-model sync truth: write-side versus read-side row counts, durable projection-job backlog, retry state, and per-scope completion status. `/showcase` now promotes that same projection into a first-class `Database Topology` section with direct links back to `/engine/databases`, `/engine/database-roles`, and `/engine/database-migrations`, plus derived operator insights that call out when the topology is aligned versus when roles, migrations, or read-model sync need attention. That keeps the split `write`/`read` story observable without requiring operators to inspect the underlying databases directly, while still leaving the raw engine-owned routes as the canonical contract.

## Current example

```json
{
  "ConnectionStrings": {
    "WriteDb": "Host=localhost;Port=5432;Database=cephalon_write;Username=cephalon;Password=secret",
    "ReadDb": "Host=localhost;Port=5432;Database=cephalon_read;Username=cephalon;Password=secret",
    "HistoryDb": "Host=localhost;Port=5432;Database=cephalon_history;Username=cephalon;Password=secret"
  },
  "Engine": {
    "Data": {
      "Provider": "EntityFramework",
      "ReadWriteSplit": true,
      "Outbox": {
        "Enabled": true
      }
    },
    "Audit": {
      "Enabled": true,
      "History": {
        "Enabled": true,
        "Provider": "entity-framework",
        "DatabaseRole": "history",
        "Export": {
          "Enabled": true,
          "MaxEntries": 1000
        },
        "Retention": {
          "Enabled": true,
          "MaxAgeDays": 90,
          "DeleteBatchSize": 250,
          "ApplyOnStartup": true
        }
      }
    },
    "Databases": {
      "Runtime": {
        "EnableDetailedErrors": true,
        "EnableSensitiveDataLogging": false,
        "EnableRetryOnFailure": true,
        "MaxRetryCount": 5,
        "MaxRetryDelaySeconds": 10,
        "CommandTimeoutSeconds": 30,
        "MaxBatchSize": 128
      },
      "Write": {
        "Provider": "PostgreSql",
        "ConnectionStringName": "WriteDb",
        "Runtime": {
          "EnableRetryOnFailure": false
        }
      },
      "Read": {
        "Provider": "PostgreSql",
        "ConnectionStringName": "ReadDb"
      },
      "Outbox": {
        "UseRole": "write",
        "Schema": "outbox01"
      },
      "History": {
        "Provider": "PostgreSql",
        "ConnectionStringName": "HistoryDb"
      },
      "Migrations": {
        "ApplyOnStartup": true,
        "ExitAfterApply": false,
        "Targets": [ "write", "read", "history" ]
      }
    }
  }
}
```

The showcase sample keeps PostgreSQL root-role settings in grouped files under `Configurations/ConnectionStrings/*` plus `Configurations/Engine/Databases/*` for Docker-backed runs, applies startup migrations for `write`, `read`, and `history`, and lets tests or alternate hosts override those same config keys to isolated in-memory roles without changing the host code. The sample now also stages durable read-projection jobs in the write database and reconciles the separate read database through a startup rebuild plus background retry loop, so the read-side split remains truthful even when immediate projection fails. The `/api/v1/showcase/system/database-topology` projection and the inline `/showcase` operator section now make that reconciliation visible in one place for demos, operator walkthroughs, and regression tests, including derived operator insights for aligned topology, migration attention, and read-model drift or backlog.

For Entity Framework-backed roles, the migration catalog now also carries operator-facing command templates such as:

- `dotnet ef migrations bundle --context <DbContext>`
- `dotnet ef migrations script --context <DbContext> --idempotent`
- `dotnet ef database update --context <DbContext>`

These templates are guidance, not execution orchestration. They keep the production bundle/script-first path visible in the same runtime surface as the migration target itself, while still allowing startup apply for local or controlled-host scenarios.

For Entity Framework-backed roles, the role catalog now also projects live probe metadata such as connectivity outcome, provider name, pending migration count, applied migration count, and last probe time. When a dependent target such as `Outbox` reuses `write` through `UseRole`, the resolved role catalog now keeps that inherited runtime truth visible without lying about the logical role id.

## What this baseline does not claim yet

The engine now owns the topology contract, but several follow-through slices are still intentionally separate:

- provider packs beyond `Cephalon.Data.EntityFramework` do not yet consume `Engine:Databases` automatically as their only runtime-registration source
- the current `UseRole` contract is intentionally narrow: only `Outbox` and `History` can reference `write`, and the engine does not yet expose arbitrary role graphs or chained references
- dedicated relational-role sharing still needs truthful migration/bootstrap guidance when multiple `DbContext` models point at the same physical database
- there is not yet an engine-owned bundle/script generation or execution orchestration path for deploy-time database changes beyond the current provider-added command templates
- replay UX and richer export formats or delivery automation for durable audit history are not yet shipped
- Cephalon does not yet expose fine-grained migration step progress, probe scheduling/caching policy, bundle/script execution telemetry, or broader provider-native operational diagnostics beyond the current resolved role catalog, migration target catalog, topology snapshot, and audit-store descriptor set

## Recommended next direction

The next follow-through should deepen this baseline instead of replacing it:

1. expand provider-pack consumption beyond `Cephalon.Data.EntityFramework` and keep the role contract consistent across companion packs
2. broaden role references beyond the current one-step dependent `UseRole -> write` contract only when the runtime and migration story stay explicit
3. deepen migration orchestration beyond the shipped Entity Framework startup hosted service while keeping production guidance bundle- and script-first
4. deepen durable audit history with replay flows, richer export formats, and additional provider packs on top of the shipped reader/exporter plus retention baseline

## What not to do

- do not move database topology back into sample-only host code
- do not make mandatory `ReadDbContextBase` / `WriteDbContextBase` inheritance the primary engine contract
- do not overload `Cephalon.Audit` and pretend durable history already exists there
- do not split migrations back into a separate root `Engine:DatabaseMigrations` section

## Related docs

- [Architecture](architecture.md)
- [App models](app-models.md)
- [Cephalon.Engine](components/engine.md)
- [Cephalon.Data](components/data.md)
- [Cephalon.Data.EntityFramework](components/data-entityframework.md)
- [Engine roadmap](engine-roadmap.md)
- [Engine backlog](engine-backlog.md)
