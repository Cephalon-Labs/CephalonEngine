namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one module-owned data product surface contributed to the active runtime.
/// </summary>
public sealed class DataProductDescriptor
{
    /// <summary>
    /// Creates a new data product descriptor.
    /// </summary>
    /// <param name="id">The stable data product identifier.</param>
    /// <param name="displayName">The operator-facing data product name.</param>
    /// <param name="description">The human-readable data product description.</param>
    /// <param name="sourceModuleId">The module identifier that owns the data product.</param>
    /// <param name="domainId">The stable domain or bounded-context identifier for the data product.</param>
    /// <param name="contractId">The stable query or contract identifier exposed by the data product.</param>
    /// <param name="mode">The access mode such as <c>query</c>, <c>snapshot</c>, or <c>feed</c>.</param>
    /// <param name="tags">Optional descriptive tags associated with the data product.</param>
    /// <param name="metadata">Optional operator-facing metadata associated with the data product.</param>
    public DataProductDescriptor(
        string id,
        string displayName,
        string description,
        string sourceModuleId,
        string domainId,
        string contractId,
        string mode = "query",
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Data product id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Data product display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Data product description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            throw new ArgumentException("Data product source module id is required.", nameof(sourceModuleId));
        }

        if (string.IsNullOrWhiteSpace(domainId))
        {
            throw new ArgumentException("Data product domain id is required.", nameof(domainId));
        }

        if (string.IsNullOrWhiteSpace(contractId))
        {
            throw new ArgumentException("Data product contract id is required.", nameof(contractId));
        }

        if (string.IsNullOrWhiteSpace(mode))
        {
            throw new ArgumentException("Data product mode is required.", nameof(mode));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        SourceModuleId = sourceModuleId.Trim();
        DomainId = domainId.Trim();
        ContractId = contractId.Trim();
        Mode = mode.Trim();
        Tags = Normalize(tags);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable data product identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing data product name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable data product description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the identifier of the module that owns the data product.
    /// </summary>
    public string SourceModuleId { get; }

    /// <summary>
    /// Gets the stable domain or bounded-context identifier for the data product.
    /// </summary>
    public string DomainId { get; }

    /// <summary>
    /// Gets the stable query or contract identifier exposed by the data product.
    /// </summary>
    public string ContractId { get; }

    /// <summary>
    /// Gets the declared access mode for the data product.
    /// </summary>
    public string Mode { get; }

    /// <summary>
    /// Gets descriptive tags associated with the data product.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets operator-facing metadata associated with the data product.
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
