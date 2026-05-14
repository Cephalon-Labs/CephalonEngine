using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.EventSourcing.Redis.Services;

internal sealed class RedisEventStoreContributor(
    string configuration,
    string keyPrefix) : IEventStoreContributor
{
    public IReadOnlyList<EventStreamDescriptor> Contribute()
    {
        return
        [
            new EventStreamDescriptor(
                id: "redis-event-store",
                displayName: "Redis Event Store",
                description: "Appends and replays domain events through Redis Streams.",
                sourceModuleId: "redis-event-sourcing",
                provider: "redis",
                mode: "append-only",
                tags: ["event-sourcing", "redis", "stream"],
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["keyPrefix"] = keyPrefix,
                    ["configurationConfigured"] = string.IsNullOrWhiteSpace(configuration) ? "false" : "true",
                    ["configurationProjection"] = "redacted",
                    ["secretProjection"] = "redacted",
                    ["snapshotKeyPrefix"] = $"{keyPrefix}snapshot:",
                    ["snapshotStorage"] = "Redis Hash latest-snapshot",
                    ["snapshotLifecycle"] = "provider-durable"
                })
        ];
    }
}
