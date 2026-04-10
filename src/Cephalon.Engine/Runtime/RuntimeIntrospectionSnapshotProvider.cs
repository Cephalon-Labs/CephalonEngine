using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Authorization;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Diagnostics;

namespace Cephalon.Engine.Runtime;

internal sealed class RuntimeIntrospectionSnapshotProvider(
    IRuntime runtime,
    IExecutionRuntimeCatalog executionRuntimeCatalog,
    IHostedExecutionRuntimeCatalog hostedExecutionRuntimeCatalog,
    IProjectionCatalog projectionCatalog,
    IOutboxCatalog outboxCatalog,
    IInboxCatalog inboxCatalog,
    IDatabaseRoleCatalog databaseRoleCatalog,
    IAuditStoreCatalog auditStoreCatalog,
    IAuthorizationPolicyCatalog authorizationPolicyCatalog,
    ITechnologyRuntimeCatalog technologyRuntimeCatalog,
    IRuntimeDiagnosticsCatalog diagnosticsCatalog) : IRuntimeIntrospectionSnapshotProvider
{
    public RuntimeIntrospectionSnapshot CreateSnapshot()
    {
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
            AuditStores = auditStoreCatalog.AuditStores,
            AuthorizationPolicies = authorizationPolicyCatalog.Policies
        };
    }
}
