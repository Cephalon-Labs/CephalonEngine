using Cephalon.Abstractions.Behaviors;
using System.Text.Json;

namespace Cephalon.Behaviors.Http.Hosting;

internal static class BehaviorRestTopologyMetadata
{
    public const string MethodKey = "cephalon.http.rest.method";
    public const string RoutePatternKey = "cephalon.http.rest.route-template";
    public const string RouteBindingsKey = "cephalon.http.rest.route-bindings";
    public const string QueryBindingsKey = "cephalon.http.rest.query-bindings";

    public static bool TryResolveExplicitRoute(
        BehaviorTopologyDescriptor descriptor,
        out string method,
        out string routePattern)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        method = string.Empty;
        routePattern = string.Empty;

        if (!descriptor.Metadata.TryGetValue(MethodKey, out var configuredMethod) ||
            !descriptor.Metadata.TryGetValue(RoutePatternKey, out var configuredRoutePattern) ||
            string.IsNullOrWhiteSpace(configuredMethod) ||
            string.IsNullOrWhiteSpace(configuredRoutePattern))
        {
            return false;
        }

        method = configuredMethod.Trim().ToUpperInvariant();
        routePattern = configuredRoutePattern.Trim();
        return true;
    }

    public static IReadOnlyDictionary<string, string> ReadBindings(
        BehaviorTopologyDescriptor descriptor,
        string key)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!descriptor.Metadata.TryGetValue(key, out var rawValue) || string.IsNullOrWhiteSpace(rawValue))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(rawValue);
            return parsed is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(parsed, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static string WriteBindings(IReadOnlyDictionary<string, string> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        return JsonSerializer.Serialize(bindings);
    }
}
