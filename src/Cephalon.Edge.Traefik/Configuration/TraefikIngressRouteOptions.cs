namespace Cephalon.Edge.Traefik.Configuration;

/// <summary>
/// Configures one cell traffic automation route that should materialize into Traefik IngressRoute intent.
/// </summary>
public sealed class TraefikIngressRouteOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TraefikIngressRouteOptions" /> class.
    /// </summary>
    public TraefikIngressRouteOptions()
    {
    }

    /// <summary>
    /// Gets or sets the Cephalon cell-route identifier that this provider projection owns.
    /// </summary>
    public string? RouteId { get; set; }

    /// <summary>
    /// Gets or sets the Kubernetes namespace that should own the projected IngressRoute resource.
    /// When omitted, the materializer falls back to the pack-level <c>RouteNamespace</c>.
    /// </summary>
    public string? RouteNamespace { get; set; }

    /// <summary>
    /// Gets or sets the IngressRoute resource name that should represent the route in Traefik.
    /// When omitted, the materializer derives a deterministic DNS-safe name from <see cref="RouteId" />.
    /// </summary>
    public string? IngressRouteName { get; set; }

    /// <summary>
    /// Gets the Traefik entry points that should accept requests for the projected route.
    /// When empty, the materializer falls back to the pack-level <c>EntryPoints</c> list.
    /// </summary>
    public IList<string> EntryPoints { get; } = [];

    /// <summary>
    /// Gets or sets the Traefik rule expression such as <c>Host(`api.example.com`) &amp;&amp; PathPrefix(`/orders`)</c>.
    /// </summary>
    public string? MatchRule { get; set; }

    /// <summary>
    /// Gets or sets the optional explicit Traefik route priority.
    /// </summary>
    public int? Priority { get; set; }

    /// <summary>
    /// Gets the middleware references that should attach to the projected route in declaration order.
    /// </summary>
    public IList<TraefikMiddlewareReferenceOptions> Middlewares { get; } = [];

    /// <summary>
    /// Gets or sets the Kubernetes namespace that owns the backend Service.
    /// When omitted, the materializer falls back to the effective IngressRoute namespace.
    /// </summary>
    public string? BackendNamespace { get; set; }

    /// <summary>
    /// Gets or sets the backend Kubernetes Service name referenced by the projected route.
    /// </summary>
    public string? BackendServiceName { get; set; }

    /// <summary>
    /// Gets or sets the backend Service port referenced by the projected route.
    /// </summary>
    public int? BackendPort { get; set; }

    /// <summary>
    /// Gets or sets the optional backend weight applied to the projected Service reference.
    /// </summary>
    public int? BackendWeight { get; set; }

    /// <summary>
    /// Gets or sets the optional backend scheme such as <c>http</c> or <c>https</c>.
    /// </summary>
    public string? BackendScheme { get; set; }

    /// <summary>
    /// Gets or sets the optional pass-host-header posture applied to the projected backend Service reference.
    /// </summary>
    public bool? PassHostHeader { get; set; }

    /// <summary>
    /// Gets or sets the optional TLS Secret name that should terminate the projected IngressRoute.
    /// </summary>
    public string? TlsSecretName { get; set; }

    /// <summary>
    /// Gets or sets the optional TLSOption resource name associated with the projected route.
    /// </summary>
    public string? TlsOptionsName { get; set; }

    /// <summary>
    /// Gets or sets the optional Kubernetes namespace that owns the referenced TLSOption.
    /// When omitted, the materializer falls back to the effective IngressRoute namespace.
    /// </summary>
    public string? TlsOptionsNamespace { get; set; }
}
