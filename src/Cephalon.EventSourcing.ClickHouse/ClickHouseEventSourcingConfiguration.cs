namespace Cephalon.EventSourcing.ClickHouse;

/// <summary>
/// Holds the DDL template for bootstrapping the ClickHouse event-store table.
/// </summary>
public static class ClickHouseEventSourcingConfiguration
{
    /// <summary>
    /// DDL template for creating the event streams table.
    /// The <c>{0}</c> placeholder must be substituted with the fully qualified table name.
    /// </summary>
    /// <remarks>
    /// The <c>MergeTree</c> engine is used for event streams because domain events are immutable —
    /// no deduplication or replacement is needed. The <c>ORDER BY (stream_id, stream_version)</c>
    /// clause makes per-stream range scans efficient and stores events in version order within
    /// each stream partition. Unlike <c>ReplacingMergeTree</c>, <c>MergeTree</c> does not merge
    /// duplicate rows — each appended event row is preserved permanently.
    /// </remarks>
    public const string CreateTableSql = @"
        CREATE TABLE IF NOT EXISTS {0} (
            stream_id String,
            stream_version Int64,
            event_type String,
            payload String,
            occurred_at_utc DateTime64(3, 'UTC'),
            appended_at_utc DateTime64(3, 'UTC')
        ) ENGINE = MergeTree()
        ORDER BY (stream_id, stream_version)";
}
