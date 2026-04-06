using Cephalon.Observability.DependencyHealth.Core.Configuration;

namespace Cephalon.Observability.ConsulDependencies.Configuration;

/// <summary>
/// Describes one Consul dependency that should contribute to runtime health.
/// </summary>
public sealed class ConsulDependencyDefinition : DependencyDefinitionBase
{
    /// <summary>Gets or sets the absolute Consul base URL or status endpoint that should be probed.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional Consul ACL token sent as the <c>X-Consul-Token</c> header.</summary>
    public string? AclToken { get; set; }

    /// <summary>Gets or sets the optional Consul datacenter name added as the <c>dc</c> query parameter.</summary>
    public string? Datacenter { get; set; }
}
