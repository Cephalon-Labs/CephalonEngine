namespace Cephalon.Behaviors.Configuration;

/// <summary>
/// Per-behavior topology settings read from the <c>Engine:Behaviors</c> configuration section.
/// </summary>
public sealed class BehaviorEntryOptions
{
    /// <summary>
    /// Gets or sets an optional interaction pattern override for this behavior.
    /// When <see langword="null" /> or empty, the engine-level default pattern is used.
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets the transport identifier overrides for this behavior.
    /// When non-empty, replaces the engine-level default transport list entirely.
    /// When empty, the engine-level defaults are inherited.
    /// </summary>
    public IReadOnlyList<string> Transport { get; set; } = [];
}
