using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

internal sealed class ManagedConnectorCommandExecutionHistoryStore
{
    internal const int MaximumRetainedEntryCount = 20;
    private readonly object syncRoot = new();
    private readonly Dictionary<string, ManagedConnectorCommandExecutionJournal> historyByRuntimeId =
        new(StringComparer.OrdinalIgnoreCase);

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
}
