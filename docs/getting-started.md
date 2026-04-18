# Getting Started

This guide is the first-run adoption path for teams that want one answer for "is this machine ready for Cephalon?"

The recommended flow is:

1. install `Cephalon.Cli`
2. run `cephalon doctor`
3. scaffold an app
4. run the generated host
5. inspect the engine and health endpoints

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

Expected success characteristics:

- required SDK/runtime checks show `[ok]`
- missing template-pack support shows `[warn]` rather than blocking `cephalon new`
- the command finishes with next steps for `cephalon new`, `dotnet run`, and the runtime inspection routes

If the command reports a required failure, fix that issue first and rerun `cephalon doctor` before generating a host.

## Scaffold A Host

Generate the default modular monolith shape:

```powershell
./.tools/cephalon/cephalon new Acme.Store --output ./Acme.Store
```

That produces a host project at `./Acme.Store/src/Acme.Store.Host/Acme.Store.Host.csproj`.
The generated app root now also includes `NuGet.config`, `./.cephalon/packages/README.md`, `deploy/windows-service/README.md`, `deploy/windows-service/install-service.ps1`, `deploy/windows-service/remove-service.ps1`, `deploy/iis/README.md`, `deploy/iis/install-site.ps1`, `deploy/iis/remove-site.ps1`, `deploy/azure-app-service/README.md`, `deploy/azure-app-service/deploy-zip.ps1`, `deploy/container-image/README.md`, `deploy/container-image/publish-image.ps1`, `deploy/azure-container-apps/README.md`, `deploy/azure-container-apps/deploy-up.ps1`, `deploy/kubernetes/README.md`, `deploy/kubernetes/apply.ps1`, `deploy/kubernetes/kustomization.yaml`, `deploy/kubernetes/namespace.yaml`, `deploy/kubernetes/deployment.yaml`, `deploy/kubernetes/service.yaml`, `deploy/linux/systemd/README.md`, `deploy/linux/systemd/Acme.Store.service`, `deploy/linux/systemd/Acme.Store.env`, `.dockerignore`, `Dockerfile`, `compose.yaml`, and `otel-collector-config.yaml`.
The generated test project also starts with `Architecture/CompositionSmokeTests.cs` plus per-feature `Features/*BehaviorSpecifications.cs` placeholders so you can move straight into composition checks and Given/When/Then-style business behavior without building a test harness from scratch.
Generated hosts now also start with canonical kebab-case `Engine` ids plus structured `Engine:Data`, `Engine:Identity`, `Engine:Tenancy`, `Engine:Audit`, and `Engine:Messaging` sections. The narrow starter baseline keeps `Identity`, `Tenancy`, and `Messaging` dormant, enables `Audit`, and defaults the data id strategy to `Sfid` so teams can grow into richer phase-8 packs without rewriting host startup.

## Seed The Generated Package Feed

Generated apps restore `Cephalon*` packages from `./.cephalon/packages` by default.

When you are iterating from this repository before packages land on a shared feed, publish the repo-local package set into that folder:

```powershell
$generatedRoot = (Resolve-Path ./Acme.Store).Path
pwsh ./scripts/publish-package-artifacts.ps1 -OutputPath (Join-Path $generatedRoot '.cephalon/packages') -SkipBuild
```

If your team already publishes Cephalon packages to a shared source instead, replace the `cephalon` source in `./Acme.Store/NuGet.config` and skip the repo-local artifact step.

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
- `/engine/modules`
- `/engine/packages`
- `/health`
- `/health/ready`
- `/scalar`

These routes give adopters an immediate answer for what loaded, what the runtime believes is active, and whether the host is healthy.
When you later turn on broader phase-8 packs such as eventing, data persistence, or identity/tenancy, the same runtime surface family will also light up `/engine/inboxes`, `/engine/outboxes`, `/engine/projections`, and richer `event-driven-integration` entries inside `/engine/technology-surfaces` and `/engine/snapshot`.

## Optional Container Path

The generated scaffold now ships a Docker Desktop / WSL-friendly compose baseline too:

```powershell
docker compose -f ./Acme.Store/compose.yaml up --build
```

That runs the generated host on `http://localhost:8080` with a local OTLP collector sidecar. Seed `./Acme.Store/.cephalon/packages` or repoint `./Acme.Store/NuGet.config` before the first build. Inspect the same `/engine/*`, `/health/*`, and `/scalar` routes against that containerized host.

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
The template pack also ships module starters, including `cephalon-module`, `cephalon-rest-module`, and `cephalon-rest-behavior-module`, so package authors can start from either a host-agnostic module, a generic REST module, or the recommended behavior-backed REST module path without leaving the `dotnet new` flow.

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
- [Operations](operations.md)
- [Package publishing](package-publishing.md)
