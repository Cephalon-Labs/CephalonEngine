using Cephalon.Edge.KubernetesGateway.Configuration;

namespace Cephalon.Edge.KubernetesGateway.Services;

internal sealed class KubernetesGatewayTrafficProjectionCatalog
{
    private readonly IReadOnlyDictionary<string, KubernetesGatewayTrafficRouteProjection> projectionsByRouteId;

    public KubernetesGatewayTrafficProjectionCatalog(KubernetesGatewayTrafficMaterializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        projectionsByRouteId = KubernetesGatewayTrafficProjectionBuilder.Build(options);
    }

    public IReadOnlyDictionary<string, KubernetesGatewayTrafficRouteProjection> Projections => projectionsByRouteId;

    public bool ContainsRoute(string routeId)
    {
        return !string.IsNullOrWhiteSpace(routeId) &&
            projectionsByRouteId.ContainsKey(routeId.Trim());
    }

    public bool TryGetByRouteId(string routeId, out KubernetesGatewayTrafficRouteProjection projection)
    {
        if (string.IsNullOrWhiteSpace(routeId))
        {
            projection = default!;
            return false;
        }

        return projectionsByRouteId.TryGetValue(routeId.Trim(), out projection!);
    }
}
