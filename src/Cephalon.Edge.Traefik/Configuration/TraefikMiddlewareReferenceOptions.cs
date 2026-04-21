namespace Cephalon.Edge.Traefik.Configuration;

/// <summary>
/// Configures one Traefik middleware reference that should attach to a projected IngressRoute rule.
/// </summary>
public sealed class TraefikMiddlewareReferenceOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TraefikMiddlewareReferenceOptions" /> class.
    /// </summary>
    public TraefikMiddlewareReferenceOptions()
    {
    }

    /// <summary>
    /// Gets or sets the Traefik middleware resource name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the optional Kubernetes namespace that owns the middleware.
    /// When omitted, the materializer falls back to the effective IngressRoute namespace.
    /// </summary>
    public string? Namespace { get; set; }
}
