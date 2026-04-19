using System.Text.Json;
using ClickHouse.Driver.ADO;
using Cephalon.Abstractions.Data;
using Cephalon.Data.ClickHouse.Configuration;

namespace Cephalon.Data.ClickHouse.Services;

/// <summary>
/// ClickHouse-backed outbox implementation that stages messages for durable delivery using
/// a <c>ReplacingMergeTree</c> table ordered by <c>(message_id)</c> for eventual deduplication.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Idempotency semantics</strong>: ClickHouse <c>ReplacingMergeTree</c> deduplicates rows
/// with the same <c>ORDER BY</c> key (<c>message_id</c>) asynchronously during background merges.
/// Immediate idempotency is not guaranteed — duplicate rows may co-exist until the next merge cycle.
/// Use <c>SELECT ... FINAL</c> to force deduplication at read time. This provider is suitable for
/// high-throughput analytics-oriented outbox workloads where eventual deduplication is acceptable.
/// </para>
/// </remarks>
internal sealed class ClickHouseOutbox : IOutbox, IDisposable
{
    private readonly ClickHouseDataOptions _options;
    private volatile bool _tableCreated;
    private readonly SemaphoreSlim _tableLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="ClickHouseOutbox" /> class.
    /// </summary>
    /// <param name="options">The ClickHouse data options controlling connection and table names.</param>
    public ClickHouseOutbox(ClickHouseDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    public string OutboxId => "clickhouse-outbox";

    /// <inheritdoc />
    public async ValueTask EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var tableName = $"{_options.TablePrefix}outbox_messages";
        using var connection = new ClickHouseConnection(_options.BuildConnectionString());
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = $@"INSERT INTO {tableName}
            (message_id, channel_id, message_type, payload, content_type,
             correlation_id, tenant_id, occurred_at_utc, created_at_utc,
             dispatch_attempt_count, headers_json, metadata_json)
            VALUES (
                {EscapeString(message.Id)},
                {EscapeString(message.ChannelId)},
                {EscapeString(message.MessageType)},
                {EscapeString(message.Payload)},
                {EscapeString(message.ContentType ?? string.Empty)},
                {EscapeString(message.CorrelationId ?? string.Empty)},
                {EscapeString(message.TenantId ?? string.Empty)},
                {EscapeDateTime(message.OccurredAtUtc.UtcDateTime)},
                {EscapeDateTime(DateTime.UtcNow)},
                0,
                {EscapeString(JsonSerializer.Serialize(message.Headers))},
                {EscapeString(JsonSerializer.Serialize(message.Metadata))}
            )";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
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

            var tableName = $"{_options.TablePrefix}outbox_messages";
            using var connection = new ClickHouseConnection(_options.BuildConnectionString());
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                CREATE TABLE IF NOT EXISTS {tableName} (
                    message_id String,
                    channel_id String,
                    message_type String,
                    payload String,
                    content_type String,
                    correlation_id String,
                    tenant_id String,
                    occurred_at_utc DateTime64(3, 'UTC'),
                    created_at_utc DateTime64(3, 'UTC'),
                    dispatch_attempt_count Int32,
                    headers_json String,
                    metadata_json String
                ) ENGINE = ReplacingMergeTree(created_at_utc)
                ORDER BY (message_id)";
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
