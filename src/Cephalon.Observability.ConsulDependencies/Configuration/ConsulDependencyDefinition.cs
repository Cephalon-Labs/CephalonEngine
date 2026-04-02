namespace Cephalon.Observability.ConsulDependencies.Configuration;

/// <summary>
/// Describes one Consul dependency that should contribute to runtime health.
/// </summary>
public sealed class ConsulDependencyDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulDependencyDefinition" /> class.
    /// </summary>
    public ConsulDependencyDefinition()
    {
    }

    /// <summary>
    /// Gets or sets the stable dependency identifier surfaced through runtime health endpoints.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable dependency name shown to operators.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the absolute Consul base URL or status endpoint that should be probed.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional Consul ACL token sent as the <c>X-Consul-Token</c> header.
    /// </summary>
    public string? AclToken { get; set; }

    /// <summary>
    /// Gets or sets the optional Consul datacenter name added as the <c>dc</c> query parameter.
    /// </summary>
    public string? Datacenter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this dependency is required for readiness.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the per-request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;
}
