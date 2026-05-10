namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the bounded in-memory retention posture for event-dispatch remediation command history.
/// </summary>
public sealed class EventDispatchRemediationRuntimeRetention
{
    /// <summary>
    /// Gets an empty remediation command-retention state when no bounded command history is available.
    /// </summary>
    public static EventDispatchRemediationRuntimeRetention Empty { get; } = new();

    /// <summary>
    /// Creates a new remediation command-retention state.
    /// </summary>
    /// <param name="historyLimit">The configured maximum number of command results retained in memory.</param>
    /// <param name="retainedCommandCount">The number of command results currently retained in the bounded history.</param>
    /// <param name="totalRecordedCommandCount">The number of non-duplicate command results accepted into the bounded catalog since startup.</param>
    /// <param name="droppedCommandCount">The number of older command results dropped because the bounded history limit was exceeded.</param>
    /// <param name="truncated">A value indicating whether the retained history no longer contains every command result recorded since startup.</param>
    /// <param name="oldestRetainedCommandId">The oldest retained command identifier when one exists.</param>
    /// <param name="oldestRetainedObservedAtUtc">The UTC timestamp for the oldest retained command result when one exists.</param>
    /// <param name="latestRetainedCommandId">The newest retained command identifier when one exists.</param>
    /// <param name="latestRetainedObservedAtUtc">The UTC timestamp for the newest retained command result when one exists.</param>
    public EventDispatchRemediationRuntimeRetention(
        int historyLimit = 0,
        int retainedCommandCount = 0,
        long totalRecordedCommandCount = 0,
        long droppedCommandCount = 0,
        bool truncated = false,
        string? oldestRetainedCommandId = null,
        DateTimeOffset? oldestRetainedObservedAtUtc = null,
        string? latestRetainedCommandId = null,
        DateTimeOffset? latestRetainedObservedAtUtc = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(historyLimit);
        ArgumentOutOfRangeException.ThrowIfNegative(retainedCommandCount);
        ArgumentOutOfRangeException.ThrowIfNegative(totalRecordedCommandCount);
        ArgumentOutOfRangeException.ThrowIfNegative(droppedCommandCount);

        HistoryLimit = historyLimit;
        RetainedCommandCount = retainedCommandCount;
        TotalRecordedCommandCount = totalRecordedCommandCount;
        DroppedCommandCount = droppedCommandCount;
        Truncated = truncated;
        OldestRetainedCommandId = string.IsNullOrWhiteSpace(oldestRetainedCommandId)
            ? null
            : oldestRetainedCommandId.Trim();
        OldestRetainedObservedAtUtc = oldestRetainedObservedAtUtc;
        LatestRetainedCommandId = string.IsNullOrWhiteSpace(latestRetainedCommandId)
            ? null
            : latestRetainedCommandId.Trim();
        LatestRetainedObservedAtUtc = latestRetainedObservedAtUtc;
    }

    /// <summary>
    /// Gets the configured maximum number of command results retained in memory.
    /// </summary>
    public int HistoryLimit { get; }

    /// <summary>
    /// Gets the number of command results currently retained in the bounded history.
    /// </summary>
    public int RetainedCommandCount { get; }

    /// <summary>
    /// Gets the number of non-duplicate command results accepted into the bounded catalog since startup.
    /// </summary>
    public long TotalRecordedCommandCount { get; }

    /// <summary>
    /// Gets the number of older command results dropped because the bounded history limit was exceeded.
    /// </summary>
    public long DroppedCommandCount { get; }

    /// <summary>
    /// Gets a value indicating whether the retained history no longer contains every command result recorded since startup.
    /// </summary>
    public bool Truncated { get; }

    /// <summary>
    /// Gets the oldest retained command identifier when one exists.
    /// </summary>
    public string? OldestRetainedCommandId { get; }

    /// <summary>
    /// Gets the UTC timestamp for the oldest retained command result when one exists.
    /// </summary>
    public DateTimeOffset? OldestRetainedObservedAtUtc { get; }

    /// <summary>
    /// Gets the newest retained command identifier when one exists.
    /// </summary>
    public string? LatestRetainedCommandId { get; }

    /// <summary>
    /// Gets the UTC timestamp for the newest retained command result when one exists.
    /// </summary>
    public DateTimeOffset? LatestRetainedObservedAtUtc { get; }
}
