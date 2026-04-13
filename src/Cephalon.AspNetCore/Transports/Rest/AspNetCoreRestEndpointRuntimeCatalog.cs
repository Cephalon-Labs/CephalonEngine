using Cephalon.Abstractions.Transports;

namespace Cephalon.AspNetCore.Transports.Rest;

internal sealed class AspNetCoreRestEndpointRuntimeCatalog : IRestEndpointRuntimeCatalog, IRestEndpointRuntimeRegistry
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private RestEndpointRuntimeDescriptor[] endpoints = [];
    private Dictionary<string, RestEndpointRuntimeDescriptor> endpointsById = new(Comparer);
    private Dictionary<string, RestEndpointRuntimeDescriptor> endpointsByRouteIdentity = new(Comparer);
    private Dictionary<string, IReadOnlyList<RestEndpointRuntimeDescriptor>> endpointsBySourceModule = new(Comparer);
    private Dictionary<string, IReadOnlyList<RestEndpointRuntimeDescriptor>> endpointsByBehaviorId = new(Comparer);

    public IReadOnlyList<RestEndpointRuntimeDescriptor> Endpoints => endpoints;

    public RestEndpointRuntimeDescriptor? GetById(string restEndpointId)
    {
        if (string.IsNullOrWhiteSpace(restEndpointId))
        {
            return null;
        }

        return endpointsById.TryGetValue(restEndpointId.Trim(), out var endpoint)
            ? endpoint
            : null;
    }

    public IReadOnlyList<RestEndpointRuntimeDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return endpointsBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<RestEndpointRuntimeDescriptor> GetByBehaviorId(string behaviorId)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            return [];
        }

        return endpointsByBehaviorId.TryGetValue(behaviorId.Trim(), out var matches)
            ? matches
            : [];
    }

    public void Clear()
    {
        endpoints = [];
        endpointsById = new Dictionary<string, RestEndpointRuntimeDescriptor>(Comparer);
        endpointsByRouteIdentity = new Dictionary<string, RestEndpointRuntimeDescriptor>(Comparer);
        endpointsBySourceModule = new Dictionary<string, IReadOnlyList<RestEndpointRuntimeDescriptor>>(Comparer);
        endpointsByBehaviorId = new Dictionary<string, IReadOnlyList<RestEndpointRuntimeDescriptor>>(Comparer);
    }

    public void Register(RestEndpointRuntimeDescriptor endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var routeIdentity = BuildRouteIdentity(endpoint.Method, endpoint.RoutePattern);
        if (endpointsByRouteIdentity.TryGetValue(routeIdentity, out var existing))
        {
            throw new InvalidOperationException(
                $"Resolved public REST endpoint collision detected for '{endpoint.Method} {endpoint.RoutePattern}'. " +
                $"Endpoint '{DescribeEndpoint(existing)}' conflicts with '{DescribeEndpoint(endpoint)}'.");
        }

        var nextEndpoints = endpoints
            .Append(endpoint)
            .OrderBy(static candidate => candidate.RoutePattern, Comparer)
            .ThenBy(static candidate => candidate.Method, Comparer)
            .ThenBy(static candidate => candidate.Id, Comparer)
            .ToArray();

        endpoints = nextEndpoints;
        endpointsById = nextEndpoints.ToDictionary(static candidate => candidate.Id, Comparer);
        endpointsByRouteIdentity = nextEndpoints.ToDictionary(
            candidate => BuildRouteIdentity(candidate.Method, candidate.RoutePattern),
            Comparer);
        endpointsBySourceModule = nextEndpoints
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate.SourceModuleId))
            .GroupBy(static candidate => candidate.SourceModuleId, Comparer)
            .ToDictionary(
                static group => group.Key!,
                static group => (IReadOnlyList<RestEndpointRuntimeDescriptor>)group.ToArray(),
                Comparer);
        endpointsByBehaviorId = nextEndpoints
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate.BehaviorId))
            .GroupBy(static candidate => candidate.BehaviorId!, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<RestEndpointRuntimeDescriptor>)group.ToArray(),
                Comparer);
    }

    private static string BuildRouteIdentity(string httpMethod, string routePattern)
    {
        return $"{httpMethod.Trim().ToUpperInvariant()} {routePattern.Trim()}";
    }

    private static string DescribeEndpoint(RestEndpointRuntimeDescriptor endpoint)
    {
        var behavior = string.IsNullOrWhiteSpace(endpoint.BehaviorId)
            ? "no behavior id"
            : $"behavior '{endpoint.BehaviorId}'";
        return $"{endpoint.Id} from module '{endpoint.SourceModuleId}' ({behavior}, source kind '{endpoint.SourceKind}')";
    }
}
