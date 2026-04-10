using Cephalon.Abstractions.Data;
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
                    metadata: CreateMetadata(runtime)))
                .ToArray());
    }

    private static Dictionary<string, string> CreateMetadata(EventDispatchRuntimeDescriptor runtime)
    {
        var metadata = new Dictionary<string, string>(runtime.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["outboxCount"] = runtime.OutboxIds.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["outboxIds"] = string.Join(",", runtime.OutboxIds)
        };

        return metadata;
    }
}
