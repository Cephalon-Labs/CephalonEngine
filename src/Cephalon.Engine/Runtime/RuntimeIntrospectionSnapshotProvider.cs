using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Authorization;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Patterns;
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
    IProjectionCatalog projectionCatalog,
    IOutboxCatalog outboxCatalog,
    IInboxCatalog inboxCatalog,
    IDatabaseRoleCatalog databaseRoleCatalog,
    IDatabaseMigrationCatalog databaseMigrationCatalog,
    IDatabaseMigrationOperationalPlaybookProvider databaseMigrationOperationalPlaybookProvider,
    IDatabaseTopologyOperationalSnapshotProvider databaseTopologyOperationalSnapshotProvider,
    IAuditStoreCatalog auditStoreCatalog,
    IAuthorizationPolicyCatalog authorizationPolicyCatalog,
    IStranglerFigRuntimeCatalog stranglerFigRuntimeCatalog,
    ITechnologyRuntimeCatalog technologyRuntimeCatalog,
    IRuntimeDiagnosticsCatalog diagnosticsCatalog) : IRuntimeIntrospectionSnapshotProvider
{
    public RuntimeIntrospectionSnapshot CreateSnapshot()
    {
        var eventDispatchRuntimeDescriptorCatalog = serviceProvider.GetService(typeof(IEventDispatchRuntimeDescriptorCatalog)) as IEventDispatchRuntimeDescriptorCatalog;
        var eventDispatchRuntimeCatalog = serviceProvider.GetService(typeof(IEventDispatchRuntimeCatalog)) as IEventDispatchRuntimeCatalog;
        var rateLimitingRuntimeCatalog = serviceProvider.GetService(typeof(IRateLimitingRuntimeCatalog)) as IRateLimitingRuntimeCatalog;
        var restEndpointCandidateRuntimeCatalog = serviceProvider.GetService(typeof(IRestEndpointCandidateRuntimeCatalog)) as IRestEndpointCandidateRuntimeCatalog;
        var restEndpointAuthoringPolicyRuntimeCatalog = serviceProvider.GetService(typeof(IRestEndpointAuthoringPolicyRuntimeCatalog)) as IRestEndpointAuthoringPolicyRuntimeCatalog;
        var restEndpointPublicationGroupRuntimeCatalog = serviceProvider.GetService(typeof(IRestEndpointPublicationGroupRuntimeCatalog)) as IRestEndpointPublicationGroupRuntimeCatalog;
        var restEndpointOverrideRuntimeCatalog = serviceProvider.GetService(typeof(IRestEndpointOverrideRuntimeCatalog)) as IRestEndpointOverrideRuntimeCatalog;
        var restEndpointRuntimeCatalog = serviceProvider.GetService(typeof(IRestEndpointRuntimeCatalog)) as IRestEndpointRuntimeCatalog;
        var restEndpointSuppressionRuntimeCatalog = serviceProvider.GetService(typeof(IRestEndpointSuppressionRuntimeCatalog)) as IRestEndpointSuppressionRuntimeCatalog;
        var behaviorResilienceRuntimeCatalog = serviceProvider.GetService(typeof(IBehaviorResilienceRuntimeCatalog)) as IBehaviorResilienceRuntimeCatalog;

        return new RuntimeIntrospectionSnapshot(
            runtime.Manifest,
            runtime.StatusSnapshot,
            executionRuntimeCatalog.Graphs,
            technologyRuntimeCatalog.Surfaces,
            diagnosticsCatalog.Conventions,
            runtime.OperationalStory)
        {
            HostedExecutions = hostedExecutionRuntimeCatalog.HostedExecutions,
            Projections = projectionCatalog.Projections,
            Outboxes = outboxCatalog.Outboxes,
            Inboxes = inboxCatalog.Inboxes,
            DatabaseRoles = databaseRoleCatalog.DatabaseRoles,
            DatabaseMigrations = databaseMigrationCatalog.DatabaseMigrations,
            DatabaseMigrationPlaybook = databaseMigrationOperationalPlaybookProvider.CreatePlaybook(),
            DatabaseTopology = databaseTopologyOperationalSnapshotProvider.CreateSnapshot(),
            EventDispatchRuntimes = eventDispatchRuntimeDescriptorCatalog?.Runtimes ?? [],
            EventDispatchStates = eventDispatchRuntimeCatalog?.States ?? [],
            AuditStores = auditStoreCatalog.AuditStores,
            AuthorizationPolicies = authorizationPolicyCatalog.Policies,
            RateLimitingPolicies = rateLimitingRuntimeCatalog?.Policies ?? [],
            RestEndpoints = restEndpointRuntimeCatalog?.Endpoints ?? [],
            RestEndpointCandidates = restEndpointCandidateRuntimeCatalog?.Candidates ?? [],
            RestEndpointPublicationGroups = restEndpointPublicationGroupRuntimeCatalog?.Groups ?? [],
            RestEndpointAuthoringPolicies = restEndpointAuthoringPolicyRuntimeCatalog?.Policies ?? [],
            RestEndpointOverrides = restEndpointOverrideRuntimeCatalog?.OverrideRules ?? [],
            RestEndpointSuppressions = restEndpointSuppressionRuntimeCatalog?.Suppressions ?? [],
            BehaviorResiliencePolicies = behaviorResilienceRuntimeCatalog?.Policies ?? [],
            StranglerFigRoutes = stranglerFigRuntimeCatalog.Routes
        };
    }
}
