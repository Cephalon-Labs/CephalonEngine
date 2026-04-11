# Cephalon Sample Showcase

`Cephalon.Sample.Showcase` is the adoption-quality sample for exercising Cephalon's runtime composition, behavior transports, topology introspection, and durable audit-history story in one host.

## Run Modes

- Default local/test mode stays zero-setup. The host automatically falls back to isolated in-memory `write`, `read`, and `history` database roles when `SHOWCASE_DOCKER` is not `true`.
- Docker mode uses the infrastructure declared in [compose.yaml](compose.yaml) and the split settings under [Configurations](Configurations).

## Start Local

```powershell
dotnet run --project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj
```

## Start Docker Infrastructure

```powershell
docker compose -f samples/Cephalon.Sample.Showcase/compose.yaml up -d
```

Then run the host against Docker-backed dependencies:

```powershell
$env:SHOWCASE_DOCKER = "true"
dotnet run --project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj
```

## Database Roles

- `WriteDb` points to `showcase_write`.
- `ReadDb` currently shares `showcase_write` on purpose. The sample exposes a logical read role, but it does not yet ship a projector-driven physically separate read store.
- `HistoryDb` points to `showcase_history`.

## Migration Policy

- Startup schema apply is enabled for `write` and `history` through `Engine:Databases:Migrations`.
- The sample keeps committed EF Core migrations for:
  - `ShowcaseWriteDbContext`
  - `ShowcaseAuditHistoryDbContext`
- The current topology does not commit `ShowcaseReadDbContext` migrations because the read role still shares the write database.

## Manual Migration Commands

Apply the write store:

```powershell
dotnet ef database update `
  --context ShowcaseWriteDbContext `
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
  --context ShowcaseWriteDbContext `
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
