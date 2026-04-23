namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector command-journal durability answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates
{
    /// <summary>
    /// Command-journal durability does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// The command journal currently remains in memory only and would not survive process restart.
    /// </summary>
    public const string InMemoryOnly = "in-memory-only";

    /// <summary>
    /// The command journal currently has a healthy durable persistence store.
    /// </summary>
    public const string Persisted = "persisted";

    /// <summary>
    /// The command journal was recovered from a durable persistence store after startup.
    /// </summary>
    public const string Recovered = "recovered";

    /// <summary>
    /// The durable command-journal store could not recover the persisted snapshot.
    /// </summary>
    public const string RecoveryFailed = "recovery-failed";

    /// <summary>
    /// The durable command-journal store could not persist the latest snapshot.
    /// </summary>
    public const string PersistenceFailed = "persistence-failed";
}
