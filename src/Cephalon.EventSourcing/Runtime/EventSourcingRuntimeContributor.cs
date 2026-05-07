using Cephalon.Abstractions.EventSourcing;
using Cephalon.Abstractions.Technologies;
using Cephalon.EventSourcing.Configuration;
using System.Globalization;

namespace Cephalon.EventSourcing.Runtime;

internal sealed class EventSourcingRuntimeContributor(
    EventSourcingOptions options,
    IEventStoreCatalog eventStores) : ITechnologyRuntimeContributor
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
                description: "Projects the merged event-stream catalog and the host-owned event-sourcing options.",
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["activeStoreCount"] = eventStores.All.Count.ToString(CultureInfo.InvariantCulture),
                    ["activeProviderCount"] = activeProviders.Length.ToString(CultureInfo.InvariantCulture),
                    ["activeProviders"] = activeProviders.Length == 0 ? "none" : string.Join(",", activeProviders),
                    ["defaultProvider"] = string.IsNullOrWhiteSpace(options.DefaultProvider)
                        ? "none"
                        : options.DefaultProvider,
                    ["enableSnapshots"] = options.EnableSnapshots ? "true" : "false"
                })
        };

        entries.AddRange(eventStores.All.Select(CreateEntry));

        return new TechnologyRuntimeSurface(
            technologyId: "event-sourcing",
            surfaceId: "event-sourcing",
            displayName: "Event Sourcing",
            description: "Summarizes the active event-sourcing provider set for the current Cephalon runtime.",
            entries: entries);
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
