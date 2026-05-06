using Cephalon.Data.Configuration;
using Cephalon.Abstractions.Data;
using System.Text.Json;

namespace Cephalon.Data.Services;

internal sealed class ManagedConnectorCommandExecutionHistoryStore
{
    internal const int MaximumRetainedEntryCount = 20;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly object syncRoot = new();
    private readonly Dictionary<string, ManagedConnectorCommandExecutionJournal> historyByRuntimeId =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeProvider timeProvider;
    private readonly string? persistencePath;
    private readonly HashSet<string> persistedRuntimeIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> recoveredRuntimeIds = new(StringComparer.OrdinalIgnoreCase);
    private DateTimeOffset? lastRecoveredAtUtc;
    private DateTimeOffset? lastPersistedAtUtc;
    private string? lastRecoveryError;
    private string? lastPersistenceError;
    private long version;

    internal long Version => System.Threading.Interlocked.Read(ref version);

    public ManagedConnectorCommandExecutionHistoryStore(
        DataRuntimeOptions? options = null,
        TimeProvider? timeProvider = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
        persistencePath = string.IsNullOrWhiteSpace(options?.ManagedConnectorCommandJournalPersistencePath)
            ? null
            : options!.ManagedConnectorCommandJournalPersistencePath!.Trim();

        if (!string.IsNullOrWhiteSpace(persistencePath))
        {
            LoadPersistedHistory();
        }
    }

    public CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult Record(
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult result,
        DateTimeOffset recordedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(result.ExecutionRuntimeId);

        var normalizedResult = Normalize(result, recordedAtUtc);
        lock (syncRoot)
        {
            historyByRuntimeId.TryGetValue(normalizedResult.ExecutionRuntimeId, out var existingJournal);
            existingJournal ??= ManagedConnectorCommandExecutionJournal.Empty(normalizedResult.ExecutionRuntimeId);
            var existingHistory = existingJournal.Entries;

            var updatedHistory = new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult[
                Math.Min(existingHistory.Count + 1, MaximumRetainedEntryCount)];
            updatedHistory[0] = normalizedResult;

            for (var index = 0; index < updatedHistory.Length - 1; index++)
            {
                updatedHistory[index + 1] = existingHistory[index];
            }

            historyByRuntimeId[normalizedResult.ExecutionRuntimeId] = new ManagedConnectorCommandExecutionJournal(
                normalizedResult.ExecutionRuntimeId,
                updatedHistory,
                existingJournal.TotalRecordedEntryCount + 1,
                MaximumRetainedEntryCount);

            if (!string.IsNullOrWhiteSpace(persistencePath))
            {
                PersistSnapshotLocked(timeProvider.GetUtcNow());
            }

            IncrementVersion();
        }

        return normalizedResult;
    }

    public CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult? GetLatest(string executionRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(executionRuntimeId))
        {
            return null;
        }

        lock (syncRoot)
        {
            return historyByRuntimeId.TryGetValue(executionRuntimeId.Trim(), out var journal) &&
                   journal.Entries.Count > 0
                ? journal.Entries[0]
                : null;
        }
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult> GetHistory(string executionRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(executionRuntimeId))
        {
            return [];
        }

        lock (syncRoot)
        {
            return historyByRuntimeId.TryGetValue(executionRuntimeId.Trim(), out var journal)
                ? [.. journal.Entries]
                : [];
        }
    }

    public ManagedConnectorCommandExecutionJournal GetJournal(string executionRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(executionRuntimeId))
        {
            return ManagedConnectorCommandExecutionJournal.Empty(string.Empty);
        }

        lock (syncRoot)
        {
            if (!historyByRuntimeId.TryGetValue(executionRuntimeId.Trim(), out var journal))
            {
                return ManagedConnectorCommandExecutionJournal.Empty(executionRuntimeId.Trim());
            }

            return journal with
            {
                Entries = [.. journal.Entries]
            };
        }
    }

    public ManagedConnectorCommandExecutionJournalDurabilitySnapshot GetDurabilitySnapshot(string executionRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(executionRuntimeId))
        {
            return ManagedConnectorCommandExecutionJournalDurabilitySnapshot.Empty(string.Empty);
        }

        var normalizedExecutionRuntimeId = executionRuntimeId.Trim();
        lock (syncRoot)
        {
            return new ManagedConnectorCommandExecutionJournalDurabilitySnapshot(
                normalizedExecutionRuntimeId,
                persistencePath,
                lastRecoveredAtUtc,
                lastPersistedAtUtc,
                lastRecoveryError,
                lastPersistenceError,
                HasPersistedSnapshot: lastPersistedAtUtc.HasValue,
                HasPersistedRecordedHistory: persistedRuntimeIds.Contains(normalizedExecutionRuntimeId),
                HasRecoveredPersistedHistory: recoveredRuntimeIds.Contains(normalizedExecutionRuntimeId));
        }
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult Normalize(
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult result,
        DateTimeOffset recordedAtUtc)
    {
        var normalizedRecordedAtUtc = result.RecordedAtUtc ?? recordedAtUtc;

        return result with
        {
            AttemptId = string.IsNullOrWhiteSpace(result.AttemptId)
                ? CreateAttemptId(result.ExecutionRuntimeId, result.ExecutionFingerprint, result.InvocationSourceId, normalizedRecordedAtUtc)
                : result.AttemptId.Trim(),
            RecordedAtUtc = normalizedRecordedAtUtc
        };
    }

    private static string CreateAttemptId(
        string executionRuntimeId,
        string executionFingerprint,
        string invocationSourceId,
        DateTimeOffset recordedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionRuntimeId);

        return string.Join(
            "|",
            [
                "cephalon-managed-connector-command-execution-attempt/v1",
                $"runtime={executionRuntimeId.Trim()}",
                $"fingerprint={NormalizeFingerprintSegment(executionFingerprint)}",
                $"invocationSource={NormalizeFingerprintSegment(invocationSourceId)}",
                $"recordedAt={recordedAtUtc:O}"
            ]);
    }

    private static string NormalizeFingerprintSegment(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "none"
            : value.Trim();
    }

    private void LoadPersistedHistory()
    {
        lock (syncRoot)
        {
            try
            {
                if (!File.Exists(persistencePath))
                {
                    PersistSnapshotLocked(timeProvider.GetUtcNow());
                    return;
                }

                var document = JsonSerializer.Deserialize<ManagedConnectorCommandExecutionHistoryPersistenceDocument>(
                    File.ReadAllText(persistencePath),
                    SerializerOptions);
                if (document is null)
                {
                    throw new InvalidOperationException("The managed-connector durable command journal file did not contain a valid persistence document.");
                }

                historyByRuntimeId.Clear();
                persistedRuntimeIds.Clear();
                recoveredRuntimeIds.Clear();

                foreach (var journal in document.Journals)
                {
                    if (journal is null || string.IsNullOrWhiteSpace(journal.ExecutionRuntimeId))
                    {
                        continue;
                    }

                    var normalizedExecutionRuntimeId = journal.ExecutionRuntimeId.Trim();
                    var entries = journal.Entries is null
                        ? []
                        : journal.Entries
                            .Where(static entry => entry is not null)
                            .ToArray();
                    var normalizedJournal = new ManagedConnectorCommandExecutionJournal(
                        normalizedExecutionRuntimeId,
                        entries,
                        journal.TotalRecordedEntryCount,
                        journal.MaximumRetainedEntryCount <= 0
                            ? MaximumRetainedEntryCount
                            : journal.MaximumRetainedEntryCount);

                    historyByRuntimeId[normalizedExecutionRuntimeId] = normalizedJournal;
                    persistedRuntimeIds.Add(normalizedExecutionRuntimeId);
                    recoveredRuntimeIds.Add(normalizedExecutionRuntimeId);
                }

                lastPersistedAtUtc = document.PersistedAtUtc;
                lastRecoveredAtUtc = timeProvider.GetUtcNow();
                lastRecoveryError = null;
                lastPersistenceError = null;
                IncrementVersion();
            }
            catch (Exception exception)
            {
                historyByRuntimeId.Clear();
                persistedRuntimeIds.Clear();
                recoveredRuntimeIds.Clear();
                lastPersistedAtUtc = null;
                lastRecoveredAtUtc = null;
                lastRecoveryError = exception.Message;
                IncrementVersion();
            }
        }
    }

    private void PersistSnapshotLocked(DateTimeOffset persistedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(persistencePath))
        {
            return;
        }

        var temporaryPath = $"{persistencePath}.tmp";
        try
        {
            var directoryPath = Path.GetDirectoryName(persistencePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var document = new ManagedConnectorCommandExecutionHistoryPersistenceDocument
            {
                PersistedAtUtc = persistedAtUtc,
                Journals = historyByRuntimeId.Values
                    .OrderBy(static journal => journal.ExecutionRuntimeId, StringComparer.OrdinalIgnoreCase)
                    .Select(static journal => journal with
                    {
                        Entries = [.. journal.Entries]
                    })
                    .ToArray()
            };
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(document, SerializerOptions));
            File.Move(temporaryPath, persistencePath, overwrite: true);

            persistedRuntimeIds.Clear();
            foreach (var executionRuntimeId in historyByRuntimeId.Keys)
            {
                persistedRuntimeIds.Add(executionRuntimeId);
            }

            lastPersistedAtUtc = persistedAtUtc;
            lastPersistenceError = null;
            lastRecoveryError = null;
            IncrementVersion();
        }
        catch (Exception exception)
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            lastPersistenceError = exception.Message;
            IncrementVersion();
        }
    }

    private void IncrementVersion()
    {
        System.Threading.Interlocked.Increment(ref version);
    }
}
