using Cephalon.Abstractions.Features;

namespace Cephalon.Engine.Features;

internal sealed class FeatureFlagRuntimeCatalogSnapshot : IFeatureFlagRuntimeCatalog
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private readonly FeatureFlagDescriptor[] featureFlags;
    private readonly FeatureFlagDescriptor[] enabledFeatureFlags;
    private readonly FeatureFlagDescriptor[] disabledFeatureFlags;
    private readonly Dictionary<string, FeatureFlagDescriptor> featureFlagsById;
    private readonly Dictionary<string, IReadOnlyList<FeatureFlagDescriptor>> featureFlagsBySourceModule;

    public FeatureFlagRuntimeCatalogSnapshot(IEnumerable<FeatureFlagDescriptor> featureFlags)
    {
        ArgumentNullException.ThrowIfNull(featureFlags);

        this.featureFlags = featureFlags
            .OrderBy(static flag => flag.SourceKind)
            .ThenBy(static flag => flag.SourceModuleId, Comparer)
            .ThenBy(static flag => flag.Id, Comparer)
            .ToArray();

        ValidateDuplicateIds(this.featureFlags);

        enabledFeatureFlags = this.featureFlags
            .Where(static flag => flag.Enabled)
            .ToArray();
        disabledFeatureFlags = this.featureFlags
            .Where(static flag => !flag.Enabled)
            .ToArray();
        featureFlagsById = this.featureFlags.ToDictionary(static flag => flag.Id, Comparer);
        featureFlagsBySourceModule = this.featureFlags
            .Where(static flag => !string.IsNullOrWhiteSpace(flag.SourceModuleId))
            .GroupBy(static flag => flag.SourceModuleId!, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<FeatureFlagDescriptor>)group.ToArray(),
                Comparer);
    }

    public IReadOnlyList<FeatureFlagDescriptor> FeatureFlags => featureFlags;

    public FeatureFlagDescriptor? GetById(string featureFlagId)
    {
        if (string.IsNullOrWhiteSpace(featureFlagId))
        {
            return null;
        }

        return featureFlagsById.TryGetValue(featureFlagId.Trim(), out var featureFlag)
            ? featureFlag
            : null;
    }

    public IReadOnlyList<FeatureFlagDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return featureFlagsBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<FeatureFlagDescriptor> GetEnabled()
    {
        return enabledFeatureFlags;
    }

    public IReadOnlyList<FeatureFlagDescriptor> GetDisabled()
    {
        return disabledFeatureFlags;
    }

    private static void ValidateDuplicateIds(IReadOnlyList<FeatureFlagDescriptor> featureFlags)
    {
        var duplicateId = featureFlags
            .GroupBy(static flag => flag.Id, Comparer)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicateId is null)
        {
            return;
        }

        var owners = duplicateId
            .Select(static flag => flag.SourceModuleId ?? "host")
            .Distinct(Comparer)
            .OrderBy(static value => value, Comparer);

        throw new InvalidOperationException(
            $"Feature flag '{duplicateId.Key}' is registered multiple times by: {string.Join(", ", owners)}.");
    }
}
