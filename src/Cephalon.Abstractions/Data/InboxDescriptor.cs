namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one inbox surface contributed to the active runtime.
/// </summary>
public sealed class InboxDescriptor
{
    /// <summary>
    /// Creates a new inbox descriptor.
    /// </summary>
    /// <param name="id">The stable inbox identifier.</param>
    /// <param name="displayName">The operator-facing inbox name.</param>
    /// <param name="description">The human-readable inbox description.</param>
    /// <param name="sourceModuleId">The module identifier that owns the inbox surface.</param>
    /// <param name="provider">The logical provider identifier that backs the inbox.</param>
    /// <param name="mode">The inbox mode such as <c>processed-message-table</c> or <c>durable-log</c>.</param>
    /// <param name="channelIds">Optional channel identifiers that this inbox is explicitly scoped to.</param>
    /// <param name="tags">Optional descriptive tags associated with the inbox.</param>
    /// <param name="metadata">Optional operator-facing metadata associated with the inbox.</param>
    public InboxDescriptor(
        string id,
        string displayName,
        string description,
        string sourceModuleId,
        string provider,
        string mode = "processed-message-store",
        IReadOnlyList<string>? channelIds = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Inbox id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Inbox display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Inbox description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            throw new ArgumentException("Inbox source module id is required.", nameof(sourceModuleId));
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("Inbox provider is required.", nameof(provider));
        }

        if (string.IsNullOrWhiteSpace(mode))
        {
            throw new ArgumentException("Inbox mode is required.", nameof(mode));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        SourceModuleId = sourceModuleId.Trim();
        Provider = provider.Trim();
        Mode = mode.Trim();
        ChannelIds = Normalize(channelIds);
        Tags = Normalize(tags);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable inbox identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing inbox name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable inbox description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the identifier of the module that owns the inbox surface.
    /// </summary>
    public string SourceModuleId { get; }

    /// <summary>
    /// Gets the logical provider identifier that backs the inbox.
    /// </summary>
    public string Provider { get; }

    /// <summary>
    /// Gets the inbox mode.
    /// </summary>
    public string Mode { get; }

    /// <summary>
    /// Gets the optional channel identifiers that this inbox is explicitly scoped to.
    /// </summary>
    public IReadOnlyList<string> ChannelIds { get; }

    /// <summary>
    /// Gets descriptive tags associated with the inbox.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets operator-facing metadata associated with the inbox.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string[] Normalize(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
