using Microsoft.Extensions.DependencyInjection;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Technologies;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class EventingDispatchRuntimeSurfaceContributor(
    IOutboxCatalog outboxes,
    IEventDispatchRuntimeCatalog runtimeCatalog,
    IServiceProvider serviceProvider,
    EventDispatchRuntimeDescriptorCatalog dispatchRuntimes) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        using var scope = serviceProvider.CreateScope();
        var dispatchStores = scope.ServiceProvider.GetServices<IEventDispatchStore>();

        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "event-dispatches",
            displayName: "Event Dispatches",
            description: "Reported runtime state for durable event-dispatch paths backed by the active outbox surfaces.",
            entries: outboxes.Outboxes
                .Select(outbox => CreateEntry(outbox, dispatchStores))
                .ToArray());
    }

    private TechnologyRuntimeEntry CreateEntry(OutboxDescriptor outbox, IEnumerable<IEventDispatchStore> dispatchStores)
    {
        var metadata = new Dictionary<string, string>(outbox.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["sourceModuleId"] = outbox.SourceModuleId,
            ["dispatchStore"] = dispatchStores.Any() ? "available" : "not-configured",
            ["dispatchRuntime"] = dispatchRuntimes.Runtimes.Count > 0 ? "configured" : "not-configured",
            ["provider"] = outbox.Provider,
            ["mode"] = outbox.Mode
        };

        if (outbox.ChannelIds.Count > 0)
        {
            metadata["channelIds"] = string.Join(",", outbox.ChannelIds);
        }

        if (outbox.Tags.Count > 0)
        {
            metadata["tags"] = string.Join(",", outbox.Tags);
        }

        if (dispatchRuntimes.Runtimes.Count > 0)
        {
            metadata["dispatchRuntimeCount"] = dispatchRuntimes.Runtimes.Count.ToString(CultureInfo.InvariantCulture);
            metadata["dispatchRuntimeIds"] = string.Join(
                ",",
                dispatchRuntimes.Runtimes
                    .Select(static runtime => runtime.Id)
                    .OrderBy(static runtimeId => runtimeId, StringComparer.OrdinalIgnoreCase));

            foreach (var runtime in dispatchRuntimes.Runtimes
                         .OrderBy(static runtime => runtime.Id, StringComparer.OrdinalIgnoreCase))
            {
                metadata[$"dispatchRuntime.{runtime.Id}.displayName"] = runtime.DisplayName;
                metadata[$"dispatchRuntime.{runtime.Id}.description"] = runtime.Description;

                foreach (var pair in runtime.Metadata.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                {
                    metadata[$"dispatchRuntime.{runtime.Id}.{pair.Key}"] = pair.Value;
                }
            }
        }

        var state = runtimeCatalog.GetByOutboxId(outbox.Id);
        if (state is null)
        {
            metadata["runtimeState"] = "not-reported";
        }
        else
        {
            metadata["runtimeState"] = "reported";
            if (!string.IsNullOrWhiteSpace(state.LastChannelId))
            {
                metadata["lastChannelId"] = state.LastChannelId;
            }

            if (!string.IsNullOrWhiteSpace(state.LastOutcome))
            {
                metadata["lastOutcome"] = state.LastOutcome;
            }

            if (state.LastObservedAtUtc is { } lastObservedAtUtc)
            {
                metadata["lastObservedAtUtc"] = lastObservedAtUtc.ToString("O", CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrWhiteSpace(state.LastMessageId))
            {
                metadata["lastMessageId"] = state.LastMessageId;
            }

            metadata["lastAttempt"] = state.LastAttempt.ToString(CultureInfo.InvariantCulture);
            metadata["startedCount"] = state.StartedCount.ToString(CultureInfo.InvariantCulture);
            metadata["succeededCount"] = state.SucceededCount.ToString(CultureInfo.InvariantCulture);
            metadata["failedCount"] = state.FailedCount.ToString(CultureInfo.InvariantCulture);
            metadata["retryScheduledCount"] = state.RetryScheduledCount.ToString(CultureInfo.InvariantCulture);
            metadata["skippedCount"] = state.SkippedCount.ToString(CultureInfo.InvariantCulture);
            metadata["totalReports"] = state.TotalReports.ToString(CultureInfo.InvariantCulture);
            metadata["retryPending"] = state.RetryPending ? "true" : "false";
            if (!string.IsNullOrWhiteSpace(state.LastError))
            {
                metadata["lastError"] = state.LastError;
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
        }

        return new TechnologyRuntimeEntry(
            id: outbox.Id,
            displayName: outbox.DisplayName,
            description: outbox.Description,
            metadata: metadata);
    }
}
