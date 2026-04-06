using Cephalon.Observability.DependencyHealth.Core.Configuration;

namespace Cephalon.Observability.RedisDependencies.Configuration;

/// <summary>
/// Describes one Redis dependency that should contribute to runtime health.
/// </summary>
public sealed class RedisDependencyDefinition : DependencyDefinitionBase
{
    /// <summary>
    /// Gets or sets the Redis host name or IP address to probe.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Redis TCP port.
    /// </summary>
    public int Port { get; set; } = 6379;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisDependencyDefinition" /> class.
    /// </summary>
    public RedisDependencyDefinition()
    {
    }
}
