using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Authorization;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Resilience;
using Cephalon.Abstractions.Technologies;
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
    IDatabaseTopologyOperationalSnapshotProvider databaseTopologyOperationalSnapshotProvider,
    IAuditStoreCatalog auditStoreCatalog,
    IAuthorizationPolicyCatalog authorizationPolicyCatalog,
    ITechnologyRuntimeCatalog technologyRuntimeCatalog,
    IRuntimeDiagnosticsCatalog diagnosticsCatalog) : IRuntimeIntrospectionSnapshotProvider
{
    public RuntimeIntrospectionSnapshot CreateSnapshot()
    {
        var eventDispatchRuntimeDescriptorCatalog = serviceProvider.GetService(typeof(IEventDispatchRuntimeDescriptorCatalog)) as IEventDispatchRuntimeDescriptorCatalog;
        var eventDispatchRuntimeCatalog = serviceProvider.GetService(typeof(IEventDispatchRuntimeCatalog)) as IEventDispatchRuntimeCatalog;
        var rateLimitingRuntimeCatalog = serviceProvider.GetService(typeof(IRateLimitingRuntimeCatalog)) as IRateLimitingRuntimeCatalog;
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
            DatabaseTopology = databaseTopologyOperationalSnapshotProvider.CreateSnapshot(),
            EventDispatchRuntimes = eventDispatchRuntimeDescriptorCatalog?.Runtimes ?? [],
            EventDispatchStates = eventDispatchRuntimeCatalog?.States ?? [],
            AuditStores = auditStoreCatalog.AuditStores,
            AuthorizationPolicies = authorizationPolicyCatalog.Policies,
            RateLimitingPolicies = rateLimitingRuntimeCatalog?.Policies ?? [],
            BehaviorResiliencePolicies = behaviorResilienceRuntimeCatalog?.Policies ?? []
        };
    }
}
