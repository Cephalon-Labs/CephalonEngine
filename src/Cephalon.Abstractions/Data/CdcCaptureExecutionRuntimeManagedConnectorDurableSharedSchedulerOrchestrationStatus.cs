namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector durable shared scheduler-orchestration posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus
{
    /// <summary>
    /// Creates a new managed-connector durable shared scheduler-orchestration answer.
    /// </summary>
    /// <param name="state">
    /// The stable durable shared scheduler-orchestration state, such as <c>not-applicable</c>, <c>disabled</c>, <c>operator-only</c>, <c>unscheduled</c>, <c>scheduled</c>, <c>lease-blocked</c>, <c>recovery-needed</c>, or <c>scheduler-conflicted</c>.
    /// </param>
    /// <param name="description">An optional operator-facing durable shared scheduler-orchestration summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector durable shared scheduler-orchestration state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector durable shared scheduler-orchestration state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing durable shared scheduler-orchestration summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable durable shared scheduler-orchestration categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with durable shared scheduler orchestration.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with durable shared scheduler orchestration.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the operator-facing execution-ownership mode that informed durable shared scheduler orchestration.
    /// </summary>
    public string ExecutionOwnership { get; init; } = "runtime-managed";

    /// <summary>
    /// Gets the operator-facing execution-topology classification that informed durable shared scheduler orchestration.
    /// </summary>
    public string ExecutionTopology { get; init; } = "not-configured";

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the stable management-operation identifier currently associated with durable shared scheduler orchestration.
    /// </summary>
    public string OperationId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionOperationIds.None;

    /// <summary>
    /// Gets the host-owned coordination owner identifier when one was configured for automatic retry.
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
    /// Gets the current managed-connector automatic background retry coordination state that informed durable shared scheduler orchestration.
    /// </summary>
    public string AutomaticRetryCoordinationState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-journal durability state that informed durable shared scheduler orchestration.
    /// </summary>
    public string CommandJournalDurabilityState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector distributed retry orchestration state that informed durable shared scheduler orchestration.
    /// </summary>
    public string DistributedRetryOrchestrationState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector broader multi-node lease-execution state that informed durable shared scheduler orchestration.
    /// </summary>
    public string MultiNodeLeaseExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates.NotApplicable;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive durable shared scheduler orchestration.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationSources.Unknown;

    /// <summary>
    /// Gets the stable shared scheduler identifier currently associated with durable shared scheduler orchestration.
    /// </summary>
    public string SchedulerId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus.DefaultSchedulerId;

    /// <summary>
    /// Gets the stable shared scheduler kind currently associated with durable shared scheduler orchestration.
    /// </summary>
    public string SchedulerKind { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus.DefaultSchedulerKind;

    /// <summary>
    /// Gets the bounded retry scheduler polling interval, in seconds, when one is configured.
    /// </summary>
    public int PollingIntervalSeconds { get; init; }

    /// <summary>
    /// Gets the deterministic retry fingerprint currently associated with durable shared scheduler orchestration.
    /// </summary>
    public string RetryFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the active retry cooldown window ends, when one applies.
    /// </summary>
    public DateTimeOffset? CooldownUntilUtc { get; init; }

    /// <summary>
    /// Gets the latest automatic retry attempt identifier currently associated with durable shared scheduler orchestration.
    /// </summary>
    public string LatestAutomaticRetryAttemptId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when Cephalon recorded the latest automatic retry attempt when one exists.
    /// </summary>
    public DateTimeOffset? LatestAutomaticRetryRecordedAtUtc { get; init; }

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
    /// Gets a value indicating whether the current node can currently keep one bounded automatic retry scheduled.
    /// </summary>
    public bool CanScheduleAutomaticRetryOnCurrentNode { get; init; }

    /// <summary>
    /// Gets the number of active durable shared scheduler-orchestration categories currently visible for the execution runtime.
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
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether durable shared scheduler orchestration is currently disabled.
    /// </summary>
    public bool IsDisabled => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates.Disabled, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether durable shared scheduler orchestration still remains operator-owned outside Cephalon.
    /// </summary>
    public bool IsOperatorOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates.OperatorOnly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether durable shared scheduler orchestration currently does not need to keep the runtime scheduled.
    /// </summary>
    public bool IsUnscheduled => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates.Unscheduled, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether durable shared scheduler orchestration can currently keep one bounded retry scheduled on the current node.
    /// </summary>
    public bool IsScheduled => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates.Scheduled, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether durable shared scheduler orchestration remains blocked by broader lease-execution truth.
    /// </summary>
    public bool IsLeaseBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates.LeaseBlocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether durable shared scheduler orchestration still needs durable journal recovery or persistence hardening.
    /// </summary>
    public bool IsRecoveryNeeded => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates.RecoveryNeeded, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether durable shared scheduler orchestration remains conflicted across coordination or lease ownership truth.
    /// </summary>
    public bool IsSchedulerConflicted => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates.SchedulerConflicted, StringComparison.OrdinalIgnoreCase);
}
