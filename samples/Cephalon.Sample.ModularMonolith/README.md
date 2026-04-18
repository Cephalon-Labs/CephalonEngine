# Cephalon Sample: Modular Monolith

This sample is the operator-ready local runtime baseline for Cephalon.

The sample now also carries the narrow phase-8 starter baseline: canonical `Engine` ids, structured `Engine:Data`, `Engine:Identity`, `Engine:Tenancy`, `Engine:Audit`, and `Engine:Messaging` sections, plus low-ceremony `Sfid` id generation and `Cephalon.Audit` wiring in the host.
Its public REST boundary is now behavior-backed: the starter module owns routes through `RestBehaviorModuleBase.ConfigureRestBehaviors(...)` and `MapProfile<TBehavior>()`, matching the shipped `cephalon-monolith` starter baseline.

## Run from source

```powershell
dotnet run --project samples/Cephalon.Sample.ModularMonolith
```

## Run with Docker Compose

From the repository root:

```powershell
docker compose -f samples/Cephalon.Sample.ModularMonolith/compose.yaml up --build
```

Inspect:

- `http://localhost:8080/engine`
- `http://localhost:8080/engine/snapshot`
- `http://localhost:8080/engine/packages`
- `http://localhost:8080/health`
- `http://localhost:8080/health/ready`
- `http://localhost:8080/api/catalog/overview`

The compose stack also starts an `otel-collector` sidecar. Collector health is exposed at `http://localhost:13133/`.

## Optional package directory flow

Stage a package into `samples/Cephalon.Sample.ModularMonolith/plugins` and add the package override:

```powershell
dotnet run --project src/Cephalon.Cli -- package stage --package artifacts/packages-release/Cephalon.ReferenceModule.Operations.0.1.0-preview.nupkg --output samples/Cephalon.Sample.ModularMonolith/plugins/operations
docker compose -f samples/Cephalon.Sample.ModularMonolith/compose.yaml -f samples/Cephalon.Sample.ModularMonolith/compose.packages.yaml up --build
```

Then inspect `http://localhost:8080/engine/packages` and `http://localhost:8080/api/operations/status`.
