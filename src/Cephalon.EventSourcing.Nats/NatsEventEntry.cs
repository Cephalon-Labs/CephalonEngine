namespace Cephalon.EventSourcing.Nats;

/// <summary>
/// Plain-object representation of a single event stored in a NATS JetStream KV entry.
/// </summary>
public sealed class NatsEventEntry
{
    /// <summary>The stream identifier this event belongs to.</summary>
    public string StreamId { get; init; } = string.Empty;

    /// <summary>The zero-based, monotonically increasing version of this event within its stream.</summary>
    public long StreamVersion { get; init; }

    /// <summary>The stable Cephalon event-type registry name of the serialized event.</summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>The JSON-serialized event payload.</summary>
    public string Payload { get; init; } = string.Empty;

    /// <summary>UTC timestamp when the domain event occurred.</summary>
    public DateTime OccurredAtUtc { get; init; }

    /// <summary>UTC timestamp when the event was appended to the store.</summary>
    public DateTime AppendedAtUtc { get; init; }
}
