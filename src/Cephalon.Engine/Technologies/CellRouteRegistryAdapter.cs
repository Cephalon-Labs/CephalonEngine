using Cephalon.Abstractions.Technologies;

namespace Cephalon.Engine.Technologies;

internal sealed class CellRouteRegistryAdapter(
    string moduleId,
    List<CellRouteDescriptor> cellRoutes) : ICellRouteRegistry
{
    public void Add(CellRouteDescriptor cellRoute)
    {
        ArgumentNullException.ThrowIfNull(cellRoute);

        if (!string.Equals(cellRoute.SourceModuleId, moduleId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Cell route '{cellRoute.Id}' declared source module '{cellRoute.SourceModuleId}', but it was contributed by module '{moduleId}'.");
        }

        cellRoutes.Add(cellRoute);
    }
}
