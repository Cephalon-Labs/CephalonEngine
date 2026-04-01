using Cephalon.Abstractions.Technologies;

namespace Cephalon.Eventing.Services;

internal sealed class EventingRuntimeSurfaceContributor(IEventChannelCatalog catalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "event-channels",
            displayName: "Event Channels",
            description: "Registered event channels available to the active eventing runtime.",
            entries: catalog.Channels
                .Select(channel => new TechnologyRuntimeEntry(
                    id: channel.Id,
                    displayName: channel.DisplayName,
                    description: channel.Description,
                    metadata: new Dictionary<string, string>
                    {
                        ["tags"] = string.Join(",", channel.Tags)
                    }))
                .ToArray());
    }
}
