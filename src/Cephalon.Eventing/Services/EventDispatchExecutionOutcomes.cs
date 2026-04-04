namespace Cephalon.Eventing.Services;

/// <summary>
/// Defines the stable outcome identifiers used when reporting durable event-dispatch activity.
/// </summary>
public static class EventDispatchExecutionOutcomes
{
    /// <summary>
    /// Gets the outcome identifier used when dispatch begins for one staged message.
    /// </summary>
    public const string Started = "started";

    /// <summary>
    /// Gets the outcome identifier used when dispatch completes successfully for one staged message.
    /// </summary>
    public const string Succeeded = "succeeded";

    /// <summary>
    /// Gets the outcome identifier used when dispatch fails for one staged message.
    /// </summary>
    public const string Failed = "failed";

    /// <summary>
    /// Gets the outcome identifier used when dispatch schedules or expects another retry attempt.
    /// </summary>
    public const string RetryScheduled = "retry-scheduled";

    /// <summary>
    /// Gets the outcome identifier used when dispatch intentionally skips one staged message.
    /// </summary>
    public const string Skipped = "skipped";
}
