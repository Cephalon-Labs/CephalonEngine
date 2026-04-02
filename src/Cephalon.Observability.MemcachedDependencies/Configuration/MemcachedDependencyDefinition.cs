namespace Cephalon.Observability.MemcachedDependencies.Configuration;

/// <summary>
/// Describes one Memcached dependency that should contribute to runtime health.
/// </summary>
public sealed class MemcachedDependencyDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemcachedDependencyDefinition" /> class.
    /// </summary>
    public MemcachedDependencyDefinition()
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
    /// Gets or sets the Memcached host name or IP address to probe.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Memcached TCP port.
    /// </summary>
    public int Port { get; set; } = 11211;

    /// <summary>
    /// Gets or sets a value indicating whether this dependency is required for readiness.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the per-probe timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;
}
