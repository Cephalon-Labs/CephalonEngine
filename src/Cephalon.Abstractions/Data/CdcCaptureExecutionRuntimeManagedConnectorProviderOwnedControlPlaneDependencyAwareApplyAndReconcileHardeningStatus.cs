namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector provider-owned control-plane dependency-aware apply-and-reconcile hardening posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStatus
{
    /// <summary>
    /// Creates a new managed-connector provider-owned control-plane dependency-aware apply-and-reconcile hardening answer.
    /// </summary>
    /// <param name="state">
    /// The stable provider-owned control-plane dependency-aware apply-and-reconcile hardening state, such as <c>not-applicable</c>, <c>operator-only</c>, <c>dependency-ready</c>, <c>dependency-blocked</c>, <c>dependency-degraded</c>, <c>apply-and-reconcile-hardened</c>, or <c>dependency-risk</c>.
    /// </param>
    /// <param name="description">An optional operator-facing dependency-aware apply-and-reconcile hardening summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector provider-owned control-plane dependency-aware apply-and-reconcile hardening state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector provider-owned control-plane dependency-aware apply-and-reconcile hardening state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing dependency-aware apply-and-reconcile hardening summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable provider-owned control-plane dependency-aware apply-and-reconcile hardening categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with dependency-aware apply-and-reconcile hardening.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with dependency-aware apply-and-reconcile hardening.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the operator-facing execution-ownership mode that informed dependency-aware apply-and-reconcile hardening.
    /// </summary>
    public string ExecutionOwnership { get; init; } = "runtime-managed";

    /// <summary>
    /// Gets the operator-facing execution-topology classification that informed dependency-aware apply-and-reconcile hardening.
    /// </summary>
    public string ExecutionTopology { get; init; } = "not-configured";

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the stable management-operation identifier currently associated with dependency-aware apply-and-reconcile hardening.
    /// </summary>
    public string OperationId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive dependency-aware apply-and-reconcile hardening.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningSources.Unknown;

    /// <summary>
    /// Gets the current broader managed-connector governance state that informed dependency-aware apply-and-reconcile hardening.
    /// </summary>
    public string GovernanceState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.NotApplicable;

    /// <summary>
    /// Gets the current broader managed-connector drift state that informed dependency-aware apply-and-reconcile hardening.
    /// </summary>
    public string DriftState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDriftStates.NotApplicable;

    /// <summary>
    /// Gets the current provider-owned control-plane apply-and-reconcile execution state that informed dependency-aware hardening.
    /// </summary>
    public string ProviderOwnedControlPlaneApplyAndReconcileExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStates.NotApplicable;

    /// <summary>
    /// Gets the current provider-owned control-plane provisioning state that informed dependency-aware hardening.
    /// </summary>
    public string ProviderOwnedControlPlaneProvisioningState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates.NotApplicable;

    /// <summary>
    /// Gets the current provider-owned control-plane mutation and reconcile state that informed dependency-aware hardening.
    /// </summary>
    public string ProviderOwnedControlPlaneMutationReconcileState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStates.NotApplicable;

    /// <summary>
    /// Gets the current provider-owned control-plane ownership state that informed dependency-aware hardening.
    /// </summary>
    public string ProviderOwnedControlPlaneOwnershipState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStates.NotApplicable;

    /// <summary>
    /// Gets the current broader provider execution-orchestration state that informed dependency-aware hardening.
    /// </summary>
    public string ProviderExecutionOrchestrationState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates.NotApplicable;

    /// <summary>
    /// Gets the current provider-owned write-path execution state that informed dependency-aware hardening.
    /// </summary>
    public string ProviderOwnedWritePathExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates.NotApplicable;

    /// <summary>
    /// Gets the latest recorded managed-connector command-execution state visible to dependency-aware hardening.
    /// </summary>
    public string LatestCommandExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded;

    /// <summary>
    /// Gets the current managed-connector command-retry state that informed dependency-aware hardening.
    /// </summary>
    public string CommandRetryState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector retry-execution policy state that informed dependency-aware hardening.
    /// </summary>
    public string RetryExecutionPolicyState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-journal state that informed dependency-aware hardening.
    /// </summary>
    public string CommandJournalState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.NotApplicable;

    /// <summary>
    /// Gets the best available connector-cluster identifier currently associated with dependency-aware hardening.
    /// </summary>
    public string? ConnectClusterId { get; init; }

    /// <summary>
    /// Gets the best available connector-class identifier currently associated with dependency-aware hardening.
    /// </summary>
    public string? ConnectorClass { get; init; }

    /// <summary>
    /// Gets the best available source-provider identifier currently associated with dependency-aware hardening.
    /// </summary>
    public string? SourceProviderId { get; init; }

    /// <summary>
    /// Gets the declared connector-cluster identifier when one is known.
    /// </summary>
    public string? DeclaredConnectClusterId { get; init; }

    /// <summary>
    /// Gets the last reported connector-cluster identifier when one is known.
    /// </summary>
    public string? ReportedConnectClusterId { get; init; }

    /// <summary>
    /// Gets the declared connector-class identifier when one is known.
    /// </summary>
    public string? DeclaredConnectorClass { get; init; }

    /// <summary>
    /// Gets the last reported connector-class identifier when one is known.
    /// </summary>
    public string? ReportedConnectorClass { get; init; }

    /// <summary>
    /// Gets the declared source-provider identifier when one is known.
    /// </summary>
    public string? DeclaredSourceProviderId { get; init; }

    /// <summary>
    /// Gets the last reported source-provider identifier when one is known.
    /// </summary>
    public string? ReportedSourceProviderId { get; init; }

    /// <summary>
    /// Gets the declared task count when one is known.
    /// </summary>
    public int? ExpectedTaskCount { get; init; }

    /// <summary>
    /// Gets the last reported task count when one is known.
    /// </summary>
    public int? ReportedTaskCount { get; init; }

    /// <summary>
    /// Gets the declared connector task identifiers when one baseline exists.
    /// </summary>
    public IReadOnlyList<string> DeclaredTaskIds { get; init; } = [];

    /// <summary>
    /// Gets the last reported connector task identifiers when one report exists.
    /// </summary>
    public IReadOnlyList<string> ReportedTaskIds { get; init; } = [];

    /// <summary>
    /// Gets the currently active connector task identifiers when one report exists.
    /// </summary>
    public IReadOnlyList<string> ActiveTaskIds { get; init; } = [];

    /// <summary>
    /// Gets the latest reconciliation state currently visible for the managed connector.
    /// </summary>
    public string? ReconciliationState { get; init; }

    /// <summary>
    /// Gets the latest operator-facing reconciliation summary currently visible for the managed connector.
    /// </summary>
    public string? ReconciliationReason { get; init; }

    /// <summary>
    /// Gets the number of visible potential shared write-path changes currently associated with dependency-aware hardening.
    /// </summary>
    public int PotentialChangeCount { get; init; }

    /// <summary>
    /// Gets the stable latest recorded command-execution attempt identifier when one exists.
    /// </summary>
    public string LatestAttemptId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when Cephalon recorded the latest command-execution outcome that informed dependency-aware hardening.
    /// </summary>
    public DateTimeOffset? LatestRecordedAtUtc { get; init; }

    /// <summary>
    /// Gets the active reporter identifier currently visible for the execution runtime when one exists.
    /// </summary>
    public string? ActiveReporterId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the active reporter lease expires when one is known.
    /// </summary>
    public DateTimeOffset? ActiveReporterLeaseExpiresAtUtc { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current provider lane would still apply one or more shared write-path changes.
    /// </summary>
    public bool WouldApplyChanges { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current provider lane still requires explicit approval.
    /// </summary>
    public bool RequiresExplicitApproval { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current provider lane targets a destructive connector operation.
    /// </summary>
    public bool IsDestructiveOperation { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current answer exposes one concrete target operation.
    /// </summary>
    public bool HasTargetOperation { get; init; }

    /// <summary>
    /// Gets a value indicating whether declared dependency identity is complete.
    /// </summary>
    public bool HasDeclaredDependencyIdentity { get; init; }

    /// <summary>
    /// Gets a value indicating whether reported dependency identity is complete.
    /// </summary>
    public bool HasReportedDependencyIdentity { get; init; }

    /// <summary>
    /// Gets a value indicating whether one declared task baseline is currently available.
    /// </summary>
    public bool HasTaskBaseline { get; init; }

    /// <summary>
    /// Gets a value indicating whether one reported task topology is currently available.
    /// </summary>
    public bool HasReportedTaskTopology { get; init; }

    /// <summary>
    /// Gets a value indicating whether one active task topology is currently available.
    /// </summary>
    public bool HasActiveTaskTopology { get; init; }

    /// <summary>
    /// Gets a value indicating whether dependency identity currently reports one declared-versus-observed mismatch.
    /// </summary>
    public bool HasDependencyIdentityMismatch { get; init; }

    /// <summary>
    /// Gets a value indicating whether task topology currently reports one declared-versus-observed mismatch.
    /// </summary>
    public bool HasTaskTopologyMismatch { get; init; }

    /// <summary>
    /// Gets a value indicating whether a durable command-journal store is currently configured.
    /// </summary>
    public bool HasDurableStoreConfigured { get; init; }

    /// <summary>
    /// Gets a value indicating whether the durable command-journal store currently exposes persisted recorded history.
    /// </summary>
    public bool HasPersistedRecordedHistory { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current process recovered persisted command history for this runtime.
    /// </summary>
    public bool HasRecoveredPersistedHistory { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current node can execute dependency-aware apply-and-reconcile safely.
    /// </summary>
    public bool CanExecuteDependencyAwareApplyAndReconcileOnCurrentNode { get; init; }

    /// <summary>
    /// Gets the number of active dependency-aware hardening categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets a value indicating whether the runtime currently exposes one active reporter lease.
    /// </summary>
    public bool HasActiveReporterLease =>
        !string.IsNullOrWhiteSpace(ActiveReporterId) &&
        ActiveReporterLeaseExpiresAtUtc.HasValue;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether dependency-aware apply-and-reconcile hardening still remains operator-owned outside Cephalon.
    /// </summary>
    public bool IsOperatorOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStates.OperatorOnly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether dependency-aware apply-and-reconcile hardening is currently ready.
    /// </summary>
    public bool IsDependencyReady => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStates.DependencyReady, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether dependency-aware apply-and-reconcile hardening currently remains blocked.
    /// </summary>
    public bool IsDependencyBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStates.DependencyBlocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether dependency-aware apply-and-reconcile hardening currently remains degraded.
    /// </summary>
    public bool IsDependencyDegraded => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStates.DependencyDegraded, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether dependency-aware apply-and-reconcile hardening is fully hardened.
    /// </summary>
    public bool IsApplyAndReconcileHardened => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStates.ApplyAndReconcileHardened, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether dependency-aware apply-and-reconcile hardening currently remains risky.
    /// </summary>
    public bool IsDependencyRisk => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningStates.DependencyRisk, StringComparison.OrdinalIgnoreCase);
}
