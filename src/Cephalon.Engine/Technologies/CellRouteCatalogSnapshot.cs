using Cephalon.Abstractions.Technologies;

namespace Cephalon.Engine.Technologies;

internal sealed class CellRouteCatalogSnapshot : ICellRouteCatalog
{
    private readonly CellRouteDescriptor[] routes;
    private readonly Dictionary<string, CellRouteDescriptor> routesById;
    private readonly Dictionary<string, IReadOnlyList<CellRouteDescriptor>> routesBySourceModule;
    private readonly Dictionary<string, IReadOnlyList<CellRouteDescriptor>> routesBySourceCellId;
    private readonly Dictionary<string, IReadOnlyList<CellRouteDescriptor>> routesByTargetCellId;

    public CellRouteCatalogSnapshot(IEnumerable<CellRouteDescriptor> routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        this.routes = routes
            .OrderBy(static route => route.SourceCellId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static route => route.TargetCellId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static route => route.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static route => route.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        ValidateDuplicateIds(this.routes);

        routesById = this.routes.ToDictionary(static route => route.Id, StringComparer.OrdinalIgnoreCase);
        routesBySourceModule = CreateIndex(this.routes, static route => route.SourceModuleId);
        routesBySourceCellId = CreateIndex(this.routes, static route => route.SourceCellId);
        routesByTargetCellId = CreateIndex(this.routes, static route => route.TargetCellId);
    }

    public IReadOnlyList<CellRouteDescriptor> Routes => routes;

    public CellRouteDescriptor? GetById(string routeId)
    {
        if (string.IsNullOrWhiteSpace(routeId))
        {
            return null;
        }

        return routesById.TryGetValue(routeId.Trim(), out var route)
            ? route
            : null;
    }

    public IReadOnlyList<CellRouteDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return routesBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<CellRouteDescriptor> GetBySourceCellId(string sourceCellId)
    {
        if (string.IsNullOrWhiteSpace(sourceCellId))
        {
            return [];
        }

        return routesBySourceCellId.TryGetValue(sourceCellId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<CellRouteDescriptor> GetByTargetCellId(string targetCellId)
    {
        if (string.IsNullOrWhiteSpace(targetCellId))
        {
            return [];
        }

        return routesByTargetCellId.TryGetValue(targetCellId.Trim(), out var matches)
            ? matches
            : [];
    }

    private static Dictionary<string, IReadOnlyList<CellRouteDescriptor>> CreateIndex(
        IReadOnlyList<CellRouteDescriptor> routes,
        Func<CellRouteDescriptor, string> keySelector)
    {
        return routes
            .GroupBy(keySelector, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<CellRouteDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static void ValidateDuplicateIds(IReadOnlyList<CellRouteDescriptor> routes)
    {
        var duplicateId = routes
            .GroupBy(static route => route.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicateId is null)
        {
            return;
        }

        var owners = duplicateId
            .Select(static route => route.SourceModuleId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase);

        throw new InvalidOperationException(
            $"Cell route '{duplicateId.Key}' is registered multiple times by: {string.Join(", ", owners)}.");
    }
}
