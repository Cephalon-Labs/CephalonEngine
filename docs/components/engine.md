# Cephalon.Engine

`Cephalon.Engine` is the composition and runtime core of Cephalon.

## What it owns

- module registration and dependency ordering
- assembly and package-based module discovery
- package compatibility, integrity, and detached-signature validation for manifest-driven module loading, including multi-signer package manifests
- package-governance policy for manifest metadata and raw assembly-path rules
- package publisher and signature provenance metadata carried through trust and manifest surfaces
- trusted public-key resolution for cryptographic package signature verification across declared signers
- configuration binding for engine, trust, localization, failure policy, and options
- runtime lifecycle, failure capture, restart policy, and health evaluation
- manifest generation and runtime introspection snapshots
- built-in blueprint, pattern, transport, and technology catalogs
- trust and capability policy evaluation

## Main surfaces

- `Composition/EngineBuilder.cs`
- `Composition/EngineServiceCollectionExtensions.cs`
- `Composition/ModuleDiscovery.cs`
- `Composition/Packages/ModulePackageLoader.cs`
- `Composition/Packages/PackageDefinitionFile.cs`
- `Runtime/EngineRuntime.cs`
- `Runtime/IRuntime.cs`
- `Runtime/IRuntimeIntrospectionSnapshotProvider.cs`
- `Manifest/RuntimeManifest.cs`
- `Manifest/PackageManifest.cs`
- `Configuration/EngineSettings.cs`
- `Configuration/EngineOptions.cs`
- `Configuration/FailurePolicy.cs`
- `Configuration/PackagePolicy.cs`
- `Configuration/TrustPolicy.cs`
- `AppModel/AppProfileBuilder.cs`
- `AppModel/AppProfileFactory.cs`

## Source structure

- `AppModel`
- `AppModel/Scaffolding`
- `Composition`
- `Composition/Packages`
- `Configuration`
- `Diagnostics`
- `Localization`
- `Manifest`
- `Patterns`
- `Runtime`
- `Technologies`
- `Transports`
- `Trust`

## How it fits

This package is the host-agnostic center of the framework. ASP.NET Core, worker hosts, CLI, scaffolding, and companion technology packs all consume this runtime model instead of rebuilding engine logic locally.

Package loading is also governed here. `cephalon.package.json` compatibility metadata, publisher/signature provenance fields, optional integrity hashes, detached signature verification against trusted public keys, publisher/signer/checksum-based trust allow-lists, and `/engine/packages` manifest output are all part of the engine contract rather than host-specific behavior.

This package also carries the public contracts that should be explained well through XML comments. Those XML comments are written so external tooling can generate API/reference docs later, while the hand-authored `.md` guides describe how teams should actually adopt the engine.

## Related docs

- [Architecture](../architecture.md)
- [App models](../app-models.md)
- [Operations](../operations.md)
- [Runtime failure policy](../runtime-failure-policy.md)
