namespace Cephalon.Eventing.Services;

/// <summary>
/// Describes one application-managed execution observation for a declared event subscription.
/// </summary>
public sealed class EventSubscriptionExecutionReport
{
    /// <summary>
    /// Creates a new execution report for a declared event subscription.
    /// </summary>
    /// <param name="subscriptionId">The stable declared subscription identifier.</param>
    /// <param name="outcome">The stable outcome identifier, such as <c>started</c> or <c>retry-scheduled</c>.</param>
    /// <param name="observedAtUtc">The UTC timestamp when the observation occurred.</param>
    /// <param name="messageId">The stable inbound message identifier when available.</param>
    /// <param name="attempt">The application-managed attempt number for this observation.</param>
    /// <param name="error">The operator-facing error summary when the observation represents a failure.</param>
    /// <param name="metadata">Optional operator-facing metadata captured alongside the observation.</param>
    public EventSubscriptionExecutionReport(
        string subscriptionId,
        string outcome,
        DateTimeOffset observedAtUtc,
        string? messageId = null,
        int attempt = 1,
        string? error = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            throw new ArgumentException("Subscription id is required.", nameof(subscriptionId));
        }

        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(outcome));
        }

        if (attempt < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(attempt), attempt, "Attempt must be greater than or equal to 1.");
        }

        SubscriptionId = subscriptionId.Trim();
        Outcome = outcome.Trim();
        ObservedAtUtc = observedAtUtc;
        MessageId = string.IsNullOrWhiteSpace(messageId) ? null : messageId.Trim();
        Attempt = attempt;
        Error = string.IsNullOrWhiteSpace(error) ? null : error.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable declared subscription identifier.
    /// </summary>
    public string SubscriptionId { get; }

    /// <summary>
    /// Gets the stable outcome identifier for the observed subscription activity.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets the UTC timestamp when the observation occurred.
    /// </summary>
    public DateTimeOffset ObservedAtUtc { get; }

    /// <summary>
    /// Gets the stable inbound message identifier when one was reported.
    /// </summary>
    public string? MessageId { get; }

    /// <summary>
    /// Gets the application-managed attempt number associated with this observation.
    /// </summary>
    public int Attempt { get; }

    /// <summary>
    /// Gets the operator-facing error summary when the observation represents a failure.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Gets optional operator-facing metadata captured alongside the observation.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
