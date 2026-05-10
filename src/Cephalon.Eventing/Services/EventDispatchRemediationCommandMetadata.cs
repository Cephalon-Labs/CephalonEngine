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
    internal const string CommandInDoubtRoute = "/engine/event-dispatch-remediation-commands/in-doubt";
    internal const string CommandInDoubtSummaryRoute = "/engine/event-dispatch-remediation-commands/in-doubt/summary";
    internal const string CommandOldestInDoubtRoute = "/engine/event-dispatch-remediation-commands/in-doubt/oldest";
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
    internal const string CommandReadLimitRoutes = "all,in-doubt,observations,outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes";
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
        metadata[$"{prefix}InDoubtRoute"] = CommandInDoubtRoute;
        metadata[$"{prefix}InDoubtSummaryRoute"] = CommandInDoubtSummaryRoute;
        metadata[$"{prefix}OldestInDoubtRoute"] = CommandOldestInDoubtRoute;
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

    internal static void AddInDoubtMetadata(
        IDictionary<string, string> metadata,
        string prefix = "command")
    {
        metadata[$"{prefix}InDoubtQuery"] = "beforeUtc";
        metadata[$"{prefix}InDoubtCutoffPolicy"] = "inclusive-observed-utc-before-or-equal";
        metadata[$"{prefix}InDoubtDetailOrder"] = "newest-first";
        metadata[$"{prefix}InDoubtInvalidCutoff"] = "reject-invalid-date-time-offset";
        metadata[$"{prefix}InDoubtSummaryQuery"] = "beforeUtc";
        metadata[$"{prefix}InDoubtSummaryPolicy"] = "retained-reserved-summary-observed-utc-before-or-equal";
        metadata[$"{prefix}InDoubtSummaryInvalidCutoff"] = "reject-invalid-date-time-offset";
        metadata[$"{prefix}OldestInDoubtQuery"] = "beforeUtc";
        metadata[$"{prefix}OldestInDoubtPolicy"] = "oldest-retained-reserved-observed-utc-before-or-equal";
        metadata[$"{prefix}OldestInDoubtInvalidCutoff"] = "reject-invalid-date-time-offset";
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
        metadata[$"{prefix}PaginationPolicy"] = "opaque-signed-route-bound-continuation-token-newest-first";
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
        metadata[EventDispatchRemediationMetadataKeys.CommandReservationPolicy] = "reserve-before-mutation";
        metadata[EventDispatchRemediationMetadataKeys.CommandReservationTiming] = "before-dispatch-store-mutation";
        metadata[EventDispatchRemediationMetadataKeys.CommandReservationDuplicatePolicy] = "duplicate-reservation-rejects-without-mutation";
        metadata[EventDispatchRemediationMetadataKeys.CommandReservationInDoubtOutcome] = EventDispatchRemediationOutcomes.Reserved;
        metadata[EventDispatchRemediationMetadataKeys.CommandReservationOwner] = "command-journal";
    }

    internal static void AddJournalMetadata(
        IDictionary<string, string> metadata,
        EventDispatchRemediationCommandJournalDescriptor? descriptor,
        string prefix = "command")
    {
        descriptor ??= new EventDispatchRemediationCommandJournalDescriptor(
            JournalId: "eventing.process-local-remediation-command-journal",
            Provider: "Cephalon.Eventing",
            Storage: "memory",
            Durability: "process-local",
            Scope: "single-process",
            CrossNodeCommandAudit: false,
            DurableReplayCursor: false);

        metadata[$"{prefix}JournalId"] = descriptor.JournalId;
        metadata[$"{prefix}JournalProvider"] = descriptor.Provider;
        metadata[$"{prefix}JournalStorage"] = descriptor.Storage;
        metadata[$"{prefix}JournalDurability"] = descriptor.Durability;
        metadata[$"{prefix}JournalScope"] = descriptor.Scope;
        metadata[$"{prefix}CrossNodeCommandAudit"] = descriptor.CrossNodeCommandAudit ? "true" : "false";
        metadata[$"{prefix}JournalReplayCursor"] = descriptor.DurableReplayCursor ? "durable" : "not-claimed";
    }
}
