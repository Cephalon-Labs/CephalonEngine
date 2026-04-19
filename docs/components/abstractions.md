# Cephalon.Abstractions

`Cephalon.Abstractions` is the stable contract layer that modules, hosts, and companion packages build against.

## What it owns

- module contracts such as `IModule`, `IModuleLifecycle`, `ModuleBase`, `ModuleDescriptor`, and `ModuleContext`
- behavior contracts such as `IAppBehavior<TIn, TOut>`, `IBehaviorContext`, `IBehaviorTopologyBuilder`, `BehaviorTopologyDescriptor`, `BehaviorFeatureDisabledException`, `IBehaviorOwnerModule`, `IBehaviorModuleBuilder`, and `OwnedBehaviorRegistration`
- capability contracts such as `Capability`, `CapabilityAccess`, and `ICapabilityRegistry`
- feature-flag contracts such as `FeatureFlagDescriptor`, `FeatureFlagTargetingDescriptor`, `IFeatureToggle`, `IFeatureFlagRuntimeCatalog`, `IFeatureFlagContributor`, and `IFeatureFlagRegistry`
- app-model contracts such as `AppBlueprint`, `AppProfile`, resilience-selection types, and scaffold-plan types
- phase-8 runtime-neutral contracts for data, authorization, tenancy, audit, and id generation
- health contracts used across hosts and packages
- localization contracts used by engine resources and package language packs
- pattern, migration-routing, technology, and transport contracts shared by the whole stack

## Main surfaces

- `Modules/IModule.cs`
- `Modules/IModuleLifecycle.cs`
- `Behaviors/IAppBehavior.cs`
- `Behaviors/IBehaviorContext.cs`
- `Behaviors/IBehaviorTopologyBuilder.cs`
- `Behaviors/IBehaviorOwnerModule.cs`
- `Behaviors/IBehaviorModuleBuilder.cs`
- `Behaviors/OwnedBehaviorRegistration.cs`
- `Capabilities/Capability.cs`
- `Capabilities/ICapabilityRegistry.cs`
- `Features/FeatureFlagDescriptor.cs`
- `Features/FeatureFlagTargetingDescriptor.cs`
- `Features/IFeatureToggle.cs`
- `Features/IFeatureFlagRuntimeCatalog.cs`
- `AppModel/AppProfile.cs`
- `AppModel/SuiteBlueprint.cs`
- `AppModel/Scaffolding/ScaffoldPlan.cs`
- `AppModel/Scaffolding/SuiteScaffoldPlan.cs`
- `AppModel/Scaffolding/SuiteScaffoldService.cs`
- `Data/ICommand.cs`
- `Data/IReadStore.cs`
- `Data/ProjectionDescriptor.cs`
- `Data/InboxDescriptor.cs`
- `Data/IInbox.cs`
- `Data/IInboxCatalog.cs`
- `Data/OutboxDescriptor.cs`
- `Data/IOutbox.cs`
- `Data/IOutboxCatalog.cs`
- `Authorization/AuthorizationPolicyDescriptor.cs`
- `Authorization/IAuthorizationEvaluator.cs`
- `Tenancy/TenantContext.cs`
- `Tenancy/ITenantResolver.cs`
- `Audit/AuditEntry.cs`
- `Audit/AuditHistoryExportRequest.cs`
- `Audit/IAuditHistoryExporter.cs`
- `Audit/IAuditHistoryReader.cs`
- `Audit/AuditStoreDescriptor.cs`
- `Audit/IAuditStoreCatalog.cs`
- `Ids/IIdGenerator.cs`
- `Health/DependencyHealthReport.cs`
- `Localization/ILocalizedResourceContributor.cs`
- `Patterns/IStranglerFigRouter.cs`
- `Patterns/StranglerFigRouteDescriptor.cs`
- `Technologies/ITechnologyRuntimeCatalog.cs`
- `Transports/IRestEndpointCandidateRuntimeCatalog.cs`
- `Transports/IRestEndpointCandidateRuntimeRegistry.cs`
- `Transports/IRestEndpointPublicationGroupRuntimeCatalog.cs`
- `Transports/IRestEndpointAuthoringPolicyRuntimeCatalog.cs`
- `Transports/RestEndpointCandidateProjectionDescriptor.cs`
- `Transports/RestEndpointCandidateRuntimeDescriptor.cs`
- `Transports/RestEndpointCandidateStatus.cs`
- `Transports/RestEndpointPublicationGroupDescriptor.cs`
- `Transports/RestEndpointAuthoringPolicyDescriptor.cs`
- `Transports/RestEndpointAuthoringPolicyAuthoringStyleDescriptor.cs`
- `Transports/RestEndpointAuthoringPolicySuppressionSummaryDescriptor.cs`
- `Transports/IRestEndpointRuntimeCatalog.cs`
- `Transports/RestEndpointRuntimeDescriptor.cs`
- `Transports/RestEndpointBindingDescriptor.cs`
- `Transports/RestEndpointBindingFallbackMode.cs`
- `Transports/RestEndpointBindingFallbackModeExtensions.cs`
- `Transports/RestEndpointBindingSource.cs`
- `Transports/RestEndpointOverrideBindingMode.cs`
- `Transports/IRestEndpointSuppressionRuntimeCatalog.cs`
- `Transports/RestEndpointSuppressionDescriptor.cs`
- `Transports/IRestEndpointOverrideRuntimeCatalog.cs`
- `Transports/RestEndpointOverrideDescriptor.cs`
- `Transports/TransportDescriptor.cs`

## Source structure

- `AppModel`
- `AppModel/Scaffolding`
- `Audit`
- `Authorization`
- `Behaviors`
- `Capabilities`
- `Data`
- `Features`
- `Health`
- `Ids`
- `Localization`
- `Modules`
- `Patterns`
- `Tenancy`
- `Technologies`
- `Transports`

## How it fits

The engine should depend on this package for contracts only. New runtime behavior belongs in `Cephalon.Engine` or a companion package unless it must become part of the public module authoring surface.

The behavior ownership contracts now follow that rule directly:

- `IBehaviorOwnerModule` and `IBehaviorModuleBuilder` let one module declare the behaviors it owns without leaking ASP.NET Core or other host APIs into `Cephalon.Abstractions`
- `OwnedBehaviorRegistration` is the normalized ownership record the engine composes at build time
- public REST exposure still belongs in adapter packages such as `Cephalon.Behaviors.Http`, so module ownership and HTTP route mapping stay separate concerns

The phase-8 families stay runtime-neutral on purpose:

- `Data` defines CQRS, projection, outbox/inbox, and outbox-catalog contracts without picking Entity Framework, Wolverine, or any storage engine.
- `Authorization` defines subjects, resources, policies, and evaluation contracts without binding to ASP.NET Core identity types.
- `Tenancy` defines tenant context and resolution contracts without assuming HTTP, DNS, or a single tenancy topology.
- `Audit` defines audit actors, entries, write/query/export contracts, and audit-store descriptors without hard-coding storage or observability sinks.
- `Ids` defines identifier-generation hints and the generator contract without choosing a concrete strategy such as `Sfid`.

The app-model contract now also carries a contract-first resilience family through
`ResilienceSelection`, `RetrySelection`, `TimeoutSelection`, `CircuitBreakerSelection`,
`BulkheadSelection`, and `RateLimitingSelection`. `RateLimitingSelection` now also carries additive
`RateLimitingOverrideSelection` entries, and `ResilienceSelection` now also carries additive
`BehaviorExecutionResilienceOverrideSelection` entries, so the public app model can describe
narrower behavior- or transport-scoped intent without leaking ASP.NET Core-specific endpoint
conventions into `Cephalon.Abstractions`. Those types stay transport- and host-agnostic on purpose:
they capture requested resilience intent in the public model without forcing ASP.NET Core, Polly, or
behavior-pipeline enforcement details into `Cephalon.Abstractions`.

When a host adapter does enforce HTTP rate limiting, the same package now also carries the narrow
runtime-facing `IRateLimitingRuntimeCatalog` and `RateLimitingRuntimeDescriptor` contracts. That
lets hosts publish effective policy truth into operator surfaces and snapshots without leaking
ASP.NET Core middleware types back into engine-core or application behavior code. The same package
now also carries `IBehaviorResilienceRuntimeCatalog`, `BehaviorResilienceRuntimeDescriptor`,
`BehaviorExecutionResilienceSelection`, and `BehaviorExecutionResilienceOverrideSelection` so the
engine can publish both requested override intent and effective behavior-execution timeout,
circuit-breaker, and bulkhead answers without leaking Polly types or host-specific middleware
contracts into consumer code. The `Behaviors` namespace now also exposes
`BehaviorIdempotencyAttribute` plus `BehaviorIdempotencyMode` so behavior authors can declare
whether automatic replay is safe without coupling that contract to HTTP verbs, transport adapters,
or host-specific policies. The same resilience namespace now also exposes
`BehaviorResilienceExceptionContext`, `BehaviorResilienceExceptionHandling`, and
`IBehaviorResilienceExceptionClassifier` so runtime packs can classify failures without hard-coding
Polly-specific exception decisions into consumer modules. The runtime descriptor now also carries
targeted behavior ids plus transport ids so operator tooling can see whether an answer came from the
default policy or from a narrower override, and `Resolve(behaviorId, transportId)` metadata can now
surface behavior-specific retry eligibility as `eligible`, `ineligible`, or `unknown` without
pretending that automatic retry is already enforced.

The same contract-first rule now also covers the first phase 12 migration surface. The `Patterns`
namespace now carries `IStranglerFigRouteContributor`, `IStranglerFigRouteRegistry`,
`IStranglerFigRuntimeCatalog`, `IStranglerFigMigrationRuntimeCatalog`, `IStranglerFigRouter`,
`StranglerFigMigrationRuntimeDescriptor`, `StranglerFigRequest`, `StranglerFigRouteDescriptor`,
`StranglerFigRouteResolution`, and `StranglerFigTarget` so modules, hosts, and operator tooling can
talk about migration-boundary ownership, effective configuration-driven target selection, route-level
progress, and request resolution without leaking ASP.NET Core proxy behavior, YARP, or cloud
traffic-manager types into `Cephalon.Abstractions`.

The same phase 12 rule now also covers backend-for-frontend REST documentation materialization.
`BackendForFrontendRestDocumentRuntimeDescriptor` and
`IBackendForFrontendRestDocumentRuntimeCatalog` live in the `Transports` namespace so hosts,
operator tooling, and companion packages can talk about one scope-specific REST document answer
without leaking ASP.NET Core `IOpenApiDocumentProvider`, Scalar, or route-mapper types into
`Cephalon.Abstractions`. Those contracts intentionally describe the derived runtime surface only:
binding-versus-client scope kind, scope id, client id, document name, published OpenAPI and Scalar
paths, and the binding/runtime/published-endpoint ids that justify that materialized document.

The same phase 12 rule now also covers progressive-delivery feature flags. The `Features`
namespace now carries `FeatureFlagDescriptor`, `FeatureFlagTargetingDescriptor`,
`FeatureFlagEvaluationContext`, `FeatureFlagEvaluationResult`, `IFeatureToggle`,
`IFeatureFlagRuntimeCatalog`, `IFeatureFlagContributor`, and `IFeatureFlagRegistry` so modules,
hosts, and operator tooling can talk about feature ownership, targeting, merged runtime catalogs,
and evaluation context without leaking ASP.NET Core route mappers or third-party provider SDK types
into `Cephalon.Abstractions`. Those contracts intentionally separate host-owned and module-owned
flags through `FeatureFlagSourceKind`, which lets the engine preserve ownership truth when modules
contribute flags into the shared runtime.

That same host-agnostic rule now also reaches shared behavior execution. The `Behaviors` namespace
now keeps ordered `BehaviorTopologyDescriptor.RequiredFeatureFlagIds` plus
`BehaviorTopologyDescriptor.SourceModuleId`, adds `IBehaviorTopologyBuilder.RequireFeatureFlag(...)`
and `RequireFeatureFlags(...)`, and exposes `BehaviorFeatureDisabledException` so behavior authors,
source generators, runtime catalogs, and transport adapters can share one behavior-owned
feature-gating contract without leaking ASP.NET Core endpoint metadata or third-party provider SDKs
into `Cephalon.Abstractions`.

The same transport-first rule also now covers the published REST runtime answer. The `Transports`
namespace owns `IRestEndpointRuntimeCatalog`, `RestEndpointRuntimeDescriptor`,
`RestEndpointBindingDescriptor`, `RestEndpointBindingFallbackMode`, and
`RestEndpointBindingSource` plus its stable `route`, `query`, `header`, and `body` wire names so
hosts, operator tooling, and companion packages can read resolved
public REST route truth plus explicit request-binding plans and preserved shorthand fallback truth
through one host-agnostic transport contract. That keeps the runtime answer transport-owned instead
of behavior-package-owned and avoids treating `metadata` dictionaries as the canonical binding-plan
or binding-fallback surface. `RestEndpointRuntimeDescriptor` now also carries first-class
`AuthoringStyle`, `RouteGroupPrefix`, `RelativePattern`, nullable `BehaviorType`, nullable
`SourceId`, nullable `CandidateId`, and nullable `OriginalEndpointName` /
`OriginalSummary` / `OriginalDescription`, ordered `RequiredFeatureFlagIds`, ordered
`OriginalRequiredFeatureFlagIds`, plus ordered `SkippedSuppressionIds` /
`SkippedOverrideIds` for governance-ineligible explicit DSL routes, so published endpoints do not need
`metadata.authoringStyle`, `metadata.routeGroupPrefix`, `metadata.relativePattern`,
`metadata.behaviorType`, or `metadata.sourceId` as the canonical authored-route answer and
published behavior-backed endpoints can still point back to the originating shorthand candidate
and compare original-versus-effective endpoint metadata, rollout boundaries, plus
skipped-governance visibility without
consumers reverse-engineering that join from route text, endpoint ids, or behavior docs. The same
namespace now also owns
`IRestEndpointCandidateRuntimeCatalog`,
`IRestEndpointCandidateRuntimeRegistry`, `RestEndpointCandidateProjectionDescriptor`,
`RestEndpointCandidateRuntimeDescriptor`, `RestEndpointCandidateStatus`,
`IRestEndpointPublicationGroupRuntimeCatalog`, and `RestEndpointPublicationGroupDescriptor` so
hosts can surface both candidate-level and grouped publication truth for module-owned REST
candidates without inventing an ASP.NET Core-specific precedence contract, and so operator tooling
can compare the original shorthand projection shape with the final effective projected endpoint
explicitly while also seeing skipped-governance visibility and the grouped
published-versus-suppressed answer per behavior. `RestEndpointCandidateStatus` now also carries
stable `published` / `suppressed` JSON wire names through
`RestEndpointCandidateStatusExtensions.GetWireName()` plus `TryParseWireName(...)`, so candidate
catalogs and snapshots no longer rely on raw enum-number serialization for operator-facing status
truth. Grouped publication answers now also carry typed
host-governance eligibility/ineligibility candidate buckets plus grouped skipped suppression and
override rule ids, plus typed
`RestEndpointPublicationGroupGovernanceSkippedSuppressionSummaryDescriptor` and
`RestEndpointPublicationGroupGovernanceSkippedOverrideSummaryDescriptor` entries through
`SkippedSuppressionSummaries` and `SkippedOverrideSummaries`, so callers do not need to repartition
the ordered candidate set just to see which behavior boundary stayed outside host governance or
which skipped host rule targeted which ineligible candidate ids. The same
transport namespace now also owns `IRestEndpointAuthoringPolicyRuntimeCatalog`,
`RestEndpointAuthoringPolicyDescriptor`,
`RestEndpointAuthoringPolicyAuthoringStyleDescriptor`, and
`RestEndpointAuthoringPolicySuppressionSummaryDescriptor`, so hosts can surface one behavior-level
REST authoring-policy answer directly, including explicitly configured policies with no current
candidates plus separate `CandidateIds`, `RetainedCandidateIds`, `PublishedCandidateIds`,
`PrecedenceSuppressedCandidateIds`, `GovernanceSuppressedCandidateIds`,
`SuppressedCandidateIds`, grouped authoring-policy suppression summaries, and per-style
`AuthoringStyleSummaries` that partition those same runtime buckets by normalized authoring style
without reopening grouped publication answers first. That same authoring-policy contract now also
keeps `HostGovernanceEligibleCandidateIds`, `HostGovernanceIneligibleCandidateIds`,
`SkippedSuppressionIds`, `SkippedOverrideIds`, `GovernanceSuppressionSummaries`,
`GovernanceOverrideSummaries`, `SkippedSuppressionSummaries`, and
`SkippedOverrideSummaries` visible at both the behavior and per-style level so callers can see
where explicit ownership kept host governance out of scope and which rules matched, won, applied,
or were skipped without reopening publication-group answers first. The same
transport namespace now also exposes
`RestEndpointPublicationGroupGovernanceSelectionBasisSummaryDescriptor` plus
`RestEndpointPublicationGroupGovernanceOverrideActionKindSummaryDescriptor`, and grouped
governance summaries now surface those typed buckets through `SelectionBasisSummaries`,
`SelectedActionKindSummaries`, and `AppliedActionKindSummaries` so grouped publication answers can
explain decisive governance precedence and declared-versus-effective override action visibility
without reopening the candidate catalog. The same
transport namespace now also lets the rule catalogs publish the inverse view directly:
`RestEndpointSuppressionDescriptor` now carries `MatchedCandidateIds`,
`SuppressedCandidateIds`, `SkippedCandidateIds`, `SelectionBases`, `SelectionBasisSummaries`, and
additive `HostGovernanceScopes`, while `RestEndpointOverrideDescriptor` now carries
`MatchedCandidateIds`, `SelectedCandidateIds`, `AppliedCandidateIds`, `SkippedCandidateIds`,
`SelectionBases`, `SelectionBasisSummaries`, additive `HostGovernanceScopes`,
`SelectedActionKinds`, `SelectedActionKindSummaries`, `AppliedActionKinds`, and
`AppliedActionKindSummaries`, so callers can inspect one rule's runtime footprint and grouped
provenance without rejoining the grouped or per-candidate answers first. The same transport
namespace now also owns the reusable grouped bucket contracts
`RestEndpointGovernanceSelectionBasisSummaryDescriptor` and
`RestEndpointGovernanceOverrideActionKindSummaryDescriptor`, so behavior-grouped and rule-centric
governance answers reuse one stable candidate-bucket shape. The same
transport namespace now also owns `IRestEndpointOverrideRuntimeCatalog` plus
`RestEndpointOverrideDescriptor`, including shorthand binding resets through `ClearBindings` plus
the shorthand endpoint-metadata clear actions `ClearEndpointName`, `ClearSummary`, and
`ClearDescription`, plus feature-rollout actions `RequiredFeatureFlagIds` and
`ClearRequiredFeatureFlags`, so hosts can publish set-or-clear governance truth without inventing ASP.NET
Core-specific override DTOs. `RestEndpointCandidateProjectionDescriptor` now also carries optional
`HostGovernanceScope`, and the same candidate/runtime descriptor family keeps that original
selector truth visible through `OriginalProjection.HostGovernanceScope` alongside original
shorthand endpoint metadata on `ProjectedEndpoint` through `OriginalEndpointName`,
`OriginalSummary`, and `OriginalDescription`, keeps preserved shorthand fallback truth visible
through typed `BindingFallbackMode` properties that distinguish preserved source implicit fallback
from preserved remaining request-body fallback, and now also exposes
`RestEndpointBindingFallbackModeExtensions.GetWireName()` plus `TryParseWireName(...)` as the
canonical compatibility bridge for the stable wire names used by JSON serialization and additive
`metadata.bindingFallbackMode`, while keeping that original metadata visible even when
a host-level override intentionally clears the effective endpoint metadata, while additive
`metadata.bindingFallbackMode`, `metadata.authoringStyle`, `metadata.routeGroupPrefix`,
`metadata.relativePattern`, `metadata.behaviorType`, and `metadata.sourceId` remain
compatibility-only metadata.

## Related docs

- [Architecture](../architecture.md)
- [App models](../app-models.md)
- [Module authoring](../module-authoring.md)
