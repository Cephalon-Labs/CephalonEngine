namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Describes one module-owned cell boundary visible to the active Cephalon runtime.
/// </summary>
public sealed class CellBoundaryDescriptor
{
    /// <summary>
    /// Creates a cell-boundary descriptor.
    /// </summary>
    /// <param name="id">The stable cell identifier.</param>
    /// <param name="sourceModuleId">The Cephalon module that owns this cell boundary.</param>
    /// <param name="displayName">The operator-facing cell name.</param>
    /// <param name="description">The human-readable description of the cell boundary.</param>
    /// <param name="blastRadius">The operator-facing blast-radius posture for this cell.</param>
    /// <param name="routingStrategy">The operator-facing routing strategy applied to this cell.</param>
    /// <param name="moduleIds">The module identifiers that belong to this cell boundary.</param>
    /// <param name="metadata">Optional operator-facing metadata.</param>
    public CellBoundaryDescriptor(
        string id,
        string sourceModuleId,
        string displayName,
        string description,
        string blastRadius,
        string routingStrategy,
        IReadOnlyList<string>? moduleIds = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Cell id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            throw new ArgumentException("Source module id is required.", nameof(sourceModuleId));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Cell display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Cell description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(blastRadius))
        {
            throw new ArgumentException("Cell blast radius is required.", nameof(blastRadius));
        }

        if (string.IsNullOrWhiteSpace(routingStrategy))
        {
            throw new ArgumentException("Cell routing strategy is required.", nameof(routingStrategy));
        }

        var normalizedSourceModuleId = sourceModuleId.Trim();

        Id = id.Trim();
        SourceModuleId = normalizedSourceModuleId;
        DisplayName = displayName.Trim();
        Description = description.Trim();
        BlastRadius = blastRadius.Trim();
        RoutingStrategy = routingStrategy.Trim();
        ModuleIds = NormalizeModuleIds(moduleIds, normalizedSourceModuleId);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable cell identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the module that owns this cell boundary.
    /// </summary>
    public string SourceModuleId { get; }

    /// <summary>
    /// Gets the operator-facing cell name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable description of the cell boundary.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the operator-facing blast-radius posture for this cell.
    /// </summary>
    public string BlastRadius { get; }

    /// <summary>
    /// Gets the operator-facing routing strategy for this cell.
    /// </summary>
    public string RoutingStrategy { get; }

    /// <summary>
    /// Gets the module identifiers that belong to this cell boundary.
    /// </summary>
    public IReadOnlyList<string> ModuleIds { get; }

    /// <summary>
    /// Gets optional operator-facing metadata for this cell boundary.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string[] NormalizeModuleIds(
        IReadOnlyList<string>? moduleIds,
        string sourceModuleId)
    {
        var normalized = moduleIds?
            .Where(static moduleId => !string.IsNullOrWhiteSpace(moduleId))
            .Select(static moduleId => moduleId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static moduleId => moduleId, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        if (!normalized.Contains(sourceModuleId, StringComparer.OrdinalIgnoreCase))
        {
            normalized.Add(sourceModuleId);
        }

        return normalized
            .OrderBy(static moduleId => moduleId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
