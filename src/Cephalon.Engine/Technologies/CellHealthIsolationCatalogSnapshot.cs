using Cephalon.Abstractions.Technologies;

namespace Cephalon.Engine.Technologies;

internal sealed class CellHealthIsolationCatalogSnapshot : ICellHealthIsolationCatalog
{
    private readonly CellHealthIsolationDescriptor[] healthIsolations;
    private readonly Dictionary<string, CellHealthIsolationDescriptor> healthIsolationsById;
    private readonly Dictionary<string, IReadOnlyList<CellHealthIsolationDescriptor>> healthIsolationsBySourceModule;
    private readonly Dictionary<string, IReadOnlyList<CellHealthIsolationDescriptor>> healthIsolationsByCellId;
    private readonly Dictionary<string, IReadOnlyList<CellHealthIsolationDescriptor>> healthIsolationsByDependencyId;

    public CellHealthIsolationCatalogSnapshot(IEnumerable<CellHealthIsolationDescriptor> healthIsolations)
    {
        ArgumentNullException.ThrowIfNull(healthIsolations);

        this.healthIsolations = healthIsolations
            .OrderBy(static isolation => isolation.CellId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static isolation => isolation.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static isolation => isolation.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static isolation => isolation.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        ValidateDuplicateIds(this.healthIsolations);

        healthIsolationsById = this.healthIsolations.ToDictionary(static isolation => isolation.Id, StringComparer.OrdinalIgnoreCase);
        healthIsolationsBySourceModule = CreateIndex(this.healthIsolations, static isolation => isolation.SourceModuleId);
        healthIsolationsByCellId = CreateIndex(this.healthIsolations, static isolation => isolation.CellId);
        healthIsolationsByDependencyId = this.healthIsolations
            .SelectMany(static isolation => isolation.DependencyIds.Select(dependencyId => new KeyValuePair<string, CellHealthIsolationDescriptor>(dependencyId, isolation)))
            .GroupBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<CellHealthIsolationDescriptor>)group
                    .Select(static pair => pair.Value)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<CellHealthIsolationDescriptor> HealthIsolations => healthIsolations;

    public CellHealthIsolationDescriptor? GetById(string healthIsolationId)
    {
        if (string.IsNullOrWhiteSpace(healthIsolationId))
        {
            return null;
        }

        return healthIsolationsById.TryGetValue(healthIsolationId.Trim(), out var healthIsolation)
            ? healthIsolation
            : null;
    }

    public IReadOnlyList<CellHealthIsolationDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return healthIsolationsBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<CellHealthIsolationDescriptor> GetByCellId(string cellId)
    {
        if (string.IsNullOrWhiteSpace(cellId))
        {
            return [];
        }

        return healthIsolationsByCellId.TryGetValue(cellId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<CellHealthIsolationDescriptor> GetByDependencyId(string dependencyId)
    {
        if (string.IsNullOrWhiteSpace(dependencyId))
        {
            return [];
        }

        return healthIsolationsByDependencyId.TryGetValue(dependencyId.Trim(), out var matches)
            ? matches
            : [];
    }

    private static Dictionary<string, IReadOnlyList<CellHealthIsolationDescriptor>> CreateIndex(
        IReadOnlyList<CellHealthIsolationDescriptor> healthIsolations,
        Func<CellHealthIsolationDescriptor, string> keySelector)
    {
        return healthIsolations
            .GroupBy(keySelector, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<CellHealthIsolationDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static void ValidateDuplicateIds(IReadOnlyList<CellHealthIsolationDescriptor> healthIsolations)
    {
        var duplicateId = healthIsolations
            .GroupBy(static isolation => isolation.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicateId is null)
        {
            return;
        }

        var owners = duplicateId
            .Select(static isolation => isolation.SourceModuleId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase);

        throw new InvalidOperationException(
            $"Cell health isolation '{duplicateId.Key}' is registered multiple times by: {string.Join(", ", owners)}.");
    }
}
