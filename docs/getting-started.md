# Getting Started

This guide is the first-run adoption path for teams that want one answer for "is this machine ready for Cephalon?"

The recommended flow is:

1. install `Cephalon.Cli`
2. run `cephalon doctor`
3. scaffold an app
4. validate the generated app bootstrap
5. run the generated host
6. inspect the engine and health endpoints

## Prerequisites

- a .NET 10 SDK on the machine
- `Microsoft.NETCore.App` 10.x and `Microsoft.AspNetCore.App` 10.x available through that SDK or a matching runtime install
- a package source that contains `Cephalon.Cli` and, if you want the `dotnet new` path, `Cephalon.TemplatePack`

Inside this repository, you can create a local package source with:

```powershell
pwsh ./scripts/publish-package-artifacts.ps1 -Configuration Release -SkipBuild
```

That produces `./artifacts/packages-release`, which works as the `<package-source>` in the examples below.

## Install The CLI

Install the CLI into a local tool path:

```powershell
dotnet tool install --tool-path ./.tools/cephalon Cephalon.Cli --add-source <package-source> --ignore-failed-sources --no-cache --prerelease
./.tools/cephalon/cephalon doctor
```

If you are running the repo-local artifact flow, replace `<package-source>` with `./artifacts/packages-release`.

## Run Cephalon Doctor

`cephalon doctor` verifies the first-run baseline from the current shell:

- the active `dotnet --version` selection is a 10.x SDK
- a 10.x SDK family is installed
- `Microsoft.NETCore.App` 10.x is installed
- `Microsoft.AspNetCore.App` 10.x is installed
- the optional Cephalon template pack is available through `dotnet new`
- the packaged deployment-mode support contract is echoed back so adopters can see the stable shipping floor, the `.NET 11` readiness lane, and the current trim / Native AOT / single-file posture from one command

Expected success characteristics:

- required SDK/runtime checks show `[ok]`
- missing template-pack support shows `[warn]` rather than blocking `cephalon new`
- the deployment-mode shipping baseline shows `[ok]` for `net10.0` plus the `.NET 11` `assessment-only` readiness lane
- trim, Native AOT, and single-file support stay visible as `[warn]` while they remain outside the current support contract
- the command finishes with next steps for `cephalon new`, `dotnet run`, and the runtime inspection routes

If the command reports a required failure, fix that issue first and rerun `cephalon doctor` before generating a host.

## Scaffold A Host

Generate the default modular monolith shape:

```powershell
./.tools/cephalon/cephalon new Acme.Store --output ./Acme.Store
```

That produces a host project at `./Acme.Store/src/Acme.Store.Host/Acme.Store.Host.csproj`.
The generated app root now also includes `README.md`, `NuGet.config`, `./.cephalon/packages/README.md`, `src/Acme.Store.Host/Configurations/README.md`, `deploy/windows-service/README.md`, `deploy/windows-service/install-service.ps1`, `deploy/windows-service/remove-service.ps1`, `deploy/iis/README.md`, `deploy/iis/install-site.ps1`, `deploy/iis/remove-site.ps1`, `deploy/azure-app-service/README.md`, `deploy/azure-app-service/deploy-zip.ps1`, `deploy/container-image/README.md`, `deploy/container-image/publish-image.ps1`, `deploy/azure-container-apps/README.md`, `deploy/azure-container-apps/deploy-up.ps1`, `deploy/kubernetes/README.md`, `deploy/kubernetes/apply.ps1`, `deploy/kubernetes/kustomization.yaml`, `deploy/kubernetes/namespace.yaml`, `deploy/kubernetes/deployment.yaml`, `deploy/kubernetes/service.yaml`, `deploy/linux/systemd/README.md`, `deploy/linux/systemd/Acme.Store.service`, `deploy/linux/systemd/Acme.Store.env`, `.dockerignore`, `Dockerfile`, `compose.yaml`, and `otel-collector-config.yaml`.
The generated test project also starts with `Architecture/CompositionSmokeTests.cs` plus per-feature `Features/*BehaviorSpecifications.cs` placeholders so you can move straight into composition checks and Given/When/Then-style business behavior without building a test harness from scratch.
Generated hosts now also start with canonical kebab-case `Engine` ids plus structured `Engine:Data`, `Engine:Identity`, `Engine:Tenancy`, `Engine:Audit`, and `Engine:Messaging` sections. The narrow starter baseline keeps `Identity`, `Tenancy`, and `Messaging` dormant, enables `Audit`, and defaults the data id strategy to `Sfid` so teams can grow into richer phase-8 packs without rewriting host startup.
When `RestApi` is enabled, the generated public starter modules now also derive from `RestBehaviorModuleBase` and publish their public surface in `ConfigureRestBehaviors(...)` via `MapProfile<TBehavior>()`, so the scaffold output starts from Cephalon's settled module-owned behavior-backed REST baseline instead of generic endpoint modules.

## Seed The Generated Package Feed

Generated apps restore `Cephalon*` packages from `./.cephalon/packages` by default.

When you are iterating from this repository before packages land on a shared feed, publish the repo-local package set into that folder:

```powershell
$generatedRoot = (Resolve-Path ./Acme.Store).Path
pwsh ./scripts/publish-package-artifacts.ps1 -OutputPath (Join-Path $generatedRoot '.cephalon/packages') -SkipBuild
```

If your team already publishes Cephalon packages to a shared source instead, replace the `cephalon` source in `./Acme.Store/NuGet.config` and skip the repo-local artifact step.

## Validate The Generated App Bootstrap

After scaffolding, rerun doctor against the generated app root:

```powershell
./.tools/cephalon/cephalon doctor --app-root ./Acme.Store
```

`cephalon doctor --app-root ./Acme.Store` keeps the machine baseline checks, then also verifies that the generated app still has:

- `Acme.Store.slnx`
- `Directory.Packages.props` with `Cephalon*` package versions
- `NuGet.config` with a `cephalon` package source
- a seeded local `./.cephalon/packages` feed or a non-local shared feed
- a generated host project under `src/`
- the generated `Program.cs` host bootstrap source with explicit `AddCephalonProjectConfigurations`, observability wiring, and `MapCephalon` flow
- the generated host-project `PackageReference` baseline plus `Configurations/**/*.json` copy/publish settings
- a generated test project under `tests/`
- the generated `Architecture/CompositionSmokeTests.cs` plus `Features/*BehaviorSpecifications.cs` placeholders so the starter composition and Given/When/Then test harness stays explicit
- `Properties/PublishProfiles/CephalonFolder.pubxml`
- the generated host target framework against the stable `net10.0` shipping floor and the `.NET 11` `assessment-only` readiness lane
- the generated split-config assets `Configurations/AddEngine.*.json` and `Configurations/Observability/Development.json`
- the generated app-model, engine feature, observability, localization, and development Serilog baselines so split project configuration stays explicit before teams rely on runtime, docs, or telemetry defaults
- the generated guidance docs `README.md`, `./.cephalon/packages/README.md`, `src/<Host>/Configurations/README.md`, and `deploy/*/README.md` so the human-facing package-feed, run, publish, and deploy instructions stay explicit before teams follow them literally
- the generated documentation-surface assets `Configurations/AddOpenApi.json` and `Configurations/AddReferenceDocs.json`
- the generated OpenAPI title baseline so `/openapi/*` and `/scalar` stay explicit in split project settings
- the generated hosted reference-doc route, directory, default-document, and enablement baseline so optional hosted reference docs stay explicit before that route is turned on
- the generated Windows Service, IIS, Azure App Service, and Linux `systemd` deployment assets that back the published-output self-hosted and hosted deployment paths
- the generated Windows Service and IIS teardown-script baselines in `deploy/windows-service/remove-service.ps1` and `deploy/iis/remove-site.ps1`, plus the generated Linux `systemd` environment baseline in `deploy/linux/systemd/Acme.Store.env`
- the generated `Dockerfile` plus the shipped container-image, Azure Container Apps, and Kubernetes deployment assets
- the generated `deploy/container-image/publish-image.ps1`, `deploy/azure-container-apps/deploy-up.ps1`, and `deploy/kubernetes/apply.ps1` script baselines against the current Dockerfile, `NuGet.config`, image placeholder, host-project/source-root, namespace, and manifest-root contract
- the generated Kubernetes manifest baselines in `deploy/kubernetes/kustomization.yaml`, `deploy/kubernetes/namespace.yaml`, `deploy/kubernetes/deployment.yaml`, and `deploy/kubernetes/service.yaml` against the shipped namespace, label, image, probe, env, and service contract
- the generated Dockerfile SDK/runtime image tags against the generated host target framework baseline
- the generated local orchestration assets `compose.yaml` and `otel-collector-config.yaml`
- the generated compose baseline against the shipped Dockerfile, OTLP collector handoff, and current local container defaults
- the generated OpenTelemetry collector baseline against `health_check`, `otlp/http` on `4318`, and the debug-exporter pipeline shape
- generated `PublishTrimmed`, `PublishAot`, and `PublishSingleFile` posture against the current `not-claimed` support contract

Expected success characteristics:

- the generated-app checks show `[ok]`
- a generated host that stays on `net10.0` shows `[ok]` for the stable shipping floor
- a generated host that keeps the scaffolded `Program.cs` bootstrap explicit shows `[ok]` for the generated host bootstrap source baseline and keeps `AddCephalonProjectConfigurations` plus `MapCephalon` visible before teams edit startup
- a generated host that keeps the scaffolded host-project `PackageReference` set plus `Configurations/**/*.json` copy/publish behavior intact shows `[ok]` for the generated host project baseline before teams rely on split project configuration at build or publish time
- a generated app that keeps the scaffolded test project plus `Architecture/CompositionSmokeTests.cs` and `Features/*BehaviorSpecifications.cs` placeholders intact shows `[ok]` for generated test project and generated test harness baseline before teams replace the starter specs with real composition and Given/When/Then behavior checks
- a generated host that keeps the scaffolded `Configurations/AddEngine.*.json` files plus `Configurations/Observability/Development.json` intact shows `[ok]` for generated split configuration assets plus the app-model, engine-feature, observability, localization, and development-observability baseline checks
- a generated app that keeps the scaffolded root README, `./.cephalon/packages/README.md`, `Configurations/README.md`, and `deploy/*/README.md` guides intact shows `[ok]` for generated guidance docs assets plus the root/local-package-feed/configuration/deployment guide baselines before teams rely on the scaffolded human-facing instructions
- a generated host that keeps the scaffolded `Configurations/AddOpenApi.json` and `Configurations/AddReferenceDocs.json` files intact shows `[ok]` for generated documentation surface assets plus the OpenAPI and hosted reference-doc baseline checks
- a generated host that keeps the scaffolded Windows Service, IIS, Azure App Service, Linux `systemd`, and container deployment assets shows `[ok]` for self-hosted and hosted deployment assets plus Dockerfile alignment
- a generated host that keeps the scaffolded `deploy/windows-service/remove-service.ps1`, `deploy/iis/remove-site.ps1`, and `deploy/linux/systemd/Acme.Store.env` files aligned with the shipped teardown and environment contract shows `[ok]` for the generated Windows Service teardown, IIS teardown, and Linux `systemd` environment baselines
- a generated host that keeps the scaffolded `deploy/container-image/publish-image.ps1`, `deploy/azure-container-apps/deploy-up.ps1`, and `deploy/kubernetes/apply.ps1` scripts aligned with the generated Dockerfile, `NuGet.config`, host-project/source-root, image placeholder, namespace, and manifest-root contract shows `[ok]` for the generated container deployment-script baselines
- a generated host that keeps the scaffolded `deploy/kubernetes/kustomization.yaml`, `deploy/kubernetes/namespace.yaml`, `deploy/kubernetes/deployment.yaml`, and `deploy/kubernetes/service.yaml` manifests aligned with the shipped namespace, label, image, probe, env, and `ClusterIP` service contract shows `[ok]` for the generated Kubernetes manifest baselines
- a generated host that keeps the scaffolded local orchestration assets plus the shipped `compose.yaml` and `otel-collector-config.yaml` baseline shows `[ok]` for generated local orchestration assets and the compose plus collector baseline checks
- a generated host on `net11.0` or publish settings that enable trim / Native AOT / single-file show `[warn]` so teams can see readiness-only or out-of-contract posture before publish and deployment work
- a generated app that drops `Program.cs`, removes the generated `AddCephalonProjectConfigurations` or `MapCephalon` bootstrap flow, drifts the generated host-project `PackageReference` set or `Configurations/**/*.json` copy/publish settings, drops the generated test project, removes `Architecture/CompositionSmokeTests.cs`, strips the starter `Features/*BehaviorSpecifications.cs` placeholders, drops the generated split-config assets, drifts the generated app-model, engine feature, observability, localization, or development Serilog baseline, drops the generated guidance docs or the local package-feed guide, drops the generated documentation-surface assets, clears the generated OpenAPI title, clears the hosted reference-doc route or directory settings, drops the shipped self-hosted and hosted deployment assets, drifts `deploy/windows-service/remove-service.ps1`, `deploy/iis/remove-site.ps1`, or `deploy/linux/systemd/Acme.Store.env`, drops the container deployment assets, drifts `deploy/container-image/publish-image.ps1`, `deploy/azure-container-apps/deploy-up.ps1`, or `deploy/kubernetes/apply.ps1`, drifts `deploy/kubernetes/kustomization.yaml`, `deploy/kubernetes/namespace.yaml`, `deploy/kubernetes/deployment.yaml`, or `deploy/kubernetes/service.yaml`, drops the generated local orchestration assets, drifts `compose.yaml` or `otel-collector-config.yaml`, or retargets Docker base images away from the host baseline shows `[error]`
- a generated host that targets something outside the current contract shows `[error]`
- the command ends with `Set-Location`, `dotnet restore`, and `dotnet run` next steps that are copy/paste-ready for that app root

If the command reports a generated-app failure, fix the missing bootstrap asset, generated split-config asset or baseline drift, generated guidance docs or local package-feed guidance drift, generated documentation-surface asset, generated self-hosted and hosted deployment assets, generated teardown or environment baseline drift, generated container deployment-script or Kubernetes manifest baseline drift, generated local orchestration assets, compose or collector baseline drift, or package-source problem first, then rerun `cephalon doctor --app-root ./Acme.Store` before build, publish, or deployment work.

## Repo-native cold-start replay

If you want one repo-native replay of the external-adoption path from a clean temporary workspace, run:

```powershell
pwsh ./scripts/validate-generated-app-adoption.ps1
```

That script:

- publishes a temporary repo-local package feed
- installs `Cephalon.Cli` into a temporary tool path with `dotnet tool install`
- runs `cephalon doctor`
- scaffolds a fresh app outside the repository
- seeds the generated `./.cephalon/packages` folder while keeping the generated `README.md` guidance intact
- reruns `cephalon doctor --app-root <path>`
- runs `dotnet restore`, `dotnet build`, and `dotnet run`
- validates `/health/ready`, `/engine`, `/engine/snapshot`, and `/scalar`

## Optional Published-Output Path

Generated host projects now also include `Properties/PublishProfiles/CephalonFolder.pubxml`.

Publish the generated host with:

```powershell
dotnet publish ./Acme.Store/src/Acme.Store.Host/Acme.Store.Host.csproj -p:PublishProfile=CephalonFolder
```

That writes the host to `./Acme.Store/artifacts/publish/Acme.Store.Host/`.

To replay the published output locally:

```powershell
dotnet ./Acme.Store/artifacts/publish/Acme.Store.Host/Acme.Store.Host.dll --urls http://127.0.0.1:18080
```

Inspect the same `/engine/*`, `/health/*`, and `/scalar` routes against that published host.

For a repo-native one-command replay of scaffold -> seed packages -> publish -> run published output -> probe routes, use:

```powershell
pwsh ./scripts/validate-generated-app-publish.ps1
```

## Optional Windows Service Path

Generated apps now also include `deploy/windows-service/README.md`, `deploy/windows-service/install-service.ps1`, and `deploy/windows-service/remove-service.ps1`.

Use those files after the published-output step when you want a self-hosted Windows Service baseline for a Windows VM or bare-metal host without inventing your own `sc.exe` contract first.

Continue with [Windows Service deployment](windows-service-deployment.md) for the preview, install, verify, and removal flow.

For a repo-native replay of scaffold -> seed packages -> publish -> preview the generated Windows Service install/remove scripts, use:

```powershell
pwsh ./scripts/validate-generated-app-windows-service.ps1
```

## Optional IIS Path

Generated apps now also include `deploy/iis/README.md`, `deploy/iis/install-site.ps1`, and `deploy/iis/remove-site.ps1`.

Use those files after the published-output step when you want a hosted Windows baseline for IIS site plus app-pool deployment without rediscovering the ASP.NET Core Module handoff or `appcmd.exe` flow.

Continue with [IIS deployment](iis-deployment.md) for the preview, install, verify, and removal flow.

For a repo-native replay of scaffold -> seed packages -> publish -> preview the generated IIS install/remove scripts against the published output, use:

```powershell
pwsh ./scripts/validate-generated-app-iis.ps1
```

## Optional Azure App Service Path

Generated apps now also include `deploy/azure-app-service/README.md` and `deploy/azure-app-service/deploy-zip.ps1`.

Use those files after the published-output step when you want a hosted Azure App Service baseline without rediscovering the ZIP packaging path, `WEBSITE_RUN_FROM_PACKAGE=1`, or the current `az webapp deploy` contract.

Continue with [Azure App Service deployment](azure-app-service-deployment.md) for the preview, package, and deploy flow.

For a repo-native replay of scaffold -> seed packages -> publish -> package the generated host -> preview the Azure CLI deploy contract, use:

```powershell
pwsh ./scripts/validate-generated-app-app-service.ps1
```

## Optional Container Image Path

Generated apps now also include `deploy/container-image/README.md` and `deploy/container-image/publish-image.ps1`.

Use those files from the generated app root when you want a provider-neutral build/tag/push image baseline from the shipped Dockerfile before you hand the image to Kubernetes or another hosted container platform.

Continue with [Container image publishing](container-image-publishing.md) for the preview, build, and push flow.

For a repo-native replay of scaffold -> seed packages -> preview the generated build/push contract -> build the generated image -> prove push against a local registry, use:

```powershell
pwsh ./scripts/validate-generated-app-container-image.ps1
```

## Optional Azure Container Apps Path

Generated apps now also include `deploy/azure-container-apps/README.md` and `deploy/azure-container-apps/deploy-up.ps1`.

Use those files from the generated app root when you want a hosted Azure Container Apps baseline from the shipped Dockerfile and source tree without first publishing a folder output.

Continue with [Azure Container Apps deployment](azure-container-apps-deployment.md) for the preview and deploy flow.

For a repo-native replay of scaffold -> seed packages -> validate the generated Dockerfile locally -> preview the generated Azure CLI source-deploy contract, use:

```powershell
pwsh ./scripts/validate-generated-app-container-apps.ps1
```

## Optional Kubernetes Path

Generated apps now also include `deploy/kubernetes/README.md`, `deploy/kubernetes/apply.ps1`, `deploy/kubernetes/kustomization.yaml`, `deploy/kubernetes/namespace.yaml`, `deploy/kubernetes/deployment.yaml`, and `deploy/kubernetes/service.yaml`.

Use those files from the generated app root when you want a platform-neutral Kubernetes baseline from the shipped Dockerfile and source tree without inventing a manifest set from scratch.

Continue with [Kubernetes deployment](kubernetes-deployment.md) for the image, render, preview, and apply flow.

For a repo-native replay of scaffold -> seed packages -> validate the generated Dockerfile locally -> preview the generated Kubernetes manifest/apply contract, use:

```powershell
pwsh ./scripts/validate-generated-app-kubernetes.ps1
```

## Optional Linux systemd Path

Generated apps now also include `deploy/linux/systemd/README.md`, `deploy/linux/systemd/Acme.Store.service`, and `deploy/linux/systemd/Acme.Store.env`.

Use those files after the published-output step when you want a self-hosted Linux service-manager baseline for a VM, bare-metal host, or a Linux-class environment outside the Docker path.

Continue with [Linux systemd deployment](linux-systemd-deployment.md) for the install, verify, and `systemctl` flow.

For a repo-native replay of scaffold -> seed packages -> publish -> verify the generated unit under WSL `systemd-analyze`, use:

```powershell
pwsh ./scripts/validate-generated-app-systemd.ps1
```

## Run The Generated App

Start the generated host:

```powershell
dotnet run --project ./Acme.Store/src/Acme.Store.Host/Acme.Store.Host.csproj
```

Once the app is running, inspect these routes:

- `/engine`
- `/engine/manifest`
- `/engine/snapshot`
- `/engine/runtime-story`
- `/engine/technology-surfaces`
- `/engine/diagnostics`
- `/engine/cdc-captures`
- `/engine/modules`
- `/engine/packages`
- `/health`
- `/health/ready`
- `/scalar`

These routes give adopters an immediate answer for what loaded, what the runtime believes is active, and whether the host is healthy.
When you later turn on broader phase-8 and phase-13 packs such as eventing, data persistence, or richer module-owned data-sync conventions, the same runtime surface family will also light up `/engine/data-products`, `/engine/cdc-captures`, `/engine/inboxes`, `/engine/outboxes`, `/engine/projections`, and richer `event-driven-integration` entries inside `/engine/technology-surfaces` and `/engine/snapshot`.

## Optional Container Path

The generated scaffold now ships a Docker Desktop / WSL-friendly compose baseline too:

```powershell
docker compose -f ./Acme.Store/compose.yaml up --build
```

That runs the generated host on `http://localhost:8080` with a local OTLP collector sidecar. Seed `./Acme.Store/.cephalon/packages` or repoint `./Acme.Store/NuGet.config` before the first build, then rerun `cephalon doctor --app-root ./Acme.Store` so the generated local orchestration assets plus `compose.yaml` and `otel-collector-config.yaml` baseline stay truthful before the first `docker compose up --build`. Inspect the same `/engine/*`, `/health/*`, and `/scalar` routes against that containerized host.

## Optional Template-Pack Path

If your team prefers `dotnet new`, install and verify the template pack too:

```powershell
dotnet new install Cephalon.TemplatePack --nuget-source <package-source>
dotnet new list cephalon
dotnet new cephalon-monolith -n Acme.Store.TemplateStarter
```

Inside this repository, the repo-local artifact variant is:

```powershell
dotnet new install ./artifacts/packages-release/Cephalon.TemplatePack.0.1.0-preview.nupkg
dotnet new list cephalon
```

`cephalon doctor` should then report the template-pack check as `[ok]`.
The `dotnet new` app starters also emit the same `NuGet.config`, `./.cephalon/packages/README.md`, `deploy/windows-service/README.md`, `deploy/windows-service/install-service.ps1`, `deploy/windows-service/remove-service.ps1`, `deploy/iis/README.md`, `deploy/iis/install-site.ps1`, `deploy/iis/remove-site.ps1`, `deploy/azure-app-service/README.md`, `deploy/azure-app-service/deploy-zip.ps1`, `deploy/container-image/README.md`, `deploy/container-image/publish-image.ps1`, `deploy/azure-container-apps/README.md`, `deploy/azure-container-apps/deploy-up.ps1`, `deploy/kubernetes/README.md`, `deploy/kubernetes/apply.ps1`, `deploy/kubernetes/kustomization.yaml`, `deploy/kubernetes/namespace.yaml`, `deploy/kubernetes/deployment.yaml`, `deploy/kubernetes/service.yaml`, `deploy/linux/systemd/README.md`, `deploy/linux/systemd/<App>.service`, `deploy/linux/systemd/<App>.env`, `.dockerignore`, `Dockerfile`, `compose.yaml`, and `otel-collector-config.yaml` baseline.
The template starters also emit the same structured phase-8 `Engine:Data`, `Engine:Identity`, `Engine:Tenancy`, `Engine:Audit`, and `Engine:Messaging` sections, using canonical ids and the same low-ceremony `Sfid` plus `Audit` starter path as `cephalon new`.
When those app starters include `RestApi`, their public starter modules now also begin on `RestBehaviorModuleBase.ConfigureRestBehaviors(...)` plus `MapProfile<TBehavior>()`, keeping `dotnet new` aligned with the shipped engine-first REST boundary.
The template pack also ships module starters, including `cephalon-module`, `cephalon-rest-module`, and `cephalon-rest-behavior-module`, so package authors can start from either a host-agnostic module, a generic REST module, or the recommended behavior-backed REST module path without leaving the `dotnet new` flow.
For the settled engine-first REST baseline, start with `cephalon-rest-behavior-module` for behavior-backed public APIs, then continue with `docs/module-authoring.md` and `docs/architecture/rest-endpoint-authoring-strategy.md` so route ownership and host governance stay aligned with the shipped runtime model.

## Next Docs

- [Generated app publishing](generated-app-publishing.md)
- [Container image publishing](container-image-publishing.md)
- [Container runtime](container-runtime.md)
- [Windows Service deployment](windows-service-deployment.md)
- [IIS deployment](iis-deployment.md)
- [Azure App Service deployment](azure-app-service-deployment.md)
- [Azure Container Apps deployment](azure-container-apps-deployment.md)
- [Kubernetes deployment](kubernetes-deployment.md)
- [Linux systemd deployment](linux-systemd-deployment.md)
- [App models](app-models.md)
- [Module authoring](module-authoring.md)
- [REST endpoint authoring strategy](architecture/rest-endpoint-authoring-strategy.md)
- [Operations](operations.md)
- [Package publishing](package-publishing.md)
