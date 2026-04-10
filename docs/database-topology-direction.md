# Cephalon Database Topology Direction

This page captures the recommended long-term direction for database topology, relational role wiring, migration orchestration, and durable audit history in Cephalon.

It is a design-direction document, not a claim that every item below is already shipped.

## Why this direction exists

Cephalon already ships a truthful phase-8 baseline:

- `Cephalon.Data` keeps read/write dispatch runtime-neutral
- `Cephalon.Data.EntityFramework` proves one relational-first path with one shared or split read/write `DbContext`
- `Cephalon.Audit` stays intentionally narrow and truthful as an in-memory-first recording baseline

That baseline is useful, but it is not yet the long-term engine answer for:

- physical database roles such as `Write`, `Read`, and `History`
- migration targeting and operational policy
- durable audit history storage
- moving one codebase between single-database and split-topology deployments without rewriting host code

If Cephalon leaves those choices inside provider packs or sample hosts, the repo will drift toward hidden conventions instead of a real engine contract.

## North star

Cephalon should let one module and behavior codebase move between physical database layouts through configuration and additive companion packs.

That means the engine should own the topology contract, while provider packs own how a given store family implements that topology.

Write persistence should continue to behave like a short-lived unit of work. Cephalon should not introduce an ambient long-lived engine-wide `DbContext`; advanced flows should use explicit factories or separate role-specific contexts instead.

## Recommended architecture

### 1. Keep `Engine:Data` logical and add `Engine:Databases` for physical topology

`Engine:Data` should stay the app-model and capability-selection layer:

- primary data-provider family
- read/write split intent
- outbox enablement
- id generation

Physical database layout should move into a dedicated `Engine:Databases` section:

- named database roles such as `Write`, `Read`, and `History`
- provider-family selection per role
- provider-family-specific connection metadata
- role-targeted runtime overrides
- migration targeting and operational policy

That separation keeps Cephalon honest:

- `Engine:Data` answers what kind of data/runtime shape the app selected
- `Engine:Databases` answers how that shape is physically deployed

### 2. Make database roles first-class and introspectable

The engine should treat named roles as runtime descriptors, not as incidental sample conventions.

Recommended first-class roles:

- `Write`
- `Read`
- `History`

`Outbox` should usually reference a role instead of redefining a separate connection block when it shares the write-side store.

The runtime should eventually expose that answer through a dedicated surface such as `/engine/databases` and the broader `/engine/snapshot` payload.

### 3. Keep provider-family standards intact inside the topology model

Cephalon already standardized provider configuration by family. The database-topology contract should reuse that rule instead of flattening everything into one fake universal shape:

- connection-string-native roles use `ConnectionStringName` plus `ConnectionString`
- URI-first roles use `UriName` plus `Uri`
- topology-first providers keep explicit topology blocks

That lets the engine own role semantics without pretending every storage system is configured the same way.

The same guardrail should keep the read side flexible. A named `Read` role should not lock Cephalon into one relational implementation forever; the relational-first path can be EF Core today while later query-oriented packs still target the same role with Dapper-, SQL-, or provider-specific read paths.

### 4. Do not make `DbContext` base classes the primary engine contract

Mandatory `ReadDbContextBase` and `WriteDbContextBase` types would make Cephalon feel familiar at first, but they are not the strongest long-term contract for an engine/framework:

- they push too much policy into inheritance
- they make plain EF Core `DbContext` authoring harder to reuse
- they blur host-agnostic engine contracts with one relational implementation style

The preferred baseline is:

- engine-owned database-role topology
- provider registration helpers
- marker interfaces where shared tables are needed
- model-builder extensions for shared schema slices
- interceptors for cross-cutting persistence concerns such as history capture
- explicit factories when a workflow needs more than one short-lived unit of work in the same host scope

Optional convenience base classes can still land later if they reduce boilerplate, but they should remain thin and opt-in. They should never be the only supported path.

### 5. Keep durable audit history additive

`Cephalon.Audit` should remain the narrow host-agnostic recording baseline.

Durable history should arrive as additive provider-specific follow-through, starting with the relational-first path:

- core toggle and policy under `Engine:Audit`
- storage targeting through `Engine:Databases`
- first durable provider pack through a future `Cephalon.Audit.EntityFramework`

That keeps the current audit pack truthful while still opening a path to:

- dedicated history databases
- shared write-side persistence
- retention and redaction policy
- operator-facing audit-store introspection

### 6. Treat migrations as an operational contract, not just a `DbContext` trick

Cephalon should give teams one engine-owned answer for migration targets and startup behavior, but it should not force startup schema changes as the only deployment model.

Recommended split:

- runtime config can opt into startup apply when a host deliberately wants that behavior
- production guidance should prefer deploy-time bundles or scripts
- migration targeting should be role-based, not `DbContext`-name-based

That aligns with official EF Core guidance to treat migration bundles and scripts as a first-class production path rather than assuming every app should mutate schema during startup:

- [DbContext lifetime and configuration](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)
- [EF Core interceptors](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors)
- [Applying migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)
- [Separate migrations projects](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/projects)

## Recommended configuration shape

The first relational-first shape should look roughly like this:

```json
{
  "Engine": {
    "Data": {
      "Provider": "entity-framework",
      "ReadWriteSplit": true,
      "Outbox": {
        "Enabled": true
      }
    },
    "Databases": {
      "EntityFrameworkDefaults": {
        "EnableDetailedErrors": true,
        "EnableSensitiveDataLogging": false,
        "EnableRetryOnFailure": true,
        "MaxRetryCount": 5,
        "MaxRetryDelaySeconds": 10,
        "CommandTimeoutSeconds": 30,
        "MaxBatchSize": 128
      },
      "Roles": {
        "Write": {
          "Provider": "postgresql",
          "ConnectionStringName": "WriteDb",
          "EntityFramework": {
            "EnableRetryOnFailure": false
          }
        },
        "Read": {
          "Provider": "postgresql",
          "ConnectionStringName": "ReadDb"
        },
        "History": {
          "Provider": "postgresql",
          "ConnectionStringName": "HistoryDb"
        }
      },
      "Outbox": {
        "UseRole": "Write",
        "Schema": "outbox01"
      },
      "Migrations": {
        "ApplyOnStartup": false,
        "ExitAfterApply": false,
        "Targets": [ "Write", "History" ]
      }
    },
    "Audit": {
      "Enabled": true,
      "History": {
        "Enabled": true,
        "UseRole": "History"
      }
    }
  }
}
```

## What not to do

The current recommendation is to avoid these shortcuts:

- do not make `ReadDbContextBase` and `WriteDbContextBase` mandatory for all consumers
- do not let `Cephalon.Data.EntityFramework` become the de facto owner of physical database topology
- do not hide durable audit history behind the current in-memory audit baseline
- do not couple audit history storage to tenant resolution or identity host adapters
- do not make startup migration apply the only supported operational story
- do not duplicate provider, connection, and schema settings across `Write`, `Outbox`, and `History` when roles can reference one another explicitly

## Where Cephalon can differentiate

Typical frameworks stop at `DbContext` registration, migrations, and auditing helpers.

Cephalon can do more because runtime introspection is part of the product:

- one module codebase can move between single-db, split read/write, shared-write-plus-outbox, and dedicated-history layouts through config and companion-pack selection
- operators can inspect active database roles, migration targets, outbox routing, and audit-store truth through runtime surfaces instead of reading startup code
- audit history, outbox staging, and migration policy can share the same role model instead of living in unrelated sections
- provider packs stay additive and replaceable because the engine owns the topology contract

## Planned implementation slices

The recommended follow-through is:

1. `ENG-060` engine-owned database topology and runtime catalog baseline
2. `ENG-061` role-aware relational runtime and migration orchestration baseline
3. `ENG-062` durable audit-history provider baseline

See [Engine backlog](engine-backlog.md) and [Engine roadmap](engine-roadmap.md) for the planned sequencing.
