namespace Cephalon.Edge.Services;

/// <summary>
/// Describes an edge node that can be surfaced through the edge runtime pack.
/// </summary>
public sealed class EdgeNodeDescriptor
{
    /// <summary>
    /// Creates a new edge node descriptor.
    /// </summary>
    /// <param name="id">The stable node identifier.</param>
    /// <param name="displayName">The operator-facing node name.</param>
    /// <param name="description">The human-readable description of the node.</param>
    /// <param name="tags">Optional tags that classify the node.</param>
    public EdgeNodeDescriptor(
        string id,
        string displayName,
        string description,
        IReadOnlyList<string>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Node id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Node display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Node description is required.", nameof(description));
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
    /// Gets the stable node identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing display name for the node.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable description of the node.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the normalized tag set associated with the node.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }
}
