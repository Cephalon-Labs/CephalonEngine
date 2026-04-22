using System.Text.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Data.Postgres.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Npgsql.Replication;
using Npgsql.Replication.PgOutput;
using Npgsql.Replication.PgOutput.Messages;
using NpgsqlTypes;

namespace Cephalon.Data.Postgres.Services;

internal sealed class PostgresLogicalReplicationTransport(
    string connectionString,
    ILogger<PostgresLogicalReplicationTransport> logger)
    : IPostgresLogicalReplicationTransport, IAsyncDisposable
{
    private const string ContentType = "application/vnd.cephalon.postgresql.logical-replication+json";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly Action<ILogger, string, string, Exception?> LogCreatedReplicationSlotMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(6990, nameof(LogCreatedReplicationSlot)),
            "PostgreSQL logical-replication capture '{CdcCaptureId}' created replication slot '{SlotName}'.");
    private readonly Lock pendingGate = new();
    private readonly Dictionary<string, PendingReplicationSession> pendingSessions = new(StringComparer.OrdinalIgnoreCase);

    public async Task<PostgresLogicalReplicationReadBatch> ReadBatchAsync(
        PostgresLogicalReplicationCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(captureOptions);
        ArgumentNullException.ThrowIfNull(descriptor);

        ValidateCaptureOptions(captureOptions);
        var expectedDatabaseName = ResolveExpectedDatabaseName(descriptor);

        lock (pendingGate)
        {
            if (pendingSessions.ContainsKey(descriptor.Id))
            {
                throw new InvalidOperationException(
                    $"PostgreSQL logical replication capture '{descriptor.Id}' already has a pending unacknowledged batch. Commit or abandon that batch before reading the next one.");
            }
        }

        var publicationStatus = await ReadPublicationAndSlotStatusAsync(captureOptions, cancellationToken).ConfigureAwait(false);
        if (!publicationStatus.PublicationIncludesTable)
        {
            throw new PostgresLogicalReplicationCaptureException(
                $"PostgreSQL publication '{captureOptions.PublicationName}' does not publish table '{captureOptions.TableSchema}.{captureOptions.TableName}'. Configure publication ownership before starting the Cephalon PostgreSQL CDC runner.",
                "publication-table-missing",
                MergeMetadata(
                    publicationStatus.ToMetadata(captureOptions, expectedDatabaseName),
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["slotLifecycleAction"] = "none"
                    }));
        }

        LogicalReplicationConnection? replicationConnection = null;
        IAsyncEnumerator<PgOutputReplicationMessage>? enumerator = null;
        PendingReplicationSession? pendingSession = null;

        try
        {
            replicationConnection = new LogicalReplicationConnection(connectionString);
            await replicationConnection.Open(cancellationToken).ConfigureAwait(false);

            var slot = await EnsureReplicationSlotAsync(
                    replicationConnection,
                    captureOptions,
                    descriptor.Id,
                    expectedDatabaseName,
                    publicationStatus,
                    cancellationToken)
                .ConfigureAwait(false);
            var batchMetadata = MergeMetadata(
                publicationStatus.ToMetadata(captureOptions, expectedDatabaseName),
                slot.Metadata);

            var stream = replicationConnection.StartReplication(
                slot.Slot,
                new PgOutputReplicationOptions(
                    captureOptions.PublicationName.Trim(),
                    PgOutputProtocolVersion.V1,
                    null,
                    null,
                    null,
                    null),
                cancellationToken,
                walLocation: null);

            enumerator = stream.GetAsyncEnumerator(cancellationToken);

            var deadlineUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, captureOptions.MaxAwaitTimeSeconds));
            var maxChangesPerRead = Math.Max(1, captureOptions.MaxChangesPerRead);
            var bufferedChanges = new List<BufferedReplicationOperation>();
            var capturedChanges = new List<PostgresLogicalReplicationCapturedChange>(maxChangesPerRead);
            NpgsqlLogSequenceNumber? currentTransactionFinalLsn = null;
            DateTime? currentTransactionCommitTimestamp = null;
            var capturedChangeCountAtCurrentCommit = 0;
            var hasMoreChanges = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                var remaining = deadlineUtc - DateTimeOffset.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                var moveNextTask = enumerator.MoveNextAsync().AsTask();
                bool movedNext;
                try
                {
                    movedNext = await moveNextTask.WaitAsync(remaining, cancellationToken).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    break;
                }

                if (!movedNext)
                {
                    break;
                }

                var message = enumerator.Current;
                switch (message)
                {
                    case BeginMessage begin:
                        bufferedChanges.Clear();
                        currentTransactionFinalLsn = begin.TransactionFinalLsn;
                        currentTransactionCommitTimestamp = begin.TransactionCommitTimestamp;
                        capturedChangeCountAtCurrentCommit = 0;
                        break;
                    case InsertMessage insert:
                        if (IsTargetRelation(insert.Relation, captureOptions))
                        {
                            bufferedChanges.Add(new BufferedReplicationOperation(
                                OperationName: "insert",
                                Relation: insert.Relation,
                                NewRow: await SerializeTupleAsync(insert.Relation, insert.NewRow, cancellationToken).ConfigureAwait(false),
                                OldRow: null,
                                KeyRow: null,
                                WalStart: insert.WalStart.ToString(),
                                WalEnd: insert.WalEnd.ToString(),
                                ServerClock: insert.ServerClock));
                        }
                        break;
                    case DefaultUpdateMessage update:
                        if (IsTargetRelation(update.Relation, captureOptions))
                        {
                            bufferedChanges.Add(new BufferedReplicationOperation(
                                OperationName: "update",
                                Relation: update.Relation,
                                NewRow: await SerializeTupleAsync(update.Relation, update.NewRow, cancellationToken).ConfigureAwait(false),
                                OldRow: null,
                                KeyRow: null,
                                WalStart: update.WalStart.ToString(),
                                WalEnd: update.WalEnd.ToString(),
                                ServerClock: update.ServerClock));
                        }
                        break;
                    case FullUpdateMessage update:
                        if (IsTargetRelation(update.Relation, captureOptions))
                        {
                            bufferedChanges.Add(new BufferedReplicationOperation(
                                OperationName: "update",
                                Relation: update.Relation,
                                NewRow: await SerializeTupleAsync(update.Relation, update.NewRow, cancellationToken).ConfigureAwait(false),
                                OldRow: await SerializeTupleAsync(update.Relation, update.OldRow, cancellationToken).ConfigureAwait(false),
                                KeyRow: null,
                                WalStart: update.WalStart.ToString(),
                                WalEnd: update.WalEnd.ToString(),
                                ServerClock: update.ServerClock));
                        }
                        break;
                    case IndexUpdateMessage update:
                        if (IsTargetRelation(update.Relation, captureOptions))
                        {
                            bufferedChanges.Add(new BufferedReplicationOperation(
                                OperationName: "update",
                                Relation: update.Relation,
                                NewRow: await SerializeTupleAsync(update.Relation, update.NewRow, cancellationToken).ConfigureAwait(false),
                                OldRow: null,
                                KeyRow: await SerializeTupleAsync(update.Relation, update.Key, cancellationToken).ConfigureAwait(false),
                                WalStart: update.WalStart.ToString(),
                                WalEnd: update.WalEnd.ToString(),
                                ServerClock: update.ServerClock));
                        }
                        break;
                    case KeyDeleteMessage delete:
                        if (IsTargetRelation(delete.Relation, captureOptions))
                        {
                            bufferedChanges.Add(new BufferedReplicationOperation(
                                OperationName: "delete",
                                Relation: delete.Relation,
                                NewRow: null,
                                OldRow: null,
                                KeyRow: await SerializeTupleAsync(delete.Relation, delete.Key, cancellationToken).ConfigureAwait(false),
                                WalStart: delete.WalStart.ToString(),
                                WalEnd: delete.WalEnd.ToString(),
                                ServerClock: delete.ServerClock));
                        }
                        break;
                    case FullDeleteMessage delete:
                        if (IsTargetRelation(delete.Relation, captureOptions))
                        {
                            bufferedChanges.Add(new BufferedReplicationOperation(
                                OperationName: "delete",
                                Relation: delete.Relation,
                                NewRow: null,
                                OldRow: await SerializeTupleAsync(delete.Relation, delete.OldRow, cancellationToken).ConfigureAwait(false),
                                KeyRow: null,
                                WalStart: delete.WalStart.ToString(),
                                WalEnd: delete.WalEnd.ToString(),
                                ServerClock: delete.ServerClock));
                        }
                        break;
                    case TruncateMessage truncate:
                        foreach (var relation in truncate.Relations.Where(relation => IsTargetRelation(relation, captureOptions)))
                        {
                            bufferedChanges.Add(new BufferedReplicationOperation(
                                OperationName: "truncate",
                                Relation: relation,
                                NewRow: null,
                                OldRow: null,
                                KeyRow: null,
                                WalStart: truncate.WalStart.ToString(),
                                WalEnd: truncate.WalEnd.ToString(),
                                ServerClock: truncate.ServerClock));
                        }
                        break;
                    case CommitMessage commit when bufferedChanges.Count > 0:
                    {
                        var checkpointToken = new PostgresLogicalReplicationCheckpointToken(
                            captureOptions.SlotName.Trim(),
                            commit.CommitLsn.ToString(),
                            commit.TransactionEndLsn.ToString());
                        for (var index = 0; index < bufferedChanges.Count; index++)
                        {
                            capturedChanges.Add(CreateCapturedChange(
                                descriptor,
                                captureOptions,
                                checkpointToken,
                                currentTransactionFinalLsn?.ToString(),
                                currentTransactionCommitTimestamp,
                                commit,
                                bufferedChanges[index],
                                capturedChangeCountAtCurrentCommit + index + 1));
                        }

                        capturedChangeCountAtCurrentCommit += bufferedChanges.Count;
                        bufferedChanges.Clear();
                        currentTransactionFinalLsn = null;
                        currentTransactionCommitTimestamp = null;

                        if (capturedChanges.Count >= maxChangesPerRead)
                        {
                            hasMoreChanges = true;
                            pendingSession = new PendingReplicationSession(
                                descriptor.Id,
                                replicationConnection,
                                enumerator,
                                commit.TransactionEndLsn,
                                checkpointToken);
                            replicationConnection = null;
                            enumerator = null;
                            StorePendingSession(pendingSession);

                            return new PostgresLogicalReplicationReadBatch(
                                capturedChanges,
                                hasMoreChanges,
                                new Dictionary<string, string>(batchMetadata, StringComparer.OrdinalIgnoreCase));
                        }

                        pendingSession = new PendingReplicationSession(
                            descriptor.Id,
                            replicationConnection,
                            enumerator,
                            commit.TransactionEndLsn,
                            checkpointToken);
                        replicationConnection = null;
                        enumerator = null;
                        StorePendingSession(pendingSession);

                        return new PostgresLogicalReplicationReadBatch(
                            capturedChanges,
                            hasMoreChanges,
                            new Dictionary<string, string>(batchMetadata, StringComparer.OrdinalIgnoreCase));
                    }
                }
            }

            return PostgresLogicalReplicationReadBatch.Idle(new Dictionary<string, string>(batchMetadata, StringComparer.OrdinalIgnoreCase));
        }
        finally
        {
            if (enumerator is not null)
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }

            if (replicationConnection is not null)
            {
                await replicationConnection.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    public async Task CommitCheckpointAsync(
        PostgresLogicalReplicationCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        PostgresLogicalReplicationCheckpointToken checkpointToken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(captureOptions);
        ArgumentNullException.ThrowIfNull(descriptor);

        var session = GetPendingSession(descriptor.Id);
        if (session is null)
        {
            throw new InvalidOperationException(
                $"PostgreSQL logical replication capture '{descriptor.Id}' has no pending batch to acknowledge.");
        }

        if (!string.Equals(session.CheckpointToken.Serialize(), checkpointToken.Serialize(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"PostgreSQL logical replication capture '{descriptor.Id}' attempted to acknowledge checkpoint '{checkpointToken.Serialize()}', but the pending batch expects '{session.CheckpointToken.Serialize()}'.");
        }

        try
        {
            session.Connection.SetReplicationStatus(session.AcknowledgementLsn);
            await session.Connection.SendStatusUpdate(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await ReleasePendingSessionAsync(descriptor.Id).ConfigureAwait(false);
        }
    }

    public Task AbandonPendingBatchAsync(
        PostgresLogicalReplicationCaptureOptions captureOptions,
        CdcCaptureDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(captureOptions);
        ArgumentNullException.ThrowIfNull(descriptor);

        cancellationToken.ThrowIfCancellationRequested();
        return ReleasePendingSessionAsync(descriptor.Id);
    }

    public async ValueTask DisposeAsync()
    {
        PendingReplicationSession[] sessions;
        lock (pendingGate)
        {
            sessions = pendingSessions.Values.ToArray();
            pendingSessions.Clear();
        }

        foreach (var session in sessions)
        {
            await session.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task<PublicationAndSlotStatus> ReadPublicationAndSlotStatusAsync(
        PostgresLogicalReplicationCaptureOptions captureOptions,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var publicationCommand = new NpgsqlCommand(
            """
SELECT EXISTS (
    SELECT 1
    FROM pg_catalog.pg_publication_tables
    WHERE pubname = @publicationName
      AND schemaname = @schemaName
      AND tablename = @tableName
);
""",
            connection);
        publicationCommand.Parameters.AddWithValue("publicationName", captureOptions.PublicationName.Trim());
        publicationCommand.Parameters.AddWithValue("schemaName", captureOptions.TableSchema.Trim());
        publicationCommand.Parameters.AddWithValue("tableName", captureOptions.TableName.Trim());
        var publicationIncludesTable = (bool)(await publicationCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) ?? false);

        await using var slotCommand = new NpgsqlCommand(
            """
SELECT *
FROM pg_catalog.pg_replication_slots
WHERE slot_name = @slotName;
""",
            connection);
        slotCommand.Parameters.AddWithValue("slotName", captureOptions.SlotName.Trim());
        await using var slotReader = await slotCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await slotReader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return PublicationAndSlotStatus.Missing(publicationIncludesTable);
        }

        var ordinals = Enumerable.Range(0, slotReader.FieldCount)
            .ToDictionary(slotReader.GetName, static index => index, StringComparer.OrdinalIgnoreCase);

        return new PublicationAndSlotStatus(
            PublicationIncludesTable: publicationIncludesTable,
            SlotExists: true,
            SlotType: ReadString(slotReader, ordinals, "slot_type"),
            SlotDatabaseName: ReadString(slotReader, ordinals, "database"),
            SlotPlugin: ReadString(slotReader, ordinals, "plugin"),
            SlotTemporary: ReadBoolean(slotReader, ordinals, "temporary"),
            SlotActive: ReadBoolean(slotReader, ordinals, "active"),
            SlotActivePid: ReadInt32(slotReader, ordinals, "active_pid"),
            SlotRestartLsn: ReadString(slotReader, ordinals, "restart_lsn"),
            SlotConfirmedFlushLsn: ReadString(slotReader, ordinals, "confirmed_flush_lsn"),
            SlotWalStatus: ReadString(slotReader, ordinals, "wal_status"),
            SlotSafeWalSize: ReadInt64(slotReader, ordinals, "safe_wal_size"),
            SlotInactiveSinceUtc: ReadDateTimeOffset(slotReader, ordinals, "inactive_since"),
            SlotConflicting: ReadBoolean(slotReader, ordinals, "conflicting"),
            SlotInvalidationReason: ReadString(slotReader, ordinals, "invalidation_reason"),
            SlotFailover: ReadBoolean(slotReader, ordinals, "failover"),
            SlotSynced: ReadBoolean(slotReader, ordinals, "synced"));
    }

    private async Task<SlotResolution> EnsureReplicationSlotAsync(
        LogicalReplicationConnection connection,
        PostgresLogicalReplicationCaptureOptions captureOptions,
        string cdcCaptureId,
        string? expectedDatabaseName,
        PublicationAndSlotStatus slotStatus,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var normalizedInitialPosition = NormalizeInitialPosition(captureOptions.InitialPosition);
        var baseMetadata = slotStatus.ToMetadata(captureOptions, expectedDatabaseName);

        if (slotStatus.HasTypeMismatch)
        {
            throw new PostgresLogicalReplicationCaptureException(
                $"PostgreSQL replication slot '{captureOptions.SlotName}' already exists, but it uses slot type '{slotStatus.SlotType}' instead of the required logical decoding path.",
                "slot-type-mismatch",
                MergeMetadata(
                    baseMetadata,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["slotLifecycleAction"] = "fail"
                    }));
        }

        if (slotStatus.HasPluginMismatch)
        {
            throw new PostgresLogicalReplicationCaptureException(
                $"PostgreSQL replication slot '{captureOptions.SlotName}' already exists, but it uses plugin '{slotStatus.SlotPlugin}' instead of 'pgoutput'.",
                "slot-plugin-mismatch",
                MergeMetadata(
                    baseMetadata,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["slotLifecycleAction"] = "fail"
                    }));
        }

        if (slotStatus.HasDatabaseMismatch(expectedDatabaseName))
        {
            throw new PostgresLogicalReplicationCaptureException(
                $"PostgreSQL replication slot '{captureOptions.SlotName}' belongs to database '{slotStatus.SlotDatabaseName}' instead of '{expectedDatabaseName}'. Recreate the slot against the configured database before starting the Cephalon PostgreSQL CDC runner.",
                "slot-database-mismatch",
                MergeMetadata(
                    baseMetadata,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["slotLifecycleAction"] = "fail"
                    }));
        }

        if (slotStatus.IsSyncedStandbySlot)
        {
            throw new PostgresLogicalReplicationCaptureException(
                $"PostgreSQL replication slot '{captureOptions.SlotName}' is synchronized from a primary server and cannot be used for logical decoding on this standby. Promote a primary-owned slot or switch the Cephalon runner to the writable publisher before resuming capture.",
                "slot-synced-standby",
                MergeMetadata(
                    baseMetadata,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["slotLifecycleAction"] = "fail"
                    }));
        }

        if (slotStatus.SlotActive == true)
        {
            throw new PostgresLogicalReplicationCaptureException(
                slotStatus.SlotActivePid.HasValue
                    ? $"PostgreSQL replication slot '{captureOptions.SlotName}' is already active on walsender pid {slotStatus.SlotActivePid.Value}. Stop the current consumer before starting the Cephalon PostgreSQL CDC runner."
                    : $"PostgreSQL replication slot '{captureOptions.SlotName}' is already active. Stop the current consumer before starting the Cephalon PostgreSQL CDC runner.",
                "slot-active",
                MergeMetadata(
                    baseMetadata,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["slotLifecycleAction"] = "fail"
                    }));
        }

        if (slotStatus.IsInvalidated || slotStatus.IsLost)
        {
            if (!captureOptions.RecreateSlotIfInvalidated)
            {
                throw new PostgresLogicalReplicationCaptureException(
                    CreateInvalidatedSlotMessage(captureOptions, slotStatus),
                    slotStatus.IsLost ? "slot-lost" : "slot-invalidated",
                    MergeMetadata(
                        baseMetadata,
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["slotLifecycleAction"] = "fail"
                        }));
            }

            try
            {
                await connection.DropReplicationSlot(captureOptions.SlotName.Trim(), false, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                throw new PostgresLogicalReplicationCaptureException(
                    $"PostgreSQL replication slot '{captureOptions.SlotName}' could not be dropped before recreation. Resolve the slot lifecycle conflict and retry the Cephalon PostgreSQL CDC runner.",
                    "slot-recreate",
                    MergeMetadata(
                        baseMetadata,
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["slotLifecycleAction"] = "recreate"
                        }),
                    exception);
            }

            var recreatedSlot = await CreateReplicationSlotAsync(
                    connection,
                    captureOptions,
                    cdcCaptureId,
                    normalizedInitialPosition,
                    cancellationToken)
                .ConfigureAwait(false);
            var recreatedMetadata = CreateCreatedSlotMetadata(
                captureOptions,
                expectedDatabaseName,
                recreatedSlot,
                "recreate",
                normalizedInitialPosition);
            if (!string.IsNullOrWhiteSpace(slotStatus.SlotInvalidationReason))
            {
                recreatedMetadata["slotPreviousInvalidationReason"] = slotStatus.SlotInvalidationReason;
            }

            if (!string.IsNullOrWhiteSpace(slotStatus.SlotWalStatus))
            {
                recreatedMetadata["slotPreviousWalStatus"] = slotStatus.SlotWalStatus;
            }

            return new SlotResolution(recreatedSlot, recreatedMetadata);
        }

        if (slotStatus.SlotExists)
        {
            return new SlotResolution(
                new PgOutputReplicationSlot(captureOptions.SlotName.Trim()),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["slotLifecycleAction"] = "reuse"
                });
        }

        if (!captureOptions.CreateSlotIfMissing)
        {
            throw new PostgresLogicalReplicationCaptureException(
                $"PostgreSQL replication slot '{captureOptions.SlotName}' was not found. Set CreateSlotIfMissing to true or provision the slot before starting the Cephalon PostgreSQL CDC runner.",
                "slot-missing",
                MergeMetadata(
                    baseMetadata,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["slotLifecycleAction"] = "fail"
                    }));
        }

        var slot = await CreateReplicationSlotAsync(
                connection,
                captureOptions,
                cdcCaptureId,
                normalizedInitialPosition,
                cancellationToken)
            .ConfigureAwait(false);
        return new SlotResolution(
            slot,
            CreateCreatedSlotMetadata(
                captureOptions,
                expectedDatabaseName,
                slot,
                "create",
                normalizedInitialPosition));
    }

    private static PostgresLogicalReplicationCapturedChange CreateCapturedChange(
        CdcCaptureDescriptor descriptor,
        PostgresLogicalReplicationCaptureOptions captureOptions,
        PostgresLogicalReplicationCheckpointToken checkpointToken,
        string? transactionFinalLsn,
        DateTime? transactionCommitTimestamp,
        CommitMessage commit,
        BufferedReplicationOperation operation,
        int ordinal)
    {
        var databaseName = descriptor.Metadata.TryGetValue("databaseName", out var configuredDatabaseName)
            ? configuredDatabaseName ?? string.Empty
            : string.Empty;
        var changeId = $"{checkpointToken.CommitLsn}:{ordinal.ToString("D4", System.Globalization.CultureInfo.InvariantCulture)}";
        var payload = JsonSerializer.Serialize(
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["publicationName"] = captureOptions.PublicationName.Trim(),
                ["slotName"] = captureOptions.SlotName.Trim(),
                ["databaseName"] = databaseName,
                ["table"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["schema"] = operation.Relation.Namespace,
                    ["name"] = operation.Relation.RelationName
                },
                ["operation"] = operation.OperationName,
                ["commitLsn"] = checkpointToken.CommitLsn,
                ["transactionEndLsn"] = checkpointToken.TransactionEndLsn,
                ["transactionFinalLsn"] = transactionFinalLsn,
                ["transactionCommitTimestamp"] = transactionCommitTimestamp,
                ["serverCommitTimestamp"] = commit.TransactionCommitTimestamp,
                ["walStart"] = operation.WalStart,
                ["walEnd"] = operation.WalEnd,
                ["serverClock"] = operation.ServerClock,
                ["data"] = operation.NewRow,
                ["oldData"] = operation.OldRow,
                ["keyData"] = operation.KeyRow
            },
            SerializerOptions);

        var message = new OutboxMessage(
            id: $"{captureOptions.Id.Trim()}:{changeId}",
            channelId: captureOptions.ChannelId.Trim(),
            messageType: captureOptions.MessageType.Trim(),
            payload: payload,
            occurredAtUtc: DateTimeOffset.UtcNow,
            contentType: ContentType,
            headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["provider"] = PostgresDataOptions.ProviderId,
                ["cdcCaptureId"] = descriptor.Id,
                ["databaseName"] = databaseName,
                ["schemaName"] = operation.Relation.Namespace,
                ["tableName"] = operation.Relation.RelationName,
                ["publicationName"] = captureOptions.PublicationName.Trim(),
                ["slotName"] = captureOptions.SlotName.Trim(),
                ["operation"] = operation.OperationName
            },
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["sourceId"] = descriptor.SourceId,
                ["eventFormat"] = descriptor.EventFormat,
                ["checkpointToken"] = checkpointToken.Serialize()
            });

        return new PostgresLogicalReplicationCapturedChange(changeId, operation.OperationName, checkpointToken, message);
    }

    private static bool IsTargetRelation(
        RelationMessage relation,
        PostgresLogicalReplicationCaptureOptions captureOptions)
    {
        return string.Equals(relation.Namespace, captureOptions.TableSchema.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(relation.RelationName, captureOptions.TableName.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<Dictionary<string, object?>> SerializeTupleAsync(
        RelationMessage relation,
        ReplicationTuple tuple,
        CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var index = 0;
        await foreach (var value in tuple.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var columnName = relation.Columns.Count > index
                ? relation.Columns[index].ColumnName
                : $"column{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

            object? serializedValue;
            if (value.IsDBNull)
            {
                serializedValue = null;
            }
            else if (value.IsUnchangedToastedValue)
            {
                serializedValue = "[unchanged-toasted]";
            }
            else
            {
                serializedValue = MakeJsonFriendly(await value.Get(cancellationToken).ConfigureAwait(false));
            }

            values[columnName] = serializedValue;
            index++;
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

    private async Task<PgOutputReplicationSlot> CreateReplicationSlotAsync(
        LogicalReplicationConnection connection,
        PostgresLogicalReplicationCaptureOptions captureOptions,
        string cdcCaptureId,
        string normalizedInitialPosition,
        CancellationToken cancellationToken)
    {
        try
        {
            var slot = await connection.CreatePgOutputReplicationSlot(
                    captureOptions.SlotName.Trim(),
                    false,
                    ResolveSnapshotInitMode(normalizedInitialPosition),
                    false,
                    cancellationToken)
                .ConfigureAwait(false);
            LogCreatedReplicationSlot(logger, cdcCaptureId, captureOptions.SlotName.Trim());
            return slot;
        }
        catch (Exception exception)
        {
            throw new PostgresLogicalReplicationCaptureException(
                $"PostgreSQL replication slot '{captureOptions.SlotName}' could not be created for the Cephalon PostgreSQL CDC runner.",
                "slot-create",
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["publicationName"] = captureOptions.PublicationName.Trim(),
                    ["slotName"] = captureOptions.SlotName.Trim(),
                    ["slotLifecycleAction"] = "create",
                    ["slotResumeMode"] = normalizedInitialPosition
                },
                exception);
        }
    }

    private static string NormalizeInitialPosition(string? initialPosition)
    {
        var normalized = string.IsNullOrWhiteSpace(initialPosition)
            ? "slot-consistent-point"
            : initialPosition.Trim().ToLowerInvariant();

        return normalized switch
        {
            "slot-consistent-point" => normalized,
            "latest-available" => normalized,
            _ => throw new InvalidOperationException(
                $"PostgreSQL logical-replication initial position '{initialPosition}' is not supported. Use slot-consistent-point or latest-available.")
        };
    }

    private static LogicalSlotSnapshotInitMode ResolveSnapshotInitMode(string normalizedInitialPosition)
    {
        return normalizedInitialPosition switch
        {
            "latest-available" => LogicalSlotSnapshotInitMode.NoExport,
            _ => LogicalSlotSnapshotInitMode.Export
        };
    }

    private static void ValidateCaptureOptions(PostgresLogicalReplicationCaptureOptions captureOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.PublicationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.SlotName);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.TableSchema);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.TableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.OutboxId);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.ChannelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureOptions.MessageType);

        if (captureOptions.MaxChangesPerRead <= 0)
        {
            throw new InvalidOperationException(
                $"PostgreSQL logical-replication capture '{captureOptions.Id}' must configure MaxChangesPerRead greater than zero.");
        }

        if (captureOptions.MaxAwaitTimeSeconds <= 0)
        {
            throw new InvalidOperationException(
                $"PostgreSQL logical-replication capture '{captureOptions.Id}' must configure MaxAwaitTimeSeconds greater than zero.");
        }

        if (captureOptions.PollingIntervalSeconds <= 0)
        {
            throw new InvalidOperationException(
                $"PostgreSQL logical-replication capture '{captureOptions.Id}' must configure PollingIntervalSeconds greater than zero.");
        }

        _ = NormalizeInitialPosition(captureOptions.InitialPosition);
    }

    private static string? ResolveExpectedDatabaseName(CdcCaptureDescriptor descriptor)
    {
        return descriptor.Metadata.TryGetValue("databaseName", out var databaseName) &&
               !string.IsNullOrWhiteSpace(databaseName)
            ? databaseName.Trim()
            : null;
    }

    private static Dictionary<string, string> MergeMetadata(
        IReadOnlyDictionary<string, string> metadata,
        IReadOnlyDictionary<string, string>? overrides)
    {
        var merged = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        if (overrides is null)
        {
            return merged;
        }

        foreach (var pair in overrides)
        {
            merged[pair.Key] = pair.Value;
        }

        return merged;
    }

    private static string CreateInvalidatedSlotMessage(
        PostgresLogicalReplicationCaptureOptions captureOptions,
        PublicationAndSlotStatus slotStatus)
    {
        var reason = !string.IsNullOrWhiteSpace(slotStatus.SlotInvalidationReason)
            ? $" invalidation reason '{slotStatus.SlotInvalidationReason}'"
            : string.Empty;
        var walStatus = !string.IsNullOrWhiteSpace(slotStatus.SlotWalStatus)
            ? $" WAL status '{slotStatus.SlotWalStatus}'"
            : string.Empty;

        return
            $"PostgreSQL replication slot '{captureOptions.SlotName}' is no longer usable.{reason}{walStatus}. Set RecreateSlotIfInvalidated to true or recreate the slot before starting the Cephalon PostgreSQL CDC runner.";
    }

    private static Dictionary<string, string> CreateCreatedSlotMetadata(
        PostgresLogicalReplicationCaptureOptions captureOptions,
        string? expectedDatabaseName,
        PgOutputReplicationSlot slot,
        string slotLifecycleAction,
        string normalizedInitialPosition)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["publicationName"] = captureOptions.PublicationName.Trim(),
            ["slotName"] = captureOptions.SlotName.Trim(),
            ["publicationState"] = "includes-table",
            ["slotExists"] = "true",
            ["slotLifecycleState"] = "created",
            ["slotLifecycleAction"] = slotLifecycleAction,
            ["slotResumeMode"] = normalizedInitialPosition,
            ["slotType"] = "logical",
            ["slotPlugin"] = "pgoutput",
            ["slotTemporary"] = "false",
            ["slotActive"] = "false",
            ["slotCreated"] = "true",
            ["slotCreationSnapshotMode"] = ResolveSnapshotInitMode(normalizedInitialPosition) == LogicalSlotSnapshotInitMode.NoExport
                ? "no-export"
                : "export",
            ["slotCreationConsistentPoint"] = slot.ConsistentPoint.ToString()
        };

        if (!string.IsNullOrWhiteSpace(expectedDatabaseName))
        {
            metadata["slotExpectedDatabaseName"] = expectedDatabaseName;
            metadata["slotDatabaseName"] = expectedDatabaseName;
        }

        if (!string.IsNullOrWhiteSpace(slot.SnapshotName))
        {
            metadata["slotCreationSnapshotName"] = slot.SnapshotName;
        }

        return metadata;
    }

    private static string? ReadString(
        NpgsqlDataReader reader,
        Dictionary<string, int> ordinals,
        string columnName)
    {
        return !ordinals.TryGetValue(columnName, out var ordinal) || reader.IsDBNull(ordinal)
            ? null
            : reader.GetValue(ordinal)?.ToString();
    }

    private static bool? ReadBoolean(
        NpgsqlDataReader reader,
        Dictionary<string, int> ordinals,
        string columnName)
    {
        return !ordinals.TryGetValue(columnName, out var ordinal) || reader.IsDBNull(ordinal)
            ? null
            : reader.GetBoolean(ordinal);
    }

    private static int? ReadInt32(
        NpgsqlDataReader reader,
        Dictionary<string, int> ordinals,
        string columnName)
    {
        return !ordinals.TryGetValue(columnName, out var ordinal) || reader.IsDBNull(ordinal)
            ? null
            : reader.GetInt32(ordinal);
    }

    private static long? ReadInt64(
        NpgsqlDataReader reader,
        Dictionary<string, int> ordinals,
        string columnName)
    {
        return !ordinals.TryGetValue(columnName, out var ordinal) || reader.IsDBNull(ordinal)
            ? null
            : reader.GetInt64(ordinal);
    }

    private static DateTimeOffset? ReadDateTimeOffset(
        NpgsqlDataReader reader,
        Dictionary<string, int> ordinals,
        string columnName)
    {
        return !ordinals.TryGetValue(columnName, out var ordinal) || reader.IsDBNull(ordinal)
            ? null
            : reader.GetFieldValue<DateTimeOffset>(ordinal);
    }

    private static void UpsertOptional(IDictionary<string, string> metadata, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            metadata[key] = value;
        }
    }

    private static void UpsertOptional(IDictionary<string, string> metadata, string key, bool? value)
    {
        if (value.HasValue)
        {
            metadata[key] = value.Value ? "true" : "false";
        }
    }

    private static void UpsertOptional(IDictionary<string, string> metadata, string key, int? value)
    {
        if (value.HasValue)
        {
            metadata[key] = value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    private static void UpsertOptional(IDictionary<string, string> metadata, string key, long? value)
    {
        if (value.HasValue)
        {
            metadata[key] = value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    private static void UpsertOptional(IDictionary<string, string> metadata, string key, DateTimeOffset? value)
    {
        if (value.HasValue)
        {
            metadata[key] = value.Value.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    private void StorePendingSession(PendingReplicationSession session)
    {
        lock (pendingGate)
        {
            pendingSessions.Add(session.CdcCaptureId, session);
        }
    }

    private PendingReplicationSession? GetPendingSession(string cdcCaptureId)
    {
        lock (pendingGate)
        {
            return pendingSessions.TryGetValue(cdcCaptureId, out var session)
                ? session
                : null;
        }
    }

    private async Task ReleasePendingSessionAsync(string cdcCaptureId)
    {
        PendingReplicationSession? session;
        lock (pendingGate)
        {
            if (!pendingSessions.Remove(cdcCaptureId, out session))
            {
                return;
            }
        }

        await session.DisposeAsync().ConfigureAwait(false);
    }

    private static void LogCreatedReplicationSlot(ILogger logger, string cdcCaptureId, string slotName) =>
        LogCreatedReplicationSlotMessage(logger, cdcCaptureId, slotName, null);

    private sealed record PublicationAndSlotStatus(
        bool PublicationIncludesTable,
        bool SlotExists,
        string? SlotType,
        string? SlotDatabaseName,
        string? SlotPlugin,
        bool? SlotTemporary,
        bool? SlotActive,
        int? SlotActivePid,
        string? SlotRestartLsn,
        string? SlotConfirmedFlushLsn,
        string? SlotWalStatus,
        long? SlotSafeWalSize,
        DateTimeOffset? SlotInactiveSinceUtc,
        bool? SlotConflicting,
        string? SlotInvalidationReason,
        bool? SlotFailover,
        bool? SlotSynced)
    {
        public static PublicationAndSlotStatus Missing(bool publicationIncludesTable) =>
            new(
                PublicationIncludesTable: publicationIncludesTable,
                SlotExists: false,
                SlotType: null,
                SlotDatabaseName: null,
                SlotPlugin: null,
                SlotTemporary: null,
                SlotActive: null,
                SlotActivePid: null,
                SlotRestartLsn: null,
                SlotConfirmedFlushLsn: null,
                SlotWalStatus: null,
                SlotSafeWalSize: null,
                SlotInactiveSinceUtc: null,
                SlotConflicting: null,
                SlotInvalidationReason: null,
                SlotFailover: null,
                SlotSynced: null);

        public bool HasPluginMismatch =>
            SlotExists &&
            !string.IsNullOrWhiteSpace(SlotPlugin) &&
            !string.Equals(SlotPlugin, "pgoutput", StringComparison.OrdinalIgnoreCase);

        public bool HasTypeMismatch =>
            SlotExists &&
            !string.IsNullOrWhiteSpace(SlotType) &&
            !string.Equals(SlotType, "logical", StringComparison.OrdinalIgnoreCase);

        public bool IsInvalidated => !string.IsNullOrWhiteSpace(SlotInvalidationReason);

        public bool IsLost => string.Equals(SlotWalStatus, "lost", StringComparison.OrdinalIgnoreCase);

        public bool IsSyncedStandbySlot => SlotSynced == true;

        public bool HasDatabaseMismatch(string? expectedDatabaseName) =>
            SlotExists &&
            !string.IsNullOrWhiteSpace(expectedDatabaseName) &&
            !string.IsNullOrWhiteSpace(SlotDatabaseName) &&
            !string.Equals(SlotDatabaseName, expectedDatabaseName, StringComparison.OrdinalIgnoreCase);

        public string LifecycleState
        {
            get
            {
                if (!SlotExists)
                {
                    return "missing";
                }

                if (IsSyncedStandbySlot)
                {
                    return "synced-standby";
                }

                if (IsLost)
                {
                    return "lost";
                }

                if (IsInvalidated)
                {
                    return "invalidated";
                }

                if (SlotActive == true)
                {
                    return "active";
                }

                if (string.Equals(SlotWalStatus, "unreserved", StringComparison.OrdinalIgnoreCase))
                {
                    return "wal-at-risk";
                }

                if (SlotTemporary == true)
                {
                    return "temporary";
                }

                return "ready";
            }
        }

        public string ResumeMode
        {
            get
            {
                if (!SlotExists)
                {
                    return "slot-create-required";
                }

                if (IsSyncedStandbySlot)
                {
                    return "standby-synced-slot";
                }

                if (!string.IsNullOrWhiteSpace(SlotConfirmedFlushLsn))
                {
                    return "slot-confirmed-flush-lsn";
                }

                if (!string.IsNullOrWhiteSpace(SlotRestartLsn))
                {
                    return "slot-restart-lsn";
                }

                return "slot-consistent-point";
            }
        }

        public Dictionary<string, string> ToMetadata(
            PostgresLogicalReplicationCaptureOptions captureOptions,
            string? expectedDatabaseName)
        {
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["publicationName"] = captureOptions.PublicationName.Trim(),
                ["slotName"] = captureOptions.SlotName.Trim(),
                ["publicationState"] = PublicationIncludesTable ? "includes-table" : "missing-table",
                ["slotExists"] = SlotExists ? "true" : "false",
                ["slotLifecycleState"] = LifecycleState,
                ["slotResumeMode"] = ResumeMode,
                ["replicationCheckpointSource"] = "slot-confirmed-flush-lsn"
            };

            if (!string.IsNullOrWhiteSpace(expectedDatabaseName))
            {
                metadata["slotExpectedDatabaseName"] = expectedDatabaseName;
            }

            UpsertOptional(metadata, "slotType", SlotType);
            UpsertOptional(metadata, "slotDatabaseName", SlotDatabaseName);
            UpsertOptional(metadata, "slotPlugin", SlotPlugin);
            UpsertOptional(metadata, "slotTemporary", SlotTemporary);
            UpsertOptional(metadata, "slotActive", SlotActive);
            UpsertOptional(metadata, "slotActivePid", SlotActivePid);
            UpsertOptional(metadata, "slotRestartLsn", SlotRestartLsn);
            UpsertOptional(metadata, "slotConfirmedFlushLsn", SlotConfirmedFlushLsn);
            UpsertOptional(metadata, "slotWalStatus", SlotWalStatus);
            UpsertOptional(metadata, "slotSafeWalSize", SlotSafeWalSize);
            UpsertOptional(metadata, "slotInactiveSinceUtc", SlotInactiveSinceUtc);
            UpsertOptional(metadata, "slotConflicting", SlotConflicting);
            UpsertOptional(metadata, "slotInvalidationReason", SlotInvalidationReason);
            UpsertOptional(metadata, "slotFailover", SlotFailover);
            UpsertOptional(metadata, "slotSynced", SlotSynced);
            return metadata;
        }
    }

    private sealed record SlotResolution(
        PgOutputReplicationSlot Slot,
        IReadOnlyDictionary<string, string> Metadata);

    private sealed record BufferedReplicationOperation(
        string OperationName,
        RelationMessage Relation,
        IReadOnlyDictionary<string, object?>? NewRow,
        IReadOnlyDictionary<string, object?>? OldRow,
        IReadOnlyDictionary<string, object?>? KeyRow,
        string WalStart,
        string WalEnd,
        DateTime ServerClock);

    private sealed class PendingReplicationSession(
        string cdcCaptureId,
        LogicalReplicationConnection connection,
        IAsyncEnumerator<PgOutputReplicationMessage> enumerator,
        NpgsqlLogSequenceNumber acknowledgementLsn,
        PostgresLogicalReplicationCheckpointToken checkpointToken)
        : IAsyncDisposable
    {
        public string CdcCaptureId { get; } = cdcCaptureId;

        public LogicalReplicationConnection Connection { get; } = connection;

        public IAsyncEnumerator<PgOutputReplicationMessage> Enumerator { get; } = enumerator;

        public NpgsqlLogSequenceNumber AcknowledgementLsn { get; } = acknowledgementLsn;

        public PostgresLogicalReplicationCheckpointToken CheckpointToken { get; } = checkpointToken;

        public async ValueTask DisposeAsync()
        {
            await Enumerator.DisposeAsync().ConfigureAwait(false);
            await Connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
