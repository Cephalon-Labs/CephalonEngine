using ClickHouse.Driver.ADO;
using Cephalon.Abstractions.Data;
using Cephalon.Data.ClickHouse.Configuration;

namespace Cephalon.Data.ClickHouse.Services;

/// <summary>
/// ClickHouse-backed inbox implementation that tracks processed messages for idempotent handling
/// using a <c>ReplacingMergeTree</c> table ordered by <c>(message_id)</c> for eventual deduplication.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Idempotency semantics</strong>: <see cref="HasProcessedAsync" /> issues a <c>SELECT ... FINAL</c>
/// query which forces ClickHouse to deduplicate rows at read time. <c>MarkProcessedAsync</c> inserts a
/// receipt row; duplicates are eventually merged by ClickHouse background merges. This pattern is suitable
/// for analytics-oriented workloads where eventual deduplication is acceptable.
/// </para>
/// </remarks>
internal sealed class ClickHouseInbox : IInbox, IDisposable
{
    private readonly ClickHouseDataOptions _options;
    private volatile bool _tableCreated;
    private readonly SemaphoreSlim _tableLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="ClickHouseInbox" /> class.
    /// </summary>
    /// <param name="options">The ClickHouse data options controlling connection and table names.</param>
    public ClickHouseInbox(ClickHouseDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    public async ValueTask<bool> HasProcessedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var tableName = $"{_options.TablePrefix}inbox_receipts";
        using var connection = new ClickHouseConnection(_options.BuildConnectionString());
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        // FINAL forces deduplication at read time for ReplacingMergeTree.
        command.CommandText = $"SELECT count() as cnt FROM {tableName} FINAL WHERE message_id = {EscapeString(messageId.Trim())}";

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var cnt = reader["cnt"];
            if (cnt is not null and not DBNull)
            {
                return Convert.ToInt64(cnt, System.Globalization.CultureInfo.InvariantCulture) > 0;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public async ValueTask MarkProcessedAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var tableName = $"{_options.TablePrefix}inbox_receipts";
        using var connection = new ClickHouseConnection(_options.BuildConnectionString());
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = $@"INSERT INTO {tableName}
            (message_id, channel_id, message_type, correlation_id, tenant_id,
             received_at_utc, processed_at_utc)
            VALUES (
                {EscapeString(message.Id)},
                {EscapeString(message.ChannelId)},
                {EscapeString(message.MessageType)},
                {EscapeString(message.CorrelationId ?? string.Empty)},
                {EscapeString(message.TenantId ?? string.Empty)},
                {EscapeDateTime(message.ReceivedAtUtc.UtcDateTime)},
                {EscapeDateTime(DateTime.UtcNow)}
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

            var tableName = $"{_options.TablePrefix}inbox_receipts";
            using var connection = new ClickHouseConnection(_options.BuildConnectionString());
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                CREATE TABLE IF NOT EXISTS {tableName} (
                    message_id String,
                    channel_id String,
                    message_type String,
                    correlation_id String,
                    tenant_id String,
                    received_at_utc DateTime64(3, 'UTC'),
                    processed_at_utc DateTime64(3, 'UTC')
                ) ENGINE = ReplacingMergeTree(processed_at_utc)
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
