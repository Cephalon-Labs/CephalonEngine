namespace Cephalon.Behaviors.Configuration;

/// <summary>
/// Top-level behavior topology options read from the <c>Engine</c> configuration section.
/// </summary>
public sealed class BehaviorOptions
{
    /// <summary>
    /// Gets or sets the engine-level defaults applied to all behaviors.
    /// </summary>
    public BehaviorDefaultsOptions BehaviorDefaults { get; set; } = new();

    /// <summary>
    /// Gets or sets per-behavior topology overrides, keyed by behavior identifier.
    /// </summary>
    public Dictionary<string, BehaviorEntryOptions> Behaviors { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
