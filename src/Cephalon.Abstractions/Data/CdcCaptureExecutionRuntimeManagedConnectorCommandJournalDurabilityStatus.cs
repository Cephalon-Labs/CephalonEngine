namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector command-journal durability posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStatus
{
    /// <summary>
    /// Creates a new managed-connector command-journal durability answer.
    /// </summary>
    /// <param name="state">
    /// The stable command-journal durability state, such as <c>not-applicable</c>, <c>in-memory-only</c>, <c>persisted</c>, <c>recovered</c>, <c>recovery-failed</c>, or <c>persistence-failed</c>.
    /// </param>
    /// <param name="description">An optional operator-facing command-journal durability summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector command-journal durability state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector command-journal durability state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing command-journal durability summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable command-journal durability categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with command-journal durability.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with command-journal durability.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the operator-facing execution-topology classification that informed command-journal durability.
    /// </summary>
    public string ExecutionTopology { get; init; } = "not-configured";

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the current bounded managed-connector command-journal state that informed durability.
    /// </summary>
    public string CommandJournalState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector automatic background retry execution state that informed durability.
    /// </summary>
    public string AutomaticRetryExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector automatic background retry coordination state that informed durability.
    /// </summary>
    public string AutomaticRetryCoordinationState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.NotApplicable;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive command-journal durability.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilitySources.Unknown;

    /// <summary>
    /// Gets the configured durable persistence path when one exists.
    /// </summary>
    public string? PersistencePath { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the current process last recovered the durable journal snapshot, when recovery happened.
    /// </summary>
    public DateTimeOffset? LastRecoveredAtUtc { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the durable journal snapshot was last persisted successfully.
    /// </summary>
    public DateTimeOffset? LastPersistedAtUtc { get; init; }

    /// <summary>
    /// Gets the latest durable journal recovery error when one exists.
    /// </summary>
    public string? LastRecoveryError { get; init; }

    /// <summary>
    /// Gets the latest durable journal persistence error when one exists.
    /// </summary>
    public string? LastPersistenceError { get; init; }

    /// <summary>
    /// Gets the total number of command-execution outcomes currently visible to the durability answer.
    /// </summary>
    public int TotalRecordedEntryCount { get; init; }

    /// <summary>
    /// Gets the number of bounded journal entries currently retained for the execution runtime.
    /// </summary>
    public int RetainedEntryCount { get; init; }

    /// <summary>
    /// Gets the maximum number of bounded journal entries retained for one execution runtime.
    /// </summary>
    public int MaximumRetainedEntryCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether a durable journal store is currently configured.
    /// </summary>
    public bool HasDurableStoreConfigured { get; init; }

    /// <summary>
    /// Gets a value indicating whether the durable journal store currently has a persisted snapshot.
    /// </summary>
    public bool HasPersistedSnapshot { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current runtime has recorded command history in the persisted snapshot.
    /// </summary>
    public bool HasPersistedRecordedHistory { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current process recovered command history for this runtime from durable storage.
    /// </summary>
    public bool HasRecoveredPersistedHistory { get; init; }

    /// <summary>
    /// Gets a value indicating whether the durable journal store currently reports a recovery error.
    /// </summary>
    public bool HasRecoveryError => !string.IsNullOrWhiteSpace(LastRecoveryError);

    /// <summary>
    /// Gets a value indicating whether the durable journal store currently reports a persistence error.
    /// </summary>
    public bool HasPersistenceError => !string.IsNullOrWhiteSpace(LastPersistenceError);

    /// <summary>
    /// Gets a value indicating whether the current runtime currently retains recorded command history.
    /// </summary>
    public bool HasRecordedCommandHistory => TotalRecordedEntryCount > 0;

    /// <summary>
    /// Gets a value indicating whether the retained history currently represents bounded truncation.
    /// </summary>
    public bool HasTruncatedHistory => TotalRecordedEntryCount > RetainedEntryCount;

    /// <summary>
    /// Gets a value indicating whether the current runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the current durability answer remains in memory only.
    /// </summary>
    public bool IsInMemoryOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.InMemoryOnly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the current durability answer reports healthy persisted storage.
    /// </summary>
    public bool IsPersisted => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.Persisted, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the current durability answer reports recovered persisted history.
    /// </summary>
    public bool IsRecovered => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.Recovered, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the durable journal store currently failed while recovering persisted history.
    /// </summary>
    public bool IsRecoveryFailed => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.RecoveryFailed, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the durable journal store currently failed while persisting the latest snapshot.
    /// </summary>
    public bool IsPersistenceFailed => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalDurabilityStates.PersistenceFailed, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the current durability answer already provides restart-safe retained history.
    /// </summary>
    public bool IsDurable => IsPersisted || IsRecovered;
}
