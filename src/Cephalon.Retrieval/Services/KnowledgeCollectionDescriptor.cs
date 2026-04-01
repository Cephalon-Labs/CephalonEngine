namespace Cephalon.Retrieval.Services;

/// <summary>
/// Describes a knowledge collection that can be surfaced through the retrieval runtime pack.
/// </summary>
public sealed class KnowledgeCollectionDescriptor
{
    /// <summary>
    /// Creates a new knowledge collection descriptor.
    /// </summary>
    /// <param name="id">The stable collection identifier.</param>
    /// <param name="displayName">The operator-facing collection name.</param>
    /// <param name="description">The human-readable description of the collection.</param>
    /// <param name="tags">Optional tags that classify the collection.</param>
    public KnowledgeCollectionDescriptor(
        string id,
        string displayName,
        string description,
        IReadOnlyList<string>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Collection id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Collection display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Collection description is required.", nameof(description));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Tags = tags?
            .Where(static tag => !string.IsNullOrWhiteSpace(tag))
            .Select(static tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    /// <summary>
    /// Gets the stable collection identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing display name for the collection.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable description of the collection.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the normalized tag set associated with the collection.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }
}
