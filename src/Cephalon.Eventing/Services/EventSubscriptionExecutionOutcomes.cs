namespace Cephalon.Eventing.Services;

/// <summary>
/// Defines the stable outcome identifiers used when reporting declared event-subscription activity.
/// </summary>
public static class EventSubscriptionExecutionOutcomes
{
    /// <summary>
    /// Gets the outcome identifier used when subscription handling begins for one message.
    /// </summary>
    public const string Started = "started";

    /// <summary>
    /// Gets the outcome identifier used when subscription handling completes successfully for one message.
    /// </summary>
    public const string Succeeded = "succeeded";

    /// <summary>
    /// Gets the outcome identifier used when subscription handling fails for one message.
    /// </summary>
    public const string Failed = "failed";

    /// <summary>
    /// Gets the outcome identifier used when subscription handling schedules or expects another retry attempt.
    /// </summary>
    public const string RetryScheduled = "retry-scheduled";

    /// <summary>
    /// Gets the outcome identifier used when subscription handling intentionally skips one message.
    /// </summary>
    public const string Skipped = "skipped";
}
