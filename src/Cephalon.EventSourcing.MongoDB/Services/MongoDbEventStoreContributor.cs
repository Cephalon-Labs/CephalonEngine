using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.EventSourcing.MongoDB.Services;

internal sealed class MongoDbEventStoreContributor(
    string connectionString,
    string databaseName,
    string collectionName,
    string snapshotCollectionName) : IEventStoreContributor
{
    public IReadOnlyList<EventStreamDescriptor> Contribute()
    {
        return
        [
            new EventStreamDescriptor(
                id: "mongodb-event-store",
                displayName: "MongoDB Event Store",
                description: "Appends and replays domain events through a MongoDB event-stream collection.",
                sourceModuleId: "mongodb-event-sourcing",
                provider: "mongodb",
                mode: "append-only",
                tags: ["event-sourcing", "mongodb", "document"],
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["databaseName"] = databaseName,
                    ["collectionName"] = collectionName,
                    ["connectionStringConfigured"] = string.IsNullOrWhiteSpace(connectionString) ? "false" : "true",
                    ["connectionStringProjection"] = "redacted",
                    ["secretProjection"] = "redacted",
                    ["snapshotStorage"] = snapshotCollectionName,
                    ["snapshotLifecycle"] = "provider-durable"
                })
        ];
    }
}
