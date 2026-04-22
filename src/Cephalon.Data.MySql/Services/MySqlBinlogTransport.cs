using System.Reflection;
using System.Text.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Data.MySql.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using SciSharp.MySQL.Replication;
using SciSharp.MySQL.Replication.Events;
using SuperSocket.Connection;

namespace Cephalon.Data.MySql.Services;

internal sealed class MySqlBinlogTransport(
    string connectionString,
    MySqlDataOptions options,
    ILogger<MySqlBinlogTransport> logger)
    : IMySqlBinlogTransport
{
    private const string ContentType = "application/vnd.cephalon.mysql.binlog+json";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly MethodInfo GetBinlogChecksumMethod = ResolveRequiredMethod("GetBinlogChecksum");
    private static readonly MethodInfo ConfirmChecksumMethod = ResolveRequiredMethod("ConfirmChecksum");
    private static readonly MethodInfo GetStreamFromConnectionMethod = ResolveRequiredMethod("GetStreamFromMySQLConnection");
    private static readonly MethodInfo StartDumpBinlogMethod = ResolveRequiredMethod("StartDumpBinlog");
    private static readonly MethodInfo SetupConnectionMethod = ResolveRequiredHierarchyMethod("SetupConnection");
    private static readonly FieldInfo ConnectionField = ResolveRequiredField("_connection");
    private static readonly FieldInfo ServerIdField = ResolveRequiredField("_serverId");
    private static readonly FieldInfo StreamField = ResolveRequiredField("_stream");
    private static readonly PropertyInfo LogEventChecksumTypeProperty =
        typeof(LogEvent).GetProperty(nameof(LogEvent.ChecksumType), BindingFlags.Public | BindingFlags.Static)
        ?? throw new InvalidOperationException(
            "SciSharp.MySQL.Replication.LogEvent.ChecksumType is required for MySQL provider-native CDC.");
    private readonly Lock checkpointInitializationGate = new();
    private bool checkpointStoreEnsured;

    public async Task<MySqlBinlogReadBatch> ReadBatchAsync(
        MySqlBinlogCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(captureOptions);
        ArgumentNullException.ThrowIfNull(descriptor);

        ValidateCaptureOptions(captureOptions);

        await using var connection = CreateOperationalConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await EnsureCheckpointStoreAsync(connection, cancellationToken).ConfigureAwait(false);

        var normalizedSchema = ResolveTableSchema(captureOptions);
        var checkpoint = await ReadCheckpointAsync(connection, descriptor.Id, cancellationToken).ConfigureAwait(false);
        var startingPosition = checkpoint is not null
            ? new MySqlBinlogStartPosition(checkpoint.Value.BinlogFile, checkpoint.Value.Position)
            : await ResolveInitialPositionAsync(connection, captureOptions, cancellationToken).ConfigureAwait(false);

        var batchMetadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["databaseName"] = options.DatabaseName.Trim(),
            ["tableSchema"] = normalizedSchema,
            ["tableName"] = captureOptions.TableName.Trim(),
            ["serverId"] = captureOptions.ServerId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["initialPosition"] = captureOptions.InitialPosition.Trim(),
            ["checkpointStore"] = $"{options.DatabaseName.Trim()}.{options.CheckpointTableName.Trim()}",
            ["binlogCheckpointSource"] = "cephalon-checkpoint-table",
            ["binlogFile"] = startingPosition.BinlogFile,
            ["binlogPosition"] = startingPosition.Position.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["binlogResumeMode"] = checkpoint is null
                ? NormalizeInitialPosition(captureOptions.InitialPosition)
                : "checkpoint"
        };

        if (checkpoint is not null)
        {
            batchMetadata["resumeCheckpoint"] = checkpoint.Value.Serialize();
        }

        var hasMoreChanges = false;
        var capturedChanges = new List<MySqlBinlogCapturedChange>();
        var tableMap = new Dictionary<long, MySqlBinlogTableAddress>();
        var currentBinlogFile = startingPosition.BinlogFile;
        var replicationClient = await ConnectReplicationClientAsync(
                captureOptions,
                startingPosition,
                batchMetadata,
                cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var deadline = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, captureOptions.MaxAwaitTimeSeconds));
            while (!cancellationToken.IsCancellationRequested)
            {
                var remaining = deadline - DateTimeOffset.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                LogEvent? logEvent;
                try
                {
                    logEvent = await replicationClient.ReceiveAsync()
                        .AsTask()
                        .WaitAsync(remaining, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    break;
                }

                if (logEvent is null)
                {
                    break;
                }

                switch (logEvent)
                {
                    case RotateEvent rotate:
                        currentBinlogFile = rotate.NextBinlogFileName;
                        batchMetadata["binlogFile"] = currentBinlogFile;
                        batchMetadata["binlogPosition"] = rotate.RotatePosition.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        continue;
                    case TableMapEvent map:
                        tableMap[map.TableID] = new MySqlBinlogTableAddress(map.SchemaName, map.TableName);
                        continue;
                    case WriteRowsEvent write:
                        AppendRowChanges(
                            capturedChanges,
                            write.RowSet,
                            "insert",
                            write,
                            descriptor,
                            captureOptions,
                            currentBinlogFile,
                            tableMap,
                            normalizedSchema,
                            batchMetadata,
                            cancellationToken);
                        break;
                    case UpdateRowsEvent update:
                        AppendRowChanges(
                            capturedChanges,
                            update.RowSet,
                            "update",
                            update,
                            descriptor,
                            captureOptions,
                            currentBinlogFile,
                            tableMap,
                            normalizedSchema,
                            batchMetadata,
                            cancellationToken);
                        break;
                    case DeleteRowsEvent delete:
                        AppendRowChanges(
                            capturedChanges,
                            delete.RowSet,
                            "delete",
                            delete,
                            descriptor,
                            captureOptions,
                            currentBinlogFile,
                            tableMap,
                            normalizedSchema,
                            batchMetadata,
                            cancellationToken);
                        break;
                }

                if (capturedChanges.Count >= Math.Max(1, captureOptions.MaxChangesPerRead))
                {
                    hasMoreChanges = true;
                    break;
                }
            }
        }
        finally
        {
            await replicationClient.CloseAsync().ConfigureAwait(false);
        }

        return capturedChanges.Count == 0
            ? MySqlBinlogReadBatch.Idle(batchMetadata)
            : new MySqlBinlogReadBatch(capturedChanges, hasMoreChanges, batchMetadata);
    }

    public async Task CommitCheckpointAsync(
        MySqlBinlogCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        MySqlBinlogCheckpointToken checkpointToken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(captureOptions);
        ArgumentNullException.ThrowIfNull(descriptor);

        await using var connection = CreateOperationalConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await EnsureCheckpointStoreAsync(connection, cancellationToken).ConfigureAwait(false);
        await UpsertCheckpointAsync(connection, descriptor.Id, checkpointToken, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureCheckpointStoreAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);

        lock (checkpointInitializationGate)
        {
            if (checkpointStoreEnsured)
            {
                return;
            }
        }

        var commandText = $"""
CREATE TABLE IF NOT EXISTS `{EscapeIdentifier(options.CheckpointTableName.Trim())}`
(
    `CdcCaptureId` varchar(128) NOT NULL,
    `BinlogFile` varchar(255) NOT NULL,
    `BinlogPosition` bigint NOT NULL,
    `CheckpointToken` varchar(512) NOT NULL,
    `UpdatedAtUtc` datetime(6) NOT NULL,
    PRIMARY KEY (`CdcCaptureId`)
) ENGINE=InnoDB;
""";

        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        lock (checkpointInitializationGate)
        {
            checkpointStoreEnsured = true;
        }
    }

    private async Task<MySqlBinlogStartPosition> ResolveInitialPositionAsync(
        MySqlConnection connection,
        MySqlBinlogCaptureOptions captureOptions,
        CancellationToken cancellationToken)
    {
        var normalizedInitialPosition = NormalizeInitialPosition(captureOptions.InitialPosition);

        await using var command = connection.CreateCommand();
        command.CommandText = normalizedInitialPosition == "earliest-available"
            ? "SHOW BINARY LOGS;"
            : "SHOW MASTER STATUS;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new MySqlBinlogCaptureException(
                $"MySQL binlog capture '{captureOptions.Id}' could not resolve an initial binlog position because the server did not return binary-log metadata.",
                "binlog-unavailable",
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["databaseName"] = options.DatabaseName.Trim(),
                    ["tableSchema"] = ResolveTableSchema(captureOptions),
                    ["tableName"] = captureOptions.TableName.Trim(),
                    ["serverId"] = captureOptions.ServerId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["initialPosition"] = normalizedInitialPosition,
                    ["checkpointStore"] = $"{options.DatabaseName.Trim()}.{options.CheckpointTableName.Trim()}",
                    ["binlogCheckpointSource"] = "cephalon-checkpoint-table"
                });
        }

        if (normalizedInitialPosition == "earliest-available")
        {
            return new MySqlBinlogStartPosition(reader.GetString(0), 4);
        }

        var file = reader.GetString(reader.GetOrdinal("File"));
        var position = reader.GetInt64(reader.GetOrdinal("Position"));
        return new MySqlBinlogStartPosition(file, position);
    }

    private async Task<MySqlBinlogCheckpointToken?> ReadCheckpointAsync(
        MySqlConnection connection,
        string cdcCaptureId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
SELECT `BinlogFile`, `BinlogPosition`
FROM `{EscapeIdentifier(options.CheckpointTableName.Trim())}`
WHERE `CdcCaptureId` = @cdcCaptureId;
""";
        command.Parameters.AddWithValue("@cdcCaptureId", cdcCaptureId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new MySqlBinlogCheckpointToken(
            reader.GetString(0),
            reader.GetInt64(1));
    }

    private async Task UpsertCheckpointAsync(
        MySqlConnection connection,
        string cdcCaptureId,
        MySqlBinlogCheckpointToken checkpointToken,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
INSERT INTO `{EscapeIdentifier(options.CheckpointTableName.Trim())}`
(
    `CdcCaptureId`,
    `BinlogFile`,
    `BinlogPosition`,
    `CheckpointToken`,
    `UpdatedAtUtc`
)
VALUES
(
    @cdcCaptureId,
    @binlogFile,
    @binlogPosition,
    @checkpointToken,
    @updatedAtUtc
)
ON DUPLICATE KEY UPDATE
    `BinlogFile` = VALUES(`BinlogFile`),
    `BinlogPosition` = VALUES(`BinlogPosition`),
    `CheckpointToken` = VALUES(`CheckpointToken`),
    `UpdatedAtUtc` = VALUES(`UpdatedAtUtc`);
""";
        command.Parameters.AddWithValue("@cdcCaptureId", cdcCaptureId);
        command.Parameters.AddWithValue("@binlogFile", checkpointToken.BinlogFile);
        command.Parameters.AddWithValue("@binlogPosition", checkpointToken.Position);
        command.Parameters.AddWithValue("@checkpointToken", checkpointToken.Serialize());
        command.Parameters.AddWithValue("@updatedAtUtc", DateTime.UtcNow);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private MySqlConnection CreateOperationalConnection()
    {
        var builder = new MySqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.Database))
        {
            builder.Database = options.DatabaseName.Trim();
        }

        return new MySqlConnection(builder.ConnectionString);
    }

    private ReplicationConnectionSettings ParseReplicationConnectionSettings()
    {
        var builder = new MySqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.UserID))
        {
            throw new InvalidOperationException("The configured MySQL connection string must include a user id before binlog capture can start.");
        }

        var server = builder.Port > 0 && builder.Port != 3306
            ? $"{builder.Server}:{builder.Port}"
            : builder.Server;

        return new ReplicationConnectionSettings(
            server,
            builder.UserID,
            builder.Password);
    }

    private async Task<ReplicationClient> ConnectReplicationClientAsync(
        MySqlBinlogCaptureOptions captureOptions,
        MySqlBinlogStartPosition startingPosition,
        IReadOnlyDictionary<string, string> batchMetadata,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var replicationClient = new ReplicationClient
        {
            Logger = logger
        };

        var connection = CreateOperationalConnection();
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var checksumType = await InvokeAsync<object>(
                    GetBinlogChecksumMethod,
                    replicationClient,
                    connection)
                .ConfigureAwait(false);
            LogEventChecksumTypeProperty.SetValue(null, checksumType);

            await InvokeAsync(ConfirmChecksumMethod, replicationClient, connection).ConfigureAwait(false);

            var stream = (Stream?)GetStreamFromConnectionMethod.Invoke(replicationClient, [connection]);
            if (stream is null)
            {
                throw new InvalidOperationException("The MySQL replication client did not expose an operational stream.");
            }

            await InvokeAsync(
                    StartDumpBinlogMethod,
                    replicationClient,
                    stream,
                    captureOptions.ServerId,
                    startingPosition.BinlogFile,
                    checked((int)startingPosition.Position))
                .ConfigureAwait(false);

            ConnectionField.SetValue(replicationClient, connection);
            ServerIdField.SetValue(replicationClient, captureOptions.ServerId);
            StreamField.SetValue(replicationClient, stream);

            var transportConnection = new StreamPipeConnection(
                stream: stream,
                remoteEndPoint: null,
                options: new ConnectionOptions
                {
                    Logger = logger
                });
            SetupConnectionMethod.Invoke(replicationClient, [transportConnection]);
            return replicationClient;
        }
        catch (Exception exception) when (exception is not MySqlBinlogCaptureException)
        {
            await connection.DisposeAsync().ConfigureAwait(false);

            throw new MySqlBinlogCaptureException(
                $"MySQL binlog capture '{captureOptions.Id}' could not establish a provider-native replication session: {exception.Message}",
                "replication-connect",
                new Dictionary<string, string>(batchMetadata, StringComparer.OrdinalIgnoreCase));
        }
    }

    private void AppendRowChanges(
        List<MySqlBinlogCapturedChange> capturedChanges,
        RowSet rowSet,
        string operationName,
        RowsEvent rowsEvent,
        CdcCaptureDescriptor descriptor,
        MySqlBinlogCaptureOptions captureOptions,
        string currentBinlogFile,
        IReadOnlyDictionary<long, MySqlBinlogTableAddress> tableMap,
        string normalizedSchema,
        Dictionary<string, string> batchMetadata,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!tableMap.TryGetValue(rowsEvent.TableID, out var tableAddress))
        {
            return;
        }

        if (!string.Equals(tableAddress.SchemaName, normalizedSchema, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(tableAddress.TableName, captureOptions.TableName.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var checkpointPosition = rowsEvent.Position + rowsEvent.EventSize;
        var checkpointToken = new MySqlBinlogCheckpointToken(currentBinlogFile, checkpointPosition);
        batchMetadata["binlogFile"] = currentBinlogFile;
        batchMetadata["binlogPosition"] = checkpointPosition.ToString(System.Globalization.CultureInfo.InvariantCulture);

        for (var rowIndex = 0; rowIndex < rowSet.Rows.Count; rowIndex++)
        {
            var row = rowSet.Rows[rowIndex];
            var changeId = string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"{currentBinlogFile}:{checkpointPosition}:{rowIndex + 1:D4}");
            var payload = JsonSerializer.Serialize(
                CreatePayload(
                    operationName,
                    currentBinlogFile,
                    checkpointPosition,
                    rowsEvent,
                    rowSet.ColumnNames,
                    row),
                SerializerOptions);
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["provider"] = MySqlDataOptions.ProviderId,
                ["cdcCaptureId"] = descriptor.Id,
                ["databaseName"] = options.DatabaseName.Trim(),
                ["schemaName"] = tableAddress.SchemaName,
                ["tableName"] = tableAddress.TableName,
                ["serverId"] = captureOptions.ServerId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["operation"] = operationName,
                ["binlogFile"] = currentBinlogFile
            };
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["sourceId"] = descriptor.SourceId,
                ["eventFormat"] = descriptor.EventFormat,
                ["checkpointToken"] = checkpointToken.Serialize()
            };

            capturedChanges.Add(new MySqlBinlogCapturedChange(
                changeId,
                operationName,
                checkpointToken,
                new OutboxMessage(
                    id: $"{descriptor.Id}:{changeId}",
                    channelId: captureOptions.ChannelId.Trim(),
                    messageType: captureOptions.MessageType.Trim(),
                    payload: payload,
                    occurredAtUtc: NormalizeTimestamp(rowsEvent.Timestamp),
                    contentType: ContentType,
                    headers: headers,
                    metadata: metadata)));
        }
    }

    private static object CreatePayload(
        string operationName,
        string binlogFile,
        long checkpointPosition,
        RowsEvent rowsEvent,
        IReadOnlyList<string>? columnNames,
        object[] row)
    {
        return operationName switch
        {
            "update" => new
            {
                provider = MySqlDataOptions.ProviderId,
                operation = operationName,
                binlogFile,
                binlogPosition = checkpointPosition,
                eventType = rowsEvent.EventType.ToString(),
                eventSize = rowsEvent.EventSize,
                serverId = rowsEvent.ServerID,
                occurredAtUtc = rowsEvent.Timestamp,
                oldRow = ConvertUpdateRow(columnNames, row, oldValues: true),
                newRow = ConvertUpdateRow(columnNames, row, oldValues: false)
            },
            "delete" => new
            {
                provider = MySqlDataOptions.ProviderId,
                operation = operationName,
                binlogFile,
                binlogPosition = checkpointPosition,
                eventType = rowsEvent.EventType.ToString(),
                eventSize = rowsEvent.EventSize,
                serverId = rowsEvent.ServerID,
                occurredAtUtc = rowsEvent.Timestamp,
                row = ConvertPlainRow(columnNames, row)
            },
            _ => new
            {
                provider = MySqlDataOptions.ProviderId,
                operation = operationName,
                binlogFile,
                binlogPosition = checkpointPosition,
                eventType = rowsEvent.EventType.ToString(),
                eventSize = rowsEvent.EventSize,
                serverId = rowsEvent.ServerID,
                occurredAtUtc = rowsEvent.Timestamp,
                row = ConvertPlainRow(columnNames, row)
            }
        };
    }

    private static Dictionary<string, object?> ConvertPlainRow(IReadOnlyList<string>? columnNames, object[] row)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < row.Length; index++)
        {
            var columnName = columnNames is not null && index < columnNames.Count && !string.IsNullOrWhiteSpace(columnNames[index])
                ? columnNames[index]
                : $"column{index + 1}";
            values[columnName] = MakeJsonFriendly(row[index]);
        }

        return values;
    }

    private static Dictionary<string, object?> ConvertUpdateRow(
        IReadOnlyList<string>? columnNames,
        object[] row,
        bool oldValues)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < row.Length; index++)
        {
            var columnName = columnNames is not null && index < columnNames.Count && !string.IsNullOrWhiteSpace(columnNames[index])
                ? columnNames[index]
                : $"column{index + 1}";
            values[columnName] = row[index] is CellValue change
                ? MakeJsonFriendly(oldValues ? change.OldValue : change.NewValue)
                : MakeJsonFriendly(row[index]);
        }

        return values;
    }

    private static object? MakeJsonFriendly(object? value)
    {
        return value switch
        {
            null => null,
            byte[] bytes => Convert.ToHexString(bytes),
            char[] chars => new string(chars),
            DateTimeOffset timestamp => timestamp,
            DateTime timestamp => DateTime.SpecifyKind(timestamp, DateTimeKind.Utc),
            Guid guid => guid,
            TimeSpan timeSpan => timeSpan.ToString("c", System.Globalization.CultureInfo.InvariantCulture),
            decimal number => number,
            _ => value
        };
    }

    private static DateTimeOffset NormalizeTimestamp(DateTime timestamp)
    {
        return timestamp.Kind switch
        {
            DateTimeKind.Utc => new DateTimeOffset(timestamp),
            DateTimeKind.Local => timestamp.ToUniversalTime(),
            _ => new DateTimeOffset(DateTime.SpecifyKind(timestamp, DateTimeKind.Utc))
        };
    }

    private static void ValidateCaptureOptions(MySqlBinlogCaptureOptions captureOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.TableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.OutboxId);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.ChannelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.MessageType);

        if (captureOptions.ServerId <= 0)
        {
            throw new InvalidOperationException(
                $"MySQL binlog capture '{captureOptions.Id}' must configure a positive ServerId.");
        }

        if (captureOptions.MaxChangesPerRead <= 0)
        {
            throw new InvalidOperationException(
                $"MySQL binlog capture '{captureOptions.Id}' must configure MaxChangesPerRead greater than zero.");
        }

        if (captureOptions.MaxAwaitTimeSeconds <= 0)
        {
            throw new InvalidOperationException(
                $"MySQL binlog capture '{captureOptions.Id}' must configure MaxAwaitTimeSeconds greater than zero.");
        }

        if (captureOptions.PollingIntervalSeconds <= 0)
        {
            throw new InvalidOperationException(
                $"MySQL binlog capture '{captureOptions.Id}' must configure PollingIntervalSeconds greater than zero.");
        }

        _ = NormalizeInitialPosition(captureOptions.InitialPosition);
    }

    private string ResolveTableSchema(MySqlBinlogCaptureOptions captureOptions)
    {
        return string.IsNullOrWhiteSpace(captureOptions.TableSchema)
            ? options.DatabaseName.Trim()
            : captureOptions.TableSchema.Trim();
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
                $"MySQL binlog initial position '{initialPosition}' is not supported. Use latest-available or earliest-available.")
        };
    }

    private static string EscapeIdentifier(string identifier)
    {
        return identifier.Replace("`", "``", StringComparison.Ordinal);
    }

    private static MethodInfo ResolveRequiredMethod(string name)
    {
        return typeof(ReplicationClient).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"SciSharp.MySQL.Replication.ReplicationClient.{name} is required for MySQL provider-native CDC.");
    }

    private static MethodInfo ResolveRequiredHierarchyMethod(string name)
    {
        return typeof(ReplicationClient).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
            ?? throw new InvalidOperationException(
                $"SciSharp.MySQL.Replication.ReplicationClient.{name} is required for MySQL provider-native CDC.");
    }

    private static FieldInfo ResolveRequiredField(string name)
    {
        return typeof(ReplicationClient).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"SciSharp.MySQL.Replication.ReplicationClient.{name} is required for MySQL provider-native CDC.");
    }

    private static async Task InvokeAsync(MethodInfo method, object instance, params object?[] arguments)
    {
        switch (method.Invoke(instance, arguments))
        {
            case null:
                return;
            case Task task:
                await task.ConfigureAwait(false);
                return;
            case ValueTask valueTask:
                await valueTask.ConfigureAwait(false);
                return;
            default:
                throw new InvalidOperationException(
                    $"Method '{method.Name}' returned an unsupported asynchronous result type.");
        }
    }

    private static async Task<T> InvokeAsync<T>(MethodInfo method, object instance, params object?[] arguments)
    {
        return method.Invoke(instance, arguments) switch
        {
            T value => value,
            Task<T> task => await task.ConfigureAwait(false),
            ValueTask<T> valueTask => await valueTask.ConfigureAwait(false),
            null => throw new InvalidOperationException($"Method '{method.Name}' returned null."),
            var value => throw new InvalidOperationException(
                $"Method '{method.Name}' returned '{value.GetType().FullName}', which cannot be converted to '{typeof(T).FullName}'.")
        };
    }

    private sealed record ReplicationConnectionSettings(
        string Server,
        string Username,
        string Password);

    private readonly record struct MySqlBinlogStartPosition(
        string BinlogFile,
        long Position);

    private sealed record MySqlBinlogTableAddress(
        string SchemaName,
        string TableName);
}
