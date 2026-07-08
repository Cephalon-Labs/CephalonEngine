# Cephalon Sample Showcase

`Cephalon.Sample.Showcase` is the adoption-quality sample for exercising Cephalon's runtime composition, behavior transports, topology introspection, and durable audit-history story in one host.

## Run Modes

- Local and Development profiles use the provider selections declared under [Configurations](Configurations).
- The checked-in showcase configs point at Docker Desktop-backed PostgreSQL, MongoDB, Redis, RabbitMQ, Kafka, and OpenTelemetry endpoints on `localhost`.
- Tests or alternate environments can still override the same configuration keys to run with `InMemory` roles instead.

## Start Local

```powershell
dotnet run --project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj
```

From Visual Studio, use:

- `Cephalon.Sample.Showcase` for the fast in-memory profile that does not require Docker.
- `Cephalon.Sample.Showcase (Docker)` for the full external-infrastructure profile.
- Both Visual Studio profiles now open `/showcase` directly so F5 lands on the sample UI instead of the root JSON summary.
- If F5 appears stuck, check for an older `Cephalon.Sample.Showcase` process still holding `https://localhost:54191` or `http://localhost:54192`; the most common failure here is `address already in use`, not a Docker dependency timeout.

## Start Docker Infrastructure

```powershell
docker compose -f samples/Cephalon.Sample.Showcase/compose.yaml up -d
```

Then run the host against Docker-backed dependencies:

```powershell
dotnet run --project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj
```

## Database Roles

- `WriteDb` points to `showcase_write`.
- `ReadDb` points to `showcase_read`.
- `HistoryDb` points to `showcase_history`.

## Migration Policy

- Startup schema apply is enabled for `write`, `read`, and `history` through `Engine:Databases:Migrations`.
- The sample keeps committed EF Core migrations for:
  - `ShowcaseWriteDbContext`
  - `ShowcaseReadDbContext`
  - `ShowcaseAuditHistoryDbContext`
- The read role now has its own committed migration history so it can evolve independently from the write database.
- Write-side mutations now stage durable projection jobs in `showcase_write`, flush them immediately when possible, and let the hosted read-model sync worker retry any unfinished work against `showcase_read`.
- The hosted read-model sync worker is enabled by default. Test or diagnostic profiles that need to prove write/read separation before any catch-up loop runs can set `Showcase:ReadModelProjection:HostedServiceEnabled=false`; request-level flushes still remain available to code paths that explicitly enqueue projection jobs.

## Operator Surfaces

- `/engine/database-roles` shows the resolved engine-owned role catalog.
- `/engine/database-migrations` shows the migration-target catalog and execution truth.
- `/engine/database-migration-playbook` shows the canonical engine-owned ordered migration playbook, including physical-target execution groups, grouped command sets, and combined command-batch templates.
- `/api/v1/showcase/system/database-topology` combines the engine-owned topology posture, role catalog, migration catalog, and migration playbook with showcase-specific read-model sync truth:
  - write-store versus read-store row counts
  - durable projection-job backlog and completion state
  - per-scope retry/completion metrics for `products`, `inventory`, `orders`, and `shipments`
- `/api/v1/showcase/system/database-topology/brief` exports the same live topology answer as a shareable Markdown operator brief.
- `/api/v1/showcase/system/database-topology/handoff` downloads a self-describing zip package that bundles a package `README.md`, the operator brief, a machine-readable `handoff-manifest.json`, and the raw topology projection.
- The showcase UI now promotes that projection into a dedicated `Database Topology` section on `/showcase`, adds a top-level readiness summary for `Ready` versus `Attention` or `Blocked` states, publishes an ordered operator action plan for what to do next, derives operator insights for healthy-versus-drifting topology state, preserves migration-command ids/display names/descriptions plus production-recommendation flags, physical-target identity, and physical co-location, adapts the published templates into repo-root runnable commands for this sample, consumes the engine-owned ordered migration playbook (`write -> read -> history`) before the lower-level target table, surfaces execution-group summaries, grouped command sets, combined command batches, and shared-target coordination warnings when multiple logical migration targets point at one physical database, and links the raw JSON projection, the operator brief, and the downloadable handoff package alongside the underlying `/engine/databases`, `/engine/database-roles`, `/engine/database-migrations`, and `/engine/database-migration-playbook` surfaces for drill-down.

## Manual Migration Commands

The same migration-command descriptors now surface inside `/showcase` with operator-friendly names, descriptions, production recommendation badges, command metadata, repo-root runnable commands, and an ordered migration playbook so the sample UI mirrors the engine-owned `/engine/database-migration-playbook` plus `/engine/database-migrations` contracts without leaving operators to reconstruct the sample invocation or execution order by hand. When the sample is configured so multiple logical migration targets share one physical database, that same UI and the exported operator brief now surface the engine-owned execution-group counts, grouped command sets, combined command batches, grouped partner targets, and warning hints instead of inventing showcase-only shared-database rules.

Apply the write store:

```powershell
dotnet ef database update `
  --context ShowcaseWriteDbContext `
  --project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj `
  --startup-project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj
```

Apply the read store:

```powershell
dotnet ef database update `
  --context ShowcaseReadDbContext `
  --project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj `
  --startup-project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj
```

Apply the audit-history store:

```powershell
dotnet ef database update `
  --context ShowcaseAuditHistoryDbContext `
  --project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj `
  --startup-project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj
```

Generate a bundle or script:

```powershell
dotnet ef migrations bundle `
  --context ShowcaseReadDbContext `
  --project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj `
  --startup-project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj
```

```powershell
dotnet ef migrations bundle `
  --context ShowcaseWriteDbContext `
  --project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj `
  --startup-project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj
```

```powershell
dotnet ef migrations script `
  --context ShowcaseReadDbContext `
  --idempotent `
  --project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj `
  --startup-project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj
```

```powershell
dotnet ef migrations script `
  --context ShowcaseAuditHistoryDbContext `
  --idempotent `
  --project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj `
  --startup-project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj
```

## Notes

- The checked-in migrations were generated with EF Core `10.0.5`.
- If your local `dotnet-ef` tool is older, update it so the CLI matches the runtime packages used by the sample.
- `Engine:Databases:*:Provider` is the source of truth for EF-backed roles. The sample host no longer swaps providers through a separate Docker toggle.
