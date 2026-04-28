namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the latest runtime state of tenant-domain ownership proof polling.
/// </summary>
public sealed class TenantDomainOwnershipProofPollingRuntimeSnapshot
{
    /// <summary>
    /// Creates a tenant-domain ownership proof polling runtime snapshot.
    /// </summary>
    /// <param name="enabled">A value indicating whether automatic background proof polling is effectively enabled.</param>
    /// <param name="ownership">The automatic background proof polling ownership mode.</param>
    /// <param name="intervalSeconds">The effective background polling interval in seconds.</param>
    /// <param name="batchLimit">The effective proof polling batch limit.</param>
    /// <param name="runOnStartup">A value indicating whether background proof polling runs once during hosted-service startup.</param>
    /// <param name="dnsTxtResolverConfigured">A value indicating whether DNS TXT proof collection has an explicit resolver endpoint.</param>
    /// <param name="runCount">The number of background polling passes that reached a completed or failed terminal state.</param>
    /// <param name="successfulRunCount">The number of background polling passes that completed without an unhandled failure.</param>
    /// <param name="failedRunCount">The number of background polling passes that failed before producing a polling result.</param>
    /// <param name="lastStartedAtUtc">The UTC timestamp when the latest background polling pass started.</param>
    /// <param name="lastCompletedAtUtc">The UTC timestamp when the latest background polling pass completed or failed.</param>
    /// <param name="lastOutcome">The latest proof polling outcome.</param>
    /// <param name="lastReason">The latest operator-facing proof polling reason.</param>
    /// <param name="lastCandidateCount">The latest candidate count.</param>
    /// <param name="lastVerificationCount">The latest verification-attempt count.</param>
    /// <param name="lastVerifiedCount">The latest verified count.</param>
    /// <param name="lastRejectedCount">The latest rejected count.</param>
    /// <param name="lastFailedCount">The latest failed-attempt count.</param>
    /// <param name="lastError">The latest unhandled background polling error message.</param>
    /// <param name="metadata">Optional runtime metadata.</param>
    public TenantDomainOwnershipProofPollingRuntimeSnapshot(
        bool enabled,
        string ownership,
        int intervalSeconds,
        int batchLimit,
        bool runOnStartup,
        bool dnsTxtResolverConfigured,
        long runCount,
        long successfulRunCount,
        long failedRunCount,
        DateTimeOffset? lastStartedAtUtc,
        DateTimeOffset? lastCompletedAtUtc,
        string? lastOutcome,
        string? lastReason,
        int lastCandidateCount,
        int lastVerificationCount,
        int lastVerifiedCount,
        int lastRejectedCount,
        int lastFailedCount,
        string? lastError,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Enabled = enabled;
        Ownership = string.IsNullOrWhiteSpace(ownership) ? "application-managed" : ownership.Trim();
        IntervalSeconds = intervalSeconds;
        BatchLimit = batchLimit;
        RunOnStartup = runOnStartup;
        DnsTxtResolverConfigured = dnsTxtResolverConfigured;
        RunCount = runCount;
        SuccessfulRunCount = successfulRunCount;
        FailedRunCount = failedRunCount;
        LastStartedAtUtc = lastStartedAtUtc;
        LastCompletedAtUtc = lastCompletedAtUtc;
        LastOutcome = string.IsNullOrWhiteSpace(lastOutcome) ? null : lastOutcome.Trim();
        LastReason = string.IsNullOrWhiteSpace(lastReason) ? null : lastReason.Trim();
        LastCandidateCount = lastCandidateCount;
        LastVerificationCount = lastVerificationCount;
        LastVerifiedCount = lastVerifiedCount;
        LastRejectedCount = lastRejectedCount;
        LastFailedCount = lastFailedCount;
        LastError = string.IsNullOrWhiteSpace(lastError) ? null : lastError.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets a value indicating whether automatic background proof polling is effectively enabled.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Gets the automatic background proof polling ownership mode.
    /// </summary>
    public string Ownership { get; }

    /// <summary>
    /// Gets the effective background polling interval in seconds.
    /// </summary>
    public int IntervalSeconds { get; }

    /// <summary>
    /// Gets the effective proof polling batch limit.
    /// </summary>
    public int BatchLimit { get; }

    /// <summary>
    /// Gets a value indicating whether background proof polling runs once during hosted-service startup.
    /// </summary>
    public bool RunOnStartup { get; }

    /// <summary>
    /// Gets a value indicating whether DNS TXT proof collection has an explicit resolver endpoint.
    /// </summary>
    public bool DnsTxtResolverConfigured { get; }

    /// <summary>
    /// Gets the number of background polling passes that reached a completed or failed terminal state.
    /// </summary>
    public long RunCount { get; }

    /// <summary>
    /// Gets the number of background polling passes that completed without an unhandled failure.
    /// </summary>
    public long SuccessfulRunCount { get; }

    /// <summary>
    /// Gets the number of background polling passes that failed before producing a polling result.
    /// </summary>
    public long FailedRunCount { get; }

    /// <summary>
    /// Gets the UTC timestamp when the latest background polling pass started.
    /// </summary>
    public DateTimeOffset? LastStartedAtUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp when the latest background polling pass completed or failed.
    /// </summary>
    public DateTimeOffset? LastCompletedAtUtc { get; }

    /// <summary>
    /// Gets the latest proof polling outcome.
    /// </summary>
    public string? LastOutcome { get; }

    /// <summary>
    /// Gets the latest operator-facing proof polling reason.
    /// </summary>
    public string? LastReason { get; }

    /// <summary>
    /// Gets the latest candidate count.
    /// </summary>
    public int LastCandidateCount { get; }

    /// <summary>
    /// Gets the latest verification-attempt count.
    /// </summary>
    public int LastVerificationCount { get; }

    /// <summary>
    /// Gets the latest verified count.
    /// </summary>
    public int LastVerifiedCount { get; }

    /// <summary>
    /// Gets the latest rejected count.
    /// </summary>
    public int LastRejectedCount { get; }

    /// <summary>
    /// Gets the latest failed-attempt count.
    /// </summary>
    public int LastFailedCount { get; }

    /// <summary>
    /// Gets the latest unhandled background polling error message.
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
