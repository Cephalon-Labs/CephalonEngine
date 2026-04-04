namespace Cephalon.Eventing.Services;

/// <summary>
/// Describes one reported dispatch observation for a durable event publication path.
/// </summary>
public sealed class EventDispatchExecutionReport
{
    /// <summary>
    /// Creates a new dispatch observation report.
    /// </summary>
    /// <param name="outboxId">The stable outbox identifier that owns the dispatch path.</param>
    /// <param name="channelId">The stable channel identifier for the dispatched event.</param>
    /// <param name="outcome">The stable outcome identifier, such as <c>started</c> or <c>retry-scheduled</c>.</param>
    /// <param name="observedAtUtc">The UTC timestamp when the observation occurred.</param>
    /// <param name="messageId">The stable outbound message identifier when available.</param>
    /// <param name="attempt">The dispatch attempt number for this observation.</param>
    /// <param name="error">The operator-facing error summary when the observation represents a failure.</param>
    /// <param name="metadata">Optional operator-facing metadata captured alongside the observation.</param>
    public EventDispatchExecutionReport(
        string outboxId,
        string channelId,
        string outcome,
        DateTimeOffset observedAtUtc,
        string? messageId = null,
        int attempt = 1,
        string? error = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(outboxId))
        {
            throw new ArgumentException("Outbox id is required.", nameof(outboxId));
        }

        if (string.IsNullOrWhiteSpace(channelId))
        {
            throw new ArgumentException("Channel id is required.", nameof(channelId));
        }

        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(outcome));
        }

        if (attempt < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(attempt), attempt, "Attempt must be greater than or equal to 1.");
        }

        OutboxId = outboxId.Trim();
        ChannelId = channelId.Trim();
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
    /// Gets the stable outbox identifier that owns the dispatch path.
    /// </summary>
    public string OutboxId { get; }

    /// <summary>
    /// Gets the stable channel identifier for the dispatched event.
    /// </summary>
    public string ChannelId { get; }

    /// <summary>
    /// Gets the stable outcome identifier for the observed dispatch activity.
    /// </summary>
    public string Outcome { get; }

    /// <summary>
    /// Gets the UTC timestamp when the observation occurred.
    /// </summary>
    public DateTimeOffset ObservedAtUtc { get; }

    /// <summary>
    /// Gets the stable outbound message identifier when one was reported.
    /// </summary>
    public string? MessageId { get; }

    /// <summary>
    /// Gets the dispatch attempt number associated with this observation.
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
