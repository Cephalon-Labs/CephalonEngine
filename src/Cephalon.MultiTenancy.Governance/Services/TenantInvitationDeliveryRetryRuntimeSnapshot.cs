namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the latest runtime state of automatic tenant-invitation delivery retry scheduling.
/// </summary>
public sealed class TenantInvitationDeliveryRetryRuntimeSnapshot
{
    /// <summary>
    /// Creates a tenant-invitation delivery retry scheduling runtime snapshot.
    /// </summary>
    /// <param name="enabled">A value indicating whether automatic background retry scheduling is effectively enabled.</param>
    /// <param name="ownership">The automatic background retry scheduling ownership mode.</param>
    /// <param name="intervalSeconds">The effective background retry interval in seconds.</param>
    /// <param name="maxItems">The effective retry entry limit for one scheduled pass.</param>
    /// <param name="runOnStartup">A value indicating whether background retry scheduling runs once during hosted-service startup.</param>
    /// <param name="runCount">The number of background retry passes that reached a completed or failed terminal state.</param>
    /// <param name="successfulRunCount">The number of background retry passes that completed without an unhandled failure.</param>
    /// <param name="failedRunCount">The number of background retry passes that failed before producing a retry result.</param>
    /// <param name="lastStartedAtUtc">The UTC timestamp when the latest background retry pass started.</param>
    /// <param name="lastCompletedAtUtc">The UTC timestamp when the latest background retry pass completed or failed.</param>
    /// <param name="lastOutcome">The latest retry runner outcome.</param>
    /// <param name="lastAttemptedCount">The latest attempted retry-entry count.</param>
    /// <param name="lastDispatchedCount">The latest successfully dispatched retry-entry count.</param>
    /// <param name="lastFailedCount">The latest still-retryable failed retry-entry count.</param>
    /// <param name="lastExhaustedCount">The latest exhausted retry-entry count.</param>
    /// <param name="lastTerminalCount">The latest terminal retry-entry count.</param>
    /// <param name="lastRemainingPendingCount">The latest remaining pending retry-entry count.</param>
    /// <param name="lastError">The latest unhandled background retry error message.</param>
    /// <param name="metadata">Optional runtime metadata.</param>
    public TenantInvitationDeliveryRetryRuntimeSnapshot(
        bool enabled,
        string ownership,
        int intervalSeconds,
        int maxItems,
        bool runOnStartup,
        long runCount,
        long successfulRunCount,
        long failedRunCount,
        DateTimeOffset? lastStartedAtUtc,
        DateTimeOffset? lastCompletedAtUtc,
        string? lastOutcome,
        int lastAttemptedCount,
        int lastDispatchedCount,
        int lastFailedCount,
        int lastExhaustedCount,
        int lastTerminalCount,
        int lastRemainingPendingCount,
        string? lastError,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Enabled = enabled;
        Ownership = string.IsNullOrWhiteSpace(ownership) ? "application-managed" : ownership.Trim();
        IntervalSeconds = intervalSeconds;
        MaxItems = maxItems;
        RunOnStartup = runOnStartup;
        RunCount = runCount;
        SuccessfulRunCount = successfulRunCount;
        FailedRunCount = failedRunCount;
        LastStartedAtUtc = lastStartedAtUtc;
        LastCompletedAtUtc = lastCompletedAtUtc;
        LastOutcome = string.IsNullOrWhiteSpace(lastOutcome) ? null : lastOutcome.Trim();
        LastAttemptedCount = lastAttemptedCount;
        LastDispatchedCount = lastDispatchedCount;
        LastFailedCount = lastFailedCount;
        LastExhaustedCount = lastExhaustedCount;
        LastTerminalCount = lastTerminalCount;
        LastRemainingPendingCount = lastRemainingPendingCount;
        LastError = string.IsNullOrWhiteSpace(lastError) ? null : lastError.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets a value indicating whether automatic background retry scheduling is effectively enabled.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Gets the automatic background retry scheduling ownership mode.
    /// </summary>
    public string Ownership { get; }

    /// <summary>
    /// Gets the effective background retry interval in seconds.
    /// </summary>
    public int IntervalSeconds { get; }

    /// <summary>
    /// Gets the effective retry entry limit for one scheduled pass.
    /// </summary>
    public int MaxItems { get; }

    /// <summary>
    /// Gets a value indicating whether background retry scheduling runs once during hosted-service startup.
    /// </summary>
    public bool RunOnStartup { get; }

    /// <summary>
    /// Gets the number of background retry passes that reached a completed or failed terminal state.
    /// </summary>
    public long RunCount { get; }

    /// <summary>
    /// Gets the number of background retry passes that completed without an unhandled failure.
    /// </summary>
    public long SuccessfulRunCount { get; }

    /// <summary>
    /// Gets the number of background retry passes that failed before producing a retry result.
    /// </summary>
    public long FailedRunCount { get; }

    /// <summary>
    /// Gets the UTC timestamp when the latest background retry pass started.
    /// </summary>
    public DateTimeOffset? LastStartedAtUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp when the latest background retry pass completed or failed.
    /// </summary>
    public DateTimeOffset? LastCompletedAtUtc { get; }

    /// <summary>
    /// Gets the latest retry runner outcome.
    /// </summary>
    public string? LastOutcome { get; }

    /// <summary>
    /// Gets the latest attempted retry-entry count.
    /// </summary>
    public int LastAttemptedCount { get; }

    /// <summary>
    /// Gets the latest successfully dispatched retry-entry count.
    /// </summary>
    public int LastDispatchedCount { get; }

    /// <summary>
    /// Gets the latest still-retryable failed retry-entry count.
    /// </summary>
    public int LastFailedCount { get; }

    /// <summary>
    /// Gets the latest exhausted retry-entry count.
    /// </summary>
    public int LastExhaustedCount { get; }

    /// <summary>
    /// Gets the latest terminal retry-entry count.
    /// </summary>
    public int LastTerminalCount { get; }

    /// <summary>
    /// Gets the latest remaining pending retry-entry count.
    /// </summary>
    public int LastRemainingPendingCount { get; }

    /// <summary>
    /// Gets the latest unhandled background retry error message.
    /// </summary>
    public string? LastError { get; }

    /// <summary>
    /// Gets optional runtime metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static Dictionary<string, string> CopyMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return metadata
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(
                static pair => pair.Key.Trim(),
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }
}
