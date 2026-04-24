namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector provider-specific control-plane dependency-aware teardown and mutation-execution hardening answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCategories
{
    /// <summary>
    /// The runtime participates in the provider-specific dependency-aware teardown and mutation-execution hardening lane.
    /// </summary>
    public const string ProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening =
        "provider-specific-control-plane-dependency-aware-teardown-and-mutation-execution-hardening";

    /// <summary>
    /// Provider-specific dependency-aware teardown and mutation-execution hardening still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.OperatorOnly;

    /// <summary>
    /// Provider-specific dependency-aware teardown and mutation-execution hardening is currently ready.
    /// </summary>
    public const string DependencyReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.DependencyReady;

    /// <summary>
    /// Provider-specific dependency-aware teardown currently remains blocked.
    /// </summary>
    public const string TeardownBlocked = "teardown-blocked";

    /// <summary>
    /// Provider-specific dependency-aware mutation execution currently remains blocked.
    /// </summary>
    public const string MutationExecutionBlocked = "mutation-execution-blocked";

    /// <summary>
    /// Provider-specific dependency-aware teardown and mutation execution currently remains degraded.
    /// </summary>
    public const string DependencyDegraded = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.DependencyDegraded;

    /// <summary>
    /// Provider-specific dependency-aware teardown is fully hardened.
    /// </summary>
    public const string TeardownHardened = "teardown-hardened";

    /// <summary>
    /// Provider-specific dependency-aware mutation execution is fully hardened.
    /// </summary>
    public const string MutationExecutionHardened = "mutation-execution-hardened";

    /// <summary>
    /// Provider-specific dependency-aware teardown and mutation execution currently remains risky.
    /// </summary>
    public const string DependencyRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.DependencyRisk;

    /// <summary>
    /// One provider-specific control-plane materializer is currently unavailable.
    /// </summary>
    public const string MaterializerUnavailable = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.MaterializerUnavailable;

    /// <summary>
    /// One provider-specific control-plane materializer is currently ready.
    /// </summary>
    public const string MaterializerReady = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.MaterializerReady;

    /// <summary>
    /// One provider-specific control-plane materializer is currently selected.
    /// </summary>
    public const string MaterializerSelected = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.MaterializerSelected;

    /// <summary>
    /// The selected provider-specific control-plane materializer is currently executing.
    /// </summary>
    public const string MaterializerExecuting = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.MaterializerExecuting;

    /// <summary>
    /// The selected provider-specific control-plane materializer currently remains risky.
    /// </summary>
    public const string MaterializerRisk = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.MaterializerRisk;

    /// <summary>
    /// The current target operation is one teardown operation.
    /// </summary>
    public const string TeardownOperation = "teardown-operation";

    /// <summary>
    /// The current target operation is one mutation-execution operation.
    /// </summary>
    public const string MutationExecutionOperation = "mutation-execution-operation";

    /// <summary>
    /// The current target operation is one provider-owned mutation.
    /// </summary>
    public const string MutationOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.MutationOperation;

    /// <summary>
    /// The current target operation is one provider-owned reconcile.
    /// </summary>
    public const string ReconcileOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.ReconcileOperation;

    /// <summary>
    /// No provider-owned operation is currently targeted.
    /// </summary>
    public const string NoTargetOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.NoTargetOperation;

    /// <summary>
    /// The current node can execute the broader provider-specific teardown or mutation-execution lane.
    /// </summary>
    public const string CurrentNodeExecutable = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.CurrentNodeExecutable;

    /// <summary>
    /// The current node cannot yet execute the broader provider-specific teardown or mutation-execution lane.
    /// </summary>
    public const string CurrentNodeBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.CurrentNodeBlocked;

    /// <summary>
    /// The current node can execute dependency-aware teardown.
    /// </summary>
    public const string CurrentNodeTeardownExecutable = "current-node-teardown-executable";

    /// <summary>
    /// The current node cannot yet execute dependency-aware teardown.
    /// </summary>
    public const string CurrentNodeTeardownBlocked = "current-node-teardown-blocked";

    /// <summary>
    /// The current node can execute dependency-aware mutation execution.
    /// </summary>
    public const string CurrentNodeMutationExecutionExecutable = "current-node-mutation-execution-executable";

    /// <summary>
    /// The current node cannot yet execute dependency-aware mutation execution.
    /// </summary>
    public const string CurrentNodeMutationExecutionBlocked = "current-node-mutation-execution-blocked";

    /// <summary>
    /// The runtime currently exposes one provider identifier.
    /// </summary>
    public const string ProviderIdentityReady = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.ProviderIdentityReady;

    /// <summary>
    /// The runtime does not currently expose one provider identifier.
    /// </summary>
    public const string MissingProviderId = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.MissingProviderId;

    /// <summary>
    /// The runtime currently exposes one materializer identifier.
    /// </summary>
    public const string MaterializerIdentityReady = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.MaterializerIdentityReady;

    /// <summary>
    /// The runtime does not currently expose one materializer identifier.
    /// </summary>
    public const string MissingMaterializerId = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.MissingMaterializerId;

    /// <summary>
    /// The runtime currently exposes one transport kind.
    /// </summary>
    public const string TransportIdentityReady = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.TransportIdentityReady;

    /// <summary>
    /// The runtime does not currently expose one transport kind.
    /// </summary>
    public const string MissingTransportKind = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.MissingTransportKind;

    /// <summary>
    /// The runtime currently exposes one provider-facing surface identifier.
    /// </summary>
    public const string ProviderSurfaceReady = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.ProviderSurfaceReady;

    /// <summary>
    /// The runtime does not currently expose one provider-facing surface identifier.
    /// </summary>
    public const string MissingProviderSurface = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.MissingProviderSurface;

    /// <summary>
    /// The runtime currently exposes one connector identity.
    /// </summary>
    public const string ConnectorIdentityReady = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.ConnectorIdentityReady;

    /// <summary>
    /// The runtime does not currently expose one connector identity.
    /// </summary>
    public const string MissingConnectorId = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.MissingConnectorId;

    /// <summary>
    /// The runtime currently exposes one provider worker identity.
    /// </summary>
    public const string WorkerIdentityVisible = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.WorkerIdentityVisible;

    /// <summary>
    /// The runtime does not currently expose one provider worker identity.
    /// </summary>
    public const string WorkerIdentityUnavailable = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.WorkerIdentityUnavailable;

    /// <summary>
    /// The runtime currently declares complete dependency identity metadata.
    /// </summary>
    public const string DeclaredDependencyIdentityReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.DependencyIdentityReady;

    /// <summary>
    /// The runtime currently reports complete dependency identity metadata.
    /// </summary>
    public const string ReportedDependencyIdentityReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategories.ReportedDependencyIdentityReady;

    /// <summary>
    /// The runtime currently exposes one declared task baseline.
    /// </summary>
    public const string TaskBaselineReady = "task-baseline-ready";

    /// <summary>
    /// The runtime currently exposes one reported task topology.
    /// </summary>
    public const string ReportedTaskTopologyReady = "reported-task-topology-ready";

    /// <summary>
    /// The runtime currently exposes one active task topology.
    /// </summary>
    public const string ActiveTaskTopologyReady = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.ActiveTaskTopology;

    /// <summary>
    /// The runtime currently reports one dependency identity mismatch.
    /// </summary>
    public const string DependencyIdentityMismatch = "dependency-identity-mismatch";

    /// <summary>
    /// The runtime currently reports one task-topology mismatch.
    /// </summary>
    public const string TaskTopologyMismatch = "task-topology-mismatch";

    /// <summary>
    /// The runtime currently has one durable store configured.
    /// </summary>
    public const string DurableStoreConfigured = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.DurableJournalConfigured;

    /// <summary>
    /// The runtime currently exposes persisted history.
    /// </summary>
    public const string PersistedHistoryVisible = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.PersistedHistory;

    /// <summary>
    /// The runtime recovered persisted history in the current process.
    /// </summary>
    public const string RecoveredPersistedHistory = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategories.RecoveredHistory;

    /// <summary>
    /// The runtime currently exposes an active reporter lease.
    /// </summary>
    public const string ReporterLeaseActive = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.ReporterLeaseActive;

    /// <summary>
    /// The runtime is missing one active reporter lease or currently reports degraded reporter ownership.
    /// </summary>
    public const string ReporterLeaseMissingOrStale = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.ReporterLeaseMissingOrStale;

    /// <summary>
    /// The current provider lane would still apply one or more shared write-path changes.
    /// </summary>
    public const string WouldApplyChanges = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.WouldApplyChanges;

    /// <summary>
    /// The current provider lane would not apply another shared write-path change.
    /// </summary>
    public const string NoChangesRequired = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.NoChangesRequired;

    /// <summary>
    /// The current provider lane still requires explicit approval.
    /// </summary>
    public const string ApprovalRequired = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.ApprovalRequired;

    /// <summary>
    /// The current provider lane still targets a destructive operation.
    /// </summary>
    public const string DestructiveOperation = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.DestructiveOperation;

    /// <summary>
    /// The latest provider command translated into a provider-facing shape.
    /// </summary>
    public const string ProviderCommandAdapted = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.ProviderCommandAdapted;

    /// <summary>
    /// The latest provider command remained blocked.
    /// </summary>
    public const string ProviderCommandBlocked = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.ProviderCommandBlocked;

    /// <summary>
    /// The latest provider command determined that no provider command is required.
    /// </summary>
    public const string ProviderCommandNoOp = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.ProviderCommandNoOp;

    /// <summary>
    /// The latest provider command failed while Cephalon translated it.
    /// </summary>
    public const string ProviderCommandFailed = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerCategories.ProviderCommandFailed;
}
