namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector distributed retry lease answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseCategories
{
    /// <summary>
    /// The runtime can evaluate automatic retry on a single node without lease coordination.
    /// </summary>
    public const string SingleNodeRuntime = "single-node-runtime";

    /// <summary>
    /// The runtime depends on cross-node lease coordination before automatic retry should execute.
    /// </summary>
    public const string LeaseCoordinatedRuntime = "lease-coordinated-runtime";

    /// <summary>
    /// The current host declared a local coordination owner identifier for distributed retry.
    /// </summary>
    public const string CoordinationOwnerConfigured = "coordination-owner-configured";

    /// <summary>
    /// The current host did not declare a local coordination owner identifier for distributed retry.
    /// </summary>
    public const string CoordinationOwnerMissing = "coordination-owner-missing";

    /// <summary>
    /// The current host coordination owner matches the active reporter lease.
    /// </summary>
    public const string OwnerMatch = "owner-match";

    /// <summary>
    /// The current host coordination owner does not match the active reporter lease.
    /// </summary>
    public const string OwnerMismatch = "owner-mismatch";

    /// <summary>
    /// The current host currently holds the active retry lease.
    /// </summary>
    public const string LeaseHeld = "lease-held";

    /// <summary>
    /// No active retry lease is currently visible for automatic retry.
    /// </summary>
    public const string LeaseMissing = "lease-missing";

    /// <summary>
    /// Cross-node lease coordination remains conflicted.
    /// </summary>
    public const string LeaseConflict = "lease-conflict";

    /// <summary>
    /// A durable command-journal store is configured for the runtime.
    /// </summary>
    public const string DurableJournalConfigured = "durable-journal-configured";

    /// <summary>
    /// The durable command journal currently looks healthy for cross-node retry decisions.
    /// </summary>
    public const string DurableJournalHealthy = "durable-journal-healthy";

    /// <summary>
    /// The durable command journal currently exposes persisted recorded history.
    /// </summary>
    public const string PersistedHistory = "persisted-history";

    /// <summary>
    /// The durable command journal currently exposes recovered recorded history.
    /// </summary>
    public const string RecoveredHistory = "recovered-history";

    /// <summary>
    /// Automatic retry currently depends on in-memory command history only.
    /// </summary>
    public const string InMemoryJournalOnly = "in-memory-journal-only";

    /// <summary>
    /// The durable command journal currently reports a recovery or persistence failure.
    /// </summary>
    public const string JournalFailure = "journal-failure";

    /// <summary>
    /// The bounded command journal currently retains matching retry history.
    /// </summary>
    public const string RetryHistoryPresent = "retry-history-present";

    /// <summary>
    /// The bounded command journal currently retains one matching automatic retry attempt.
    /// </summary>
    public const string MatchingAutomaticRetryAttempt = "matching-automatic-retry-attempt";

    /// <summary>
    /// The bounded command journal currently retains multiple automatic retry attempts for the same retry fingerprint.
    /// </summary>
    public const string DuplicateAutomaticRetryAttempts = "duplicate-automatic-retry-attempts";

    /// <summary>
    /// The current host can execute automatic retry safely on this node.
    /// </summary>
    public const string CurrentNodeExecutable = "current-node-executable";

    /// <summary>
    /// Cross-node idempotency currently looks safe for automatic retry.
    /// </summary>
    public const string CrossNodeIdempotentSafe = "cross-node-idempotent-safe";

    /// <summary>
    /// Cross-node idempotency currently remains risky for automatic retry.
    /// </summary>
    public const string CrossNodeIdempotencyRisk = "cross-node-idempotency-risk";

    /// <summary>
    /// Distributed retry lease posture still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.OperatorOnly;
}
