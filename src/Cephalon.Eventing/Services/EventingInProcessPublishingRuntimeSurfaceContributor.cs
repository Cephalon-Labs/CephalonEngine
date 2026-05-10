using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Technologies;
using Cephalon.Eventing.Configuration;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class EventingInProcessPublishingRuntimeSurfaceContributor(
    EventingOptions options,
    IEventChannelCatalog channels,
    InProcessEventSubscriptionExecutorCatalog executors,
    IEventPublicationRuntimeCatalog publicationRuntimeCatalog,
    EventingRuntimeTopology topology,
    EventPublicationScheduleQueue scheduleQueue) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var maxAttempts = InProcessEventingRetryPolicy.GetMaxAttempts(options);
        var retryDelayMilliseconds = InProcessEventingRetryPolicy.GetRetryDelayMilliseconds(options);
        var retryPolicy = InProcessEventingRetryPolicy.GetPolicyId(options);
        var retryBackoff = InProcessEventingRetryPolicy.GetBackoff(options);
        var retryBackoffMultiplier = InProcessEventingRetryPolicy.GetBackoffMultiplier(options);
        var retryMaxDelayMilliseconds = InProcessEventingRetryPolicy.GetMaxDelayMilliseconds(options);
        var retryJitterPercent = InProcessEventingRetryPolicy.GetJitterPercent(options);
        var idempotencyPolicy = InProcessEventingIdempotencyPolicy.GetPolicyId(options);
        var idempotencyKey = InProcessEventingIdempotencyPolicy.GetKeyShape(options);
        var idempotencyStore = InProcessEventingIdempotencyPolicy.GetStore(options);
        var idempotencyScope = InProcessEventingIdempotencyPolicy.GetScope(options);
        var idempotencyDurability = InProcessEventingIdempotencyPolicy.GetDurability(options);
        var idempotencyRetentionMinutes = InProcessEventingIdempotencyPolicy.GetRetentionMinutes(options);
        var channelIds = channels.Channels
            .Select(static channel => channel.Id)
            .OrderBy(static channelId => channelId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var subscriptionIds = executors.Entries
            .Select(static entry => entry.Subscription.Id)
            .OrderBy(static subscriptionId => subscriptionId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var publicationStates = publicationRuntimeCatalog.States;
        var lastPublicationState = publicationStates
            .Where(static state => state.LastObservedAtUtc is not null)
            .OrderByDescending(static state => state.LastObservedAtUtc)
            .FirstOrDefault();

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
                    description: "Accepts integration events and directly invokes matching in-process subscription executors without durable broker or inbox ownership.",
                    metadata: new Dictionary<string, string>
                    {
                        ["handoff"] = "in-process",
                        ["dispatchRuntime"] = "cephalon-managed",
                        ["dispatchStore"] = "not-configured",
                        ["subscriptionExecution"] = "cephalon-managed",
                        ["subscriptionExecutionRuntimeId"] = InProcessEventingRuntimeIds.SubscriptionExecutionRuntimeId,
                        ["subscriptionExecutionPipeline"] = topology.SubscriptionExecutionPipeline,
                        ["subscriptionExecutionMiddlewareCount"] = topology.SubscriptionExecutionMiddlewareCount.ToString(CultureInfo.InvariantCulture),
                        ["publicationDispatcher"] = "available",
                        ["publicationRuntimeState"] = publicationStates.Count > 0 ? "reported" : "not-reported",
                        ["publicationSchedulingPolicy"] = EventPublicationSchedulingPolicy.GetPolicyId(options),
                        ["publicationSchedulingScope"] = EventPublicationSchedulingPolicy.GetScope(options),
                        ["publicationSchedulingDurability"] = EventPublicationSchedulingPolicy.GetDurability(options),
                        ["publicationSchedulingMaxDelayMilliseconds"] = options.PublicationSchedulingMaxDelayMilliseconds.ToString(CultureInfo.InvariantCulture),
                        ["publicationSchedulingMaxPendingCount"] = options.PublicationSchedulingMaxPendingCount.ToString(CultureInfo.InvariantCulture),
                        ["publicationRoutingPolicy"] = EventPublicationRoutingPolicy.GetPolicyId(options),
                        ["publicationRoutingRouteCount"] = EventPublicationRoutingPolicy.GetRouteCount(options),
                        ["publicationRoutingAutoChannelId"] = EventPublicationRoutingPolicy.GetAutoChannelId(options),
                        ["publicationRoutingRequireMatchedRoute"] = options.PublicationRoutingRequireMatchedRoute.ToString().ToLowerInvariant(),
                        ["publicationRoutingRejectMismatchedExplicitChannel"] = options.PublicationRoutingRejectMismatchedExplicitChannel.ToString().ToLowerInvariant(),
                        ["scheduledPublicationPendingCount"] = scheduleQueue.PendingCount.ToString(CultureInfo.InvariantCulture),
                        ["nextScheduledPublicationDueAtUtc"] = scheduleQueue.NextDueAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
                        ["publicationStateCount"] = publicationStates.Count.ToString(CultureInfo.InvariantCulture),
                        ["publicationAcceptedCount"] = publicationStates.Sum(static state => state.AcceptedCount).ToString(CultureInfo.InvariantCulture),
                        ["publicationSucceededCount"] = publicationStates.Sum(static state => state.SucceededCount).ToString(CultureInfo.InvariantCulture),
                        ["publicationFailedCount"] = publicationStates.Sum(static state => state.FailedCount).ToString(CultureInfo.InvariantCulture),
                        ["publicationSkippedCount"] = publicationStates.Sum(static state => state.SkippedCount).ToString(CultureInfo.InvariantCulture),
                        ["lastPublicationId"] = lastPublicationState?.PublicationId ?? string.Empty,
                        ["lastPublicationOutcome"] = lastPublicationState?.LastOutcome ?? "unknown",
                        ["executionMode"] = "in-process-direct",
                        ["deliveryMode"] = "direct",
                        ["retryPolicy"] = retryPolicy,
                        ["retryMaxAttempts"] = maxAttempts.ToString(CultureInfo.InvariantCulture),
                        ["retryDelayMilliseconds"] = retryDelayMilliseconds.ToString(CultureInfo.InvariantCulture),
                        ["retryBackoff"] = retryBackoff,
                        ["retryBackoffMultiplier"] = retryBackoffMultiplier.ToString(CultureInfo.InvariantCulture),
                        ["retryMaxDelayMilliseconds"] = retryMaxDelayMilliseconds.ToString(CultureInfo.InvariantCulture),
                        ["retryJitterPercent"] = retryJitterPercent.ToString(CultureInfo.InvariantCulture),
                        ["retryDurability"] = "none",
                        ["retryScope"] = "process-local",
                        ["idempotencyPolicy"] = idempotencyPolicy,
                        ["idempotencyKey"] = idempotencyKey,
                        ["idempotencyStore"] = idempotencyStore,
                        ["idempotencyRetentionMinutes"] = idempotencyRetentionMinutes.ToString(CultureInfo.InvariantCulture),
                        ["idempotencyDurability"] = idempotencyDurability,
                        ["idempotencyScope"] = idempotencyScope,
                        ["inbox"] = topology.HasInboxPath ? "available" : "not-configured",
                        ["channelCount"] = channelIds.Length.ToString(CultureInfo.InvariantCulture),
                        ["channelIds"] = string.Join(",", channelIds),
                        ["subscriptionExecutorCount"] = subscriptionIds.Length.ToString(CultureInfo.InvariantCulture),
                        ["subscriptionIds"] = string.Join(",", subscriptionIds),
                        ["continueAfterFailure"] = options.ContinueInProcessSubscriptionExecutionAfterFailure.ToString().ToLowerInvariant()
                    })
            ]);
    }
}
