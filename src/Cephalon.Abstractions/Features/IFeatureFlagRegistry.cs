namespace Cephalon.Abstractions.Features;

/// <summary>
/// Collects feature flags contributed to the active runtime.
/// </summary>
public interface IFeatureFlagRegistry
{
    /// <summary>
    /// Adds a feature-flag descriptor to the current runtime composition.
    /// </summary>
    /// <param name="featureFlag">The feature-flag descriptor to register.</param>
    void Add(FeatureFlagDescriptor featureFlag);
}
