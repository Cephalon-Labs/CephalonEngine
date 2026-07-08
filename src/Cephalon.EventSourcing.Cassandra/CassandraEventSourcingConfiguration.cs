namespace Cephalon.EventSourcing.Cassandra;

/// <summary>
/// Holds the CQL DDL template for bootstrapping the Cassandra event-store table.
/// </summary>
public static class CassandraEventSourcingConfiguration
{
    /// <summary>
    /// CQL DDL template for creating the event streams table.
    /// The <c>{0}</c> placeholder must be substituted with the fully qualified table name
    /// (optionally including keyspace prefix).
    /// </summary>
    /// <remarks>
    /// The composite primary key <c>(stream_id, stream_version)</c> with clustering order
    /// <c>ASC</c> on <c>stream_version</c> makes per-stream range scans efficient and
    /// enforces uniqueness at the partition/clustering-key level. LWT <c>INSERT IF NOT EXISTS</c>
    /// uses this same uniqueness constraint to detect concurrent-write conflicts.
    /// </remarks>
    public const string CreateTableCql =
        "\n        CREATE TABLE IF NOT EXISTS {0} (" +
        "\n            stream_id text," +
        "\n            stream_version bigint," +
        "\n            event_type text," +
        "\n            payload text," +
        "\n            occurred_at_utc timestamp," +
        "\n            appended_at_utc timestamp," +
        "\n            PRIMARY KEY (stream_id, stream_version)" +
        "\n        ) WITH CLUSTERING ORDER BY (stream_version ASC)";
}
