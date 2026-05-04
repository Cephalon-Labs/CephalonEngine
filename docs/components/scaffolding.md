# Cephalon.Scaffolding

> **Maturity:** `M4` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Scaffolding` turns the app-model scaffold contract into concrete files and folders.

## What it owns

- scaffold request parsing for generation-time decisions
- rendered solution, project, file, and folder models
- package version catalog used by generated output
- filesystem writing for generated scaffolds

## Main surfaces

- `Generation/ScaffoldGenerator.cs`
- `Generation/ScaffoldRequest.cs`
- `Generation/RenderedScaffold.cs`
- `Generation/RenderedProject.cs`
- `Generation/RenderedFile.cs`
- `Generation/PackageVersionCatalog.cs`
- `IO/FileSystemScaffoldWriter.cs`

## Source structure

- `Generation`
- `IO`

## How it fits

This package consumes the same `AppProfile.Scaffold` model exposed by the runtime. It is the shared implementation behind CLI generation and future template or automation workflows. App-host scaffolds now also emit the operator-ready generated-app baseline used across adoption docs: `NuGet.config`, `.cephalon/packages/README.md`, `Properties/PublishProfiles/CephalonFolder.pubxml`, `deploy/windows-service/README.md`, `deploy/windows-service/install-service.ps1`, `deploy/windows-service/remove-service.ps1`, `deploy/iis/README.md`, `deploy/iis/install-site.ps1`, `deploy/iis/remove-site.ps1`, `deploy/azure-app-service/README.md`, `deploy/azure-app-service/deploy-zip.ps1`, `deploy/container-image/README.md`, `deploy/container-image/publish-image.ps1`, `deploy/azure-container-apps/README.md`, `deploy/azure-container-apps/deploy-up.ps1`, `deploy/kubernetes/README.md`, `deploy/kubernetes/apply.ps1`, `deploy/kubernetes/kustomization.yaml`, `deploy/kubernetes/namespace.yaml`, `deploy/kubernetes/deployment.yaml`, `deploy/kubernetes/service.yaml`, `deploy/linux/systemd/README.md`, `deploy/linux/systemd/<App>.service`, `deploy/linux/systemd/<App>.env`, `.dockerignore`, `Dockerfile`, `compose.yaml`, and `otel-collector-config.yaml`, plus host wiring that can activate `Cephalon.Observability.OpenTelemetry` only when an endpoint or self-hosted default is supplied and can align ASP.NET Core content-root/service lifetime behavior under Windows Service hosts. The generated `NuGet.config` keeps `Cephalon*` restore deterministic through a repo-local feed placeholder until a team swaps in its shared package source. The repository now also carries `SuiteBlueprint`, `SuiteScaffoldPlan`, and `SuiteScaffoldService` for solution-level blueprint work, and `Cephalon.Engine` now defines a built-in `MicroserviceSuite` composition baseline on top of the existing `Microservice` app scaffold. The reference sample suite is now shipped under `samples/Cephalon.Sample.MicroserviceSuite`, while generator support for rendering that suite blueprint still stays in later suite-generator follow-through so this package remains honest about what it renders today.
The scaffold output now also carries the narrow phase-8 starter contract: canonical kebab-case `Engine` ids, structured `Engine:Data`, `Engine:Identity`, `Engine:Tenancy`, `Engine:Audit`, and `Engine:Messaging` sections, low-ceremony `Sfid` plus `Audit` defaults, and test placeholders under `Architecture/CompositionSmokeTests.cs` plus `Features/*BehaviorSpecifications.cs` so generated apps start from business-focused seams instead of framework boilerplate. When `RestApi` is selected, the generated public module stubs now also default to `RestBehaviorModuleBase` with `ConfigureRestBehaviors(...)` and `MapProfile<TBehavior>()`, keeping scaffold output aligned with the shipped module-owned REST strategy while leaving `cephalon-rest-module` as the generic non-behavior package path.

## Related docs

- [App models](../app-models.md)
- [Module authoring](../module-authoring.md)
- [Windows Service deployment](../windows-service-deployment.md)
- [IIS deployment](../iis-deployment.md)
- [Azure App Service deployment](../azure-app-service-deployment.md)
- [Azure Container Apps deployment](../azure-container-apps-deployment.md)
- [Kubernetes deployment](../kubernetes-deployment.md)
- [Linux systemd deployment](../linux-systemd-deployment.md)
