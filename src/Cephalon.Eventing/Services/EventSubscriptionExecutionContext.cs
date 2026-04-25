namespace Cephalon.Eventing.Services;

/// <summary>
/// Describes the host-agnostic execution context delivered to a managed event-subscription executor.
/// </summary>
public sealed class EventSubscriptionExecutionContext
{
    /// <summary>
    /// Creates a new managed event-subscription execution context.
    /// </summary>
    /// <param name="subscription">The declared subscription that is being executed.</param>
    /// <param name="publication">The staged publication being delivered to the subscription.</param>
    /// <param name="attempt">The current managed execution attempt.</param>
    /// <param name="metadata">Optional operator-facing metadata associated with the current execution attempt.</param>
    public EventSubscriptionExecutionContext(
        EventSubscriptionDescriptor subscription,
        EventPublication publication,
        int attempt = 1,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
        Publication = publication ?? throw new ArgumentNullException(nameof(publication));
        if (attempt < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(attempt), attempt, "Attempt must be greater than or equal to 1.");
        }

        Attempt = attempt;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the declared subscription that is currently being executed.
    /// </summary>
    public EventSubscriptionDescriptor Subscription { get; }

    /// <summary>
    /// Gets the staged publication being delivered to the managed subscription.
    /// </summary>
    public EventPublication Publication { get; }

    /// <summary>
    /// Gets the current managed execution attempt number.
    /// </summary>
    public int Attempt { get; }

    /// <summary>
    /// Gets operator-facing metadata associated with the current execution attempt.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
