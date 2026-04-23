namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector cross-node idempotency-hardening posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStatus
{
    /// <summary>
    /// Creates a new managed-connector cross-node idempotency-hardening answer.
    /// </summary>
    /// <param name="state">
    /// The stable cross-node idempotency-hardening state, such as <c>not-applicable</c>, <c>operator-only</c>, <c>idempotent-safe</c>, <c>stale-owner-risk</c>, <c>duplicate-lineage-risk</c>, or <c>replay-window-risk</c>.
    /// </param>
    /// <param name="description">An optional operator-facing cross-node idempotency-hardening summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector cross-node idempotency-hardening state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector cross-node idempotency-hardening state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing cross-node idempotency-hardening summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable cross-node idempotency-hardening categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with cross-node idempotency hardening.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with cross-node idempotency hardening.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the operator-facing execution-ownership mode that informed cross-node idempotency hardening.
    /// </summary>
    public string ExecutionOwnership { get; init; } = "runtime-managed";

    /// <summary>
    /// Gets the operator-facing execution-topology classification that informed cross-node idempotency hardening.
    /// </summary>
    public string ExecutionTopology { get; init; } = "not-configured";

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the stable management-operation identifier currently associated with cross-node idempotency hardening.
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
    /// Gets the current managed-connector automatic background retry execution state that informed cross-node idempotency hardening.
    /// </summary>
    public string AutomaticRetryExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector automatic background retry coordination state that informed cross-node idempotency hardening.
    /// </summary>
    public string AutomaticRetryCoordinationState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector retry-execution policy state that informed cross-node idempotency hardening.
    /// </summary>
    public string RetryExecutionPolicyState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-journal state that informed cross-node idempotency hardening.
    /// </summary>
    public string CommandJournalState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-journal durability state that informed cross-node idempotency hardening.
    /// </summary>
    public string CommandJournalDurabilityState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector distributed retry lease state that informed cross-node idempotency hardening.
    /// </summary>
    public string DistributedRetryLeaseState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates.NotApplicable;

    /// <summary>
    /// Gets the latest recorded managed-connector command-execution state visible to cross-node idempotency hardening.
    /// </summary>
    public string LatestCommandExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive cross-node idempotency hardening.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningSources.Unknown;

    /// <summary>
    /// Gets the deterministic command fingerprint currently associated with cross-node idempotency hardening.
    /// </summary>
    public string CommandFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic retry fingerprint currently associated with cross-node idempotency hardening.
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
    /// Gets the total number of command-execution outcomes currently visible to cross-node idempotency hardening.
    /// </summary>
    public int TotalRecordedEntryCount { get; init; }

    /// <summary>
    /// Gets the number of retained bounded journal entries currently visible to cross-node idempotency hardening.
    /// </summary>
    public int RetainedEntryCount { get; init; }

    /// <summary>
    /// Gets the number of retained command-execution entries that currently match the command lineage fingerprint.
    /// </summary>
    public int MatchingCommandLineageEntryCount { get; init; }

    /// <summary>
    /// Gets the number of retained automatic retry attempts currently visible for the runtime.
    /// </summary>
    public int AutomaticRetryAttemptCount { get; init; }

    /// <summary>
    /// Gets the number of retained automatic retry attempts that currently match the retry fingerprint.
    /// </summary>
    public int MatchingAutomaticRetryAttemptCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether a durable command-journal store is currently configured.
    /// </summary>
    public bool HasDurableStoreConfigured { get; init; }

    /// <summary>
    /// Gets a value indicating whether the durable command journal currently exposes persisted recorded history.
    /// </summary>
    public bool HasPersistedRecordedHistory { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current process recovered persisted command history for this runtime.
    /// </summary>
    public bool HasRecoveredPersistedHistory { get; init; }

    /// <summary>
    /// Gets a value indicating whether retained history currently contains evidence for the current retry fingerprint.
    /// </summary>
    public bool HasMatchingRetryFingerprintHistory { get; init; }

    /// <summary>
    /// Gets a value indicating whether retained history currently contains one automatic retry attempt for the current retry fingerprint.
    /// </summary>
    public bool HasMatchingAutomaticRetryAttempt { get; init; }

    /// <summary>
    /// Gets a value indicating whether retained history currently contains duplicated command lineage for the current retry posture.
    /// </summary>
    public bool HasDuplicateCommandLineage { get; init; }

    /// <summary>
    /// Gets a value indicating whether retained history currently contains duplicated automatic retry attempts for the current retry posture.
    /// </summary>
    public bool HasDuplicateAutomaticRetryAttempts { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current host coordination owner matches the active reporter identifier.
    /// </summary>
    public bool CoordinationOwnerMatchesActiveReporter { get; init; }

    /// <summary>
    /// Gets a value indicating whether the runtime currently exposes one active reporter lease.
    /// </summary>
    public bool HasActiveReporterLease =>
        !string.IsNullOrWhiteSpace(ActiveReporterId) &&
        ActiveReporterLeaseExpiresAtUtc.HasValue;

    /// <summary>
    /// Gets a value indicating whether the current node can execute automatic retry safely for the current cross-node hardening answer.
    /// </summary>
    public bool CanExecuteAutomaticRetryOnCurrentNode =>
        string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates.IdempotentSafe, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the number of active cross-node idempotency-hardening categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether cross-node idempotency hardening still remains operator-owned.
    /// </summary>
    public bool IsOperatorOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates.OperatorOnly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether cross-node idempotency currently looks safe for the current retry posture.
    /// </summary>
    public bool IsIdempotentSafe => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates.IdempotentSafe, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether cross-node idempotency currently remains risky because ownership truth still looks stale.
    /// </summary>
    public bool IsStaleOwnerRisk => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates.StaleOwnerRisk, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether cross-node idempotency currently remains risky because retained command lineage already looks duplicated.
    /// </summary>
    public bool IsDuplicateLineageRisk => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates.DuplicateLineageRisk, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether cross-node idempotency currently remains risky because the durable replay window still lacks enough retained evidence.
    /// </summary>
    public bool IsReplayWindowRisk => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningStates.ReplayWindowRisk, StringComparison.OrdinalIgnoreCase);
}
