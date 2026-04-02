namespace Cephalon.Abstractions.Capabilities;

/// <summary>
/// Describes a capability contributed by a module or package.
/// </summary>
public sealed class Capability
{
    /// <summary>
    /// Creates a capability descriptor.
    /// </summary>
    /// <param name="key">The stable capability key.</param>
    /// <param name="displayName">The human-readable capability name.</param>
    /// <param name="description">The capability description.</param>
    /// <param name="metadata">Optional capability metadata.</param>
    public Capability(
        string key,
        string displayName,
        string description,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Capability key is required.", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Capability display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Capability description is required.", nameof(description));
        }

        Key = key.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable capability key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the human-readable capability name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the capability description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets optional capability metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
