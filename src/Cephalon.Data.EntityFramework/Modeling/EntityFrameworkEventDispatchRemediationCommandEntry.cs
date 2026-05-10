namespace Cephalon.Data.EntityFramework.Modeling;

/// <summary>
/// Represents one durable event-dispatch remediation command result stored through Entity Framework Core.
/// </summary>
public sealed class EntityFrameworkEventDispatchRemediationCommandEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityFrameworkEventDispatchRemediationCommandEntry" /> class.
    /// </summary>
    public EntityFrameworkEventDispatchRemediationCommandEntry()
    {
    }

    /// <summary>
    /// Gets or sets the stable remediation command identifier.
    /// </summary>
    public string CommandId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the outbox identifier that owned the targeted staged event.
    /// </summary>
    public string OutboxId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the staged event message identifier targeted by the command.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event channel identifier associated with the targeted staged event.
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the remediation operation identifier requested by the operator.
    /// </summary>
    public string OperationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stable command outcome identifier.
    /// </summary>
    public string Outcome { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the dispatch-store outcome applied by the accepted command.
    /// </summary>
    public string DispatchOutcome { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when the command result was observed.
    /// </summary>
    public DateTimeOffset ObservedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the operator-facing error summary when the command was rejected.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the operator actor identifier when it was supplied with the command.
    /// </summary>
    public string? ActorId { get; set; }

    /// <summary>
    /// Gets or sets the operator correlation identifier when it was supplied with the command.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the operator-facing command reason when it was supplied.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the serialized command metadata payload.
    /// </summary>
    public string MetadataJson { get; set; } = "{}";
}
