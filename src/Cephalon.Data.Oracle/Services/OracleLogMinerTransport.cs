using System.Data;
using System.Globalization;
using System.Text.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Data.Oracle.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;

namespace Cephalon.Data.Oracle.Services;

internal sealed class OracleLogMinerTransport(
    string connectionString,
    OracleDataOptions options,
    ILogger<OracleLogMinerTransport> logger) : IOracleLogMinerTransport
{
    private const int OracleObjectAlreadyExists = 955;
    private static readonly Action<ILogger, Exception?> LogIgnoredEndLogMinerFailureMessage =
        LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(7300, nameof(LogIgnoredEndLogMinerFailure)),
            "Ignoring Oracle LogMiner END_LOGMNR failure while cleaning up capture session.");

    public async Task<OracleLogMinerReadBatch> ReadBatchAsync(
        OracleLogMinerCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(captureOptions);
        ArgumentNullException.ThrowIfNull(descriptor);

        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await EnsureCheckpointTableAsync(connection, cancellationToken).ConfigureAwait(false);

        var checkpoint = await ReadCheckpointAsync(connection, descriptor.Id, cancellationToken).ConfigureAwait(false);
        var currentScn = await GetCurrentScnAsync(connection, cancellationToken).ConfigureAwait(false);
        var earliestAvailableScn = await GetEarliestAvailableScnAsync(connection, currentScn, cancellationToken).ConfigureAwait(false);

        var resumeMode = ResolveResumeMode(captureOptions, checkpoint);
        var startScn = checkpoint?.Token.ChangeScn
            ?? (string.Equals(resumeMode, "earliest-available", StringComparison.OrdinalIgnoreCase)
                ? earliestAvailableScn
                : currentScn);

        var metadata = CreateReadMetadata(
            descriptor,
            captureOptions,
            currentScn,
            earliestAvailableScn,
            resumeMode,
            checkpoint);

        if (currentScn <= startScn)
        {
            metadata["startScn"] = startScn.ToString(CultureInfo.InvariantCulture);
            metadata["endScn"] = currentScn.ToString(CultureInfo.InvariantCulture);
            metadata["logFileCount"] = "0";
            return OracleLogMinerReadBatch.Idle(metadata);
        }

        var logFiles = await GetLogFilesForRangeAsync(connection, startScn, currentScn, cancellationToken).ConfigureAwait(false);
        metadata["startScn"] = startScn.ToString(CultureInfo.InvariantCulture);
        metadata["endScn"] = currentScn.ToString(CultureInfo.InvariantCulture);
        metadata["logFileCount"] = logFiles.Count.ToString(CultureInfo.InvariantCulture);

        if (logFiles.Count == 0)
        {
            throw new OracleLogMinerCaptureException(
                $"Oracle LogMiner capture '{descriptor.Id}' could not find redo or archive log files that cover SCN range {startScn.ToString(CultureInfo.InvariantCulture)} to {currentScn.ToString(CultureInfo.InvariantCulture)}.",
                "log-files-unavailable",
                metadata);
        }

        await ConfigureLogMinerFilesAsync(connection, logFiles, cancellationToken).ConfigureAwait(false);

        try
        {
            await StartLogMinerAsync(connection, startScn, currentScn, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            throw new OracleLogMinerCaptureException(
                $"Oracle LogMiner capture '{descriptor.Id}' failed to start LogMiner for SCN range {startScn.ToString(CultureInfo.InvariantCulture)} to {currentScn.ToString(CultureInfo.InvariantCulture)}.",
                "logminer-start",
                metadata,
                exception);
        }

        try
        {
            var changes = await ReadChangesAsync(connection, captureOptions, descriptor, checkpoint, cancellationToken).ConfigureAwait(false);
            if (changes.Count == 0)
            {
                return OracleLogMinerReadBatch.Idle(metadata);
            }

            return new OracleLogMinerReadBatch(
                changes,
                hasMoreChanges: changes.Count >= Math.Max(1, captureOptions.MaxChangesPerRead),
                metadata);
        }
        finally
        {
            await TryEndLogMinerAsync(connection, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task CommitCheckpointAsync(
        OracleLogMinerCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        OracleLogMinerCheckpointToken checkpointToken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(captureOptions);
        ArgumentNullException.ThrowIfNull(descriptor);

        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await EnsureCheckpointTableAsync(connection, cancellationToken).ConfigureAwait(false);

        var tableName = NormalizeCheckpointTableName();
        using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText =
            $"""
            MERGE INTO {tableName} target
            USING (
                SELECT
                    :captureId AS CdcCaptureId,
                    :commitScn AS CommitScn,
                    :changeScn AS ChangeScn,
                    :recordSetId AS RecordSetId,
                    :sqlSequenceNumber AS SqlSequenceNumber,
                    :checkpointToken AS CheckpointToken,
                    :updatedAtUtc AS UpdatedAtUtc
                FROM dual
            ) source
            ON (target.CdcCaptureId = source.CdcCaptureId)
            WHEN MATCHED THEN UPDATE SET
                target.CommitScn = source.CommitScn,
                target.ChangeScn = source.ChangeScn,
                target.RecordSetId = source.RecordSetId,
                target.SqlSequenceNumber = source.SqlSequenceNumber,
                target.CheckpointToken = source.CheckpointToken,
                target.UpdatedAtUtc = source.UpdatedAtUtc
            WHEN NOT MATCHED THEN INSERT (
                CdcCaptureId,
                CommitScn,
                ChangeScn,
                RecordSetId,
                SqlSequenceNumber,
                CheckpointToken,
                UpdatedAtUtc)
            VALUES (
                source.CdcCaptureId,
                source.CommitScn,
                source.ChangeScn,
                source.RecordSetId,
                source.SqlSequenceNumber,
                source.CheckpointToken,
                source.UpdatedAtUtc)
            """;
        command.Parameters.Add("captureId", OracleDbType.Varchar2, descriptor.Id, ParameterDirection.Input);
        command.Parameters.Add("commitScn", OracleDbType.Decimal, checkpointToken.CommitScn, ParameterDirection.Input);
        command.Parameters.Add("changeScn", OracleDbType.Decimal, checkpointToken.ChangeScn, ParameterDirection.Input);
        command.Parameters.Add("recordSetId", OracleDbType.Varchar2, checkpointToken.RecordSetId, ParameterDirection.Input);
        command.Parameters.Add("sqlSequenceNumber", OracleDbType.Int64, checkpointToken.SqlSequenceNumber, ParameterDirection.Input);
        command.Parameters.Add("checkpointToken", OracleDbType.Varchar2, checkpointToken.Serialize(), ParameterDirection.Input);
        command.Parameters.Add("updatedAtUtc", OracleDbType.TimeStampTZ, DateTimeOffset.UtcNow, ParameterDirection.Input);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<OracleLogMinerCapturedChange>> ReadChangesAsync(
        OracleConnection connection,
        OracleLogMinerCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        OracleCheckpointRow? checkpoint,
        CancellationToken cancellationToken)
    {
        var maxChanges = Math.Max(1, captureOptions.MaxChangesPerRead);
        using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText =
            $"""
            SELECT
                SCN,
                COMMIT_SCN,
                RS_ID,
                SSN,
                OPERATION,
                OPERATION_CODE,
                SEG_OWNER,
                TABLE_NAME,
                TIMESTAMP,
                SQL_REDO
            FROM V$LOGMNR_CONTENTS
            WHERE SEG_OWNER = :tableSchema
              AND TABLE_NAME = :tableName
              AND OPERATION_CODE IN (1, 2, 3)
            ORDER BY COMMIT_SCN, SCN, RS_ID, SSN
            FETCH FIRST {maxChanges.ToString(CultureInfo.InvariantCulture)} ROWS ONLY
            """;
        command.Parameters.Add("tableSchema", OracleDbType.Varchar2, captureOptions.TableSchema.Trim().ToUpperInvariant(), ParameterDirection.Input);
        command.Parameters.Add("tableName", OracleDbType.Varchar2, captureOptions.TableName.Trim().ToUpperInvariant(), ParameterDirection.Input);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var changes = new List<OracleLogMinerCapturedChange>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var changeScn = ReadDecimal(reader, 0);
            var commitScn = ReadDecimal(reader, 1);
            var recordSetId = ReadString(reader, 2);
            var sqlSequenceNumber = ReadInt64(reader, 3);
            var operationName = ReadString(reader, 4);
            var operationCode = Convert.ToInt32(reader.GetValue(5), CultureInfo.InvariantCulture);
            var schemaName = ReadString(reader, 6);
            var tableName = ReadString(reader, 7);
            var occurredAtUtc = reader.IsDBNull(8)
                ? DateTimeOffset.UtcNow
                : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(8), DateTimeKind.Utc));
            var sqlRedo = reader.IsDBNull(9)
                ? string.Empty
                : reader.GetString(9);

            var checkpointToken = new OracleLogMinerCheckpointToken(
                commitScn,
                changeScn,
                recordSetId,
                sqlSequenceNumber);

            if (checkpoint is not null && Compare(checkpointToken, checkpoint.Token) <= 0)
            {
                continue;
            }

            var changeId = checkpointToken.Serialize();
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["provider"] = OracleDataOptions.ProviderId,
                ["cdcCaptureId"] = descriptor.Id,
                ["databaseName"] = options.DatabaseName.Trim(),
                ["schemaName"] = schemaName,
                ["tableName"] = tableName,
                ["operation"] = operationName,
                ["operationCode"] = operationCode.ToString(CultureInfo.InvariantCulture),
                ["commitScn"] = commitScn.ToString(CultureInfo.InvariantCulture),
                ["changeScn"] = changeScn.ToString(CultureInfo.InvariantCulture),
                ["recordSetId"] = recordSetId,
                ["sqlSequenceNumber"] = sqlSequenceNumber.ToString(CultureInfo.InvariantCulture)
            };
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["sourceId"] = descriptor.SourceId,
                ["eventFormat"] = descriptor.EventFormat,
                ["checkpointToken"] = checkpointToken.Serialize()
            };

            var payload = JsonSerializer.Serialize(new
            {
                provider = OracleDataOptions.ProviderId,
                databaseName = options.DatabaseName.Trim(),
                schemaName,
                tableName,
                operation = operationName,
                operationCode,
                commitScn = commitScn.ToString(CultureInfo.InvariantCulture),
                changeScn = changeScn.ToString(CultureInfo.InvariantCulture),
                recordSetId,
                sqlSequenceNumber,
                sqlRedo,
                occurredAtUtc
            });

            changes.Add(new OracleLogMinerCapturedChange(
                changeId,
                operationName,
                operationCode,
                checkpointToken,
                new OutboxMessage(
                    id: $"{descriptor.Id}:{changeId}",
                    channelId: captureOptions.ChannelId.Trim(),
                    messageType: captureOptions.MessageType.Trim(),
                    payload: payload,
                    occurredAtUtc: occurredAtUtc,
                    contentType: "application/vnd.cephalon.oracle.logminer+json",
                    headers: headers,
                    metadata: metadata)));
        }

        return changes;
    }

    private static async Task ConfigureLogMinerFilesAsync(
        OracleConnection connection,
        IReadOnlyList<string> logFiles,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < logFiles.Count; index++)
        {
            using var command = connection.CreateCommand();
            command.BindByName = true;
            command.CommandText =
                """
                BEGIN
                    DBMS_LOGMNR.ADD_LOGFILE(
                        LOGFILENAME => :logFileName,
                        OPTIONS => CASE WHEN :isFirst = 1 THEN DBMS_LOGMNR.NEW ELSE DBMS_LOGMNR.ADDFILE END);
                END;
                """;
            command.Parameters.Add("logFileName", OracleDbType.Varchar2, logFiles[index], ParameterDirection.Input);
            command.Parameters.Add("isFirst", OracleDbType.Int32, index == 0 ? 1 : 0, ParameterDirection.Input);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task StartLogMinerAsync(
        OracleConnection connection,
        decimal startScn,
        decimal endScn,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText =
            """
            BEGIN
                DBMS_LOGMNR.START_LOGMNR(
                    STARTSCN => :startScn,
                    ENDSCN => :endScn,
                    OPTIONS => DBMS_LOGMNR.COMMITTED_DATA_ONLY + DBMS_LOGMNR.DICT_FROM_ONLINE_CATALOG);
            END;
            """;
        command.Parameters.Add("startScn", OracleDbType.Decimal, startScn, ParameterDirection.Input);
        command.Parameters.Add("endScn", OracleDbType.Decimal, endScn, ParameterDirection.Input);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task TryEndLogMinerAsync(
        OracleConnection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "BEGIN DBMS_LOGMNR.END_LOGMNR; END;";
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogIgnoredEndLogMinerFailure(logger, exception);
        }
    }

    private async Task EnsureCheckpointTableAsync(
        OracleConnection connection,
        CancellationToken cancellationToken)
    {
        var tableName = NormalizeCheckpointTableName();
        using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            CREATE TABLE {tableName} (
                CdcCaptureId VARCHAR2(128) PRIMARY KEY,
                CommitScn NUMBER(38) NOT NULL,
                ChangeScn NUMBER(38) NOT NULL,
                RecordSetId VARCHAR2(64) NOT NULL,
                SqlSequenceNumber NUMBER(19) NOT NULL,
                CheckpointToken VARCHAR2(512) NOT NULL,
                UpdatedAtUtc TIMESTAMP WITH TIME ZONE NOT NULL
            )
            """;

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OracleException exception) when (exception.Number == OracleObjectAlreadyExists)
        {
        }
    }

    private async Task<OracleCheckpointRow?> ReadCheckpointAsync(
        OracleConnection connection,
        string cdcCaptureId,
        CancellationToken cancellationToken)
    {
        var tableName = NormalizeCheckpointTableName();
        using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText =
            $"""
            SELECT CommitScn, ChangeScn, RecordSetId, SqlSequenceNumber, CheckpointToken, UpdatedAtUtc
            FROM {tableName}
            WHERE CdcCaptureId = :captureId
            """;
        command.Parameters.Add("captureId", OracleDbType.Varchar2, cdcCaptureId, ParameterDirection.Input);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var token = new OracleLogMinerCheckpointToken(
            ReadDecimal(reader, 0),
            ReadDecimal(reader, 1),
            ReadString(reader, 2),
            ReadInt64(reader, 3));
        var serializedToken = ReadString(reader, 4);
        var updatedAtUtc = reader.IsDBNull(5)
            ? DateTimeOffset.MinValue
            : reader.GetFieldType(5) == typeof(DateTimeOffset)
                ? reader.GetFieldValue<DateTimeOffset>(5)
                : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc));

        if (!string.Equals(serializedToken, token.Serialize(), StringComparison.Ordinal))
        {
            token = OracleLogMinerCheckpointToken.Parse(serializedToken);
        }

        return new OracleCheckpointRow(token, serializedToken, updatedAtUtc);
    }

    private static async Task<decimal> GetCurrentScnAsync(
        OracleConnection connection,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT CURRENT_SCN FROM V$DATABASE";
        var scalar = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return scalar is null
            ? 0m
            : Convert.ToDecimal(scalar, CultureInfo.InvariantCulture);
    }

    private static async Task<decimal> GetEarliestAvailableScnAsync(
        OracleConnection connection,
        decimal fallbackScn,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT MIN(first_change) FROM (
                SELECT MIN(FIRST_CHANGE#) AS first_change FROM V$ARCHIVED_LOG WHERE NAME IS NOT NULL
                UNION ALL
                SELECT MIN(FIRST_CHANGE#) AS first_change FROM V$LOG
            )
            """;
        var scalar = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return scalar is null || scalar is DBNull
            ? fallbackScn
            : Convert.ToDecimal(scalar, CultureInfo.InvariantCulture);
    }

    private static async Task<IReadOnlyList<string>> GetLogFilesForRangeAsync(
        OracleConnection connection,
        decimal startScn,
        decimal endScn,
        CancellationToken cancellationToken)
    {
        var candidates = new List<(decimal FirstChange, string Path)>();

        using (var archivedCommand = connection.CreateCommand())
        {
            archivedCommand.BindByName = true;
            archivedCommand.CommandText =
                """
                SELECT FIRST_CHANGE#, NAME
                FROM V$ARCHIVED_LOG
                WHERE NAME IS NOT NULL
                  AND FIRST_CHANGE# <= :endScn
                  AND NVL(NEXT_CHANGE#, FIRST_CHANGE# + 1) > :startScn
                ORDER BY FIRST_CHANGE#
                """;
            archivedCommand.Parameters.Add("endScn", OracleDbType.Decimal, endScn, ParameterDirection.Input);
            archivedCommand.Parameters.Add("startScn", OracleDbType.Decimal, startScn, ParameterDirection.Input);

            using var archivedReader = await archivedCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await archivedReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                candidates.Add((ReadDecimal(archivedReader, 0), ReadString(archivedReader, 1)));
            }
        }

        using (var onlineCommand = connection.CreateCommand())
        {
            onlineCommand.BindByName = true;
            onlineCommand.CommandText =
                """
                SELECT l.FIRST_CHANGE#, lf.MEMBER
                FROM V$LOG l
                JOIN V$LOGFILE lf ON lf.GROUP# = l.GROUP#
                WHERE l.FIRST_CHANGE# <= :endScn
                  AND NVL(l.NEXT_CHANGE#, l.FIRST_CHANGE# + 1) > :startScn
                ORDER BY l.FIRST_CHANGE#
                """;
            onlineCommand.Parameters.Add("endScn", OracleDbType.Decimal, endScn, ParameterDirection.Input);
            onlineCommand.Parameters.Add("startScn", OracleDbType.Decimal, startScn, ParameterDirection.Input);

            using var onlineReader = await onlineCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await onlineReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                candidates.Add((ReadDecimal(onlineReader, 0), ReadString(onlineReader, 1)));
            }
        }

        return candidates
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate.Path))
            .GroupBy(static candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.OrderBy(static item => item.FirstChange).First())
            .OrderBy(static item => item.FirstChange)
            .Select(static item => item.Path)
            .ToArray();
    }

    private Dictionary<string, string> CreateReadMetadata(
        CdcCaptureDescriptor descriptor,
        OracleLogMinerCaptureOptions captureOptions,
        decimal currentScn,
        decimal earliestAvailableScn,
        string resumeMode,
        OracleCheckpointRow? checkpoint)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["provider"] = OracleDataOptions.ProviderId,
            ["databaseName"] = options.DatabaseName.Trim(),
            ["tableSchema"] = captureOptions.TableSchema.Trim(),
            ["tableName"] = captureOptions.TableName.Trim(),
            ["logMinerDictionary"] = "online-catalog",
            ["logMinerMode"] = "committed-only",
            ["initialPosition"] = captureOptions.InitialPosition.Trim(),
            ["currentScn"] = currentScn.ToString(CultureInfo.InvariantCulture),
            ["earliestAvailableScn"] = earliestAvailableScn.ToString(CultureInfo.InvariantCulture),
            ["checkpointStore"] = NormalizeCheckpointTableName(),
            ["checkpointSource"] = "cephalon-checkpoint-table",
            ["redoCursor"] = "commit-scn|change-scn|rs-id|ssn",
            ["sourceId"] = descriptor.SourceId,
            ["resumeMode"] = resumeMode
        };

        if (checkpoint is not null)
        {
            metadata["resumeCheckpoint"] = checkpoint.SerializedToken;
            metadata["checkpointUpdatedAtUtc"] = checkpoint.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture);
        }

        return metadata;
    }

    private static string ResolveResumeMode(
        OracleLogMinerCaptureOptions captureOptions,
        OracleCheckpointRow? checkpoint)
    {
        if (checkpoint is not null)
        {
            return "checkpoint";
        }

        return string.Equals(captureOptions.InitialPosition.Trim(), "earliest-available", StringComparison.OrdinalIgnoreCase)
            ? "earliest-available"
            : "latest-available";
    }

    private string NormalizeCheckpointTableName()
    {
        var normalized = options.CheckpointTableName.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException(
                $"{OracleDataOptions.SectionPath}:CheckpointTableName must not be blank when Oracle LogMiner CDC is enabled.");
        }

        foreach (var character in normalized)
        {
            if (!(char.IsLetterOrDigit(character) || character is '_' or '$' or '#' or '.'))
            {
                throw new InvalidOperationException(
                    $"{OracleDataOptions.SectionPath}:CheckpointTableName '{normalized}' contains unsupported characters for Oracle object naming.");
            }
        }

        return normalized;
    }

    private static int Compare(
        OracleLogMinerCheckpointToken left,
        OracleLogMinerCheckpointToken right)
    {
        var result = left.CommitScn.CompareTo(right.CommitScn);
        if (result != 0)
        {
            return result;
        }

        result = left.ChangeScn.CompareTo(right.ChangeScn);
        if (result != 0)
        {
            return result;
        }

        result = string.Compare(left.RecordSetId, right.RecordSetId, StringComparison.Ordinal);
        if (result != 0)
        {
            return result;
        }

        return left.SqlSequenceNumber.CompareTo(right.SqlSequenceNumber);
    }

    private static decimal ReadDecimal(OracleDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? 0m
            : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    private static long ReadInt64(OracleDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? 0L
            : Convert.ToInt64(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    private static string ReadString(OracleDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? string.Empty
            : reader.GetString(ordinal).Trim();
    }

    private sealed record OracleCheckpointRow(
        OracleLogMinerCheckpointToken Token,
        string SerializedToken,
        DateTimeOffset UpdatedAtUtc);

    private static void LogIgnoredEndLogMinerFailure(ILogger logger, Exception exception) =>
        LogIgnoredEndLogMinerFailureMessage(logger, exception);
}
