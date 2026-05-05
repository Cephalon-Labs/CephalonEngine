using System.Runtime.CompilerServices;
using ClickHouse.Driver.ADO;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.EventSourcing.Services;

namespace Cephalon.EventSourcing.ClickHouse;

/// <summary>
/// ClickHouse-backed implementation of <see cref="IEventStore" /> using a <c>MergeTree</c> table
/// ordered by <c>(stream_id, stream_version)</c> for efficient per-stream range scans and append operations.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Known limitation — optimistic concurrency</strong>: ClickHouse <c>MergeTree</c> does NOT enforce
/// uniqueness on the <c>ORDER BY</c> key. Two concurrent writers that both observe the same
/// <c>expectedVersion</c> can both INSERT successfully — there is no database-level rejection.
/// The pre-read version check in <see cref="AppendAsync" /> catches most single-process conflicts
/// and throws <see cref="EventStreamConcurrencyException" />, but a race window remains between
/// the version read and the INSERT. For strict concurrency control, wrap append operations in
/// application-layer distributed locking.
/// </para>
/// <para>
/// ClickHouse is optimized for append-heavy analytics workloads. This provider is well-suited to
/// high-throughput event append scenarios where occasional duplicate detection at the application
/// layer is acceptable.
/// </para>
/// </remarks>
public sealed class ClickHouseEventStore : IEventStore, IDisposable
{
    private readonly string _connectionString;
    private readonly IEventTypeRegistry _eventTypes;
    private readonly string _tableName;
    private volatile bool _tableCreated;
    private readonly SemaphoreSlim _tableLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="ClickHouseEventStore" /> class.
    /// </summary>
    /// <param name="connectionString">The ClickHouse ADO.NET connection string.</param>
    /// <param name="tableName">The ClickHouse table name used to persist event stream rows.</param>
    public ClickHouseEventStore(string connectionString, string tableName)
        : this(connectionString, tableName, EventTypeRegistry.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClickHouseEventStore" /> class.
    /// </summary>
    /// <param name="connectionString">The ClickHouse ADO.NET connection string.</param>
    /// <param name="tableName">The ClickHouse table name used to persist event stream rows.</param>
    /// <param name="eventTypes">The closed event-type registry used to serialize and rehydrate domain events.</param>
    public ClickHouseEventStore(
        string connectionString,
        string tableName,
        IEventTypeRegistry eventTypes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentNullException.ThrowIfNull(eventTypes);
        _connectionString = connectionString;
        _eventTypes = eventTypes;
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

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var normalizedStreamId = streamId.Trim();
        var actualVersion = await GetVersionInternalAsync(normalizedStreamId, cancellationToken).ConfigureAwait(false);
        if (actualVersion != expectedVersion)
        {
            throw new EventStreamConcurrencyException(normalizedStreamId, expectedVersion, actualVersion);
        }

        var nextVersion = expectedVersion;
        var appendedAtUtc = DateTime.UtcNow;

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

            using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = $@"INSERT INTO {_tableName}
                (stream_id, stream_version, event_type, payload, occurred_at_utc, appended_at_utc)
                VALUES (
                    {EscapeString(normalizedStreamId)},
                    {evt.StreamVersion},
                    {EscapeString(eventType)},
                    {EscapeString(payload)},
                    {EscapeDateTime(evt.OccurredAtUtc)},
                    {EscapeDateTime(appendedAtUtc)}
                )";
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
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

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var normalizedStreamId = streamId.Trim();

        using var connection = new ClickHouseConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = $@"
            SELECT stream_id, stream_version, event_type, payload, occurred_at_utc, appended_at_utc
            FROM {_tableName}
            WHERE stream_id = {EscapeString(normalizedStreamId)} AND stream_version >= {fromVersion}
            ORDER BY stream_version ASC";

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var eventTypeName = reader["event_type"] as string
                ?? throw new InvalidOperationException($"Null event_type encountered while reading stream '{normalizedStreamId}'.");

            var payloadJson = reader["payload"] as string
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

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        return await GetVersionInternalAsync(streamId.Trim(), cancellationToken).ConfigureAwait(false);
    }

    private async Task<long> GetVersionInternalAsync(string normalizedStreamId, CancellationToken cancellationToken)
    {
        using var connection = new ClickHouseConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT max(stream_version) as max_version FROM {_tableName} WHERE stream_id = {EscapeString(normalizedStreamId)}";

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var maxVersion = reader["max_version"];
            if (maxVersion is not null and not DBNull)
            {
                return Convert.ToInt64(maxVersion, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        return -1;
    }

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        if (_tableCreated)
        {
            return;
        }

        await _tableLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_tableCreated)
            {
                return;
            }

            var createSql = ClickHouseEventSourcingConfiguration.CreateTableSql.Replace("{0}", _tableName, StringComparison.Ordinal);

            using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = createSql;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            _tableCreated = true;
        }
        finally
        {
            _tableLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _tableLock.Dispose();
    }

    private static string EscapeString(string value) =>
        $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";

    private static string EscapeDateTime(DateTime dt) =>
        $"'{dt:yyyy-MM-dd HH:mm:ss.fff}'";
}
