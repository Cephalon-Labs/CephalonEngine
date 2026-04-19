using Cephalon.Abstractions.Features;

namespace Cephalon.Engine.Features;

internal sealed class FeatureFlagRegistryAdapter(
    string moduleId,
    List<FeatureFlagDescriptor> featureFlags) : IFeatureFlagRegistry
{
    public void Add(FeatureFlagDescriptor featureFlag)
    {
        ArgumentNullException.ThrowIfNull(featureFlag);

        if (featureFlag.SourceKind != FeatureFlagSourceKind.Module)
        {
            throw new InvalidOperationException(
                $"Feature flag '{featureFlag.Id}' must be declared as module-owned when contributed by module '{moduleId}'.");
        }

        if (!string.Equals(featureFlag.SourceModuleId, moduleId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Feature flag '{featureFlag.Id}' declared source module '{featureFlag.SourceModuleId}', but it was contributed by module '{moduleId}'.");
        }

        featureFlags.Add(featureFlag);
    }
}
