using Cassandra;
using Cephalon.Abstractions.Data;
using Cephalon.Data.Cassandra.Configuration;

namespace Cephalon.Data.Cassandra.Services;

/// <summary>
/// Cassandra-backed inbox implementation that tracks processed messages for idempotent handling
/// using Lightweight Transaction (LWT) <c>INSERT IF NOT EXISTS</c>.
/// </summary>
internal sealed class CassandraInbox : IInbox, IDisposable, IAsyncDisposable
{
    private readonly ICluster _cluster;
    private readonly CassandraDataOptions _options;
    private ISession? _session;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _tableCreated;

    /// <summary>
    /// Initializes a new instance of the <see cref="CassandraInbox" /> class.
    /// </summary>
    /// <param name="cluster">The Cassandra cluster used to open sessions lazily.</param>
    /// <param name="options">The Cassandra data options controlling keyspace and table names.</param>
    public CassandraInbox(ICluster cluster, CassandraDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(cluster);
        ArgumentNullException.ThrowIfNull(options);
        _cluster = cluster;
        _options = options;
    }

    /// <inheritdoc />
    public async ValueTask<bool> HasProcessedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var session = await GetSessionAsync(cancellationToken).ConfigureAwait(false);
        await EnsureTableAsync(session, cancellationToken).ConfigureAwait(false);

        var tableName = $"{_options.TablePrefix}inbox_receipts";
        var ps = await session.PrepareAsync($"SELECT message_id FROM {tableName} WHERE message_id = ? LIMIT 1")
            .ConfigureAwait(false);

        var rowSet = await session.ExecuteAsync(ps.Bind(messageId.Trim())).ConfigureAwait(false);
        return rowSet.Any();
    }

    /// <inheritdoc />
    public async ValueTask MarkProcessedAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var session = await GetSessionAsync(cancellationToken).ConfigureAwait(false);
        await EnsureTableAsync(session, cancellationToken).ConfigureAwait(false);

        var tableName = $"{_options.TablePrefix}inbox_receipts";
        var ps = await session.PrepareAsync($@"
            INSERT INTO {tableName} (
                message_id, channel_id, message_type, correlation_id, tenant_id,
                received_at_utc, processed_at_utc
            ) VALUES (?, ?, ?, ?, ?, ?, ?) IF NOT EXISTS")
            .ConfigureAwait(false);

        // LWT INSERT IF NOT EXISTS: [applied]=false means already recorded — idempotent, not an error.
        await session.ExecuteAsync(ps.Bind(
            message.Id,
            message.ChannelId,
            message.MessageType,
            message.CorrelationId ?? string.Empty,
            message.TenantId ?? string.Empty,
            message.ReceivedAtUtc.UtcDateTime,
            DateTime.UtcNow)).ConfigureAwait(false);
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

        var tableName = $"{_options.TablePrefix}inbox_receipts";
        await session.ExecuteAsync(new SimpleStatement($@"
            CREATE TABLE IF NOT EXISTS {tableName} (
                message_id text PRIMARY KEY,
                channel_id text,
                message_type text,
                correlation_id text,
                tenant_id text,
                received_at_utc timestamp,
                processed_at_utc timestamp
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
