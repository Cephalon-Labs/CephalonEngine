namespace Cephalon.Edge.Traefik.Configuration;

/// <summary>
/// Configures the Traefik IngressRoute control-plane materializer for provider-managed cell traffic automation.
/// </summary>
public sealed class TraefikTrafficMaterializerOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TraefikTrafficMaterializerOptions" /> class.
    /// </summary>
    public TraefikTrafficMaterializerOptions()
    {
    }

    /// <summary>
    /// Gets the default provider identifier used by the Traefik traffic materializer.
    /// </summary>
    public const string DefaultProviderId = "traefik";

    /// <summary>
    /// Gets the default materializer identifier used by the Traefik traffic materializer.
    /// </summary>
    public const string DefaultMaterializerId = "traefik-materializer";

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
    /// Gets or sets the default Kubernetes namespace that contains projected IngressRoute resources.
    /// </summary>
    public string? RouteNamespace { get; set; }

    /// <summary>
    /// Gets the default Traefik entry points applied when a route-level override is absent.
    /// </summary>
    public IList<string> EntryPoints { get; } = [];

    /// <summary>
    /// Gets the route-level Traefik IngressRoute projections owned by this materializer.
    /// </summary>
    public IList<TraefikIngressRouteOptions> Routes { get; } = [];

    /// <summary>
    /// Gets the live-observation options used to overlay Traefik IngressRoute status back into the shared runtime catalog.
    /// </summary>
    public TraefikTrafficObservationOptions Observation { get; } = new();
}
