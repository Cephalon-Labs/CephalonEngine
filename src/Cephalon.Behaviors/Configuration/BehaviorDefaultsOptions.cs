namespace Cephalon.Behaviors.Configuration;

/// <summary>
/// Engine-level default topology settings applied to all behaviors
/// that do not have an explicit per-behavior configuration entry.
/// Corresponds to the <c>Engine:BehaviorDefaults</c> configuration section.
/// </summary>
public sealed class BehaviorDefaultsOptions
{
    /// <summary>
    /// Gets or sets the default interaction pattern applied when no per-behavior pattern is specified.
    /// Defaults to <c>direct</c>.
    /// </summary>
    public string Pattern { get; set; } = "direct";

    /// <summary>
    /// Gets or sets the default transport identifiers applied when no per-behavior transport is specified.
    /// Defaults to an empty list (no transport exposure).
    /// </summary>
    public IReadOnlyList<string> Transport { get; set; } = [];
}
