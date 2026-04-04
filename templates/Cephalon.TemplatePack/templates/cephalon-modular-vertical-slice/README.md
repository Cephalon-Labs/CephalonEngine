# CephalonTemplateApp

Generated from the Cephalon modular vertical-slice template.

## Included shape

- Blueprint: `ModularVerticalSlice`
- Project style: single ASP.NET Core host with feature slices inside a bounded module
- Starter slice: `Orders/Checkout`

## Next steps

1. Decide where `Cephalon*` packages should restore from. `NuGet.config` points at `./.cephalon/packages` by default for repo-local package artifacts.
2. Populate `./.cephalon/packages` or replace the `cephalon` source in `NuGet.config` with your published package feed.
3. Update package versions to the Cephalon feed or release you want to target.
4. Add more slices under `Modules/*/Features/`.
5. Keep endpoint, command, query, and policy code close to the feature.
6. Publish reference docs, then flip `ReferenceDocs:Enabled` to `true` when you want the host to serve them.

## Optional published-output path

This starter also includes `Properties/PublishProfiles/CephalonFolder.pubxml`.

From the generated app root:

```powershell
dotnet publish CephalonTemplateApp.csproj -p:PublishProfile=CephalonFolder
```

That writes the host to `./artifacts/publish/CephalonTemplateApp/`.

To replay the published host locally:

```powershell
dotnet ./artifacts/publish/CephalonTemplateApp/CephalonTemplateApp.dll --urls http://127.0.0.1:18080
```

Then inspect `/engine`, `/engine/snapshot`, `/health/ready`, and `/scalar`.

## Optional Windows Service path

This starter also includes:

- `deploy/windows-service/README.md`
- `deploy/windows-service/install-service.ps1`
- `deploy/windows-service/remove-service.ps1`

Use these files after you publish the host into `./artifacts/publish/CephalonTemplateApp/`.

The generated Windows Service baseline assumes:

- published output is copied to `C:\Services\CephalonTemplateApp\current`
- the install script is run from an elevated PowerShell session on the Windows target

See `deploy/windows-service/README.md` for the preview, install, verify, and removal steps.

## Optional IIS path

This starter also includes:

- `deploy/iis/README.md`
- `deploy/iis/install-site.ps1`
- `deploy/iis/remove-site.ps1`

Use these files after you publish the host into `./artifacts/publish/CephalonTemplateApp/`.

The generated IIS baseline assumes:

- published output is copied to `C:\inetpub\sites\CephalonTemplateApp\current`
- the install script is run from an elevated PowerShell session on a Windows host with IIS installed
- the published output keeps the SDK-generated `web.config`

See `deploy/iis/README.md` for the preview, install, verify, and removal steps.

## Optional Azure App Service path

This starter also includes:

- `deploy/azure-app-service/README.md`
- `deploy/azure-app-service/deploy-zip.ps1`

Use these files after you publish the host into `./artifacts/publish/CephalonTemplateApp/`.

The generated Azure App Service baseline assumes:

- published output is packaged into `./artifacts/deploy/CephalonTemplateApp/azure-app-service.zip`
- Azure CLI is installed and authenticated on the machine that performs the deploy
- the target App Service app already exists

See `deploy/azure-app-service/README.md` for the package, preview, and deploy steps.

## Optional container image path

This starter also includes:

- `deploy/container-image/README.md`
- `deploy/container-image/publish-image.ps1`

Use these files from the generated app root when you want a provider-neutral build/tag/push baseline from the shipped Dockerfile before you hand the image to Kubernetes or another hosted container platform.

The generated container-image baseline assumes:

- `NuGet.config` points at a reachable Cephalon package source or `./.cephalon/packages` has been seeded before the container image is built
- Docker Desktop or another compatible Docker engine is installed on the build machine
- `docker login` has already been completed for the target registry before you use `-Push`

See `deploy/container-image/README.md` for the preview, build, and push steps.

## Optional Azure Container Apps path

This starter also includes:

- `deploy/azure-container-apps/README.md`
- `deploy/azure-container-apps/deploy-up.ps1`

Use these files from the generated app root when you want a hosted Azure Container Apps baseline from the shipped Dockerfile and source tree.

The generated Azure Container Apps baseline assumes:

- `NuGet.config` points at a reachable Cephalon package source or `./.cephalon/packages` has been seeded before the container image is built
- Azure CLI is installed and authenticated on the machine that performs the deploy
- the generated app root keeps the shipped `Dockerfile`

See `deploy/azure-container-apps/README.md` for the preview and deploy steps.

## Optional Kubernetes path

This starter also includes:

- `deploy/kubernetes/README.md`
- `deploy/kubernetes/apply.ps1`
- `deploy/kubernetes/kustomization.yaml`
- `deploy/kubernetes/namespace.yaml`
- `deploy/kubernetes/deployment.yaml`
- `deploy/kubernetes/service.yaml`

Use these files from the generated app root when you want a platform-neutral Kubernetes baseline from the shipped Dockerfile and source tree.

The generated Kubernetes baseline assumes:

- `NuGet.config` points at a reachable Cephalon package source or `./.cephalon/packages` has been seeded before the container image is built
- `kubectl` with `kustomize` support is installed and already targets the cluster context you want to update
- the image you pass to the apply script is pullable by the target cluster

See `deploy/kubernetes/README.md` for the render, preview, and apply steps.

## Optional Linux systemd path

This starter also includes:

- `deploy/linux/systemd/README.md`
- `deploy/linux/systemd/CephalonTemplateApp.service`
- `deploy/linux/systemd/CephalonTemplateApp.env`

Use these files after you publish the host into `./artifacts/publish/CephalonTemplateApp/`.

The generated Linux service baseline assumes:

- published output is copied to `/opt/CephalonTemplateApp/current`
- the optional environment override file lives at `/etc/cephalon/CephalonTemplateApp.env`

See `deploy/linux/systemd/README.md` for the install, verify, and `systemctl` steps.

## Optional container path

This starter now also includes `NuGet.config`, `.cephalon/packages/README.md`, `.dockerignore`, `Dockerfile`, `compose.yaml`, and `otel-collector-config.yaml`.

Populate `./.cephalon/packages` or repoint `NuGet.config` before you build or run the container path:

```powershell
dotnet build
```

From the generated app root:

```powershell
docker compose up --build
```

Then inspect `/engine`, `/engine/snapshot`, `/health/ready`, and `/scalar`.
