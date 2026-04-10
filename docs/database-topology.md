# Database Topology

This page describes the shipped `Engine:Databases` baseline in Cephalon and the next follow-through that will deepen provider integration, migration orchestration, and durable history storage.

## What is shipped now

Cephalon now owns a first database-topology contract at the engine layer.

That baseline currently includes:

- `Engine:Databases` as the physical-topology section under `Engine`
- shared runtime tuning through `Engine:Databases:Runtime`
- first-class role slots for `Write`, `Read`, `Outbox`, and `History`
- nested migration policy through `Engine:Databases:Migrations`
- projection into `EngineSettings`
- projection into `AppProfile.Databases`
- validation in the app-profile selection pipeline
- introspection through `/engine/databases`, `/engine/app-model`, and `/engine/snapshot`

This means Cephalon now has one engine-owned answer for database topology instead of leaving every host or provider pack to invent its own shape.

The first provider follow-through is also now in place: `Cephalon.Data.EntityFramework` consumes the engine-owned `Write` and optional `Read` roles directly, projects those choices into the `data-management/database-roles` runtime surface, and can apply startup schema creation or migrations for the registered relational `DbContext` roles through a generic-host hosted service when `Engine:Databases:Migrations:ApplyOnStartup` is enabled.

## Current shipped shape

The shipped baseline is intentionally narrow and relational-first.

The current shape is:

- `Runtime`
- `Write`
- `Read`
- `Outbox`
- `History`
- `Migrations`

Each target currently carries its own provider plus either `ConnectionStringName` or `ConnectionString`, along with optional per-role runtime overrides.

That is not yet the final long-term shape, but it is the first truthful engine contract.

## Current validation rules

The shipped baseline currently validates:

- `Read` requires the `cqrs` pattern
- `Outbox` requires the `outbox` pattern
- `Read` cannot be configured when `Engine:Data:ReadWriteSplit` is explicitly disabled
- `Outbox` cannot be configured when `Engine:Data:Outbox:Enabled` is explicitly disabled
- `ExitAfterApply` requires `ApplyOnStartup = true`
- migration targets must be one of `Write`, `Read`, `Outbox`, or `History`
- migration targets must reference a configured role
- a database target cannot set both `ConnectionStringName` and `ConnectionString`

## Introspection surface

The new baseline is visible through:

- `/engine/databases`
- `/engine/app-model`
- `/engine/snapshot`

That keeps database-topology choices explicit and operator-visible even before deeper provider packs consume every part of the contract.

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
        "Provider": "PostgreSql",
        "ConnectionStringName": "WriteDb",
        "Schema": "outbox01"
      },
      "History": {
        "Provider": "PostgreSql",
        "ConnectionStringName": "HistoryDb"
      },
      "Migrations": {
        "ApplyOnStartup": false,
        "ExitAfterApply": false,
        "Targets": [ "write", "outbox", "history" ]
      }
    }
  }
}
```

## What this baseline does not claim yet

The engine now owns the topology contract, but several follow-through slices are still intentionally separate:

- provider packs beyond `Cephalon.Data.EntityFramework` do not yet consume `Engine:Databases` automatically as their only runtime-registration source
- `Outbox` and `History` still use full target blocks instead of role references such as `UseRole`
- the shipped Entity Framework baseline only executes startup apply for the registered `write` and optional `read` `DbContext` roles; dedicated `outbox` and `history` migration execution are not shipped yet
- there is not yet an engine-owned bundle/script orchestration path for deploy-time database changes
- durable audit history is not yet shipped as a provider-backed storage path
- Cephalon does not yet expose a richer runtime catalog with role health, resolved provider metadata, or migration-execution state beyond the current topology snapshot

## Recommended next direction

The next follow-through should deepen this baseline instead of replacing it:

1. expand provider-pack consumption beyond `Cephalon.Data.EntityFramework` and keep the role contract consistent across companion packs
2. add role references so dependent stores do not duplicate provider and connection settings
3. deepen migration orchestration beyond the shipped Entity Framework startup hosted service while keeping production guidance bundle- and script-first
4. add durable audit history as an additive provider-backed follow-through

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
