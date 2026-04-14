using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Hosting;

namespace Cephalon.AspNetCore.Transports.Rest;

internal sealed class AspNetCoreRestEndpointSuppressionRuntimeCatalog : IRestEndpointSuppressionRuntimeCatalog
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private readonly RestEndpointSuppressionDescriptor[] suppressions;
    private readonly Dictionary<string, RestEndpointSuppressionDescriptor> suppressionsById;
    private readonly Dictionary<string, IReadOnlyList<RestEndpointSuppressionDescriptor>> suppressionsBySourceModule;
    private readonly Dictionary<string, IReadOnlyList<RestEndpointSuppressionDescriptor>> suppressionsByBehaviorId;

    public AspNetCoreRestEndpointSuppressionRuntimeCatalog(RestApiGovernanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        suppressions = options.Suppressions
            .Select(static suppression => new RestEndpointSuppressionDescriptor(
                suppression.Id,
                suppression.BehaviorIds,
                suppression.SourceModuleIds,
                suppression.AuthoringStyles))
            .OrderBy(static suppression => suppression.Id, Comparer)
            .ToArray();

        suppressionsById = suppressions.ToDictionary(static suppression => suppression.Id, Comparer);
        suppressionsBySourceModule = suppressions
            .Where(static suppression => suppression.SourceModuleIds.Count > 0)
            .SelectMany(static suppression => suppression.SourceModuleIds.Select(sourceModuleId => new KeyValuePair<string, RestEndpointSuppressionDescriptor>(sourceModuleId, suppression)))
            .GroupBy(static pair => pair.Key, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<RestEndpointSuppressionDescriptor>)group.Select(static pair => pair.Value).ToArray(),
                Comparer);
        suppressionsByBehaviorId = suppressions
            .Where(static suppression => suppression.BehaviorIds.Count > 0)
            .SelectMany(static suppression => suppression.BehaviorIds.Select(behaviorId => new KeyValuePair<string, RestEndpointSuppressionDescriptor>(behaviorId, suppression)))
            .GroupBy(static pair => pair.Key, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<RestEndpointSuppressionDescriptor>)group.Select(static pair => pair.Value).ToArray(),
                Comparer);
    }

    public IReadOnlyList<RestEndpointSuppressionDescriptor> Suppressions => suppressions;

    public RestEndpointSuppressionDescriptor? GetById(string suppressionId)
    {
        if (string.IsNullOrWhiteSpace(suppressionId))
        {
            return null;
        }

        return suppressionsById.TryGetValue(suppressionId.Trim(), out var suppression)
            ? suppression
            : null;
    }

    public IReadOnlyList<RestEndpointSuppressionDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return suppressionsBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<RestEndpointSuppressionDescriptor> GetByBehaviorId(string behaviorId)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            return [];
        }

        return suppressionsByBehaviorId.TryGetValue(behaviorId.Trim(), out var matches)
            ? matches
            : [];
    }
}
