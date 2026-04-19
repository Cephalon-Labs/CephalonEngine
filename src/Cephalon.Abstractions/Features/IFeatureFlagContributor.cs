namespace Cephalon.Abstractions.Features;

/// <summary>
/// Allows a module to contribute feature flags to the active runtime.
/// </summary>
public interface IFeatureFlagContributor
{
    /// <summary>
    /// Registers the feature flags owned by the contributing module.
    /// </summary>
    /// <param name="registry">The registry that receives feature-flag descriptors.</param>
    void RegisterFeatureFlags(IFeatureFlagRegistry registry);
}
