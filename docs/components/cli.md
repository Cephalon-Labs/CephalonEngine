# Cephalon.Cli

`Cephalon.Cli` is the user-facing command-line surface for Cephalon scaffolding, external package staging, first-run environment verification, and documentation workflows.

Stable public surface:

- `CliApplication`

Internal command pipeline:

- `Commands/*`
- `Console/CliConsole.cs`
- `Commands/DocumentationLauncher.cs`

## What it owns

- blueprint-driven app generation
- generated app Windows Service deployment assets for app hosts
- generated app IIS deployment assets for app hosts
- generated app Azure App Service deployment assets for app hosts
- generated app Azure Container Apps deployment assets for app hosts
- generated app Kubernetes deployment assets for app hosts
- generated app Linux `systemd` deployment assets for app hosts
- generated app container-runtime assets for app hosts
- generated app local package-feed bootstrap assets for app hosts
- external package staging from published `.nupkg` artifacts
- first-run doctor checks for SDK/runtime/template readiness, deployment-mode support-contract posture, plus generated-app bootstrap verification
- optional reference-doc publishing
- hosted reference-doc configuration updates
- hosted reference-doc validation
- optional open-in-browser flows for local or hosted docs

## Main implementation surfaces

- `CliApplication.cs`
- `Commands/NewAppCommand.cs`
- `Commands/PackageStageCommand.cs`
- `Commands/DoctorCommand.cs`
- `Commands/CommandProcessRunner.cs`
- `Commands/DocsPublishCommand.cs`
- `Commands/DocsEnableHostingCommand.cs`
- `Commands/DocsValidateHostingCommand.cs`
- `Commands/DocumentationLauncher.cs`
- `Console/CliConsole.cs`

## Source structure

- `Commands`
- `Console`
- `Properties`

## How it fits

This package is the shell over engine, scaffolding, and optional reference-doc services. It should stay aligned with engine semantics rather than inventing its own blueprint or documentation behavior. The `cephalon new` path now emits the same operator-ready local orchestration assets and broader container assets, Windows Service deployment assets, IIS deployment assets, Azure App Service deployment assets, provider-neutral container-image publishing assets, Azure Container Apps deployment assets, Kubernetes deployment assets, Linux `systemd` deployment assets, `NuGet.config` bootstrap, `Program.cs` host bootstrap source, host-project `PackageReference` baseline plus `Configurations/**/*.json` copy/publish behavior, and `Properties/PublishProfiles/CephalonFolder.pubxml` profile used by the shipped app-starter baseline so generated hosts can restore from `./.cephalon/packages`, or a swapped-in shared feed, before they are validated with `dotnet publish`, Windows Service install previews, IIS install previews, Azure App Service ZIP deploy previews, container-image publish previews, Azure Container Apps source-deploy previews, Kubernetes manifest previews, WSL `systemd-analyze` verification, published-output smoke runs, or `docker compose up --build`.
That same adoption path now stays truthful through `cephalon doctor --app-root <path>`, which layers generated-app bootstrap checks for the solution, package baseline, package-source reachability, generated host project, generated `Program.cs` bootstrap source with explicit `AddCephalonProjectConfigurations` and `MapCephalon` flow, host-project `PackageReference` plus `Configurations/**/*.json` copy/publish baseline alignment, `CephalonFolder.pubxml`, generated host target framework, generated split-config assets (`Configurations/AddEngine.*.json` plus `Configurations/Observability/Development.json`), generated app-model plus engine-feature plus observability plus localization plus development-Serilog baseline alignment, generated documentation-surface assets (`Configurations/AddOpenApi.json` and `Configurations/AddReferenceDocs.json`), generated self-hosted and hosted deployment assets, generated container deployment assets, generated local orchestration assets, generated Dockerfile base-image alignment, generated compose plus collector baseline alignment, and generated publish-mode claims on top of the machine-level SDK/runtime/template checks.
`cephalon doctor` now also surfaces the packaged deployment-mode support contract directly from the CLI so adopters see the stable shipping floor (`net10.0`), the `.NET 11` readiness lane (`assessment-only`), and the current trim / Native AOT / single-file `not-claimed` posture from the same command path instead of rediscovering those limits only in repo docs, while `cephalon doctor --app-root` compares the generated host's target framework, generated Dockerfile baseline, and `PublishTrimmed`, `PublishAot`, and `PublishSingleFile` settings against that same contract.
The same CLI path now owns the phase-8 starter contract too: canonical kebab-case `Engine` ids, structured `Engine:Data`, `Engine:Identity`, `Engine:Tenancy`, `Engine:Audit`, and `Engine:Messaging` sections, a low-ceremony `Sfid` plus `Audit` default, and test placeholders that steer teams toward composition smoke checks plus Given/When/Then-style business specifications instead of hand-written plumbing.

For package-surface hardening, the command handlers, parsed option objects, console abstraction, and browser launcher are implementation details. External callers should integrate through `CliApplication` rather than binding directly to individual command types.

## Related docs

- [Reference docs publishing](../reference-docs.md)
- [Getting started](../getting-started.md)
- [Generated app publishing](../generated-app-publishing.md)
- [Windows Service deployment](../windows-service-deployment.md)
- [IIS deployment](../iis-deployment.md)
- [Azure App Service deployment](../azure-app-service-deployment.md)
- [Azure Container Apps deployment](../azure-container-apps-deployment.md)
- [Kubernetes deployment](../kubernetes-deployment.md)
- [Linux systemd deployment](../linux-systemd-deployment.md)
- [External package lifecycle](../external-package-lifecycle.md)
- [App models](../app-models.md)
- [Module authoring](../module-authoring.md)
