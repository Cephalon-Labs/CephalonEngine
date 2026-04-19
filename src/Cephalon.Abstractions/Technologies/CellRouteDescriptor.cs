namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Describes one module-owned cell-to-cell routing and governance answer visible to the active runtime.
/// </summary>
public sealed class CellRouteDescriptor
{
    /// <summary>
    /// Creates a cell-route descriptor.
    /// </summary>
    /// <param name="id">The stable cell-route identifier.</param>
    /// <param name="sourceModuleId">The Cephalon module that owns this cell route.</param>
    /// <param name="sourceCellId">The source cell identifier.</param>
    /// <param name="targetCellId">The target cell identifier.</param>
    /// <param name="displayName">The operator-facing route name.</param>
    /// <param name="description">The human-readable description of the cell route.</param>
    /// <param name="routingStrategy">The operator-facing routing strategy used for this route.</param>
    /// <param name="governanceMode">The operator-facing governance posture applied to this route.</param>
    /// <param name="transportIds">Optional transport identifiers associated with this route.</param>
    /// <param name="requiredCapabilityKey">An optional capability key required to use this route.</param>
    /// <param name="metadata">Optional operator-facing metadata.</param>
    public CellRouteDescriptor(
        string id,
        string sourceModuleId,
        string sourceCellId,
        string targetCellId,
        string displayName,
        string description,
        string routingStrategy,
        string governanceMode,
        IReadOnlyList<string>? transportIds = null,
        string? requiredCapabilityKey = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Cell route id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            throw new ArgumentException("Source module id is required.", nameof(sourceModuleId));
        }

        if (string.IsNullOrWhiteSpace(sourceCellId))
        {
            throw new ArgumentException("Source cell id is required.", nameof(sourceCellId));
        }

        if (string.IsNullOrWhiteSpace(targetCellId))
        {
            throw new ArgumentException("Target cell id is required.", nameof(targetCellId));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Cell route display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Cell route description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(routingStrategy))
        {
            throw new ArgumentException("Cell route routing strategy is required.", nameof(routingStrategy));
        }

        if (string.IsNullOrWhiteSpace(governanceMode))
        {
            throw new ArgumentException("Cell route governance mode is required.", nameof(governanceMode));
        }

        Id = id.Trim();
        SourceModuleId = sourceModuleId.Trim();
        SourceCellId = sourceCellId.Trim();
        TargetCellId = targetCellId.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        RoutingStrategy = routingStrategy.Trim();
        GovernanceMode = governanceMode.Trim();
        TransportIds = NormalizeTransportIds(transportIds);
        RequiredCapabilityKey = string.IsNullOrWhiteSpace(requiredCapabilityKey)
            ? null
            : requiredCapabilityKey.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable cell-route identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the module that owns this cell route.
    /// </summary>
    public string SourceModuleId { get; }

    /// <summary>
    /// Gets the source cell identifier.
    /// </summary>
    public string SourceCellId { get; }

    /// <summary>
    /// Gets the target cell identifier.
    /// </summary>
    public string TargetCellId { get; }

    /// <summary>
    /// Gets the operator-facing route name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable description of the cell route.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the operator-facing routing strategy used for this route.
    /// </summary>
    public string RoutingStrategy { get; }

    /// <summary>
    /// Gets the operator-facing governance posture applied to this route.
    /// </summary>
    public string GovernanceMode { get; }

    /// <summary>
    /// Gets the normalized transport identifiers associated with this route.
    /// </summary>
    public IReadOnlyList<string> TransportIds { get; }

    /// <summary>
    /// Gets the optional capability key required to use this route.
    /// </summary>
    public string? RequiredCapabilityKey { get; }

    /// <summary>
    /// Gets optional operator-facing metadata for this route.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string[] NormalizeTransportIds(IReadOnlyList<string>? transportIds)
    {
        return transportIds?
            .Where(static transportId => !string.IsNullOrWhiteSpace(transportId))
            .Select(static transportId => transportId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static transportId => transportId, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
