# Azure App Service deployment

This guide is the Azure App Service ZIP deployment baseline for freshly generated Cephalon apps.

Use it when you want a generated host to move from scaffolded source to deterministic published output and then into an Azure App Service deployable package without relying on an IDE publish profile or source-tree deployment.

## What ships now

Generated app roots from `cephalon new` and the app-focused `dotnet new cephalon-*` starters now include:

- `Properties/PublishProfiles/CephalonFolder.pubxml`
- `deploy/azure-app-service/README.md`
- `deploy/azure-app-service/deploy-zip.ps1`

The generated deployment script packages the published output into `azure-app-service.zip`, sets `WEBSITE_RUN_FROM_PACKAGE=1`, and deploys the ZIP artifact through `az webapp deploy`.

## Prerequisites

- a generated app root from `cephalon new` or `dotnet new cephalon-*`
- `Cephalon*` packages available either through `./.cephalon/packages` or a shared package source that replaces the generated `cephalon` feed entry
- Azure CLI installed on the machine that performs the deploy
- `az login` already completed for the target subscription
- an existing Azure App Service web app

If you still need to create the App Service app itself, follow the official ASP.NET Core App Service quickstart first and then return to this ZIP-deploy flow.

## Publish the host

For scaffolded multi-project apps from `cephalon new`:

```powershell
dotnet publish ./Acme.Store/src/Acme.Store.Host/Acme.Store.Host.csproj -p:PublishProfile=CephalonFolder
```

For app-starter templates from `dotnet new cephalon-*`:

```powershell
dotnet publish ./Acme.Store.TemplateStarter/Acme.Store.TemplateStarter.csproj -p:PublishProfile=CephalonFolder
```

## Preview the Azure deploy contract

Example for a scaffolded modular-monolith app:

```powershell
pwsh ./Acme.Store/deploy/azure-app-service/deploy-zip.ps1 `
  -ResourceGroupName my-resource-group `
  -AppName my-cephalon-app `
  -Preview
```

The preview step still builds `./Acme.Store/artifacts/deploy/Acme.Store.Host/azure-app-service.zip`, but it only prints the Azure CLI commands instead of contacting Azure.

## Deploy to Azure App Service

When you are ready to deploy for real:

```powershell
az login
pwsh ./Acme.Store/deploy/azure-app-service/deploy-zip.ps1 `
  -ResourceGroupName my-resource-group `
  -AppName my-cephalon-app
```

The generated script:

- validates that the published output contains the expected host DLL and `web.config`
- creates `azure-app-service.zip` under `./artifacts/deploy/<HostProjectName>/`
- sets `WEBSITE_RUN_FROM_PACKAGE=1`
- deploys the ZIP package through `az webapp deploy --type zip`

If you deploy to a slot, pass `-SlotName <slot-name>` to the script.

## Inspect the running host

Once the app is deployed, inspect:

- `/engine`
- `/engine/snapshot`
- `/engine/runtime-story`
- `/health`
- `/health/ready`
- `/scalar`

These routes should behave the same way they do under `dotnet run`, the published-output smoke path, and the container path.

## Optional repo-native validation script

To replay the generated Azure App Service baseline from the Cephalon repository in one command, run:

```powershell
pwsh ./scripts/validate-generated-app-app-service.ps1
```

That script:

- scaffolds a fresh app through `Cephalon.Cli`
- seeds `./.cephalon/packages` with repo-local package artifacts
- publishes the generated host through `CephalonFolder.pubxml`
- replays the shipped App Service deploy script in preview mode
- verifies that the generated ZIP package contains the expected host DLL and `web.config`
- tears the temporary app back down afterward unless `-KeepOutput` is supplied

## Related docs

- [Getting started](getting-started.md)
- [Generated app publishing](generated-app-publishing.md)
- [Windows Service deployment](windows-service-deployment.md)
- [IIS deployment](iis-deployment.md)
- [Linux systemd deployment](linux-systemd-deployment.md)
- [Operations](operations.md)
