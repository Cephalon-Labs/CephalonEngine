using Cephalon.Edge.Traefik.Configuration;

namespace Cephalon.Edge.Traefik.Services;

internal sealed class TraefikTrafficProjectionCatalog
{
    private readonly IReadOnlyDictionary<string, TraefikIngressRouteProjection> projectionsByRouteId;

    public TraefikTrafficProjectionCatalog(TraefikTrafficMaterializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        projectionsByRouteId = TraefikTrafficProjectionBuilder.Build(options);
    }

    public IReadOnlyDictionary<string, TraefikIngressRouteProjection> Projections => projectionsByRouteId;

    public bool ContainsRoute(string routeId)
    {
        return !string.IsNullOrWhiteSpace(routeId) &&
            projectionsByRouteId.ContainsKey(routeId.Trim());
    }

    public bool TryGetByRouteId(string routeId, out TraefikIngressRouteProjection projection)
    {
        if (string.IsNullOrWhiteSpace(routeId))
        {
            projection = default!;
            return false;
        }

        return projectionsByRouteId.TryGetValue(routeId.Trim(), out projection!);
    }
}
