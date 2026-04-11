# Cephalon.Engine

`Cephalon.Engine` is the composition and runtime core of Cephalon.

## What it owns

- module registration and dependency ordering
- assembly and package-based module discovery
- package compatibility, integrity, and detached-signature validation for manifest-driven module loading, including multi-signer package manifests
- package-governance policy for manifest metadata and raw assembly-path rules
- package publisher and signature provenance metadata carried through trust and manifest surfaces
- trusted public-key resolution for cryptographic package signature verification across declared signers
- configuration binding for engine, trust, localization, failure policy, options, the phase-8 `Data`, `Databases`, `Identity`, `Tenancy`, `Audit`, and `Messaging` sections, and the phase-11 contract-first `Resilience` section, including `Engine:Audit:History:Export` and `Engine:Audit:History:Retention`
- runtime lifecycle, failure capture, restart policy, and health evaluation
- additive execution-graph contracts and runtime execution-graph catalogs
- additive hosted-execution contracts and runtime hosted-execution catalogs
- additive projection contracts and runtime projection catalogs
- additive inbox contracts and runtime inbox catalogs
- additive outbox contracts, dispatch-policy contracts, and runtime outbox catalogs
- additive database-role contracts and runtime database-role catalogs
- additive database-migration contracts and runtime database-migration catalogs
- additive authorization-policy contracts and runtime authorization-policy catalogs
- additive audit-store contracts and runtime audit-store catalogs
- additive event-dispatch runtime descriptor and state catalogs
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
- `Data/DatabaseRoleCatalogSnapshot.cs`
- `Data/DatabaseMigrationCatalogSnapshot.cs`
- `Authorization/AuthorizationPolicyCatalogSnapshot.cs`
- `Audit/AuditStoreCatalogSnapshot.cs`
- `Runtime/RuntimeIntrospectionSnapshotProvider.cs`
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

This package is the host-agnostic center of the framework. ASP.NET Core, worker hosts, CLI, scaffolding, and companion technology packs all consume this runtime model instead of rebuilding engine logic locally. That now includes the runtime diagnostics catalog that publishes stable event-id conventions for the active engine and companion packages, the execution-graph and hosted-execution transition counters exposed through the shared `Cephalon.Engine` meter, the runtime story contracts that explain what loaded, started, failed, and why in one ordered payload, the additive execution-graph catalog surfaced through `/engine/execution-graphs` and `/engine/snapshot`, the additive hosted-execution catalog surfaced through `/engine/hosted-executions` and `/engine/snapshot`, the additive projection catalog surfaced through `/engine/projections` and `/engine/snapshot`, the additive inbox catalog surfaced through `/engine/inboxes` and `/engine/snapshot`, the additive outbox catalog surfaced through `/engine/outboxes` and `/engine/snapshot`, the additive authorization-policy catalog surfaced through `/engine/authorization-policies` and `/engine/snapshot`, the additive audit-store catalog surfaced through `/engine/audit-stores` and `/engine/snapshot`, the additive event-dispatch runtime descriptor and state catalogs surfaced through `snapshot.EventDispatchRuntimes` and `snapshot.EventDispatchStates`, the additive rate-limiting runtime catalog surfaced through host-owned routes and `snapshot.RateLimitingPolicies`, the live aggregate `Summary` now carried by each dispatch-runtime descriptor so operator tooling has one canonical per-runtime answer, and the outbox-dispatch-policy enrichment that lets `/engine/outboxes` answer `disabled`, `consumer-managed`, or runtime-managed ownership without hardwiring provider or adapter logic into the engine core. The execution-graph and hosted-execution lifecycle state still surface through `/engine/runtime-story` and `/engine/snapshot`, and the configuration-driven failure-policy windows let hosts tune readiness warmup, shutdown drain, and manual restart backoff without hardwiring host-specific lifecycle logic. The technology-runtime catalog is also now projected on demand from active contributors when the engine builds it that way, so application-managed companion-pack state such as event-subscription runtime reporting and outbox-dispatch runtime reporting can stay fresh in `/engine/technology-surfaces` and `/engine/snapshot` instead of freezing at the first resolution.

Just as importantly, this package exists to lower ceremony for consumer apps. The engine should absorb repetitive composition, configuration binding, runtime wiring, introspection, and companion-pack coordination so Cephalon-based apps spend less code on plumbing and declarations, emit less boilerplate, and stay focused on project-specific business logic.

That same rule now applies to the shipped database-topology and durable audit-history baseline. `Engine:Databases` is the engine-owned physical-topology contract for shared runtime tuning, the first named roles (`Write`, `Read`, `Outbox`, `History`), narrow dependent role references through `UseRole`, nested migration policy, and the `/engine/databases` introspection surface. The engine now also publishes additive `IDatabaseRoleCatalog` and `IDatabaseMigrationCatalog` surfaces over that same contract, so `/engine/database-roles` plus `snapshot.DatabaseRoles` can answer requested versus resolved roles, `UseRole` truth, provider, schema, connection mode, merged runtime tuning, operator-facing consumers, and provider-contributed live runtime health, while `/engine/database-migrations` plus `snapshot.DatabaseMigrations` can answer logical migration targets, execution mode, provider ownership, current status, role-runtime decoration, and provider-added deploy-time command templates without leaking provider specifics back into host startup. `Engine:Audit:History` now lets additive provider packs such as `Cephalon.Audit.EntityFramework` target the logical `history` role by default, or another supported engine-owned role through `Engine:Audit:History:DatabaseRole`, while `Engine:Audit:History:Export` and `Engine:Audit:History:Retention` now drive the first engine-owned export plus retention baselines. `/engine/audit-history` exposes the first queryable operator surface when a durable reader is active, and `/engine/audit-history/export` exposes the first bounded NDJSON export surface when a durable exporter is active. Broader role graphs, non-relational history providers, replay UX, and fine-grained provider-native diagnostics remain later slices, but the engine now owns the contract instead of leaving it to sample-only host code. See [Database topology](../database-topology.md).

The same contract-first approach now drives phase 11 resilience work. `Engine:Resilience` is now a
first-class configuration section, `AppProfile.Resilience` is now part of the public app-model
surface, `/engine/resilience` is now the direct operator route for that requested contract, and
`BuiltInPatterns` now includes `onion-architecture` plus `anti-corruption-layer` so the taxonomy no
longer lags the roadmap. The new host-agnostic `IRateLimitingRuntimeCatalog` and
`RateLimitingRuntimeDescriptor` contracts also let host adapters publish effective enforcement into
`snapshot.RateLimitingPolicies` without pretending the engine core itself performs HTTP throttling.
`RateLimitingSelection` now also carries additive `Overrides` projected as
`RateLimitingOverrideSelection` entries, and `ResilienceSelection` now also carries additive
`BehaviorExecutionOverrides` projected as `BehaviorExecutionResilienceOverrideSelection` entries, so
the public app model can describe narrower behavior- or transport-scoped intent without leaking
ASP.NET Core-specific types into the engine core. The current shipped follow-through now includes
both ASP.NET Core public-HTTP rate limiting plus an endpoint-scoped effective runtime catalog and the
first behavior-execution runtime catalog through `IBehaviorResilienceRuntimeCatalog`,
`/engine/behavior-resilience`, and `snapshot.BehaviorResiliencePolicies`. The current behavior
pipeline truthfully enforces retry, timeout, circuit breaker, and bulkhead across dispatch, resolves
narrower behavior/transport overrides with explicit disable answers, and now also consumes the
behavior-authored `BehaviorIdempotencyAttribute` contract so runtime metadata and exception
classification can answer whether retry is `eligible`, `ineligible`, or `unknown` for a
specific behavior. Automatic retry execution now uses that same classifier plus the effective retry
policy to enforce backoff/jitter only for explicitly idempotent transient failures, while broader
transport-native rate-limiting semantics beyond HTTP route mapping remain later work.

Package loading is also governed here. `cephalon.package.json` compatibility metadata, external distribution and provenance hints, publisher/signature provenance fields, optional integrity hashes, detached signature verification against trusted public keys or trusted signing certificate chains, publisher/signer/checksum-based trust allow-lists, and `/engine/packages` manifest output are all part of the engine contract rather than host-specific behavior.

This package also carries the public contracts that should be explained well through XML comments. Those XML comments are written so external tooling can generate API/reference docs later, while the hand-authored `.md` guides describe how teams should actually adopt the engine.

## Related docs

- [Architecture](../architecture.md)
- [App models](../app-models.md)
- [Operations](../operations.md)
- [Runtime failure policy](../runtime-failure-policy.md)
