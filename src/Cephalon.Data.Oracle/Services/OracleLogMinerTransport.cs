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
    private const int OracleColumnAlreadyExists = 1430;
    private readonly Lock checkpointInitializationGate = new();
    private bool checkpointTableEnsured;
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

        ValidateCaptureOptions(captureOptions);

        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await EnsureCheckpointTableAsync(connection, cancellationToken).ConfigureAwait(false);

        var checkpoint = await ReadCheckpointAsync(connection, descriptor.Id, cancellationToken).ConfigureAwait(false);
        var databaseProfile = await ReadDatabaseProfileAsync(connection, cancellationToken).ConfigureAwait(false);
        var earliestAvailableScn = await GetEarliestAvailableScnAsync(
                connection,
                databaseProfile.CurrentScn,
                cancellationToken)
            .ConfigureAwait(false);

        var resumeMode = ResolveResumeMode(captureOptions, checkpoint);
        var metadata = CreateReadMetadata(
            descriptor,
            captureOptions,
            databaseProfile,
            earliestAvailableScn,
            resumeMode,
            checkpoint);

        ValidateArchiveLogMode(captureOptions, databaseProfile, metadata);
        ValidateDatabaseIdentity(captureOptions, checkpoint, databaseProfile, metadata);

        var startScn = ResolveStartScn(
            captureOptions,
            checkpoint,
            databaseProfile.CurrentScn,
            earliestAvailableScn,
            metadata);

        metadata["startScn"] = startScn.ToString(CultureInfo.InvariantCulture);
        metadata["endScn"] = databaseProfile.CurrentScn.ToString(CultureInfo.InvariantCulture);

        if (databaseProfile.CurrentScn <= startScn)
        {
            metadata["logFileCount"] = "0";
            return OracleLogMinerReadBatch.Idle(metadata);
        }

        var logFiles = await GetLogFilesForRangeAsync(
                connection,
                startScn,
                databaseProfile.CurrentScn,
                cancellationToken)
            .ConfigureAwait(false);
        metadata["logFileCount"] = logFiles.Count.ToString(CultureInfo.InvariantCulture);

        if (logFiles.Count == 0)
        {
            metadata["archiveLogLifecycleState"] = "window-unavailable";
            metadata["archiveLogLifecycleAction"] = "fail";
            throw new OracleLogMinerCaptureException(
                $"Oracle LogMiner capture '{descriptor.Id}' could not find redo or archive log files that cover SCN range {startScn.ToString(CultureInfo.InvariantCulture)} to {databaseProfile.CurrentScn.ToString(CultureInfo.InvariantCulture)}.",
                "log-files-unavailable",
                metadata);
        }

        await ConfigureLogMinerFilesAsync(connection, logFiles, cancellationToken).ConfigureAwait(false);

        try
        {
            await StartLogMinerAsync(connection, startScn, databaseProfile.CurrentScn, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            throw new OracleLogMinerCaptureException(
                $"Oracle LogMiner capture '{descriptor.Id}' failed to start LogMiner for SCN range {startScn.ToString(CultureInfo.InvariantCulture)} to {databaseProfile.CurrentScn.ToString(CultureInfo.InvariantCulture)}.",
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

        ValidateCaptureOptions(captureOptions);

        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await EnsureCheckpointTableAsync(connection, cancellationToken).ConfigureAwait(false);

        var databaseProfile = await ReadDatabaseProfileAsync(connection, cancellationToken).ConfigureAwait(false);
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
                    :databaseId AS DatabaseId,
                    :databaseUniqueName AS DatabaseUniqueName,
                    :resetLogsChangeNumber AS ResetLogsChangeNumber,
                    :archiveLogMode AS ArchiveLogMode,
                    :supplementalLogDataMin AS SupplementalLogDataMin,
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
                target.DatabaseId = source.DatabaseId,
                target.DatabaseUniqueName = source.DatabaseUniqueName,
                target.ResetLogsChangeNumber = source.ResetLogsChangeNumber,
                target.ArchiveLogMode = source.ArchiveLogMode,
                target.SupplementalLogDataMin = source.SupplementalLogDataMin,
                target.UpdatedAtUtc = source.UpdatedAtUtc
            WHEN NOT MATCHED THEN INSERT (
                CdcCaptureId,
                CommitScn,
                ChangeScn,
                RecordSetId,
                SqlSequenceNumber,
                CheckpointToken,
                DatabaseId,
                DatabaseUniqueName,
                ResetLogsChangeNumber,
                ArchiveLogMode,
                SupplementalLogDataMin,
                UpdatedAtUtc)
            VALUES (
                source.CdcCaptureId,
                source.CommitScn,
                source.ChangeScn,
                source.RecordSetId,
                source.SqlSequenceNumber,
                source.CheckpointToken,
                source.DatabaseId,
                source.DatabaseUniqueName,
                source.ResetLogsChangeNumber,
                source.ArchiveLogMode,
                source.SupplementalLogDataMin,
                source.UpdatedAtUtc)
            """;
        command.Parameters.Add("captureId", OracleDbType.Varchar2, descriptor.Id, ParameterDirection.Input);
        command.Parameters.Add("commitScn", OracleDbType.Decimal, checkpointToken.CommitScn, ParameterDirection.Input);
        command.Parameters.Add("changeScn", OracleDbType.Decimal, checkpointToken.ChangeScn, ParameterDirection.Input);
        command.Parameters.Add("recordSetId", OracleDbType.Varchar2, checkpointToken.RecordSetId, ParameterDirection.Input);
        command.Parameters.Add("sqlSequenceNumber", OracleDbType.Int64, checkpointToken.SqlSequenceNumber, ParameterDirection.Input);
        command.Parameters.Add("checkpointToken", OracleDbType.Varchar2, checkpointToken.Serialize(), ParameterDirection.Input);
        command.Parameters.Add(
            "databaseId",
            OracleDbType.Decimal,
            databaseProfile.DatabaseId,
            ParameterDirection.Input);
        command.Parameters.Add(
            "databaseUniqueName",
            OracleDbType.Varchar2,
            (object?)databaseProfile.DatabaseUniqueName ?? DBNull.Value,
            ParameterDirection.Input);
        command.Parameters.Add(
            "resetLogsChangeNumber",
            OracleDbType.Decimal,
            databaseProfile.ResetLogsChangeNumber.HasValue ? databaseProfile.ResetLogsChangeNumber.Value : DBNull.Value,
            ParameterDirection.Input);
        command.Parameters.Add(
            "archiveLogMode",
            OracleDbType.Varchar2,
            (object?)databaseProfile.ArchiveLogMode ?? DBNull.Value,
            ParameterDirection.Input);
        command.Parameters.Add(
            "supplementalLogDataMin",
            OracleDbType.Varchar2,
            (object?)databaseProfile.SupplementalLogDataMin ?? DBNull.Value,
            ParameterDirection.Input);
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
        lock (checkpointInitializationGate)
        {
            if (checkpointTableEnsured)
            {
                return;
            }
        }

        var tableName = NormalizeCheckpointTableName();
        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                $"""
                CREATE TABLE {tableName} (
                    CdcCaptureId VARCHAR2(128) PRIMARY KEY,
                    CommitScn NUMBER(38) NOT NULL,
                    ChangeScn NUMBER(38) NOT NULL,
                    RecordSetId VARCHAR2(64) NOT NULL,
                    SqlSequenceNumber NUMBER(19) NOT NULL,
                    CheckpointToken VARCHAR2(512) NOT NULL,
                    DatabaseId NUMBER(38) NULL,
                    DatabaseUniqueName VARCHAR2(128) NULL,
                    ResetLogsChangeNumber NUMBER(38) NULL,
                    ArchiveLogMode VARCHAR2(32) NULL,
                    SupplementalLogDataMin VARCHAR2(32) NULL,
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

        await EnsureCheckpointTableColumnsAsync(connection, cancellationToken).ConfigureAwait(false);

        lock (checkpointInitializationGate)
        {
            checkpointTableEnsured = true;
        }
    }

    private async Task EnsureCheckpointTableColumnsAsync(
        OracleConnection connection,
        CancellationToken cancellationToken)
    {
        var tableName = NormalizeCheckpointTableName();
        var objectName = ParseOracleObjectName(tableName);

        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var command = connection.CreateCommand())
        {
            command.BindByName = true;
            command.CommandText =
                """
                SELECT COLUMN_NAME
                FROM ALL_TAB_COLUMNS
                WHERE OWNER = NVL(:owner, SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'))
                  AND TABLE_NAME = :tableName
                """;
            command.Parameters.Add(
                "owner",
                OracleDbType.Varchar2,
                string.IsNullOrWhiteSpace(objectName.Owner) ? DBNull.Value : objectName.Owner,
                ParameterDirection.Input);
            command.Parameters.Add("tableName", OracleDbType.Varchar2, objectName.TableName, ParameterDirection.Input);

            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                existingColumns.Add(ReadString(reader, 0));
            }
        }

        var missingColumns = new List<string>();
        if (!existingColumns.Contains("DatabaseId"))
        {
            missingColumns.Add("DatabaseId NUMBER(38) NULL");
        }

        if (!existingColumns.Contains("DatabaseUniqueName"))
        {
            missingColumns.Add("DatabaseUniqueName VARCHAR2(128) NULL");
        }

        if (!existingColumns.Contains("ResetLogsChangeNumber"))
        {
            missingColumns.Add("ResetLogsChangeNumber NUMBER(38) NULL");
        }

        if (!existingColumns.Contains("ArchiveLogMode"))
        {
            missingColumns.Add("ArchiveLogMode VARCHAR2(32) NULL");
        }

        if (!existingColumns.Contains("SupplementalLogDataMin"))
        {
            missingColumns.Add("SupplementalLogDataMin VARCHAR2(32) NULL");
        }

        foreach (var definition in missingColumns)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"ALTER TABLE {tableName} ADD ({definition})";

            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OracleException exception) when (exception.Number == OracleColumnAlreadyExists)
            {
            }
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
            SELECT
                CommitScn,
                ChangeScn,
                RecordSetId,
                SqlSequenceNumber,
                CheckpointToken,
                DatabaseId,
                DatabaseUniqueName,
                ResetLogsChangeNumber,
                ArchiveLogMode,
                SupplementalLogDataMin,
                UpdatedAtUtc
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
        var databaseId = ReadNullableDecimal(reader, 5);
        var databaseUniqueName = ReadNullableString(reader, 6);
        var resetLogsChangeNumber = ReadNullableDecimal(reader, 7);
        var archiveLogMode = ReadNullableString(reader, 8);
        var supplementalLogDataMin = ReadNullableString(reader, 9);
        var updatedAtUtc = reader.IsDBNull(10)
            ? DateTimeOffset.MinValue
            : reader.GetFieldType(10) == typeof(DateTimeOffset)
                ? reader.GetFieldValue<DateTimeOffset>(10)
                : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(10), DateTimeKind.Utc));

        if (!string.Equals(serializedToken, token.Serialize(), StringComparison.Ordinal))
        {
            token = OracleLogMinerCheckpointToken.Parse(serializedToken);
        }

        return new OracleCheckpointRow(
            token,
            serializedToken,
            updatedAtUtc,
            databaseId,
            databaseUniqueName,
            resetLogsChangeNumber,
            archiveLogMode,
            supplementalLogDataMin);
    }

    private static async Task<OracleDatabaseProfile> ReadDatabaseProfileAsync(
        OracleConnection connection,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                CURRENT_SCN,
                DBID,
                NAME,
                DB_UNIQUE_NAME,
                DATABASE_ROLE,
                OPEN_MODE,
                LOG_MODE,
                RESETLOGS_CHANGE#,
                SUPPLEMENTAL_LOG_DATA_MIN
            FROM V$DATABASE
            """;

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Oracle LogMiner capture could not read V$DATABASE profile data from the source database.");
        }

        return new OracleDatabaseProfile(
            CurrentScn: ReadDecimal(reader, 0),
            DatabaseId: ReadDecimal(reader, 1),
            DatabaseName: ReadString(reader, 2),
            DatabaseUniqueName: ReadNullableString(reader, 3),
            DatabaseRole: ReadNullableString(reader, 4),
            OpenMode: ReadNullableString(reader, 5),
            ArchiveLogMode: ReadNullableString(reader, 6),
            ResetLogsChangeNumber: ReadNullableDecimal(reader, 7),
            SupplementalLogDataMin: ReadNullableString(reader, 8));
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
        OracleDatabaseProfile databaseProfile,
        decimal earliestAvailableScn,
        string resumeMode,
        OracleCheckpointRow? checkpoint)
    {
        var normalizedInitialPosition = NormalizeInitialPosition(captureOptions.InitialPosition);
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["provider"] = OracleDataOptions.ProviderId,
            ["databaseName"] = options.DatabaseName.Trim(),
            ["databaseId"] = databaseProfile.DatabaseId.ToString(CultureInfo.InvariantCulture),
            ["tableSchema"] = captureOptions.TableSchema.Trim(),
            ["tableName"] = captureOptions.TableName.Trim(),
            ["logMinerDictionary"] = "online-catalog",
            ["logMinerMode"] = "committed-only",
            ["initialPosition"] = normalizedInitialPosition,
            ["currentScn"] = databaseProfile.CurrentScn.ToString(CultureInfo.InvariantCulture),
            ["earliestAvailableScn"] = earliestAvailableScn.ToString(CultureInfo.InvariantCulture),
            ["checkpointStore"] = NormalizeCheckpointTableName(),
            ["checkpointSource"] = "cephalon-checkpoint-table",
            ["redoCursor"] = "commit-scn|change-scn|rs-id|ssn",
            ["sourceId"] = descriptor.SourceId,
            ["resumeMode"] = resumeMode,
            ["databaseIdentityState"] = "observed",
            ["databaseIdentityAction"] = "accept",
            ["archiveLogLifecycleState"] = "available",
            ["archiveLogLifecycleAction"] = checkpoint is null ? "start" : "resume",
            ["resumeFromEarliestAvailableScnIfCheckpointUnavailable"] =
                captureOptions.ResumeFromEarliestAvailableScnIfCheckpointUnavailable ? "true" : "false",
            ["archiveLogLifecyclePolicy"] = captureOptions.ResumeFromEarliestAvailableScnIfCheckpointUnavailable
                ? "resume-from-earliest-available-when-checkpoint-unavailable"
                : "fail-when-checkpoint-unavailable"
        };

        UpsertOptional(metadata, "databaseUniqueName", databaseProfile.DatabaseUniqueName);
        UpsertOptional(metadata, "databaseRole", databaseProfile.DatabaseRole);
        UpsertOptional(metadata, "databaseOpenMode", databaseProfile.OpenMode);
        UpsertOptional(metadata, "archiveLogMode", databaseProfile.ArchiveLogMode);
        UpsertOptional(metadata, "resetLogsChangeNumber", databaseProfile.ResetLogsChangeNumber);
        UpsertOptional(metadata, "supplementalLogDataMin", databaseProfile.SupplementalLogDataMin);
        UpsertOptional(metadata, "expectedDatabaseId", captureOptions.ExpectedDatabaseId);
        UpsertOptional(metadata, "expectedDatabaseUniqueName", captureOptions.ExpectedDatabaseUniqueName);

        if (checkpoint is not null)
        {
            metadata["resumeCheckpoint"] = checkpoint.SerializedToken;
            metadata["checkpointUpdatedAtUtc"] = checkpoint.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture);
            UpsertOptional(metadata, "checkpointDatabaseId", checkpoint.DatabaseId);
            UpsertOptional(metadata, "checkpointDatabaseUniqueName", checkpoint.DatabaseUniqueName);
            UpsertOptional(metadata, "checkpointResetLogsChangeNumber", checkpoint.ResetLogsChangeNumber);
            UpsertOptional(metadata, "checkpointArchiveLogMode", checkpoint.ArchiveLogMode);
            UpsertOptional(metadata, "checkpointSupplementalLogDataMin", checkpoint.SupplementalLogDataMin);
        }

        return metadata;
    }

    private static void ValidateCaptureOptions(OracleLogMinerCaptureOptions captureOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.SourceModuleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.TableSchema);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.TableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.OutboxId);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.ChannelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.MessageType);

        if (captureOptions.MaxChangesPerRead <= 0)
        {
            throw new InvalidOperationException(
                $"Oracle LogMiner capture '{captureOptions.Id}' must configure MaxChangesPerRead greater than zero.");
        }

        if (captureOptions.MaxAwaitTimeSeconds <= 0)
        {
            throw new InvalidOperationException(
                $"Oracle LogMiner capture '{captureOptions.Id}' must configure MaxAwaitTimeSeconds greater than zero.");
        }

        if (captureOptions.PollingIntervalSeconds <= 0)
        {
            throw new InvalidOperationException(
                $"Oracle LogMiner capture '{captureOptions.Id}' must configure PollingIntervalSeconds greater than zero.");
        }

        _ = NormalizeInitialPosition(captureOptions.InitialPosition);
    }

    private static void ValidateArchiveLogMode(
        OracleLogMinerCaptureOptions captureOptions,
        OracleDatabaseProfile databaseProfile,
        Dictionary<string, string> metadata)
    {
        if (string.Equals(databaseProfile.ArchiveLogMode, "ARCHIVELOG", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        metadata["archiveLogLifecycleState"] = "disabled";
        metadata["archiveLogLifecycleAction"] = "fail";
        throw new OracleLogMinerCaptureException(
            $"Oracle LogMiner capture '{captureOptions.Id}' requires ARCHIVELOG mode on database '{databaseProfile.DatabaseName}'. Configure archive logging before starting the Cephalon Oracle CDC runner.",
            "archive-log-disabled",
            new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase));
    }

    private static void ValidateDatabaseIdentity(
        OracleLogMinerCaptureOptions captureOptions,
        OracleCheckpointRow? checkpoint,
        OracleDatabaseProfile databaseProfile,
        Dictionary<string, string> metadata)
    {
        var expectedDatabaseUniqueName = captureOptions.ExpectedDatabaseUniqueName.Trim();
        if (captureOptions.ExpectedDatabaseId.HasValue)
        {
            if (databaseProfile.DatabaseId != captureOptions.ExpectedDatabaseId.Value)
            {
                metadata["databaseIdentityState"] = "mismatch";
                metadata["databaseIdentityAction"] = "fail";
                throw new OracleLogMinerCaptureException(
                    $"Oracle LogMiner capture '{captureOptions.Id}' expected database ID '{captureOptions.ExpectedDatabaseId.Value.ToString(CultureInfo.InvariantCulture)}', but the live source database reported '{databaseProfile.DatabaseId.ToString(CultureInfo.InvariantCulture)}'.",
                    "database-identity-mismatch",
                    new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase));
            }

            metadata["databaseIdentityState"] = "expected-match";
            metadata["databaseIdentityAction"] = "accept";
        }

        if (!string.IsNullOrWhiteSpace(expectedDatabaseUniqueName))
        {
            if (string.IsNullOrWhiteSpace(databaseProfile.DatabaseUniqueName))
            {
                metadata["databaseIdentityState"] = "unknown";
                metadata["databaseIdentityAction"] = "fail";
                throw new OracleLogMinerCaptureException(
                    $"Oracle LogMiner capture '{captureOptions.Id}' expected database unique name '{expectedDatabaseUniqueName}', but the live source database did not report DB_UNIQUE_NAME.",
                    "database-identity-unavailable",
                    new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase));
            }

            if (!string.Equals(databaseProfile.DatabaseUniqueName, expectedDatabaseUniqueName, StringComparison.OrdinalIgnoreCase))
            {
                metadata["databaseIdentityState"] = "mismatch";
                metadata["databaseIdentityAction"] = "fail";
                throw new OracleLogMinerCaptureException(
                    $"Oracle LogMiner capture '{captureOptions.Id}' expected database unique name '{expectedDatabaseUniqueName}', but the live source database reported '{databaseProfile.DatabaseUniqueName}'.",
                    "database-identity-mismatch",
                    new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase));
            }

            metadata["databaseIdentityState"] = "expected-match";
            metadata["databaseIdentityAction"] = "accept";
        }

        if (checkpoint is null)
        {
            return;
        }

        if (checkpoint.DatabaseId.HasValue && checkpoint.DatabaseId.Value != databaseProfile.DatabaseId)
        {
            metadata["databaseIdentityState"] = "checkpoint-mismatch";
            metadata["databaseIdentityAction"] = "fail";
            throw new OracleLogMinerCaptureException(
                $"Oracle LogMiner capture '{captureOptions.Id}' cannot resume checkpoint '{checkpoint.SerializedToken}' because it was recorded against database ID '{checkpoint.DatabaseId.Value.ToString(CultureInfo.InvariantCulture)}', but the live source database reported '{databaseProfile.DatabaseId.ToString(CultureInfo.InvariantCulture)}'.",
                "checkpoint-database-mismatch",
                new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(checkpoint.DatabaseUniqueName))
        {
            if (string.IsNullOrWhiteSpace(databaseProfile.DatabaseUniqueName))
            {
                metadata["databaseIdentityState"] = "unknown";
                metadata["databaseIdentityAction"] = "fail";
                throw new OracleLogMinerCaptureException(
                    $"Oracle LogMiner capture '{captureOptions.Id}' needs DB_UNIQUE_NAME to validate checkpoint '{checkpoint.SerializedToken}', but the live source database did not report DB_UNIQUE_NAME.",
                    "database-identity-unavailable",
                    new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase));
            }

            if (!string.Equals(checkpoint.DatabaseUniqueName, databaseProfile.DatabaseUniqueName, StringComparison.OrdinalIgnoreCase))
            {
                metadata["databaseIdentityState"] = "checkpoint-mismatch";
                metadata["databaseIdentityAction"] = "fail";
                throw new OracleLogMinerCaptureException(
                    $"Oracle LogMiner capture '{captureOptions.Id}' cannot resume checkpoint '{checkpoint.SerializedToken}' because it was recorded against database unique name '{checkpoint.DatabaseUniqueName}', but the live source database reported '{databaseProfile.DatabaseUniqueName}'.",
                    "checkpoint-database-mismatch",
                    new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase));
            }
        }

        if (checkpoint.ResetLogsChangeNumber.HasValue)
        {
            if (!databaseProfile.ResetLogsChangeNumber.HasValue)
            {
                metadata["databaseIdentityState"] = "unknown";
                metadata["databaseIdentityAction"] = "fail";
                throw new OracleLogMinerCaptureException(
                    $"Oracle LogMiner capture '{captureOptions.Id}' needs RESETLOGS_CHANGE# to validate checkpoint '{checkpoint.SerializedToken}', but the live source database did not report it.",
                    "database-identity-unavailable",
                    new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase));
            }

            if (checkpoint.ResetLogsChangeNumber.Value != databaseProfile.ResetLogsChangeNumber.Value)
            {
                metadata["databaseIdentityState"] = "checkpoint-mismatch";
                metadata["databaseIdentityAction"] = "fail";
                throw new OracleLogMinerCaptureException(
                    $"Oracle LogMiner capture '{captureOptions.Id}' cannot resume checkpoint '{checkpoint.SerializedToken}' because it was recorded against RESETLOGS_CHANGE# '{checkpoint.ResetLogsChangeNumber.Value.ToString(CultureInfo.InvariantCulture)}', but the live source database reported '{databaseProfile.ResetLogsChangeNumber.Value.ToString(CultureInfo.InvariantCulture)}'.",
                    "checkpoint-database-mismatch",
                    new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase));
            }
        }

        if (checkpoint.DatabaseId.HasValue ||
            !string.IsNullOrWhiteSpace(checkpoint.DatabaseUniqueName) ||
            checkpoint.ResetLogsChangeNumber.HasValue)
        {
            metadata["databaseIdentityState"] = "checkpoint-match";
            metadata["databaseIdentityAction"] = "resume";
        }
    }

    private static decimal ResolveStartScn(
        OracleLogMinerCaptureOptions captureOptions,
        OracleCheckpointRow? checkpoint,
        decimal currentScn,
        decimal earliestAvailableScn,
        Dictionary<string, string> metadata)
    {
        if (checkpoint is null)
        {
            metadata["archiveLogLifecycleState"] = "available";
            metadata["archiveLogLifecycleAction"] = "start";
            return string.Equals(
                    NormalizeInitialPosition(captureOptions.InitialPosition),
                    "earliest-available",
                    StringComparison.OrdinalIgnoreCase)
                ? earliestAvailableScn
                : currentScn;
        }

        metadata["archiveLogLifecycleState"] = "available";
        metadata["archiveLogLifecycleAction"] = "resume";

        if (checkpoint.Token.ChangeScn > currentScn)
        {
            metadata["archiveLogLifecycleState"] = "checkpoint-ahead";
            metadata["archiveLogLifecycleAction"] = "fail";
            throw new OracleLogMinerCaptureException(
                $"Oracle LogMiner capture '{captureOptions.Id}' cannot resume checkpoint '{checkpoint.SerializedToken}' because checkpoint SCN '{checkpoint.Token.ChangeScn.ToString(CultureInfo.InvariantCulture)}' is newer than the live database current SCN '{currentScn.ToString(CultureInfo.InvariantCulture)}'.",
                "checkpoint-scn-ahead-of-current",
                new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase));
        }

        if (checkpoint.Token.ChangeScn < earliestAvailableScn)
        {
            metadata["archiveLogLifecycleState"] = "checkpoint-pruned";

            if (!captureOptions.ResumeFromEarliestAvailableScnIfCheckpointUnavailable)
            {
                metadata["archiveLogLifecycleAction"] = "fail";
                throw new OracleLogMinerCaptureException(
                    $"Oracle LogMiner capture '{captureOptions.Id}' cannot resume checkpoint '{checkpoint.SerializedToken}' because checkpoint SCN '{checkpoint.Token.ChangeScn.ToString(CultureInfo.InvariantCulture)}' is older than the earliest retained archive-log SCN '{earliestAvailableScn.ToString(CultureInfo.InvariantCulture)}'. Set ResumeFromEarliestAvailableScnIfCheckpointUnavailable to true to reseed from the earliest retained SCN.",
                    "checkpoint-scn-unavailable",
                    new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase));
            }

            metadata["archiveLogLifecycleAction"] = "reseed";
            metadata["resumeMode"] = "earliest-available-after-checkpoint-gap";
            return earliestAvailableScn;
        }

        return checkpoint.Token.ChangeScn;
    }

    private static string ResolveResumeMode(
        OracleLogMinerCaptureOptions captureOptions,
        OracleCheckpointRow? checkpoint)
    {
        if (checkpoint is not null)
        {
            return "checkpoint";
        }

        return NormalizeInitialPosition(captureOptions.InitialPosition);
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
                $"Oracle LogMiner initial position '{initialPosition}' is not supported. Use latest-available or earliest-available.")
        };
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

    private static OracleObjectName ParseOracleObjectName(string normalizedName)
    {
        var parts = normalizedName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            1 => new OracleObjectName(null, parts[0].ToUpperInvariant()),
            2 => new OracleObjectName(parts[0].ToUpperInvariant(), parts[1].ToUpperInvariant()),
            _ => throw new InvalidOperationException(
                $"Oracle checkpoint table name '{normalizedName}' must be either <table> or <schema>.<table>.")
        };
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

    private static decimal? ReadNullableDecimal(OracleDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? null
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
            : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    }

    private static string? ReadNullableString(OracleDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? null
            : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture)?.Trim();
    }

    private static void UpsertOptional(Dictionary<string, string> metadata, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            metadata[key] = value.Trim();
        }
    }

    private static void UpsertOptional(Dictionary<string, string> metadata, string key, decimal? value)
    {
        if (value.HasValue)
        {
            metadata[key] = value.Value.ToString(CultureInfo.InvariantCulture);
        }
    }

    private static void LogIgnoredEndLogMinerFailure(ILogger logger, Exception exception) =>
        LogIgnoredEndLogMinerFailureMessage(logger, exception);

    private sealed record OracleCheckpointRow(
        OracleLogMinerCheckpointToken Token,
        string SerializedToken,
        DateTimeOffset UpdatedAtUtc,
        decimal? DatabaseId,
        string? DatabaseUniqueName,
        decimal? ResetLogsChangeNumber,
        string? ArchiveLogMode,
        string? SupplementalLogDataMin);

    private sealed record OracleDatabaseProfile(
        decimal CurrentScn,
        decimal DatabaseId,
        string DatabaseName,
        string? DatabaseUniqueName,
        string? DatabaseRole,
        string? OpenMode,
        string? ArchiveLogMode,
        decimal? ResetLogsChangeNumber,
        string? SupplementalLogDataMin);

    private sealed record OracleObjectName(
        string? Owner,
        string TableName);
}
