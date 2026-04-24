namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector provider-owned control-plane dependency-aware provisioning and mutation hardening answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories
{
    /// <summary>
    /// The runtime participates in the provider-owned control-plane dependency-aware provisioning and mutation hardening lane.
    /// </summary>
    public const string ProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening = "provider-owned-control-plane-dependency-aware-provisioning-and-mutation-hardening";

    /// <summary>
    /// Provider-owned control-plane dependency-aware provisioning and mutation hardening still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.OperatorOnly;

    /// <summary>
    /// Provider-owned control-plane dependency-aware provisioning and mutation hardening is currently ready.
    /// </summary>
    public const string DependencyReady = "dependency-ready";

    /// <summary>
    /// Provider-owned control-plane dependency-aware provisioning currently remains blocked.
    /// </summary>
    public const string ProvisioningBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProvisioningBlocked;

    /// <summary>
    /// Provider-owned control-plane dependency-aware mutation currently remains blocked.
    /// </summary>
    public const string MutationBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.MutationBlocked;

    /// <summary>
    /// Provider-owned control-plane dependency-aware provisioning and mutation hardening is currently degraded.
    /// </summary>
    public const string DependencyDegraded = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.DependencyDegraded;

    /// <summary>
    /// Provider-owned control-plane dependency-aware provisioning hardening is fully hardened.
    /// </summary>
    public const string ProvisioningHardened = "provisioning-hardened";

    /// <summary>
    /// Provider-owned control-plane dependency-aware mutation hardening is fully hardened.
    /// </summary>
    public const string MutationHardened = "mutation-hardened";

    /// <summary>
    /// Provider-owned control-plane dependency-aware provisioning and mutation hardening currently remains risky.
    /// </summary>
    public const string DependencyRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.DependencyRisk;

    /// <summary>
    /// Broader provider-owned control-plane provisioning is currently ready.
    /// </summary>
    public const string ProvisioningReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProvisioningReady;

    /// <summary>
    /// Broader provider-owned control-plane provisioning is currently executing.
    /// </summary>
    public const string ProvisioningExecuting = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProvisioningExecuting;

    /// <summary>
    /// Broader provider-owned control-plane provisioning is currently partial on the shared lane.
    /// </summary>
    public const string ProvisioningPartial = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProvisioningPartial;

    /// <summary>
    /// Broader provider-owned control-plane provisioning currently remains risky.
    /// </summary>
    public const string ProvisioningRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningCategories.ProvisioningRisk;

    /// <summary>
    /// Broader provider-owned control-plane mutation is currently ready.
    /// </summary>
    public const string MutationReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.MutationReady;

    /// <summary>
    /// Broader provider-owned control-plane reconcile is currently ready.
    /// </summary>
    public const string ReconcileReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ReconcileReady;

    /// <summary>
    /// Broader provider-owned control-plane mutation or reconcile is currently executing.
    /// </summary>
    public const string MutationExecuting = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.MutationExecuting;

    /// <summary>
    /// Broader provider-owned control-plane mutation currently remains risky.
    /// </summary>
    public const string MutationRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.MutationRisk;

    /// <summary>
    /// The broader dependency-aware apply-and-reconcile hardening lane is fully hardened.
    /// </summary>
    public const string ApplyAndReconcileHardened = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ApplyAndReconcileHardened;

    /// <summary>
    /// The broader provider-owned apply-and-reconcile lane is currently ready.
    /// </summary>
    public const string ApplyAndReconcileReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ApplyAndReconcileReady;

    /// <summary>
    /// The broader provider-owned apply-and-reconcile lane remains blocked.
    /// </summary>
    public const string ApplyAndReconcileBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ApplyAndReconcileBlocked;

    /// <summary>
    /// The broader provider-owned apply-and-reconcile lane is currently executing.
    /// </summary>
    public const string ApplyAndReconcileExecuting = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ApplyAndReconcileExecuting;

    /// <summary>
    /// The broader provider-owned apply-and-reconcile lane no longer needs another execution step.
    /// </summary>
    public const string ApplyAndReconcileCompleted = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ApplyAndReconcileCompleted;

    /// <summary>
    /// The broader provider-owned apply-and-reconcile lane currently remains risky.
    /// </summary>
    public const string ApplyAndReconcileRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ApplyAndReconcileRisk;

    /// <summary>
    /// The current target operation is one provider-owned mutation.
    /// </summary>
    public const string MutationOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.MutationOperation;

    /// <summary>
    /// The current target operation is one provider-owned reconcile.
    /// </summary>
    public const string ReconcileOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.ReconcileOperation;

    /// <summary>
    /// No provider-owned mutation or reconcile operation is currently targeted.
    /// </summary>
    public const string NoTargetOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileCategories.NoTargetOperation;

    /// <summary>
    /// The current node can execute dependency-aware provisioning and mutation work safely.
    /// </summary>
    public const string CurrentNodeExecutable = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.CurrentNodeExecutable;

    /// <summary>
    /// The current node cannot yet execute dependency-aware provisioning and mutation work safely.
    /// </summary>
    public const string CurrentNodeBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.CurrentNodeBlocked;

    /// <summary>
    /// The current node can execute dependency-aware provisioning work safely.
    /// </summary>
    public const string CurrentNodeProvisioningExecutable = "current-node-provisioning-executable";

    /// <summary>
    /// The current node cannot yet execute dependency-aware provisioning work safely.
    /// </summary>
    public const string CurrentNodeProvisioningBlocked = "current-node-provisioning-blocked";

    /// <summary>
    /// The current node can execute dependency-aware mutation work safely.
    /// </summary>
    public const string CurrentNodeMutationExecutable = "current-node-mutation-executable";

    /// <summary>
    /// The current node cannot yet execute dependency-aware mutation work safely.
    /// </summary>
    public const string CurrentNodeMutationBlocked = "current-node-mutation-blocked";

    /// <summary>
    /// The current provider lane would still apply one or more shared write-path changes.
    /// </summary>
    public const string WouldApplyChanges = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.WouldApplyChanges;

    /// <summary>
    /// The current provider lane would not apply another shared write-path change.
    /// </summary>
    public const string NoChangesRequired = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.NoChangesRequired;

    /// <summary>
    /// The current provider lane still requires explicit approval.
    /// </summary>
    public const string ApprovalRequired = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ApprovalRequired;

    /// <summary>
    /// The current provider lane still targets a destructive operation.
    /// </summary>
    public const string DestructiveOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.DestructiveOperation;

    /// <summary>
    /// The runtime currently declares complete dependency identity metadata.
    /// </summary>
    public const string DependencyIdentityReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.DependencyIdentityReady;

    /// <summary>
    /// The runtime currently reports complete dependency identity metadata.
    /// </summary>
    public const string ReportedDependencyIdentityReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ReportedDependencyIdentityReady;

    /// <summary>
    /// The runtime does not currently declare the upstream connector-cluster identifier.
    /// </summary>
    public const string MissingConnectClusterId = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.MissingConnectClusterId;

    /// <summary>
    /// The runtime does not currently declare the upstream connector class.
    /// </summary>
    public const string MissingConnectorClass = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.MissingConnectorClass;

    /// <summary>
    /// The runtime does not currently declare the upstream source-provider identifier.
    /// </summary>
    public const string MissingSourceProviderId = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.MissingSourceProviderId;

    /// <summary>
    /// The managed connector does not currently declare task ids or an expected task count.
    /// </summary>
    public const string MissingTaskBaseline = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.MissingTaskBaseline;

    /// <summary>
    /// The managed connector has not yet reported task ids or a reported task count.
    /// </summary>
    public const string ReportedTaskTopologyUnavailable = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ReportedTaskTopologyUnavailable;

    /// <summary>
    /// The managed connector reports task counts, but not task identities.
    /// </summary>
    public const string ReportedTaskIdentityUnavailable = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ReportedTaskIdentityUnavailable;

    /// <summary>
    /// The managed connector reports a different task count than the declared baseline.
    /// </summary>
    public const string TaskCountMismatch = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.TaskCountMismatch;

    /// <summary>
    /// One or more declared task ids are missing from the latest reported task ids.
    /// </summary>
    public const string MissingDeclaredTaskReports = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.MissingDeclaredTaskReports;

    /// <summary>
    /// One or more reported task ids were not part of the declared task baseline.
    /// </summary>
    public const string UnexpectedReportedTasks = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.UnexpectedReportedTasks;

    /// <summary>
    /// The latest reported connector-cluster identifier differs from the declared baseline.
    /// </summary>
    public const string ConnectClusterMismatch = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ConnectClusterMismatch;

    /// <summary>
    /// The latest reported connector class differs from the declared baseline.
    /// </summary>
    public const string ConnectorClassMismatch = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ConnectorClassMismatch;

    /// <summary>
    /// The latest reported source-provider identifier differs from the declared baseline.
    /// </summary>
    public const string SourceProviderMismatch = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.SourceProviderMismatch;

    /// <summary>
    /// The runtime currently reports one or more active connector tasks.
    /// </summary>
    public const string ActiveTaskTopology = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ActiveTaskTopology;

    /// <summary>
    /// The runtime does not currently report any active connector task ids.
    /// </summary>
    public const string NoActiveTaskTopology = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.NoActiveTaskTopology;

    /// <summary>
    /// The runtime currently reports a degraded reconciliation posture.
    /// </summary>
    public const string ReconciliationDegraded = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ReconciliationDegraded;

    /// <summary>
    /// The runtime currently reports a stable reconciliation posture.
    /// </summary>
    public const string ReconciliationStable = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ReconciliationStable;

    /// <summary>
    /// The runtime currently exposes an active reporter lease.
    /// </summary>
    public const string ReporterLeaseActive = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ReporterLeaseActive;

    /// <summary>
    /// The runtime is missing one active reporter lease or currently reports degraded reporter ownership.
    /// </summary>
    public const string ReporterLeaseMissingOrStale = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ReporterLeaseMissingOrStale;

    /// <summary>
    /// A durable command-journal store is configured for the runtime.
    /// </summary>
    public const string DurableJournalConfigured = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.DurableJournalConfigured;

    /// <summary>
    /// The durable command journal currently exposes persisted recorded history.
    /// </summary>
    public const string PersistedHistory = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.PersistedHistory;

    /// <summary>
    /// The durable command journal currently exposes recovered recorded history.
    /// </summary>
    public const string RecoveredHistory = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.RecoveredHistory;

    /// <summary>
    /// Dependency-aware hardening currently depends on in-memory history only.
    /// </summary>
    public const string InMemoryJournalOnly = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.InMemoryJournalOnly;

    /// <summary>
    /// The latest provider command remained blocked.
    /// </summary>
    public const string ProviderCommandBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ProviderCommandBlocked;

    /// <summary>
    /// The latest provider command translated into a provider-facing shape.
    /// </summary>
    public const string ProviderCommandAdapted = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ProviderCommandAdapted;

    /// <summary>
    /// The latest provider command determined that no provider command is required.
    /// </summary>
    public const string ProviderCommandNoOp = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ProviderCommandNoOp;

    /// <summary>
    /// The latest provider command failed while Cephalon translated it.
    /// </summary>
    public const string ProviderCommandFailed = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ProviderCommandFailed;
}
