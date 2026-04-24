namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector provider-owned control-plane ownership posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus
{
    /// <summary>
    /// Creates a new managed-connector provider-owned control-plane ownership answer.
    /// </summary>
    /// <param name="state">
    /// The stable provider-owned control-plane ownership state, such as <c>not-applicable</c>, <c>operator-only</c>, <c>ownership-ready</c>, <c>ownership-blocked</c>, <c>ownership-active</c>, <c>ownership-partial</c>, or <c>ownership-risk</c>.
    /// </param>
    /// <param name="description">An optional operator-facing provider-owned control-plane ownership summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector provider-owned control-plane ownership state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector provider-owned control-plane ownership state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing provider-owned control-plane ownership summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable provider-owned control-plane ownership categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with provider-owned control-plane ownership.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with provider-owned control-plane ownership.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the operator-facing execution-ownership mode that informed provider-owned control-plane ownership.
    /// </summary>
    public string ExecutionOwnership { get; init; } = "runtime-managed";

    /// <summary>
    /// Gets the operator-facing execution-topology classification that informed provider-owned control-plane ownership.
    /// </summary>
    public string ExecutionTopology { get; init; } = "not-configured";

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the stable management-operation identifier currently associated with provider-owned control-plane ownership.
    /// </summary>
    public string OperationId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive provider-owned control-plane ownership.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipSources.Unknown;

    /// <summary>
    /// Gets the current broader provider execution-orchestration state that informed provider-owned control-plane ownership.
    /// </summary>
    public string ProviderExecutionOrchestrationState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates.NotApplicable;

    /// <summary>
    /// Gets the current provider-owned write-path execution state that informed provider-owned control-plane ownership.
    /// </summary>
    public string ProviderOwnedWritePathExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector execution-adapter state that informed provider-owned control-plane ownership.
    /// </summary>
    public string ExecutionAdapterState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.NotApplicable;

    /// <summary>
    /// Gets the latest recorded managed-connector command-execution state visible to provider-owned control-plane ownership.
    /// </summary>
    public string LatestCommandExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded;

    /// <summary>
    /// Gets the invocation-source identifier of the latest recorded command-execution outcome.
    /// </summary>
    public string LatestCommandExecutionInvocationSourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.None;

    /// <summary>
    /// Gets the current managed-connector retry-execution policy state that informed provider-owned control-plane ownership.
    /// </summary>
    public string RetryExecutionPolicyState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-journal state that informed provider-owned control-plane ownership.
    /// </summary>
    public string CommandJournalState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.NotApplicable;

    /// <summary>
    /// Gets the current durable shared scheduler-orchestration state that informed provider-owned control-plane ownership.
    /// </summary>
    public string DurableSharedSchedulerOrchestrationState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates.NotApplicable;

    /// <summary>
    /// Gets the current scheduler recovery and execution-hardening state that informed provider-owned control-plane ownership.
    /// </summary>
    public string SchedulerRecoveryExecutionHardeningState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates.NotApplicable;

    /// <summary>
    /// Gets the stable provider execution-adapter identifier currently associated with provider-owned control-plane ownership.
    /// </summary>
    public string AdapterId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.None;

    /// <summary>
    /// Gets the best available provider identifier currently associated with provider-owned control-plane ownership.
    /// </summary>
    public string? ProviderId { get; init; }

    /// <summary>
    /// Gets the best available connector-cluster identifier currently associated with provider-owned control-plane ownership.
    /// </summary>
    public string? ConnectClusterId { get; init; }

    /// <summary>
    /// Gets the best available connector-class identifier currently associated with provider-owned control-plane ownership.
    /// </summary>
    public string? ConnectorClass { get; init; }

    /// <summary>
    /// Gets the best available source-provider identifier currently associated with provider-owned control-plane ownership.
    /// </summary>
    public string? SourceProviderId { get; init; }

    /// <summary>
    /// Gets the deterministic command fingerprint currently associated with provider-owned control-plane ownership.
    /// </summary>
    public string CommandFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic execution-adapter fingerprint currently associated with provider-owned control-plane ownership.
    /// </summary>
    public string AdapterFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic latest recorded execution fingerprint currently visible to provider-owned control-plane ownership.
    /// </summary>
    public string LatestExecutionFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic retry fingerprint currently associated with provider-owned control-plane ownership.
    /// </summary>
    public string RetryFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of visible potential shared provider-execution changes currently associated with provider-owned control-plane ownership.
    /// </summary>
    public int PotentialChangeCount { get; init; }

    /// <summary>
    /// Gets the stable latest recorded command-execution attempt identifier when one exists.
    /// </summary>
    public string LatestAttemptId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when Cephalon recorded the latest command-execution outcome that informed provider-owned control-plane ownership.
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
    /// Gets the stable shared scheduler identifier currently associated with provider-owned control-plane ownership.
    /// </summary>
    public string SchedulerId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus.DefaultSchedulerId;

    /// <summary>
    /// Gets the stable shared scheduler kind currently associated with provider-owned control-plane ownership.
    /// </summary>
    public string SchedulerKind { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus.DefaultSchedulerKind;

    /// <summary>
    /// Gets the bounded retry scheduler polling interval, in seconds, when one is configured.
    /// </summary>
    public int PollingIntervalSeconds { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current provider-owned control-plane lane would still apply one or more shared write-path changes.
    /// </summary>
    public bool WouldApplyChanges { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current provider-owned control-plane lane still requires explicit approval.
    /// </summary>
    public bool RequiresExplicitApproval { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current provider-owned control-plane lane targets a destructive connector operation.
    /// </summary>
    public bool IsDestructiveOperation { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current command journal already exposes provider-owned control-plane evidence.
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
    /// Gets a value indicating whether the current node can exercise bounded provider-owned control-plane work safely.
    /// </summary>
    public bool CanExerciseProviderOwnedControlPlaneOnCurrentNode { get; init; }

    /// <summary>
    /// Gets the number of active provider-owned control-plane ownership categories currently visible for the execution runtime.
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
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-owned control-plane ownership still remains operator-owned outside Cephalon.
    /// </summary>
    public bool IsOperatorOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStates.OperatorOnly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-owned control-plane ownership is currently ready.
    /// </summary>
    public bool IsOwnershipReady => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStates.OwnershipReady, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-owned control-plane ownership currently remains blocked.
    /// </summary>
    public bool IsOwnershipBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStates.OwnershipBlocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-owned control-plane ownership is currently active on one bounded provider-facing step.
    /// </summary>
    public bool IsOwnershipActive => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStates.OwnershipActive, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-owned control-plane ownership currently remains partial.
    /// </summary>
    public bool IsOwnershipPartial => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStates.OwnershipPartial, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-owned control-plane ownership currently remains risky.
    /// </summary>
    public bool IsOwnershipRisk => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedControlPlaneOwnershipStates.OwnershipRisk, StringComparison.OrdinalIgnoreCase);
}
