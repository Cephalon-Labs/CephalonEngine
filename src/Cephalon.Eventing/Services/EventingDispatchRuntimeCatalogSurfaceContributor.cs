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
            ["outboxIds"] = string.Join(",", runtime.OutboxIds),
            ["reportedOutboxCount"] = runtime.Summary.ReportedOutboxCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedOutboxIds"] = string.Join(",", runtime.Summary.ReportedOutboxIds),
            ["reportedStartedCount"] = runtime.Summary.StartedCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedSucceededCount"] = runtime.Summary.SucceededCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedFailedCount"] = runtime.Summary.FailedCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedRetryScheduledCount"] = runtime.Summary.RetryScheduledCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedSkippedCount"] = runtime.Summary.SkippedCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedTotalCount"] = runtime.Summary.TotalReports.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedRetryPendingCount"] = runtime.Summary.RetryPendingCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedTerminalFailureCount"] = runtime.Summary.TerminalFailureCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedTerminalOutboxCount"] = runtime.Summary.TerminalOutboxCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reportedHasTerminalFailures"] = runtime.Summary.HasTerminalFailures ? "true" : "false",
            ["runtimeState"] = runtime.Summary.HasReports ? "reported" : "not-reported"
        };

        if (!string.IsNullOrWhiteSpace(runtime.Summary.LastOutboxId))
        {
            metadata["lastOutboxId"] = runtime.Summary.LastOutboxId;
        }

        if (!string.IsNullOrWhiteSpace(runtime.Summary.LastChannelId))
        {
            metadata["lastChannelId"] = runtime.Summary.LastChannelId;
        }

        if (!string.IsNullOrWhiteSpace(runtime.Summary.LastOutcome))
        {
            metadata["lastOutcome"] = runtime.Summary.LastOutcome;
        }

        if (runtime.Summary.LastObservedAtUtc is { } lastObservedAtUtc)
        {
            metadata["lastObservedAtUtc"] = lastObservedAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(runtime.Summary.LastMessageId))
        {
            metadata["lastMessageId"] = runtime.Summary.LastMessageId;
        }

        metadata["lastAttempt"] = runtime.Summary.LastAttempt.ToString(System.Globalization.CultureInfo.InvariantCulture);

        if (!string.IsNullOrWhiteSpace(runtime.Summary.LastError))
        {
            metadata["lastError"] = runtime.Summary.LastError;
        }

        return metadata;
    }
}
