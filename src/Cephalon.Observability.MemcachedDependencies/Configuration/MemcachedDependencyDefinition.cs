using Cephalon.Observability.DependencyHealth.Core.Configuration;

namespace Cephalon.Observability.MemcachedDependencies.Configuration;

/// <summary>
/// Describes one Memcached dependency that should contribute to runtime health.
/// </summary>
public sealed class MemcachedDependencyDefinition : DependencyDefinitionBase
{
    /// <summary>Gets or sets the Memcached host name or IP address to probe.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Gets or sets the Memcached TCP port.</summary>
    public int Port { get; set; } = 11211;
}
