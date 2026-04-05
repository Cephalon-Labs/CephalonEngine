namespace Cephalon.Abstractions.Audit;

/// <summary>
/// Describes one audit store surface contributed to the active runtime.
/// </summary>
public sealed class AuditStoreDescriptor
{
    /// <summary>
    /// Creates a new audit-store descriptor.
    /// </summary>
    /// <param name="id">The stable audit-store identifier.</param>
    /// <param name="displayName">The operator-facing audit-store name.</param>
    /// <param name="description">The human-readable audit-store description.</param>
    /// <param name="sourceModuleId">The module identifier that owns the audit-store surface.</param>
    /// <param name="provider">The logical provider identifier that backs the audit-store surface.</param>
    /// <param name="mode">The audit-store mode such as <c>volatile-buffer</c> or <c>transactional-table</c>.</param>
    /// <param name="tags">Optional descriptive tags associated with the audit store.</param>
    /// <param name="metadata">Optional operator-facing metadata associated with the audit store.</param>
    public AuditStoreDescriptor(
        string id,
        string displayName,
        string description,
        string sourceModuleId,
        string provider,
        string mode = "application-managed",
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Audit store id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Audit store display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Audit store description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            throw new ArgumentException("Audit store source module id is required.", nameof(sourceModuleId));
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("Audit store provider is required.", nameof(provider));
        }

        if (string.IsNullOrWhiteSpace(mode))
        {
            throw new ArgumentException("Audit store mode is required.", nameof(mode));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        SourceModuleId = sourceModuleId.Trim();
        Provider = provider.Trim();
        Mode = mode.Trim();
        Tags = Normalize(tags);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable audit-store identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing audit-store name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable audit-store description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the identifier of the module that owns the audit-store surface.
    /// </summary>
    public string SourceModuleId { get; }

    /// <summary>
    /// Gets the logical provider identifier that backs the audit-store surface.
    /// </summary>
    public string Provider { get; }

    /// <summary>
    /// Gets the audit-store mode.
    /// </summary>
    public string Mode { get; }

    /// <summary>
    /// Gets descriptive tags associated with the audit store.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets operator-facing metadata associated with the audit store.
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
