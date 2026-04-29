using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Authorization;
using Cephalon.Abstractions.Agentics;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Features;
using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.Retrieval;
using Cephalon.Abstractions.Resilience;
using Cephalon.Abstractions.Technologies;
using Cephalon.Abstractions.Transports;
using Cephalon.Engine.Diagnostics;

namespace Cephalon.Engine.Runtime;

internal sealed class RuntimeIntrospectionSnapshotProvider(
    IServiceProvider serviceProvider,
    IRuntime runtime,
    IExecutionRuntimeCatalog executionRuntimeCatalog,
    IHostedExecutionRuntimeCatalog hostedExecutionRuntimeCatalog,
    IDataProductCatalog dataProductCatalog,
    ICdcCaptureCatalog cdcCaptureCatalog,
    IProjectionCatalog projectionCatalog,
    IOutboxCatalog outboxCatalog,
    IInboxCatalog inboxCatalog,
    IDatabaseRoleCatalog databaseRoleCatalog,
    IDatabaseMigrationCatalog databaseMigrationCatalog,
    IDatabaseMigrationOperationalPlaybookProvider databaseMigrationOperationalPlaybookProvider,
    IDatabaseTopologyOperationalSnapshotProvider databaseTopologyOperationalSnapshotProvider,
    IAuditStoreCatalog auditStoreCatalog,
    IAuthorizationPolicyCatalog authorizationPolicyCatalog,
    IBackendForFrontendRuntimeCatalog backendForFrontendRuntimeCatalog,
    IStranglerFigRuntimeCatalog stranglerFigRuntimeCatalog,
    IStranglerFigMigrationRuntimeCatalog stranglerFigMigrationRuntimeCatalog,
    IStranglerFigIngressRuntimeCatalog stranglerFigIngressRuntimeCatalog,
    ICellBoundaryCatalog cellBoundaryCatalog,
    ICellRouteCatalog cellRouteCatalog,
    ICellHealthIsolationCatalog cellHealthIsolationCatalog,
    ICellTrafficAutomationRuntimeCatalog cellTrafficAutomationRuntimeCatalog,
    ITechnologyRuntimeCatalog technologyRuntimeCatalog,
    IRuntimeDiagnosticsCatalog diagnosticsCatalog) : IRuntimeIntrospectionSnapshotProvider
{
    public RuntimeIntrospectionSnapshot CreateSnapshot()
    {
        var eventDispatchRuntimeDescriptorCatalog = serviceProvider.GetService(typeof(IEventDispatchRuntimeDescriptorCatalog)) as IEventDispatchRuntimeDescriptorCatalog;
        var eventDispatchRuntimeCatalog = serviceProvider.GetService(typeof(IEventDispatchRuntimeCatalog)) as IEventDispatchRuntimeCatalog;
        var eventPublicationRuntimeCatalog = serviceProvider.GetService(typeof(IEventPublicationRuntimeCatalog)) as IEventPublicationRuntimeCatalog;
        var eventSubscriptionExecutionReadinessCatalog = serviceProvider.GetService(typeof(IEventSubscriptionExecutionReadinessCatalog)) as IEventSubscriptionExecutionReadinessCatalog;
        var agentToolRunCatalog = serviceProvider.GetService(typeof(IAgentToolRunCatalog)) as IAgentToolRunCatalog;
        var knowledgeIndexCatalog = serviceProvider.GetService(typeof(IKnowledgeIndexCatalog)) as IKnowledgeIndexCatalog;
        var cdcCaptureRuntimeStateCatalog = serviceProvider.GetService(typeof(ICdcCaptureRuntimeStateCatalog)) as ICdcCaptureRuntimeStateCatalog;
        var cdcCaptureExecutionRuntimeCatalog = serviceProvider.GetService(typeof(ICdcCaptureExecutionRuntimeCatalog)) as ICdcCaptureExecutionRuntimeCatalog;
        var featureFlagRuntimeCatalog = serviceProvider.GetService(typeof(IFeatureFlagRuntimeCatalog)) as IFeatureFlagRuntimeCatalog;
        var sagaChoreographyRuntimeCatalog = serviceProvider.GetService(typeof(ISagaChoreographyRuntimeCatalog)) as ISagaChoreographyRuntimeCatalog;
        var sagaChoreographyPublicationRuntimeStateCatalog = serviceProvider.GetService(typeof(ISagaChoreographyPublicationRuntimeStateCatalog)) as ISagaChoreographyPublicationRuntimeStateCatalog;
        var rateLimitingRuntimeCatalog = serviceProvider.GetService(typeof(IRateLimitingRuntimeCatalog)) as IRateLimitingRuntimeCatalog;
        var restEndpointCandidateRuntimeCatalog = serviceProvider.GetService(typeof(IRestEndpointCandidateRuntimeCatalog)) as IRestEndpointCandidateRuntimeCatalog;
        var restEndpointAuthoringPolicyRuntimeCatalog = serviceProvider.GetService(typeof(IRestEndpointAuthoringPolicyRuntimeCatalog)) as IRestEndpointAuthoringPolicyRuntimeCatalog;
        var restEndpointPublicationGroupRuntimeCatalog = serviceProvider.GetService(typeof(IRestEndpointPublicationGroupRuntimeCatalog)) as IRestEndpointPublicationGroupRuntimeCatalog;
        var restEndpointOverrideRuntimeCatalog = serviceProvider.GetService(typeof(IRestEndpointOverrideRuntimeCatalog)) as IRestEndpointOverrideRuntimeCatalog;
        var restEndpointRuntimeCatalog = serviceProvider.GetService(typeof(IRestEndpointRuntimeCatalog)) as IRestEndpointRuntimeCatalog;
        var restEndpointSuppressionRuntimeCatalog = serviceProvider.GetService(typeof(IRestEndpointSuppressionRuntimeCatalog)) as IRestEndpointSuppressionRuntimeCatalog;
        var backendForFrontendRestRuntimeCatalog = serviceProvider.GetService(typeof(IBackendForFrontendRestRuntimeCatalog)) as IBackendForFrontendRestRuntimeCatalog;
        var backendForFrontendRestDocumentRuntimeCatalog = serviceProvider.GetService(typeof(IBackendForFrontendRestDocumentRuntimeCatalog)) as IBackendForFrontendRestDocumentRuntimeCatalog;
        var behaviorResilienceRuntimeCatalog = serviceProvider.GetService(typeof(IBehaviorResilienceRuntimeCatalog)) as IBehaviorResilienceRuntimeCatalog;
        var durableExecutionRuntimeCatalog = serviceProvider.GetService(typeof(IDurableExecutionRuntimeCatalog)) as IDurableExecutionRuntimeCatalog;
        var durableExecutionRuntimeStateCatalog = serviceProvider.GetService(typeof(IDurableExecutionRuntimeStateCatalog)) as IDurableExecutionRuntimeStateCatalog;

        return new RuntimeIntrospectionSnapshot(
            runtime.Manifest,
            runtime.StatusSnapshot,
            executionRuntimeCatalog.Graphs,
            technologyRuntimeCatalog.Surfaces,
            diagnosticsCatalog.Conventions,
            runtime.OperationalStory)
        {
            CellBoundaries = cellBoundaryCatalog.CellBoundaries,
            CellRoutes = cellRouteCatalog.Routes,
            CellHealthIsolations = cellHealthIsolationCatalog.HealthIsolations,
            CellTrafficAutomations = cellTrafficAutomationRuntimeCatalog.Automations,
            HostedExecutions = hostedExecutionRuntimeCatalog.HostedExecutions,
            DataProducts = dataProductCatalog.DataProducts,
            CdcCaptures = cdcCaptureCatalog.CdcCaptures,
            CdcCaptureStates = cdcCaptureRuntimeStateCatalog?.States ?? [],
            CdcCaptureExecutionRuntimes = cdcCaptureExecutionRuntimeCatalog?.Runtimes ?? [],
            Projections = projectionCatalog.Projections,
            Outboxes = outboxCatalog.Outboxes,
            Inboxes = inboxCatalog.Inboxes,
            DatabaseRoles = databaseRoleCatalog.DatabaseRoles,
            DatabaseMigrations = databaseMigrationCatalog.DatabaseMigrations,
            DatabaseMigrationPlaybook = databaseMigrationOperationalPlaybookProvider.CreatePlaybook(),
            DatabaseTopology = databaseTopologyOperationalSnapshotProvider.CreateSnapshot(),
            AgentToolRuns = agentToolRunCatalog?.Runs ?? [],
            KnowledgeIndexes = knowledgeIndexCatalog?.States ?? [],
            EventPublicationStates = eventPublicationRuntimeCatalog?.States ?? [],
            EventDispatchRuntimes = eventDispatchRuntimeDescriptorCatalog?.Runtimes ?? [],
            EventDispatchStates = eventDispatchRuntimeCatalog?.States ?? [],
            EventSubscriptionExecutionReadiness = eventSubscriptionExecutionReadinessCatalog?.Readiness ?? [],
            AuditStores = auditStoreCatalog.AuditStores,
            AuthorizationPolicies = authorizationPolicyCatalog.Policies,
            FeatureFlags = featureFlagRuntimeCatalog?.FeatureFlags ?? [],
            SagaChoreographies = sagaChoreographyRuntimeCatalog?.SagaChoreographies ?? [],
            SagaChoreographyPublicationStates = sagaChoreographyPublicationRuntimeStateCatalog?.States ?? [],
            RateLimitingPolicies = rateLimitingRuntimeCatalog?.Policies ?? [],
            RestEndpoints = restEndpointRuntimeCatalog?.Endpoints ?? [],
            RestEndpointCandidates = restEndpointCandidateRuntimeCatalog?.Candidates ?? [],
            RestEndpointPublicationGroups = restEndpointPublicationGroupRuntimeCatalog?.Groups ?? [],
            RestEndpointAuthoringPolicies = restEndpointAuthoringPolicyRuntimeCatalog?.Policies ?? [],
            RestEndpointOverrides = restEndpointOverrideRuntimeCatalog?.OverrideRules ?? [],
            RestEndpointSuppressions = restEndpointSuppressionRuntimeCatalog?.Suppressions ?? [],
            BehaviorResiliencePolicies = behaviorResilienceRuntimeCatalog?.Policies ?? [],
            DurableExecutions = durableExecutionRuntimeCatalog?.DurableExecutions ?? [],
            DurableExecutionStates = durableExecutionRuntimeStateCatalog?.States ?? [],
            BackendForFrontendBindings = backendForFrontendRuntimeCatalog.Bindings,
            BackendForFrontendRestEndpoints = backendForFrontendRestRuntimeCatalog?.Endpoints ?? [],
            BackendForFrontendRestDocuments = backendForFrontendRestDocumentRuntimeCatalog?.Documents ?? [],
            StranglerFigRoutes = stranglerFigRuntimeCatalog.Routes,
            StranglerFigRoutePolicies = stranglerFigMigrationRuntimeCatalog.Routes,
            StranglerFigIngressRoutes = stranglerFigIngressRuntimeCatalog.Routes
        };
    }
}
