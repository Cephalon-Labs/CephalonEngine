# Cephalon.Abstractions

> **Maturity:** `M4` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Abstractions` is the stable contract layer that modules, hosts, and companion packages build against.

See also: [Engine surface maturity audit](../engine-surface-maturity-audit.md), [Conformance matrix](../conformance-matrix.md), and [Runtime contract index](../runtime-contract-index.md) for the per-package adoption-truth, maturity, ownership, and `I*Catalog` interface inventory that this contract layer ships. [Long-range engine direction](../long-range-direction.md) frames why these contracts stay host-agnostic and additive across multi-decade horizons; [Engineering standards](../engineering-standards.md) records the library-design and binary-stability baseline these public contracts ship against.

## What it owns

- module contracts such as `IModule`, `IModuleLifecycle`, `ModuleBase`, `ModuleDescriptor`, `ModuleContext`, `ModuleDiscoveryDescriptor`, and `ModuleDiscoveryRegistry`
- behavior contracts such as `IAppBehavior<TIn, TOut>`, `IBehaviorContext`, `IBehaviorTopologyBuilder`, `BehaviorTopologyDescriptor`, `BehaviorFeatureDisabledException`, `IBehaviorOwnerModule`, `IBehaviorModuleBuilder`, and `OwnedBehaviorRegistration`
- agentics read and operator-action contracts such as `AgentToolExecutionOutcomes`, `AgentToolExecutionRequest`, `AgentToolExecutionResult`, `AgentToolRunState`, `IAgentToolDispatcher`, and `IAgentToolRunCatalog`
- retrieval read and operator-action contracts such as `KnowledgeIndexState`, `KnowledgeIndexFreshnessStates`, `KnowledgeIndexingOutcomes`, `IKnowledgeIndexCatalog`, `IKnowledgeIndexer`, `KnowledgeIndexingRequest`, `KnowledgeIndexingResult`, `IKnowledgeQueryEngine`, `KnowledgeQueryRequest`, `KnowledgeQueryResult`, and `KnowledgeQueryMatch`
- eventing read and operator-action contracts such as `EventPublicationOutcomes`, `EventPublicationRequest`, `EventPublicationResult`, `IEventPublicationDispatcher`, `EventSubscriptionExecutionReadinessDescriptor`, `EventSubscriptionExecutionReadinessStates`, and `IEventSubscriptionExecutionReadinessCatalog`
- capability contracts such as `Capability`, `CapabilityAccess`, and `ICapabilityRegistry`
- feature-flag contracts such as `FeatureFlagDescriptor`, `FeatureFlagProviderBindingDescriptor`, `FeatureFlagProviderEvaluationResult`, `FeatureFlagTargetingDescriptor`, `IFeatureToggle`, `IFeatureFlagProvider`, `IFeatureFlagRuntimeCatalog`, `IFeatureFlagContributor`, and `IFeatureFlagRegistry`
- execution/runtime-catalog contracts such as `DurableExecutionRuntimeDescriptor`, `IDurableExecutionRuntimeCatalog`, `DurableExecutionRuntimeState`, `IDurableExecutionRuntimeStateCatalog`, `SagaChoreographyRuntimeDescriptor`, `ISagaChoreographyRuntimeCatalog`, `SagaChoreographyPublicationRuntimeState`, and `ISagaChoreographyPublicationRuntimeStateCatalog`
- technology runtime contracts such as `CellBoundaryDescriptor`, `ICellBoundaryContributor`, `ICellBoundaryRegistry`, `ICellBoundaryCatalog`, `CellRouteDescriptor`, `ICellRouteContributor`, `ICellRouteRegistry`, `ICellRouteCatalog`, `CellHealthIsolationDescriptor`, `ICellHealthIsolationContributor`, `ICellHealthIsolationRegistry`, `ICellHealthIsolationCatalog`, `CellTrafficAutomationRuntimeDescriptor`, `ICellTrafficAutomationRuntimeCatalog`, `TechnologyDescriptor`, and `ITechnologyRuntimeCatalog`
- app-model contracts such as `AppBlueprint`, `AppProfile`, resilience-selection types, and scaffold-plan types
- phase-8 and phase-13 runtime-neutral contracts for data, authorization, tenancy, audit, and id generation
- health contracts used across hosts and packages
- localization contracts used by engine resources and package language packs
- pattern, migration-routing, technology, and transport contracts shared by the whole stack
- REST endpoint wire-name helper extensions for transport/governance enums; these use closed switch mappings so JSON/runtime wire names stay stable without enum-field reflection in trim/AOT analysis

## Deployment-mode posture

`Cephalon.Abstractions` carries a package-scoped `singleFile` claim through `scripts/deployment-mode-support.json`. The project declares `PublishSingleFile=true` and `EnableSingleFileAnalyzer=true`, and the harness reports the package claim separately from the global trim / Native AOT / single-file rows, which remain `not-claimed`.

## Main surfaces

- `Modules/IModule.cs`
- `Modules/IModuleLifecycle.cs`
- `Behaviors/IAppBehavior.cs`
- `Behaviors/IBehaviorContext.cs`
- `Behaviors/IBehaviorTopologyBuilder.cs`
- `Behaviors/IBehaviorOwnerModule.cs`
- `Behaviors/IBehaviorModuleBuilder.cs`
- `Behaviors/OwnedBehaviorRegistration.cs`
- `Agentics/AgentToolExecutionOutcomes.cs`
- `Agentics/AgentToolExecutionRequest.cs`
- `Agentics/AgentToolExecutionResult.cs`
- `Agentics/AgentToolRunState.cs`
- `Agentics/IAgentToolDispatcher.cs`
- `Agentics/IAgentToolRunCatalog.cs`
- `Retrieval/IKnowledgeIndexCatalog.cs`
- `Retrieval/IKnowledgeIndexer.cs`
- `Retrieval/IKnowledgeQueryEngine.cs`
- `Retrieval/KnowledgeIndexFreshnessStates.cs`
- `Retrieval/KnowledgeIndexingOutcomes.cs`
- `Retrieval/KnowledgeIndexingRequest.cs`
- `Retrieval/KnowledgeIndexingResult.cs`
- `Retrieval/KnowledgeIndexState.cs`
- `Retrieval/KnowledgeQueryMatch.cs`
- `Retrieval/KnowledgeQueryRequest.cs`
- `Retrieval/KnowledgeQueryResult.cs`
- `Capabilities/Capability.cs`
- `Capabilities/ICapabilityRegistry.cs`
- `Features/FeatureFlagDescriptor.cs`
- `Features/FeatureFlagProviderBindingDescriptor.cs`
- `Features/FeatureFlagProviderEvaluationResult.cs`
- `Features/FeatureFlagTargetingDescriptor.cs`
- `Features/IFeatureFlagProvider.cs`
- `Features/IFeatureToggle.cs`
- `Features/IFeatureFlagRuntimeCatalog.cs`
- `Execution/DurableExecutionRuntimeDescriptor.cs`
- `Execution/IDurableExecutionRuntimeCatalog.cs`
- `Execution/DurableExecutionRuntimeState.cs`
- `Execution/IDurableExecutionRuntimeStateCatalog.cs`
- `Execution/SagaChoreographyRuntimeDescriptor.cs`
- `Execution/ISagaChoreographyRuntimeCatalog.cs`
- `Execution/SagaChoreographyPublicationRuntimeState.cs`
- `Execution/ISagaChoreographyPublicationRuntimeStateCatalog.cs`
- `AppModel/AppProfile.cs`
- `AppModel/SuiteBlueprint.cs`
- `AppModel/Scaffolding/ScaffoldPlan.cs`
- `AppModel/Scaffolding/SuiteScaffoldPlan.cs`
- `AppModel/Scaffolding/SuiteScaffoldService.cs`
- `Data/ICommand.cs`
- `Data/IDataProduct.cs`
- `Data/DataProductDescriptor.cs`
- `Data/IDataProductCatalog.cs`
- `Data/ICdcCapture.cs`
- `Data/CdcCaptureExecutionResult.cs`
- `Data/CdcCaptureDescriptor.cs`
- `Data/CdcCaptureExecutionBindingDescriptor.cs`
- `Data/ICdcCaptureCatalog.cs`
- `Data/ICdcCaptureContributor.cs`
- `Data/ICdcCaptureRegistry.cs`
- `Data/CdcCaptureFreshnessStatus.cs`
- `Data/CdcCaptureLagStatus.cs`
- `Data/CdcCapturePublicationStatus.cs`
- `Data/CdcCaptureReporterCoordinationIssueReasons.cs`
- `Data/CdcCaptureReporterTakeoverStates.cs`
- `Data/CdcCaptureRuntimeState.cs`
- `Data/ICdcCaptureRuntimeStateCatalog.cs`
- `Data/CdcCaptureExecutionRuntimeDescriptor.cs`
- `Data/CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.cs`
- `Data/CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories.cs`
- `Data/CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.cs`
- `Data/CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus.cs`
- `Data/CdcCaptureExecutionRuntimeManagedConnectorGovernanceActionIds.cs`
- `Data/CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories.cs`
- `Data/CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.cs`
- `Data/CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus.cs`
- `Data/CdcCaptureExecutionRuntimeRemediationCategories.cs`
- `Data/CdcCaptureExecutionRuntimeRemediationStates.cs`
- `Data/CdcCaptureExecutionRuntimeRemediationStatus.cs`
- `Data/CdcCaptureExecutionRuntimeReportingCoverageStates.cs`
- `Data/CdcCaptureExecutionRuntimeReportingCoverageStatus.cs`
- `Data/CdcCaptureExecutionRuntimeSummary.cs`
- `Data/ICdcCaptureExecutionRuntimeCatalog.cs`
- `Data/CdcCaptureRuntimeObservation.cs`
- `Data/ICdcCaptureExecutionRuntimeReportSink.cs`
- `Data/IReadStore.cs`
- `Data/ProjectionDescriptor.cs`
- `Data/InboxDescriptor.cs`
- `Data/IInbox.cs`
- `Data/IInboxCatalog.cs`
- `Data/OutboxDescriptor.cs`
- `Data/IOutbox.cs`
- `Data/IOutboxCatalog.cs`
- `Data/EventPublicationOutcomes.cs`
- `Data/EventPublicationRequest.cs`
- `Data/EventPublicationResult.cs`
- `Data/IEventPublicationDispatcher.cs`
- `Data/EventSubscriptionExecutionReadinessDescriptor.cs`
- `Data/EventSubscriptionExecutionReadinessStates.cs`
- `Data/IEventSubscriptionExecutionReadinessCatalog.cs`
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
- `Technologies/CellBoundaryDescriptor.cs`
- `Technologies/ICellBoundaryCatalog.cs`
- `Technologies/CellHealthIsolationDescriptor.cs`
- `Technologies/ICellHealthIsolationCatalog.cs`
- `Technologies/CellTrafficAutomationMaterializationResult.cs`
- `Technologies/CellTrafficAutomationMaterializationStates.cs`
- `Technologies/CellTrafficAutomationRuntimeDescriptor.cs`
- `Technologies/ICellTrafficAutomationEdgeMaterializer.cs`
- `Technologies/ICellTrafficAutomationProviderMaterializer.cs`
- `Technologies/CellTrafficAutomationProviderMaterializationResult.cs`
- `Technologies/CellTrafficAutomationProviderMaterializationStates.cs`
- `Technologies/ICellTrafficAutomationRuntimeCatalog.cs`
- `Technologies/CellRouteDescriptor.cs`
- `Technologies/ICellRouteCatalog.cs`
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
- `Agentics`
- `Audit`
- `Authorization`
- `Behaviors`
- `Capabilities`
- `Data`
- `Execution`
- `Features`
- `Health`
- `Ids`
- `Localization`
- `Modules`
- `Patterns`
- `Retrieval`
- `Tenancy`
- `Technologies`
- `Transports`

## How it fits

The engine should depend on this package for contracts only. New runtime behavior belongs in `Cephalon.Engine` or a companion package unless it must become part of the public module authoring surface.

The behavior ownership contracts now follow that rule directly:

- `IBehaviorOwnerModule` and `IBehaviorModuleBuilder` let one module declare the behaviors it owns without leaking ASP.NET Core or other host APIs into `Cephalon.Abstractions`
- `IBehaviorModuleBuilder` exposes both generic `Add<TBehavior>()` overloads and type-based `Add(Type, ...)` overloads so adapter packages can register module-owned behaviors discovered from generated metadata without reflecting over the generic authoring methods; the public contract also carries trim/AOT annotations so generic behavior types preserve interface metadata while type-based closed input/output registration remains an explicit compatibility boundary for trim-ready hosts
- `OwnedBehaviorRegistration` is the normalized ownership record the engine composes at build time
- public REST exposure still belongs in adapter packages such as `Cephalon.Behaviors.Http`, so module ownership and HTTP route mapping stay separate concerns

The same host-agnostic rule now covers agent-tool run-state reads. `AgentToolExecutionOutcomes`,
`AgentToolRunState`, and `IAgentToolRunCatalog` live here so `Cephalon.Engine`, host adapters, and
operator tooling can read latest agent-tool run posture through `/engine/snapshot` and host routes
without referencing `Cephalon.Agentics` directly. The implementation and write path still belong to
the selected agentics pack through its dispatcher, executors, policies, observers, and reporter.

The same host-agnostic rule now covers retrieval index-state reads, manual reindex command requests,
and bounded query command requests. `KnowledgeIndexState`, `KnowledgeIndexFreshnessStates`,
`KnowledgeIndexingOutcomes`, `IKnowledgeIndexCatalog`, `IKnowledgeIndexer`,
`KnowledgeIndexingRequest`, `KnowledgeIndexingResult`, `IKnowledgeQueryEngine`,
`KnowledgeQueryRequest`, `KnowledgeQueryResult`, and `KnowledgeQueryMatch` live here so
`Cephalon.Engine`, host adapters, and operator tooling can read managed knowledge-index posture,
request a bounded reindex action, and execute a bounded collection query through `/engine/snapshot`
and host routes without referencing `Cephalon.Retrieval` directly. The indexer/query implementation,
document-provider integration, and provider-specific write path still belong to the selected
retrieval pack.

The phase-8 families stay runtime-neutral on purpose:

- `Data` defines CQRS, data-product, CDC capture, CDC runtime-state, projection, outbox/inbox, and outbox-catalog contracts without picking Entity Framework, Wolverine, or any storage engine.
- `Authorization` defines subjects, resources, policies, and evaluation contracts without binding to ASP.NET Core identity types.
- `Tenancy` defines tenant context and resolution contracts without assuming HTTP, DNS, or a single tenancy topology.
- `Audit` defines audit actors, entries, write/query/export contracts, and audit-store descriptors without hard-coding storage or observability sinks.
- `Ids` defines identifier-generation hints and the generator contract without choosing a concrete strategy such as `Sfid`.

That same data contract family now also keeps shared CDC execution and operator answers
host-agnostic. `ICdcCapture` now returns `CdcCaptureExecutionResult` so provider packs can report
one bounded capture batch, its outbox-ready publications, checkpoints, freshness/lag/publication
posture, and additive metadata without hard-coding one hosting model. `IOutbox.OutboxId` keeps the
durable delivery boundary explicit instead of collapsing CDC capture into broker or storage
delivery.

`CdcCaptureRuntimeState` plus `ICdcCaptureRuntimeStateCatalog` then let provider packs and hosts
report latest capture posture, totals, checkpoints, errors, typed freshness/lag/publication
posture, and optional linked `EventDispatchRuntimeState` publication posture without turning CDC
execution into an ASP.NET Core-only or provider-specific registry.

`CdcCaptureExecutionBindingDescriptor` now keeps the inverse per-capture ownership answer
host-agnostic too, so CDC descriptor and runtime-state surfaces can both report authored, requested,
and effective execution-runtime ids plus the resolved ownership mode and topology without forcing
packs or hosts to infer capture ownership only from runtime-side membership lists or metadata.
`ICdcCaptureCatalog` and `ICdcCaptureRuntimeStateCatalog` now also expose inverse
`GetByExecutionRuntimeId(...)` lookups directly, so hosts and higher-level runtime catalogs can
reuse one capture-first ownership truth instead of re-filtering runtime memberships ad hoc.

`CdcCaptureExecutionRuntimeDescriptor`, `CdcCaptureExecutionRuntimeSummary`, and
`ICdcCaptureExecutionRuntimeCatalog` then keep the execution-topology layer host-agnostic too, so
shared or provider-specific CDC runners can publish one operator-facing runtime answer with stable
ownership metadata, first-class `executionOwnership`, `executionTopology`,
`acknowledgementMode`, `hostedExecutionId`, and `executionGraphId` fields, bounded linked capture
ids, aggregate latest-plus-total posture, and replay-safe acknowledgement visibility without
inventing a second host-only runner inventory beside the per-capture descriptor and runtime-state
catalogs.

`CdcCaptureRuntimeObservation` plus `ICdcCaptureExecutionRuntimeReportSink` now keep the
out-of-process reporting seam host-agnostic too. External or managed execution runtimes can report
capture observations back through the same descriptor-backed runtime-state catalog by stable
`executionRuntimeId` and `cdcCaptureId`, while higher-level hosts and adapters can validate the
effective ownership boundary without introducing a second HTTP-only or provider-only CDC status
contract.

The same data contract family now carries the event-subscription execution-readiness read seam.
`EventSubscriptionExecutionReadinessDescriptor`, `EventSubscriptionExecutionReadinessStates`, and
`IEventSubscriptionExecutionReadinessCatalog` live here so `Cephalon.Engine`, host adapters, and
operator tooling can read subscription readiness through `/engine/snapshot` and HTTP routes without
referencing `Cephalon.Eventing` directly. The implementation still belongs to the eventing pack or
an optional companion; the abstraction only defines the host-agnostic answer for whether a declared
subscription is `runtime-bound`, `hosted-execution-linked`, `application-managed-state`, or
`declared-only`.

The same eventing contract family now also carries a bounded publication action seam.
`EventPublicationOutcomes`, `EventPublicationRequest`, `EventPublicationResult`, and
`IEventPublicationDispatcher` live here so host adapters can request one publication through the
active eventing runtime without referencing `Cephalon.Eventing` implementation types. The selected
eventing pack still owns the actual publish path, whether that path is the opt-in direct
in-process publisher or an outbox-backed staged handoff.

That same reporting seam now also keeps retry, reporter identity, edge topology, ordering, and
freshness-expiry policy host-agnostic. `CdcCaptureRuntimeObservation.ReportId` gives external
runners a stable idempotency key for retry-safe submissions, while `ReporterId` plus `EdgeNodeId`
keep one runner or edge agent observable without baking host-local lease or topology rules into an
adapter. `CdcCaptureExecutionRuntimeDescriptor` now exposes `ObservationStaleAfterSeconds`,
`RejectOutOfOrderReports`, `ReporterLeaseSeconds`, `RejectConflictingReporterIds`, and declared
`EdgeNodeIds` for runtime policy, `CdcCaptureRuntimeState` now preserves `LastReportId`,
`LastReporterId`, `ReporterLeaseExpiresAtUtc`, `LastEdgeNodeId`, typed `ObservationFreshness`, and
typed `ReporterCoordination`, while `CdcCaptureExecutionRuntimeSummary` can aggregate `fresh`,
`stale`, `mixed`, or `unknown` observation posture together with `LastReporterId`,
`ActiveReporterId`, `ReporterLeaseExpiresAtUtc`, `ObservedEdgeNodeIds`, `LastEdgeNodeId`, and the
same `ReporterCoordination` answer. That coordination answer now keeps first-class `active`,
`lease-expired`, `conflicted`, `not-configured`, and `unreported` posture together with active
owner, previous owner, lease-expiry, last takeover, and last conflicting reporter metadata without
forcing adapters to invent their own HTTP-local failover, takeover, or degraded-topology
contracts. It now also keeps participant-level operator truth through
`CdcCaptureReporterParticipantRoles`, `CdcCaptureReporterParticipantStatus`, and
`CdcCaptureReporterCoordinationStatus.ReporterParticipants`, so shared CDC surfaces can answer
which reporters are currently `active`, `standby`, or `rejected` together with the additive
`HasStandbyReporters`, `HasRejectedReporters`, `ParticipantCount`, `ActiveReporterCount`,
`StandbyReporterCount`, and `RejectedReporterCount` summaries instead of leaving operator flows to
infer that story from one last-conflict field. The same shared contract now also publishes stable
`TakeoverState` and `DegradedReason` identifiers together with derived `RequiresTakeover`,
`HasCompletedTakeover`, and `HasMultipleActiveReporters` helpers, so hosts and tooling can
distinguish an `awaiting-takeover` expired lease from a `rejected-reporter-conflict`, a
`multiple-active-reporters` ambiguity, or a completed handoff without inventing a second operator
taxonomy. Later accepted reports can now also normalize stale rejected-conflict evidence and old
takeover standby evidence without changing this shared contract shape, so operator surfaces stay
truthful while historical `PreviousReporterId` plus takeover timestamps remain available. That
same shared execution-runtime contract now also publishes declared-versus-reported coverage
through `CdcCaptureExecutionRuntimeReportingCoverageStates`,
`CdcCaptureExecutionRuntimeReportingCoverageStatus`,
`CdcCaptureExecutionRuntimeSummary.ReportingCoverage`, and the derived
`HasUnreportedDeclaredCaptures` / `HasFullCaptureCoverage` helpers, so hosts and tooling can tell
whether one runtime is `not-bound`, `unreported`, `partially-reported`, or `fully-reported`
without inventing a second external-runtime coverage vocabulary.

That same shared execution-runtime contract now also publishes aggregate remediation posture
through `CdcCaptureExecutionRuntimeRemediationStates`,
`CdcCaptureExecutionRuntimeRemediationCategories`,
`CdcCaptureExecutionRuntimeRemediationStatus`,
`CdcCaptureExecutionRuntimeSummary.Remediation`, and the derived
`RequiresRemediation` / `HasBlockingRemediation` helpers, so hosts and tooling can tell whether
one runtime is currently `ready`, needs operator `attention`, or is `blocked` by failed captures
while still seeing the active remediation categories plus affected capture ids on the same shared
contract instead of inventing a second external-runtime remediation registry.

That same shared execution-runtime contract now also publishes managed-connector governance
posture through `CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates`,
`CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories`,
`CdcCaptureExecutionRuntimeManagedConnectorGovernanceActionIds`,
`CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus`,
`CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorGovernance`, and additive
`ICdcCaptureExecutionRuntimeCatalog.GetByManagedConnectorGovernanceState(...)` /
`GetByManagedConnectorGovernanceCategory(...)` filters. That keeps `managed-connector` runtimes
queryable as shared `not-applicable`, `observe-only`, `future-control-plane`, or
`out-of-policy` answers together with governance categories, recommended action ids, declared
versus reported task identity, and connector lifecycle or reconciliation context instead of
inventing a Debezium-only governance contract.

That same shared execution-runtime contract now also publishes managed-connector
desired-versus-observed drift posture through
`CdcCaptureExecutionRuntimeManagedConnectorDriftStates`,
`CdcCaptureExecutionRuntimeManagedConnectorDriftCategories`,
`CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds`,
`CdcCaptureExecutionRuntimeManagedConnectorDriftStatus`,
`CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorDrift`, and additive
`ICdcCaptureExecutionRuntimeCatalog.GetByManagedConnectorDriftState(...)` /
`GetByManagedConnectorDriftCategory(...)` filters. That keeps `managed-connector` runtimes
queryable as shared `not-applicable`, `unknown`, `in-sync`, or `drifted` answers together with
task-topology and connector-identity drift categories, recommended action ids, declared versus
reported connector identity, and reconciliation context instead of inventing a Debezium-only
drift contract.

That same shared execution-runtime contract now also publishes managed-connector action-planning
posture through `CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates`,
`CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories`,
`CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds`,
`CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus`,
`CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorActionPlan`, and additive
`ICdcCaptureExecutionRuntimeCatalog.GetByManagedConnectorActionPlanState(...)` /
`GetByManagedConnectorActionId(...)` filters. That keeps `managed-connector` runtimes queryable as
shared `not-applicable`, `observe`, `waiting`, `action-required`, or `blocked` answers together
with ordered action ids, operator-facing action-plan categories, and the source remediation,
governance, plus drift state instead of inventing a Debezium-only action-planning contract.

The same host-agnostic CDC runtime surface now also carries
`CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates`,
`CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessCategories`,
`CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus`,
`CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorWritePathReadiness`, and additive
`ICdcCaptureExecutionRuntimeCatalog.GetByManagedConnectorWritePathReadinessState(...)` /
`GetByManagedConnectorWritePathReadinessCategory(...)` filters. That keeps managed connectors
queryable as shared `not-applicable`, `deferred`, `not-ready`, `ready`, or `blocked` answers
together with stable readiness categories, source coverage/remediation/governance/drift/action-plan
state, and the current primary action id instead of inventing a Debezium-only write-path
readiness contract.

That same host-agnostic CDC runtime surface now also carries
`CdcCaptureExecutionRuntimeManagedConnectorPreflightStates`,
`CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories`,
`CdcCaptureExecutionRuntimeManagedConnectorPreflightOperationIds`,
`CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus`,
`CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorPreflight`, and additive
`ICdcCaptureExecutionRuntimeCatalog.GetByManagedConnectorPreflightState(...)` /
`GetByManagedConnectorPreflightCategory(...)` /
`GetByManagedConnectorPreflightOperationId(...)` filters. That keeps managed connectors queryable
as shared `not-applicable`, `deferred`, `not-ready`, `ready`, or `blocked` preflight answers
together with stable preflight categories, the intended operation id, source
coverage/remediation/governance/drift/action-plan/write-path-readiness state, and the current
primary action id instead of inventing a Debezium-only preflight contract.

That same host-agnostic CDC runtime surface now also carries
`CdcCaptureExecutionRuntimeManagedConnectorDryRunStates`,
`CdcCaptureExecutionRuntimeManagedConnectorDryRunCategories`,
`CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds`,
`CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus`,
`CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorDryRun`, and additive
`ICdcCaptureExecutionRuntimeCatalog.GetByManagedConnectorDryRunState(...)` /
`GetByManagedConnectorDryRunCategory(...)` /
`GetByManagedConnectorDryRunOperationId(...)` filters. That keeps managed connectors queryable
as shared `not-applicable`, `deferred`, `blocked`, `no-op`, or `would-change` dry-run answers
together with stable dry-run categories, the intended operation id, source
coverage/remediation/governance/drift/action-plan/write-path-readiness/preflight state, the
current primary action id, and additive potential-change detail instead of inventing a
Debezium-only dry-run contract.

That same host-agnostic CDC runtime surface now also carries
`CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates`,
`CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories`,
`CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds`,
`CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentSources`,
`CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus`,
`CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorExecutionIntent`, and additive
`ICdcCaptureExecutionRuntimeCatalog.GetByManagedConnectorExecutionIntentState(...)` /
`GetByManagedConnectorExecutionIntentCategory(...)` /
`GetByManagedConnectorExecutionIntentOperationId(...)` filters. That keeps managed connectors
queryable as shared `not-applicable`, `deferred`, `blocked`, `operator-action`,
`requires-approval`, or `ready-to-execute` answers together with stable execution-intent
categories, the intended operation id, source
coverage/remediation/governance/drift/action-plan/write-path-readiness/preflight/dry-run state,
the current primary action id, confidence-source truth, and additive potential-change detail
instead of inventing a Debezium-only execution planner.

That same host-agnostic CDC runtime surface now also carries
`CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates`,
`CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories`,
`CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds`,
`CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources`,
`CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus`,
`CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorExecutionApproval`, and additive
`ICdcCaptureExecutionRuntimeCatalog.GetByManagedConnectorExecutionApprovalState(...)` /
`GetByManagedConnectorExecutionApprovalCategory(...)` /
`GetByManagedConnectorExecutionApprovalOperationId(...)` filters. That keeps managed connectors
queryable as shared `not-applicable`, `auto-blocked`, `policy-blocked`, `approval-required`,
`approval-ready`, or `auto-eligible` answers together with stable execution-approval categories,
the intended operation id, source
coverage/remediation/governance/drift/action-plan/write-path-readiness/preflight/dry-run/execution-intent
state, the current primary action id, safety-gating source truth, and explicit-approval detail
instead of inventing a Debezium-only approval registry.

That same host-agnostic CDC runtime surface now also carries
`CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates`,
`CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeCategories`,
`CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds`,
`CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources`,
`CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus`,
`CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorCommandEnvelope`, and additive
`ICdcCaptureExecutionRuntimeCatalog.GetByManagedConnectorCommandEnvelopeState(...)` /
`GetByManagedConnectorCommandEnvelopeCategory(...)` /
`GetByManagedConnectorCommandEnvelopeOperationId(...)` filters. That keeps managed connectors
queryable as shared `not-applicable`, `blocked`, `operator-only`, `approval-gated`, or
`engine-ready` answers together with stable command-envelope categories, the intended operation
id, source coverage/remediation/governance/drift/action-plan/write-path-readiness/preflight/
dry-run/execution-intent/execution-approval state, the current primary action id, target
connector identity, deterministic command fingerprints, and safety flags instead of inventing a
Debezium-only execution registry.

That same abstractions surface now also carries
`CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates`,
`CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceCategories`,
`CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds`,
`CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources`,
`CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus`,
`CdcCaptureExecutionRuntimeDescriptor.ManagedConnectorCommandIssuance`, and additive
`ICdcCaptureExecutionRuntimeCatalog.GetByManagedConnectorCommandIssuanceState(...)` /
`GetByManagedConnectorCommandIssuanceCategory(...)` /
`GetByManagedConnectorCommandIssuanceOperationId(...)` filters. That keeps managed connectors
queryable as shared `not-applicable`, `blocked`, `operator-only`, `accepted`, `rejected`, or
`issued` answers together with stable command-issuance categories, the intended operation id,
source coverage/remediation/governance/drift/action-plan/write-path-readiness/preflight/dry-run/
execution-intent/execution-approval/command-envelope state, issuance source truth, deterministic
issuance fingerprints, and safety flags instead of inventing a Debezium-only issuance registry.

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

The same host-agnostic rule now also covers the phase 13 cell baseline. The `Technologies`
namespace now carries `CellBoundaryDescriptor`, `ICellBoundaryContributor`,
`ICellBoundaryRegistry`, `ICellBoundaryCatalog`, `CellRouteDescriptor`,
`ICellRouteContributor`, `ICellRouteRegistry`, `ICellRouteCatalog`,
`CellHealthIsolationDescriptor`, `ICellHealthIsolationContributor`,
`ICellHealthIsolationRegistry`, `ICellHealthIsolationCatalog`,
`CellTrafficAutomationRuntimeDescriptor`, and `ICellTrafficAutomationRuntimeCatalog` so modules,
hosts, and operator tooling can talk about explicit module-owned blast-radius boundaries,
governed cell-to-cell routing posture, cell health-isolation posture, and effective automation
policy without leaking ASP.NET Core ingress, service-mesh, traffic-manager, edge-pack, or
health-probe types into `Cephalon.Abstractions`. That same runtime descriptor now also carries
first-class optional `providerId` and `edgeNodeIds` targeting, while the shared catalog can answer
provider and edge-node drill-downs without depending on `Cephalon.Edge` concrete types. The same
host-agnostic surface now also includes `CellTrafficAutomationMaterializationResult`,
`CellTrafficAutomationMaterializationStates`, `ICellTrafficAutomationProviderMaterializer`, and
`ICellTrafficAutomationEdgeMaterializer`; the provider-named result/state types stay available as
compatibility helpers over that generic materialization contract. Provider materializers now expose
`Priority` plus `CanMaterialize(...)`, edge materializers now expose `Priority`, and the engine can
therefore keep deterministic highest-priority selection host-agnostic without leaking control-plane
SDK or edge runtime types into `Cephalon.Abstractions`. `CellTrafficAutomationRuntimeDescriptor`
now carries `materializationState`, `materializationObservedAtUtc`, `materializationError`,
`edgeMaterializerId`, `edgeMaterializationState`,
`edgeMaterializationObservedAtUtc`, `edgeMaterializationError`, `providerMaterializerId`,
`providerMaterializationState`, `providerMaterializationObservedAtUtc`, and
`providerMaterializationError` so provider-managed, edge-managed, or provider-and-edge-managed
automation reconciliation can stay visible on the shared runtime catalog without leaking
control-plane SDK or host-adapter types into `Cephalon.Abstractions`.

The same phase 12 rule now also covers backend-for-frontend REST documentation materialization.
`BackendForFrontendRestDocumentRuntimeDescriptor` and
`IBackendForFrontendRestDocumentRuntimeCatalog` live in the `Transports` namespace so hosts,
operator tooling, and companion packages can talk about one scope-specific REST document answer
without leaking ASP.NET Core `IOpenApiDocumentProvider`, Scalar, or route-mapper types into
`Cephalon.Abstractions`. Those contracts intentionally describe the derived runtime surface only:
binding-versus-client scope kind, scope id, client id, document name, published OpenAPI and Scalar
paths, and the binding/runtime/published-endpoint ids that justify that materialized document.

The same phase 12 rule now also covers progressive-delivery feature flags. The `Features`
namespace now carries `FeatureFlagDescriptor`, `FeatureFlagProviderBindingDescriptor`,
`FeatureFlagProviderEvaluationResult`, `FeatureFlagTargetingDescriptor`,
`FeatureFlagEvaluationContext`, `FeatureFlagEvaluationResult`, `IFeatureToggle`,
`IFeatureFlagProvider`, `IFeatureFlagRuntimeCatalog`, `IFeatureFlagContributor`, and
`IFeatureFlagRegistry` so modules, hosts, provider companion packs, and operator tooling can talk
about feature ownership, targeting, provider-backed rollout gates, merged runtime catalogs, and
evaluation context without leaking ASP.NET Core route mappers or third-party provider SDK types
into `Cephalon.Abstractions`. Those contracts intentionally separate host-owned and module-owned
flags through `FeatureFlagSourceKind`, let `FeatureFlagDescriptor.ProviderBindings` keep the local
Cephalon descriptor as the source of runtime truth, and let `IFeatureFlagProvider` contribute
additional gate decisions without claiming ownership of the flag itself.

The same host-agnostic rule now also covers choreography and durable execution operator truth. The
`Execution` namespace carries `SagaChoreographyRuntimeDescriptor` plus
`ISagaChoreographyRuntimeCatalog`, `SagaChoreographyPublicationRuntimeState` plus
`ISagaChoreographyPublicationRuntimeStateCatalog`, together with
`DurableExecutionRuntimeDescriptor`, `IDurableExecutionRuntimeCatalog`,
`DurableExecutionRuntimeState`, and `IDurableExecutionRuntimeStateCatalog`, so modules, hosts,
operator tooling, and companion packs can talk about static workflow ownership, choreography
publication handoff posture, transport exposure, feature gates, result-contract shape, and live
durable posture without leaking ASP.NET Core route mappers, event-store implementations, or
eventing-bridge internals into `Cephalon.Abstractions`. The choreography publication-state
contracts intentionally describe only the shared strategy-owned handoff observations, not
provider-specific outbox dispatch or broker delivery state.

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
- [Engine surface maturity audit](../engine-surface-maturity-audit.md)
- [Conformance matrix](../conformance-matrix.md)
- [Runtime contract index](../runtime-contract-index.md)
- [Long-range engine direction](../long-range-direction.md)
- [Engineering standards](../engineering-standards.md)
- [Architecture review (May 2026)](../architecture-review-2026-05.md)
- [Compatibility](../compatibility.md)
- [App models](../app-models.md)
- [Module authoring](../module-authoring.md)
