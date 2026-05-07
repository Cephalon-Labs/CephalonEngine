using System.Globalization;
using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.EventSourcing.Qdrant.Services;

internal sealed class QdrantEventStoreContributor(
    string host,
    int port,
    string collectionName) : IEventStoreContributor
{
    public IReadOnlyList<EventStreamDescriptor> Contribute()
    {
        return
        [
            new EventStreamDescriptor(
                id: "qdrant-event-store",
                displayName: "Qdrant Event Store",
                description: "Appends and replays domain events through a Qdrant collection.",
                sourceModuleId: "qdrant-event-sourcing",
                provider: "qdrant",
                mode: "append-only",
                tags: ["event-sourcing", "qdrant", "vector"],
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["host"] = host,
                    ["port"] = port.ToString(CultureInfo.InvariantCulture),
                    ["collectionName"] = collectionName
                })
        ];
    }
}
