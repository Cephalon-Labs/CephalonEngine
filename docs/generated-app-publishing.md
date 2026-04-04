# Generated App Publishing

This guide is the published-output baseline for freshly generated Cephalon apps.

Use it when you want to prove that a generated host can move from scaffolded source to a deterministic publish folder and then run from the published output without relying on the SDK build loop or the Docker build stage.

## What ships now

Generated app roots from `cephalon new` and the app-focused `dotnet new cephalon-*` starters now include:

- `NuGet.config`
- `.cephalon/packages/README.md`
- `Properties/PublishProfiles/CephalonFolder.pubxml`
- `deploy/container-image/README.md`
- `deploy/container-image/publish-image.ps1`
- `deploy/windows-service/README.md`
- `deploy/windows-service/install-service.ps1`
- `deploy/windows-service/remove-service.ps1`
- `deploy/iis/README.md`
- `deploy/iis/install-site.ps1`
- `deploy/iis/remove-site.ps1`
- `deploy/azure-app-service/README.md`
- `deploy/azure-app-service/deploy-zip.ps1`
- `deploy/azure-container-apps/README.md`
- `deploy/azure-container-apps/deploy-up.ps1`
- `deploy/kubernetes/README.md`
- `deploy/kubernetes/apply.ps1`
- `deploy/kubernetes/kustomization.yaml`
- `deploy/kubernetes/namespace.yaml`
- `deploy/kubernetes/deployment.yaml`
- `deploy/kubernetes/service.yaml`
- `deploy/linux/systemd/README.md`
- `deploy/linux/systemd/<AppName>.service`
- `deploy/linux/systemd/<AppName>.env`
- `.dockerignore`
- `Dockerfile`
- `compose.yaml`
- `otel-collector-config.yaml`

That means the published-output path is now part of the generated app contract rather than an implicit `dotnet publish` guess.
The Azure Container Apps and Kubernetes assets ship alongside that same generated host root, but they stay source/Dockerfile-based rather than depending on the published-output folder.

## Prerequisites

- a generated app root from `cephalon new` or `dotnet new cephalon-*`
- `Cephalon*` packages available either through `./.cephalon/packages` or a shared package source that replaces the generated `cephalon` feed entry

If you are iterating from the Cephalon repository before packages land on a shared feed, seed the generated app root with:

```powershell
$generatedRoot = (Resolve-Path ./Acme.Store).Path
pwsh ./scripts/publish-package-artifacts.ps1 -OutputPath (Join-Path $generatedRoot '.cephalon/packages')
```

## Publish the host

For scaffolded multi-project apps from `cephalon new`:

```powershell
dotnet publish ./Acme.Store/src/Acme.Store.Host/Acme.Store.Host.csproj -p:PublishProfile=CephalonFolder
```

For app-starter templates from `dotnet new cephalon-*`:

```powershell
dotnet publish ./Acme.Store.TemplateStarter/Acme.Store.TemplateStarter.csproj -p:PublishProfile=CephalonFolder
```

`CephalonFolder.pubxml` keeps the publish output deterministic:

- scaffolded multi-project apps publish to `./artifacts/publish/<HostProjectName>/`
- single-project template starters publish to `./artifacts/publish/<ProjectName>/`
- `UseAppHost=false` stays on so the published output is easy to replay across Windows, WSL, and Linux-class shells

## Run the published output

Example for a scaffolded modular-monolith host:

```powershell
dotnet ./Acme.Store/artifacts/publish/Acme.Store.Host/Acme.Store.Host.dll --urls http://127.0.0.1:18080
```

Once the published host is running, inspect:

- `/engine`
- `/engine/snapshot`
- `/engine/runtime-story`
- `/health`
- `/health/ready`
- `/scalar`

Those routes should behave the same way they do under `dotnet run` and under the generated Docker compose path.

From this published-output baseline, teams can continue into the shipped Windows Service, IIS, Azure App Service, or Linux `systemd` install surfaces without changing the generated host shape. For the source/Dockerfile-based hosted container paths, continue with the generated `deploy/container-image/*` assets and [Container image publishing](container-image-publishing.md), then into the generated `deploy/azure-container-apps/*` assets and [Azure Container Apps deployment](azure-container-apps-deployment.md), or with the generated `deploy/kubernetes/*` assets and [Kubernetes deployment](kubernetes-deployment.md).

## Optional repo-native validation script

To replay the full generated-app publish baseline from the Cephalon repository in one command, run:

```powershell
pwsh ./scripts/validate-generated-app-publish.ps1
```

That script:

- scaffolds a fresh app through `Cephalon.Cli`
- seeds `./.cephalon/packages` with repo-local package artifacts
- publishes the generated host through `CephalonFolder.pubxml`
- runs the published output on a local HTTP URL
- validates `/health/ready`, `/engine`, `/engine/snapshot`, and `/scalar`
- tears the temporary app back down afterward unless `-KeepOutput` is supplied

## Related docs

- [Getting started](getting-started.md)
- [Container runtime](container-runtime.md)
- [Container image publishing](container-image-publishing.md)
- [Windows Service deployment](windows-service-deployment.md)
- [IIS deployment](iis-deployment.md)
- [Azure App Service deployment](azure-app-service-deployment.md)
- [Azure Container Apps deployment](azure-container-apps-deployment.md)
- [Kubernetes deployment](kubernetes-deployment.md)
- [Linux systemd deployment](linux-systemd-deployment.md)
- [Package publishing](package-publishing.md)
- [Operations](operations.md)
