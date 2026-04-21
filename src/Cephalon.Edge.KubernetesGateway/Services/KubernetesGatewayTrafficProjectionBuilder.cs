using System.Globalization;
using System.Text;
using Cephalon.Edge.KubernetesGateway.Configuration;

namespace Cephalon.Edge.KubernetesGateway.Services;

internal static class KubernetesGatewayTrafficProjectionBuilder
{
    public static IReadOnlyDictionary<string, KubernetesGatewayTrafficRouteProjection> Build(
        KubernetesGatewayTrafficMaterializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var materializerId = NormalizeRequired(
            options.MaterializerId,
            $"{nameof(KubernetesGatewayTrafficMaterializerOptions.MaterializerId)} is required.");
        var providerId = NormalizeRequired(
            options.ProviderId,
            $"{nameof(KubernetesGatewayTrafficMaterializerOptions.ProviderId)} is required.");
        _ = materializerId;
        _ = providerId;

        var defaultControllerName = NormalizeOptional(options.ControllerName);
        var defaultGatewayClassName = NormalizeOptional(options.GatewayClassName);
        var defaultGatewayNamespace = NormalizeOptional(options.GatewayNamespace);
        var defaultGatewayName = NormalizeOptional(options.GatewayName);
        var defaultListenerName = NormalizeOptional(options.ListenerName);
        var defaultRouteNamespace = NormalizeOptional(options.RouteNamespace);

        var projections = new Dictionary<string, KubernetesGatewayTrafficRouteProjection>(StringComparer.OrdinalIgnoreCase);
        foreach (var route in options.Routes)
        {
            ArgumentNullException.ThrowIfNull(route);

            var routeId = NormalizeRequired(
                route.RouteId,
                $"{nameof(KubernetesGatewayTrafficRouteOptions.RouteId)} is required for Kubernetes Gateway traffic materialization.");
            var gatewayNamespace = NormalizeRequired(
                route.GatewayNamespace ?? defaultGatewayNamespace,
                $"Kubernetes Gateway traffic route '{routeId}' requires {nameof(KubernetesGatewayTrafficRouteOptions.GatewayNamespace)} or a pack-level {nameof(KubernetesGatewayTrafficMaterializerOptions.GatewayNamespace)}.");
            var gatewayName = NormalizeRequired(
                route.GatewayName ?? defaultGatewayName,
                $"Kubernetes Gateway traffic route '{routeId}' requires {nameof(KubernetesGatewayTrafficRouteOptions.GatewayName)} or a pack-level {nameof(KubernetesGatewayTrafficMaterializerOptions.GatewayName)}.");
            var routeNamespace = NormalizeOptional(route.RouteNamespace)
                ?? defaultRouteNamespace
                ?? gatewayNamespace;
            var httpRouteName = NormalizeOptional(route.HttpRouteName)
                ?? CreateDeterministicRouteName(routeId);
            var backendServiceName = NormalizeRequired(
                route.BackendServiceName,
                $"Kubernetes Gateway traffic route '{routeId}' requires {nameof(KubernetesGatewayTrafficRouteOptions.BackendServiceName)}.");
            var backendPort = route.BackendPort is > 0
                ? route.BackendPort.Value
                : throw new InvalidOperationException(
                    $"Kubernetes Gateway traffic route '{routeId}' requires a positive {nameof(KubernetesGatewayTrafficRouteOptions.BackendPort)}.");
            var backendWeight = route.BackendWeight;
            if (backendWeight is <= 0)
            {
                throw new InvalidOperationException(
                    $"Kubernetes Gateway traffic route '{routeId}' must use a positive {nameof(KubernetesGatewayTrafficRouteOptions.BackendWeight)} when a weight is configured.");
            }

            var hostnames = route.Hostnames
                .Where(static hostname => !string.IsNullOrWhiteSpace(hostname))
                .Select(static hostname => hostname.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var projection = new KubernetesGatewayTrafficRouteProjection(
                routeId,
                defaultControllerName,
                defaultGatewayClassName,
                gatewayNamespace,
                gatewayName,
                NormalizeOptional(route.ListenerName) ?? defaultListenerName,
                routeNamespace,
                httpRouteName,
                hostnames,
                NormalizeOptional(route.BackendNamespace) ?? routeNamespace,
                backendServiceName,
                backendPort,
                backendWeight,
                NormalizeOptional(route.ControllerName) ?? defaultControllerName,
                NormalizeOptional(route.GatewayClassName) ?? defaultGatewayClassName);

            if (!projections.TryAdd(routeId, projection))
            {
                throw new InvalidOperationException(
                    $"Kubernetes Gateway traffic materializer '{materializerId}' declares duplicate projections for route '{routeId}'.");
            }
        }

        return projections;
    }

    private static string NormalizeRequired(string? value, string message)
    {
        var normalized = NormalizeOptional(value);
        return normalized ?? throw new InvalidOperationException(message);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string CreateDeterministicRouteName(string routeId)
    {
        Span<char> buffer = stackalloc char[routeId.Length];
        var length = 0;
        var previousWasHyphen = false;

        foreach (var character in routeId.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                if (length < buffer.Length)
                {
                    buffer[length++] = character;
                }

                previousWasHyphen = false;
                continue;
            }

            if (previousWasHyphen || length == 0)
            {
                continue;
            }

            if (length < buffer.Length)
            {
                buffer[length++] = '-';
            }

            previousWasHyphen = true;
        }

        while (length > 0 && buffer[length - 1] == '-')
        {
            length--;
        }

        if (length == 0)
        {
            return "cell-traffic-route";
        }

        return new string(buffer[..length]);
    }
}

internal sealed record KubernetesGatewayTrafficRouteProjection(
    string RouteId,
    string? DefaultControllerName,
    string? DefaultGatewayClassName,
    string GatewayNamespace,
    string GatewayName,
    string? ListenerName,
    string RouteNamespace,
    string HttpRouteName,
    IReadOnlyList<string> Hostnames,
    string BackendNamespace,
    string BackendServiceName,
    int BackendPort,
    int? BackendWeight,
    string? ControllerName,
    string? GatewayClassName)
{
    public const string GatewayApiGroup = "gateway.networking.k8s.io";
    public const string GatewayApiVersion = "v1";
    public const string GatewayApiVersionLabel = GatewayApiGroup + "/" + GatewayApiVersion;
    public const string ConfiguredIntentStatusSource = "configured-intent";
    public const string UnknownConditionState = "unknown";

    public string ProviderRouteId => $"httproute/{RouteNamespace}/{HttpRouteName}";

    public string GatewayResourceId => $"gateway/{GatewayNamespace}/{GatewayName}";

    public string ParentReference =>
        string.IsNullOrWhiteSpace(ListenerName)
            ? $"gateway/{GatewayNamespace}/{GatewayName}"
            : $"gateway/{GatewayNamespace}/{GatewayName}#listener/{ListenerName}";

    public string BackendReference =>
        BackendWeight is > 0
            ? $"service/{BackendNamespace}/{BackendServiceName}:{BackendPort.ToString(CultureInfo.InvariantCulture)}@weight/{BackendWeight.Value.ToString(CultureInfo.InvariantCulture)}"
            : $"service/{BackendNamespace}/{BackendServiceName}:{BackendPort.ToString(CultureInfo.InvariantCulture)}";

    public Dictionary<string, string> CreateMetadata()
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["gatewayApiVersion"] = GatewayApiVersionLabel,
            ["gatewayNamespace"] = GatewayNamespace,
            ["gatewayName"] = GatewayName,
            ["gatewayResourceId"] = GatewayResourceId,
            ["httpRouteNamespace"] = RouteNamespace,
            ["httpRouteName"] = HttpRouteName,
            ["providerRouteId"] = ProviderRouteId,
            ["httpRouteResourceId"] = ProviderRouteId,
            ["httpRouteParentRefs"] = ParentReference,
            ["httpRouteParentRefCount"] = "1",
            ["httpRouteBackendRefs"] = BackendReference,
            ["httpRouteBackendRefCount"] = "1",
            ["httpRouteHostnames"] = string.Join(",", Hostnames),
            ["httpRouteHostnameCount"] = Hostnames.Count.ToString(CultureInfo.InvariantCulture),
            ["statusSource"] = ConfiguredIntentStatusSource,
            ["httpRouteAcceptedCondition"] = UnknownConditionState,
            ["httpRouteResolvedRefsCondition"] = UnknownConditionState,
            ["httpRouteProgrammedCondition"] = UnknownConditionState
        };

        if (!string.IsNullOrWhiteSpace(ListenerName))
        {
            metadata["listenerName"] = ListenerName!;
        }

        if (!string.IsNullOrWhiteSpace(ControllerName))
        {
            metadata["controllerName"] = ControllerName!;
        }

        if (!string.IsNullOrWhiteSpace(GatewayClassName))
        {
            metadata["gatewayClassName"] = GatewayClassName!;
            metadata["gatewayClassResourceId"] = $"gatewayclass/{GatewayClassName}";
        }

        return metadata;
    }
}
