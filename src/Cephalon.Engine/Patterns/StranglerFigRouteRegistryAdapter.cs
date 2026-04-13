using Cephalon.Abstractions.Patterns;

namespace Cephalon.Engine.Patterns;

internal sealed class StranglerFigRouteRegistryAdapter(
    string moduleId,
    List<StranglerFigRouteDescriptor> routes) : IStranglerFigRouteRegistry
{
    public void Add(StranglerFigRouteDescriptor route)
    {
        ArgumentNullException.ThrowIfNull(route);

        if (!string.Equals(route.SourceModuleId, moduleId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Strangler-fig route '{route.Id}' declared source module '{route.SourceModuleId}', but it was contributed by module '{moduleId}'.");
        }

        routes.Add(route);
    }
}
