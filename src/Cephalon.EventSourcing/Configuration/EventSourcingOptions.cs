namespace Cephalon.EventSourcing.Configuration;

/// <summary>
/// Describes the host-owned options for the runtime-neutral Cephalon event-sourcing pack.
/// </summary>
public sealed class EventSourcingOptions
{
    /// <summary>
    /// The configuration section that owns the host-level event-sourcing settings.
    /// </summary>
    public const string SectionName = "Engine:EventSourcing";

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSourcingOptions" /> class.
    /// </summary>
    public EventSourcingOptions()
    {
    }

    /// <summary>
    /// Gets or sets the default event-store provider identifier.
    /// </summary>
    public string DefaultProvider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether snapshot-aware paths are enabled.
    /// </summary>
    public bool EnableSnapshots { get; set; }
}
