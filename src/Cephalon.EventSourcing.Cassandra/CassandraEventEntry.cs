namespace Cephalon.EventSourcing.Cassandra;

/// <summary>
/// Represents one persisted domain event row stored by the Cassandra event-store provider.
/// </summary>
public sealed class CassandraEventEntry
{
    /// <summary>Gets or sets the stable logical stream identifier.</summary>
    public string StreamId { get; set; } = string.Empty;

    /// <summary>Gets or sets the optimistic stream version for the event (1-based, monotonically increasing per stream).</summary>
    public long StreamVersion { get; set; }

    /// <summary>Gets or sets the assembly-qualified CLR event type name.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Gets or sets the <c>System.Text.Json</c>-serialized event payload.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC time at which the domain event occurred.</summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>Gets or sets the UTC time at which the event was appended to the store.</summary>
    public DateTime AppendedAtUtc { get; set; }
}
