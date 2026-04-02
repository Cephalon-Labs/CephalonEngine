namespace Cephalon.Observability.HttpDependencies.Configuration;

/// <summary>
/// Describes one external HTTP dependency that should contribute to runtime health.
/// </summary>
public sealed class HttpDependencyDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpDependencyDefinition" /> class.
    /// </summary>
    public HttpDependencyDefinition()
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
    /// Gets or sets the absolute endpoint that should be probed for this dependency.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP method used for the probe request.
    /// </summary>
    public string Method { get; set; } = "GET";

    /// <summary>
    /// Gets or sets a value indicating whether this dependency is required for readiness.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the per-request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the explicit HTTP status codes that should be treated as healthy.
    /// </summary>
    public IReadOnlyList<int> ExpectedStatusCodes { get; set; } = Array.Empty<int>();
}
