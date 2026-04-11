# Container Runtime

This guide is the Docker Desktop / WSL-friendly operator baseline for Cephalon.

It uses the shipped modular monolith sample so teams can validate:

- container startup with the same `Cephalon.Sample.ModularMonolith` host they can run from source
- `/engine/*` and `/health/*` surfaces through a reproducible local deployment boundary
- OpenTelemetry OTLP handoff to a local collector without adding vendor-specific behavior to the engine core

The same container runtime baseline now also ships in generated app outputs from `cephalon new` and the app-focused `dotnet new cephalon-*` starters.

## Prerequisites

- Docker Desktop on Windows, or a Docker Engine plus Docker Compose environment inside WSL/Linux
- the repository checked out locally

WSL is optional. The same compose files work from Windows PowerShell, PowerShell 7, or a Linux-class shell as long as Docker Compose is available.

## Start the sample

From the repository root:

```powershell
docker compose -f samples/Cephalon.Sample.ModularMonolith/compose.yaml up --build
```

The compose stack starts:

- `modular-monolith` on `http://localhost:8080`
- `otel-collector` with health on `http://localhost:13133/`

The sample host keeps its ordinary Cephalon config model. Docker only supplies late-bound environment values such as:

- `DOTNET_ENVIRONMENT=Container`
- `Engine:Observability:Telemetry:*` for OTLP export

## Inspect the runtime

Once the app is running, inspect:

- `http://localhost:8080/engine`
- `http://localhost:8080/engine/snapshot`
- `http://localhost:8080/engine/runtime-story`
- `http://localhost:8080/engine/packages`
- `http://localhost:8080/health`
- `http://localhost:8080/health/ready`
- `http://localhost:8080/api/catalog/overview`

To inspect collector output:

```powershell
docker compose -f samples/Cephalon.Sample.ModularMonolith/compose.yaml logs otel-collector
```

The collector uses `samples/Cephalon.Sample.ModularMonolith/otel-collector-config.yaml` and exports received logs, metrics, and traces through the debug exporter so the handoff stays visible during local runs.

## Optional package-directory replay

If you want the same container run to load a staged external package directory, stage a package under `samples/Cephalon.Sample.ModularMonolith/plugins` and add the package override compose file.

Example:

```powershell
dotnet run --project src/Cephalon.Cli -- package stage --package artifacts/packages-release/Cephalon.ReferenceModule.Operations.0.1.0-preview.nupkg --output samples/Cephalon.Sample.ModularMonolith/plugins/operations
docker compose -f samples/Cephalon.Sample.ModularMonolith/compose.yaml -f samples/Cephalon.Sample.ModularMonolith/compose.packages.yaml up --build
```

The override mounts `./plugins` into `/app/plugins` and enables `Engine:Discovery:PackageDirectories` plus the package metadata requirements that match the staged package manifest flow.

After startup, inspect:

- `http://localhost:8080/engine/packages`
- `http://localhost:8080/engine/package-policy`
- `http://localhost:8080/engine/trust-policy`
- `http://localhost:8080/api/operations/status`

## Generated-app baseline

`cephalon new` and the app-focused `dotnet new` starters now emit:

- `NuGet.config`
- `.cephalon/packages/README.md`
- `.dockerignore`
- `Dockerfile`
- `compose.yaml`
- `otel-collector-config.yaml`

That means a newly generated host can use the same local command from its own root:

```powershell
docker compose up --build
```

Before the first build, either publish repo-local Cephalon packages into `./.cephalon/packages` or replace the `cephalon` source in `NuGet.config` with your shared package feed. The generated app baseline intentionally keeps `Engine:Observability:Telemetry` active in `Configurations/AddEngine.Observability.json` but leaves the endpoint unset, so local `dotnet run` stays quiet unless a host deliberately adds an endpoint or self-hosted default. If a team prefers the standard project path, they can still override those defaults in `appsettings.json` or `appsettings.{Environment}.json`. The compose file supplies the OTLP endpoint through environment variables instead.

If you also want the non-container published-output baseline for that same generated host, continue with `docs/generated-app-publishing.md`.
If you want the self-hosted Linux `systemd` baseline for that same generated host, continue with `docs/linux-systemd-deployment.md`.

## Optional smoke validation script

For a single-command replay, run:

```powershell
pwsh ./scripts/validate-container-runtime.ps1
```

That script:

- starts the compose stack
- waits for collector health plus `/health/ready`, `/engine`, `/engine/snapshot`, and `/api/catalog/overview`
- when `-UsePackages` is enabled, also verifies that `/engine/packages` contains `reference-operations` and that `/api/operations/status` responds from the staged package
- tears the stack down afterward

Use `-UsePackages` when you also want the compose package-directory override.

This script is intentionally optional. `scripts/validate-release.ps1` still stays Docker-free so Docker is not a hard requirement for every repo validation environment.
