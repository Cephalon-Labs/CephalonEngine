using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

internal sealed class ManagedConnectorCommandExecutionHistoryStore
{
    private const int MaxHistoryEntriesPerRuntime = 20;
    private readonly object syncRoot = new();
    private readonly Dictionary<string, CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult[]> historyByRuntimeId =
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
            historyByRuntimeId.TryGetValue(normalizedResult.ExecutionRuntimeId, out var existingHistory);
            existingHistory ??= [];

            var updatedHistory = new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult[
                Math.Min(existingHistory.Length + 1, MaxHistoryEntriesPerRuntime)];
            updatedHistory[0] = normalizedResult;

            for (var index = 0; index < updatedHistory.Length - 1; index++)
            {
                updatedHistory[index + 1] = existingHistory[index];
            }

            historyByRuntimeId[normalizedResult.ExecutionRuntimeId] = updatedHistory;
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
            return historyByRuntimeId.TryGetValue(executionRuntimeId.Trim(), out var history) && history.Length > 0
                ? history[0]
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
            return historyByRuntimeId.TryGetValue(executionRuntimeId.Trim(), out var history)
                ? [.. history]
                : [];
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
                ? CreateAttemptId(result.ExecutionRuntimeId, result.ExecutionFingerprint, normalizedRecordedAtUtc)
                : result.AttemptId.Trim(),
            RecordedAtUtc = normalizedRecordedAtUtc
        };
    }

    private static string CreateAttemptId(
        string executionRuntimeId,
        string executionFingerprint,
        DateTimeOffset recordedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionRuntimeId);

        return string.Join(
            "|",
            [
                "cephalon-managed-connector-command-execution-attempt/v1",
                $"runtime={executionRuntimeId.Trim()}",
                $"fingerprint={NormalizeFingerprintSegment(executionFingerprint)}",
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
