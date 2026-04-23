namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector command-journal durability answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilitySources
{
    /// <summary>
    /// The durability answer was derived primarily from the in-memory shared command history store.
    /// </summary>
    public const string InMemoryHistoryStore = "in-memory-history-store";

    /// <summary>
    /// The durability answer was derived primarily from a healthy durable journal store.
    /// </summary>
    public const string DurableJournalStore = "durable-journal-store";

    /// <summary>
    /// The durability answer was derived primarily from recovered durable journal history.
    /// </summary>
    public const string RecoveredDurableJournalStore = "recovered-durable-journal-store";

    /// <summary>
    /// The durability answer was derived primarily from a durable journal recovery failure.
    /// </summary>
    public const string RecoveryError = "recovery-error";

    /// <summary>
    /// The durability answer was derived primarily from a durable journal persistence failure.
    /// </summary>
    public const string PersistenceError = "persistence-error";

    /// <summary>
    /// The durability answer does not currently resolve to one specific source.
    /// </summary>
    public const string Unknown = "unknown";
}
