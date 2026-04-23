namespace Cephalon.Data.Services;

internal sealed class ManagedConnectorCommandExecutionHistoryPersistenceDocument
{
    public DateTimeOffset? PersistedAtUtc { get; init; }

    public IReadOnlyList<ManagedConnectorCommandExecutionJournal> Journals { get; init; } = [];
}
