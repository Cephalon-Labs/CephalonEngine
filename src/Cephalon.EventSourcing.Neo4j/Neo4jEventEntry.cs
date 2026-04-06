namespace Cephalon.EventSourcing.Neo4j;

/// <summary>
/// Represents the fields stored on each Neo4j <c>:Event</c> node by the event-store provider.
/// </summary>
public sealed class Neo4jEventEntry
{
    /// <summary>Gets or sets the stable logical stream identifier.</summary>
    public string StreamId { get; set; } = string.Empty;

    /// <summary>Gets or sets the optimistic stream version for the event.</summary>
    public long StreamVersion { get; set; }

    /// <summary>Gets or sets the assembly-qualified CLR event type name.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Gets or sets the <c>System.Text.Json</c>-serialized event payload.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Gets or sets the ISO 8601 UTC timestamp at which the domain event occurred.</summary>
    public string OccurredAtUtc { get; set; } = string.Empty;

    /// <summary>Gets or sets the ISO 8601 UTC timestamp at which the event was appended to the store.</summary>
    public string AppendedAtUtc { get; set; } = string.Empty;
}
