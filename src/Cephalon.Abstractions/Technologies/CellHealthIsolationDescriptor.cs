namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Describes one module-owned cell health-isolation answer visible to the active runtime.
/// </summary>
public sealed class CellHealthIsolationDescriptor
{
    /// <summary>
    /// Creates a cell health-isolation descriptor.
    /// </summary>
    /// <param name="id">The stable health-isolation identifier.</param>
    /// <param name="sourceModuleId">The Cephalon module that owns this health-isolation answer.</param>
    /// <param name="cellId">The cell identifier governed by this health-isolation answer.</param>
    /// <param name="displayName">The operator-facing health-isolation name.</param>
    /// <param name="description">The human-readable description of the health-isolation posture.</param>
    /// <param name="failureIsolationMode">The operator-facing failure-isolation mode for this cell.</param>
    /// <param name="readinessScope">The operator-facing readiness scope used for this cell.</param>
    /// <param name="restartScope">The operator-facing restart scope used for this cell.</param>
    /// <param name="dependencyIds">Optional dependency identifiers associated with this health-isolation answer.</param>
    /// <param name="metadata">Optional operator-facing metadata.</param>
    public CellHealthIsolationDescriptor(
        string id,
        string sourceModuleId,
        string cellId,
        string displayName,
        string description,
        string failureIsolationMode,
        string readinessScope,
        string restartScope,
        IReadOnlyList<string>? dependencyIds = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Cell health-isolation id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            throw new ArgumentException("Source module id is required.", nameof(sourceModuleId));
        }

        if (string.IsNullOrWhiteSpace(cellId))
        {
            throw new ArgumentException("Cell id is required.", nameof(cellId));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Health-isolation display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Health-isolation description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(failureIsolationMode))
        {
            throw new ArgumentException("Failure isolation mode is required.", nameof(failureIsolationMode));
        }

        if (string.IsNullOrWhiteSpace(readinessScope))
        {
            throw new ArgumentException("Readiness scope is required.", nameof(readinessScope));
        }

        if (string.IsNullOrWhiteSpace(restartScope))
        {
            throw new ArgumentException("Restart scope is required.", nameof(restartScope));
        }

        Id = id.Trim();
        SourceModuleId = sourceModuleId.Trim();
        CellId = cellId.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        FailureIsolationMode = failureIsolationMode.Trim();
        ReadinessScope = readinessScope.Trim();
        RestartScope = restartScope.Trim();
        DependencyIds = NormalizeDependencyIds(dependencyIds);
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable health-isolation identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the module that owns this health-isolation answer.
    /// </summary>
    public string SourceModuleId { get; }

    /// <summary>
    /// Gets the cell identifier governed by this health-isolation answer.
    /// </summary>
    public string CellId { get; }

    /// <summary>
    /// Gets the operator-facing health-isolation name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable description of the health-isolation posture.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the operator-facing failure-isolation mode for this cell.
    /// </summary>
    public string FailureIsolationMode { get; }

    /// <summary>
    /// Gets the operator-facing readiness scope for this cell.
    /// </summary>
    public string ReadinessScope { get; }

    /// <summary>
    /// Gets the operator-facing restart scope for this cell.
    /// </summary>
    public string RestartScope { get; }

    /// <summary>
    /// Gets the normalized dependency identifiers associated with this health-isolation answer.
    /// </summary>
    public IReadOnlyList<string> DependencyIds { get; }

    /// <summary>
    /// Gets optional operator-facing metadata for this health-isolation answer.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string[] NormalizeDependencyIds(IReadOnlyList<string>? dependencyIds)
    {
        return dependencyIds?
            .Where(static dependencyId => !string.IsNullOrWhiteSpace(dependencyId))
            .Select(static dependencyId => dependencyId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static dependencyId => dependencyId, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
