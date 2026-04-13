namespace Cephalon.Abstractions.Patterns;

/// <summary>
/// Describes one request that should be evaluated by a strangler-fig router.
/// </summary>
public sealed class StranglerFigRequest
{
    /// <summary>
    /// Creates a new strangler-fig routing request.
    /// </summary>
    /// <param name="path">The request path or absolute URI that should be evaluated.</param>
    /// <param name="method">The request method to evaluate, such as <c>GET</c> or <c>POST</c>.</param>
    /// <param name="metadata">Optional host-specific metadata that can accompany the request.</param>
    public StranglerFigRequest(
        string path,
        string method,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Request path is required.", nameof(path));
        }

        if (string.IsNullOrWhiteSpace(method))
        {
            throw new ArgumentException("Request method is required.", nameof(method));
        }

        Path = path.Trim();
        Method = method.Trim().ToUpperInvariant();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the request path or absolute URI that should be evaluated.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the normalized request method.
    /// </summary>
    public string Method { get; }

    /// <summary>
    /// Gets optional host-specific metadata that accompanied the request.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
