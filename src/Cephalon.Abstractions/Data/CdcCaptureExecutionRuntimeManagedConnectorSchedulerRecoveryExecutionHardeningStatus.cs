namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector scheduler recovery and execution-hardening posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus
{
    /// <summary>
    /// Creates a new managed-connector scheduler recovery and execution-hardening answer.
    /// </summary>
    /// <param name="state">
    /// The stable scheduler recovery and execution-hardening state, such as <c>not-applicable</c>, <c>operator-only</c>, <c>recovery-ready</c>, <c>recovery-blocked</c>, <c>replaying</c>, <c>execution-hardened</c>, or <c>execution-risk</c>.
    /// </param>
    /// <param name="description">An optional operator-facing scheduler recovery and execution-hardening summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector scheduler recovery and execution-hardening state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector scheduler recovery and execution-hardening state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing scheduler recovery and execution-hardening summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable scheduler recovery and execution-hardening categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with scheduler recovery and execution hardening.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with scheduler recovery and execution hardening.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the operator-facing execution-ownership mode that informed scheduler recovery and execution hardening.
    /// </summary>
    public string ExecutionOwnership { get; init; } = "runtime-managed";

    /// <summary>
    /// Gets the operator-facing execution-topology classification that informed scheduler recovery and execution hardening.
    /// </summary>
    public string ExecutionTopology { get; init; } = "not-configured";

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the stable management-operation identifier currently associated with scheduler recovery and execution hardening.
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
    /// Gets the current managed-connector durable shared scheduler-orchestration state that informed scheduler recovery and execution hardening.
    /// </summary>
    public string DurableSharedSchedulerOrchestrationState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector broader multi-node lease-execution state that informed scheduler recovery and execution hardening.
    /// </summary>
    public string MultiNodeLeaseExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector distributed retry orchestration state that informed scheduler recovery and execution hardening.
    /// </summary>
    public string DistributedRetryOrchestrationState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-journal durability state that informed scheduler recovery and execution hardening.
    /// </summary>
    public string CommandJournalDurabilityState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.NotApplicable;

    /// <summary>
    /// Gets the latest recorded managed-connector command-execution state visible to scheduler recovery and execution hardening.
    /// </summary>
    public string LatestCommandExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded;

    /// <summary>
    /// Gets the invocation-source identifier of the latest recorded command-execution outcome.
    /// </summary>
    public string LatestCommandExecutionInvocationSourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.None;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive scheduler recovery and execution hardening.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningSources.Unknown;

    /// <summary>
    /// Gets the stable shared scheduler identifier currently associated with scheduler recovery and execution hardening.
    /// </summary>
    public string SchedulerId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus.DefaultSchedulerId;

    /// <summary>
    /// Gets the stable shared scheduler kind currently associated with scheduler recovery and execution hardening.
    /// </summary>
    public string SchedulerKind { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus.DefaultSchedulerKind;

    /// <summary>
    /// Gets the bounded retry scheduler polling interval, in seconds, when one is configured.
    /// </summary>
    public int PollingIntervalSeconds { get; init; }

    /// <summary>
    /// Gets the deterministic retry fingerprint currently associated with scheduler recovery and execution hardening.
    /// </summary>
    public string RetryFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the stable latest automatic retry attempt identifier when one exists.
    /// </summary>
    public string LatestAutomaticRetryAttemptId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic execution fingerprint of the latest automatic retry attempt when one exists.
    /// </summary>
    public string LatestAutomaticRetryExecutionFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic execution fingerprint of the latest recorded command-execution outcome when one exists.
    /// </summary>
    public string LatestCommandExecutionFingerprint { get; init; } = string.Empty;

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
    /// Gets a value indicating whether retained history currently contains one automatic retry attempt for the current retry fingerprint.
    /// </summary>
    public bool HasMatchingAutomaticRetryAttempt { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current node can safely execute the next bounded automatic retry step.
    /// </summary>
    public bool CanExecuteAutomaticRetryOnCurrentNode { get; init; }

    /// <summary>
    /// Gets the number of active scheduler recovery and execution-hardening categories currently visible for the execution runtime.
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
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether scheduler recovery and execution hardening still remains operator-owned outside Cephalon.
    /// </summary>
    public bool IsOperatorOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates.OperatorOnly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether scheduler recovery is ready for safe bounded execution on the current node.
    /// </summary>
    public bool IsRecoveryReady => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates.RecoveryReady, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether scheduler recovery still remains blocked by missing or unhealthy durable evidence.
    /// </summary>
    public bool IsRecoveryBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates.RecoveryBlocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether scheduler recovery is replaying retained execution evidence.
    /// </summary>
    public bool IsReplaying => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates.Replaying, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether scheduler execution truth currently looks hardened enough for bounded execution.
    /// </summary>
    public bool IsExecutionHardened => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates.ExecutionHardened, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether scheduler execution truth currently remains risky.
    /// </summary>
    public bool IsExecutionRisk => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates.ExecutionRisk, StringComparison.OrdinalIgnoreCase);
}
