using System.Reflection;
using System.Text.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Data.MySql.Configuration;
using Cephalon.Data.MySql.Services;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using SciSharp.MySQL.Replication;
using SciSharp.MySQL.Replication.Events;
using SuperSocket.Connection;

namespace Cephalon.Data.MySql.SciSharpReplication.Services;

internal sealed class SciSharpMySqlBinlogTransport(
    string connectionString,
    MySqlDataOptions options,
    ILogger<SciSharpMySqlBinlogTransport> logger)
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
        var serverProfile = await ReadServerProfileAsync(connection, cancellationToken).ConfigureAwait(false);
        var availableBinlogFiles = await ReadAvailableBinlogFilesAsync(connection, serverProfile, cancellationToken).ConfigureAwait(false);
        var batchMetadata = CreateBatchMetadata(
            captureOptions,
            normalizedSchema,
            checkpoint,
            serverProfile,
            availableBinlogFiles);

        ValidateServerProfile(captureOptions, serverProfile, batchMetadata);
        ValidateSourceServerIdentity(captureOptions, checkpoint, serverProfile, batchMetadata);

        var startingPosition = checkpoint is not null
            ? ResolveCheckpointStartPosition(captureOptions, checkpoint.Value, availableBinlogFiles, batchMetadata)
            : ResolveInitialPosition(captureOptions, serverProfile, availableBinlogFiles, batchMetadata);

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
                            serverProfile,
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
                            serverProfile,
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
                            serverProfile,
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
    `SourceServerUuid` varchar(128) NULL,
    `SourceServerId` bigint NULL,
    `GtidExecutedSet` longtext NULL,
    `BinlogFormat` varchar(32) NULL,
    `BinlogRowImage` varchar(32) NULL,
    `UpdatedAtUtc` datetime(6) NOT NULL,
    PRIMARY KEY (`CdcCaptureId`)
) ENGINE=InnoDB;
""";

        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        await EnsureCheckpointStoreColumnsAsync(connection, cancellationToken).ConfigureAwait(false);

        lock (checkpointInitializationGate)
        {
            checkpointStoreEnsured = true;
        }
    }

    private async Task EnsureCheckpointStoreColumnsAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT `COLUMN_NAME`
FROM `information_schema`.`COLUMNS`
WHERE `TABLE_SCHEMA` = @tableSchema
  AND `TABLE_NAME` = @tableName;
""";
        command.Parameters.AddWithValue("@tableSchema", options.DatabaseName.Trim());
        command.Parameters.AddWithValue("@tableName", options.CheckpointTableName.Trim());

        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                existingColumns.Add(reader.GetString(0));
            }
        }

        var missingColumns = new List<string>();
        if (!existingColumns.Contains("SourceServerUuid"))
        {
            missingColumns.Add("ADD COLUMN `SourceServerUuid` varchar(128) NULL AFTER `CheckpointToken`");
        }

        if (!existingColumns.Contains("SourceServerId"))
        {
            missingColumns.Add("ADD COLUMN `SourceServerId` bigint NULL AFTER `SourceServerUuid`");
        }

        if (!existingColumns.Contains("GtidExecutedSet"))
        {
            missingColumns.Add("ADD COLUMN `GtidExecutedSet` longtext NULL AFTER `SourceServerId`");
        }

        if (!existingColumns.Contains("BinlogFormat"))
        {
            missingColumns.Add("ADD COLUMN `BinlogFormat` varchar(32) NULL AFTER `GtidExecutedSet`");
        }

        if (!existingColumns.Contains("BinlogRowImage"))
        {
            missingColumns.Add("ADD COLUMN `BinlogRowImage` varchar(32) NULL AFTER `BinlogFormat`");
        }

        if (missingColumns.Count == 0)
        {
            return;
        }

        foreach (var alterStatement in missingColumns)
        {
            await using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText =
                $"ALTER TABLE `{EscapeIdentifier(options.CheckpointTableName.Trim())}` {alterStatement};";
            await alterCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static MySqlBinlogStartPosition ResolveInitialPosition(
        MySqlBinlogCaptureOptions captureOptions,
        MySqlBinlogServerProfile serverProfile,
        IReadOnlyList<string> availableBinlogFiles,
        Dictionary<string, string> batchMetadata)
    {
        var normalizedInitialPosition = NormalizeInitialPosition(captureOptions.InitialPosition);

        if (normalizedInitialPosition == "earliest-available")
        {
            if (availableBinlogFiles.Count == 0)
            {
                batchMetadata["binlogLifecycleState"] = "unavailable";
                batchMetadata["binlogLifecycleAction"] = "fail";

                throw new MySqlBinlogCaptureException(
                    $"MySQL binlog capture '{captureOptions.Id}' could not resolve the earliest binlog file because the server did not return any retained binary logs.",
                    "binlog-unavailable",
                    new Dictionary<string, string>(batchMetadata, StringComparer.OrdinalIgnoreCase));
            }

            batchMetadata["binlogResumeMode"] = normalizedInitialPosition;
            batchMetadata["binlogLifecycleState"] = "available";
            batchMetadata["binlogLifecycleAction"] = "start";
            batchMetadata["binlogFile"] = availableBinlogFiles[0];
            batchMetadata["binlogPosition"] = "4";
            return new MySqlBinlogStartPosition(availableBinlogFiles[0], 4);
        }

        if (string.IsNullOrWhiteSpace(serverProfile.CurrentBinlogFile) || serverProfile.CurrentBinlogPosition is null)
        {
            batchMetadata["binlogLifecycleState"] = "unavailable";
            batchMetadata["binlogLifecycleAction"] = "fail";

            throw new MySqlBinlogCaptureException(
                $"MySQL binlog capture '{captureOptions.Id}' could not resolve the latest binlog position because the server did not return current binary-log metadata.",
                "binlog-unavailable",
                new Dictionary<string, string>(batchMetadata, StringComparer.OrdinalIgnoreCase));
        }

        batchMetadata["binlogResumeMode"] = normalizedInitialPosition;
        batchMetadata["binlogLifecycleState"] = "available";
        batchMetadata["binlogLifecycleAction"] = "start";
        batchMetadata["binlogFile"] = serverProfile.CurrentBinlogFile;
        batchMetadata["binlogPosition"] = serverProfile.CurrentBinlogPosition.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return new MySqlBinlogStartPosition(serverProfile.CurrentBinlogFile, serverProfile.CurrentBinlogPosition.Value);
    }

    private async Task<MySqlBinlogStoredCheckpoint?> ReadCheckpointAsync(
        MySqlConnection connection,
        string cdcCaptureId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
SELECT `BinlogFile`,
       `BinlogPosition`,
       `SourceServerUuid`,
       `SourceServerId`,
       `GtidExecutedSet`,
       `BinlogFormat`,
       `BinlogRowImage`,
       `UpdatedAtUtc`
FROM `{EscapeIdentifier(options.CheckpointTableName.Trim())}`
WHERE `CdcCaptureId` = @cdcCaptureId;
""";
        command.Parameters.AddWithValue("@cdcCaptureId", cdcCaptureId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var sourceServerIdOrdinal = reader.GetOrdinal("SourceServerId");
        var updatedAtUtcOrdinal = reader.GetOrdinal("UpdatedAtUtc");

        return new MySqlBinlogStoredCheckpoint(
            reader.GetString(reader.GetOrdinal("BinlogFile")),
            reader.GetInt64(reader.GetOrdinal("BinlogPosition")),
            ReadNullableString(reader, reader.GetOrdinal("SourceServerUuid")),
            reader.IsDBNull(sourceServerIdOrdinal) ? null : reader.GetInt64(sourceServerIdOrdinal),
            ReadNullableString(reader, reader.GetOrdinal("GtidExecutedSet")),
            ReadNullableString(reader, reader.GetOrdinal("BinlogFormat")),
            ReadNullableString(reader, reader.GetOrdinal("BinlogRowImage")),
            reader.IsDBNull(updatedAtUtcOrdinal)
                ? null
                : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(updatedAtUtcOrdinal), DateTimeKind.Utc)));
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
    `SourceServerUuid`,
    `SourceServerId`,
    `GtidExecutedSet`,
    `BinlogFormat`,
    `BinlogRowImage`,
    `UpdatedAtUtc`
)
VALUES
(
    @cdcCaptureId,
    @binlogFile,
    @binlogPosition,
    @checkpointToken,
    @sourceServerUuid,
    @sourceServerId,
    @gtidExecutedSet,
    @binlogFormat,
    @binlogRowImage,
    @updatedAtUtc
)
ON DUPLICATE KEY UPDATE
    `BinlogFile` = VALUES(`BinlogFile`),
    `BinlogPosition` = VALUES(`BinlogPosition`),
    `CheckpointToken` = VALUES(`CheckpointToken`),
    `SourceServerUuid` = VALUES(`SourceServerUuid`),
    `SourceServerId` = VALUES(`SourceServerId`),
    `GtidExecutedSet` = VALUES(`GtidExecutedSet`),
    `BinlogFormat` = VALUES(`BinlogFormat`),
    `BinlogRowImage` = VALUES(`BinlogRowImage`),
    `UpdatedAtUtc` = VALUES(`UpdatedAtUtc`);
""";
        command.Parameters.AddWithValue("@cdcCaptureId", cdcCaptureId);
        command.Parameters.AddWithValue("@binlogFile", checkpointToken.BinlogFile);
        command.Parameters.AddWithValue("@binlogPosition", checkpointToken.Position);
        command.Parameters.AddWithValue("@checkpointToken", checkpointToken.Serialize());
        command.Parameters.AddWithValue("@sourceServerUuid", (object?)checkpointToken.SourceServerUuid ?? DBNull.Value);
        command.Parameters.AddWithValue("@sourceServerId", checkpointToken.SourceServerId is null ? DBNull.Value : checkpointToken.SourceServerId.Value);
        command.Parameters.AddWithValue("@gtidExecutedSet", (object?)checkpointToken.GtidExecutedSet ?? DBNull.Value);
        command.Parameters.AddWithValue("@binlogFormat", (object?)checkpointToken.BinlogFormat ?? DBNull.Value);
        command.Parameters.AddWithValue("@binlogRowImage", (object?)checkpointToken.BinlogRowImage ?? DBNull.Value);
        command.Parameters.AddWithValue("@updatedAtUtc", DateTime.UtcNow);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<MySqlBinlogServerProfile> ReadServerProfileAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await using (var variablesCommand = connection.CreateCommand())
        {
            variablesCommand.CommandText = """
SHOW VARIABLES
WHERE `Variable_name` IN ('server_uuid', 'server_id', 'gtid_mode', 'gtid_executed', 'log_bin', 'binlog_format', 'binlog_row_image');
""";

            await using var variablesReader = await variablesCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await variablesReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                variables[variablesReader.GetString(0)] = variablesReader.GetString(1);
            }
        }

        string? currentBinlogFile = null;
        long? currentBinlogPosition = null;
        string? executedGtidSetFromStatus = null;
        await using (var masterStatusCommand = connection.CreateCommand())
        {
            masterStatusCommand.CommandText = "SHOW MASTER STATUS;";
            await using var masterStatusReader = await masterStatusCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await masterStatusReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var ordinals = Enumerable.Range(0, masterStatusReader.FieldCount)
                    .ToDictionary(masterStatusReader.GetName, static index => index, StringComparer.OrdinalIgnoreCase);

                if (ordinals.TryGetValue("File", out var fileOrdinal) && !masterStatusReader.IsDBNull(fileOrdinal))
                {
                    currentBinlogFile = masterStatusReader.GetString(fileOrdinal);
                }

                if (ordinals.TryGetValue("Position", out var positionOrdinal) && !masterStatusReader.IsDBNull(positionOrdinal))
                {
                    currentBinlogPosition = masterStatusReader.GetInt64(positionOrdinal);
                }

                if (ordinals.TryGetValue("Executed_Gtid_Set", out var gtidOrdinal) && !masterStatusReader.IsDBNull(gtidOrdinal))
                {
                    executedGtidSetFromStatus = masterStatusReader.GetString(gtidOrdinal);
                }
            }
        }

        var binaryLoggingEnabled = ParseBoolean(variables.TryGetValue("log_bin", out var logBinValue) ? logBinValue : null)
            ?? !string.IsNullOrWhiteSpace(currentBinlogFile);

        return new MySqlBinlogServerProfile(
            ReadOptionalValue(variables, "server_uuid"),
            ParseInt64(ReadOptionalValue(variables, "server_id")),
            ReadOptionalValue(variables, "gtid_mode"),
            string.IsNullOrWhiteSpace(executedGtidSetFromStatus)
                ? ReadOptionalValue(variables, "gtid_executed")
                : executedGtidSetFromStatus,
            binaryLoggingEnabled,
            ReadOptionalValue(variables, "binlog_format"),
            ReadOptionalValue(variables, "binlog_row_image"),
            currentBinlogFile,
            currentBinlogPosition);
    }

    private static async Task<IReadOnlyList<string>> ReadAvailableBinlogFilesAsync(
        MySqlConnection connection,
        MySqlBinlogServerProfile serverProfile,
        CancellationToken cancellationToken)
    {
        if (!serverProfile.BinaryLoggingEnabled)
        {
            return Array.Empty<string>();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SHOW BINARY LOGS;";

        var files = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            files.Add(reader.GetString(0));
        }

        return files;
    }

    private Dictionary<string, string> CreateBatchMetadata(
        MySqlBinlogCaptureOptions captureOptions,
        string normalizedSchema,
        MySqlBinlogStoredCheckpoint? checkpoint,
        MySqlBinlogServerProfile serverProfile,
        IReadOnlyList<string> availableBinlogFiles)
    {
        var normalizedInitialPosition = NormalizeInitialPosition(captureOptions.InitialPosition);
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["databaseName"] = options.DatabaseName.Trim(),
            ["tableSchema"] = normalizedSchema,
            ["tableName"] = captureOptions.TableName.Trim(),
            ["serverId"] = captureOptions.ServerId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["initialPosition"] = normalizedInitialPosition,
            ["checkpointStore"] = $"{options.DatabaseName.Trim()}.{options.CheckpointTableName.Trim()}",
            ["binlogCheckpointSource"] = "cephalon-checkpoint-table",
            ["binlogResumeMode"] = checkpoint is null ? normalizedInitialPosition : "checkpoint",
            ["sourceServerIdentityState"] = "observed",
            ["sourceServerIdentityAction"] = "accept",
            ["gtidMetadataMode"] = "observe-only"
        };

        AddOptionalMetadata(metadata, "expectedSourceServerUuid", captureOptions.ExpectedSourceServerUuid);
        AddOptionalMetadata(metadata, "sourceServerUuid", serverProfile.ServerUuid);
        AddOptionalMetadata(metadata, "sourceServerId", serverProfile.SourceServerId);
        AddOptionalMetadata(metadata, "gtidMode", serverProfile.GtidMode);
        AddOptionalMetadata(metadata, "gtidExecutedSet", serverProfile.GtidExecutedSet);
        AddOptionalMetadata(metadata, "binaryLoggingEnabled", serverProfile.BinaryLoggingEnabled);
        AddOptionalMetadata(metadata, "binlogFormat", serverProfile.BinlogFormat);
        AddOptionalMetadata(metadata, "binlogRowImage", serverProfile.BinlogRowImage);
        AddOptionalMetadata(metadata, "currentBinlogFile", serverProfile.CurrentBinlogFile);
        AddOptionalMetadata(metadata, "currentBinlogPosition", serverProfile.CurrentBinlogPosition);

        if (availableBinlogFiles.Count > 0)
        {
            metadata["availableBinlogFileCount"] = availableBinlogFiles.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            metadata["earliestAvailableBinlogFile"] = availableBinlogFiles[0];
            metadata["latestAvailableBinlogFile"] = availableBinlogFiles[^1];
        }

        if (checkpoint is not null)
        {
            metadata["resumeCheckpoint"] = checkpoint.Value.Serialize();
            metadata["binlogFile"] = checkpoint.Value.BinlogFile;
            metadata["binlogPosition"] = checkpoint.Value.Position.ToString(System.Globalization.CultureInfo.InvariantCulture);
            AddOptionalMetadata(metadata, "checkpointSourceServerUuid", checkpoint.Value.SourceServerUuid);
            AddOptionalMetadata(metadata, "checkpointSourceServerId", checkpoint.Value.SourceServerId);
            AddOptionalMetadata(metadata, "checkpointGtidExecutedSet", checkpoint.Value.GtidExecutedSet);
            AddOptionalMetadata(metadata, "checkpointBinlogFormat", checkpoint.Value.BinlogFormat);
            AddOptionalMetadata(metadata, "checkpointBinlogRowImage", checkpoint.Value.BinlogRowImage);
            AddOptionalMetadata(metadata, "checkpointUpdatedAtUtc", checkpoint.Value.UpdatedAtUtc);
        }

        return metadata;
    }

    private static void ValidateServerProfile(
        MySqlBinlogCaptureOptions captureOptions,
        MySqlBinlogServerProfile serverProfile,
        Dictionary<string, string> batchMetadata)
    {
        if (!serverProfile.BinaryLoggingEnabled)
        {
            batchMetadata["binlogLifecycleState"] = "unavailable";
            batchMetadata["binlogLifecycleAction"] = "fail";

            throw new MySqlBinlogCaptureException(
                $"MySQL binlog capture '{captureOptions.Id}' requires binary logging to be enabled on the source server before the Cephalon MySQL CDC runner can start.",
                "binary-logging-disabled",
                new Dictionary<string, string>(batchMetadata, StringComparer.OrdinalIgnoreCase));
        }

        if (!string.Equals(serverProfile.BinlogFormat, "ROW", StringComparison.OrdinalIgnoreCase))
        {
            batchMetadata["binlogLifecycleState"] = "unsupported-format";
            batchMetadata["binlogLifecycleAction"] = "fail";

            throw new MySqlBinlogCaptureException(
                $"MySQL binlog capture '{captureOptions.Id}' requires ROW binlog_format, but the source server reported '{serverProfile.BinlogFormat ?? "unknown"}'. Configure row-based binary logging before starting the Cephalon MySQL CDC runner.",
                "binlog-format-unsupported",
                new Dictionary<string, string>(batchMetadata, StringComparer.OrdinalIgnoreCase));
        }
    }

    private static void ValidateSourceServerIdentity(
        MySqlBinlogCaptureOptions captureOptions,
        MySqlBinlogStoredCheckpoint? checkpoint,
        MySqlBinlogServerProfile serverProfile,
        Dictionary<string, string> batchMetadata)
    {
        var expectedSourceServerUuid = captureOptions.ExpectedSourceServerUuid.Trim();
        if (!string.IsNullOrWhiteSpace(expectedSourceServerUuid))
        {
            if (string.IsNullOrWhiteSpace(serverProfile.ServerUuid))
            {
                batchMetadata["sourceServerIdentityState"] = "unknown";
                batchMetadata["sourceServerIdentityAction"] = "fail";

                throw new MySqlBinlogCaptureException(
                    $"MySQL binlog capture '{captureOptions.Id}' expected source server UUID '{expectedSourceServerUuid}', but the source server did not report server_uuid.",
                    "source-server-identity-unavailable",
                    new Dictionary<string, string>(batchMetadata, StringComparer.OrdinalIgnoreCase));
            }

            if (!string.Equals(serverProfile.ServerUuid, expectedSourceServerUuid, StringComparison.OrdinalIgnoreCase))
            {
                batchMetadata["sourceServerIdentityState"] = "mismatch";
                batchMetadata["sourceServerIdentityAction"] = "fail";

                throw new MySqlBinlogCaptureException(
                    $"MySQL binlog capture '{captureOptions.Id}' expected source server UUID '{expectedSourceServerUuid}', but the live source server reported '{serverProfile.ServerUuid}'.",
                    "source-server-mismatch",
                    new Dictionary<string, string>(batchMetadata, StringComparer.OrdinalIgnoreCase));
            }

            batchMetadata["sourceServerIdentityState"] = "expected-match";
            batchMetadata["sourceServerIdentityAction"] = "accept";
            return;
        }

        if (checkpoint is null || string.IsNullOrWhiteSpace(checkpoint.Value.SourceServerUuid))
        {
            if (string.IsNullOrWhiteSpace(serverProfile.ServerUuid))
            {
                batchMetadata["sourceServerIdentityState"] = "unknown";
                batchMetadata["sourceServerIdentityAction"] = "observe";
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(serverProfile.ServerUuid))
        {
            batchMetadata["sourceServerIdentityState"] = "unknown";
            batchMetadata["sourceServerIdentityAction"] = "fail";

            throw new MySqlBinlogCaptureException(
                $"MySQL binlog capture '{captureOptions.Id}' needs live source-server identity to validate checkpoint '{checkpoint.Value.Serialize()}', but the source server did not report server_uuid.",
                "source-server-identity-unavailable",
                new Dictionary<string, string>(batchMetadata, StringComparer.OrdinalIgnoreCase));
        }

        if (!string.Equals(serverProfile.ServerUuid, checkpoint.Value.SourceServerUuid, StringComparison.OrdinalIgnoreCase))
        {
            batchMetadata["sourceServerIdentityState"] = "checkpoint-mismatch";
            batchMetadata["sourceServerIdentityAction"] = "fail";

            throw new MySqlBinlogCaptureException(
                $"MySQL binlog capture '{captureOptions.Id}' cannot resume checkpoint '{checkpoint.Value.Serialize()}' because it was recorded against source server UUID '{checkpoint.Value.SourceServerUuid}', but the live source server reported '{serverProfile.ServerUuid}'.",
                "checkpoint-source-server-mismatch",
                new Dictionary<string, string>(batchMetadata, StringComparer.OrdinalIgnoreCase));
        }

        batchMetadata["sourceServerIdentityState"] = "checkpoint-match";
        batchMetadata["sourceServerIdentityAction"] = "resume";
    }

    private static MySqlBinlogStartPosition ResolveCheckpointStartPosition(
        MySqlBinlogCaptureOptions captureOptions,
        MySqlBinlogStoredCheckpoint checkpoint,
        IReadOnlyList<string> availableBinlogFiles,
        Dictionary<string, string> batchMetadata)
    {
        batchMetadata["binlogResumeMode"] = "checkpoint";

        if (!availableBinlogFiles.Contains(checkpoint.BinlogFile, StringComparer.OrdinalIgnoreCase))
        {
            batchMetadata["binlogLifecycleState"] = "purged";
            batchMetadata["binlogLifecycleAction"] = "fail";

            throw new MySqlBinlogCaptureException(
                $"MySQL binlog capture '{captureOptions.Id}' cannot resume checkpoint '{checkpoint.Serialize()}' because binlog file '{checkpoint.BinlogFile}' is no longer retained on the source server. Reset the checkpoint or restore the missing binlog file before starting the Cephalon MySQL CDC runner.",
                "checkpoint-binlog-unavailable",
                new Dictionary<string, string>(batchMetadata, StringComparer.OrdinalIgnoreCase));
        }

        batchMetadata["binlogLifecycleState"] = "checkpoint-available";
        batchMetadata["binlogLifecycleAction"] = "resume";
        batchMetadata["binlogFile"] = checkpoint.BinlogFile;
        batchMetadata["binlogPosition"] = checkpoint.Position.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return checkpoint.ToStartPosition();
    }

    private static string? ReadOptionalValue(
        Dictionary<string, string> values,
        string key)
    {
        return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;
    }

    private static void AddOptionalMetadata(
        Dictionary<string, string> metadata,
        string key,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            metadata[key] = value.Trim();
        }
    }

    private static void AddOptionalMetadata(
        Dictionary<string, string> metadata,
        string key,
        long? value)
    {
        if (value is not null)
        {
            metadata[key] = value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    private static void AddOptionalMetadata(
        Dictionary<string, string> metadata,
        string key,
        bool value)
    {
        metadata[key] = value ? "true" : "false";
    }

    private static void AddOptionalMetadata(
        Dictionary<string, string> metadata,
        string key,
        DateTimeOffset? value)
    {
        if (value is not null)
        {
            metadata[key] = value.Value.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    private static string? ReadNullableString(System.Data.Common.DbDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetString(ordinal);
    }

    private static bool? ParseBoolean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToUpperInvariant() switch
        {
            "1" => true,
            "ON" => true,
            "TRUE" => true,
            "YES" => true,
            "0" => false,
            "OFF" => false,
            "FALSE" => false,
            "NO" => false,
            _ => null
        };
    }

    private static long? ParseInt64(string? value)
    {
        return long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
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
        MySqlBinlogServerProfile serverProfile,
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
        var checkpointToken = new MySqlBinlogCheckpointToken(
            currentBinlogFile,
            checkpointPosition,
            serverProfile.ServerUuid,
            serverProfile.SourceServerId,
            serverProfile.GtidExecutedSet,
            serverProfile.BinlogFormat,
            serverProfile.BinlogRowImage);
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
            if (!string.IsNullOrWhiteSpace(serverProfile.ServerUuid))
            {
                headers["sourceServerUuid"] = serverProfile.ServerUuid;
            }

            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["sourceId"] = descriptor.SourceId,
                ["eventFormat"] = descriptor.EventFormat,
                ["checkpointToken"] = checkpointToken.Serialize()
            };
            if (!string.IsNullOrWhiteSpace(serverProfile.GtidMode))
            {
                metadata["gtidMode"] = serverProfile.GtidMode;
            }

            if (!string.IsNullOrWhiteSpace(serverProfile.GtidExecutedSet))
            {
                metadata["gtidExecutedSet"] = serverProfile.GtidExecutedSet;
            }

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

    private readonly record struct MySqlBinlogStoredCheckpoint(
        string BinlogFile,
        long Position,
        string? SourceServerUuid,
        long? SourceServerId,
        string? GtidExecutedSet,
        string? BinlogFormat,
        string? BinlogRowImage,
        DateTimeOffset? UpdatedAtUtc)
    {
        public string Serialize()
        {
            return new MySqlBinlogCheckpointToken(BinlogFile, Position).Serialize();
        }

        public MySqlBinlogStartPosition ToStartPosition()
        {
            return new MySqlBinlogStartPosition(BinlogFile, Position);
        }
    }

    private readonly record struct MySqlBinlogServerProfile(
        string? ServerUuid,
        long? SourceServerId,
        string? GtidMode,
        string? GtidExecutedSet,
        bool BinaryLoggingEnabled,
        string? BinlogFormat,
        string? BinlogRowImage,
        string? CurrentBinlogFile,
        long? CurrentBinlogPosition);

    private readonly record struct MySqlBinlogStartPosition(
        string BinlogFile,
        long Position);

    private sealed record MySqlBinlogTableAddress(
        string SchemaName,
        string TableName);
}
