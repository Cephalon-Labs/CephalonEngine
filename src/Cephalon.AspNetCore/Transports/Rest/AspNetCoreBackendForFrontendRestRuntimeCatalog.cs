using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.Transports;

namespace Cephalon.AspNetCore.Transports.Rest;

internal sealed class AspNetCoreBackendForFrontendRestRuntimeCatalog : IBackendForFrontendRestRuntimeCatalog
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private readonly BackendForFrontendRestEndpointRuntimeDescriptor[] endpoints;
    private readonly Dictionary<string, BackendForFrontendRestEndpointRuntimeDescriptor> endpointsById;
    private readonly Dictionary<string, IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor>> endpointsByBindingId;
    private readonly Dictionary<string, IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor>> endpointsByClientId;
    private readonly Dictionary<string, IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor>> endpointsBySourceModuleId;
    private readonly Dictionary<string, IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor>> endpointsByRestEndpointId;

    public AspNetCoreBackendForFrontendRestRuntimeCatalog(
        IBackendForFrontendRuntimeCatalog backendForFrontendRuntimeCatalog,
        IRestEndpointRuntimeCatalog restEndpointRuntimeCatalog)
    {
        ArgumentNullException.ThrowIfNull(backendForFrontendRuntimeCatalog);
        ArgumentNullException.ThrowIfNull(restEndpointRuntimeCatalog);

        endpoints = BuildEndpoints(
            backendForFrontendRuntimeCatalog.Bindings,
            restEndpointRuntimeCatalog.Endpoints);
        endpointsById = endpoints.ToDictionary(static endpoint => endpoint.Id, Comparer);
        endpointsByBindingId = GroupBy(endpoints, static endpoint => endpoint.BindingId);
        endpointsByClientId = GroupBy(endpoints, static endpoint => endpoint.ClientId);
        endpointsBySourceModuleId = GroupBy(endpoints, static endpoint => endpoint.SourceModuleId);
        endpointsByRestEndpointId = GroupBy(endpoints, static endpoint => endpoint.RestEndpointId);
    }

    public IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor> Endpoints => endpoints;

    public BackendForFrontendRestEndpointRuntimeDescriptor? GetById(string runtimeEndpointId)
    {
        if (string.IsNullOrWhiteSpace(runtimeEndpointId))
        {
            return null;
        }

        return endpointsById.TryGetValue(runtimeEndpointId.Trim(), out var runtimeEndpoint)
            ? runtimeEndpoint
            : null;
    }

    public IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor> GetByBindingId(string bindingId)
    {
        if (string.IsNullOrWhiteSpace(bindingId))
        {
            return [];
        }

        return endpointsByBindingId.TryGetValue(bindingId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor> GetByClientId(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return [];
        }

        return endpointsByClientId.TryGetValue(clientId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return endpointsBySourceModuleId.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor> GetByRestEndpointId(string restEndpointId)
    {
        if (string.IsNullOrWhiteSpace(restEndpointId))
        {
            return [];
        }

        return endpointsByRestEndpointId.TryGetValue(restEndpointId.Trim(), out var matches)
            ? matches
            : [];
    }

    private static BackendForFrontendRestEndpointRuntimeDescriptor[] BuildEndpoints(
        IReadOnlyList<BackendForFrontendClientBindingDescriptor> bindings,
        IReadOnlyList<RestEndpointRuntimeDescriptor> restEndpoints)
    {
        var items = new List<BackendForFrontendRestEndpointRuntimeDescriptor>();
        var activeRestEndpoints = restEndpoints
            .Where(static endpoint => IsRestTransport(endpoint.TransportId))
            .ToArray();

        foreach (var binding in bindings.Where(static binding => IsRestTransport(binding.TransportId)))
        {
            foreach (var endpoint in activeRestEndpoints)
            {
                if (TryCreateRuntimeDescriptor(binding, endpoint, out var runtimeDescriptor))
                {
                    items.Add(runtimeDescriptor);
                }
            }
        }

        return items
            .OrderBy(static endpoint => endpoint.ClientId, Comparer)
            .ThenBy(static endpoint => endpoint.BindingId, Comparer)
            .ThenBy(static endpoint => endpoint.Endpoint.RoutePattern, Comparer)
            .ThenBy(static endpoint => endpoint.Endpoint.Method, Comparer)
            .ThenBy(static endpoint => endpoint.RestEndpointId, Comparer)
            .ToArray();
    }

    private static bool TryCreateRuntimeDescriptor(
        BackendForFrontendClientBindingDescriptor binding,
        RestEndpointRuntimeDescriptor endpoint,
        out BackendForFrontendRestEndpointRuntimeDescriptor runtimeDescriptor)
    {
        var filter = binding.BehaviorFilter;
        var matchedBehaviorIds = MatchSingle(endpoint.BehaviorId, filter.IncludedBehaviorIds);
        var matchedCapabilityKeys = MatchSingle(endpoint.RequiredCapabilityKey, filter.IncludedCapabilityKeys);
        var matchedTags = MatchMany(endpoint.Tags, filter.IncludedTags);

        if (IsExcluded(endpoint, filter))
        {
            runtimeDescriptor = null!;
            return false;
        }

        var hasExplicitIncludeFilters =
            filter.IncludedBehaviorIds.Count > 0 ||
            filter.IncludedCapabilityKeys.Count > 0 ||
            filter.IncludedTags.Count > 0;
        var matchedByDefault = !hasExplicitIncludeFilters;
        if (!matchedByDefault &&
            matchedBehaviorIds.Length == 0 &&
            matchedCapabilityKeys.Length == 0 &&
            matchedTags.Length == 0)
        {
            runtimeDescriptor = null!;
            return false;
        }

        runtimeDescriptor = new BackendForFrontendRestEndpointRuntimeDescriptor(
            id: BuildRuntimeDescriptorId(binding.Id, endpoint.Id),
            binding: binding,
            endpoint: endpoint,
            matchedByDefault: matchedByDefault,
            matchedBehaviorIds: matchedByDefault ? null : matchedBehaviorIds,
            matchedCapabilityKeys: matchedByDefault ? null : matchedCapabilityKeys,
            matchedTags: matchedByDefault ? null : matchedTags);
        return true;
    }

    private static bool IsExcluded(
        RestEndpointRuntimeDescriptor endpoint,
        BackendForFrontendBehaviorFilterDescriptor filter)
    {
        return MatchSingle(endpoint.BehaviorId, filter.ExcludedBehaviorIds).Length > 0 ||
               MatchSingle(endpoint.RequiredCapabilityKey, filter.ExcludedCapabilityKeys).Length > 0 ||
               MatchMany(endpoint.Tags, filter.ExcludedTags).Length > 0;
    }

    private static string[] MatchSingle(string? value, IReadOnlyList<string> filters)
    {
        if (string.IsNullOrWhiteSpace(value) || filters.Count == 0)
        {
            return [];
        }

        var normalizedValue = value.Trim();
        return filters.Contains(normalizedValue, Comparer)
            ? [normalizedValue]
            : [];
    }

    private static string[] MatchMany(
        IReadOnlyList<string> values,
        IReadOnlyList<string> filters)
    {
        if (values.Count == 0 || filters.Count == 0)
        {
            return [];
        }

        return values
            .Where(value => filters.Contains(value, Comparer))
            .Distinct(Comparer)
            .OrderBy(static value => value, Comparer)
            .ToArray();
    }

    private static Dictionary<string, IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor>> GroupBy(
        IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor> values,
        Func<BackendForFrontendRestEndpointRuntimeDescriptor, string> keySelector)
    {
        return values
            .GroupBy(keySelector, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor>)group.ToArray(),
                Comparer);
    }

    private static string BuildRuntimeDescriptorId(string bindingId, string restEndpointId)
    {
        return $"{bindingId.Trim()}::{restEndpointId.Trim()}";
    }

    private static bool IsRestTransport(string transportId)
    {
        return transportId.Trim().ToLowerInvariant() switch
        {
            "rest-api" => true,
            "rest" => true,
            "http.rest" => true,
            _ => false
        };
    }
}
