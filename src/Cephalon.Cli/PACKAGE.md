# Cephalon CLI

`Cephalon.Cli` is the user-facing command-line shell for Cephalon blueprint generation, external package staging, and reference-doc workflows.

## Install

Install from a packaged artifact:

```powershell
dotnet tool install --tool-path .\.tools\cephalon Cephalon.Cli `
  --add-source .\artifacts\packages-release `
  --ignore-failed-sources `
  --no-cache `
  --prerelease
```

Or update an existing local tool-path install:

```powershell
dotnet tool update --tool-path .\.tools\cephalon Cephalon.Cli `
  --add-source .\artifacts\packages-release `
  --ignore-failed-sources `
  --no-cache `
  --prerelease
```

## Usage

```powershell
.\.tools\cephalon\cephalon --help
.\.tools\cephalon\cephalon doctor
.\.tools\cephalon\cephalon new Acme.Store --blueprint Microservice
.\.tools\cephalon\cephalon doctor --app-root ./Acme.Store
.\.tools\cephalon\cephalon package stage --package .\artifacts\packages-release\Cephalon.ReferenceModule.Operations.0.1.0-preview.nupkg --output .\plugins\reference-operations
.\.tools\cephalon\cephalon docs publish --root .
```

`cephalon doctor` is the recommended first-run verification step after installing the tool. It checks the active .NET SDK selection, the installed .NET 10 runtime baselines, whether the optional `dotnet new` template pack is available, and the packaged deployment-mode support contract that currently keeps the stable shipping floor on `net10.0` while `.NET 11` stays an assessment-only readiness lane and trim / Native AOT / single-file remain `not-claimed`.
If you pass `--app-root <path>`, the same command also verifies the generated app bootstrap: solution file, `Directory.Packages.props`, `NuGet.config`, local or shared Cephalon package-source reachability, generated host project, generated `Program.cs` bootstrap source with explicit `AddCephalonProjectConfigurations` and `MapCephalon` wiring, generated host-project `PackageReference` plus `Configurations/**/*.json` copy/publish baseline shape, generated test-project discovery plus `Architecture/CompositionSmokeTests.cs` and `Features/*BehaviorSpecifications.cs` Given/When/Then placeholder alignment, `Properties/PublishProfiles/CephalonFolder.pubxml`, the generated host target framework, generated split-config assets (`Configurations/AddEngine.*.json` plus `Configurations/Observability/Development.json`), generated app-model plus engine-feature plus observability plus localization plus development-Serilog baseline shape, generated guidance docs (`README.md`, `./.cephalon/packages/README.md`, `Configurations/README.md`, and `deploy/*/README.md`), the generated documentation-surface assets (`Configurations/AddOpenApi.json` plus `Configurations/AddReferenceDocs.json`), generated OpenAPI and hosted reference-doc baseline shape, the shipped Windows Service, IIS, Azure App Service, and Linux `systemd` deployment assets, the shipped Dockerfile plus container deployment assets, generated local orchestration assets (`compose.yaml` plus `otel-collector-config.yaml`), generated Dockerfile base-image alignment, generated compose plus collector baseline alignment, and whether `PublishTrimmed`, `PublishAot`, or `PublishSingleFile` push that app outside the current support contract.

`cephalon new` now emits generated app roots with `README.md`, `NuGet.config`, `./.cephalon/packages/README.md`, `Properties/PublishProfiles/CephalonFolder.pubxml`, `Configurations/README.md`, `Configurations/AddEngine.*.json`, `Configurations/Observability/Development.json`, `deploy/windows-service/README.md`, `deploy/windows-service/install-service.ps1`, `deploy/windows-service/remove-service.ps1`, `deploy/iis/README.md`, `deploy/iis/install-site.ps1`, `deploy/iis/remove-site.ps1`, `deploy/azure-app-service/README.md`, `deploy/azure-app-service/deploy-zip.ps1`, `deploy/container-image/README.md`, `deploy/container-image/publish-image.ps1`, `deploy/azure-container-apps/README.md`, `deploy/azure-container-apps/deploy-up.ps1`, `deploy/kubernetes/README.md`, `deploy/kubernetes/apply.ps1`, `deploy/kubernetes/kustomization.yaml`, `deploy/kubernetes/namespace.yaml`, `deploy/kubernetes/deployment.yaml`, `deploy/kubernetes/service.yaml`, `deploy/linux/systemd/README.md`, `deploy/linux/systemd/<App>.service`, `deploy/linux/systemd/<App>.env`, `.dockerignore`, `Dockerfile`, `compose.yaml`, and `otel-collector-config.yaml` so a new host can be validated with `dotnet publish`, Windows Service install previews, IIS install previews, Azure App Service ZIP deploy previews, provider-neutral container-image publish previews, Azure Container Apps source-deploy previews, Kubernetes manifest previews, WSL `systemd-analyze` verification, published-output smoke runs, `docker compose up --build`, and `dotnet run`.
The same starter path now also emits canonical kebab-case `Engine` ids and structured `Engine:Data`, `Engine:Identity`, `Engine:Tenancy`, `Engine:Audit`, and `Engine:Messaging` sections. The shipped low-ceremony baseline keeps `Identity`, `Tenancy`, and `Messaging` disabled until selected, enables `Audit`, and defaults ids to `Sfid` so consumer apps can stay focused on business logic while phase-8 packs grow around them.
Generated apps now also start their test project with `Architecture/CompositionSmokeTests.cs` plus per-feature `Features/*BehaviorSpecifications.cs` placeholders so teams can turn the starter directly into composition checks and Given/When/Then-style business behavior instead of hand-authoring boilerplate test harness code first.

When you are iterating from the Cephalon repository before packages land on a shared feed, publish package artifacts into that generated `./.cephalon/packages` folder and keep the generated `NuGet.config` as-is. If your team already publishes Cephalon packages somewhere else, replace the generated `cephalon` source instead.

After scaffolding and seeding packages, rerun:

```powershell
.\.tools\cephalon\cephalon doctor --app-root ./Acme.Store
```

That gives adopters one truthful command for machine readiness, deployment-mode support posture, generated-app bootstrap readiness, generated `Program.cs` plus host-project `PackageReference` and `Configurations/**/*.json` baseline posture, generated test-project plus `CompositionSmokeTests.cs` and `BehaviorSpecifications.cs` test-harness posture, generated host target-framework posture, generated split-config posture, generated guidance docs plus local package-feed guidance posture, generated self-hosted and hosted deployment assets plus container deployment-asset and Dockerfile-baseline posture, generated local orchestration assets plus compose and collector baseline posture, and generated publish-mode claim posture before they restore, run, publish, or deploy.

The shipped `CephalonFolder.pubxml` profile publishes generated hosts to a deterministic `./artifacts/publish/<ProjectName>/` path. For a repo-native replay of scaffold -> seed packages -> publish -> run published output -> probe routes, use `pwsh ./scripts/validate-generated-app-publish.ps1`.

The shipped Windows Service assets give generated apps a self-hosted Windows baseline under `deploy/windows-service/`. For a repo-native replay of scaffold -> seed packages -> publish -> preview the generated install/remove scripts, use `pwsh ./scripts/validate-generated-app-windows-service.ps1`.

The shipped IIS assets give generated apps a hosted Windows baseline under `deploy/iis/`. For a repo-native replay of scaffold -> seed packages -> publish -> verify the SDK-generated `web.config` -> preview the generated install/remove scripts, use `pwsh ./scripts/validate-generated-app-iis.ps1`.

The shipped Azure App Service assets give generated apps a hosted cloud baseline under `deploy/azure-app-service/`. For a repo-native replay of scaffold -> seed packages -> publish -> package the generated ZIP artifact -> preview the generated Azure CLI deploy contract, use `pwsh ./scripts/validate-generated-app-app-service.ps1`.

The shipped container-image assets give generated apps a provider-neutral build/tag/push baseline under `deploy/container-image/`. For a repo-native replay of scaffold -> seed packages -> preview the generated Docker contract -> build the image -> prove push through a local registry, use `pwsh ./scripts/validate-generated-app-container-image.ps1`.

The shipped Azure Container Apps assets give generated apps a hosted source-deploy baseline under `deploy/azure-container-apps/`. For a repo-native replay of scaffold -> seed packages -> validate the generated Dockerfile locally -> preview the generated Azure CLI deploy contract, use `pwsh ./scripts/validate-generated-app-container-apps.ps1`.

The shipped Kubernetes assets give generated apps a platform-neutral manifest baseline under `deploy/kubernetes/`. For a repo-native replay of scaffold -> seed packages -> validate the generated Dockerfile locally -> preview the generated Kubernetes manifest/apply contract, use `pwsh ./scripts/validate-generated-app-kubernetes.ps1`.

The shipped Linux `systemd` assets give generated apps a self-hosted service-manager baseline under `deploy/linux/systemd/`. For a repo-native replay of scaffold -> seed packages -> publish -> WSL `systemd-analyze` verification, use `pwsh ./scripts/validate-generated-app-systemd.ps1`.

`cephalon package stage` turns a published module `.nupkg` into a loadable package directory for `Engine:Discovery:PackageDirectories` or `Engine:Discovery:Packages:ManifestPath`.

## Docs

- [Docs hub](https://github.com/Cephalon-Labs/CephalonEngine/tree/master/docs)
- [External package lifecycle](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/external-package-lifecycle.md)
- [Getting started](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/getting-started.md)
- [Generated app publishing](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/generated-app-publishing.md)
- [Container image publishing](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/container-image-publishing.md)
- [Windows Service deployment](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/windows-service-deployment.md)
- [IIS deployment](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/iis-deployment.md)
- [Azure App Service deployment](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/azure-app-service-deployment.md)
- [Azure Container Apps deployment](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/azure-container-apps-deployment.md)
- [Kubernetes deployment](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/kubernetes-deployment.md)
- [Linux systemd deployment](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/linux-systemd-deployment.md)
- [App models](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/app-models.md)
- [Module authoring](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/module-authoring.md)
- [Reference docs publishing](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/reference-docs.md)
