namespace Cephalon.Abstractions.Features;

/// <summary>
/// Identifies who owns a feature flag visible to the active runtime.
/// </summary>
public enum FeatureFlagSourceKind
{
    /// <summary>
    /// Indicates the feature flag is host-owned and was configured directly by the app.
    /// </summary>
    Host = 0,

    /// <summary>
    /// Indicates the feature flag is module-owned and was contributed by a Cephalon module.
    /// </summary>
    Module = 1
}
