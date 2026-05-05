using System.Runtime.CompilerServices;
using Cassandra;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.EventSourcing.Services;

namespace Cephalon.EventSourcing.Cassandra;

/// <summary>
/// Cassandra-backed implementation of <see cref="IEventStore" /> using a wide-column table
/// with a composite primary key of <c>(stream_id, stream_version)</c> and optimistic concurrency
/// detection via Lightweight Transaction (LWT) <c>INSERT IF NOT EXISTS</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Known limitation</strong>: The version-read followed by LWT INSERT is not fully atomic.
/// Two concurrent writers that both observe the same <c>expectedVersion</c> will both proceed
/// to issue their LWT INSERT statements. The second writer's LWT will return <c>[applied]=false</c>
/// (because the clustering key is already occupied by the first writer's event), which is caught
/// and rethrown as <see cref="EventStreamConcurrencyException" />. The optimistic-concurrency
/// guard therefore holds, but it is enforced at the LWT layer rather than through a single
/// atomic compare-and-swap on the version itself.
/// </para>
/// </remarks>
public sealed class CassandraEventStore : IEventStore, IDisposable, IAsyncDisposable
{
    private readonly ICluster _cluster;
    private readonly IEventTypeRegistry _eventTypes;
    private readonly string _keyspace;
    private readonly string _tableName;
    private ISession? _session;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _tableCreated;

    /// <summary>
    /// Initializes a new instance of the <see cref="CassandraEventStore" /> class.
    /// </summary>
    /// <param name="cluster">The Cassandra cluster. Session is opened lazily on first operation.</param>
    /// <param name="keyspace">The Cassandra keyspace that contains the event-streams table.</param>
    /// <param name="tableName">The Cassandra table name used to persist event stream rows.</param>
    public CassandraEventStore(ICluster cluster, string keyspace, string tableName)
        : this(cluster, keyspace, tableName, EventTypeRegistry.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CassandraEventStore" /> class.
    /// </summary>
    /// <param name="cluster">The Cassandra cluster. Session is opened lazily on first operation.</param>
    /// <param name="keyspace">The Cassandra keyspace that contains the event-streams table.</param>
    /// <param name="tableName">The Cassandra table name used to persist event stream rows.</param>
    /// <param name="eventTypes">The closed event-type registry used to serialize and rehydrate domain events.</param>
    public CassandraEventStore(
        ICluster cluster,
        string keyspace,
        string tableName,
        IEventTypeRegistry eventTypes)
    {
        ArgumentNullException.ThrowIfNull(cluster);
        ArgumentNullException.ThrowIfNull(eventTypes);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyspace);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        _cluster = cluster;
        _eventTypes = eventTypes;
        _keyspace = keyspace;
        _tableName = tableName;
    }

    /// <inheritdoc />
    public async Task AppendAsync(
        string streamId,
        IReadOnlyCollection<IDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        ArgumentNullException.ThrowIfNull(events);

        if (events.Count == 0)
        {
            return;
        }

        var session = await GetSessionAsync(cancellationToken).ConfigureAwait(false);
        await EnsureTableAsync(session, cancellationToken).ConfigureAwait(false);

        var normalizedStreamId = streamId.Trim();
        var actualVersion = await GetVersionInternalAsync(session, normalizedStreamId, cancellationToken).ConfigureAwait(false);
        if (actualVersion != expectedVersion)
        {
            throw new EventStreamConcurrencyException(normalizedStreamId, expectedVersion, actualVersion);
        }

        var nextVersion = expectedVersion;
        var appendedAtUtc = DateTime.UtcNow;

        var insertCql = $@"INSERT INTO {_tableName} (
                stream_id, stream_version, event_type, payload, occurred_at_utc, appended_at_utc
            ) VALUES (?, ?, ?, ?, ?, ?) IF NOT EXISTS";

        var ps = await session.PrepareAsync(insertCql).ConfigureAwait(false);

        foreach (var evt in events)
        {
            ArgumentNullException.ThrowIfNull(evt);

            if (!string.Equals(evt.StreamId, normalizedStreamId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Domain event stream id '{evt.StreamId}' did not match append target '{normalizedStreamId}'.");
            }

            nextVersion++;
            if (evt.StreamVersion != nextVersion)
            {
                throw new InvalidOperationException(
                    $"Domain event '{evt.GetType().FullName}' declared stream version {evt.StreamVersion}, but the append expected version {nextVersion}.");
            }

            var eventType = _eventTypes.GetName(evt);
            var payload = _eventTypes.Serialize(evt);

            var bound = ps.Bind(
                normalizedStreamId,
                evt.StreamVersion,
                eventType,
                payload,
                evt.OccurredAtUtc,
                appendedAtUtc);

            var rowSet = await session.ExecuteAsync(bound).ConfigureAwait(false);
            var resultRow = rowSet.FirstOrDefault();
            if (resultRow is not null && resultRow["[applied]"] is bool applied && !applied)
            {
                // A concurrent writer already inserted this (stream_id, stream_version) — optimistic concurrency conflict.
                var rereadVersion = await GetVersionInternalAsync(session, normalizedStreamId, cancellationToken).ConfigureAwait(false);
                throw new EventStreamConcurrencyException(normalizedStreamId, expectedVersion, rereadVersion);
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IDomainEvent> ReadStreamAsync(
        string streamId,
        long fromVersion = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        var session = await GetSessionAsync(cancellationToken).ConfigureAwait(false);
        await EnsureTableAsync(session, cancellationToken).ConfigureAwait(false);

        var normalizedStreamId = streamId.Trim();

        var ps = await session.PrepareAsync($@"
            SELECT stream_id, stream_version, event_type, payload, occurred_at_utc, appended_at_utc
            FROM {_tableName}
            WHERE stream_id = ? AND stream_version >= ?
            ORDER BY stream_version ASC")
            .ConfigureAwait(false);

        var rowSet = await session.ExecuteAsync(ps.Bind(normalizedStreamId, fromVersion)).ConfigureAwait(false);

        foreach (var row in rowSet)
        {
            var eventTypeName = row["event_type"] as string
                ?? throw new InvalidOperationException($"Null event_type encountered while reading stream '{normalizedStreamId}'.");

            var payloadJson = row["payload"] as string
                ?? throw new InvalidOperationException($"Null payload encountered while reading stream '{normalizedStreamId}'.");

            yield return _eventTypes.Deserialize(eventTypeName, payloadJson);
        }
    }

    /// <inheritdoc />
    public async Task<long> GetVersionAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        var session = await GetSessionAsync(cancellationToken).ConfigureAwait(false);
        await EnsureTableAsync(session, cancellationToken).ConfigureAwait(false);

        return await GetVersionInternalAsync(session, streamId.Trim(), cancellationToken).ConfigureAwait(false);
    }

    private async Task<long> GetVersionInternalAsync(ISession session, string normalizedStreamId, CancellationToken cancellationToken)
    {
        var ps = await session.PrepareAsync($"SELECT MAX(stream_version) AS max_version FROM {_tableName} WHERE stream_id = ?")
            .ConfigureAwait(false);

        var rowSet = await session.ExecuteAsync(ps.Bind(normalizedStreamId)).ConfigureAwait(false);
        var row = rowSet.FirstOrDefault();

        if (row is null || row["max_version"] is null or DBNull)
        {
            return -1;
        }

        return (long)row["max_version"];
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
            _session ??= await _cluster.ConnectAsync(_keyspace).ConfigureAwait(false);
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

        var createCql = CassandraEventSourcingConfiguration.CreateTableCql.Replace("{0}", _tableName, StringComparison.Ordinal);

        await session.ExecuteAsync(new SimpleStatement(createCql)).ConfigureAwait(false);
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
