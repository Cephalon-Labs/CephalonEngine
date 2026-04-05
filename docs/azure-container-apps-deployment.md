# Azure Container Apps deployment

This guide is the Azure Container Apps source-deployment baseline for freshly generated Cephalon apps.

Use it when you want a generated host to move from scaffolded source into a hosted Azure Container Apps baseline through the shipped Dockerfile and `az containerapp up --source`, without inventing a separate image-packaging workflow first.

## What ships now

Generated app roots from `cephalon new` and the app-focused `dotnet new cephalon-*` starters now include:

- `NuGet.config`
- `.cephalon/packages/README.md`
- `Dockerfile`
- `deploy/azure-container-apps/README.md`
- `deploy/azure-container-apps/deploy-up.ps1`

The generated deployment script validates the source root, keeps ingress external by default, targets port `8080`, and deploys the app through `az containerapp up --source`.

## Prerequisites

- a generated app root from `cephalon new` or `dotnet new cephalon-*`
- `Cephalon*` packages available either through `./.cephalon/packages` or a shared package source that replaces the generated `cephalon` feed entry
- Azure CLI installed on the machine that performs the deploy
- `az login` already completed for the target subscription

If you want to locally validate the generated Dockerfile before the Azure deploy, keep Docker Desktop or another compatible Docker engine available too.
If you want a provider-neutral registry build/tag/push step first, continue with [Container image publishing](container-image-publishing.md).

## Preview the Azure deploy contract

Example for a scaffolded modular-monolith app:

```powershell
pwsh ./Acme.Store/deploy/azure-container-apps/deploy-up.ps1 `
  -ResourceGroupName my-resource-group `
  -Location eastus `
  -AppName my-cephalon-app `
  -Preview
```

The preview step only validates the generated source assets and prints the Azure CLI command. It does not contact Azure.

If you want to pin the deployment to an existing Container Apps environment, also pass `-ContainerAppEnvironment my-container-apps-env`.

## Deploy to Azure Container Apps

When you are ready to deploy for real:

```powershell
az login
pwsh ./Acme.Store/deploy/azure-container-apps/deploy-up.ps1 `
  -ResourceGroupName my-resource-group `
  -Location eastus `
  -AppName my-cephalon-app `
  -ContainerAppEnvironment my-container-apps-env
```

The generated script:

- validates that the generated app root still contains `Dockerfile` and `NuGet.config`
- deploys from the generated source root through `az containerapp up --source`
- configures ingress as `external`
- sets the target port to `8080`
- passes `ASPNETCORE_HTTP_PORTS=8080` and `DOTNET_ENVIRONMENT=Production`

If you need extra runtime configuration, pass additional `-EnvironmentVariables key=value` entries when you invoke the script.

## Inspect the running host

Once the app is deployed, inspect:

- `/engine`
- `/engine/snapshot`
- `/engine/runtime-story`
- `/health`
- `/health/ready`
- `/scalar`

These routes should behave the same way they do under `dotnet run`, the published-output smoke path, and the local container path.

## Optional repo-native validation script

To replay the generated Azure Container Apps baseline from the Cephalon repository in one command, run:

```powershell
pwsh ./scripts/validate-generated-app-container-apps.ps1
```

That script:

- scaffolds a fresh app through `Cephalon.Cli`
- seeds `./.cephalon/packages` with repo-local package artifacts
- validates that the generated app root contains the shipped Docker and Azure deploy assets
- builds the generated container image locally unless `-SkipDockerBuild` is supplied
- replays the shipped Container Apps deploy script in preview mode
- tears the temporary app back down afterward unless `-KeepOutput` is supplied

## Related docs

- [Getting started](getting-started.md)
- [Generated app publishing](generated-app-publishing.md)
- [Container image publishing](container-image-publishing.md)
- [Container runtime](container-runtime.md)
- [Azure App Service deployment](azure-app-service-deployment.md)
- [Windows Service deployment](windows-service-deployment.md)
- [IIS deployment](iis-deployment.md)
- [Linux systemd deployment](linux-systemd-deployment.md)
- [Operations](operations.md)
