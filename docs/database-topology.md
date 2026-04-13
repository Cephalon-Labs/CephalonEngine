# Database Topology

This page describes the shipped `Engine:Databases` baseline in Cephalon and the current follow-through that deepens provider integration, migration orchestration, and durable history storage.

## What is shipped now

Cephalon now owns a first database-topology contract at the engine layer.

That baseline currently includes:

- `Engine:Databases` as the physical-topology section under `Engine`
- shared runtime tuning through `Engine:Databases:Runtime`
- an engine-owned role-probe freshness contract through `Engine:Databases:Runtime:RoleProbeFreshnessSeconds`
- first-class role slots for `Write`, `Read`, `Outbox`, and `History`
- explicit dependent role references through `UseRole` for `Outbox` and `History`
- nested migration policy through `Engine:Databases:Migrations`
- projection into `EngineSettings`
- projection into `AppProfile.Databases`
- validation in the app-profile selection pipeline
- introspection through `/engine/databases`, `/engine/database-roles`, `/engine/database-migrations`, `/engine/database-migration-playbook`, `/engine/database-topology`, `/engine/app-model`, and `/engine/snapshot`

This means Cephalon now has one engine-owned answer for database topology instead of leaving every host or provider pack to invent its own shape.

The first provider follow-through is also now in place: `Cephalon.Data.EntityFramework` consumes the engine-owned `Write` and optional `Read` roles directly, projects those choices into the `data-management/database-roles` runtime surface, and can apply startup schema creation or migrations for the registered relational `DbContext` roles through a generic-host hosted service when `Engine:Databases:Migrations:ApplyOnStartup` is enabled.

The first durable audit-history follow-through is also now shipped: `Cephalon.Audit.EntityFramework` consumes `Engine:Audit:History` plus the selected engine-owned database role named by `Engine:Audit:History:DatabaseRole`, persists audit rows through a dedicated EF Core `DbContext`, publishes a durable audit-store descriptor through `/engine/audit-stores` and `/engine/snapshot`, exposes filtered-page reads through `IAuditHistoryReader`, exposes bounded NDJSON export streams through `IAuditHistoryExporter`, and can run engine-owned retention passes through `Engine:Audit:History:Retention`.

The next runtime follow-through is now also shipped: `Cephalon.Abstractions` exposes `IDatabaseRoleCatalog`, the engine now projects a resolved `DatabaseRoles` set into `/engine/snapshot`, and ASP.NET Core hosts now expose `/engine/database-roles` plus `/engine/database-roles/{databaseRoleId}`. That catalog answers requested versus resolved roles, `UseRole` truth, connection mode, provider, schema, merged runtime tuning, operator-facing consumers, co-located role references, audit-history metadata, engine-owned probe freshness policy, and provider-contributed live runtime health without forcing provider packs or hosts to invent their own runtime topology story.

The next migration follow-through is now also shipped: `Cephalon.Abstractions` exposes `IDatabaseMigrationCatalog`, the engine now projects `DatabaseMigrations` into `/engine/snapshot`, and ASP.NET Core hosts now expose `/engine/database-migrations` plus `/engine/database-migrations/{databaseMigrationId}`. That catalog keeps logical migration targets, requested versus resolved roles, provider ownership, startup/manual execution mode, runtime status (`planned`, `running`, `succeeded`, `failed`, or `unsupported`), typed recommended execution order for operator playbooks, typed resolved-role runtime fields (`RoleHealthState`, `RoleHealthDescription`, `RoleMigrationState`, `RoleMigrationDescription`, and `RoleObservedAtUtc`), and provider-added deploy-time command templates explicit instead of leaving migration truth buried in hosted-service logs or host-only wiring. `DatabaseMigrationCommandDescriptor` now also carries typed operator metadata such as tool id, execution category, and working-directory hint so hosts can reuse stable command semantics without depending only on ad-hoc metadata keys.

The next operator follow-through is now also shipped: `Cephalon.Abstractions` exposes `DatabaseMigrationOperationalPlaybook`, `DatabaseMigrationOperationalStep`, `DatabaseMigrationOperationalExecutionGroup`, and `IDatabaseMigrationOperationalPlaybookProvider`, the engine now projects `DatabaseMigrationPlaybook` into `/engine/snapshot`, and ASP.NET Core hosts now expose `/engine/database-migration-playbook`. That playbook turns the lower-level migration catalog into one ordered operator answer with generated time, target counts, production-ready and manual-path counts, startup-apply counts, ordered steps, physical-target execution groups, resolved-role truth, execution mode, current status, and the selected production-versus-manual command guidance per step.

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
- `/engine/database-migration-playbook`
- `/engine/database-topology`
- `/engine/app-model`
- `/engine/audit-stores`
- `/engine/audit-history`
- `/engine/audit-history/export`
- `/engine/snapshot`
- `/api/v1/showcase/system/database-topology` in the showcase sample
- `/api/v1/showcase/system/database-topology/brief` for a shareable sample-level Markdown operator handoff
- `/api/v1/showcase/system/database-topology/handoff` for a downloadable sample-level operator package

`/engine/databases` remains the raw engine-owned topology answer. `/engine/database-roles` plus `snapshot.DatabaseRoles` are the resolved operator catalog for active roles and their runtime metadata, including provider-contributed health and migration-pressure signals when a pack can supply them, plus stable `PhysicalTargetId`, `PhysicalTargetDisplayName`, and physical co-location answers so operators can tell when multiple logical roles share one physical database target. `/engine/database-migrations` plus `snapshot.DatabaseMigrations` are the resolved operator catalog for logical migration targets, their execution state, typed recommended execution order, typed resolved-role runtime state, and any provider-added deploy-time command templates plus typed operator metadata for known reusable command fields. `/engine/database-migration-playbook` plus `snapshot.DatabaseMigrationPlaybook` are now the canonical engine-owned ordered migration answer on top of that catalog: one generated playbook with ordered steps, physical-target execution groups, production-ready and manual-path counts, startup-apply truth, resolved-role identity, current status, physical-target identity, per-step coordinated migration ids, per-group coordination counts and hints for shared targets, and selected production-versus-manual command guidance per target. `/engine/database-topology` plus `snapshot.DatabaseTopology` now add the canonical engine-owned posture answer on top of those catalogs: one summary, advisory set, and ordered action plan that answer `Ready`, `Attention`, or `Blocked` from role health, migration status, and production-guidance completeness while also publishing the stable next actions, remediation categories, and source role or migration ids that contributed to each action, including shared-physical-target migration-coordination warnings when pending or failed logical migration work spans one physical database. Together these routes keep database-topology choices explicit and operator-visible even before deeper provider packs consume every part of the contract.

The showcase sample now also exposes `/api/v1/showcase/system/database-topology` as an adoption-quality operator projection that starts from the engine-owned database-topology posture plus the engine-owned role catalog, migration catalog, and migration playbook, then layers only sample-specific read-model sync truth on top: write-side versus read-side row counts, durable projection-job backlog, retry state, and per-scope completion status. `/showcase` now promotes that same projection into a first-class `Database Topology` section with direct links back to `/engine/databases`, `/engine/database-topology`, `/engine/database-roles`, `/engine/database-migrations`, and `/engine/database-migration-playbook`, uses the engine-owned summary, advisory set, ordered action plan, and execution-group guidance as the readiness, insight, and engine-action baseline, and keeps sample-only follow-through limited to read-model drift, backlog, retry pressure, and disabled-sync warnings. The sample-level projection now also preserves engine-owned action categories plus source role and migration ids instead of collapsing those stable fields, preserves physical-target display names plus physical co-location answers from the role catalog, preserves migration-command ids, display names, descriptions, recommendation flags, typed recommended execution order, typed role-runtime fields copied from the engine-owned migration contract, and safe metadata previews for provider-specific extras instead of collapsing the engine-owned descriptors to raw strings. Its ordered migration playbook now comes directly from the engine-owned `/engine/database-migration-playbook` answer instead of being re-derived from raw targets locally, including the engine-owned execution-group counts plus grouped shared-target hints when multiple logical migration targets share one physical database. The sample still adapts those engine-published command templates into repo-root runnable commands for the showcase repository itself, and it still publishes `/api/v1/showcase/system/database-topology/brief` as a Markdown handoff plus `/api/v1/showcase/system/database-topology/handoff` as a self-describing zip package that bundles a package README, the Markdown brief, a machine-readable handoff manifest, and the raw projection payload. That keeps the split `write`/`read` story observable without requiring operators to inspect the underlying databases directly, while still leaving the engine-owned routes as the canonical contract.

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
        "MaxBatchSize": 128,
        "RoleProbeFreshnessSeconds": 30
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

The showcase sample keeps PostgreSQL root-role settings in grouped files under `Configurations/ConnectionStrings/*` plus `Configurations/Engine/Databases/*` for Docker-backed runs, applies startup migrations for `write`, `read`, and `history`, and lets tests or alternate hosts override those same config keys to isolated in-memory roles without changing the host code. Those grouped database-runtime files now also set `RoleProbeFreshnessSeconds = 30` so the engine-owned freshness policy stays visible in local, test, and Docker-backed operator views. When two logical roles reuse the same connection name or inline connection string, the engine now still keeps the logical role identities separate while projecting one shared physical-target id plus display name, so operators can tell that the topology has multiple logical targets on one database without diffing connection strings by hand. The sample now also stages durable read-projection jobs in the write database and reconciles the separate read database through a startup rebuild plus background retry loop, so the read-side split remains truthful even when immediate projection fails. The `/api/v1/showcase/system/database-topology` projection, `/api/v1/showcase/system/database-topology/brief` Markdown export, `/api/v1/showcase/system/database-topology/handoff` package export, and the inline `/showcase` operator section now make that reconciliation visible in one place for demos, operator walkthroughs, and regression tests, including a readiness banner, an ordered operator action plan that now starts from the engine-owned action-plan contract, derived operator insights for aligned topology, migration attention, shared-target coordination when relevant, and read-model drift or backlog, plus shareable operator artifacts that can be handed to another team without copying multiple panels by hand. The handoff package now stays self-describing by including a package `README.md` and `handoff-manifest.json` alongside the brief and raw projection, so a downstream operator or automation step can understand the artifact contents without external thread context.

For Entity Framework-backed roles, the migration catalog now also carries operator-facing command templates such as:

- `dotnet ef migrations bundle --context <DbContext>`
- `dotnet ef migrations script --context <DbContext> --idempotent`
- `dotnet ef database update --context <DbContext>`

These templates are guidance, not execution orchestration. They keep the production bundle/script-first path visible in the same runtime surface as the migration target itself, while still allowing startup apply for local or controlled-host scenarios. `/engine/database-migration-playbook` now selects the canonical ordered production-versus-manual guidance from that lower-level catalog, including physical-target execution groups, shared-target coordination hints, and group-level coverage counts when multiple logical migration targets point at one physical database, while the showcase operator console keeps those descriptors rich by showing their stable ids, human-readable labels, descriptions, production recommendation flags, typed execution-order hints, typed role-health and role-migration runtime notes, command metadata previews, execution-group summaries, and shared-target warnings instead of only dumping raw command text. The sample then derives repo-root runnable commands on top so operators can execute the showcase migration flow without translating the engine template manually, while still consuming the engine-owned ordered playbook instead of inventing a second sequencing contract locally.

For Entity Framework-backed roles, the role catalog now also projects live probe metadata such as connectivity outcome, provider name, pending migration count, applied migration count, and last probe time. When a dependent target such as `Outbox` reuses `write` through `UseRole`, the resolved role catalog now keeps that inherited runtime truth visible without lying about the logical role id.

That same EF-backed role runtime now also honors the engine-owned `RoleProbeFreshnessSeconds` contract. `0` disables caching explicitly and keeps every catalog read live, while configured positive values now flow through the typed `DatabaseRoleProbeDescriptor` surface on `DatabaseRoleRuntimeDescriptor.Probe` plus `DatabaseRoleDescriptor.Probe`, including cache enablement, freshness window, freshness origin, source, fresh-until time, and answer age. The older `probeCacheEnabled`, `probeFreshnessSeconds`, `probeFreshnessOrigin`, `probeSource`, `probeFreshUntilUtc`, and `probeAgeSeconds` metadata keys remain available for additive compatibility and provider-specific extras, but hosts and samples should now prefer the typed probe contract for stable engine-owned answers. When the runtime contract does not override that setting, the current EF provider baseline keeps a 30-second freshness window by default so the operator surface stops re-probing every registered `DbContext` on every read.

## What this baseline does not claim yet

The engine now owns the topology contract, but several follow-through slices are still intentionally separate:

- provider packs beyond `Cephalon.Data.EntityFramework` do not yet consume `Engine:Databases` automatically as their only runtime-registration source
- the current `UseRole` contract is intentionally narrow: only `Outbox` and `History` can reference `write`, and the engine does not yet expose arbitrary role graphs or chained references
- the engine now surfaces shared physical-target coordination plus execution-group truth through `/engine/database-roles`, `/engine/database-migration-playbook`, and `/engine/database-topology`, but it does not yet generate one combined bundle/script artifact or one orchestrated execution flow when multiple `DbContext` models point at the same physical database
- there is not yet an engine-owned bundle/script generation or execution orchestration path for deploy-time database changes beyond the current provider-added command templates and the selected guidance surfaced through `/engine/database-migration-playbook`
- replay UX and richer export formats or delivery automation for durable audit history are not yet shipped
- Cephalon does not yet expose fine-grained migration step progress, adaptive provider-specific probe cadence beyond the shipped freshness-window baseline, bundle/script execution telemetry, or broader provider-native operational diagnostics beyond the current resolved role catalog, migration target catalog, topology snapshot, and audit-store descriptor set

## Recommended next direction

The next follow-through should deepen this baseline instead of replacing it:

1. expand provider-pack consumption beyond `Cephalon.Data.EntityFramework` and keep the role contract consistent across companion packs
2. broaden role references beyond the current one-step dependent `UseRole -> write` contract only when the runtime and migration story stay explicit
3. deepen migration orchestration beyond the shipped ordered execution-group playbook and Entity Framework startup hosted service while keeping production guidance bundle- and script-first
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
