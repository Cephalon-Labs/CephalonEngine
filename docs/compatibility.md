# Cephalon Compatibility

This guide describes the compatibility contract that must stay aligned across Cephalon packages, package manifests, scaffolding, templates, and CLI workflows.

## Compatibility contract at a glance

| Concern | Source of truth | Must stay aligned |
| --- | --- | --- |
| Cephalon package version | the package version you ship for the current engine/tooling release | `Cephalon.Cli new` defaults, `Cephalon.Scaffolding` output, `Cephalon.TemplatePack` package metadata, starter `cephalon.package.json` files, and versioned doc examples |
| Target framework baseline | the `TargetFramework` used by shipped `src/Cephalon.*` projects | CLI defaults, scaffolded project files, generated module manifests, template project files, sample/reference-module projects, and docs examples |
| Blueprint, pattern, technology, and transport identifiers | the runtime/app-model contracts in `Cephalon.Abstractions` and `Cephalon.Engine` | scaffold plans, CLI parsing/help text, template coverage, samples, and hand-authored docs |
| Package manifest contract | `cephalon.package.json` plus engine package-loading and policy enforcement | scaffolded module output, template module starters, reference modules, module-authoring docs, operations docs, and trust/package-policy guidance |
| REST authoring and governance contract | the module-owned projection, runtime-catalog, and governance surfaces in `Cephalon.Behaviors.Http` and `Cephalon.AspNetCore` | REST-enabled `Cephalon.Scaffolding` output, `cephalon-monolith` / `cephalon-slice` / `cephalon-microservice`, `cephalon-rest-behavior-module`, `cephalon-rest-module`, blueprint samples, REST strategy docs, module-authoring docs, component docs, runtime/operator guidance, and host governance config examples |
| Reference-doc publishing flow | `Cephalon.ReferenceDocs`, the CLI docs commands, and the host `ReferenceDocs` section | scaffolded host appsettings/readmes, docs-publish command help, hosted docs guidance, and docs examples |
| Release package-artifact flow | `scripts/publish-package-artifacts.ps1`, `scripts/validate-release.ps1`, and the release-validation workflow | intended packable project set, shared NuGet metadata/readme defaults, CLI tool packaging, release checksum/provenance metadata, artifact uploads, and package-publishing docs |
| Framework readiness and deployment-mode claims | `scripts/deployment-mode-support.json`, `docs/deployment-mode-support.md`, `scripts/validate-dotnet-readiness.ps1`, and the dedicated `.NET 11` readiness workflow lane | `global.json`, shipped TFMs, template baselines, scaffolding/runtime defaults, docs claims, package-publishing guidance, and roadmap/backlog planning |

## Alignment rules

### Version and framework baselines

- when the shipped Cephalon package version changes, update CLI defaults, template-pack package metadata, starter manifests, and docs snippets that show a literal version
- when the supported target framework changes, update shipped `src/Cephalon.*` projects together with CLI defaults, scaffolded output, template projects, sample/reference-module manifests, and docs examples
- keep the shipping framework baseline separate from the readiness lane: `net10.0` remains the current shipping floor until an intentional migration updates repo truth across code, docs, templates, and package metadata together
- keep scaffolded package references aligned with the repository package catalog so generated test infrastructure dependencies do not drift from `Directory.Packages.props`

### Framework readiness and deployment-mode claims

- use `scripts/deployment-mode-support.json` as the explicit deployment-mode support manifest, and use `scripts/validate-dotnet-readiness.ps1` as the repo-native validation and reporting surface for current SDK selection, future-SDK assessment, target-framework audit results, and deployment-mode claim status
- treat `.NET 11` as a readiness lane until an intentional migration changes the shipping baseline; preview compatibility does not, by itself, change Cephalon's supported default target framework
- keep higher-SDK validation separate from `global.json` pinning so Cephalon can assess future SDKs without silently changing the stable shipping toolchain
- keep `docs/deployment-mode-support.md` aligned as the human-facing explanation of that same manifest-backed support contract
- do not claim trim, Native AOT, or single-file support until the manifest, project settings, validation coverage, workflow automation, and docs all agree on the same support statement
- analyzer-only settings are readiness signals, not support claims

### Package manifest compatibility

- module packages should emit `version`, `compatibility.minimumEngineVersion`, and `compatibility.supportedTargetFrameworks` at minimum
- use `compatibility.maximumEngineVersion` only when support is intentionally capped
- keep `cephalon.package.json` examples aligned with runtime enforcement in `/engine/packages`, `Engine:PackagePolicy`, and `Engine:Trust`, including any declared package `dependencies`
- when a package is published externally, keep `distribution` and `provenance` metadata aligned with the real release channel, artifact location, source revision, and provenance evidence you shipped
- keep trust-policy examples aligned with the shipped signature-verification paths, including `TrustedSignaturePublicKeys`, `TrustedSignatureCertificates`, `TrustedSignatureCertificateAuthorities`, and the runtime `verificationSource` / `certificateThumbprint` fields surfaced through `/engine/packages`
- keep starter manifests aligned with the module author's actual assembly target framework and the engine version they intend to support

### Generation surfaces

- `Cephalon.Engine` owns blueprint, pattern, transport, and technology semantics
- `Cephalon.Scaffolding` renders those semantics into concrete files and package references
- `Cephalon.Cli` is the richer generation and docs-publishing shell over the same contracts
- `Cephalon.TemplatePack` is the lightweight install surface for the same shipped blueprint family and module starter conventions
- when blueprint, transport, docs-hosting, or package-manifest behavior changes, update all affected surfaces together instead of letting one generator path drift

### REST authoring and governance

- when behavior-backed REST authoring, shorthand projection, or host-governance semantics change, update REST-enabled `Cephalon.Scaffolding` output, the REST-enabled blueprint app starters, `cephalon-rest-behavior-module`, the matching blueprint samples, module-authoring guidance, component docs, compatibility guidance, and runtime/operator docs together
- keep starter guidance aligned with the settled module-owned boundary: REST-enabled blueprint app starters and blueprint samples now treat `RestBehaviorModuleBase.ConfigureRestBehaviors(...)` plus `MapProfile<TBehavior>()` as the default public REST path, `cephalon-rest-behavior-module` remains the recommended package starter for behavior-backed public REST, and `cephalon-rest-module` remains the generic non-behavior REST starter

### Reference-doc and DocFX flows

- XML comments on supported public APIs are the common source for IntelliSense, `Cephalon.ReferenceDocs`, and DocFX-style publishing
- keep the CLI docs commands, the publish script, scaffolded `ReferenceDocs` config, and hosted-reference docs guidance aligned when publishing behavior changes
- keep tests outside the supported reference-doc/DocFX input set unless we intentionally promote them into published documentation scope
- keep shared test-harness types internal while the test project stays outside the supported published docs set, leaving only framework-required xUnit classes and rare reflective transport-contract exceptions public

### Package publishing flow

- keep the release package-artifact script aligned with the intended packable surface instead of relying on ambient `dotnet pack` defaults across the whole solution
- keep shared NuGet metadata, package readme defaults, CLI tool packaging, and release artifact output aligned across shipped packages, the CLI tool package, and the reference module package
- keep the release checksum/provenance manifest aligned with the actual repository source revision, packed file set, and published checksum sidecar
- keep the stable `cephalon` command name aligned across `Cephalon.Cli` packaging, docs, and validation coverage whenever the tool install surface changes

## Repository verification points

- `ScaffoldGeneratorTests` verifies scaffolded package versions and generated module manifests
- `TemplatePackTests` verifies starter coverage, package contents, and emitted module manifest conventions
- `CliApplicationTests` verifies CLI entry points and end-to-end command behavior for the user-facing shell
- `PackagePublishingTests` verifies the intended release artifact set, packaged readmes, and the local-install smoke path for the `Cephalon.Cli` tool package
- `EngineBuilderTests` verifies package-manifest compatibility enforcement such as engine-version and target-framework checks
- `PackageSurfaceTests` verifies the intended exported surface of the CLI, reference-doc tooling, adapters, scaffolding, and companion packs
- `TestHarnessSurfaceTests` verifies that `tests/Cephalon.Tests` exports only xUnit test classes plus the explicit reflective transport-contract allow list and keeps XML-document generation disabled

## Maintainer checklist

Use this checklist whenever compatibility-sensitive behavior changes:

1. If the Cephalon package version changed, did you update CLI defaults, scaffold output, template-pack metadata, starter manifests, and versioned docs examples?
2. If the target framework changed, did you update project files, starter manifests, template files, samples, and docs examples together?
3. If blueprint or transport semantics changed, did you update runtime contracts, scaffolding, CLI help/behavior, template starters, and docs together?
4. If `cephalon.package.json` changed, did you update engine enforcement, scaffold/template output, authoring docs, and operational guidance together?
5. If reference-doc publishing changed, did you update `Cephalon.ReferenceDocs`, CLI docs commands, scaffolded `ReferenceDocs` config, and `docs/reference-docs.md` together?
6. If framework-readiness or future-SDK behavior changed, did you update `scripts/deployment-mode-support.json`, `scripts/validate-dotnet-readiness.ps1`, the release-validation workflow, `docs/deployment-mode-support.md`, `docs/dotnet11-readiness.md`, `docs/project-memory.md`, and planning docs together?
7. If trim, Native AOT, or single-file support claims changed, did you add explicit validation and update the deployment-mode support manifest plus package-publishing/support docs instead of relying on analyzer output or local assumptions?
