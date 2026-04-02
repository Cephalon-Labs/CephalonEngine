namespace Cephalon.Observability.RedisDependencies.Configuration;

/// <summary>
/// Describes one Redis dependency that should contribute to runtime health.
/// </summary>
public sealed class RedisDependencyDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RedisDependencyDefinition" /> class.
    /// </summary>
    public RedisDependencyDefinition()
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
    /// Gets or sets the Redis host name or IP address to probe.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Redis TCP port.
    /// </summary>
    public int Port { get; set; } = 6379;

    /// <summary>
    /// Gets or sets a value indicating whether this dependency is required for readiness.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the per-probe timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the optional Redis ACL user name used for authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the optional Redis password used for authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the optional Redis logical database index to select before pinging.
    /// </summary>
    public int? Database { get; set; }
}
