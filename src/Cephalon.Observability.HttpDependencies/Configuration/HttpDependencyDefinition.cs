using Cephalon.Observability.DependencyHealth.Core.Configuration;

namespace Cephalon.Observability.HttpDependencies.Configuration;

/// <summary>
/// Describes one external HTTP dependency that should contribute to runtime health.
/// </summary>
public sealed class HttpDependencyDefinition : DependencyDefinitionBase
{
    /// <summary>
    /// Gets or sets the absolute endpoint that should be probed for this dependency.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP method used for the probe request.
    /// </summary>
    public string Method { get; set; } = "GET";

    /// <summary>
    /// Gets or sets the explicit HTTP status codes that should be treated as healthy.
    /// </summary>
    public IReadOnlyList<int> ExpectedStatusCodes { get; set; } = Array.Empty<int>();
}
