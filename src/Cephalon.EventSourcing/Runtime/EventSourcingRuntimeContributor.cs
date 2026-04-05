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

        return new TechnologyRuntimeSurface(
            technologyId: "event-sourcing",
            surfaceId: "event-sourcing",
            displayName: "Event Sourcing",
            description: "Summarizes the active event-sourcing provider set for the current Cephalon runtime.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "event-sourcing-runtime",
                    displayName: "Event Sourcing Runtime",
                    description: "Projects the merged event-stream catalog and the host-owned event-sourcing options.",
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["activeStoreCount"] = eventStores.All.Count.ToString(CultureInfo.InvariantCulture),
                        ["activeProviderCount"] = activeProviders.Length.ToString(CultureInfo.InvariantCulture),
                        ["defaultProvider"] = string.IsNullOrWhiteSpace(options.DefaultProvider)
                            ? "none"
                            : options.DefaultProvider,
                        ["enableSnapshots"] = options.EnableSnapshots ? "true" : "false"
                    })
            ]);
    }
}
