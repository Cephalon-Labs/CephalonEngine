using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Http.Hosting;

internal sealed class BehaviorRestTransportContract
{
    private readonly IReadOnlyDictionary<string, string> queryBindings;
    private readonly IReadOnlyDictionary<string, string> routeBindings;

    private BehaviorRestTransportContract(
        string? httpMethod,
        string? routeTemplate,
        IReadOnlyDictionary<string, string> routeBindings,
        IReadOnlyDictionary<string, string> queryBindings)
    {
        HttpMethod = string.IsNullOrWhiteSpace(httpMethod)
            ? null
            : httpMethod.Trim().ToUpperInvariant();
        RouteTemplate = routeTemplate;
        this.routeBindings = routeBindings;
        this.queryBindings = queryBindings;
    }

    public string? HttpMethod { get; }

    public bool HasExplicitHttpMethod => !string.IsNullOrWhiteSpace(HttpMethod);

    public string? RouteTemplate { get; }

    public static BehaviorRestTransportContract FromDescriptor(BehaviorTopologyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        string? httpMethod = null;
        string? routeTemplate = null;

        if (BehaviorRestTopologyMetadata.TryResolveExplicitRoute(descriptor, out var configuredMethod, out var configuredRoutePattern))
        {
            httpMethod = configuredMethod;
            routeTemplate = configuredRoutePattern;
        }

        return new BehaviorRestTransportContract(
            httpMethod,
            routeTemplate,
            BehaviorRestTopologyMetadata.ReadBindings(descriptor, BehaviorRestTopologyMetadata.RouteBindingsKey),
            BehaviorRestTopologyMetadata.ReadBindings(descriptor, BehaviorRestTopologyMetadata.QueryBindingsKey));
    }

    public string ResolveRouteMemberName(string tokenName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenName);

        return routeBindings.TryGetValue(tokenName.Trim(), out var memberName)
            ? memberName
            : tokenName.Trim();
    }

    public string ResolveQueryMemberName(string queryKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queryKey);

        return queryBindings.TryGetValue(queryKey.Trim(), out var memberName)
            ? memberName
            : queryKey.Trim();
    }
}
