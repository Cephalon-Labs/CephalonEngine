using Cephalon.Observability.DependencyHealth.Core.Configuration;

namespace Cephalon.Observability.OpenSearchDependencies.Configuration;

/// <summary>
/// Describes one OpenSearch dependency that should contribute to runtime health.
/// </summary>
public sealed class OpenSearchDependencyDefinition : DependencyDefinitionBase
{
    /// <summary>Gets or sets the absolute OpenSearch base URL or cluster-health endpoint that should be probed.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional index name or comma-delimited index list that should be checked through the cluster-health API.</summary>
    public string? Index { get; set; }

    /// <summary>Gets or sets the optional bearer token used for OpenSearch bearer-token authentication.</summary>
    public string? BearerToken { get; set; }

    /// <summary>Gets or sets the optional user name used for OpenSearch basic authentication.</summary>
    public string? Username { get; set; }

    /// <summary>Gets or sets the optional password used for OpenSearch basic authentication.</summary>
    public string? Password { get; set; }
}
