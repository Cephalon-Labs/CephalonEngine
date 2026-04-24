namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector provider-specific control-plane materializer posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStatus
{
    /// <summary>
    /// Creates a new provider-specific control-plane materializer answer.
    /// </summary>
    /// <param name="state">
    /// The stable provider-specific control-plane materializer state, such as <c>not-applicable</c>, <c>operator-only</c>, <c>materializer-unavailable</c>, <c>materializer-ready</c>, <c>materializer-selected</c>, <c>materializer-executing</c>, or <c>materializer-risk</c>.
    /// </param>
    /// <param name="description">An optional operator-facing provider-specific materializer summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector provider-specific control-plane materializer state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector provider-specific control-plane materializer state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing provider-specific materializer summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable provider-specific control-plane materializer categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with provider-specific control-plane materialization.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with provider-specific control-plane materialization.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the operator-facing execution-ownership mode that informed provider-specific materialization.
    /// </summary>
    public string ExecutionOwnership { get; init; } = "runtime-managed";

    /// <summary>
    /// Gets the operator-facing execution-topology classification that informed provider-specific materialization.
    /// </summary>
    public string ExecutionTopology { get; init; } = "not-configured";

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the stable management-operation identifier currently associated with provider-specific materialization.
    /// </summary>
    public string OperationId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive provider-specific materialization.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerSources.Unknown;

    /// <summary>
    /// Gets the current provider-owned control-plane dependency-aware provisioning and mutation hardening state that informed provider-specific materialization.
    /// </summary>
    public string ProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningStates.NotApplicable;

    /// <summary>
    /// Gets the current provider-owned control-plane apply-and-reconcile execution state that informed provider-specific materialization.
    /// </summary>
    public string ProviderOwnedControlPlaneApplyAndReconcileExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionStates.NotApplicable;

    /// <summary>
    /// Gets the current provider-owned control-plane provisioning state that informed provider-specific materialization.
    /// </summary>
    public string ProviderOwnedControlPlaneProvisioningState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates.NotApplicable;

    /// <summary>
    /// Gets the current provider-owned control-plane mutation and reconcile state that informed provider-specific materialization.
    /// </summary>
    public string ProviderOwnedControlPlaneMutationReconcileState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStates.NotApplicable;

    /// <summary>
    /// Gets the current provider-owned control-plane ownership state that informed provider-specific materialization.
    /// </summary>
    public string ProviderOwnedControlPlaneOwnershipState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStates.NotApplicable;

    /// <summary>
    /// Gets the current broader provider execution-orchestration state that informed provider-specific materialization.
    /// </summary>
    public string ProviderExecutionOrchestrationState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates.NotApplicable;

    /// <summary>
    /// Gets the current provider-owned write-path execution state that informed provider-specific materialization.
    /// </summary>
    public string ProviderOwnedWritePathExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates.NotApplicable;

    /// <summary>
    /// Gets the current shared execution-adapter state that informed provider-specific materialization.
    /// </summary>
    public string ExecutionAdapterState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.NotApplicable;

    /// <summary>
    /// Gets the latest recorded managed-connector command-execution state visible to provider-specific materialization.
    /// </summary>
    public string LatestCommandExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded;

    /// <summary>
    /// Gets the current managed-connector command-retry state that informed provider-specific materialization.
    /// </summary>
    public string CommandRetryState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector retry-execution policy state that informed provider-specific materialization.
    /// </summary>
    public string RetryExecutionPolicyState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-journal state that informed provider-specific materialization.
    /// </summary>
    public string CommandJournalState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.NotApplicable;

    /// <summary>
    /// Gets the provider identifier currently associated with provider-specific materialization when one is known.
    /// </summary>
    public string? ProviderId { get; init; }

    /// <summary>
    /// Gets the materializer identifier currently associated with provider-specific materialization when one is known.
    /// </summary>
    public string? MaterializerId { get; init; }

    /// <summary>
    /// Gets the provider transport kind currently associated with provider-specific materialization when one is known.
    /// </summary>
    public string? TransportKind { get; init; }

    /// <summary>
    /// Gets the provider-facing surface identifier currently associated with provider-specific materialization when one is known.
    /// </summary>
    public string? ProviderSurfaceId { get; init; }

    /// <summary>
    /// Gets the best available connector-cluster identifier currently associated with provider-specific materialization.
    /// </summary>
    public string? ConnectClusterId { get; init; }

    /// <summary>
    /// Gets the best available connector identifier currently associated with provider-specific materialization.
    /// </summary>
    public string? ConnectorId { get; init; }

    /// <summary>
    /// Gets the best available connector-class identifier currently associated with provider-specific materialization.
    /// </summary>
    public string? ConnectorClass { get; init; }

    /// <summary>
    /// Gets the best available source-provider identifier currently associated with provider-specific materialization.
    /// </summary>
    public string? SourceProviderId { get; init; }

    /// <summary>
    /// Gets the best available provider worker identifier currently associated with provider-specific materialization.
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
    /// Gets the number of visible potential shared write-path changes currently associated with provider-specific materialization.
    /// </summary>
    public int PotentialChangeCount { get; init; }

    /// <summary>
    /// Gets the stable latest recorded command-execution attempt identifier when one exists.
    /// </summary>
    public string LatestAttemptId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when Cephalon recorded the latest command-execution outcome that informed provider-specific materialization.
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
    /// Gets a value indicating whether the current answer currently represents the broader provisioning lane.
    /// </summary>
    public bool IsProvisioningOperation { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current answer currently represents one provider-owned mutation operation.
    /// </summary>
    public bool IsMutationOperation { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current answer currently represents one provider-owned reconcile operation.
    /// </summary>
    public bool IsReconcileOperation { get; init; }

    /// <summary>
    /// Gets a value indicating whether the runtime currently exposes one provider identity for provider-specific materialization.
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
    /// Gets a value indicating whether the runtime currently exposes one connector identity for provider-specific materialization.
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
    /// Gets a value indicating whether the current node can use the selected provider-specific control-plane materializer safely.
    /// </summary>
    public bool CanUseProviderSpecificControlPlaneMaterializerOnCurrentNode { get; init; }

    /// <summary>
    /// Gets the number of active provider-specific materializer categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets a value indicating whether the runtime currently exposes one active reporter lease.
    /// </summary>
    public bool HasActiveReporterLease =>
        !string.IsNullOrWhiteSpace(ActiveReporterId) &&
        ActiveReporterLeaseExpiresAtUtc.HasValue;

    /// <summary>
    /// Gets a value indicating whether the runtime currently exposes one selected provider-specific materializer.
    /// </summary>
    public bool HasSelectedMaterializer => HasProviderIdentity && HasMaterializerIdentity;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-specific control-plane materialization still remains operator-owned outside Cephalon.
    /// </summary>
    public bool IsOperatorOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates.OperatorOnly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether one provider-specific control-plane materializer is currently unavailable.
    /// </summary>
    public bool IsMaterializerUnavailable => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates.MaterializerUnavailable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether one provider-specific control-plane materializer is currently ready.
    /// </summary>
    public bool IsMaterializerReady => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates.MaterializerReady, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether one provider-specific control-plane materializer is currently selected.
    /// </summary>
    public bool IsMaterializerSelected => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates.MaterializerSelected, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the selected provider-specific control-plane materializer is currently executing.
    /// </summary>
    public bool IsMaterializerExecuting => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates.MaterializerExecuting, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-specific control-plane materialization currently remains risky.
    /// </summary>
    public bool IsMaterializerRisk => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderSpecificControlPlaneMaterializerStates.MaterializerRisk, StringComparison.OrdinalIgnoreCase);
}
