using Cephalon.Abstractions.Technologies;
using Cephalon.Eventing.Configuration;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class EventingInProcessPublishingRuntimeSurfaceContributor(
    EventingOptions options,
    IEventChannelCatalog channels,
    InProcessEventSubscriptionExecutorCatalog executors) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var channelIds = channels.Channels
            .Select(static channel => channel.Id)
            .OrderBy(static channelId => channelId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var subscriptionIds = executors.Entries
            .Select(static entry => entry.Subscription.Id)
            .OrderBy(static subscriptionId => subscriptionId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "event-publishers",
            displayName: "Event Publishers",
            description: "Active event publication paths available to the current eventing runtime.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: InProcessEventingRuntimeIds.PublisherId,
                    displayName: "In-process Event Publisher",
                    description: "Accepts integration events and directly invokes matching in-process subscription executors without durable broker, inbox, or retry ownership.",
                    metadata: new Dictionary<string, string>
                    {
                        ["handoff"] = "in-process",
                        ["dispatchRuntime"] = "cephalon-managed",
                        ["dispatchStore"] = "not-configured",
                        ["subscriptionExecution"] = "cephalon-managed",
                        ["subscriptionExecutionRuntimeId"] = InProcessEventingRuntimeIds.SubscriptionExecutionRuntimeId,
                        ["publicationDispatcher"] = "available",
                        ["executionMode"] = "in-process-direct",
                        ["deliveryMode"] = "direct",
                        ["retryPolicy"] = "none",
                        ["channelCount"] = channelIds.Length.ToString(CultureInfo.InvariantCulture),
                        ["channelIds"] = string.Join(",", channelIds),
                        ["subscriptionExecutorCount"] = subscriptionIds.Length.ToString(CultureInfo.InvariantCulture),
                        ["subscriptionIds"] = string.Join(",", subscriptionIds),
                        ["continueAfterFailure"] = options.ContinueInProcessSubscriptionExecutionAfterFailure.ToString().ToLowerInvariant()
                    })
            ]);
    }
}
