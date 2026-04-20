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
- configuration binding for the phase-12 `Features` section through `Engine:Features`
- configuration binding for the phase-13 `Cells` section through `Engine:Cells`, including `TrafficAutomation`
- runtime lifecycle, failure capture, restart policy, and health evaluation
- additive execution-graph contracts and runtime execution-graph catalogs
- additive hosted-execution contracts and runtime hosted-execution catalogs
- additive projection contracts and runtime projection catalogs
- additive inbox contracts and runtime inbox catalogs
- additive outbox contracts, dispatch-policy contracts, and runtime outbox catalogs
- additive CDC capture contracts plus runtime CDC descriptor, execution-runtime, state, freshness, lag, publication-posture, and staged-batch acknowledgement catalogs
- additive cell-boundary, cell-route, cell-health-isolation, and configuration-driven cell-traffic-automation contracts, runtime catalogs, and technology-surface projection
- additive database-role contracts and runtime database-role catalogs
- additive database-migration contracts and runtime database-migration catalogs
- additive database-migration operational playbook contracts and runtime database-migration playbook catalogs
- additive database-topology operational action-plan, advisory, and summary contracts with runtime database-topology posture catalogs
- additive authorization-policy contracts and runtime authorization-policy catalogs
- additive audit-store contracts and runtime audit-store catalogs
- additive event-dispatch runtime descriptor and state catalogs
- additive saga choreography runtime catalogs, live publication-state catalogs, module-backed capability projection, and snapshot projection
- additive backend-for-frontend client-binding contribution contracts and runtime catalogs
- additive strangler-fig route-contribution contracts plus runtime route, migration-policy, ingress, and request-resolution catalogs
- additive feature-flag contribution contracts, runtime catalogs, and evaluation
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
- `Data/DataProductCatalogSnapshot.cs`
- `Data/CdcCaptureCatalogSnapshot.cs`
- `Data/CdcCaptureRegistryAdapter.cs`
- `Data/ProjectionCatalogSnapshot.cs`
- `Data/InboxCatalogSnapshot.cs`
- `Data/OutboxCatalogSnapshot.cs`
- `Data/DatabaseRoleCatalogSnapshot.cs`
- `Data/DatabaseMigrationCatalogSnapshot.cs`
- `Data/DatabaseMigrationOperationalPlaybookProvider.cs`
- `Data/DatabaseTopologyOperationalSnapshotProvider.cs`
- `Authorization/AuthorizationPolicyCatalogSnapshot.cs`
- `Audit/AuditStoreCatalogSnapshot.cs`
- `Patterns/StranglerFigRuntimeCatalogSnapshot.cs`
- `Runtime/RuntimeIntrospectionSnapshot.cs`
- `Technologies/CellBoundaryCatalogSnapshot.cs`
- `Technologies/CellBoundaryTechnologyRuntimeContributor.cs`
- `Technologies/CellRouteCatalogSnapshot.cs`
- `Technologies/CellRouteTechnologyRuntimeContributor.cs`
- `Technologies/CellHealthIsolationCatalogSnapshot.cs`
- `Technologies/CellHealthIsolationTechnologyRuntimeContributor.cs`
- `Technologies/CellTrafficAutomationRuntimeCatalogSnapshot.cs`
- `Technologies/CellTrafficAutomationTechnologyRuntimeContributor.cs`
- `Features/FeatureFlagRegistryAdapter.cs`
- `Features/FeatureFlagRuntimeCatalogSnapshot.cs`
- `Features/InMemoryFeatureToggle.cs`
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
- `Configuration/CellSettings.cs`
- `Configuration/CellTrafficAutomationSettings.cs`
- `Configuration/CellTrafficAutomationRouteSettings.cs`
- `Configuration/FeatureSettings.cs`
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
- `Features`
- `Execution`
- `Localization`
- `Manifest`
- `Patterns`
- `Runtime`
- `Technologies`
- `Transports`
- `Trust`

## How it fits

This package is the host-agnostic center of the framework. ASP.NET Core, worker hosts, CLI, scaffolding, and companion technology packs all consume this runtime model instead of rebuilding engine logic locally. That now includes the runtime diagnostics catalog that publishes stable event-id conventions for the active engine and companion packages, the execution-graph and hosted-execution transition counters exposed through the shared `Cephalon.Engine` meter, the runtime story contracts that explain what loaded, started, failed, and why in one ordered payload, the additive execution-graph catalog surfaced through `/engine/execution-graphs` and `/engine/snapshot`, the additive hosted-execution catalog surfaced through `/engine/hosted-executions` and `/engine/snapshot`, the additive data-product catalog surfaced through `/engine/data-products` and `/engine/snapshot`, the additive CDC capture catalog surfaced through `/engine/cdc-captures` and `/engine/snapshot`, the additive CDC capture runtime-state projection surfaced through `snapshot.CdcCaptureStates`, including typed freshness, lag, publication posture, and linked dispatch truth, the additive projection catalog surfaced through `/engine/projections` and `/engine/snapshot`, the additive inbox catalog surfaced through `/engine/inboxes` and `/engine/snapshot`, the additive outbox catalog surfaced through `/engine/outboxes` and `/engine/snapshot`, the additive authorization-policy catalog surfaced through `/engine/authorization-policies` and `/engine/snapshot`, the additive audit-store catalog surfaced through `/engine/audit-stores` and `/engine/snapshot`, the additive event-dispatch runtime descriptor and state catalogs surfaced through `snapshot.EventDispatchRuntimes` and `snapshot.EventDispatchStates`, the additive choreography publication-state catalog surfaced through `snapshot.SagaChoreographyPublicationStates`, the module-backed choreography capability projection surfaced through the runtime manifest and `/engine/capabilities`, the additive rate-limiting runtime catalog surfaced through host-owned routes and `snapshot.RateLimitingPolicies`, the live aggregate `Summary` now carried by each dispatch-runtime descriptor so operator tooling has one canonical per-runtime answer, and the outbox-dispatch-policy enrichment that lets `/engine/outboxes` answer `disabled`, `consumer-managed`, or runtime-managed ownership without hardwiring provider or adapter logic into the engine core. The execution-graph and hosted-execution lifecycle state still surface through `/engine/runtime-story` and `/engine/snapshot`, and the configuration-driven failure-policy windows let hosts tune readiness warmup, shutdown drain, and manual restart backoff without hardwiring host-specific lifecycle logic. The technology-runtime catalog is also now projected on demand from active contributors when the engine builds it that way, so application-managed companion-pack state such as event-subscription runtime reporting and outbox-dispatch runtime reporting can stay fresh in `/engine/technology-surfaces` and `/engine/snapshot` instead of freezing at the first resolution.

The same engine-first runtime truth now also carries the phase 13 cell baseline end to end.
Modules and hosts can contribute `CellBoundaryDescriptor` entries, governed
`CellRouteDescriptor` entries, and `CellHealthIsolationDescriptor` entries; the engine composes
them through `ICellBoundaryCatalog`, `ICellRouteCatalog`, and `ICellHealthIsolationCatalog`,
validates route and health-isolation ownership against the active module-owned boundary graph,
binds `Engine:Cells:TrafficAutomation` into one `ICellTrafficAutomationRuntimeCatalog`, auto-selects
the built-in `cell-based-architecture` profile when any of those answers are active, and projects
the same answers through `/engine/cells`, `/engine/cell-routes`, `/engine/cell-health-isolations`,
`/engine/cell-traffic-automations`, `snapshot.CellBoundaries`, `snapshot.CellRoutes`,
`snapshot.CellHealthIsolations`, `snapshot.CellTrafficAutomations`, and the `cell-boundaries`,
`cell-routes`, `cell-health-isolations`, plus `cell-traffic-automations` technology runtime
surfaces without inventing a second host-only topology, traffic, health, or automation registry.
Only provider-specific or edge-aware traffic automation remains later follow-through over those
same catalogs.

That same engine-first runtime truth now also carries the first phase-13 data mesh and CDC
baselines end to end. Modules and hosts can contribute `DataProductDescriptor` entries and
`CdcCaptureDescriptor` entries; the engine composes them through `IDataProductCatalog` and
`ICdcCaptureCatalog`, validates CDC `sourceModuleId` and `outboxId` references against the active
module and outbox set, and projects the same answers through `/engine/data-products`,
`/engine/cdc-captures`, `snapshot.DataProducts`, and `snapshot.CdcCaptures` without inventing a
host-only data mesh or sync registry. When the shared CDC runtime-state catalog is active, that
same engine-first snapshot model now also carries `snapshot.CdcCaptureStates`, including latest
capture outcome, counts, checkpoints, errors, typed freshness windows, typed lag posture,
pending-publication posture, and optional linked `OutboxDispatchState` from the existing
event-dispatch runtime truth. The same shared execution story now also leaves room for replay-safe
provider checkpoint advancement through `CdcCaptureExecutionAcknowledgement` plus
`ICdcCaptureAcknowledger`, so future provider-native capture loops can acknowledge durable
progress only after the shared runtime stages the linked outbox publications successfully. The same
runtime now also projects `snapshot.CdcCaptureExecutionRuntimes` from
`ICdcCaptureExecutionRuntimeCatalog`, so operator tooling can read one execution-topology answer
with stable runtime ids, linked capture ids, aggregate started/captured/idle/failed totals, latest
checkpoint/change/error posture, and acknowledgement visibility without conflating that runtime
ownership layer with the per-capture descriptor or live-state catalog. ASP.NET Core now publishes
the same answer through `/engine/cdc-capture-runtimes`, and later provider-native or out-of-process
CDC runners can contribute additional execution runtimes on that same contract instead of inventing
another host-only registry. Provider-specific federation, self-serve data infrastructure, and provider-native CDC execution
loops remain later work over those same catalogs.

Just as importantly, this package exists to lower ceremony for consumer apps. The engine should absorb repetitive composition, configuration binding, runtime wiring, introspection, and companion-pack coordination so Cephalon-based apps spend less code on plumbing and declarations, emit less boilerplate, and stay focused on project-specific business logic.

That same rule now applies to the shipped database-topology and durable audit-history baseline. `Engine:Databases` is the engine-owned physical-topology contract for shared runtime tuning, the first named roles (`Write`, `Read`, `Outbox`, `History`), narrow dependent role references through `UseRole`, nested migration policy, and the `/engine/databases` introspection surface. The engine now also publishes additive `IDatabaseRoleCatalog` and `IDatabaseMigrationCatalog` surfaces over that same contract, so `/engine/database-roles` plus `snapshot.DatabaseRoles` can answer requested versus resolved roles, `UseRole` truth, provider, schema, connection mode, merged runtime tuning, operator-facing consumers, stable physical-target ids and display names, physical co-location, and provider-contributed live runtime health, while `/engine/database-migrations` plus `snapshot.DatabaseMigrations` can answer logical migration targets, execution mode, provider ownership, current status, typed recommended execution order for operator playbooks, typed resolved-role runtime fields such as `RoleHealthState`, `RoleMigrationState`, and `RoleObservedAtUtc`, and provider-added deploy-time command templates without leaking provider specifics back into host startup. `DatabaseMigrationCommandDescriptor` now also carries typed operator metadata such as `ToolId`, `ExecutionCategory`, and `WorkingDirectoryHint`, so hosts and sample projections do not need to infer those reusable command semantics only from metadata dictionaries, while provider packs can still preserve compatibility metadata for older consumers. The engine now also publishes `IDatabaseMigrationOperationalPlaybookProvider`, `/engine/database-migration-playbook`, and `snapshot.DatabaseMigrationPlaybook` as the canonical ordered migration answer over that lower-level catalog, so operators and tooling can read one generated playbook with ordered steps, physical-target execution groups, grouped production-versus-manual command sets, combined production-versus-manual command-batch templates, production-ready and manual-path counts, startup-apply truth, resolved-role identity, current status, physical-target identity, and selected production-versus-manual command guidance per target without rebuilding that answer locally. That same engine-owned playbook now also carries per-step coordinated migration ids, per-group coordination counts, grouped command paths, combined command batches, and operator hints when multiple logical targets share one physical database and still need coordinated deploy-time handling. The showcase sample now consumes that engine-owned playbook directly for ordered migration guidance and limits its own logic to repo-root runnable command adaptation plus read-model follow-through. The engine also publishes `IDatabaseTopologyOperationalSnapshotProvider`, `/engine/database-topology`, and `snapshot.DatabaseTopology` as the canonical operator posture surface over those lower-level catalogs, so one engine-owned summary, advisory set, and ordered action plan can answer `Ready`, `Attention`, or `Blocked` from role health, migration status, and production-guidance completeness while also carrying stable remediation categories plus source role or migration ids for each action, including shared-physical-target migration-coordination warnings when pending or failed logical migration work spans one physical database. The showcase sample now consumes that engine-owned posture directly for readiness, core operator insights, and the engine portion of its ordered action plan, while keeping only read-model drift, retry pressure, and backlog follow-through as sample-specific logic. `Engine:Audit:History` now lets additive provider packs such as `Cephalon.Audit.EntityFramework` target the logical `history` role by default, or another supported engine-owned role through `Engine:Audit:History:DatabaseRole`, while `Engine:Audit:History:Export` and `Engine:Audit:History:Retention` now drive the first engine-owned export plus retention baselines. `/engine/audit-history` exposes the first queryable operator surface when a durable reader is active, and `/engine/audit-history/export` exposes the first bounded NDJSON export surface when a durable exporter is active. Broader role graphs, non-relational history providers, replay UX, fine-grained provider-native diagnostics, and real generated execution artifacts for shared-target relational migrations remain later slices, but the engine now owns the contract instead of leaving it to sample-only host code. See [Database topology](../database-topology.md).

The same contract-first approach now drives phase 11 resilience work and the first phase 12
taxonomy follow-through. `Engine:Resilience` is now a first-class configuration section,
`AppProfile.Resilience` is now part of the public app-model surface, `/engine/resilience` is now
the direct operator route for that requested contract, and `BuiltInPatterns` now includes
`onion-architecture`, `anti-corruption-layer`, `strangler-fig`, and
`backend-for-frontend` so the taxonomy no longer lags the roadmap. The first non-taxonomy
strangler-fig follow-through is now also shipped: when modules or host code contribute
`StranglerFigRouteDescriptor` entries, the engine composes them through
`IStranglerFigRuntimeCatalog`, resolves requests through `IStranglerFigRouter`, auto-selects the
`strangler-fig` pattern in the app model, and projects the live route set into
`snapshot.StranglerFigRoutes` and the ASP.NET Core `/engine/strangler-fig` surface. The next
policy/progress slice is now also shipped: `Engine:Migration:StranglerFig` overlays deterministic
default plus per-route target and progress policy into `IStranglerFigMigrationRuntimeCatalog`,
`snapshot.StranglerFigRoutePolicies`, and the ASP.NET Core `/engine/strangler-fig/runtime`
surface without changing the authored route catalog. The engine-first ingress follow-through is
now also shipped: `IStranglerFigIngressRuntimeCatalog` plus
`StranglerFigIngressRuntimeDescriptor` classify pass-through, local rewrite, absolute endpoint,
and opaque endpoint materialization truth into `snapshot.StranglerFigIngressRoutes` and the
ASP.NET Core `/engine/strangler-fig/ingress` surface without creating a second host-only ingress
registry. The first host-level cutover slice now also derives from that shared ingress truth
through `Engine:Migration:StranglerFig:AspNetCore` and the ASP.NET Core
`/engine/strangler-fig/cutover` surface. The same phase now also ships the first backend-for-
frontend client-binding runtime: host code, modules, and `Engine:BackendForFrontend:Bindings` can
contribute `BackendForFrontendClientBindingDescriptor` entries, the engine composes them through
`IBackendForFrontendRuntimeCatalog`, auto-selects the `backend-for-frontend` pattern when bindings
exist, and projects the merged answer into `snapshot.BackendForFrontendBindings` and the ASP.NET
Core `/engine/backend-for-frontend` surface. The first client-aware REST follow-through is now also
shipped: host adapters can project `IBackendForFrontendRestRuntimeCatalog` into
`snapshot.BackendForFrontendRestEndpoints` and ASP.NET Core now derives that runtime answer from the
shared binding catalog plus the published REST endpoint catalog instead of inventing a second source
of truth. The next documentation/materialization follow-through is now also shipped additively:
when a host adapter registers `IBackendForFrontendRestDocumentRuntimeCatalog`,
`snapshot.BackendForFrontendRestDocuments` can publish scope-specific OpenAPI and Scalar surfaces
without moving OpenAPI generation or Scalar mapping into `Cephalon.Engine`. Broader
provider-specific ingress or edge automation plus non-REST materialization remain later slices.
The same phase now also ships the first feature-flag baseline: `Engine:Features`,
`engine.AddFeatureFlag(...)`,
`engine.AddFeatureFlags(...)`, and module-owned `IFeatureFlagContributor` inputs now merge into one
`IFeatureFlagRuntimeCatalog` plus `IFeatureToggle` runtime, `snapshot.FeatureFlags` now publishes
the merged descriptor set, and ASP.NET Core can project that same truth through `/engine/features`
rather than hiding feature ownership inside host-only provider code. Module-contributed flags must
remain `FeatureFlagSourceKind.Module` with a matching `SourceModuleId`, duplicate ids fail
composition deterministically, and the current baseline intentionally stops short of advertising a
separate `runtime.feature-flags` capability because runtime capability provenance is still
module-based and the engine would otherwise need a synthetic source. The next bridge baseline is
now also shipped there: `FeatureFlagDescriptor.ProviderBindings`,
`Engine:Features:Flags:*:ProviderBindings`, `engine.AddFeatureFlagProvider(...)`, and
`IFeatureFlagProvider` now let provider companion packs contribute additional rollout gates through
the same shared evaluator without replacing the Cephalon-owned descriptor catalog. Missing or
provider-disabled bindings now surface as disabled evaluation answers with provider details instead
of silently inventing a second host-only feature registry.
The same phase now also ships the first static saga choreography runtime/operator projection plus
the first live publication-state follow-through: `ISagaChoreographyRuntimeCatalog` stays
host-agnostic in `Cephalon.Abstractions`, `Cephalon.Behaviors.Patterns` derives it from shared
behavior topology plus registered implementation types, and `Cephalon.Engine` projects that answer
into `snapshot.SagaChoreographies` without claiming live event-publication ownership. The same
runtime now also projects `snapshot.SagaChoreographyPublicationStates` from
`ISagaChoreographyPublicationRuntimeStateCatalog`, which keeps the latest accepted-or-failed
choreography publication handoff observations readable from one engine surface by publication,
behavior, module, transport, channel, correlation, compensation, and failure posture. That keeps
choreography ownership, transport exposure, feature gates, result-shape metadata, and handoff
observations readable from one engine surface while the explicit `Cephalon.Eventing.Behaviors`
bridge remains a separate additive runtime answer for durable publish handoff and the downstream
event-dispatch runtime remains the broker-delivery truth.
The owning `Cephalon.Behaviors` module now also publishes
`behaviors.saga-choreography.runtime-catalog` and
`behaviors.saga-choreography.publication-state` into the runtime manifest and
`/engine/capabilities` only when `AddBehaviorPatterns()` actually registers those shared
choreography services, so capability provenance stays module-backed instead of synthetic
engine-owned or bridge-owned.
That same shared feature-flag runtime now also reaches the public REST boundary in ASP.NET Core:
`RequireFeatureFlag(...)` / `RequireFeatureFlags(...)` can gate request execution while keeping the
published endpoint visible through `/engine/rest-endpoints` and `snapshot.RestEndpoints`, and host
governance can rewrite or clear shorthand REST feature requirements without inventing a second
feature-rollout registry beside the engine-owned catalogs.
The new host-agnostic `IRateLimitingRuntimeCatalog` and
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
policy to enforce backoff/jitter only for explicitly idempotent transient failures, while the
remaining rate-limiting follow-through is now narrower than before: ASP.NET Core already projects
truthful request-response, long-lived stream, and long-lived connection metadata through the
runtime catalog, so later work is broader non-route or non-ASP.NET Core transport semantics rather
than basic long-lived HTTP truth.

Package loading is also governed here. `cephalon.package.json` compatibility metadata, external distribution and provenance hints, publisher/signature provenance fields, optional integrity hashes, detached signature verification against trusted public keys or trusted signing certificate chains, publisher/signer/checksum-based trust allow-lists, and `/engine/packages` manifest output are all part of the engine contract rather than host-specific behavior.

This package also carries the public contracts that should be explained well through XML comments. Those XML comments are written so external tooling can generate API/reference docs later, while the hand-authored `.md` guides describe how teams should actually adopt the engine.

## Related docs

- [Architecture](../architecture.md)
- [App models](../app-models.md)
- [Operations](../operations.md)
- [Runtime failure policy](../runtime-failure-policy.md)
