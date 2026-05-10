using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Technologies;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class EventingDispatchRemediationCommandRuntimeSurfaceContributor(
    IEventDispatchRemediationRuntimeCatalog commands) : ITechnologyRuntimeContributor
{
    private const string SurfaceId = EventDispatchRemediationCommandMetadata.SurfaceId;

    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var entries = new List<TechnologyRuntimeEntry>(commands.States.Count + 1)
        {
            CreateCatalogEntry(commands)
        };
        entries.AddRange(commands.States.Select(CreateCommandEntry));

        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: SurfaceId,
            displayName: "Event Dispatch Remediation Commands",
            description: "Bounded operator command results recorded by the provider-neutral event-dispatch remediation dispatcher.",
            entries: entries);
    }

    private static TechnologyRuntimeEntry CreateCatalogEntry(IEventDispatchRemediationRuntimeCatalog catalog)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["entryKind"] = "catalog",
            ["commandStateCount"] = catalog.States.Count.ToString(CultureInfo.InvariantCulture),
            ["summaryTotalCommandCount"] = catalog.Summary.TotalCommandCount.ToString(CultureInfo.InvariantCulture),
            ["summaryAcceptedCount"] = catalog.Summary.AcceptedCount.ToString(CultureInfo.InvariantCulture),
            ["summaryRejectedCount"] = catalog.Summary.RejectedCount.ToString(CultureInfo.InvariantCulture),
            ["summaryErrorCount"] = catalog.Summary.ErrorCount.ToString(CultureInfo.InvariantCulture),
            ["summaryDuplicateCommandCount"] = catalog.Summary.DuplicateCommandCount.ToString(CultureInfo.InvariantCulture),
            ["summaryHasCommands"] = ToMetadataValue(catalog.Summary.HasCommands),
            ["summaryHasFailures"] = ToMetadataValue(catalog.Summary.HasFailures),
            ["summaryMayBeIncomplete"] = ToMetadataValue(catalog.Summary.SummaryMayBeIncomplete),
            ["commandHistoryLimit"] = catalog.Retention.HistoryLimit.ToString(CultureInfo.InvariantCulture),
            ["retainedCommandCount"] = catalog.Retention.RetainedCommandCount.ToString(CultureInfo.InvariantCulture),
            ["totalRecordedCommandCount"] = catalog.Retention.TotalRecordedCommandCount.ToString(CultureInfo.InvariantCulture),
            ["droppedCommandCount"] = catalog.Retention.DroppedCommandCount.ToString(CultureInfo.InvariantCulture),
            ["retentionTruncated"] = ToMetadataValue(catalog.Retention.Truncated),
            ["hasLatestCommand"] = ToMetadataValue(catalog.Latest is not null),
            ["providerNeutral"] = "true",
            ["wolverineRequired"] = "false"
        };

        EventDispatchRemediationCommandMetadata.AddCommandResultRouteMetadata(metadata);
        EventDispatchRemediationCommandMetadata.AddObservationWindowMetadata(metadata);
        EventDispatchRemediationCommandMetadata.AddReadLimitMetadata(metadata);
        EventDispatchRemediationCommandMetadata.AddPaginationMetadata(metadata);
        EventDispatchRemediationCommandMetadata.AddIdempotencyMetadata(metadata);

        AddOptional(metadata, "latestCommandId", catalog.Latest?.CommandId);
        AddOptional(metadata, "latestOperationId", catalog.Summary.LastOperationId);
        AddOptional(metadata, "latestOutcome", catalog.Summary.LastOutcome);
        AddOptional(metadata, "latestDispatchOutcome", catalog.Summary.LastDispatchOutcome);
        AddOptional(metadata, "latestObservedAtUtc", catalog.Summary.LastObservedAtUtc);
        AddOptional(metadata, "oldestRetainedCommandId", catalog.Retention.OldestRetainedCommandId);
        AddOptional(metadata, "oldestRetainedObservedAtUtc", catalog.Retention.OldestRetainedObservedAtUtc);
        AddOptional(metadata, "latestRetainedCommandId", catalog.Retention.LatestRetainedCommandId);
        AddOptional(metadata, "latestRetainedObservedAtUtc", catalog.Retention.LatestRetainedObservedAtUtc);

        return new TechnologyRuntimeEntry(
            id: SurfaceId,
            displayName: "Event Dispatch Remediation Command Catalog",
            description: "Route, retention, idempotency, and audit-read policy for provider-neutral event-dispatch remediation command results.",
            metadata: metadata);
    }

    private static TechnologyRuntimeEntry CreateCommandEntry(EventDispatchRemediationRuntimeState state)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["entryKind"] = "command-result",
            ["commandCatalogEntryId"] = SurfaceId,
            ["commandId"] = state.CommandId,
            ["outboxId"] = state.OutboxId,
            ["messageId"] = state.MessageId,
            ["channelId"] = state.ChannelId,
            ["operationId"] = state.OperationId,
            ["outcome"] = state.Outcome,
            ["dispatchOutcome"] = state.DispatchOutcome,
            ["observedAtUtc"] = state.ObservedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            ["commandScope"] = "dispatch-store",
            ["providerNeutral"] = "true",
            ["wolverineRequired"] = "false",
            ["hasError"] = string.IsNullOrWhiteSpace(state.Error) ? "false" : "true"
        };

        EventDispatchRemediationCommandMetadata.AddCommandResultRouteMetadata(metadata);
        EventDispatchRemediationCommandMetadata.AddObservationWindowMetadata(metadata);
        EventDispatchRemediationCommandMetadata.AddReadLimitMetadata(metadata);
        EventDispatchRemediationCommandMetadata.AddPaginationMetadata(metadata);
        EventDispatchRemediationCommandMetadata.AddIdempotencyMetadata(metadata);

        if (!string.IsNullOrWhiteSpace(state.Error))
        {
            metadata["error"] = state.Error;
        }

        if (state.Metadata.Count > 0)
        {
            metadata["reportedMetadataKeys"] = string.Join(
                ",",
                state.Metadata.Keys.OrderBy(static key => key, StringComparer.OrdinalIgnoreCase));
            foreach (var pair in state.Metadata.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                metadata[$"reported.{pair.Key}"] = pair.Value;
            }
        }

        return new TechnologyRuntimeEntry(
            id: state.CommandId,
            displayName: state.CommandId,
            description: ResolveDescription(state),
            metadata: metadata);
    }

    private static string ResolveDescription(EventDispatchRemediationRuntimeState state) =>
        string.Equals(state.Outcome, EventDispatchRemediationOutcomes.Accepted, StringComparison.OrdinalIgnoreCase)
            ? "The event-dispatch remediation command was accepted and applied through the active dispatch store."
            : "The event-dispatch remediation command was rejected before mutating dispatch-store state.";

    private static void AddOptional(
        Dictionary<string, string> metadata,
        string key,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            metadata[key] = value;
        }
    }

    private static void AddOptional(
        Dictionary<string, string> metadata,
        string key,
        DateTimeOffset? value)
    {
        if (value.HasValue)
        {
            metadata[key] = value.Value.ToString("O", CultureInfo.InvariantCulture);
        }
    }

    private static string ToMetadataValue(bool value) => value ? "true" : "false";
}
