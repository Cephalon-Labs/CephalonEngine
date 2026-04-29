namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the latest aggregate operator-facing state reported for one durable event-dispatch runtime.
/// </summary>
public sealed class EventDispatchRuntimeSummary
{
    /// <summary>
    /// Gets an empty runtime summary when no dispatch observations have been reported yet.
    /// </summary>
    public static EventDispatchRuntimeSummary Empty { get; } = new();

    /// <summary>
    /// Creates a new aggregate runtime summary.
    /// </summary>
    /// <param name="reportedOutboxIds">The outbox identifiers that have reported state for the runtime.</param>
    /// <param name="lastOutboxId">The outbox identifier that produced the latest observation.</param>
    /// <param name="lastChannelId">The latest reported channel identifier.</param>
    /// <param name="lastOutcome">The latest reported dispatch outcome identifier.</param>
    /// <param name="lastObservedAtUtc">The UTC timestamp when the latest observation was reported.</param>
    /// <param name="lastMessageId">The latest outbound message identifier when one was reported.</param>
    /// <param name="lastAttempt">The latest reported dispatch attempt number.</param>
    /// <param name="startedCount">The total number of <c>started</c> observations reported so far.</param>
    /// <param name="succeededCount">The total number of <c>succeeded</c> observations reported so far.</param>
    /// <param name="failedCount">The total number of <c>failed</c> observations reported so far.</param>
    /// <param name="retryScheduledCount">The total number of <c>retry-scheduled</c> observations reported so far.</param>
    /// <param name="skippedCount">The total number of <c>skipped</c> observations reported so far.</param>
    /// <param name="retryPendingCount">The number of owned outboxes whose latest report still says another retry is pending.</param>
    /// <param name="lastError">The latest operator-facing error summary when one was reported.</param>
    /// <param name="terminalFailureCount">The total number of failed observations reported with terminal-failure posture.</param>
    /// <param name="terminalOutboxCount">The number of owned outboxes whose latest report marks the dispatch path as terminally failed.</param>
    public EventDispatchRuntimeSummary(
        IReadOnlyList<string>? reportedOutboxIds = null,
        string? lastOutboxId = null,
        string? lastChannelId = null,
        string? lastOutcome = null,
        DateTimeOffset? lastObservedAtUtc = null,
        string? lastMessageId = null,
        int lastAttempt = 0,
        int startedCount = 0,
        int succeededCount = 0,
        int failedCount = 0,
        int retryScheduledCount = 0,
        int skippedCount = 0,
        int retryPendingCount = 0,
        string? lastError = null,
        int terminalFailureCount = 0,
        int terminalOutboxCount = 0)
    {
        if (lastAttempt < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lastAttempt), lastAttempt, "Last attempt must be greater than or equal to 0.");
        }

        if (startedCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startedCount), startedCount, "Started count must be greater than or equal to 0.");
        }

        if (succeededCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(succeededCount), succeededCount, "Succeeded count must be greater than or equal to 0.");
        }

        if (failedCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(failedCount), failedCount, "Failed count must be greater than or equal to 0.");
        }

        if (retryScheduledCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retryScheduledCount), retryScheduledCount, "Retry-scheduled count must be greater than or equal to 0.");
        }

        if (skippedCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skippedCount), skippedCount, "Skipped count must be greater than or equal to 0.");
        }

        if (retryPendingCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retryPendingCount), retryPendingCount, "Retry-pending count must be greater than or equal to 0.");
        }

        if (terminalFailureCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(terminalFailureCount), terminalFailureCount, "Terminal-failure count must be greater than or equal to 0.");
        }

        if (terminalOutboxCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(terminalOutboxCount), terminalOutboxCount, "Terminal outbox count must be greater than or equal to 0.");
        }

        ReportedOutboxIds = Normalize(reportedOutboxIds);
        LastOutboxId = string.IsNullOrWhiteSpace(lastOutboxId) ? null : lastOutboxId.Trim();
        LastChannelId = string.IsNullOrWhiteSpace(lastChannelId) ? null : lastChannelId.Trim();
        LastOutcome = string.IsNullOrWhiteSpace(lastOutcome) ? null : lastOutcome.Trim();
        LastObservedAtUtc = lastObservedAtUtc;
        LastMessageId = string.IsNullOrWhiteSpace(lastMessageId) ? null : lastMessageId.Trim();
        LastAttempt = lastAttempt;
        StartedCount = startedCount;
        SucceededCount = succeededCount;
        FailedCount = failedCount;
        RetryScheduledCount = retryScheduledCount;
        SkippedCount = skippedCount;
        RetryPendingCount = retryPendingCount;
        LastError = string.IsNullOrWhiteSpace(lastError) ? null : lastError.Trim();
        TerminalFailureCount = terminalFailureCount;
        TerminalOutboxCount = terminalOutboxCount;
    }

    /// <summary>
    /// Gets the outbox identifiers that have reported runtime state for the dispatch runtime.
    /// </summary>
    public IReadOnlyList<string> ReportedOutboxIds { get; }

    /// <summary>
    /// Gets the outbox identifier that produced the latest observation when one exists.
    /// </summary>
    public string? LastOutboxId { get; }

    /// <summary>
    /// Gets the latest reported channel identifier when one exists.
    /// </summary>
    public string? LastChannelId { get; }

    /// <summary>
    /// Gets the latest reported dispatch outcome identifier when one exists.
    /// </summary>
    public string? LastOutcome { get; }

    /// <summary>
    /// Gets the UTC timestamp when the latest observation was reported.
    /// </summary>
    public DateTimeOffset? LastObservedAtUtc { get; }

    /// <summary>
    /// Gets the latest outbound message identifier when one was reported.
    /// </summary>
    public string? LastMessageId { get; }

    /// <summary>
    /// Gets the latest reported dispatch attempt number.
    /// </summary>
    public int LastAttempt { get; }

    /// <summary>
    /// Gets the total number of <c>started</c> observations reported so far.
    /// </summary>
    public int StartedCount { get; }

    /// <summary>
    /// Gets the total number of <c>succeeded</c> observations reported so far.
    /// </summary>
    public int SucceededCount { get; }

    /// <summary>
    /// Gets the total number of <c>failed</c> observations reported so far.
    /// </summary>
    public int FailedCount { get; }

    /// <summary>
    /// Gets the total number of <c>retry-scheduled</c> observations reported so far.
    /// </summary>
    public int RetryScheduledCount { get; }

    /// <summary>
    /// Gets the total number of <c>skipped</c> observations reported so far.
    /// </summary>
    public int SkippedCount { get; }

    /// <summary>
    /// Gets the number of owned outboxes whose latest report still says another retry is pending.
    /// </summary>
    public int RetryPendingCount { get; }

    /// <summary>
    /// Gets the total number of failed observations reported with terminal-failure posture.
    /// </summary>
    public int TerminalFailureCount { get; }

    /// <summary>
    /// Gets the number of owned outboxes whose latest report marks the dispatch path as terminally failed.
    /// </summary>
    public int TerminalOutboxCount { get; }

    /// <summary>
    /// Gets the latest operator-facing error summary when one was reported.
    /// </summary>
    public string? LastError { get; }

    /// <summary>
    /// Gets the number of outboxes that have reported runtime state for this dispatch runtime.
    /// </summary>
    public int ReportedOutboxCount => ReportedOutboxIds.Count;

    /// <summary>
    /// Gets the total number of reported observations across all owned outboxes.
    /// </summary>
    public int TotalReports => StartedCount + SucceededCount + FailedCount + RetryScheduledCount + SkippedCount;

    /// <summary>
    /// Gets a value indicating whether the dispatch runtime has reported any observations yet.
    /// </summary>
    public bool HasReports => TotalReports > 0;

    /// <summary>
    /// Gets a value indicating whether the dispatch runtime has reported any terminal failures.
    /// </summary>
    public bool HasTerminalFailures => TerminalFailureCount > 0 || TerminalOutboxCount > 0;

    private static string[] Normalize(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
