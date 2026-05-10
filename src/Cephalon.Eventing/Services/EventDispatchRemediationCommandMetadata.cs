using Cephalon.Abstractions.Data;

namespace Cephalon.Eventing.Services;

internal static class EventDispatchRemediationCommandMetadata
{
    internal const string SurfaceId = "event-dispatch-remediation-commands";
    internal const string CommandActionRoute = "/engine/event-dispatches/{outboxId}/commands/{operationId}";
    internal const string CommandListRoute = "/engine/event-dispatch-remediation-commands";
    internal const string CommandResultRoute = "/engine/event-dispatch-remediation-commands/{commandId}";
    internal const string CommandSummaryRoute = "/engine/event-dispatch-remediation-commands/summary";
    internal const string CommandLatestRoute = "/engine/event-dispatch-remediation-commands/latest";
    internal const string CommandRetentionRoute = "/engine/event-dispatch-remediation-commands/retention";
    internal const string CommandOutboxRoute = "/engine/event-dispatch-remediation-commands/outboxes/{outboxId}";
    internal const string CommandObservationRoute = "/engine/event-dispatch-remediation-commands/observations?fromUtc={fromUtc}&toUtc={toUtc}";
    internal const string CommandObservationSummaryRoute = "/engine/event-dispatch-remediation-commands/observations/summary?fromUtc={fromUtc}&toUtc={toUtc}";
    internal const string CommandOperationRoute = "/engine/event-dispatch-remediation-commands/operations/{operationId}";
    internal const string CommandActorRoute = "/engine/event-dispatch-remediation-commands/actors/{actorId}";
    internal const string CommandCorrelationRoute = "/engine/event-dispatch-remediation-commands/correlations/{correlationId}";
    internal const string CommandReasonRoute = "/engine/event-dispatch-remediation-commands/reasons/{reason}";
    internal const string CommandMessageRoute = "/engine/event-dispatch-remediation-commands/messages/{messageId}";
    internal const string CommandChannelRoute = "/engine/event-dispatch-remediation-commands/channels/{channelId}";
    internal const string CommandDispatchOutcomeRoute = "/engine/event-dispatch-remediation-commands/dispatch-outcomes/{dispatchOutcome}";
    internal const string CommandOutcomeRoute = "/engine/event-dispatch-remediation-commands/outcomes/{outcome}";
    internal const string CommandOperations = "retry-now,retry-later,skip,quarantine,dead-letter";
    internal const string CommandReadLimitRoutes = "all,observations,outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes";
    internal const string CommandPaginationRoutes = CommandReadLimitRoutes;

    internal static void AddCommandActionRouteMetadata(
        IDictionary<string, string> metadata,
        string prefix = "command")
    {
        metadata[$"{prefix}Route"] = CommandActionRoute;
    }

    internal static void AddCommandResultRouteMetadata(
        IDictionary<string, string> metadata,
        string prefix = "command")
    {
        metadata[$"{prefix}ListRoute"] = CommandListRoute;
        metadata[$"{prefix}ResultRoute"] = CommandResultRoute;
        metadata[$"{prefix}SummaryRoute"] = CommandSummaryRoute;
        metadata[$"{prefix}LatestRoute"] = CommandLatestRoute;
        metadata[$"{prefix}RetentionRoute"] = CommandRetentionRoute;
        metadata[$"{prefix}OutboxRoute"] = CommandOutboxRoute;
        metadata[$"{prefix}ObservationRoute"] = CommandObservationRoute;
        metadata[$"{prefix}ObservationSummaryRoute"] = CommandObservationSummaryRoute;
        metadata[$"{prefix}OperationRoute"] = CommandOperationRoute;
        metadata[$"{prefix}ActorRoute"] = CommandActorRoute;
        metadata[$"{prefix}CorrelationRoute"] = CommandCorrelationRoute;
        metadata[$"{prefix}ReasonRoute"] = CommandReasonRoute;
        metadata[$"{prefix}MessageRoute"] = CommandMessageRoute;
        metadata[$"{prefix}ChannelRoute"] = CommandChannelRoute;
        metadata[$"{prefix}DispatchOutcomeRoute"] = CommandDispatchOutcomeRoute;
        metadata[$"{prefix}OutcomeRoute"] = CommandOutcomeRoute;
    }

    internal static void AddObservationWindowMetadata(
        IDictionary<string, string> metadata,
        string prefix = "command")
    {
        metadata[$"{prefix}ObservationWindowQuery"] = "fromUtc,toUtc";
        metadata[$"{prefix}ObservationWindowPolicy"] = "inclusive-observed-utc";
        metadata[$"{prefix}ObservationWindowDetailOrder"] = "newest-first";
        metadata[$"{prefix}ObservationWindowSummary"] = "available";
        metadata[$"{prefix}ObservationWindowInvalidBounds"] = "reject-reversed-window";
    }

    internal static void AddReadLimitMetadata(
        IDictionary<string, string> metadata,
        string prefix = "command")
    {
        metadata[$"{prefix}ReadLimitQuery"] = "limit";
        metadata[$"{prefix}ReadLimitPolicy"] = "positive-integer-newest-first";
        metadata[$"{prefix}ReadLimitAppliesTo"] = "list-and-filter-routes";
        metadata[$"{prefix}ReadLimitRoutes"] = CommandReadLimitRoutes;
    }

    internal static void AddPaginationMetadata(
        IDictionary<string, string> metadata,
        string prefix = "command")
    {
        metadata[$"{prefix}PaginationQuery"] = "pageSize,continuationToken";
        metadata[$"{prefix}PaginationPolicy"] = "opaque-continuation-token-newest-first";
        metadata[$"{prefix}PaginationAppliesTo"] = "list-and-filter-routes";
        metadata[$"{prefix}PaginationRoutes"] = CommandPaginationRoutes;
        metadata[$"{prefix}PaginationResponse"] = "items,pageSize,returnedCount,totalRetainedCount,continuationToken,nextContinuationToken,hasMore";
        metadata[$"{prefix}PageSizeDefault"] = "50";
        metadata[$"{prefix}PageSizeMaximum"] = "500";
    }

    internal static void AddIdempotencyMetadata(IDictionary<string, string> metadata)
    {
        metadata[EventDispatchRemediationMetadataKeys.CommandIdempotencyPolicy] = "unique-command-id";
        metadata[EventDispatchRemediationMetadataKeys.DuplicateCommandPolicy] = "reject-without-mutation";
    }
}
