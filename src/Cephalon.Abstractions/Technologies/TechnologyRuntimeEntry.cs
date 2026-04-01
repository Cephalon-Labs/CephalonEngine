using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Describes one runtime-visible entry inside a technology surface.
/// </summary>
public sealed class TechnologyRuntimeEntry
{
    /// <summary>
    /// Creates a new technology runtime entry.
    /// </summary>
    /// <param name="id">The stable entry identifier.</param>
    /// <param name="displayName">The operator-facing display name.</param>
    /// <param name="description">A human-readable description of the entry.</param>
    /// <param name="metadata">Additional metadata associated with the entry.</param>
    [JsonConstructor]
    public TechnologyRuntimeEntry(
        string id,
        string displayName,
        string description,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Entry id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Entry display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Entry description is required.", nameof(description));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Metadata = metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable identifier for the entry.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing display name for the entry.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable description of the entry.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets additional metadata projected for the entry.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
