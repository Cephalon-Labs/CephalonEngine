namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector cross-node idempotency-hardening answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCrossNodeIdempotencyHardeningCategories
{
    /// <summary>
    /// The runtime can evaluate automatic retry on a single node without cross-node hardening.
    /// </summary>
    public const string SingleNodeRuntime = "single-node-runtime";

    /// <summary>
    /// The runtime depends on cross-node idempotency evidence before automatic retry should execute.
    /// </summary>
    public const string CrossNodeRuntime = "cross-node-runtime";

    /// <summary>
    /// Cross-node idempotency hardening still remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.OperatorOnly;

    /// <summary>
    /// The current host coordination owner matches the active reporter lease.
    /// </summary>
    public const string OwnerMatch = "owner-match";

    /// <summary>
    /// The current host coordination owner does not match the active reporter lease.
    /// </summary>
    public const string OwnerMismatch = "owner-mismatch";

    /// <summary>
    /// The current runtime still exposes one active reporter identifier.
    /// </summary>
    public const string ActiveReporterVisible = "active-reporter-visible";

    /// <summary>
    /// The current runtime still exposes one active reporter lease.
    /// </summary>
    public const string ActiveLeaseVisible = "active-lease-visible";

    /// <summary>
    /// Cross-node retry lease ownership remains missing.
    /// </summary>
    public const string LeaseMissing = "lease-missing";

    /// <summary>
    /// Cross-node retry lease ownership remains conflicted.
    /// </summary>
    public const string LeaseConflict = "lease-conflict";

    /// <summary>
    /// A durable command-journal store is configured for the runtime.
    /// </summary>
    public const string DurableJournalConfigured = "durable-journal-configured";

    /// <summary>
    /// Automatic retry still depends on in-memory command history only.
    /// </summary>
    public const string InMemoryJournalOnly = "in-memory-journal-only";

    /// <summary>
    /// The durable command journal currently exposes persisted recorded history.
    /// </summary>
    public const string PersistedHistory = "persisted-history";

    /// <summary>
    /// The durable command journal currently exposes recovered recorded history.
    /// </summary>
    public const string RecoveredHistory = "recovered-history";

    /// <summary>
    /// Retained history currently contains evidence for the current retry fingerprint.
    /// </summary>
    public const string MatchingRetryHistory = "matching-retry-history";

    /// <summary>
    /// Retained history currently contains one automatic retry attempt for the current retry fingerprint.
    /// </summary>
    public const string MatchingAutomaticRetryAttempt = "matching-automatic-retry-attempt";

    /// <summary>
    /// Retained history currently contains duplicated command lineage for the current retry posture.
    /// </summary>
    public const string DuplicateCommandLineage = "duplicate-command-lineage";

    /// <summary>
    /// Retained history currently contains duplicated automatic retry attempts for the current retry posture.
    /// </summary>
    public const string DuplicateAutomaticRetryAttempts = "duplicate-automatic-retry-attempts";

    /// <summary>
    /// Cross-node idempotency currently looks safe for the current retry posture.
    /// </summary>
    public const string IdempotentSafe = "idempotent-safe";

    /// <summary>
    /// Cross-node idempotency currently remains risky because ownership truth still looks stale.
    /// </summary>
    public const string StaleOwnerRisk = "stale-owner-risk";

    /// <summary>
    /// Cross-node idempotency currently remains risky because retained command lineage already looks duplicated.
    /// </summary>
    public const string DuplicateLineageRisk = "duplicate-lineage-risk";

    /// <summary>
    /// Cross-node idempotency currently remains risky because the durable replay window still lacks enough retained evidence.
    /// </summary>
    public const string ReplayWindowRisk = "replay-window-risk";

    /// <summary>
    /// The current node can execute automatic retry safely for the current cross-node hardening answer.
    /// </summary>
    public const string CurrentNodeExecutable = "current-node-executable";
}
