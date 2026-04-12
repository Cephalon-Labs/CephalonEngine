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

## Operator Surfaces

- `/engine/database-roles` shows the resolved engine-owned role catalog.
- `/engine/database-migrations` shows the migration-target catalog and execution truth.
- `/api/v1/showcase/system/database-topology` combines those engine catalogs with showcase-specific read-model sync truth:
  - write-store versus read-store row counts
  - durable projection-job backlog and completion state
  - per-scope retry/completion metrics for `products`, `inventory`, `orders`, and `shipments`
- The showcase UI links to that projection directly from the documentation link grid on `/showcase`.

## Manual Migration Commands

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
