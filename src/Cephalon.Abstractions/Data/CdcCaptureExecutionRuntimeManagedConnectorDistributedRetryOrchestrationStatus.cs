namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector distributed retry orchestration posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus
{
    /// <summary>
    /// The stable shared scheduler identifier used by the bounded distributed retry orchestration lane.
    /// </summary>
    public const string DefaultSchedulerId = "managed-connector-automatic-retry-loop";

    /// <summary>
    /// The stable shared scheduler kind used by the bounded distributed retry orchestration lane.
    /// </summary>
    public const string DefaultSchedulerKind = "bounded-polling-loop";

    /// <summary>
    /// Creates a new managed-connector distributed retry orchestration answer.
    /// </summary>
    /// <param name="state">
    /// The stable distributed retry orchestration state, such as <c>not-applicable</c>, <c>disabled</c>, <c>operator-only</c>, <c>cooldown</c>, <c>blocked</c>, <c>scheduled</c>, or <c>completed</c>.
    /// </param>
    /// <param name="description">An optional operator-facing distributed retry orchestration summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector distributed retry orchestration state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector distributed retry orchestration state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing distributed retry orchestration summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable distributed retry orchestration categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with distributed retry orchestration.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with distributed retry orchestration.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the operator-facing execution-ownership mode that informed distributed retry orchestration.
    /// </summary>
    public string ExecutionOwnership { get; init; } = "runtime-managed";

    /// <summary>
    /// Gets the operator-facing execution-topology classification that informed distributed retry orchestration.
    /// </summary>
    public string ExecutionTopology { get; init; } = "not-configured";

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

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
    /// Gets the current managed-connector automatic background retry execution state that informed distributed retry orchestration.
    /// </summary>
    public string AutomaticRetryExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector automatic background retry coordination state that informed distributed retry orchestration.
    /// </summary>
    public string AutomaticRetryCoordinationState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector retry-execution policy state that informed distributed retry orchestration.
    /// </summary>
    public string RetryExecutionPolicyState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-journal durability state that informed distributed retry orchestration.
    /// </summary>
    public string CommandJournalDurabilityState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector distributed retry lease state that informed distributed retry orchestration.
    /// </summary>
    public string DistributedRetryLeaseState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates.NotApplicable;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive distributed retry orchestration.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationSources.Unknown;

    /// <summary>
    /// Gets the stable shared scheduler identifier currently associated with distributed retry orchestration.
    /// </summary>
    public string SchedulerId { get; init; } = DefaultSchedulerId;

    /// <summary>
    /// Gets the stable shared scheduler kind currently associated with distributed retry orchestration.
    /// </summary>
    public string SchedulerKind { get; init; } = DefaultSchedulerKind;

    /// <summary>
    /// Gets the bounded retry scheduler polling interval, in seconds, when one is configured.
    /// </summary>
    public int PollingIntervalSeconds { get; init; }

    /// <summary>
    /// Gets the stable management-operation identifier currently associated with distributed retry orchestration.
    /// </summary>
    public string OperationId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionOperationIds.None;

    /// <summary>
    /// Gets the deterministic retry fingerprint currently associated with distributed retry orchestration.
    /// </summary>
    public string RetryFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the active retry cooldown window ends, when one applies.
    /// </summary>
    public DateTimeOffset? CooldownUntilUtc { get; init; }

    /// <summary>
    /// Gets the latest automatic retry attempt identifier currently associated with distributed retry orchestration.
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
    /// Gets a value indicating whether the current node can currently schedule one bounded automatic retry attempt.
    /// </summary>
    public bool CanScheduleAutomaticRetryOnCurrentNode { get; init; }

    /// <summary>
    /// Gets the number of active distributed retry orchestration categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether distributed retry orchestration is currently disabled.
    /// </summary>
    public bool IsDisabled => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates.Disabled, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether distributed retry orchestration still remains operator-owned outside Cephalon.
    /// </summary>
    public bool IsOperatorOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates.OperatorOnly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether distributed retry orchestration is currently waiting for a cooldown window.
    /// </summary>
    public bool IsCooldown => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates.Cooldown, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether distributed retry orchestration is currently blocked by shared runtime truth.
    /// </summary>
    public bool IsBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates.Blocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether distributed retry orchestration can currently schedule one bounded automatic retry attempt.
    /// </summary>
    public bool IsScheduled => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates.Scheduled, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether distributed retry orchestration currently does not need to schedule another retry attempt.
    /// </summary>
    public bool IsCompleted => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates.Completed, StringComparison.OrdinalIgnoreCase);
}
