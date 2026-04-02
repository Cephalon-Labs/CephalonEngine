namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one transport exposed by an app.
/// </summary>
public sealed class TransportDescriptor
{
    /// <summary>
    /// Creates a transport descriptor.
    /// </summary>
    /// <param name="id">The stable transport identifier.</param>
    /// <param name="displayName">The human-readable transport name.</param>
    /// <param name="description">The transport description.</param>
    /// <param name="features">The features supported by the transport.</param>
    /// <param name="tags">The tags associated with the transport.</param>
    /// <param name="metadata">Optional transport metadata.</param>
    public TransportDescriptor(
        string id,
        string displayName,
        string description,
        TransportFeatures features,
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Transport id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Transport display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Transport description is required.", nameof(description));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Features = features;
        Tags = Normalize(tags);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable transport identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the human-readable transport name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the transport description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the features supported by the transport.
    /// </summary>
    public TransportFeatures Features { get; }

    /// <summary>
    /// Gets the tags associated with the transport.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets optional transport metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string[] Normalize(IReadOnlyList<string>? values)
    {
        return values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
