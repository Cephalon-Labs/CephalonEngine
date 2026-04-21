using Cephalon.Abstractions.Data;
using Cephalon.Data.Postgres.Configuration;
using Cephalon.Data.Postgres.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Support;

public sealed class PostgresLogicalReplicationTestChange
{
    public string CommitLsn { get; set; } = "0/16B6E00";

    public string TransactionEndLsn { get; set; } = "0/16B6E30";

    public string? ChangeId { get; set; }

    public string OperationName { get; set; } = "insert";

    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public string Payload { get; set; } = """{"id":"change-001"}""";

    public IDictionary<string, string> Headers { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IDictionary<string, string> Metadata { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class PostgresLogicalReplicationTestBatch
{
    public IList<PostgresLogicalReplicationTestChange> Changes { get; } = [];

    public bool HasMoreChanges { get; set; }

    public IDictionary<string, string> Metadata { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class PostgresDataCdcTestHarness
{
    private readonly Lock gate = new();
    private readonly Queue<PostgresLogicalReplicationTestBatch> batches = new();
    private readonly List<string> committedCheckpoints = [];
    private readonly List<string> abandonedCaptureIds = [];
    private readonly FakePostgresLogicalReplicationTransport transport;

    public PostgresDataCdcTestHarness()
    {
        transport = new FakePostgresLogicalReplicationTransport(this);
    }

    public IReadOnlyList<string> CommittedCheckpoints
    {
        get
        {
            lock (gate)
            {
                return committedCheckpoints.ToArray();
            }
        }
    }

    public IReadOnlyList<string> AbandonedCaptureIds
    {
        get
        {
            lock (gate)
            {
                return abandonedCaptureIds.ToArray();
            }
        }
    }

    public void EnqueueBatch(PostgresLogicalReplicationTestBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        lock (gate)
        {
            batches.Enqueue(batch);
        }
    }

    public void EnqueueIdleBatch(IDictionary<string, string>? metadata = null)
    {
        var batch = new PostgresLogicalReplicationTestBatch();
        if (metadata is not null)
        {
            foreach (var pair in metadata)
            {
                batch.Metadata[pair.Key] = pair.Value;
            }
        }

        EnqueueBatch(batch);
    }

    public IServiceCollection Register(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IPostgresLogicalReplicationTransport>(transport);
        return services;
    }

    private PostgresLogicalReplicationTestBatch DequeueBatch()
    {
        lock (gate)
        {
            return batches.Count > 0
                ? batches.Dequeue()
                : new PostgresLogicalReplicationTestBatch();
        }
    }

    private void RecordCommittedCheckpoint(string checkpoint)
    {
        lock (gate)
        {
            committedCheckpoints.Add(checkpoint);
        }
    }

    private void RecordAbandonedCapture(string cdcCaptureId)
    {
        lock (gate)
        {
            abandonedCaptureIds.Add(cdcCaptureId);
        }
    }

    private sealed class FakePostgresLogicalReplicationTransport(PostgresDataCdcTestHarness owner)
        : IPostgresLogicalReplicationTransport
    {
        private const string ContentType = "application/vnd.cephalon.postgresql.logical-replication+json";

        public Task<PostgresLogicalReplicationReadBatch> ReadBatchAsync(
            PostgresLogicalReplicationCaptureOptions captureOptions,
            CdcCaptureDescriptor descriptor,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = owner.DequeueBatch();
            if (batch.Changes.Count == 0)
            {
                return Task.FromResult(PostgresLogicalReplicationReadBatch.Idle(
                    new Dictionary<string, string>(batch.Metadata, StringComparer.OrdinalIgnoreCase)));
            }

            var changes = batch.Changes
                .Select((change, index) =>
                {
                    var token = new PostgresLogicalReplicationCheckpointToken(
                        captureOptions.SlotName.Trim(),
                        change.CommitLsn,
                        change.TransactionEndLsn);
                    var serializedCheckpoint = token.Serialize();
                    var changeId = string.IsNullOrWhiteSpace(change.ChangeId)
                        ? $"{change.CommitLsn}:{(index + 1).ToString("D4", System.Globalization.CultureInfo.InvariantCulture)}"
                        : change.ChangeId.Trim();

                    var headers = new Dictionary<string, string>(change.Headers, StringComparer.OrdinalIgnoreCase)
                    {
                        ["provider"] = PostgresDataOptions.ProviderId,
                        ["cdcCaptureId"] = descriptor.Id,
                        ["databaseName"] = descriptor.Metadata.TryGetValue("databaseName", out var databaseName) ? databaseName : string.Empty,
                        ["schemaName"] = captureOptions.TableSchema.Trim(),
                        ["tableName"] = captureOptions.TableName.Trim(),
                        ["publicationName"] = captureOptions.PublicationName.Trim(),
                        ["slotName"] = captureOptions.SlotName.Trim(),
                        ["operation"] = change.OperationName.Trim()
                    };

                    var metadata = new Dictionary<string, string>(change.Metadata, StringComparer.OrdinalIgnoreCase)
                    {
                        ["sourceId"] = descriptor.SourceId,
                        ["eventFormat"] = descriptor.EventFormat,
                        ["checkpointToken"] = serializedCheckpoint
                    };

                    return new PostgresLogicalReplicationCapturedChange(
                        changeId,
                        change.OperationName.Trim(),
                        token,
                        new OutboxMessage(
                            id: $"{descriptor.Id}:{changeId}",
                            channelId: captureOptions.ChannelId.Trim(),
                            messageType: captureOptions.MessageType.Trim(),
                            payload: change.Payload,
                            occurredAtUtc: change.OccurredAtUtc,
                            contentType: ContentType,
                            headers: headers,
                            metadata: metadata));
                })
                .ToArray();

            return Task.FromResult(new PostgresLogicalReplicationReadBatch(
                changes,
                batch.HasMoreChanges,
                new Dictionary<string, string>(batch.Metadata, StringComparer.OrdinalIgnoreCase)));
        }

        public Task CommitCheckpointAsync(
            PostgresLogicalReplicationCaptureOptions captureOptions,
            CdcCaptureDescriptor descriptor,
            PostgresLogicalReplicationCheckpointToken checkpointToken,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            owner.RecordCommittedCheckpoint(checkpointToken.Serialize());
            return Task.CompletedTask;
        }

        public Task AbandonPendingBatchAsync(
            PostgresLogicalReplicationCaptureOptions captureOptions,
            CdcCaptureDescriptor descriptor,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            owner.RecordAbandonedCapture(descriptor.Id);
            return Task.CompletedTask;
        }
    }
}

public static class PostgresDataCdcTestHarnessServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresDataCdcTestHarness(
        this IServiceCollection services,
        PostgresDataCdcTestHarness harness)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(harness);

        return harness.Register(services);
    }
}
