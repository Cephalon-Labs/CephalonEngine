namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector distributed retry lease and cross-node idempotency posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStatus
{
    /// <summary>
    /// Creates a new managed-connector distributed retry lease answer.
    /// </summary>
    /// <param name="state">
    /// The stable distributed retry lease state, such as <c>not-applicable</c>, <c>single-node</c>, <c>lease-held</c>, <c>lease-missing</c>, <c>lease-conflicted</c>, <c>idempotent-safe</c>, <c>idempotency-risk</c>, or <c>operator-only</c>.
    /// </param>
    /// <param name="description">An optional operator-facing distributed retry lease summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector distributed retry lease state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector distributed retry lease state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing distributed retry lease summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable distributed retry lease categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with distributed retry lease posture.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with distributed retry lease posture.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the operator-facing execution-ownership mode that informed distributed retry lease posture.
    /// </summary>
    public string ExecutionOwnership { get; init; } = "runtime-managed";

    /// <summary>
    /// Gets the operator-facing execution-topology classification that informed distributed retry lease posture.
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
    /// Gets the current managed-connector automatic background retry execution state that informed distributed retry lease posture.
    /// </summary>
    public string AutomaticRetryExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector automatic background retry coordination state that informed distributed retry lease posture.
    /// </summary>
    public string AutomaticRetryCoordinationState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector retry-execution policy state that informed distributed retry lease posture.
    /// </summary>
    public string RetryExecutionPolicyState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector bounded command-journal state that informed distributed retry lease posture.
    /// </summary>
    public string CommandJournalState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-journal durability state that informed distributed retry lease posture.
    /// </summary>
    public string CommandJournalDurabilityState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.NotApplicable;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive distributed retry lease posture.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseSources.Unknown;

    /// <summary>
    /// Gets the deterministic retry fingerprint currently associated with distributed retry lease posture.
    /// </summary>
    public string RetryFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the latest automatic retry attempt identifier currently associated with distributed retry lease posture.
    /// </summary>
    public string LatestAutomaticRetryAttemptId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the latest automatic retry execution fingerprint currently associated with distributed retry lease posture.
    /// </summary>
    public string LatestAutomaticRetryExecutionFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of bounded command-history entries currently retained for the execution runtime.
    /// </summary>
    public int RetainedEntryCount { get; init; }

    /// <summary>
    /// Gets the number of retained automatic retry attempts currently visible to distributed retry lease posture.
    /// </summary>
    public int AutomaticRetryAttemptCount { get; init; }

    /// <summary>
    /// Gets the number of retained automatic retry attempts that currently match the derived retry fingerprint.
    /// </summary>
    public int MatchingAutomaticRetryAttemptCount { get; init; }

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
    /// Gets a value indicating whether the bounded command journal currently retains one matching retry fingerprint.
    /// </summary>
    public bool HasMatchingRetryFingerprintHistory { get; init; }

    /// <summary>
    /// Gets a value indicating whether the bounded command journal currently retains one matching automatic retry attempt.
    /// </summary>
    public bool HasMatchingAutomaticRetryAttempt { get; init; }

    /// <summary>
    /// Gets a value indicating whether the bounded command journal currently retains multiple automatic retry attempts for the same retry fingerprint.
    /// </summary>
    public bool HasDuplicateAutomaticRetryAttempts { get; init; }

    /// <summary>
    /// Gets the number of active distributed retry lease categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the current host declared a coordination owner identifier for distributed retry.
    /// </summary>
    public bool HasCoordinationOwner => !string.IsNullOrWhiteSpace(CoordinationOwnerId);

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently exposes one active reporter lease.
    /// </summary>
    public bool HasActiveReporterLease =>
        !string.IsNullOrWhiteSpace(ActiveReporterId) &&
        ActiveReporterLeaseExpiresAtUtc.HasValue;

    /// <summary>
    /// Gets a value indicating whether the current host coordination owner matches the active reporter identifier.
    /// </summary>
    public bool CoordinationOwnerMatchesActiveReporter =>
        HasCoordinationOwner &&
        !string.IsNullOrWhiteSpace(ActiveReporterId) &&
        string.Equals(CoordinationOwnerId, ActiveReporterId, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether automatic retry can safely execute on the current node.
    /// </summary>
    public bool CanExecuteAutomaticRetryOnCurrentNode => IsSingleNode || IsLeaseHeld || IsIdempotentSafe;

    /// <summary>
    /// Gets a value indicating whether automatic retry can execute on a single node without cross-node lease coordination.
    /// </summary>
    public bool IsSingleNode => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates.SingleNode, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the current node holds the active retry lease but retained idempotency evidence remains incomplete.
    /// </summary>
    public bool IsLeaseHeld => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates.LeaseHeld, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether no active retry lease is currently visible.
    /// </summary>
    public bool IsLeaseMissing => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates.LeaseMissing, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether cross-node lease ownership or coordination remains conflicted.
    /// </summary>
    public bool IsLeaseConflicted => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates.LeaseConflicted, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether Cephalon currently has lease ownership plus restart-safe idempotency evidence.
    /// </summary>
    public bool IsIdempotentSafe => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates.IdempotentSafe, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether cross-node idempotency evidence currently remains risky for automatic retry.
    /// </summary>
    public bool IsIdempotencyRisk => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates.IdempotencyRisk, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether distributed retry still remains operator-owned outside Cephalon.
    /// </summary>
    public bool IsOperatorOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates.OperatorOnly, StringComparison.OrdinalIgnoreCase);
}
