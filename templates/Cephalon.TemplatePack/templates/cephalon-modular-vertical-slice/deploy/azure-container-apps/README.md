# Azure Container Apps deployment

These assets provide the Azure Container Apps source-deployment baseline for `CephalonTemplateApp` from the generated app root and Dockerfile.

The generated deployment script uses `az containerapp up --source` so the generated host can move from scaffolded source into a hosted Azure Container Apps baseline without inventing a separate image-packaging workflow first.

The generated deploy script assumes:

- the generated app root keeps the shipped `Dockerfile` and `NuGet.config`
- `NuGet.config` points at a reachable Cephalon package source or `./.cephalon/packages` has been seeded before the container image is built
- Azure CLI is installed on the deployment machine
- `az login` has already been completed for the target subscription

If you want a provider-neutral registry build/tag/push step before the Azure deploy, use `../container-image/publish-image.ps1` first.

Files in this folder:

- `deploy-up.ps1`

Preview the Azure Container Apps deployment contract locally with:

```powershell
pwsh ./deploy/azure-container-apps/deploy-up.ps1 -ResourceGroupName my-resource-group -Location eastus -AppName my-cephalon-app -Preview
```

If you want to pin the deployment to an existing Container Apps environment, also pass `-ContainerAppEnvironment my-container-apps-env`.

When you are ready to deploy for real:

```powershell
az login
pwsh ./deploy/azure-container-apps/deploy-up.ps1 -ResourceGroupName my-resource-group -Location eastus -AppName my-cephalon-app -ContainerAppEnvironment my-container-apps-env
```

The generated script deploys from the app root, keeps ingress external by default, targets port `8080`, and passes `ASPNETCORE_HTTP_PORTS=8080` plus `DOTNET_ENVIRONMENT=Production` into the hosted container app.

If you need to extend the runtime environment contract, pass extra `-EnvironmentVariables key=value` entries when you invoke the script.

Then inspect the running host with `/engine`, `/engine/snapshot`, `/health/ready`, and `/scalar`.
