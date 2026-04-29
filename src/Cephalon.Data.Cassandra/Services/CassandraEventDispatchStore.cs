using System.Globalization;
using System.Text.Json;
using Cassandra;
using Cephalon.Data.Cassandra.Configuration;
using Cephalon.Eventing.Services;

namespace Cephalon.Data.Cassandra.Services;

/// <summary>
/// Cassandra-backed dispatch-store implementation that reads pending staged events through a sharded eligibility table
/// and applies durable dispatch outcomes back to the main outbox row plus the same sharded eligibility projection.
/// </summary>
internal sealed class CassandraEventDispatchStore : IEventDispatchStore, IDisposable, IAsyncDisposable
{
    private const string OutboxId = "cassandra-outbox";

    private readonly ICluster cluster;
    private readonly CassandraDataOptions options;
    private readonly SemaphoreSlim initLock = new(1, 1);
    private readonly SemaphoreSlim storageLock = new(1, 1);
    private ISession? session;
    private bool storageReady;
    private PreparedStatement? selectMessageStatement;
    private PreparedStatement? updateMessageStatement;
    private PreparedStatement? deletePendingStatement;
    private PreparedStatement? insertPendingStatement;

    public CassandraEventDispatchStore(ICluster cluster, CassandraDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(cluster);
        ArgumentNullException.ThrowIfNull(options);
        this.cluster = cluster;
        this.options = options;
    }

    public IReadOnlyList<string> OutboxIds => [OutboxId];

    public async ValueTask<IReadOnlyList<EventDispatchItem>> ReadPendingAsync(
        int maximumCount,
        CancellationToken cancellationToken = default)
    {
        if (maximumCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount), maximumCount, "Maximum count must be greater than or equal to 1.");
        }

        var activeSession = await GetSessionAsync(cancellationToken).ConfigureAwait(false);
        await EnsureStorageAsync(activeSession, cancellationToken).ConfigureAwait(false);

        var pendingTable = CassandraOutboxStorageSchema.GetPendingDispatchTableName(options);
        var now = DateTimeOffset.UtcNow.UtcDateTime;

        var shardReads = Enumerable.Range(0, options.PendingDispatchShardCount)
            .Select(shardId => ReadShardCandidatesAsync(activeSession, pendingTable, shardId, now, maximumCount, cancellationToken))
            .ToArray();

        var candidates = (await Task.WhenAll(shardReads).ConfigureAwait(false))
            .SelectMany(static rows => rows)
            .OrderBy(static candidate => candidate.EligibleAtUtc)
            .ThenBy(static candidate => candidate.CreatedAtUtc)
            .ThenBy(static candidate => candidate.MessageId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (candidates.Length == 0)
        {
            return [];
        }

        var items = new List<EventDispatchItem>(maximumCount);
        var seenMessageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!seenMessageIds.Add(candidate.MessageId))
            {
                continue;
            }

            var record = await TryGetRecordAsync(activeSession, candidate.MessageId).ConfigureAwait(false);
            if (record is null || record.DispatchedAtUtc is not null)
            {
                continue;
            }

            if (record.ComputePendingDispatchShard(options.PendingDispatchShardCount) != candidate.ShardId)
            {
                continue;
            }

            if (record.EligibleAtUtc.UtcDateTime != candidate.EligibleAtUtc)
            {
                continue;
            }

            items.Add(record.ToDispatchItem(OutboxId));
            if (items.Count >= maximumCount)
            {
                break;
            }
        }

        return items;
    }

    public async ValueTask ApplyReportAsync(
        EventDispatchExecutionReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (!string.Equals(report.OutboxId, OutboxId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Outbox '{report.OutboxId}' is not owned by the active Cassandra event dispatch store.");
        }

        if (string.IsNullOrWhiteSpace(report.MessageId))
        {
            throw new InvalidOperationException("Dispatch reports must include a message id when applied to the durable Cassandra event dispatch store.");
        }

        var activeSession = await GetSessionAsync(cancellationToken).ConfigureAwait(false);
        await EnsureStorageAsync(activeSession, cancellationToken).ConfigureAwait(false);

        var record = await TryGetRecordAsync(activeSession, report.MessageId).ConfigureAwait(false);
        if (record is null)
        {
            throw new InvalidOperationException($"Outbox message '{report.MessageId}' is not staged in the active Cassandra outbox.");
        }

        if (!string.Equals(record.ChannelId, report.ChannelId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Outbox message '{report.MessageId}' belongs to channel '{record.ChannelId}', but the dispatch report referenced channel '{report.ChannelId}'.");
        }

        var normalizedOutcome = NormalizeOutcome(report.Outcome);
        var observedAtUtc = report.ObservedAtUtc == default
            ? DateTimeOffset.UtcNow
            : report.ObservedAtUtc;
        var oldEligibleAtUtc = record.DispatchedAtUtc is null
            ? record.EligibleAtUtc
            : (DateTimeOffset?)null;
        var oldShardId = record.ComputePendingDispatchShard(options.PendingDispatchShardCount);

        record.DispatchAttemptCount = Math.Max(record.DispatchAttemptCount, report.Attempt);
        switch (normalizedOutcome)
        {
            case EventDispatchExecutionOutcomes.Started:
                record.DispatchedAtUtc = null;
                record.NextAttemptAtUtc = null;
                break;

            case EventDispatchExecutionOutcomes.Succeeded:
            case EventDispatchExecutionOutcomes.Skipped:
                record.DispatchedAtUtc = observedAtUtc;
                record.NextAttemptAtUtc = null;
                break;

            case EventDispatchExecutionOutcomes.Failed:
                record.DispatchedAtUtc = EventDispatchRuntimeMetadataKeys.IsTerminalFailure(report.Metadata)
                    ? observedAtUtc
                    : null;
                record.NextAttemptAtUtc = EventDispatchRuntimeMetadataKeys.IsTerminalFailure(report.Metadata)
                    ? null
                    : TryGetNextAttemptAtUtc(report.Metadata);
                break;

            case EventDispatchExecutionOutcomes.RetryScheduled:
                record.DispatchedAtUtc = null;
                record.NextAttemptAtUtc = TryGetNextAttemptAtUtc(report.Metadata);
                break;
        }

        var batch = new BatchStatement();
        batch.Add(updateMessageStatement!.Bind(
            record.ChannelId,
            record.MessageType,
            record.Payload,
            record.ContentType ?? string.Empty,
            record.CorrelationId ?? string.Empty,
            record.TenantId ?? string.Empty,
            record.OccurredAtUtc.UtcDateTime,
            record.CreatedAtUtc.UtcDateTime,
            record.DispatchedAtUtc?.UtcDateTime,
            record.DispatchAttemptCount,
            record.NextAttemptAtUtc?.UtcDateTime,
            JsonSerializer.Serialize(record.Headers),
            JsonSerializer.Serialize(record.Metadata),
            record.MessageId));

        if (oldEligibleAtUtc is not null)
        {
            batch.Add(deletePendingStatement!.Bind(
                oldShardId,
                oldEligibleAtUtc.Value.UtcDateTime,
                record.MessageId));
        }

        if (record.DispatchedAtUtc is null)
        {
            batch.Add(BuildPendingInsertStatement(record));
        }

        await activeSession.ExecuteAsync(batch).ConfigureAwait(false);
    }

    private async Task<ISession> GetSessionAsync(CancellationToken cancellationToken)
    {
        if (session is not null)
        {
            return session;
        }

        await initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            session ??= await cluster.ConnectAsync(options.Keyspace).ConfigureAwait(false);
        }
        finally
        {
            initLock.Release();
        }

        return session;
    }

    private async Task EnsureStorageAsync(ISession activeSession, CancellationToken cancellationToken)
    {
        if (storageReady)
        {
            return;
        }

        await storageLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (storageReady)
            {
                return;
            }

            await activeSession.ExecuteAsync(CassandraOutboxStorageSchema.CreateMessagesTable(options)).ConfigureAwait(false);
            foreach (var statement in CassandraOutboxStorageSchema.CreateMessagesTableUpgradeStatements(options))
            {
                await activeSession.ExecuteAsync(statement).ConfigureAwait(false);
            }

            await activeSession.ExecuteAsync(CassandraOutboxStorageSchema.CreatePendingDispatchTable(options)).ConfigureAwait(false);

            var messagesTable = CassandraOutboxStorageSchema.GetMessagesTableName(options);
            var pendingTable = CassandraOutboxStorageSchema.GetPendingDispatchTableName(options);

            selectMessageStatement ??= await activeSession.PrepareAsync($@"
                SELECT message_id, channel_id, message_type, payload, content_type,
                       correlation_id, tenant_id, occurred_at_utc, created_at_utc,
                       dispatched_at_utc, dispatch_attempt_count, next_attempt_at_utc,
                       headers_json, metadata_json
                FROM {messagesTable}
                WHERE message_id = ?")
                .ConfigureAwait(false);

            updateMessageStatement ??= await activeSession.PrepareAsync($@"
                UPDATE {messagesTable}
                SET channel_id = ?, message_type = ?, payload = ?, content_type = ?,
                    correlation_id = ?, tenant_id = ?, occurred_at_utc = ?, created_at_utc = ?,
                    dispatched_at_utc = ?, dispatch_attempt_count = ?, next_attempt_at_utc = ?,
                    headers_json = ?, metadata_json = ?
                WHERE message_id = ?")
                .ConfigureAwait(false);

            deletePendingStatement ??= await activeSession.PrepareAsync($@"
                DELETE FROM {pendingTable}
                WHERE shard_id = ? AND eligible_at_utc = ? AND message_id = ?")
                .ConfigureAwait(false);

            insertPendingStatement ??= await activeSession.PrepareAsync($@"
                INSERT INTO {pendingTable} (
                    shard_id, eligible_at_utc, message_id, channel_id, message_type, payload,
                    content_type, correlation_id, tenant_id, occurred_at_utc, created_at_utc,
                    dispatch_attempt_count, headers_json, metadata_json
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)")
                .ConfigureAwait(false);

            storageReady = true;
        }
        finally
        {
            storageLock.Release();
        }
    }

    private static async Task<IReadOnlyList<PendingDispatchCandidate>> ReadShardCandidatesAsync(
        ISession activeSession,
        string pendingTable,
        int shardId,
        DateTime dueAtUtc,
        int maximumCount,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var statement = new SimpleStatement($@"
            SELECT shard_id, eligible_at_utc, message_id, created_at_utc
            FROM {pendingTable}
            WHERE shard_id = ? AND eligible_at_utc <= ?
            LIMIT {maximumCount}", shardId, dueAtUtc);

        var rows = await activeSession.ExecuteAsync(statement).ConfigureAwait(false);
        return rows
            .Select(row => new PendingDispatchCandidate(
                row.GetValue<int>("shard_id"),
                row.GetValue<string>("message_id"),
                ToUtc(row.GetValue<DateTime>("eligible_at_utc")),
                ToUtc(row.GetValue<DateTime>("created_at_utc"))))
            .ToArray();
    }

    private async Task<CassandraOutboxRecord?> TryGetRecordAsync(ISession activeSession, string messageId)
    {
        var rowSet = await activeSession.ExecuteAsync(selectMessageStatement!.Bind(messageId.Trim())).ConfigureAwait(false);
        var row = rowSet.FirstOrDefault();
        return row is null
            ? null
            : CassandraOutboxRecord.FromRow(row);
    }

    private BoundStatement BuildPendingInsertStatement(CassandraOutboxRecord record) =>
        insertPendingStatement!.Bind(
            record.ComputePendingDispatchShard(options.PendingDispatchShardCount),
            record.EligibleAtUtc.UtcDateTime,
            record.MessageId,
            record.ChannelId,
            record.MessageType,
            record.Payload,
            record.ContentType ?? string.Empty,
            record.CorrelationId ?? string.Empty,
            record.TenantId ?? string.Empty,
            record.OccurredAtUtc.UtcDateTime,
            record.CreatedAtUtc.UtcDateTime,
            record.DispatchAttemptCount,
            JsonSerializer.Serialize(record.Headers),
            JsonSerializer.Serialize(record.Metadata));

    private static string NormalizeOutcome(string outcome)
    {
        var normalized = outcome.Trim().ToLowerInvariant();
        return normalized switch
        {
            EventDispatchExecutionOutcomes.Started => EventDispatchExecutionOutcomes.Started,
            EventDispatchExecutionOutcomes.Succeeded => EventDispatchExecutionOutcomes.Succeeded,
            EventDispatchExecutionOutcomes.Failed => EventDispatchExecutionOutcomes.Failed,
            EventDispatchExecutionOutcomes.RetryScheduled => EventDispatchExecutionOutcomes.RetryScheduled,
            EventDispatchExecutionOutcomes.Skipped => EventDispatchExecutionOutcomes.Skipped,
            _ => throw new InvalidOperationException($"Dispatch outcome '{outcome}' is not supported by the Cassandra event dispatch store.")
        };
    }

    private static DateTimeOffset? TryGetNextAttemptAtUtc(IReadOnlyDictionary<string, string> metadata)
    {
        if (!metadata.TryGetValue(EventDispatchRuntimeMetadataKeys.NextRetryAtUtc, out var rawValue) || string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        return DateTimeOffset.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedValue)
            ? parsedValue
            : null;
    }

    private static DateTimeOffset ToUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? new DateTimeOffset(value)
            : new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    public void Dispose()
    {
        storageLock.Dispose();
        initLock.Dispose();
        session?.Dispose();
        session = null;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private readonly record struct PendingDispatchCandidate(
        int ShardId,
        string MessageId,
        DateTimeOffset EligibleAtUtc,
        DateTimeOffset CreatedAtUtc);
}
