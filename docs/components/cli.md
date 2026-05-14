# Cephalon.Cli

> **Maturity:** `M4` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Cli` is the user-facing command-line surface for Cephalon scaffolding, external package staging, first-run environment verification, and documentation workflows.

See also: [Engine surface maturity audit](../engine-surface-maturity-audit.md), [Conformance matrix](../conformance-matrix.md), and [Runtime contract index](../runtime-contract-index.md) for what the CLI's `cephalon doctor` and `cephalon new` ultimately project against. [Engineering standards](../engineering-standards.md) records the library-design and code-quality baseline; [Long-range engine direction](../long-range-direction.md) frames why the CLI stays a thin scaffolding/verification surface over the engine's host-agnostic catalogs rather than owning runtime truth itself.

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
- explicit `cephalon doctor --scorecard <path>` summaries over generated engine-completion scorecard JSON artifacts, including deployment-mode posture plus claims-report gate, boundary/core/full-common/full-operator route-delegate readback, operator response JSON contract readback, non-operator endpoint audit readback, framework endpoint boundary audit readback, adoption-smoke execution-report, golden use-case, and per-use-case report readback, provider-integration and dependency-health provider-manifest posture, Eventing operational-superiority promotion contract/status/dimension/coverage/hot-path/Wolverine/runtime-concordance readback from `EventingOperationalSuperiorityEvidence`, SRE posture with stable-baseline manifest and pending-baseline blocker readback, supply-chain release evidence with external-policy preflight count/status and signed-release dry-run status/proof/blocker/command/output/handoff readback, test coverage evidence readback, and public API compatibility evidence readback
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

This package is the shell over engine, scaffolding, and optional reference-doc services. It should stay aligned with engine semantics rather than inventing its own blueprint or documentation behavior. The `cephalon new` path now emits the same operator-ready local orchestration assets and broader container assets, Windows Service deployment assets, IIS deployment assets, Azure App Service deployment assets, provider-neutral container-image publishing assets, Azure Container Apps deployment assets, Kubernetes deployment assets, Linux `systemd` deployment assets, generated guidance docs (`README.md`, `./.cephalon/packages/README.md`, `Configurations/README.md`, and deployment guides such as `deploy/windows-service/README.md`), `NuGet.config` bootstrap, `Program.cs` host bootstrap source, host-project `PackageReference` baseline (including `Cephalon.Engine.SourceGen` as a private analyzer package), REST behavior-module `PackageReference` baseline (including `Cephalon.Behaviors.SourceGen` as a private analyzer package), plus `Configurations/**/*.json` copy/publish behavior, and `Properties/PublishProfiles/CephalonFolder.pubxml` profile used by the shipped app-starter baseline so generated hosts can restore from `./.cephalon/packages`, or a swapped-in shared feed, before they are validated with `dotnet publish`, Windows Service install previews, IIS install previews, Azure App Service ZIP deploy previews, container-image publish previews, Azure Container Apps source-deploy previews, Kubernetes manifest previews, WSL `systemd-analyze` verification, published-output smoke runs, or `docker compose up --build`.
That same adoption path now stays truthful through `cephalon doctor --app-root <path>`, which layers generated-app bootstrap checks for the solution, package baseline, package-source reachability, generated host project, generated `Program.cs` bootstrap source with explicit `AddCephalonProjectConfigurations` and `MapCephalon` flow, host-project `PackageReference` set (including `Cephalon.Engine.SourceGen` as a private analyzer package), REST behavior-module `PackageReference` set (including `Cephalon.Behaviors.SourceGen` as a private analyzer package), plus `Configurations/**/*.json` copy/publish baseline alignment, generated test-project discovery, generated `Architecture/CompositionSmokeTests.cs` plus `Features/*BehaviorSpecifications.cs` Given/When/Then test harness placeholder alignment, `CephalonFolder.pubxml`, generated host target framework, generated split-config assets (`Configurations/AddEngine.*.json` plus `Configurations/Observability/Development.json`), generated app-model plus engine-feature plus observability plus localization plus development-Serilog baseline alignment, generated guidance docs (`README.md`, `./.cephalon/packages/README.md`, `Configurations/README.md`, and deployment guides such as `deploy/windows-service/README.md`), generated documentation-surface assets (`Configurations/AddOpenApi.json` and `Configurations/AddReferenceDocs.json`), generated self-hosted and hosted deployment assets plus teardown and environment baselines (`deploy/windows-service/remove-service.ps1`, `deploy/iis/remove-site.ps1`, and `deploy/linux/systemd/<App>.env`), generated container deployment assets, generated container deployment scripts (`deploy/container-image/publish-image.ps1`, `deploy/azure-container-apps/deploy-up.ps1`, and `deploy/kubernetes/apply.ps1`), generated Kubernetes manifest baselines (`deploy/kubernetes/kustomization.yaml`, `deploy/kubernetes/namespace.yaml`, `deploy/kubernetes/deployment.yaml`, and `deploy/kubernetes/service.yaml`), generated local orchestration assets, generated Dockerfile base-image alignment, generated compose plus collector baseline alignment, and generated publish-mode claims on top of the machine-level SDK/runtime/template checks.
`cephalon doctor` now also surfaces the packaged deployment-mode support contract directly from the CLI so adopters see the stable shipping floor (`net10.0`), the `.NET 11` readiness lane (`assessment-only`), the current trim / Native AOT / single-file global `not-claimed` posture, and any manifest-backed package-scoped deployment-mode claims from the same command path instead of rediscovering those limits only in repo docs, while `cephalon doctor --app-root` compares the generated host's target framework, generated Dockerfile baseline, and `PublishTrimmed`, `PublishAot`, and `PublishSingleFile` settings against that same contract.
When release managers already have a generated `engine-completion-scorecard.json` artifact, `cephalon doctor --scorecard <path>` gives a local readback of schema/source truth, platform-gate posture, validated evidence-reference counts, per-package GA-readiness counts, deployment-mode counts from `DeploymentModeEvidence` plus the generated claims-report gate/target/warning/error/package-claim/boundary/core/full-common/full-operator route-delegate, operator response JSON contract, non-operator endpoint audit, and framework endpoint boundary audit readback, adoption smoke counts from `AdoptionSmokeEvidence` plus execution-report path/schema/required-field readback, golden use-case execution-ready counts, and per-use-case execution-report counts, provider integration counts from `ProviderIntegrationEvidence` plus dependency-health provider-manifest readback from that same node, Eventing operational-superiority promotion contract/status/dimension/coverage/hot-path/Wolverine/runtime-concordance counts from `EventingOperationalSuperiorityEvidence`, SRE posture plus stable-baseline manifest, pending-baseline blocker, and guardrail-coverage counts from `SrePostureEvidence`, supply-chain release evidence counts from `SupplyChainEvidence` including external-policy preflight checks/status and signed-release dry-run status/proof/blocker/command/output/handoff readback, test coverage counts from `TestCoverageEvidence`, and public API compatibility counts from `PublicApiCompatibilityEvidence` without parsing Markdown, duplicating scorecard authority, or promoting GA/support claims.
The same CLI path now owns the phase-8 starter contract too: canonical kebab-case `Engine` ids, structured `Engine:Data`, `Engine:Identity`, `Engine:Tenancy`, `Engine:Audit`, and `Engine:Messaging` sections, a low-ceremony `Sfid` plus `Audit` default, and starter test harness placeholders that steer teams toward composition smoke checks plus Given/When/Then-style business specifications instead of hand-written plumbing.

The repo also now carries `scripts/validate-generated-app-adoption.ps1` as the scenario-driven external cold-start replay for that CLI-owned path: publish a temporary package feed, install the tool, run `cephalon doctor`, scaffold a fresh app outside the repository, seed `./.cephalon/packages` without losing the generated README guidance, rerun `cephalon doctor --app-root`, restore, build, run, probe the generated host from one script, and write `artifacts/adoption-smoke/generated-app-adoption.json` unless `-ReportPath` overrides it. The matching `dotnet new` parity path now lives in `scripts/validate-template-pack-adoption.ps1`, which publishes the generated app package closure including `Cephalon.Diagnostics` and `Cephalon.Resilience`, installs `Cephalon.TemplatePack` into an isolated custom hive, lets `cephalon doctor` see that hive through `CEPHALON_DOCTOR_TEMPLATE_HIVE`, scaffolds a project-root starter, writes `artifacts/adoption-smoke/template-pack-adoption.json` unless `-ReportPath` overrides it, and proves that `cephalon doctor --app-root` stays truthful for template-pack output too.

The modular-monolith REST/Worker/data follow-through now lives in `scripts/validate-modular-monolith-adoption.ps1`, which publishes the package closure including `Cephalon.Data`, `Cephalon.Ids.Sfid`, and `Cephalon.Worker`, scaffolds `CQRS` + `Outbox` over `RestApi`, validates the generated data/Sfid split-configuration and package baseline, writes `artifacts/adoption-smoke/modular-monolith-adoption.json` unless `-ReportPath` overrides it, proves `/engine/dependencies` and `/engine/runtime-story` from the ASP.NET Core host, and runs a generic-host Worker probe from the same generated configuration. The vertical-slice Eventing/outbox follow-through now lives in `scripts/validate-vertical-slice-eventing-adoption.ps1`, which publishes the Eventing/data/behavior package closure, scaffolds `ModularVerticalSlice` with `CQRS`, `Outbox`, `RestApi`, and `EventDrivenIntegration`, verifies generated startup uses `engine.AddEventingFromConfiguration(builder.Configuration)` plus executable `Engine:Messaging` channel/routing settings and a generated process-local `IOutbox` starter descriptor, writes `artifacts/adoption-smoke/vertical-slice-eventing-adoption.json` unless `-ReportPath` overrides it, publishes an application event through `/engine/event-publications`, and proves accepted outbox handoff through `/engine/event-publications/runtime/{publicationId}`, `/engine/event-dispatches/terminal-failures`, `/engine/event-dispatch-remediation-commands/summary`, `/engine/diagnostics`, and `/engine/snapshot`.

The microservice multi-transport follow-through now lives in `scripts/validate-microservice-multi-transport-adoption.ps1`, which publishes the generated app package closure including `Cephalon.AspNetCore.JsonRpc`, `Cephalon.AspNetCore.Grpc`, `Cephalon.Observability.HttpDependencies`, and `Cephalon.Observability.DependencyHealth.Core`, scaffolds `Microservice` with `RestApi`, `JsonRpc`, and `Grpc`, writes `artifacts/adoption-smoke/microservice-multi-transport-adoption.json` unless `-ReportPath` overrides it, runs the generated host on HTTP/1.1 plus HTTP/2 endpoints, proves the same generated business surface through REST, JSON-RPC, and gRPC, and checks `/engine/transports`, `/engine/snapshot`, `/engine/dependencies`, `/engine/diagnostics`, `/engine/runtime-story`, and `/scalar`. The SaaS tenant governance and audit follow-through now lives in `scripts/validate-saas-tenant-governance-audit-adoption.ps1`, which publishes the generated app package closure including `Cephalon.MultiTenancy.Governance`, `Cephalon.MultiTenancy.Governance.AspNetCore`, `Cephalon.Audit`, `Cephalon.AspNetCore.JsonRpc`, `Cephalon.AspNetCore.Grpc`, `Cephalon.Observability.HttpDependencies`, and `Cephalon.Observability.DependencyHealth.Core`, scaffolds a modular-monolith tenant host, writes `artifacts/adoption-smoke/saas-tenant-governance-audit.json` unless `-ReportPath` overrides it, proves tenant-administration invitation issue, dispatch, delivery-status observation, audit recording, REST, JSON-RPC, gRPC, dependency-health, `/engine/audit-stores`, `/engine/technology-surfaces/multi-tenancy`, `/engine/transports`, `/engine/snapshot`, and `/scalar` from one generated app. The out-of-tree package parity follow-through now lives in `scripts/validate-out-of-tree-package-adoption.ps1`, which publishes the generated app package closure including `Cephalon.Behaviors.SourceGen`, `Cephalon.Diagnostics`, and `Cephalon.Resilience`, packs and stages `Cephalon.ReferenceModule.Operations` through `cephalon package stage`, patches `Engine:Discovery:PackageDirectories` plus `Engine:PackagePolicy` and `Engine:Trust`, writes `artifacts/adoption-smoke/out-of-tree-package-adoption.json` unless `-ReportPath` overrides it, and proves that the same CLI-owned path keeps staged-package truth visible on `/engine/packages`, `/engine/package-policy`, `/engine/trust-policy`, `/engine/snapshot`, and `/api/operations/status`. The adoption-smoke manifest now treats those as seven separate execution-ready golden use cases.

The higher-assurance signed-governance follow-through now lives in `scripts/validate-signed-package-governance.ps1`, which repacks that same reference module with a deterministic detached signature, stages the signed `.nupkg`, patches stricter `Engine:PackagePolicy` plus `Engine:Trust:TrustedSignaturePublicKeys`, and proves from the same CLI-owned path that verified signer truth appears on `/engine/packages`, `/engine/trust-policy`, and `/engine/snapshot` while a tampered signed package is denied when signature verification is required. The matching certificate-chain follow-through now lives in `scripts/validate-signed-package-certificate-chain-governance.ps1`, which replays the same lane with `Engine:Trust:TrustedSignatureCertificates` plus `Engine:Trust:TrustedSignatureCertificateAuthorities` and proves that the same operator surfaces expose `trusted-certificate-chain` plus the signing `certificateThumbprint`.

For package-surface hardening, the command handlers, parsed option objects, console abstraction, and browser launcher are implementation details. External callers should integrate through `CliApplication` rather than binding directly to individual command types.

## Related docs

- [Reference docs publishing](../reference-docs.md)
- [Engine surface maturity audit](../engine-surface-maturity-audit.md)
- [Conformance matrix](../conformance-matrix.md)
- [Runtime contract index](../runtime-contract-index.md)
- [Long-range engine direction](../long-range-direction.md)
- [Engineering standards](../engineering-standards.md)
- [Architecture review (May 2026)](../architecture-review-2026-05.md)
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
