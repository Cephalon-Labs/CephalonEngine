namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector command-journal durability answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityCategories
{
    /// <summary>
    /// The command journal currently remains in memory only.
    /// </summary>
    public const string InMemoryOnly = "in-memory-only";

    /// <summary>
    /// A durable journal store is configured for the current host.
    /// </summary>
    public const string DurableStoreConfigured = "durable-store-configured";

    /// <summary>
    /// The durable journal store currently has a healthy persisted snapshot.
    /// </summary>
    public const string PersistedSnapshotAvailable = "persisted-snapshot-available";

    /// <summary>
    /// The current runtime has recorded command history inside the durable journal store.
    /// </summary>
    public const string PersistedRecordedHistory = "persisted-recorded-history";

    /// <summary>
    /// The current runtime recovered command history from the durable journal store after startup.
    /// </summary>
    public const string RecoveredHistory = "recovered-history";

    /// <summary>
    /// The durable journal store currently reports healthy persistence posture.
    /// </summary>
    public const string PersistenceHealthy = "persistence-healthy";

    /// <summary>
    /// The durable journal store currently reports a recovery error.
    /// </summary>
    public const string RecoveryError = "recovery-error";

    /// <summary>
    /// The durable journal store currently reports a persistence error.
    /// </summary>
    public const string PersistenceError = "persistence-error";

    /// <summary>
    /// The command journal currently exposes recorded command history for the runtime.
    /// </summary>
    public const string RecordedCommandHistory = "recorded-command-history";

    /// <summary>
    /// The command journal currently exposes truncated retained history.
    /// </summary>
    public const string TruncatedHistory = "truncated-history";

    /// <summary>
    /// Automatic background retry is currently eligible on the shared runtime surface.
    /// </summary>
    public const string AutomaticRetryEligible = "automatic-retry-eligible";

    /// <summary>
    /// Automatic background retry can currently execute on the active node.
    /// </summary>
    public const string CoordinationReady = "coordination-ready";
}
