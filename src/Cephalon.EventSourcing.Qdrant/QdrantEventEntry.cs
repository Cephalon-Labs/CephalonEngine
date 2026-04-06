namespace Cephalon.EventSourcing.Qdrant;

/// <summary>
/// Plain-object representation of a single event row stored in a Qdrant vector collection.
/// </summary>
public sealed class QdrantEventEntry
{
    /// <summary>The stream identifier this event belongs to.</summary>
    public string StreamId { get; init; } = string.Empty;

    /// <summary>The monotonically increasing version of this event within its stream.</summary>
    public long StreamVersion { get; init; }

    /// <summary>The assembly-qualified CLR type name of the serialized event.</summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>The JSON-serialized event payload.</summary>
    public string Payload { get; init; } = string.Empty;

    /// <summary>UTC timestamp when the domain event occurred.</summary>
    public DateTime OccurredAtUtc { get; init; }

    /// <summary>UTC timestamp when the event was appended to the store.</summary>
    public DateTime AppendedAtUtc { get; init; }

    /// <summary>Optional correlation identifier for causality tracking.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Optional tenant identifier for multi-tenancy scenarios.</summary>
    public string? TenantId { get; init; }
}
