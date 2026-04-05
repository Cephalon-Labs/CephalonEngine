namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one projection surface contributed to the active runtime.
/// </summary>
public sealed class ProjectionDescriptor
{
    /// <summary>
    /// Creates a new projection descriptor.
    /// </summary>
    /// <param name="id">The stable projection identifier.</param>
    /// <param name="displayName">The operator-facing projection name.</param>
    /// <param name="description">The human-readable projection description.</param>
    /// <param name="sourceModuleId">The module identifier that owns the projection.</param>
    /// <param name="targetStoreId">The logical target store or read-model identifier populated by the projection.</param>
    /// <param name="mode">The projection mode such as <c>synchronous</c>, <c>asynchronous</c>, or <c>rebuild</c>.</param>
    /// <param name="sourceContracts">Optional source contracts that can feed the projection.</param>
    /// <param name="tags">Optional descriptive tags associated with the projection.</param>
    /// <param name="metadata">Optional operator-facing metadata associated with the projection.</param>
    public ProjectionDescriptor(
        string id,
        string displayName,
        string description,
        string sourceModuleId,
        string targetStoreId,
        string mode = "asynchronous",
        IReadOnlyList<string>? sourceContracts = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Projection id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Projection display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Projection description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            throw new ArgumentException("Projection source module id is required.", nameof(sourceModuleId));
        }

        if (string.IsNullOrWhiteSpace(targetStoreId))
        {
            throw new ArgumentException("Projection target store id is required.", nameof(targetStoreId));
        }

        if (string.IsNullOrWhiteSpace(mode))
        {
            throw new ArgumentException("Projection mode is required.", nameof(mode));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        SourceModuleId = sourceModuleId.Trim();
        TargetStoreId = targetStoreId.Trim();
        Mode = mode.Trim();
        SourceContracts = Normalize(sourceContracts);
        Tags = Normalize(tags);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable projection identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing projection name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable projection description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the identifier of the module that owns the projection.
    /// </summary>
    public string SourceModuleId { get; }

    /// <summary>
    /// Gets the logical target store or read-model identifier populated by the projection.
    /// </summary>
    public string TargetStoreId { get; }

    /// <summary>
    /// Gets the projection mode.
    /// </summary>
    public string Mode { get; }

    /// <summary>
    /// Gets the optional source contracts that can feed the projection.
    /// </summary>
    public IReadOnlyList<string> SourceContracts { get; }

    /// <summary>
    /// Gets descriptive tags associated with the projection.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets operator-facing metadata associated with the projection.
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
