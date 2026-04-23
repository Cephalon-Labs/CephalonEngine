namespace Cephalon.Data.Services;

internal sealed record ManagedConnectorCommandExecutionJournalDurabilitySnapshot(
    string ExecutionRuntimeId,
    string? PersistencePath,
    DateTimeOffset? LastRecoveredAtUtc,
    DateTimeOffset? LastPersistedAtUtc,
    string? LastRecoveryError,
    string? LastPersistenceError,
    bool HasPersistedSnapshot,
    bool HasPersistedRecordedHistory,
    bool HasRecoveredPersistedHistory)
{
    public bool HasDurableStoreConfigured => !string.IsNullOrWhiteSpace(PersistencePath);

    public bool HasRecoveryError => !string.IsNullOrWhiteSpace(LastRecoveryError);

    public bool HasPersistenceError => !string.IsNullOrWhiteSpace(LastPersistenceError);

    public static ManagedConnectorCommandExecutionJournalDurabilitySnapshot Empty(string executionRuntimeId)
    {
        return new ManagedConnectorCommandExecutionJournalDurabilitySnapshot(
            executionRuntimeId,
            PersistencePath: null,
            LastRecoveredAtUtc: null,
            LastPersistedAtUtc: null,
            LastRecoveryError: null,
            LastPersistenceError: null,
            HasPersistedSnapshot: false,
            HasPersistedRecordedHistory: false,
            HasRecoveredPersistedHistory: false);
    }
}
