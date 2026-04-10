using Cassandra;
using Cephalon.Data.Cassandra.Configuration;

namespace Cephalon.Data.Cassandra.Services;

/// <summary>
/// Provides the shared Cassandra outbox storage table names and DDL statements used by the outbox and dispatch-store services.
/// </summary>
internal static class CassandraOutboxStorageSchema
{
    public static string GetMessagesTableName(CassandraDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return $"{options.TablePrefix}outbox_messages";
    }

    public static string GetPendingDispatchTableName(CassandraDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return $"{options.TablePrefix}outbox_pending_dispatch";
    }

    public static SimpleStatement CreateMessagesTable(CassandraDataOptions options)
    {
        var tableName = GetMessagesTableName(options);
        return new SimpleStatement($@"
            CREATE TABLE IF NOT EXISTS {tableName} (
                message_id text PRIMARY KEY,
                channel_id text,
                message_type text,
                payload text,
                content_type text,
                correlation_id text,
                tenant_id text,
                occurred_at_utc timestamp,
                created_at_utc timestamp,
                dispatched_at_utc timestamp,
                dispatch_attempt_count int,
                next_attempt_at_utc timestamp,
                headers_json text,
                metadata_json text
            )");
    }

    public static IReadOnlyList<SimpleStatement> CreateMessagesTableUpgradeStatements(CassandraDataOptions options)
    {
        var tableName = GetMessagesTableName(options);
        return
        [
            new SimpleStatement($"ALTER TABLE {tableName} ADD IF NOT EXISTS dispatched_at_utc timestamp"),
            new SimpleStatement($"ALTER TABLE {tableName} ADD IF NOT EXISTS next_attempt_at_utc timestamp")
        ];
    }

    public static SimpleStatement CreatePendingDispatchTable(CassandraDataOptions options)
    {
        var tableName = GetPendingDispatchTableName(options);
        return new SimpleStatement($@"
            CREATE TABLE IF NOT EXISTS {tableName} (
                shard_id int,
                eligible_at_utc timestamp,
                message_id text,
                channel_id text,
                message_type text,
                payload text,
                content_type text,
                correlation_id text,
                tenant_id text,
                occurred_at_utc timestamp,
                created_at_utc timestamp,
                dispatch_attempt_count int,
                headers_json text,
                metadata_json text,
                PRIMARY KEY ((shard_id), eligible_at_utc, message_id)
            ) WITH CLUSTERING ORDER BY (eligible_at_utc ASC, message_id ASC)");
    }
}
