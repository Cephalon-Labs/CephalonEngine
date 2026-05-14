using Cephalon.Abstractions.EventSourcing;
using Cephalon.Abstractions.Technologies;
using Cephalon.EventSourcing.Configuration;
using Cephalon.EventSourcing.Services;
using System.Globalization;

namespace Cephalon.EventSourcing.Runtime;

internal sealed class EventSourcingRuntimeContributor(
    EventSourcingOptions options,
    IEventStoreCatalog eventStores,
    EventStreamReplayRuntimeState replayRuntimeState) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var activeProviders = eventStores.All
            .Select(static descriptor => descriptor.Provider)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static provider => provider, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var entries = new List<TechnologyRuntimeEntry>
        {
            new(
                id: "event-sourcing-runtime",
                displayName: "Event Sourcing Runtime",
                description: "Projects the merged event-stream catalog, host-owned event-sourcing options, and managed replay state.",
                metadata: CreateSummaryMetadata(activeProviders))
        };

        entries.Add(CreateReplayWorkerEntry());
        entries.AddRange(eventStores.All.Select(CreateEntry));

        return new TechnologyRuntimeSurface(
            technologyId: "event-sourcing",
            surfaceId: "event-sourcing",
            displayName: "Event Sourcing",
            description: "Summarizes the active event-sourcing provider set for the current Cephalon runtime.",
            entries: entries);
    }

    private Dictionary<string, string> CreateSummaryMetadata(string[] activeProviders)
    {
        var latestReport = replayRuntimeState.LatestReport;
        var durableSnapshotProviders = ResolveDurableSnapshotProviders();
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["activeStoreCount"] = eventStores.All.Count.ToString(CultureInfo.InvariantCulture),
            ["activeProviderCount"] = activeProviders.Length.ToString(CultureInfo.InvariantCulture),
            ["activeProviders"] = activeProviders.Length == 0 ? "none" : string.Join(",", activeProviders),
            ["defaultProvider"] = string.IsNullOrWhiteSpace(options.DefaultProvider)
                ? "none"
                : options.DefaultProvider,
            ["enableSnapshots"] = options.EnableSnapshots ? "true" : "false",
            ["enableInMemorySnapshotStore"] = options.EnableInMemorySnapshotStore ? "true" : "false",
            ["enableReplayWorker"] = options.EnableReplayWorker ? "true" : "false",
            ["snapshotLifecycle"] = ResolveSnapshotLifecycle(durableSnapshotProviders),
            ["providerDurableSnapshotProviders"] = durableSnapshotProviders.Length == 0 ? "none" : string.Join(",", durableSnapshotProviders),
            ["projectionRebuild"] = options.EnableReplayWorker ? "on-demand-domain-event-projections" : "disabled",
            ["hostedBackgroundRunner"] = "not-claimed",
            ["latestReplayStatus"] = latestReport?.Status ?? "none",
            ["latestReplayStreamId"] = latestReport?.StreamId ?? "none",
            ["latestReplayEvents"] = latestReport?.ReplayedEventCount.ToString(CultureInfo.InvariantCulture) ?? "0",
            ["latestReplayProjectedEvents"] = latestReport?.ProjectedEventCount.ToString(CultureInfo.InvariantCulture) ?? "0",
            ["latestReplaySnapshotSaved"] = latestReport?.SnapshotSaved == true ? "true" : "false"
        };

        if (latestReport is not null)
        {
            metadata["latestReplayUsedSnapshot"] = latestReport.UsedSnapshot ? "true" : "false";
            metadata["latestReplaySnapshotVersion"] = latestReport.SnapshotVersion.ToString(CultureInfo.InvariantCulture);
            metadata["latestReplayFromVersion"] = latestReport.ReplayFromVersion.ToString(CultureInfo.InvariantCulture);
            metadata["latestReplayLastVersion"] = latestReport.LastReplayedVersion.ToString(CultureInfo.InvariantCulture);
        }

        return metadata;
    }

    private TechnologyRuntimeEntry CreateReplayWorkerEntry()
    {
        var latestReport = replayRuntimeState.LatestReport;
        var durableSnapshotProviders = ResolveDurableSnapshotProviders();
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["managedExecution"] = options.EnableReplayWorker ? "cephalon-managed" : "disabled",
            ["mode"] = options.EnableReplayWorker ? "on-demand" : "disabled",
            ["snapshotAssistedReplay"] = options.EnableSnapshots ? ResolveSnapshotLifecycle(durableSnapshotProviders) : "disabled",
            ["projectionRebuild"] = options.EnableReplayWorker ? "registered-domain-event-projections" : "disabled",
            ["hostedBackgroundRunner"] = "not-claimed",
            ["providerDurableSnapshots"] = durableSnapshotProviders.Length == 0 ? "not-claimed" : "claimed",
            ["providerDurableSnapshotProviders"] = durableSnapshotProviders.Length == 0 ? "none" : string.Join(",", durableSnapshotProviders),
            ["latestReplayStatus"] = latestReport?.Status ?? "none"
        };

        if (latestReport is not null)
        {
            metadata["latestReplayStreamId"] = latestReport.StreamId;
            metadata["latestReplayEvents"] = latestReport.ReplayedEventCount.ToString(CultureInfo.InvariantCulture);
            metadata["latestReplayProjectedEvents"] = latestReport.ProjectedEventCount.ToString(CultureInfo.InvariantCulture);
            metadata["latestReplaySnapshotSaved"] = latestReport.SnapshotSaved ? "true" : "false";
        }

        return new TechnologyRuntimeEntry(
            id: "event-sourcing-managed-replay-worker",
            displayName: "Managed Replay Worker",
            description: "Reports the Cephalon-owned on-demand replay worker for aggregate hydration, snapshot lifecycle, and projection rebuild proof.",
            metadata: metadata);
    }

    private string ResolveSnapshotLifecycle(string[] durableSnapshotProviders)
    {
        if (!options.EnableSnapshots)
        {
            return "disabled";
        }

        if (durableSnapshotProviders.Length > 0)
        {
            return "provider-durable";
        }

        return options.EnableInMemorySnapshotStore ? "process-local" : "provider-managed-or-unregistered";
    }

    private string[] ResolveDurableSnapshotProviders()
    {
        if (!options.EnableSnapshots)
        {
            return [];
        }

        return eventStores.All
            .Where(static descriptor =>
                descriptor.Metadata.TryGetValue("snapshotLifecycle", out var snapshotLifecycle) &&
                string.Equals(snapshotLifecycle, "provider-durable", StringComparison.OrdinalIgnoreCase))
            .Select(static descriptor => descriptor.Provider)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static provider => provider, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static TechnologyRuntimeEntry CreateEntry(EventStreamDescriptor descriptor)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["provider"] = descriptor.Provider,
            ["sourceModuleId"] = descriptor.SourceModuleId,
            ["mode"] = descriptor.Mode,
            ["tags"] = descriptor.Tags.Count == 0 ? "none" : string.Join(",", descriptor.Tags)
        };

        foreach (var pair in descriptor.Metadata.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            metadata[$"provider.{pair.Key}"] = pair.Value;
        }

        return new TechnologyRuntimeEntry(
            id: descriptor.Id,
            displayName: descriptor.DisplayName,
            description: descriptor.Description,
            metadata: metadata);
    }
}
