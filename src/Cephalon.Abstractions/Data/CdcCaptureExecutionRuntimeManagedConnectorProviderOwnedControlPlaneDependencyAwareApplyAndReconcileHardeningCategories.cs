namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector provider-owned control-plane dependency-aware apply-and-reconcile hardening answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories
{
    /// <summary>
    /// The runtime participates in the provider-owned control-plane dependency-aware apply-and-reconcile hardening lane.
    /// </summary>
    public const string ProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardening = "provider-owned-control-plane-dependency-aware-apply-and-reconcile-hardening";

    /// <summary>
    /// Provider-owned control-plane dependency-aware apply-and-reconcile hardening still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.OperatorOnly;

    /// <summary>
    /// Provider-owned control-plane dependency-aware apply-and-reconcile hardening is currently ready.
    /// </summary>
    public const string DependencyReady = "dependency-ready";

    /// <summary>
    /// Provider-owned control-plane dependency-aware apply-and-reconcile hardening remains blocked.
    /// </summary>
    public const string DependencyBlocked = "dependency-blocked";

    /// <summary>
    /// Provider-owned control-plane dependency-aware apply-and-reconcile hardening is currently degraded.
    /// </summary>
    public const string DependencyDegraded = "dependency-degraded";

    /// <summary>
    /// Provider-owned control-plane dependency-aware apply-and-reconcile hardening is fully hardened.
    /// </summary>
    public const string ApplyAndReconcileHardened = "apply-and-reconcile-hardened";

    /// <summary>
    /// Provider-owned control-plane dependency-aware apply-and-reconcile hardening currently remains risky.
    /// </summary>
    public const string DependencyRisk = "dependency-risk";

    /// <summary>
    /// The current provider-owned apply-and-reconcile lane is currently ready.
    /// </summary>
    public const string ApplyAndReconcileReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.ApplyAndReconcileReady;

    /// <summary>
    /// The current provider-owned apply-and-reconcile lane remains blocked.
    /// </summary>
    public const string ApplyAndReconcileBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.ApplyAndReconcileBlocked;

    /// <summary>
    /// The current provider-owned apply-and-reconcile lane is currently executing.
    /// </summary>
    public const string ApplyAndReconcileExecuting = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.ApplyAndReconcileExecuting;

    /// <summary>
    /// The current provider-owned apply-and-reconcile lane no longer needs another execution step.
    /// </summary>
    public const string ApplyAndReconcileCompleted = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.ApplyAndReconcileCompleted;

    /// <summary>
    /// The current provider-owned apply-and-reconcile lane currently remains risky.
    /// </summary>
    public const string ApplyAndReconcileRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.ApplyAndReconcileRisk;

    /// <summary>
    /// The current node can execute dependency-aware apply-and-reconcile work safely.
    /// </summary>
    public const string CurrentNodeExecutable = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.CurrentNodeExecutable;

    /// <summary>
    /// The current node cannot yet execute dependency-aware apply-and-reconcile work safely.
    /// </summary>
    public const string CurrentNodeBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.CurrentNodeBlocked;

    /// <summary>
    /// The current provider lane would still apply shared write-path changes.
    /// </summary>
    public const string WouldApplyChanges = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.WouldApplyChanges;

    /// <summary>
    /// The current provider lane would not apply another shared write-path change.
    /// </summary>
    public const string NoChangesRequired = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.NoChangesRequired;

    /// <summary>
    /// The current provider lane still requires explicit approval.
    /// </summary>
    public const string ApprovalRequired = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.ApprovalRequired;

    /// <summary>
    /// The current provider lane still targets a destructive operation.
    /// </summary>
    public const string DestructiveOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.DestructiveOperation;

    /// <summary>
    /// The runtime currently declares complete dependency identity metadata.
    /// </summary>
    public const string DependencyIdentityReady = "dependency-identity-ready";

    /// <summary>
    /// The runtime currently reports complete dependency identity metadata.
    /// </summary>
    public const string ReportedDependencyIdentityReady = "reported-dependency-identity-ready";

    /// <summary>
    /// The runtime does not currently declare the upstream connector-cluster identifier.
    /// </summary>
    public const string MissingConnectClusterId = CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories.MissingConnectClusterId;

    /// <summary>
    /// The runtime does not currently declare the upstream connector class.
    /// </summary>
    public const string MissingConnectorClass = CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories.MissingConnectorClass;

    /// <summary>
    /// The runtime does not currently declare the upstream source-provider identifier.
    /// </summary>
    public const string MissingSourceProviderId = CdcCaptureExecutionRuntimeManagedConnectorGovernanceCategories.MissingSourceProviderId;

    /// <summary>
    /// The managed connector does not currently declare task ids or an expected task count.
    /// </summary>
    public const string MissingTaskBaseline = CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.MissingTaskBaseline;

    /// <summary>
    /// The managed connector has not yet reported task ids or a reported task count.
    /// </summary>
    public const string ReportedTaskTopologyUnavailable = CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ReportedTaskTopologyUnavailable;

    /// <summary>
    /// The managed connector reports task counts, but not task identities.
    /// </summary>
    public const string ReportedTaskIdentityUnavailable = CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ReportedTaskIdentityUnavailable;

    /// <summary>
    /// The managed connector reports a different task count than the declared baseline.
    /// </summary>
    public const string TaskCountMismatch = CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.TaskCountMismatch;

    /// <summary>
    /// One or more declared task ids are missing from the latest reported task ids.
    /// </summary>
    public const string MissingDeclaredTaskReports = CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.MissingDeclaredTaskReports;

    /// <summary>
    /// One or more reported task ids were not part of the declared task baseline.
    /// </summary>
    public const string UnexpectedReportedTasks = CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.UnexpectedReportedTasks;

    /// <summary>
    /// The latest reported connector-cluster identifier differs from the declared baseline.
    /// </summary>
    public const string ConnectClusterMismatch = CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ConnectClusterMismatch;

    /// <summary>
    /// The latest reported connector class differs from the declared baseline.
    /// </summary>
    public const string ConnectorClassMismatch = CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.ConnectorClassMismatch;

    /// <summary>
    /// The latest reported source-provider identifier differs from the declared baseline.
    /// </summary>
    public const string SourceProviderMismatch = CdcCaptureExecutionRuntimeManagedConnectorDriftCategories.SourceProviderMismatch;

    /// <summary>
    /// The runtime currently reports one or more active connector tasks.
    /// </summary>
    public const string ActiveTaskTopology = "active-task-topology";

    /// <summary>
    /// The runtime does not currently report any active connector task ids.
    /// </summary>
    public const string NoActiveTaskTopology = "no-active-task-topology";

    /// <summary>
    /// The runtime currently reports a degraded reconciliation posture.
    /// </summary>
    public const string ReconciliationDegraded = "reconciliation-degraded";

    /// <summary>
    /// The runtime currently reports a stable reconciliation posture.
    /// </summary>
    public const string ReconciliationStable = "reconciliation-stable";

    /// <summary>
    /// The runtime currently exposes an active reporter lease.
    /// </summary>
    public const string ReporterLeaseActive = "reporter-lease-active";

    /// <summary>
    /// The runtime is missing one active reporter lease or currently reports degraded reporter ownership.
    /// </summary>
    public const string ReporterLeaseMissingOrStale = "reporter-lease-missing-or-stale";

    /// <summary>
    /// A durable command-journal store is configured for the runtime.
    /// </summary>
    public const string DurableJournalConfigured = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.DurableJournalConfigured;

    /// <summary>
    /// The durable command journal currently exposes persisted recorded history.
    /// </summary>
    public const string PersistedHistory = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.PersistedHistory;

    /// <summary>
    /// The durable command journal currently exposes recovered recorded history.
    /// </summary>
    public const string RecoveredHistory = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.RecoveredHistory;

    /// <summary>
    /// Dependency-aware hardening currently depends on in-memory history only.
    /// </summary>
    public const string InMemoryJournalOnly = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.InMemoryJournalOnly;

    /// <summary>
    /// The latest provider command remained blocked.
    /// </summary>
    public const string ProviderCommandBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.ProviderCommandBlocked;

    /// <summary>
    /// The latest provider command translated into a provider-facing shape.
    /// </summary>
    public const string ProviderCommandAdapted = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.ProviderCommandAdapted;

    /// <summary>
    /// The latest provider command determined that no provider command is required.
    /// </summary>
    public const string ProviderCommandNoOp = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.ProviderCommandNoOp;

    /// <summary>
    /// The latest provider command failed while Cephalon translated it.
    /// </summary>
    public const string ProviderCommandFailed = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategories.ProviderCommandFailed;
}
