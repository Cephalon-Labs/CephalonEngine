namespace Cephalon.Abstractions.Features;

/// <summary>
/// Exposes the feature flags visible to the current runtime.
/// </summary>
public interface IFeatureFlagRuntimeCatalog
{
    /// <summary>
    /// Gets all feature flags visible to the current runtime.
    /// </summary>
    IReadOnlyList<FeatureFlagDescriptor> FeatureFlags { get; }

    /// <summary>
    /// Gets one feature flag by its stable identifier.
    /// </summary>
    /// <param name="featureFlagId">The feature-flag identifier to resolve.</param>
    /// <returns>The matching feature flag, or <see langword="null" /> when it is not active.</returns>
    FeatureFlagDescriptor? GetById(string featureFlagId);

    /// <summary>
    /// Gets all module-owned feature flags contributed by the requested source module.
    /// </summary>
    /// <param name="sourceModuleId">The source-module identifier to filter by.</param>
    /// <returns>The matching feature flags, or an empty list when none were contributed.</returns>
    IReadOnlyList<FeatureFlagDescriptor> GetBySourceModule(string sourceModuleId);

    /// <summary>
    /// Gets all feature flags that are enabled before targeting is applied.
    /// </summary>
    /// <returns>The enabled feature flags.</returns>
    IReadOnlyList<FeatureFlagDescriptor> GetEnabled();

    /// <summary>
    /// Gets all feature flags that are disabled before targeting is applied.
    /// </summary>
    /// <returns>The disabled feature flags.</returns>
    IReadOnlyList<FeatureFlagDescriptor> GetDisabled();
}
