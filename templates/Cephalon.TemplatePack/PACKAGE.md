# Cephalon Template Pack

`Cephalon.TemplatePack` provides `dotnet new` templates for the shipped Cephalon v1 blueprints:

- `cephalon-monolith`
- `cephalon-slice`
- `cephalon-microservice`
- `cephalon-module`
- `cephalon-rest-module`
- `cephalon-rest-behavior-module`

These templates are the lightweight installation surface for teams that want a fast starting point without cloning the full Cephalon repository.

## Local packaging workflow

```powershell
dotnet pack templates/Cephalon.TemplatePack/Cephalon.TemplatePack.csproj -c Release -o artifacts/template-pack
dotnet new install .\artifacts\template-pack\Cephalon.TemplatePack.0.1.0-preview.nupkg
dotnet new list cephalon
```

## Create an app

```powershell
dotnet new cephalon-monolith -n Acme.Store
dotnet new cephalon-slice -n Acme.Store
dotnet new cephalon-microservice -n Acme.Customers
dotnet new cephalon-module -n Acme.Orders.Module
dotnet new cephalon-rest-module -n Acme.Orders.RestModule
dotnet new cephalon-rest-behavior-module -n Acme.Orders.BehaviorRestModule
```

If you also install `Cephalon.Cli`, run `cephalon doctor` first so the active SDK/runtime baseline, packaged deployment-mode support contract, and template-pack availability are verified from one place before you scaffold.
After you scaffold an app starter, rerun `cephalon doctor --app-root ./Acme.Store` so the generated solution, package-source bootstrap, host project, generated `Program.cs` bootstrap source with explicit `AddCephalonProjectConfigurations` and `MapCephalon` flow, generated host-project `PackageReference` plus `Configurations/**/*.json` copy/publish baseline, generated test project plus `Architecture/CompositionSmokeTests.cs` and `Features/*BehaviorSpecifications.cs` Given/When/Then test-harness placeholders, `CephalonFolder.pubxml` profile, generated host target framework, generated split-config assets (`Configurations/AddEngine.*.json` plus `Configurations/Observability/Development.json`), generated app-model plus engine-feature plus observability plus localization plus development-Serilog baseline shape, generated guidance docs (`README.md`, `./.cephalon/packages/README.md`, `Configurations/README.md`, and `deploy/*/README.md`), generated documentation-surface assets (`Configurations/AddOpenApi.json` and `Configurations/AddReferenceDocs.json`), generated self-hosted and hosted deployment assets, generated container deployment assets, generated container deployment scripts (`deploy/container-image/publish-image.ps1`, `deploy/azure-container-apps/deploy-up.ps1`, and `deploy/kubernetes/apply.ps1`), generated Kubernetes manifest baselines (`deploy/kubernetes/kustomization.yaml`, `deploy/kubernetes/namespace.yaml`, `deploy/kubernetes/deployment.yaml`, and `deploy/kubernetes/service.yaml`), generated local orchestration assets, generated Dockerfile baseline, generated compose plus collector baseline, and generated publish-mode claims are verified from that same command path before restore or publish.

The app-focused starters now also include `README.md`, `NuGet.config`, `./.cephalon/packages/README.md`, `Properties/PublishProfiles/CephalonFolder.pubxml`, `Configurations/README.md`, `Configurations/AddEngine.*.json`, `Configurations/Observability/Development.json`, `deploy/windows-service/README.md`, `deploy/windows-service/install-service.ps1`, `deploy/windows-service/remove-service.ps1`, `deploy/iis/README.md`, `deploy/iis/install-site.ps1`, `deploy/iis/remove-site.ps1`, `deploy/azure-app-service/README.md`, `deploy/azure-app-service/deploy-zip.ps1`, `deploy/container-image/README.md`, `deploy/container-image/publish-image.ps1`, `deploy/azure-container-apps/README.md`, `deploy/azure-container-apps/deploy-up.ps1`, `deploy/kubernetes/README.md`, `deploy/kubernetes/apply.ps1`, `deploy/kubernetes/kustomization.yaml`, `deploy/kubernetes/namespace.yaml`, `deploy/kubernetes/deployment.yaml`, `deploy/kubernetes/service.yaml`, `deploy/linux/systemd/README.md`, `deploy/linux/systemd/<App>.service`, `deploy/linux/systemd/<App>.env`, `.dockerignore`, `Dockerfile`, `compose.yaml`, and `otel-collector-config.yaml` so a generated app can be validated with `dotnet publish`, Windows Service install previews, IIS install previews, Azure App Service ZIP deploy previews, provider-neutral container-image publish previews, Azure Container Apps source-deploy previews, Kubernetes manifest previews, WSL `systemd-analyze` verification, published-output smoke runs, or `docker compose up --build` without cloning the repository samples first.

## Notes

- The templates mirror the current shipped blueprint set in the repository.
- For richer customization, `Cephalon.Cli` and `Cephalon.Scaffolding` remain the more expressive generation path.
- The app starters now use canonical phase-8 ids plus structured `Engine:Data`, `Engine:Identity`, `Engine:Tenancy`, `Engine:Audit`, and `Engine:Messaging` sections so `dotnet new` stays aligned with the runtime app-model contract.
- The app starters also ship a narrow low-ceremony `Sfid` plus `Audit` baseline so teams can start with additive ids and audit plumbing before they choose fuller data, identity, tenancy, or messaging follow-through.
- When a blueprint app starter includes `RestApi`, its public starter module now uses `RestBehaviorModuleBase`, `ConfigureRestBehaviors(...)`, and `MapProfile<TBehavior>()` so `cephalon-monolith`, `cephalon-slice`, and `cephalon-microservice` stay aligned with the settled engine-first REST model.
- `cephalon-rest-behavior-module` is the recommended package starter for new behavior-backed public REST modules, while `cephalon-rest-module` remains the generic non-behavior package path.
- The generated test project now starts with `Architecture/CompositionSmokeTests.cs` plus per-feature `Features/*BehaviorSpecifications.cs` placeholders so teams can move straight into composition checks and Given/When/Then-style business behavior instead of inventing a starter harness from scratch.
- Generated projects assume you will restore Cephalon packages from the feed or local package source you target.
- The shipped `NuGet.config` points the `cephalon` source at `./.cephalon/packages` by default so repo-local package artifacts can unblock first-run restore; replace that source when your team has a shared feed.
- The shipped `CephalonFolder.pubxml` profile publishes generated hosts to a deterministic `./artifacts/publish/<ProjectName>/` path.
- The shipped Windows Service assets live under `deploy/windows-service/` so generated apps have an installable self-hosted Windows baseline alongside publish, Linux, and container paths.
- The shipped IIS assets live under `deploy/iis/` so generated apps have a hosted Windows baseline alongside publish, self-hosted service, Linux, and container paths.
- The shipped Azure App Service assets live under `deploy/azure-app-service/` so generated apps have a hosted cloud ZIP-deploy baseline alongside publish, self-hosted service, IIS, Linux, and container paths.
- The shipped container-image assets live under `deploy/container-image/` so generated apps have a provider-neutral build/tag/push baseline that can feed Kubernetes or other hosted container deployment paths.
- The shipped Azure Container Apps assets live under `deploy/azure-container-apps/` so generated apps have a hosted cloud source-deploy baseline alongside publish, self-hosted service, IIS, Linux, and container paths.
- The shipped Kubernetes assets live under `deploy/kubernetes/` so generated apps have a platform-neutral hosted container baseline alongside publish, self-hosted service, IIS, Azure, Linux, and container paths.
- The shipped Linux `systemd` assets live under `deploy/linux/systemd/` so generated apps have an installable self-hosted service baseline alongside publish and container paths.
- Generated app starters keep OTLP wiring available through `Cephalon.Observability.OpenTelemetry`, but they leave the endpoint unset until a local compose file or deployment environment supplies it.
- Generated app starters now also use canonical phase-8 `Engine` ids plus structured `Engine:Data`, `Engine:Identity`, `Engine:Tenancy`, `Engine:Audit`, and `Engine:Messaging` sections so `dotnet new` stays aligned with `cephalon new`.
- The current template baseline keeps ceremony low with a narrow built-in `Sfid` plus `Audit` starter path while leaving broader phase-8 data, identity, tenancy, and messaging choices available for later configuration or the richer CLI/scaffolding path.
- The recommended adoption walkthrough lives in [docs/getting-started.md](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/getting-started.md).
- The published-output walkthrough lives in [docs/generated-app-publishing.md](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/generated-app-publishing.md).
- The container-image walkthrough lives in [docs/container-image-publishing.md](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/container-image-publishing.md).
- The Windows self-hosted walkthrough lives in [docs/windows-service-deployment.md](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/windows-service-deployment.md).
- The Windows hosted-IIS walkthrough lives in [docs/iis-deployment.md](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/iis-deployment.md).
- The Azure App Service walkthrough lives in [docs/azure-app-service-deployment.md](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/azure-app-service-deployment.md).
- The Azure Container Apps walkthrough lives in [docs/azure-container-apps-deployment.md](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/azure-container-apps-deployment.md).
- The Kubernetes walkthrough lives in [docs/kubernetes-deployment.md](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/kubernetes-deployment.md).
- The Linux self-hosted walkthrough lives in [docs/linux-systemd-deployment.md](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/linux-systemd-deployment.md).

## Compatibility expectations

- keep the template-pack version, starter project target frameworks, and starter `cephalon.package.json` files aligned with the current Cephalon release baseline
- keep the module starters aligned with the same manifest contract described in `docs/module-authoring.md`
- when blueprint, transport, version, or docs-hosting behavior changes, keep `Cephalon.TemplatePack`, `Cephalon.Cli`, and `Cephalon.Scaffolding` aligned rather than letting one generation path drift
- use `docs/compatibility.md` as the maintainer checklist for cross-surface compatibility changes
