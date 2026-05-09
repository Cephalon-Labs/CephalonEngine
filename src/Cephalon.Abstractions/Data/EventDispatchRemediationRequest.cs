namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one host-agnostic request to remediate a staged event-dispatch path.
/// </summary>
public sealed class EventDispatchRemediationRequest
{
    /// <summary>
    /// Creates an event-dispatch remediation request.
    /// </summary>
    /// <param name="outboxId">The outbox identifier that owns the staged event.</param>
    /// <param name="messageId">The staged event message identifier.</param>
    /// <param name="channelId">The event channel identifier associated with the staged event.</param>
    /// <param name="operationId">The remediation operation identifier.</param>
    /// <param name="commandId">The stable command identifier. A generated identifier is used when omitted.</param>
    /// <param name="requestedAtUtc">The UTC timestamp when the command was requested. The current UTC time is used when omitted.</param>
    /// <param name="nextAttemptAtUtc">The UTC retry eligibility timestamp used by delayed retry commands.</param>
    /// <param name="reason">The operator-facing reason for the command.</param>
    /// <param name="actorId">The actor responsible for requesting the command.</param>
    /// <param name="correlationId">The correlation identifier associated with the command.</param>
    /// <param name="metadata">Optional operator-facing command metadata.</param>
    public EventDispatchRemediationRequest(
        string outboxId,
        string messageId,
        string channelId,
        string operationId,
        string? commandId = null,
        DateTimeOffset? requestedAtUtc = null,
        DateTimeOffset? nextAttemptAtUtc = null,
        string? reason = null,
        string? actorId = null,
        string? correlationId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(outboxId))
        {
            throw new ArgumentException("Outbox id is required.", nameof(outboxId));
        }

        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentException("Message id is required.", nameof(messageId));
        }

        if (string.IsNullOrWhiteSpace(channelId))
        {
            throw new ArgumentException("Channel id is required.", nameof(channelId));
        }

        if (string.IsNullOrWhiteSpace(operationId))
        {
            throw new ArgumentException("Operation id is required.", nameof(operationId));
        }

        OutboxId = outboxId.Trim();
        MessageId = messageId.Trim();
        ChannelId = channelId.Trim();
        OperationId = operationId.Trim();
        CommandId = string.IsNullOrWhiteSpace(commandId)
            ? $"event-dispatch-command-{Guid.NewGuid():N}"
            : commandId.Trim();
        RequestedAtUtc = requestedAtUtc.GetValueOrDefault(DateTimeOffset.UtcNow);
        if (RequestedAtUtc == default)
        {
            throw new ArgumentException("Command request time is required.", nameof(requestedAtUtc));
        }

        NextAttemptAtUtc = nextAttemptAtUtc;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        ActorId = string.IsNullOrWhiteSpace(actorId) ? null : actorId.Trim();
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        Metadata = CopyValues(metadata);
    }

    /// <summary>
    /// Gets the stable command identifier.
    /// </summary>
    public string CommandId { get; }

    /// <summary>
    /// Gets the outbox identifier that owns the staged event.
    /// </summary>
    public string OutboxId { get; }

    /// <summary>
    /// Gets the staged event message identifier.
    /// </summary>
    public string MessageId { get; }

    /// <summary>
    /// Gets the event channel identifier associated with the staged event.
    /// </summary>
    public string ChannelId { get; }

    /// <summary>
    /// Gets the remediation operation identifier.
    /// </summary>
    public string OperationId { get; }

    /// <summary>
    /// Gets the UTC timestamp when the command was requested.
    /// </summary>
    public DateTimeOffset RequestedAtUtc { get; }

    /// <summary>
    /// Gets the UTC retry eligibility timestamp used by delayed retry commands.
    /// </summary>
    public DateTimeOffset? NextAttemptAtUtc { get; }

    /// <summary>
    /// Gets the operator-facing reason for the command.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Gets the actor responsible for requesting the command.
    /// </summary>
    public string? ActorId { get; }

    /// <summary>
    /// Gets the correlation identifier associated with the command.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets optional operator-facing command metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static Dictionary<string, string> CopyValues(IReadOnlyDictionary<string, string>? values)
    {
        if (values is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return values
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(
                static pair => pair.Key.Trim(),
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }
}
