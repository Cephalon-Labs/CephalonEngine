namespace Cephalon.Observability.ElasticsearchDependencies.Configuration;

/// <summary>
/// Describes one Elasticsearch dependency that should contribute to runtime health.
/// </summary>
public sealed class ElasticsearchDependencyDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticsearchDependencyDefinition" /> class.
    /// </summary>
    public ElasticsearchDependencyDefinition()
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
    /// Gets or sets the absolute Elasticsearch base URL or cluster-health endpoint that should be probed.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional API key used for Elasticsearch API-key authentication.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the optional bearer token used for Elasticsearch bearer-token authentication.
    /// </summary>
    public string? BearerToken { get; set; }

    /// <summary>
    /// Gets or sets the optional user name used for Elasticsearch basic authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the optional password used for Elasticsearch basic authentication.
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
