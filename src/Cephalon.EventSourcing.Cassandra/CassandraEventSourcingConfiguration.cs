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
    public const string CreateTableCql = @"
        CREATE TABLE IF NOT EXISTS {0} (
            stream_id text,
            stream_version bigint,
            event_type text,
            payload text,
            occurred_at_utc timestamp,
            appended_at_utc timestamp,
            PRIMARY KEY (stream_id, stream_version)
        ) WITH CLUSTERING ORDER BY (stream_version ASC)";
}
