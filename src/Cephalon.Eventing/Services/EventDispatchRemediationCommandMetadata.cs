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
    internal const string CommandOperationSummaryRoute = "/engine/event-dispatch-remediation-commands/operations/{operationId}/summary";
    internal const string CommandActorRoute = "/engine/event-dispatch-remediation-commands/actors/{actorId}";
    internal const string CommandActorSummaryRoute = "/engine/event-dispatch-remediation-commands/actors/{actorId}/summary";
    internal const string CommandCorrelationRoute = "/engine/event-dispatch-remediation-commands/correlations/{correlationId}";
    internal const string CommandCorrelationSummaryRoute = "/engine/event-dispatch-remediation-commands/correlations/{correlationId}/summary";
    internal const string CommandReasonRoute = "/engine/event-dispatch-remediation-commands/reasons/{reason}";
    internal const string CommandReasonSummaryRoute = "/engine/event-dispatch-remediation-commands/reasons/{reason}/summary";
    internal const string CommandMessageRoute = "/engine/event-dispatch-remediation-commands/messages/{messageId}";
    internal const string CommandMessageSummaryRoute = "/engine/event-dispatch-remediation-commands/messages/{messageId}/summary";
    internal const string CommandChannelRoute = "/engine/event-dispatch-remediation-commands/channels/{channelId}";
    internal const string CommandChannelSummaryRoute = "/engine/event-dispatch-remediation-commands/channels/{channelId}/summary";
    internal const string CommandDispatchOutcomeRoute = "/engine/event-dispatch-remediation-commands/dispatch-outcomes/{dispatchOutcome}";
    internal const string CommandDispatchOutcomeSummaryRoute = "/engine/event-dispatch-remediation-commands/dispatch-outcomes/{dispatchOutcome}/summary";
    internal const string CommandOutcomeRoute = "/engine/event-dispatch-remediation-commands/outcomes/{outcome}";
    internal const string CommandOutcomeSummaryRoute = "/engine/event-dispatch-remediation-commands/outcomes/{outcome}/summary";
    internal const string CommandOutboxSummaryRoute = "/engine/event-dispatch-remediation-commands/outboxes/{outboxId}/summary";
    internal const string CommandOperations = "retry-now,retry-later,skip,quarantine,dead-letter";
    internal const string CommandReadLimitRoutes = "all,in-doubt,observations,outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes";
    internal const string CommandPaginationRoutes = CommandReadLimitRoutes;
    internal const string CommandFilterSummaryRoutes = "outboxes,messages,channels,operations,actors,correlations,reasons,dispatch-outcomes,outcomes";

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
        metadata[$"{prefix}OperationSummaryRoute"] = CommandOperationSummaryRoute;
        metadata[$"{prefix}ActorRoute"] = CommandActorRoute;
        metadata[$"{prefix}ActorSummaryRoute"] = CommandActorSummaryRoute;
        metadata[$"{prefix}CorrelationRoute"] = CommandCorrelationRoute;
        metadata[$"{prefix}CorrelationSummaryRoute"] = CommandCorrelationSummaryRoute;
        metadata[$"{prefix}ReasonRoute"] = CommandReasonRoute;
        metadata[$"{prefix}ReasonSummaryRoute"] = CommandReasonSummaryRoute;
        metadata[$"{prefix}MessageRoute"] = CommandMessageRoute;
        metadata[$"{prefix}MessageSummaryRoute"] = CommandMessageSummaryRoute;
        metadata[$"{prefix}ChannelRoute"] = CommandChannelRoute;
        metadata[$"{prefix}ChannelSummaryRoute"] = CommandChannelSummaryRoute;
        metadata[$"{prefix}DispatchOutcomeRoute"] = CommandDispatchOutcomeRoute;
        metadata[$"{prefix}DispatchOutcomeSummaryRoute"] = CommandDispatchOutcomeSummaryRoute;
        metadata[$"{prefix}OutcomeRoute"] = CommandOutcomeRoute;
        metadata[$"{prefix}OutcomeSummaryRoute"] = CommandOutcomeSummaryRoute;
        metadata[$"{prefix}OutboxSummaryRoute"] = CommandOutboxSummaryRoute;
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

    internal static void AddFilterSummaryMetadata(
        IDictionary<string, string> metadata,
        string prefix = "command")
    {
        metadata[$"{prefix}FilterSummaryPolicy"] = "retained-filter-server-side-aggregate";
        metadata[$"{prefix}FilterSummaryRoutes"] = CommandFilterSummaryRoutes;
        metadata[$"{prefix}FilterSummaryResponse"] = nameof(EventDispatchRemediationRuntimeSummary);
        metadata[$"{prefix}FilterSummaryMaterialization"] = "not-required";
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
