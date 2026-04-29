using Cephalon.Abstractions.Agentics;
using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Authorization;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Features;
using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.Retrieval;
using Cephalon.Abstractions.Resilience;
using Cephalon.Abstractions.Technologies;
using Cephalon.Abstractions.Transports;
using Cephalon.Engine.Diagnostics;
using Cephalon.Engine.Manifest;

namespace Cephalon.Engine.Runtime;

/// <summary>
/// Combines the main operator-facing runtime views into a single payload.
/// </summary>
/// <param name="Manifest">The immutable manifest that describes the built runtime shape.</param>
/// <param name="Status">The current lifecycle status of the runtime.</param>
/// <param name="ExecutionGraphs">
/// The execution graphs contributed by active modules and visible to the runtime at the time the snapshot was created.
/// </param>
/// <param name="TechnologySurfaces">
/// The active technology-pack runtime surfaces visible to the runtime at the time the snapshot was created.
/// </param>
/// <param name="DiagnosticsConventions">
/// The diagnostics conventions and published event-id catalogs visible to the runtime at the time the snapshot was created.
/// </param>
/// <param name="OperationalStory">
/// The richer operator-facing lifecycle story that combines loaded packages, execution-graph state, hosted-execution state, module state, and the ordered runtime timeline.
/// </param>
/// <remarks>
/// This snapshot is intended for tooling and operator surfaces that need one coherent view of the runtime
/// without issuing separate requests for manifest, status, execution-graph details, hosted-execution details, technology-pack details,
/// diagnostics conventions, data product details, CDC capture details, data projection details, outbox details, inbox details,
/// agent-tool run-state details, retrieval index-state details, event-publication runtime details, event-dispatch runtime details, event-subscription execution-readiness details,
/// durable-execution runtime details, authorization-policy details, database-migration playbook details,
/// database-topology posture details, and lifecycle story data.
/// </remarks>
public sealed record RuntimeIntrospectionSnapshot(
    RuntimeManifest Manifest,
    RuntimeStatusSnapshot Status,
    IReadOnlyList<ExecutionGraphDescriptor> ExecutionGraphs,
    IReadOnlyList<TechnologyRuntimeSurface> TechnologySurfaces,
    IReadOnlyList<DiagnosticsConvention> DiagnosticsConventions,
    RuntimeOperationalStory OperationalStory)
{
    /// <summary>
    /// Gets the cell boundaries visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<CellBoundaryDescriptor> CellBoundaries { get; init; } = [];

    /// <summary>
    /// Gets the cell-to-cell routing and governance answers visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<CellRouteDescriptor> CellRoutes { get; init; } = [];

    /// <summary>
    /// Gets the cell health-isolation answers visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<CellHealthIsolationDescriptor> CellHealthIsolations { get; init; } = [];

    /// <summary>
    /// Gets the effective cell traffic-automation answers visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> CellTrafficAutomations { get; init; } = [];

    /// <summary>
    /// Gets the hosted executions contributed by active modules and visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<HostedExecutionDescriptor> HostedExecutions { get; init; } = [];

    /// <summary>
    /// Gets the projections contributed by active modules and visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<ProjectionDescriptor> Projections { get; init; } = [];

    /// <summary>
    /// Gets the data products contributed by active modules and visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<DataProductDescriptor> DataProducts { get; init; } = [];

    /// <summary>
    /// Gets the CDC captures contributed by active modules and visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<CdcCaptureDescriptor> CdcCaptures { get; init; } = [];

    /// <summary>
    /// Gets the CDC runtime-state entries visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<CdcCaptureRuntimeState> CdcCaptureStates { get; init; } = [];

    /// <summary>
    /// Gets the configured CDC capture execution runtimes visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> CdcCaptureExecutionRuntimes { get; init; } = [];

    /// <summary>
    /// Gets the outbox surfaces contributed by active modules and visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<OutboxDescriptor> Outboxes { get; init; } = [];

    /// <summary>
    /// Gets the inbox surfaces contributed by active modules and visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<InboxDescriptor> Inboxes { get; init; } = [];

    /// <summary>
    /// Gets the engine-owned database-role catalog visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<DatabaseRoleDescriptor> DatabaseRoles { get; init; } = [];

    /// <summary>
    /// Gets the database-migration catalog visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<DatabaseMigrationDescriptor> DatabaseMigrations { get; init; } = [];

    /// <summary>
    /// Gets the engine-owned ordered database-migration playbook visible to the runtime at the time the snapshot was created.
    /// </summary>
    public DatabaseMigrationOperationalPlaybook? DatabaseMigrationPlaybook { get; init; }

    /// <summary>
    /// Gets the engine-owned database-topology posture snapshot visible to the runtime at the time the snapshot was created.
    /// </summary>
    public DatabaseTopologyOperationalSnapshot? DatabaseTopology { get; init; }

    /// <summary>
    /// Gets the authorization policies contributed by active modules and visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<AuthorizationPolicyDescriptor> AuthorizationPolicies { get; init; } = [];

    /// <summary>
    /// Gets the latest reported agent-tool run states visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<AgentToolRunState> AgentToolRuns { get; init; } = [];

    /// <summary>
    /// Gets the latest managed retrieval index states visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<KnowledgeIndexState> KnowledgeIndexes { get; init; } = [];

    /// <summary>
    /// Gets the latest reported event-publication runtime state entries visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<EventPublicationRuntimeState> EventPublicationStates { get; init; } = [];

    /// <summary>
    /// Gets the configured event-dispatch runtimes visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<EventDispatchRuntimeDescriptor> EventDispatchRuntimes { get; init; } = [];

    /// <summary>
    /// Gets the latest reported event-dispatch runtime state entries visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<EventDispatchRuntimeState> EventDispatchStates { get; init; } = [];

    /// <summary>
    /// Gets the event-subscription execution-readiness entries visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<EventSubscriptionExecutionReadinessDescriptor> EventSubscriptionExecutionReadiness { get; init; } = [];

    /// <summary>
    /// Gets the audit-store surfaces contributed by active modules and visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<AuditStoreDescriptor> AuditStores { get; init; } = [];

    /// <summary>
    /// Gets the effective rate-limiting policies visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<RateLimitingRuntimeDescriptor> RateLimitingPolicies { get; init; } = [];

    /// <summary>
    /// Gets the feature flags visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<FeatureFlagDescriptor> FeatureFlags { get; init; } = [];

    /// <summary>
    /// Gets the active saga-choreography behaviors visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<SagaChoreographyRuntimeDescriptor> SagaChoreographies { get; init; } = [];

    /// <summary>
    /// Gets the latest reported saga-choreography publication-state entries visible to the runtime at
    /// the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<SagaChoreographyPublicationRuntimeState> SagaChoreographyPublicationStates { get; init; } = [];

    /// <summary>
    /// Gets the resolved public REST endpoints visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<RestEndpointRuntimeDescriptor> RestEndpoints { get; init; } = [];

    /// <summary>
    /// Gets the module-owned REST endpoint candidates visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<RestEndpointCandidateRuntimeDescriptor> RestEndpointCandidates { get; init; } = [];

    /// <summary>
    /// Gets the grouped module-owned REST publication answers visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<RestEndpointPublicationGroupDescriptor> RestEndpointPublicationGroups { get; init; } = [];

    /// <summary>
    /// Gets the behavior-level REST authoring-policy answers visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<RestEndpointAuthoringPolicyDescriptor> RestEndpointAuthoringPolicies { get; init; } = [];

    /// <summary>
    /// Gets the host-level REST endpoint override rules visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<RestEndpointOverrideDescriptor> RestEndpointOverrides { get; init; } = [];

    /// <summary>
    /// Gets the host-level REST endpoint suppression rules visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<RestEndpointSuppressionDescriptor> RestEndpointSuppressions { get; init; } = [];

    /// <summary>
    /// Gets the effective behavior-execution resilience policies visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<BehaviorResilienceRuntimeDescriptor> BehaviorResiliencePolicies { get; init; } = [];

    /// <summary>
    /// Gets the active durable-execution workflows visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<DurableExecutionRuntimeDescriptor> DurableExecutions { get; init; } = [];

    /// <summary>
    /// Gets the latest reported durable-execution runtime state entries visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<DurableExecutionRuntimeState> DurableExecutionStates { get; init; } = [];

    /// <summary>
    /// Gets the backend-for-frontend client bindings visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<BackendForFrontendClientBindingDescriptor> BackendForFrontendBindings { get; init; } = [];

    /// <summary>
    /// Gets the client-aware published REST endpoint projections derived from the active
    /// backend-for-frontend bindings and visible to the runtime at the time the snapshot was
    /// created.
    /// </summary>
    public IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor> BackendForFrontendRestEndpoints { get; init; } = [];

    /// <summary>
    /// Gets the client-aware REST documentation surfaces derived from the active backend-for-frontend
    /// bindings and visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<BackendForFrontendRestDocumentRuntimeDescriptor> BackendForFrontendRestDocuments { get; init; } = [];

    /// <summary>
    /// Gets the strangler-fig migration routes visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<StranglerFigRouteDescriptor> StranglerFigRoutes { get; init; } = [];

    /// <summary>
    /// Gets the effective strangler-fig migration-policy answers visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<StranglerFigMigrationRuntimeDescriptor> StranglerFigRoutePolicies { get; init; } = [];

    /// <summary>
    /// Gets the effective strangler-fig ingress materialization answers visible to the runtime at the time the snapshot was created.
    /// </summary>
    public IReadOnlyList<StranglerFigIngressRuntimeDescriptor> StranglerFigIngressRoutes { get; init; } = [];
}
