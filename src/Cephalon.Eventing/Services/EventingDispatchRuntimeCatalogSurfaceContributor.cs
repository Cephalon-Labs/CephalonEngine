using Cephalon.Abstractions.Technologies;

namespace Cephalon.Eventing.Services;

internal sealed class EventingDispatchRuntimeCatalogSurfaceContributor(
    EventDispatchRuntimeDescriptorCatalog runtimes) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "event-dispatch-runtimes",
            displayName: "Event Dispatch Runtimes",
            description: "Operator-facing durable dispatch runtimes currently contributing event handoff behavior to the active eventing technology.",
            entries: runtimes.Runtimes
                .Select(static runtime => new TechnologyRuntimeEntry(
                    id: runtime.Id,
                    displayName: runtime.DisplayName,
                    description: runtime.Description,
                    metadata: runtime.Metadata))
                .ToArray());
    }
}
