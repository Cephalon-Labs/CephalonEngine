# Cephalon Compatibility

This guide describes the compatibility contract that must stay aligned across Cephalon packages, package manifests, scaffolding, templates, and CLI workflows.

## Compatibility contract at a glance

| Concern | Source of truth | Must stay aligned |
| --- | --- | --- |
| Cephalon package version | the package version you ship for the current engine/tooling release | `Cephalon.Cli new` defaults, `Cephalon.Scaffolding` output, `Cephalon.TemplatePack` package metadata, starter `cephalon.package.json` files, and versioned doc examples |
| Target framework baseline | the `TargetFramework` used by shipped `src/Cephalon.*` projects | CLI defaults, scaffolded project files, generated module manifests, template project files, sample/reference-module projects, and docs examples |
| Blueprint, pattern, technology, and transport identifiers | the runtime/app-model contracts in `Cephalon.Abstractions` and `Cephalon.Engine` | scaffold plans, CLI parsing/help text, template coverage, samples, and hand-authored docs |
| Package manifest contract | `cephalon.package.json` plus engine package-loading and policy enforcement | scaffolded module output, template module starters, reference modules, module-authoring docs, operations docs, and trust/package-policy guidance |
| Reference-doc publishing flow | `Cephalon.ReferenceDocs`, the CLI docs commands, and the host `ReferenceDocs` section | scaffolded host appsettings/readmes, docs-publish command help, hosted docs guidance, and docs examples |

## Alignment rules

### Version and framework baselines

- when the shipped Cephalon package version changes, update CLI defaults, template-pack package metadata, starter manifests, and docs snippets that show a literal version
- when the supported target framework changes, update shipped `src/Cephalon.*` projects together with CLI defaults, scaffolded output, template projects, sample/reference-module manifests, and docs examples
- keep scaffolded package references aligned with the repository package catalog so generated test infrastructure dependencies do not drift from `Directory.Packages.props`

### Package manifest compatibility

- module packages should emit `version`, `compatibility.minimumEngineVersion`, and `compatibility.supportedTargetFrameworks` at minimum
- use `compatibility.maximumEngineVersion` only when support is intentionally capped
- keep `cephalon.package.json` examples aligned with runtime enforcement in `/engine/packages`, `Engine:PackagePolicy`, and `Engine:Trust`, including any declared package `dependencies`
- keep trust-policy examples aligned with the shipped signature-verification paths, including `TrustedSignaturePublicKeys`, `TrustedSignatureCertificates`, `TrustedSignatureCertificateAuthorities`, and the runtime `verificationSource` / `certificateThumbprint` fields surfaced through `/engine/packages`
- keep starter manifests aligned with the module author's actual assembly target framework and the engine version they intend to support

### Generation surfaces

- `Cephalon.Engine` owns blueprint, pattern, transport, and technology semantics
- `Cephalon.Scaffolding` renders those semantics into concrete files and package references
- `Cephalon.Cli` is the richer generation and docs-publishing shell over the same contracts
- `Cephalon.TemplatePack` is the lightweight install surface for the same shipped blueprint family and module starter conventions
- when blueprint, transport, docs-hosting, or package-manifest behavior changes, update all affected surfaces together instead of letting one generator path drift

### Reference-doc and DocFX flows

- XML comments on supported public APIs are the common source for IntelliSense, `Cephalon.ReferenceDocs`, and DocFX-style publishing
- keep the CLI docs commands, the publish script, scaffolded `ReferenceDocs` config, and hosted-reference docs guidance aligned when publishing behavior changes
- keep tests outside the supported reference-doc/DocFX input set unless we intentionally promote them into published documentation scope

## Repository verification points

- `ScaffoldGeneratorTests` verifies scaffolded package versions and generated module manifests
- `TemplatePackTests` verifies starter coverage, package contents, and emitted module manifest conventions
- `CliApplicationTests` verifies CLI entry points and end-to-end command behavior for the user-facing shell
- `EngineBuilderTests` verifies package-manifest compatibility enforcement such as engine-version and target-framework checks
- `PackageSurfaceTests` verifies the intended exported surface of the CLI, reference-doc tooling, adapters, scaffolding, and companion packs

## Maintainer checklist

Use this checklist whenever compatibility-sensitive behavior changes:

1. If the Cephalon package version changed, did you update CLI defaults, scaffold output, template-pack metadata, starter manifests, and versioned docs examples?
2. If the target framework changed, did you update project files, starter manifests, template files, samples, and docs examples together?
3. If blueprint or transport semantics changed, did you update runtime contracts, scaffolding, CLI help/behavior, template starters, and docs together?
4. If `cephalon.package.json` changed, did you update engine enforcement, scaffold/template output, authoring docs, and operational guidance together?
5. If reference-doc publishing changed, did you update `Cephalon.ReferenceDocs`, CLI docs commands, scaffolded `ReferenceDocs` config, and `docs/reference-docs.md` together?
