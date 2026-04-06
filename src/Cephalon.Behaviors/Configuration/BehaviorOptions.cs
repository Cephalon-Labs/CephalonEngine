namespace Cephalon.Behaviors.Configuration;

/// <summary>Configuration options for the Engine:BehaviorDefaults and Engine:Behaviors sections.</summary>
public sealed class BehaviorOptions
{
    /// <summary>The configuration section name for behavior defaults.</summary>
    public const string DefaultsSectionName = "Engine:BehaviorDefaults";

    /// <summary>The configuration section name for per-behavior entries.</summary>
    public const string SectionName = "Engine:Behaviors";

    /// <summary>Initializes a new instance of <see cref="BehaviorOptions"/>.</summary>
    public BehaviorOptions() { }

    /// <summary>Gets or sets the default pattern applied to all behaviors that do not specify one.</summary>
    public string Pattern { get; set; } = "direct";

    /// <summary>Gets or sets the default transport list applied to all behaviors that do not specify one.</summary>
    public List<string> Transport { get; set; } = [];
}

/// <summary>Per-behavior topology configuration entry.</summary>
public sealed class BehaviorConfigEntry
{
    /// <summary>Gets or sets the pattern override. Null or empty means inherit from defaults.</summary>
    public string? Pattern { get; set; }

    /// <summary>Gets or sets the transport override. Empty list means inherit from defaults.</summary>
    public List<string> Transport { get; set; } = [];

    /// <summary>Gets or sets a value indicating whether inbox is enabled.</summary>
    public bool InboxEnabled { get; set; }

    /// <summary>Gets or sets a value indicating whether outbox is enabled.</summary>
    public bool OutboxEnabled { get; set; }

    /// <summary>Gets or sets a value indicating whether event sourcing is enabled.</summary>
    public bool EventSourcingEnabled { get; set; }
}
