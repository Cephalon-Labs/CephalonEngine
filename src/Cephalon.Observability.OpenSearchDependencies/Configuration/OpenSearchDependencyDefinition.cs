namespace Cephalon.Observability.OpenSearchDependencies.Configuration;

/// <summary>
/// Describes one OpenSearch dependency that should contribute to runtime health.
/// </summary>
public sealed class OpenSearchDependencyDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenSearchDependencyDefinition" /> class.
    /// </summary>
    public OpenSearchDependencyDefinition()
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
    /// Gets or sets the absolute OpenSearch base URL or cluster-health endpoint that should be probed.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional index name or comma-delimited index list that should be checked through the cluster-health API.
    /// </summary>
    public string? Index { get; set; }

    /// <summary>
    /// Gets or sets the optional bearer token used for OpenSearch bearer-token authentication.
    /// </summary>
    public string? BearerToken { get; set; }

    /// <summary>
    /// Gets or sets the optional user name used for OpenSearch basic authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the optional password used for OpenSearch basic authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this dependency is required for readiness.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the per-request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;
}
