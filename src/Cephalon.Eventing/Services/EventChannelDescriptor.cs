namespace Cephalon.Eventing.Services;

/// <summary>
/// Describes an event channel that can be surfaced through the eventing runtime pack.
/// </summary>
public sealed class EventChannelDescriptor
{
    /// <summary>
    /// Creates a new event channel descriptor.
    /// </summary>
    /// <param name="id">The stable channel identifier.</param>
    /// <param name="displayName">The operator-facing channel name.</param>
    /// <param name="description">The human-readable description of the channel.</param>
    /// <param name="tags">Optional tags that classify the channel.</param>
    public EventChannelDescriptor(
        string id,
        string displayName,
        string description,
        IReadOnlyList<string>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Channel id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Channel display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Channel description is required.", nameof(description));
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
    /// Gets the stable channel identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing display name for the channel.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable description of the channel.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the normalized tag set associated with the channel.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }
}
