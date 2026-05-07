using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.EventSourcing.ClickHouse.Services;

internal sealed class ClickHouseEventStoreContributor(
    string host,
    string database,
    string tableName,
    string username,
    string password) : IEventStoreContributor
{
    public IReadOnlyList<EventStreamDescriptor> Contribute()
    {
        return
        [
            new EventStreamDescriptor(
                id: "clickhouse-event-store",
                displayName: "ClickHouse Event Store",
                description: "Appends and replays domain events through a ClickHouse event table.",
                sourceModuleId: "clickhouse-event-sourcing",
                provider: "clickhouse",
                mode: "append-only",
                tags: ["event-sourcing", "clickhouse", "olap"],
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["host"] = host,
                    ["database"] = database,
                    ["tableName"] = tableName,
                    ["usernameConfigured"] = string.IsNullOrWhiteSpace(username) ? "false" : "true",
                    ["passwordConfigured"] = string.IsNullOrWhiteSpace(password) ? "false" : "true",
                    ["secretProjection"] = "redacted"
                })
        ];
    }
}
