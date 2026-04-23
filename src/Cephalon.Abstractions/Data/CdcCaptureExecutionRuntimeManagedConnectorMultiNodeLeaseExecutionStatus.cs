namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing broader multi-node lease-execution posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStatus
{
    /// <summary>
    /// Creates a new managed-connector broader multi-node lease-execution answer.
    /// </summary>
    /// <param name="state">
    /// The stable broader multi-node lease-execution state, such as <c>not-applicable</c>, <c>operator-only</c>, <c>single-node</c>, <c>lease-executable</c>, <c>lease-blocked</c>, <c>lease-conflicted</c>, or <c>stale-lease-risk</c>.
    /// </param>
    /// <param name="description">An optional operator-facing broader multi-node lease-execution summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector broader multi-node lease-execution state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector broader multi-node lease-execution state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing broader multi-node lease-execution summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable broader multi-node lease-execution categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with broader multi-node lease execution.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with broader multi-node lease execution.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the operator-facing execution-ownership mode that informed broader multi-node lease execution.
    /// </summary>
    public string ExecutionOwnership { get; init; } = "runtime-managed";

    /// <summary>
    /// Gets the operator-facing execution-topology classification that informed broader multi-node lease execution.
    /// </summary>
    public string ExecutionTopology { get; init; } = "not-configured";

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the stable management-operation identifier currently associated with broader multi-node lease execution.
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
    /// Gets the current managed-connector automatic background retry coordination state that informed broader multi-node lease execution.
    /// </summary>
    public string AutomaticRetryCoordinationState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector distributed retry lease state that informed broader multi-node lease execution.
    /// </summary>
    public string DistributedRetryLeaseState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector cross-node idempotency-hardening state that informed broader multi-node lease execution.
    /// </summary>
    public string CrossNodeIdempotencyHardeningState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector distributed retry orchestration state that informed broader multi-node lease execution.
    /// </summary>
    public string DistributedRetryOrchestrationState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStates.NotApplicable;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive broader multi-node lease execution.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionSources.Unknown;

    /// <summary>
    /// Gets the stable shared scheduler identifier currently associated with broader multi-node lease execution.
    /// </summary>
    public string SchedulerId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus.DefaultSchedulerId;

    /// <summary>
    /// Gets the stable shared scheduler kind currently associated with broader multi-node lease execution.
    /// </summary>
    public string SchedulerKind { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus.DefaultSchedulerKind;

    /// <summary>
    /// Gets the bounded retry scheduler polling interval, in seconds, when one is configured.
    /// </summary>
    public int PollingIntervalSeconds { get; init; }

    /// <summary>
    /// Gets the deterministic retry fingerprint currently associated with broader multi-node lease execution.
    /// </summary>
    public string RetryFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the active retry cooldown window ends, when one applies.
    /// </summary>
    public DateTimeOffset? CooldownUntilUtc { get; init; }

    /// <summary>
    /// Gets the latest automatic retry attempt identifier currently associated with broader multi-node lease execution.
    /// </summary>
    public string LatestAutomaticRetryAttemptId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when Cephalon recorded the latest automatic retry attempt when one exists.
    /// </summary>
    public DateTimeOffset? LatestAutomaticRetryRecordedAtUtc { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current host coordination owner matches the active reporter identifier.
    /// </summary>
    public bool CoordinationOwnerMatchesActiveReporter { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current node can execute the next bounded automatic retry step.
    /// </summary>
    public bool CanExecuteAutomaticRetryOnCurrentNode { get; init; }

    /// <summary>
    /// Gets the number of active broader multi-node lease-execution categories currently visible for the execution runtime.
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
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether broader multi-node lease execution still remains operator-owned outside Cephalon.
    /// </summary>
    public bool IsOperatorOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates.OperatorOnly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether broader multi-node lease execution currently runs as a single-node posture.
    /// </summary>
    public bool IsSingleNode => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates.SingleNode, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the current node can execute the next bounded automatic retry step under the active multi-node lease posture.
    /// </summary>
    public bool IsLeaseExecutable => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates.LeaseExecutable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the current multi-node lease posture still blocks execution on this node.
    /// </summary>
    public bool IsLeaseBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates.LeaseBlocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the current multi-node lease posture remains conflicted across nodes.
    /// </summary>
    public bool IsLeaseConflicted => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates.LeaseConflicted, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the current multi-node lease posture still looks stale.
    /// </summary>
    public bool IsStaleLeaseRisk => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorMultiNodeLeaseExecutionStates.StaleLeaseRisk, StringComparison.OrdinalIgnoreCase);
}
