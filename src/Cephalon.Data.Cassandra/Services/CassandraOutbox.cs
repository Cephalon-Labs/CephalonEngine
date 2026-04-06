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
    private bool _tableCreated;

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
        await EnsureTableAsync(session, cancellationToken).ConfigureAwait(false);

        var tableName = $"{_options.TablePrefix}outbox_messages";
        var ps = await session.PrepareAsync($@"
            INSERT INTO {tableName} (
                message_id, channel_id, message_type, payload, content_type,
                correlation_id, tenant_id, occurred_at_utc, created_at_utc,
                dispatch_attempt_count, headers_json, metadata_json
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?) IF NOT EXISTS")
            .ConfigureAwait(false);

        var bound = ps.Bind(
            message.Id,
            message.ChannelId,
            message.MessageType,
            message.Payload,
            message.ContentType ?? string.Empty,
            message.CorrelationId ?? string.Empty,
            message.TenantId ?? string.Empty,
            message.OccurredAtUtc.UtcDateTime,
            DateTime.UtcNow,
            0,
            JsonSerializer.Serialize(message.Headers),
            JsonSerializer.Serialize(message.Metadata));

        // LWT INSERT IF NOT EXISTS: [applied]=false means already staged — idempotent, not an error.
        await session.ExecuteAsync(bound).ConfigureAwait(false);
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

    private async Task EnsureTableAsync(ISession session, CancellationToken cancellationToken)
    {
        if (_tableCreated)
        {
            return;
        }

        var tableName = $"{_options.TablePrefix}outbox_messages";
        await session.ExecuteAsync(new SimpleStatement($@"
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
                dispatch_attempt_count int,
                headers_json text,
                metadata_json text
            )")).ConfigureAwait(false);

        _tableCreated = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
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
