using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Hosting;

namespace Cephalon.AspNetCore.Transports.Rest;

internal sealed class AspNetCoreRestEndpointOverrideRuntimeCatalog : IRestEndpointOverrideRuntimeCatalog
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private readonly RestEndpointOverrideDescriptor[] overrides;
    private readonly Dictionary<string, RestEndpointOverrideDescriptor> overridesById;
    private readonly Dictionary<string, IReadOnlyList<RestEndpointOverrideDescriptor>> overridesBySourceModule;
    private readonly Dictionary<string, IReadOnlyList<RestEndpointOverrideDescriptor>> overridesByBehaviorId;

    public AspNetCoreRestEndpointOverrideRuntimeCatalog(RestApiGovernanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        overrides = options.Overrides
            .Select(static item => new RestEndpointOverrideDescriptor(
                item.Id,
                item.BehaviorIds,
                item.SourceModuleIds,
                item.AuthoringStyles,
                item.ApiVersionMajors,
                item.Methods,
                item.RelativePatterns,
                item.RouteGroupPrefixes,
                item.ApiVersionMajor,
                item.Method,
                item.Pattern,
                item.RouteGroupPrefix,
                item.Bindings,
                item.BindingMode))
            .OrderBy(static item => item.Id, Comparer)
            .ToArray();

        overridesById = overrides.ToDictionary(static item => item.Id, Comparer);
        overridesBySourceModule = overrides
            .Where(static item => item.SourceModuleIds.Count > 0)
            .SelectMany(static item => item.SourceModuleIds.Select(sourceModuleId => new KeyValuePair<string, RestEndpointOverrideDescriptor>(sourceModuleId, item)))
            .GroupBy(static pair => pair.Key, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<RestEndpointOverrideDescriptor>)group.Select(static pair => pair.Value).ToArray(),
                Comparer);
        overridesByBehaviorId = overrides
            .Where(static item => item.BehaviorIds.Count > 0)
            .SelectMany(static item => item.BehaviorIds.Select(behaviorId => new KeyValuePair<string, RestEndpointOverrideDescriptor>(behaviorId, item)))
            .GroupBy(static pair => pair.Key, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<RestEndpointOverrideDescriptor>)group.Select(static pair => pair.Value).ToArray(),
                Comparer);
    }

    public IReadOnlyList<RestEndpointOverrideDescriptor> OverrideRules => overrides;

    public RestEndpointOverrideDescriptor? GetById(string overrideId)
    {
        if (string.IsNullOrWhiteSpace(overrideId))
        {
            return null;
        }

        return overridesById.TryGetValue(overrideId.Trim(), out var item)
            ? item
            : null;
    }

    public IReadOnlyList<RestEndpointOverrideDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return overridesBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<RestEndpointOverrideDescriptor> GetByBehaviorId(string behaviorId)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            return [];
        }

        return overridesByBehaviorId.TryGetValue(behaviorId.Trim(), out var matches)
            ? matches
            : [];
    }
}
