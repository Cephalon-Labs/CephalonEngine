namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Describes the latest runtime state of process-local tenant-invitation delivery retry execution coordination.
/// </summary>
public sealed class TenantInvitationDeliveryRetryExecutionCoordinationSnapshot
{
    /// <summary>
    /// Creates a tenant-invitation delivery retry execution coordination snapshot.
    /// </summary>
    /// <param name="enabled">A value indicating whether process-local retry execution coordination is effectively enabled.</param>
    /// <param name="ownership">The retry execution coordination ownership mode.</param>
    /// <param name="scope">The coordination scope, such as process-local or none.</param>
    /// <param name="mode">The coordination mode, such as skip-overlap or disabled.</param>
    /// <param name="isRunning">A value indicating whether a coordinated retry pass is currently running.</param>
    /// <param name="attemptCount">The number of attempts to enter the coordinator.</param>
    /// <param name="acceptedCount">The number of coordinator attempts accepted for execution.</param>
    /// <param name="skippedCount">The number of coordinator attempts skipped because another pass was already running.</param>
    /// <param name="completedCount">The number of coordinated retry passes completed with a retry result.</param>
    /// <param name="failedCount">The number of coordinated retry passes that ended with an unhandled failure.</param>
    /// <param name="lastStartedAtUtc">The UTC timestamp when the latest accepted coordinated retry pass started.</param>
    /// <param name="lastCompletedAtUtc">The UTC timestamp when the latest coordinated retry pass completed or failed.</param>
    /// <param name="lastSkippedAtUtc">The UTC timestamp when the latest overlapping retry pass was skipped.</param>
    /// <param name="lastOutcome">The latest coordination or retry runner outcome.</param>
    /// <param name="lastError">The latest unhandled coordinated retry error message.</param>
    /// <param name="metadata">Optional runtime metadata.</param>
    public TenantInvitationDeliveryRetryExecutionCoordinationSnapshot(
        bool enabled,
        string ownership,
        string scope,
        string mode,
        bool isRunning,
        long attemptCount,
        long acceptedCount,
        long skippedCount,
        long completedCount,
        long failedCount,
        DateTimeOffset? lastStartedAtUtc,
        DateTimeOffset? lastCompletedAtUtc,
        DateTimeOffset? lastSkippedAtUtc,
        string? lastOutcome,
        string? lastError,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Enabled = enabled;
        Ownership = string.IsNullOrWhiteSpace(ownership) ? "application-managed" : ownership.Trim();
        Scope = string.IsNullOrWhiteSpace(scope) ? "none" : scope.Trim();
        Mode = string.IsNullOrWhiteSpace(mode) ? "disabled" : mode.Trim();
        IsRunning = isRunning;
        AttemptCount = Math.Max(0, attemptCount);
        AcceptedCount = Math.Max(0, acceptedCount);
        SkippedCount = Math.Max(0, skippedCount);
        CompletedCount = Math.Max(0, completedCount);
        FailedCount = Math.Max(0, failedCount);
        LastStartedAtUtc = lastStartedAtUtc;
        LastCompletedAtUtc = lastCompletedAtUtc;
        LastSkippedAtUtc = lastSkippedAtUtc;
        LastOutcome = string.IsNullOrWhiteSpace(lastOutcome) ? null : lastOutcome.Trim();
        LastError = string.IsNullOrWhiteSpace(lastError) ? null : lastError.Trim();
        Metadata = CopyMetadata(metadata);
    }

    /// <summary>
    /// Gets a value indicating whether process-local retry execution coordination is effectively enabled.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Gets the retry execution coordination ownership mode.
    /// </summary>
    public string Ownership { get; }

    /// <summary>
    /// Gets the coordination scope, such as process-local or none.
    /// </summary>
    public string Scope { get; }

    /// <summary>
    /// Gets the coordination mode, such as skip-overlap or disabled.
    /// </summary>
    public string Mode { get; }

    /// <summary>
    /// Gets a value indicating whether a coordinated retry pass is currently running.
    /// </summary>
    public bool IsRunning { get; }

    /// <summary>
    /// Gets the number of attempts to enter the coordinator.
    /// </summary>
    public long AttemptCount { get; }

    /// <summary>
    /// Gets the number of coordinator attempts accepted for execution.
    /// </summary>
    public long AcceptedCount { get; }

    /// <summary>
    /// Gets the number of coordinator attempts skipped because another pass was already running.
    /// </summary>
    public long SkippedCount { get; }

    /// <summary>
    /// Gets the number of coordinated retry passes completed with a retry result.
    /// </summary>
    public long CompletedCount { get; }

    /// <summary>
    /// Gets the number of coordinated retry passes that ended with an unhandled failure.
    /// </summary>
    public long FailedCount { get; }

    /// <summary>
    /// Gets the UTC timestamp when the latest accepted coordinated retry pass started.
    /// </summary>
    public DateTimeOffset? LastStartedAtUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp when the latest coordinated retry pass completed or failed.
    /// </summary>
    public DateTimeOffset? LastCompletedAtUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp when the latest overlapping retry pass was skipped.
    /// </summary>
    public DateTimeOffset? LastSkippedAtUtc { get; }

    /// <summary>
    /// Gets the latest coordination or retry runner outcome.
    /// </summary>
    public string? LastOutcome { get; }

    /// <summary>
    /// Gets the latest unhandled coordinated retry error message.
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
