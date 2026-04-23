using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

internal sealed record ManagedConnectorCommandExecutionJournal(
    string ExecutionRuntimeId,
    IReadOnlyList<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult> Entries,
    int TotalRecordedEntryCount,
    int MaximumRetainedEntryCount)
{
    public int RetainedEntryCount => Entries.Count;

    public bool HasRecordedEntries => TotalRecordedEntryCount > 0;

    public bool IsTruncated => TotalRecordedEntryCount > RetainedEntryCount;

    public CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult? LatestEntry =>
        Entries.Count > 0 ? Entries[0] : null;

    public CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult? OldestRetainedEntry =>
        Entries.Count > 0 ? Entries[^1] : null;

    public static ManagedConnectorCommandExecutionJournal Empty(string executionRuntimeId)
    {
        return new ManagedConnectorCommandExecutionJournal(
            executionRuntimeId,
            [],
            0,
            ManagedConnectorCommandExecutionHistoryStore.MaximumRetainedEntryCount);
    }
}
