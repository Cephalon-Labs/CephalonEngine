using System.Globalization;
using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.EventSourcing.Cassandra.Services;

internal sealed class CassandraEventStoreContributor(
    IReadOnlyList<string> contactPoints,
    string keyspace,
    string tableName) : IEventStoreContributor
{
    public IReadOnlyList<EventStreamDescriptor> Contribute()
    {
        return
        [
            new EventStreamDescriptor(
                id: "cassandra-event-store",
                displayName: "Cassandra Event Store",
                description: "Appends and replays domain events through an Apache Cassandra wide-column table.",
                sourceModuleId: "cassandra-event-sourcing",
                provider: "cassandra",
                mode: "append-only",
                tags: ["event-sourcing", "cassandra", "wide-column"],
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["contactPointCount"] = contactPoints.Count.ToString(CultureInfo.InvariantCulture),
                    ["contactPoints"] = string.Join(",", contactPoints),
                    ["keyspace"] = keyspace,
                    ["tableName"] = tableName,
                    ["port"] = "9042",
                    ["concurrency"] = "optimistic-version+lwt"
                })
        ];
    }
}
