# Azure App Service deployment

These assets provide the Azure App Service run-from-package baseline for `CephalonTemplateApp` after the host is published through `CephalonFolder.pubxml`.

The generated deployment script packages the published output into `azure-app-service.zip`, sets `WEBSITE_RUN_FROM_PACKAGE=1`, and deploys the ZIP artifact through `az webapp deploy`.

The generated deploy script assumes:

- published output lives at `./artifacts/publish/CephalonTemplateApp/`
- Azure CLI is installed on the deployment machine
- `az login` has already been completed for the target subscription
- the target App Service app already exists

Files in this folder:

- `deploy-zip.ps1`

From the generated app root, publish the host with:

```powershell
dotnet publish CephalonTemplateApp.csproj -p:PublishProfile=CephalonFolder
```

Preview the Azure deployment contract locally with:

```powershell
pwsh ./deploy/azure-app-service/deploy-zip.ps1 -ResourceGroupName my-resource-group -AppName my-cephalon-app -Preview
```

When you are ready to deploy for real:

```powershell
az login
pwsh ./deploy/azure-app-service/deploy-zip.ps1 -ResourceGroupName my-resource-group -AppName my-cephalon-app
```

The generated ZIP package is written to `./artifacts/deploy/CephalonTemplateApp/azure-app-service.zip`.

Then inspect the running host with `/engine`, `/engine/snapshot`, `/health/ready`, and `/scalar`.
