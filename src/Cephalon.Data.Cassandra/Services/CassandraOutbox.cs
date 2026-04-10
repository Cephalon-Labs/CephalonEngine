using System.Text.Json;
using Cassandra;
using Cephalon.Abstractions.Data;
using Cephalon.Data.Cassandra.Configuration;

namespace Cephalon.Data.Cassandra.Services;

/// <summary>
/// Cassandra-backed outbox implementation that stages messages for durable delivery using
/// Lightweight Transaction (LWT) <c>INSERT IF NOT EXISTS</c> for idempotent staging.
/// </summary>
internal sealed class CassandraOutbox : IOutbox, IDisposable, IAsyncDisposable
{
    private readonly ICluster _cluster;
    private readonly CassandraDataOptions _options;
    private ISession? _session;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly SemaphoreSlim _storageLock = new(1, 1);
    private bool _storageReady;
    private PreparedStatement? _insertMessageStatement;
    private PreparedStatement? _selectMessageStatement;
    private PreparedStatement? _insertPendingStatement;

    /// <summary>
    /// Initializes a new instance of the <see cref="CassandraOutbox" /> class.
    /// </summary>
    /// <param name="cluster">The Cassandra cluster used to open sessions lazily.</param>
    /// <param name="options">The Cassandra data options controlling keyspace and table names.</param>
    public CassandraOutbox(ICluster cluster, CassandraDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(cluster);
        ArgumentNullException.ThrowIfNull(options);
        _cluster = cluster;
        _options = options;
    }

    /// <inheritdoc />
    public async ValueTask EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var session = await GetSessionAsync(cancellationToken).ConfigureAwait(false);
        await EnsureStorageAsync(session, cancellationToken).ConfigureAwait(false);

        var record = CassandraOutboxRecord.Create(message, DateTimeOffset.UtcNow);
        var appliedRowSet = await session.ExecuteAsync(_insertMessageStatement!.Bind(
                record.MessageId,
                record.ChannelId,
                record.MessageType,
                record.Payload,
                record.ContentType ?? string.Empty,
                record.CorrelationId ?? string.Empty,
                record.TenantId ?? string.Empty,
                record.OccurredAtUtc.UtcDateTime,
                record.CreatedAtUtc.UtcDateTime,
                record.DispatchedAtUtc?.UtcDateTime,
                record.DispatchAttemptCount,
                record.NextAttemptAtUtc?.UtcDateTime,
                JsonSerializer.Serialize(record.Headers),
                JsonSerializer.Serialize(record.Metadata)))
            .ConfigureAwait(false);

        if (WasApplied(appliedRowSet))
        {
            await UpsertPendingAsync(session, record).ConfigureAwait(false);
            return;
        }

        var existing = await TryGetRecordAsync(session, record.MessageId).ConfigureAwait(false);
        if (existing is null || existing.DispatchedAtUtc is not null)
        {
            return;
        }

        await UpsertPendingAsync(session, existing).ConfigureAwait(false);
    }

    private async Task<ISession> GetSessionAsync(CancellationToken cancellationToken)
    {
        if (_session is not null)
        {
            return _session;
        }

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _session ??= await _cluster.ConnectAsync(_options.Keyspace).ConfigureAwait(false);
        }
        finally
        {
            _initLock.Release();
        }

        return _session;
    }

    private async Task EnsureStorageAsync(ISession session, CancellationToken cancellationToken)
    {
        if (_storageReady)
        {
            return;
        }

        await _storageLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_storageReady)
            {
                return;
            }

            await session.ExecuteAsync(CassandraOutboxStorageSchema.CreateMessagesTable(_options)).ConfigureAwait(false);
            foreach (var statement in CassandraOutboxStorageSchema.CreateMessagesTableUpgradeStatements(_options))
            {
                await session.ExecuteAsync(statement).ConfigureAwait(false);
            }

            await session.ExecuteAsync(CassandraOutboxStorageSchema.CreatePendingDispatchTable(_options)).ConfigureAwait(false);

            var messagesTable = CassandraOutboxStorageSchema.GetMessagesTableName(_options);
            var pendingTable = CassandraOutboxStorageSchema.GetPendingDispatchTableName(_options);

            _insertMessageStatement ??= await session.PrepareAsync($@"
                INSERT INTO {messagesTable} (
                    message_id, channel_id, message_type, payload, content_type,
                    correlation_id, tenant_id, occurred_at_utc, created_at_utc,
                    dispatched_at_utc, dispatch_attempt_count, next_attempt_at_utc,
                    headers_json, metadata_json
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?) IF NOT EXISTS")
                .ConfigureAwait(false);

            _selectMessageStatement ??= await session.PrepareAsync($@"
                SELECT message_id, channel_id, message_type, payload, content_type,
                       correlation_id, tenant_id, occurred_at_utc, created_at_utc,
                       dispatched_at_utc, dispatch_attempt_count, next_attempt_at_utc,
                       headers_json, metadata_json
                FROM {messagesTable}
                WHERE message_id = ?")
                .ConfigureAwait(false);

            _insertPendingStatement ??= await session.PrepareAsync($@"
                INSERT INTO {pendingTable} (
                    shard_id, eligible_at_utc, message_id, channel_id, message_type, payload,
                    content_type, correlation_id, tenant_id, occurred_at_utc, created_at_utc,
                    dispatch_attempt_count, headers_json, metadata_json
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)")
                .ConfigureAwait(false);

            _storageReady = true;
        }
        finally
        {
            _storageLock.Release();
        }
    }

    private async Task<CassandraOutboxRecord?> TryGetRecordAsync(ISession session, string messageId)
    {
        var rowSet = await session.ExecuteAsync(_selectMessageStatement!.Bind(messageId.Trim())).ConfigureAwait(false);
        var row = rowSet.FirstOrDefault();
        return row is null
            ? null
            : CassandraOutboxRecord.FromRow(row);
    }

    private async Task UpsertPendingAsync(ISession session, CassandraOutboxRecord record)
    {
        await session.ExecuteAsync(_insertPendingStatement!.Bind(
                record.ComputePendingDispatchShard(_options.PendingDispatchShardCount),
                record.EligibleAtUtc.UtcDateTime,
                record.MessageId,
                record.ChannelId,
                record.MessageType,
                record.Payload,
                record.ContentType ?? string.Empty,
                record.CorrelationId ?? string.Empty,
                record.TenantId ?? string.Empty,
                record.OccurredAtUtc.UtcDateTime,
                record.CreatedAtUtc.UtcDateTime,
                record.DispatchAttemptCount,
                JsonSerializer.Serialize(record.Headers),
                JsonSerializer.Serialize(record.Metadata)))
            .ConfigureAwait(false);
    }

    private static bool WasApplied(RowSet rowSet)
    {
        var row = rowSet.FirstOrDefault();
        return row is null || row.GetValue<bool>("[applied]");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _storageLock.Dispose();
        _initLock.Dispose();
        _session?.Dispose();
        _session = null;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
