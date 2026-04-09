using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Http.Hosting;

/// <summary>
/// Configures the generic REST route contract contributed through <see cref="IBehaviorTopologyBuilder" />.
/// </summary>
/// <remarks>
/// This builder describes the canonical generic REST transport surface only. For fully public,
/// OpenAPI-owned REST endpoints, prefer <see cref="BehaviorRestEndpointRouteBuilderExtensions.MapBehaviorRestGroup" />.
/// </remarks>
public sealed class BehaviorRestTopologyBuilder
{
    private readonly IBehaviorTopologyBuilder topologyBuilder;
    private readonly Dictionary<string, string> queryBindings;
    private readonly Dictionary<string, string> routeBindings;

    internal BehaviorRestTopologyBuilder(IBehaviorTopologyBuilder topologyBuilder)
    {
        this.topologyBuilder = topologyBuilder ?? throw new ArgumentNullException(nameof(topologyBuilder));
        queryBindings = new(
            StringComparer.OrdinalIgnoreCase);
        routeBindings = new(
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Maps the behavior onto a generic REST <c>GET</c> route.
    /// </summary>
    /// <param name="pattern">The route pattern relative to the configured REST prefix and version segment.</param>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorRestTopologyBuilder MapGet(string pattern) => Map("GET", pattern);

    /// <summary>
    /// Maps the behavior onto a generic REST <c>POST</c> route.
    /// </summary>
    /// <param name="pattern">The route pattern relative to the configured REST prefix and version segment.</param>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorRestTopologyBuilder MapPost(string pattern) => Map("POST", pattern);

    /// <summary>
    /// Maps the behavior onto a generic REST <c>PUT</c> route.
    /// </summary>
    /// <param name="pattern">The route pattern relative to the configured REST prefix and version segment.</param>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorRestTopologyBuilder MapPut(string pattern) => Map("PUT", pattern);

    /// <summary>
    /// Maps the behavior onto a generic REST <c>PATCH</c> route.
    /// </summary>
    /// <param name="pattern">The route pattern relative to the configured REST prefix and version segment.</param>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorRestTopologyBuilder MapPatch(string pattern) => Map("PATCH", pattern);

    /// <summary>
    /// Maps the behavior onto a generic REST <c>DELETE</c> route.
    /// </summary>
    /// <param name="pattern">The route pattern relative to the configured REST prefix and version segment.</param>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorRestTopologyBuilder MapDelete(string pattern) => Map("DELETE", pattern);

    /// <summary>
    /// Maps a route token such as <c>cartId</c> onto a different input member name.
    /// </summary>
    /// <param name="routeToken">The route token name from the template.</param>
    /// <param name="memberName">The target input member name.</param>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorRestTopologyBuilder BindRoute(string routeToken, string memberName)
    {
        routeBindings[NormalizeBindingKey(routeToken)] = NormalizeBindingValue(memberName);
        topologyBuilder.WithMetadata(
            BehaviorRestTopologyMetadata.RouteBindingsKey,
            BehaviorRestTopologyMetadata.WriteBindings(routeBindings));
        return this;
    }

    /// <summary>
    /// Maps a query-string key such as <c>tenant</c> onto a different input member name.
    /// </summary>
    /// <param name="queryKey">The public query-string key.</param>
    /// <param name="memberName">The target input member name.</param>
    /// <returns>The same builder for fluent chaining.</returns>
    public BehaviorRestTopologyBuilder BindQuery(string queryKey, string memberName)
    {
        queryBindings[NormalizeBindingKey(queryKey)] = NormalizeBindingValue(memberName);
        topologyBuilder.WithMetadata(
            BehaviorRestTopologyMetadata.QueryBindingsKey,
            BehaviorRestTopologyMetadata.WriteBindings(queryBindings));
        return this;
    }

    private BehaviorRestTopologyBuilder Map(string method, string pattern)
    {
        var normalizedPattern = NormalizePattern(pattern);
        topologyBuilder.WithMetadata(BehaviorRestTopologyMetadata.MethodKey, method);
        topologyBuilder.WithMetadata(BehaviorRestTopologyMetadata.RoutePatternKey, normalizedPattern);
        return this;
    }

    internal static string NormalizePattern(string pattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var normalized = pattern.Trim().Trim('/');

        return normalized.Length == 0
            ? string.Empty
            : normalized;
    }

    private static string NormalizeBindingKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return key.Trim();
    }

    private static string NormalizeBindingValue(string memberName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);
        return memberName.Trim();
    }
}
