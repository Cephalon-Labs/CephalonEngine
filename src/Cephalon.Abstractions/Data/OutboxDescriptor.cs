namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one outbox surface contributed to the active runtime.
/// </summary>
public sealed class OutboxDescriptor
{
    /// <summary>
    /// Creates a new outbox descriptor.
    /// </summary>
    /// <param name="id">The stable outbox identifier.</param>
    /// <param name="displayName">The operator-facing outbox name.</param>
    /// <param name="description">The human-readable outbox description.</param>
    /// <param name="sourceModuleId">The module identifier that owns the outbox surface.</param>
    /// <param name="provider">The logical provider identifier that backs the outbox.</param>
    /// <param name="mode">The outbox mode such as <c>transactional-table</c> or <c>append-only-log</c>.</param>
    /// <param name="channelIds">Optional channel identifiers that this outbox is explicitly scoped to.</param>
    /// <param name="tags">Optional descriptive tags associated with the outbox.</param>
    /// <param name="metadata">Optional operator-facing metadata associated with the outbox.</param>
    public OutboxDescriptor(
        string id,
        string displayName,
        string description,
        string sourceModuleId,
        string provider,
        string mode = "transactional-store",
        IReadOnlyList<string>? channelIds = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Outbox id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Outbox display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Outbox description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            throw new ArgumentException("Outbox source module id is required.", nameof(sourceModuleId));
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("Outbox provider is required.", nameof(provider));
        }

        if (string.IsNullOrWhiteSpace(mode))
        {
            throw new ArgumentException("Outbox mode is required.", nameof(mode));
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
    /// Gets the stable outbox identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing outbox name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable outbox description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the identifier of the module that owns the outbox surface.
    /// </summary>
    public string SourceModuleId { get; }

    /// <summary>
    /// Gets the logical provider identifier that backs the outbox.
    /// </summary>
    public string Provider { get; }

    /// <summary>
    /// Gets the outbox mode.
    /// </summary>
    public string Mode { get; }

    /// <summary>
    /// Gets the optional channel identifiers that this outbox is explicitly scoped to.
    /// </summary>
    public IReadOnlyList<string> ChannelIds { get; }

    /// <summary>
    /// Gets descriptive tags associated with the outbox.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets operator-facing metadata associated with the outbox.
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
