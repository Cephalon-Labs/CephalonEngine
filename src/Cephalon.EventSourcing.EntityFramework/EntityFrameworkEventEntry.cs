using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cephalon.EventSourcing.EntityFramework;

/// <summary>
/// Represents one persisted domain event row stored by the Entity Framework event-store provider.
/// </summary>
[Table("CephalonEvents")]
public sealed class EntityFrameworkEventEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityFrameworkEventEntry" /> class.
    /// </summary>
    public EntityFrameworkEventEntry()
    {
    }

    /// <summary>
    /// Gets or sets the database-assigned row identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the stable logical stream identifier.
    /// </summary>
    [MaxLength(500)]
    public string StreamId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the zero-based optimistic stream version for the event.
    /// </summary>
    public long StreamVersion { get; set; }

    /// <summary>
    /// Gets or sets the stable Cephalon event-type registry name.
    /// </summary>
    [MaxLength(500)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized event payload.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC time at which the domain event occurred.
    /// </summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC time at which the event was appended to the store.
    /// </summary>
    public DateTime AppendedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier associated with the event when known.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier associated with the event when known.
    /// </summary>
    public string? TenantId { get; set; }
}
