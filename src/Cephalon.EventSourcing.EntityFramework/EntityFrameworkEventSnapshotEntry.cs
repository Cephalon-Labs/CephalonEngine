using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cephalon.EventSourcing.EntityFramework;

/// <summary>
/// Represents one persisted aggregate snapshot row stored by the Entity Framework event-sourcing provider.
/// </summary>
[Table("CephalonEventSnapshots")]
public sealed class EntityFrameworkEventSnapshotEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityFrameworkEventSnapshotEntry" /> class.
    /// </summary>
    public EntityFrameworkEventSnapshotEntry()
    {
    }

    /// <summary>
    /// Gets or sets the database-assigned row identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the stable logical stream identifier represented by the snapshot.
    /// </summary>
    [MaxLength(500)]
    public string StreamId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the aggregate state type key represented by the snapshot payload.
    /// </summary>
    [MaxLength(1000)]
    public string StateType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the zero-based stream version represented by the snapshot.
    /// </summary>
    public long StreamVersion { get; set; }

    /// <summary>
    /// Gets or sets the serialized aggregate state payload.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC time at which the snapshot was saved.
    /// </summary>
    public DateTime SavedAtUtc { get; set; }
}
