using System.Data;
using System.Globalization;
using System.Text.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Data.SqlServer.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Cephalon.Data.SqlServer.Services;

internal sealed class SqlServerCdcTransport(
    string connectionString,
    SqlServerDataOptions options,
    ILogger<SqlServerCdcTransport> logger) : ISqlServerCdcTransport
{
    private const string ContentType = "application/vnd.cephalon.sqlserver.cdc+json";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly Action<ILogger, string, string, Exception?> LogBootstrappedLatestCheckpointMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(6960, nameof(LogBootstrappedLatestCheckpoint)),
            "SQL Server CDC capture '{CdcCaptureId}' bootstrapped the latest checkpoint in store '{CheckpointStore}'.");
    private readonly Lock checkpointInitializationGate = new();
    private bool checkpointStoreEnsured;

    public async Task<SqlServerCdcReadBatch> ReadBatchAsync(
        SqlServerCdcCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(captureOptions);
        ArgumentNullException.ThrowIfNull(descriptor);

        ValidateCaptureOptions(captureOptions);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await EnsureCheckpointStoreAsync(connection, cancellationToken).ConfigureAwait(false);
        await EnsureCaptureInstanceExistsAsync(connection, captureOptions, cancellationToken).ConfigureAwait(false);

        var checkpoint = await ReadCheckpointAsync(connection, descriptor.Id, cancellationToken).ConfigureAwait(false);
        if (checkpoint is null &&
            string.Equals(
                NormalizeInitialPosition(captureOptions.InitialPosition),
                "latest-available",
                StringComparison.OrdinalIgnoreCase))
        {
            var latestCheckpoint = await GetLatestCheckpointAsync(connection, captureOptions, cancellationToken).ConfigureAwait(false)
                ?? SqlServerCdcCheckpointToken.Zero(await GetCurrentDatabaseLsnAsync(connection, cancellationToken).ConfigureAwait(false));

            await UpsertCheckpointAsync(connection, descriptor.Id, latestCheckpoint, cancellationToken).ConfigureAwait(false);
            LogBootstrappedLatestCheckpoint(logger, descriptor.Id, GetCheckpointStoreName());

            return SqlServerCdcReadBatch.Idle(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["checkpointStore"] = GetCheckpointStoreName(),
                ["checkpointInitialization"] = "latest-available"
            });
        }

        var limit = Math.Max(1, captureOptions.MaxChangesPerPoll);
        var rows = await ReadChangesAsync(
                connection,
                captureOptions,
                descriptor.SourceId,
                checkpoint,
                limit + 1,
                cancellationToken)
            .ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return SqlServerCdcReadBatch.Idle(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["checkpointStore"] = GetCheckpointStoreName()
            });
        }

        var hasMoreChanges = rows.Count > limit;
        if (hasMoreChanges)
        {
            rows = rows.Take(limit).ToArray();
        }

        return new SqlServerCdcReadBatch(
            rows,
            hasMoreChanges,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["checkpointStore"] = GetCheckpointStoreName()
            });
    }

    public async Task CommitCheckpointAsync(
        SqlServerCdcCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        SqlServerCdcCheckpointToken checkpointToken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(captureOptions);
        ArgumentNullException.ThrowIfNull(descriptor);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await EnsureCheckpointStoreAsync(connection, cancellationToken).ConfigureAwait(false);
        await UpsertCheckpointAsync(connection, descriptor.Id, checkpointToken, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureCheckpointStoreAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);

        lock (checkpointInitializationGate)
        {
            if (checkpointStoreEnsured)
            {
                return;
            }
        }

        var schemaName = EscapeIdentifier(options.CheckpointTableSchema.Trim());
        var tableName = EscapeIdentifier(options.CheckpointTableName.Trim());
        var objectIdName = EscapeSqlLiteral($"{options.CheckpointTableSchema.Trim()}.{options.CheckpointTableName.Trim()}");

        var commandText = $"""
IF OBJECT_ID(N'{objectIdName}', N'U') IS NULL
BEGIN
    CREATE TABLE [{schemaName}].[{tableName}]
    (
        [CdcCaptureId] nvarchar(128) NOT NULL,
        [CheckpointStartLsn] varbinary(10) NOT NULL,
        [CheckpointSequenceValue] varbinary(10) NOT NULL,
        [CheckpointOperation] int NOT NULL,
        [CheckpointToken] nvarchar(256) NOT NULL,
        [UpdatedAtUtc] datetimeoffset(7) NOT NULL,
        CONSTRAINT [PK_{tableName}_CdcCaptureId] PRIMARY KEY ([CdcCaptureId])
    );
END;
""";

        await using var command = new SqlCommand(commandText, connection);
        _ = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        lock (checkpointInitializationGate)
        {
            checkpointStoreEnsured = true;
        }
    }

    private static async Task EnsureCaptureInstanceExistsAsync(
        SqlConnection connection,
        SqlServerCdcCaptureOptions captureOptions,
        CancellationToken cancellationToken)
    {
        var commandText = """
SELECT TOP (1) ct.start_lsn
FROM cdc.change_tables AS ct
WHERE ct.capture_instance = @captureInstance;
""";

        await using var command = new SqlCommand(commandText, connection);
        command.Parameters.Add(new SqlParameter("@captureInstance", SqlDbType.NVarChar, 128)
        {
            Value = captureOptions.CaptureInstance.Trim()
        });

        var scalar = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (scalar is null || scalar == DBNull.Value)
        {
            throw new InvalidOperationException(
                $"SQL Server CDC capture instance '{captureOptions.CaptureInstance}' was not found. Enable CDC for '{captureOptions.TableSchema}.{captureOptions.TableName}' before starting the Cephalon SQL Server CDC runner.");
        }
    }

    private static async Task<SqlServerCdcCheckpointToken?> GetLatestCheckpointAsync(
        SqlConnection connection,
        SqlServerCdcCaptureOptions captureOptions,
        CancellationToken cancellationToken)
    {
        var commandText = $"""
SELECT TOP (1)
    ct.__$start_lsn,
    ct.__$seqval,
    ct.__$operation
FROM {GetChangeTableReference(captureOptions.CaptureInstance)}
ORDER BY
    ct.__$start_lsn DESC,
    ct.__$seqval DESC,
    ct.__$operation DESC;
""";

        await using var command = new SqlCommand(commandText, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new SqlServerCdcCheckpointToken(
            reader.GetFieldValue<byte[]>(0),
            reader.GetFieldValue<byte[]>(1),
            reader.GetInt32(2));
    }

    private static async Task<byte[]> GetCurrentDatabaseLsnAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT sys.fn_cdc_get_max_lsn();", connection);
        var scalar = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return scalar switch
        {
            byte[] bytes => bytes,
            _ => new byte[10]
        };
    }

    private async Task<SqlServerCdcCheckpointToken?> ReadCheckpointAsync(
        SqlConnection connection,
        string cdcCaptureId,
        CancellationToken cancellationToken)
    {
        var commandText = $"""
SELECT
    [CheckpointStartLsn],
    [CheckpointSequenceValue],
    [CheckpointOperation]
FROM [{EscapeIdentifier(options.CheckpointTableSchema.Trim())}].[{EscapeIdentifier(options.CheckpointTableName.Trim())}]
WHERE [CdcCaptureId] = @cdcCaptureId;
""";

        await using var command = new SqlCommand(commandText, connection);
        command.Parameters.Add(new SqlParameter("@cdcCaptureId", SqlDbType.NVarChar, 128)
        {
            Value = cdcCaptureId
        });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new SqlServerCdcCheckpointToken(
            reader.GetFieldValue<byte[]>(0),
            reader.GetFieldValue<byte[]>(1),
            reader.GetInt32(2));
    }

    private async Task UpsertCheckpointAsync(
        SqlConnection connection,
        string cdcCaptureId,
        SqlServerCdcCheckpointToken checkpointToken,
        CancellationToken cancellationToken)
    {
        var commandText = $"""
UPDATE [{EscapeIdentifier(options.CheckpointTableSchema.Trim())}].[{EscapeIdentifier(options.CheckpointTableName.Trim())}]
SET
    [CheckpointStartLsn] = @checkpointStartLsn,
    [CheckpointSequenceValue] = @checkpointSequenceValue,
    [CheckpointOperation] = @checkpointOperation,
    [CheckpointToken] = @checkpointToken,
    [UpdatedAtUtc] = SYSUTCDATETIME()
WHERE [CdcCaptureId] = @cdcCaptureId;

IF @@ROWCOUNT = 0
BEGIN
    INSERT INTO [{EscapeIdentifier(options.CheckpointTableSchema.Trim())}].[{EscapeIdentifier(options.CheckpointTableName.Trim())}]
    (
        [CdcCaptureId],
        [CheckpointStartLsn],
        [CheckpointSequenceValue],
        [CheckpointOperation],
        [CheckpointToken],
        [UpdatedAtUtc]
    )
    VALUES
    (
        @cdcCaptureId,
        @checkpointStartLsn,
        @checkpointSequenceValue,
        @checkpointOperation,
        @checkpointToken,
        SYSUTCDATETIME()
    );
END;
""";

        await using var command = new SqlCommand(commandText, connection);
        command.Parameters.Add(new SqlParameter("@cdcCaptureId", SqlDbType.NVarChar, 128) { Value = cdcCaptureId });
        command.Parameters.Add(new SqlParameter("@checkpointStartLsn", SqlDbType.VarBinary, 10) { Value = checkpointToken.StartLsn });
        command.Parameters.Add(new SqlParameter("@checkpointSequenceValue", SqlDbType.VarBinary, 10) { Value = checkpointToken.SequenceValue });
        command.Parameters.Add(new SqlParameter("@checkpointOperation", SqlDbType.Int) { Value = checkpointToken.Operation });
        command.Parameters.Add(new SqlParameter("@checkpointToken", SqlDbType.NVarChar, 256) { Value = checkpointToken.Serialize() });
        _ = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<SqlServerCdcCapturedChange>> ReadChangesAsync(
        SqlConnection connection,
        SqlServerCdcCaptureOptions captureOptions,
        string descriptorSourceId,
        SqlServerCdcCheckpointToken? checkpointToken,
        int topCount,
        CancellationToken cancellationToken)
    {
        var commandText = $"""
SELECT TOP (@topCount) *
FROM {GetChangeTableReference(captureOptions.CaptureInstance)}
WHERE
    (@hasCheckpoint = 0)
    OR (
        ct.__$start_lsn > @checkpointStartLsn
        OR (ct.__$start_lsn = @checkpointStartLsn AND ct.__$seqval > @checkpointSequenceValue)
        OR (ct.__$start_lsn = @checkpointStartLsn AND ct.__$seqval = @checkpointSequenceValue AND ct.__$operation > @checkpointOperation)
    )
ORDER BY
    ct.__$start_lsn,
    ct.__$seqval,
    ct.__$operation;
""";

        await using var command = new SqlCommand(commandText, connection);
        command.Parameters.Add(new SqlParameter("@topCount", SqlDbType.Int) { Value = topCount });
        command.Parameters.Add(new SqlParameter("@hasCheckpoint", SqlDbType.Bit) { Value = checkpointToken is not null });
        command.Parameters.Add(new SqlParameter("@checkpointStartLsn", SqlDbType.VarBinary, 10)
        {
            Value = checkpointToken?.StartLsn ?? new byte[10]
        });
        command.Parameters.Add(new SqlParameter("@checkpointSequenceValue", SqlDbType.VarBinary, 10)
        {
            Value = checkpointToken?.SequenceValue ?? new byte[10]
        });
        command.Parameters.Add(new SqlParameter("@checkpointOperation", SqlDbType.Int)
        {
            Value = checkpointToken?.Operation ?? 0
        });

        var changes = new List<SqlServerCdcCapturedChange>(topCount);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            changes.Add(CreateCapturedChange(reader, captureOptions, descriptorSourceId));
        }

        return changes;
    }

    private SqlServerCdcCapturedChange CreateCapturedChange(
        SqlDataReader reader,
        SqlServerCdcCaptureOptions captureOptions,
        string? descriptorSourceId)
    {
        var startLsn = reader.GetFieldValue<byte[]>(reader.GetOrdinal("__$start_lsn"));
        var sequenceValue = reader.GetFieldValue<byte[]>(reader.GetOrdinal("__$seqval"));
        var operation = reader.GetInt32(reader.GetOrdinal("__$operation"));
        var updateMaskOrdinal = reader.GetOrdinal("__$update_mask");
        var updateMask = reader.IsDBNull(updateMaskOrdinal)
            ? null
            : reader.GetFieldValue<byte[]>(updateMaskOrdinal);
        var checkpointToken = new SqlServerCdcCheckpointToken(startLsn, sequenceValue, operation);
        var changeId = $"{Convert.ToHexString(startLsn).ToLowerInvariant()}-{Convert.ToHexString(sequenceValue).ToLowerInvariant()}-{operation.ToString(CultureInfo.InvariantCulture)}";
        var operationName = MapOperation(operation);
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < reader.FieldCount; index++)
        {
            var name = reader.GetName(index);
            if (name.StartsWith("__$", StringComparison.Ordinal))
            {
                continue;
            }

            data[name] = MakeJsonFriendly(reader.IsDBNull(index) ? null : reader.GetValue(index));
        }

        var payload = JsonSerializer.Serialize(
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["captureInstance"] = captureOptions.CaptureInstance.Trim(),
                ["databaseName"] = options.DatabaseName.Trim(),
                ["table"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["schema"] = captureOptions.TableSchema.Trim(),
                    ["name"] = captureOptions.TableName.Trim()
                },
                ["operation"] = operationName,
                ["startLsn"] = $"0x{Convert.ToHexString(startLsn)}",
                ["sequenceValue"] = $"0x{Convert.ToHexString(sequenceValue)}",
                ["updateMask"] = updateMask is null ? null : $"0x{Convert.ToHexString(updateMask)}",
                ["data"] = data
            },
            SerializerOptions);

        var sourceId = string.IsNullOrWhiteSpace(descriptorSourceId)
            ? $"{SqlServerDataOptions.ProviderId}:{options.DatabaseName.Trim()}/{captureOptions.TableSchema.Trim()}.{captureOptions.TableName.Trim()}"
            : descriptorSourceId;

        var message = new OutboxMessage(
            id: $"{captureOptions.Id.Trim()}:{changeId}",
            channelId: captureOptions.ChannelId.Trim(),
            messageType: captureOptions.MessageType.Trim(),
            payload: payload,
            occurredAtUtc: DateTimeOffset.UtcNow,
            contentType: ContentType,
            headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["provider"] = SqlServerDataOptions.ProviderId,
                ["cdcCaptureId"] = captureOptions.Id.Trim(),
                ["databaseName"] = options.DatabaseName.Trim(),
                ["schemaName"] = captureOptions.TableSchema.Trim(),
                ["tableName"] = captureOptions.TableName.Trim(),
                ["captureInstance"] = captureOptions.CaptureInstance.Trim(),
                ["operation"] = operationName
            },
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["sourceId"] = sourceId,
                ["eventFormat"] = captureOptions.EventFormat.Trim(),
                ["checkpointToken"] = checkpointToken.Serialize()
            });

        return new SqlServerCdcCapturedChange(changeId, operationName, checkpointToken, message);
    }

    private static object? MakeJsonFriendly(object? value)
    {
        return value switch
        {
            null => null,
            byte[] bytes => $"0x{Convert.ToHexString(bytes)}",
            char[] chars => new string(chars),
            DateTimeOffset timestamp => timestamp,
            DateTime timestamp => DateTime.SpecifyKind(timestamp, DateTimeKind.Utc),
            Guid guid => guid,
            TimeSpan timeSpan => timeSpan.ToString("c", CultureInfo.InvariantCulture),
            _ => value
        };
    }

    private static string MapOperation(int operation)
    {
        return operation switch
        {
            1 => "delete",
            2 => "insert",
            3 => "update-before",
            4 => "update-after",
            _ => $"operation-{operation.ToString(CultureInfo.InvariantCulture)}"
        };
    }

    private string GetCheckpointStoreName()
    {
        return $"{options.CheckpointTableSchema.Trim()}.{options.CheckpointTableName.Trim()}";
    }

    private static string GetChangeTableReference(string captureInstance)
    {
        var normalized = captureInstance.Trim();
        return $"[cdc].[{EscapeIdentifier(normalized)}_CT] AS ct";
    }

    private static string NormalizeInitialPosition(string? initialPosition)
    {
        var normalized = string.IsNullOrWhiteSpace(initialPosition)
            ? "latest-available"
            : initialPosition.Trim().ToLowerInvariant();

        return normalized switch
        {
            "latest-available" => normalized,
            "earliest-available" => normalized,
            _ => throw new InvalidOperationException(
                $"SQL Server CDC initial position '{initialPosition}' is not supported. Use latest-available or earliest-available.")
        };
    }

    private static void ValidateCaptureOptions(SqlServerCdcCaptureOptions captureOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.CaptureInstance);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.TableSchema);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.TableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.OutboxId);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.ChannelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.MessageType);

        if (captureOptions.MaxChangesPerPoll <= 0)
        {
            throw new InvalidOperationException(
                $"SQL Server CDC capture '{captureOptions.Id}' must configure MaxChangesPerPoll greater than zero.");
        }

        if (captureOptions.PollingIntervalSeconds <= 0)
        {
            throw new InvalidOperationException(
                $"SQL Server CDC capture '{captureOptions.Id}' must configure PollingIntervalSeconds greater than zero.");
        }

        _ = NormalizeInitialPosition(captureOptions.InitialPosition);
    }

    private static string EscapeIdentifier(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim().Replace("]", "]]", StringComparison.Ordinal);
    }

    private static string EscapeSqlLiteral(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim().Replace("'", "''", StringComparison.Ordinal);
    }

    private static void LogBootstrappedLatestCheckpoint(ILogger logger, string cdcCaptureId, string checkpointStore) =>
        LogBootstrappedLatestCheckpointMessage(logger, cdcCaptureId, checkpointStore, null);
}
