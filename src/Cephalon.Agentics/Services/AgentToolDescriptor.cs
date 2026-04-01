namespace Cephalon.Agentics.Services;

/// <summary>
/// Describes a tool that can be surfaced through the agentic runtime pack.
/// </summary>
public sealed class AgentToolDescriptor
{
    /// <summary>
    /// Creates a new agent tool descriptor.
    /// </summary>
    /// <param name="id">The stable tool identifier.</param>
    /// <param name="displayName">The operator-facing tool name.</param>
    /// <param name="description">The human-readable description of the tool.</param>
    /// <param name="tags">Optional tags that classify the tool.</param>
    public AgentToolDescriptor(
        string id,
        string displayName,
        string description,
        IReadOnlyList<string>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Tool id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Tool display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Tool description is required.", nameof(description));
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
    /// Gets the stable tool identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing display name for the tool.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable description of the tool.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the normalized tag set associated with the tool.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }
}
