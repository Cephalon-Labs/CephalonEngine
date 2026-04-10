using Microsoft.Extensions.DependencyInjection;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Technologies;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class EventingPublishingRuntimeSurfaceContributor(
    IEventChannelCatalog channels,
    IOutboxCatalog outboxes,
    IServiceProvider serviceProvider,
    EventDispatchRuntimeDescriptorCatalog dispatchRuntimes) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var outboxEntries = outboxes.Outboxes.ToArray();
        var channelIds = channels.Channels
            .Select(static channel => channel.Id)
            .OrderBy(static channelId => channelId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        using var scope = serviceProvider.CreateScope();
        var hasDispatchStore = scope.ServiceProvider.GetServices<IEventDispatchStore>().Any();
        var runtimeIds = dispatchRuntimes.Runtimes
            .Select(static runtime => runtime.Id)
            .OrderBy(static runtimeId => runtimeId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "event-publishers",
            displayName: "Event Publishers",
            description: "Active event publication paths available to the current eventing runtime.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "outbox-backed-publisher",
                    displayName: "Outbox-backed Publisher",
                    description: "Accepts integration events and stages them through the active outbox path for later delivery.",
                    metadata: new Dictionary<string, string>
                    {
                        ["handoff"] = "outbox",
                        ["dispatchRuntime"] = runtimeIds.Length > 0 ? "configured" : "not-configured",
                        ["dispatchStore"] = hasDispatchStore ? "available" : "not-configured",
                        ["channelCount"] = channelIds.Length.ToString(CultureInfo.InvariantCulture),
                        ["channelIds"] = string.Join(",", channelIds),
                        ["outboxCount"] = outboxEntries.Length.ToString(CultureInfo.InvariantCulture),
                        ["outboxIds"] = string.Join(",", outboxEntries.Select(static outbox => outbox.Id)),
                        ["dispatchRuntimeCount"] = runtimeIds.Length.ToString(CultureInfo.InvariantCulture),
                        ["dispatchRuntimeIds"] = string.Join(",", runtimeIds)
                    })
            ]);
    }
}
