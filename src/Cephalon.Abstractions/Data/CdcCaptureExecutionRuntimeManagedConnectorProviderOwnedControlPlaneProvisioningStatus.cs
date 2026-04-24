namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector provider-owned control-plane provisioning posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus
{
    /// <summary>
    /// Creates a new managed-connector provider-owned control-plane provisioning answer.
    /// </summary>
    /// <param name="state">
    /// The stable provider-owned control-plane provisioning state, such as <c>not-applicable</c>, <c>operator-only</c>, <c>provisioning-ready</c>, <c>provisioning-blocked</c>, <c>provisioning-executing</c>, <c>provisioning-partial</c>, or <c>provisioning-risk</c>.
    /// </param>
    /// <param name="description">An optional operator-facing provider-owned control-plane provisioning summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector provider-owned control-plane provisioning state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector provider-owned control-plane provisioning state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing provider-owned control-plane provisioning summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable provider-owned control-plane provisioning categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with provider-owned control-plane provisioning.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with provider-owned control-plane provisioning.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the operator-facing execution-ownership mode that informed provider-owned control-plane provisioning.
    /// </summary>
    public string ExecutionOwnership { get; init; } = "runtime-managed";

    /// <summary>
    /// Gets the operator-facing execution-topology classification that informed provider-owned control-plane provisioning.
    /// </summary>
    public string ExecutionTopology { get; init; } = "not-configured";

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the stable management-operation identifier currently associated with provider-owned control-plane provisioning.
    /// </summary>
    public string OperationId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive provider-owned control-plane provisioning.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningSources.Unknown;

    /// <summary>
    /// Gets the current provider-owned control-plane mutation and reconcile state that informed provider-owned control-plane provisioning.
    /// </summary>
    public string ProviderOwnedControlPlaneMutationReconcileState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneMutationReconcileStates.NotApplicable;

    /// <summary>
    /// Gets the current provider-owned control-plane ownership state that informed provider-owned control-plane provisioning.
    /// </summary>
    public string ProviderOwnedControlPlaneOwnershipState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStates.NotApplicable;

    /// <summary>
    /// Gets the current broader provider execution-orchestration state that informed provider-owned control-plane provisioning.
    /// </summary>
    public string ProviderExecutionOrchestrationState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates.NotApplicable;

    /// <summary>
    /// Gets the current provider-owned write-path execution state that informed provider-owned control-plane provisioning.
    /// </summary>
    public string ProviderOwnedWritePathExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-envelope state that informed provider-owned control-plane provisioning.
    /// </summary>
    public string CommandEnvelopeState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-issuance state that informed provider-owned control-plane provisioning.
    /// </summary>
    public string CommandIssuanceState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.NotApplicable;

    /// <summary>
    /// Gets the latest recorded managed-connector command-execution state visible to provider-owned control-plane provisioning.
    /// </summary>
    public string LatestCommandExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded;

    /// <summary>
    /// Gets the invocation-source identifier of the latest recorded command-execution outcome.
    /// </summary>
    public string LatestCommandExecutionInvocationSourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.None;

    /// <summary>
    /// Gets the current managed-connector command-retry state that informed provider-owned control-plane provisioning.
    /// </summary>
    public string CommandRetryState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector retry-execution policy state that informed provider-owned control-plane provisioning.
    /// </summary>
    public string RetryExecutionPolicyState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-journal state that informed provider-owned control-plane provisioning.
    /// </summary>
    public string CommandJournalState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.NotApplicable;

    /// <summary>
    /// Gets the stable provider execution-adapter identifier currently associated with provider-owned control-plane provisioning.
    /// </summary>
    public string AdapterId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.None;

    /// <summary>
    /// Gets the best available provider identifier currently associated with provider-owned control-plane provisioning.
    /// </summary>
    public string? ProviderId { get; init; }

    /// <summary>
    /// Gets the best available connector-cluster identifier currently associated with provider-owned control-plane provisioning.
    /// </summary>
    public string? ConnectClusterId { get; init; }

    /// <summary>
    /// Gets the best available connector-class identifier currently associated with provider-owned control-plane provisioning.
    /// </summary>
    public string? ConnectorClass { get; init; }

    /// <summary>
    /// Gets the best available source-provider identifier currently associated with provider-owned control-plane provisioning.
    /// </summary>
    public string? SourceProviderId { get; init; }

    /// <summary>
    /// Gets the deterministic command fingerprint currently associated with provider-owned control-plane provisioning.
    /// </summary>
    public string CommandFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic issuance fingerprint currently associated with provider-owned control-plane provisioning.
    /// </summary>
    public string IssuanceFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic latest recorded execution fingerprint currently visible to provider-owned control-plane provisioning.
    /// </summary>
    public string LatestExecutionFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic retry fingerprint currently associated with provider-owned control-plane provisioning.
    /// </summary>
    public string RetryFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of visible potential shared write-path changes currently associated with provider-owned control-plane provisioning.
    /// </summary>
    public int PotentialChangeCount { get; init; }

    /// <summary>
    /// Gets the stable latest recorded command-execution attempt identifier when one exists.
    /// </summary>
    public string LatestAttemptId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when Cephalon recorded the latest command-execution outcome that informed provider-owned control-plane provisioning.
    /// </summary>
    public DateTimeOffset? LatestRecordedAtUtc { get; init; }

    /// <summary>
    /// Gets the host-owned coordination owner identifier when one is known.
    /// </summary>
    public string? CoordinationOwnerId { get; init; }

    /// <summary>
    /// Gets the active reporter identifier currently visible for the execution runtime when one exists.
    /// </summary>
    public string? ActiveReporterId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the active reporter lease expires when one is known.
    /// </summary>
    public DateTimeOffset? ActiveReporterLeaseExpiresAtUtc { get; init; }

    /// <summary>
    /// Gets the stable shared scheduler identifier currently associated with provider-owned control-plane provisioning.
    /// </summary>
    public string SchedulerId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus.DefaultSchedulerId;

    /// <summary>
    /// Gets the stable shared scheduler kind currently associated with provider-owned control-plane provisioning.
    /// </summary>
    public string SchedulerKind { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus.DefaultSchedulerKind;

    /// <summary>
    /// Gets the bounded retry scheduler polling interval, in seconds, when one is configured.
    /// </summary>
    public int PollingIntervalSeconds { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current provider-owned control-plane provisioning lane would still apply one or more shared write-path changes.
    /// </summary>
    public bool WouldApplyChanges { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current provider-owned control-plane provisioning lane still requires explicit approval.
    /// </summary>
    public bool RequiresExplicitApproval { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current provider-owned control-plane provisioning lane targets a destructive connector operation.
    /// </summary>
    public bool IsDestructiveOperation { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current answer exposes one concrete target operation.
    /// </summary>
    public bool HasTargetOperation { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current target operation is a provider-owned mutation rather than reconcile.
    /// </summary>
    public bool IsMutationOperation { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current target operation is a provider-owned reconcile.
    /// </summary>
    public bool IsReconcileOperation { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current command journal already exposes provider-owned control-plane provisioning evidence.
    /// </summary>
    public bool HasCommandJournalEvidence { get; init; }

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
    /// Gets a value indicating whether the current node can exercise bounded provider-owned control-plane ownership safely.
    /// </summary>
    public bool CanExerciseProviderOwnedControlPlaneOnCurrentNode { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current node can exercise the current provider-owned control-plane mutation or reconcile answer safely.
    /// </summary>
    public bool CanMutateOrReconcileOnCurrentNode { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current node can exercise the current provider-owned control-plane provisioning answer safely.
    /// </summary>
    public bool CanProvisionProviderOwnedControlPlaneOnCurrentNode { get; init; }

    /// <summary>
    /// Gets the number of active provider-owned control-plane provisioning categories currently visible for the execution runtime.
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
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-owned control-plane provisioning still remains operator-owned outside Cephalon.
    /// </summary>
    public bool IsOperatorOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates.OperatorOnly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-owned control-plane provisioning is currently ready.
    /// </summary>
    public bool IsProvisioningReady => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates.ProvisioningReady, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-owned control-plane provisioning currently remains blocked.
    /// </summary>
    public bool IsProvisioningBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates.ProvisioningBlocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-owned control-plane provisioning is currently executing one bounded provider-facing step.
    /// </summary>
    public bool IsProvisioningExecuting => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates.ProvisioningExecuting, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-owned control-plane provisioning currently remains partial.
    /// </summary>
    public bool IsProvisioningPartial => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates.ProvisioningPartial, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-owned control-plane provisioning currently remains risky.
    /// </summary>
    public bool IsProvisioningRisk => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneProvisioningStates.ProvisioningRisk, StringComparison.OrdinalIgnoreCase);
}
