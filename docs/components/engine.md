# Cephalon.Engine

`Cephalon.Engine` is the composition and runtime core of Cephalon.

## What it owns

- module registration and dependency ordering
- assembly and package-based module discovery
- package compatibility, integrity, and detached-signature validation for manifest-driven module loading, including multi-signer package manifests
- package-governance policy for manifest metadata and raw assembly-path rules
- package publisher and signature provenance metadata carried through trust and manifest surfaces
- trusted public-key resolution for cryptographic package signature verification across declared signers
- configuration binding for engine, trust, localization, failure policy, options, and the phase-8 `Data`, `Identity`, `Tenancy`, `Audit`, and `Messaging` sections
- runtime lifecycle, failure capture, restart policy, and health evaluation
- additive execution-graph contracts and runtime execution-graph catalogs
- additive hosted-execution contracts and runtime hosted-execution catalogs
- additive projection contracts and runtime projection catalogs
- additive inbox contracts and runtime inbox catalogs
- additive outbox contracts and runtime outbox catalogs
- additive authorization-policy contracts and runtime authorization-policy catalogs
- additive audit-store contracts and runtime audit-store catalogs
- manifest generation and runtime introspection snapshots
- built-in blueprint, pattern, transport, and technology catalogs
- trust and capability policy evaluation

## Main surfaces

- `Composition/EngineBuilder.cs`
- `Composition/EngineServiceCollectionExtensions.cs`
- `Composition/ModuleDiscovery.cs`
- `Composition/Packages/ModulePackageLoader.cs`
- `Composition/Packages/PackageDefinitionFile.cs`
- `Execution/ExecutionGraphValidation.cs`
- `Execution/ExecutionRuntimeCatalogSnapshot.cs`
- `Execution/HostedExecutionValidation.cs`
- `Execution/HostedExecutionRuntimeCatalogSnapshot.cs`
- `Data/ProjectionCatalogSnapshot.cs`
- `Data/InboxCatalogSnapshot.cs`
- `Data/OutboxCatalogSnapshot.cs`
- `Authorization/AuthorizationPolicyCatalogSnapshot.cs`
- `Audit/AuditStoreCatalogSnapshot.cs`
- `Runtime/EngineRuntime.cs`
- `Runtime/IRuntime.cs`
- `Runtime/IRuntimeIntrospectionSnapshotProvider.cs`
- `Runtime/RuntimeOperationalStory.cs`
- `Runtime/RuntimeHostedExecutionState.cs`
- `Runtime/RuntimeModuleLifecycleState.cs`
- `Runtime/RuntimeLifecycleEvent.cs`
- `Diagnostics/IRuntimeDiagnosticsCatalog.cs`
- `Diagnostics/DiagnosticsConvention.cs`
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
- `Execution`
- `Localization`
- `Manifest`
- `Patterns`
- `Runtime`
- `Technologies`
- `Transports`
- `Trust`

## How it fits

This package is the host-agnostic center of the framework. ASP.NET Core, worker hosts, CLI, scaffolding, and companion technology packs all consume this runtime model instead of rebuilding engine logic locally. That now includes the runtime diagnostics catalog that publishes stable event-id conventions for the active engine and companion packages, the execution-graph and hosted-execution transition counters exposed through the shared `Cephalon.Engine` meter, the runtime story contracts that explain what loaded, started, failed, and why in one ordered payload, the additive execution-graph catalog surfaced through `/engine/execution-graphs` and `/engine/snapshot`, the additive hosted-execution catalog surfaced through `/engine/hosted-executions` and `/engine/snapshot`, the additive projection catalog surfaced through `/engine/projections` and `/engine/snapshot`, the additive inbox catalog surfaced through `/engine/inboxes` and `/engine/snapshot`, the additive outbox catalog surfaced through `/engine/outboxes` and `/engine/snapshot`, the additive authorization-policy catalog surfaced through `/engine/authorization-policies` and `/engine/snapshot`, the additive audit-store catalog surfaced through `/engine/audit-stores` and `/engine/snapshot`, the execution-graph and hosted-execution lifecycle state surfaced through `/engine/runtime-story` and `/engine/snapshot`, and the configuration-driven failure-policy windows that let hosts tune readiness warmup, shutdown drain, and manual restart backoff without hardwiring host-specific lifecycle logic. The technology-runtime catalog is also now projected on demand from active contributors when the engine builds it that way, so application-managed companion-pack state such as event-subscription runtime reporting can stay fresh in `/engine/technology-surfaces` and `/engine/snapshot` instead of freezing at the first resolution.

Just as importantly, this package exists to lower ceremony for consumer apps. The engine should absorb repetitive composition, configuration binding, runtime wiring, introspection, and companion-pack coordination so Cephalon-based apps spend less code on plumbing and declarations, emit less boilerplate, and stay focused on project-specific business logic.

The same rule should drive the next data follow-through. Physical database roles, migration targeting, and durable audit-history routing should become engine-owned runtime answers instead of drifting into provider-pack-specific host config. The current direction for that follow-through is documented in [Database topology direction](../database-topology-direction.md).

Package loading is also governed here. `cephalon.package.json` compatibility metadata, external distribution and provenance hints, publisher/signature provenance fields, optional integrity hashes, detached signature verification against trusted public keys or trusted signing certificate chains, publisher/signer/checksum-based trust allow-lists, and `/engine/packages` manifest output are all part of the engine contract rather than host-specific behavior.

This package also carries the public contracts that should be explained well through XML comments. Those XML comments are written so external tooling can generate API/reference docs later, while the hand-authored `.md` guides describe how teams should actually adopt the engine.

## Related docs

- [Architecture](../architecture.md)
- [App models](../app-models.md)
- [Operations](../operations.md)
- [Runtime failure policy](../runtime-failure-policy.md)
