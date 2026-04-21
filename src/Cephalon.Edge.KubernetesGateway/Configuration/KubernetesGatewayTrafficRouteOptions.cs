namespace Cephalon.Edge.KubernetesGateway.Configuration;

/// <summary>
/// Configures one cell traffic automation route that should materialize into Kubernetes Gateway API intent.
/// </summary>
public sealed class KubernetesGatewayTrafficRouteOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KubernetesGatewayTrafficRouteOptions" /> class.
    /// </summary>
    public KubernetesGatewayTrafficRouteOptions()
    {
    }

    /// <summary>
    /// Gets or sets the Cephalon cell-route identifier that this provider projection owns.
    /// </summary>
    public string? RouteId { get; set; }

    /// <summary>
    /// Gets or sets the Kubernetes namespace that should own the projected HTTPRoute resource.
    /// When omitted, the materializer falls back to the pack-level <c>RouteNamespace</c> and then the effective Gateway namespace.
    /// </summary>
    public string? RouteNamespace { get; set; }

    /// <summary>
    /// Gets or sets the HTTPRoute resource name that should represent the route in Kubernetes Gateway API.
    /// When omitted, the materializer derives a deterministic DNS-safe name from <see cref="RouteId" />.
    /// </summary>
    public string? HttpRouteName { get; set; }

    /// <summary>
    /// Gets the optional hostnames published by the projected HTTPRoute.
    /// </summary>
    public IList<string> Hostnames { get; } = [];

    /// <summary>
    /// Gets or sets the Kubernetes namespace that contains the parent Gateway.
    /// When omitted, the materializer falls back to the pack-level <c>GatewayNamespace</c>.
    /// </summary>
    public string? GatewayNamespace { get; set; }

    /// <summary>
    /// Gets or sets the parent Gateway name targeted by the projected HTTPRoute.
    /// When omitted, the materializer falls back to the pack-level <c>GatewayName</c>.
    /// </summary>
    public string? GatewayName { get; set; }

    /// <summary>
    /// Gets or sets the optional Gateway listener or section name used by the projected parent reference.
    /// When omitted, the materializer falls back to the pack-level <c>ListenerName</c>.
    /// </summary>
    public string? ListenerName { get; set; }

    /// <summary>
    /// Gets or sets the optional GatewayClass name associated with the parent Gateway.
    /// When omitted, the materializer falls back to the pack-level <c>GatewayClassName</c>.
    /// </summary>
    public string? GatewayClassName { get; set; }

    /// <summary>
    /// Gets or sets the optional controller name associated with the parent GatewayClass.
    /// When omitted, the materializer falls back to the pack-level <c>ControllerName</c>.
    /// </summary>
    public string? ControllerName { get; set; }

    /// <summary>
    /// Gets or sets the Kubernetes namespace that contains the projected backend Service.
    /// When omitted, the materializer falls back to the effective target route namespace.
    /// </summary>
    public string? BackendNamespace { get; set; }

    /// <summary>
    /// Gets or sets the backend Kubernetes Service name referenced by the projected HTTPRoute.
    /// </summary>
    public string? BackendServiceName { get; set; }

    /// <summary>
    /// Gets or sets the backend Service port referenced by the projected HTTPRoute.
    /// </summary>
    public int? BackendPort { get; set; }

    /// <summary>
    /// Gets or sets the optional backend weight applied to the projected Service reference.
    /// </summary>
    public int? BackendWeight { get; set; }
}
