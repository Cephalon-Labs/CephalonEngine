namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector provider-specific control-plane dependency-aware teardown and mutation-execution hardening posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus
{
    /// <summary>
    /// Creates a new managed-connector provider-specific control-plane dependency-aware teardown and mutation-execution hardening answer.
    /// </summary>
    /// <param name="state">
    /// The stable provider-specific dependency-aware teardown and mutation-execution hardening state, such as <c>not-applicable</c>, <c>operator-only</c>, <c>dependency-ready</c>, <c>teardown-blocked</c>, <c>mutation-execution-blocked</c>, <c>dependency-degraded</c>, <c>teardown-hardened</c>, <c>mutation-execution-hardened</c>, or <c>dependency-risk</c>.
    /// </param>
    /// <param name="description">An optional operator-facing teardown and mutation-execution hardening summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector provider-specific control-plane dependency-aware teardown and mutation-execution hardening state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector provider-specific control-plane dependency-aware teardown and mutation-execution hardening state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing teardown and mutation-execution hardening summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable provider-specific dependency-aware teardown and mutation-execution hardening categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with provider-specific teardown and mutation-execution hardening.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with provider-specific teardown and mutation-execution hardening.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the operator-facing execution-ownership mode that informed provider-specific teardown and mutation-execution hardening.
    /// </summary>
    public string ExecutionOwnership { get; init; } = "runtime-managed";

    /// <summary>
    /// Gets the operator-facing execution-topology classification that informed provider-specific teardown and mutation-execution hardening.
    /// </summary>
    public string ExecutionTopology { get; init; } = "not-configured";

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the stable management-operation identifier currently associated with provider-specific teardown and mutation-execution hardening.
    /// </summary>
    public string OperationId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive provider-specific teardown and mutation-execution hardening.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningSources.Unknown;

    /// <summary>
    /// Gets the current provider-specific control-plane materializer state that informed this hardening answer.
    /// </summary>
    public string ProviderSpecificControlPlaneMaterializerState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates.NotApplicable;

    /// <summary>
    /// Gets the current provider-owned control-plane dependency-aware provisioning and mutation hardening state that informed this hardening answer.
    /// </summary>
    public string ProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStates.NotApplicable;

    /// <summary>
    /// Gets the current provider-owned control-plane apply-and-reconcile execution state that informed this hardening answer.
    /// </summary>
    public string ProviderOwnedControlPlaneApplyAndReconcileExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStates.NotApplicable;

    /// <summary>
    /// Gets the current provider-owned control-plane provisioning state that informed this hardening answer.
    /// </summary>
    public string ProviderOwnedControlPlaneProvisioningState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates.NotApplicable;

    /// <summary>
    /// Gets the current provider-owned control-plane mutation and reconcile state that informed this hardening answer.
    /// </summary>
    public string ProviderOwnedControlPlaneMutationReconcileState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStates.NotApplicable;

    /// <summary>
    /// Gets the current provider-owned control-plane ownership state that informed this hardening answer.
    /// </summary>
    public string ProviderOwnedControlPlaneOwnershipState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStates.NotApplicable;

    /// <summary>
    /// Gets the current broader provider execution-orchestration state that informed this hardening answer.
    /// </summary>
    public string ProviderExecutionOrchestrationState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates.NotApplicable;

    /// <summary>
    /// Gets the current provider-owned write-path execution state that informed this hardening answer.
    /// </summary>
    public string ProviderOwnedWritePathExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates.NotApplicable;

    /// <summary>
    /// Gets the current shared execution-adapter state that informed this hardening answer.
    /// </summary>
    public string ExecutionAdapterState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.NotApplicable;

    /// <summary>
    /// Gets the latest recorded managed-connector command-execution state visible to this hardening answer.
    /// </summary>
    public string LatestCommandExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded;

    /// <summary>
    /// Gets the current managed-connector command-retry state that informed this hardening answer.
    /// </summary>
    public string CommandRetryState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector retry-execution policy state that informed this hardening answer.
    /// </summary>
    public string RetryExecutionPolicyState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-journal state that informed this hardening answer.
    /// </summary>
    public string CommandJournalState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.NotApplicable;

    /// <summary>
    /// Gets the provider identifier currently associated with provider-specific teardown and mutation execution when one is known.
    /// </summary>
    public string? ProviderId { get; init; }

    /// <summary>
    /// Gets the materializer identifier currently associated with provider-specific teardown and mutation execution when one is known.
    /// </summary>
    public string? MaterializerId { get; init; }

    /// <summary>
    /// Gets the provider transport kind currently associated with provider-specific teardown and mutation execution when one is known.
    /// </summary>
    public string? TransportKind { get; init; }

    /// <summary>
    /// Gets the provider-facing surface identifier currently associated with provider-specific teardown and mutation execution when one is known.
    /// </summary>
    public string? ProviderSurfaceId { get; init; }

    /// <summary>
    /// Gets the best available connector-cluster identifier currently associated with provider-specific teardown and mutation execution.
    /// </summary>
    public string? ConnectClusterId { get; init; }

    /// <summary>
    /// Gets the best available connector identifier currently associated with provider-specific teardown and mutation execution.
    /// </summary>
    public string? ConnectorId { get; init; }

    /// <summary>
    /// Gets the best available connector-class identifier currently associated with provider-specific teardown and mutation execution.
    /// </summary>
    public string? ConnectorClass { get; init; }

    /// <summary>
    /// Gets the best available source-provider identifier currently associated with provider-specific teardown and mutation execution.
    /// </summary>
    public string? SourceProviderId { get; init; }

    /// <summary>
    /// Gets the best available provider worker identifier currently associated with provider-specific teardown and mutation execution.
    /// </summary>
    public string? WorkerId { get; init; }

    /// <summary>
    /// Gets the latest reconciliation state currently visible for the managed connector.
    /// </summary>
    public string? ReconciliationState { get; init; }

    /// <summary>
    /// Gets the latest operator-facing reconciliation summary currently visible for the managed connector.
    /// </summary>
    public string? ReconciliationReason { get; init; }

    /// <summary>
    /// Gets the number of visible potential shared write-path changes currently associated with this hardening answer.
    /// </summary>
    public int PotentialChangeCount { get; init; }

    /// <summary>
    /// Gets the stable latest recorded command-execution attempt identifier when one exists.
    /// </summary>
    public string LatestAttemptId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when Cephalon recorded the latest command-execution outcome that informed this hardening answer.
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
    /// Gets a value indicating whether the current answer currently represents one teardown operation.
    /// </summary>
    public bool IsTeardownOperation { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current answer currently represents one mutation-execution operation.
    /// </summary>
    public bool IsMutationExecutionOperation { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current answer currently represents one provider-owned mutation operation.
    /// </summary>
    public bool IsMutationOperation { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current answer currently represents one provider-owned reconcile operation.
    /// </summary>
    public bool IsReconcileOperation { get; init; }

    /// <summary>
    /// Gets a value indicating whether the runtime currently exposes one provider identity.
    /// </summary>
    public bool HasProviderIdentity { get; init; }

    /// <summary>
    /// Gets a value indicating whether the runtime currently exposes one selected materializer identifier.
    /// </summary>
    public bool HasMaterializerIdentity { get; init; }

    /// <summary>
    /// Gets a value indicating whether the runtime currently exposes one provider transport kind.
    /// </summary>
    public bool HasTransportIdentity { get; init; }

    /// <summary>
    /// Gets a value indicating whether the runtime currently exposes one provider-facing surface identifier.
    /// </summary>
    public bool HasProviderSurfaceIdentity { get; init; }

    /// <summary>
    /// Gets a value indicating whether the runtime currently exposes one connector identity.
    /// </summary>
    public bool HasConnectorIdentity { get; init; }

    /// <summary>
    /// Gets a value indicating whether the runtime currently exposes one provider worker identity.
    /// </summary>
    public bool HasWorkerIdentity { get; init; }

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
    /// Gets a value indicating whether the current node can execute dependency-aware teardown or mutation execution safely.
    /// </summary>
    public bool CanExecuteDependencyAwareTeardownAndMutationExecutionOnCurrentNode { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current node can execute dependency-aware teardown safely.
    /// </summary>
    public bool CanExecuteDependencyAwareTeardownOnCurrentNode { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current node can execute dependency-aware mutation execution safely.
    /// </summary>
    public bool CanExecuteDependencyAwareMutationExecutionOnCurrentNode { get; init; }

    /// <summary>
    /// Gets the number of active provider-specific teardown and mutation-execution hardening categories currently visible for the execution runtime.
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
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-specific dependency-aware teardown and mutation-execution hardening still remains operator-owned outside Cephalon.
    /// </summary>
    public bool IsOperatorOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates.OperatorOnly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-specific dependency-aware teardown and mutation execution is currently ready.
    /// </summary>
    public bool IsDependencyReady => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates.DependencyReady, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-specific dependency-aware teardown currently remains blocked.
    /// </summary>
    public bool IsTeardownBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates.TeardownBlocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-specific dependency-aware mutation execution currently remains blocked.
    /// </summary>
    public bool IsMutationExecutionBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates.MutationExecutionBlocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-specific dependency-aware teardown and mutation execution currently remains degraded.
    /// </summary>
    public bool IsDependencyDegraded => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates.DependencyDegraded, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-specific dependency-aware teardown is fully hardened.
    /// </summary>
    public bool IsTeardownHardened => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates.TeardownHardened, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-specific dependency-aware mutation execution is fully hardened.
    /// </summary>
    public bool IsMutationExecutionHardened => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates.MutationExecutionHardened, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-specific dependency-aware teardown and mutation execution currently remains risky.
    /// </summary>
    public bool IsDependencyRisk => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningStates.DependencyRisk, StringComparison.OrdinalIgnoreCase);
}
