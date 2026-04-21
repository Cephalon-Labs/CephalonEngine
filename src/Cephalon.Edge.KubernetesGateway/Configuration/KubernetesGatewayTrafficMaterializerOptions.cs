namespace Cephalon.Edge.KubernetesGateway.Configuration;

/// <summary>
/// Configures the Kubernetes Gateway API control-plane materializer for provider-managed cell traffic automation.
/// </summary>
public sealed class KubernetesGatewayTrafficMaterializerOptions
{
    /// <summary>
    /// Gets the default provider identifier used by the Kubernetes Gateway traffic materializer.
    /// </summary>
    public const string DefaultProviderId = "kubernetes-gateway";

    /// <summary>
    /// Gets the default materializer identifier used by the Kubernetes Gateway traffic materializer.
    /// </summary>
    public const string DefaultMaterializerId = "kubernetes-gateway-materializer";

    /// <summary>
    /// Gets or sets the stable materializer identifier that should appear on operator-facing runtime answers.
    /// </summary>
    public string MaterializerId { get; set; } = DefaultMaterializerId;

    /// <summary>
    /// Gets or sets the provider identifier that the materializer owns.
    /// </summary>
    public string ProviderId { get; set; } = DefaultProviderId;

    /// <summary>
    /// Gets or sets the priority used when multiple provider materializers can reconcile the same automation answer.
    /// Higher values win while ties still fail deterministically in the engine.
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Gets or sets the optional Gateway controller name that owns the configured GatewayClass.
    /// </summary>
    public string? ControllerName { get; set; }

    /// <summary>
    /// Gets or sets the optional default GatewayClass name that backs the projected traffic intent.
    /// </summary>
    public string? GatewayClassName { get; set; }

    /// <summary>
    /// Gets or sets the default Kubernetes namespace that contains the projected Gateway resource.
    /// </summary>
    public string? GatewayNamespace { get; set; }

    /// <summary>
    /// Gets or sets the default Gateway name targeted by projected HTTPRoute parent references.
    /// </summary>
    public string? GatewayName { get; set; }

    /// <summary>
    /// Gets or sets the optional default Gateway listener or section name used by projected parent references.
    /// </summary>
    public string? ListenerName { get; set; }

    /// <summary>
    /// Gets or sets the default namespace used for projected HTTPRoute resources when a route-level override is absent.
    /// </summary>
    public string? RouteNamespace { get; set; }

    /// <summary>
    /// Gets the route-level Kubernetes Gateway projections owned by this materializer.
    /// </summary>
    public IList<KubernetesGatewayTrafficRouteOptions> Routes { get; } = [];

    /// <summary>
    /// Gets the live-observation options used to overlay Kubernetes Gateway API status back into the shared runtime catalog.
    /// </summary>
    public KubernetesGatewayTrafficObservationOptions Observation { get; } = new();
}
