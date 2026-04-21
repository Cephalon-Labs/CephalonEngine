using Cephalon.Abstractions.Data;
using Cephalon.Data.SqlServer.Configuration;
using Cephalon.Data.SqlServer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Support;

public sealed class SqlServerCdcTestChange
{
    public string StartLsn { get; set; } = "0x00000000000000000001";

    public string SequenceValue { get; set; } = "0x00000000000000000001";

    public int Operation { get; set; } = 2;

    public string? ChangeId { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public string Payload { get; set; } = """{"id":"change-001"}""";

    public IDictionary<string, string> Headers { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IDictionary<string, string> Metadata { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class SqlServerCdcTestBatch
{
    public IList<SqlServerCdcTestChange> Changes { get; } = [];

    public bool HasMoreChanges { get; set; }

    public IDictionary<string, string> Metadata { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class SqlServerCdcTestHarness
{
    private readonly Lock gate = new();
    private readonly Queue<SqlServerCdcTestBatch> batches = new();
    private readonly List<string> committedCheckpoints = [];
    private readonly FakeSqlServerCdcTransport transport;

    public SqlServerCdcTestHarness()
    {
        transport = new FakeSqlServerCdcTransport(this);
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

    public void EnqueueBatch(SqlServerCdcTestBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        lock (gate)
        {
            batches.Enqueue(batch);
        }
    }

    public void EnqueueIdleBatch(IDictionary<string, string>? metadata = null)
    {
        var batch = new SqlServerCdcTestBatch();
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

        services.AddSingleton<ISqlServerCdcTransport>(transport);
        return services;
    }

    private SqlServerCdcTestBatch DequeueBatch()
    {
        lock (gate)
        {
            return batches.Count > 0
                ? batches.Dequeue()
                : new SqlServerCdcTestBatch();
        }
    }

    private void RecordCommittedCheckpoint(string checkpoint)
    {
        lock (gate)
        {
            committedCheckpoints.Add(checkpoint);
        }
    }

    private sealed class FakeSqlServerCdcTransport(SqlServerCdcTestHarness owner) : ISqlServerCdcTransport
    {
        private const string ContentType = "application/vnd.cephalon.sqlserver.cdc+json";

        public Task<SqlServerCdcReadBatch> ReadBatchAsync(
            SqlServerCdcCaptureOptions captureOptions,
            CdcCaptureDescriptor descriptor,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = owner.DequeueBatch();
            if (batch.Changes.Count == 0)
            {
                return Task.FromResult(SqlServerCdcReadBatch.Idle(
                    new Dictionary<string, string>(batch.Metadata, StringComparer.OrdinalIgnoreCase)));
            }

            var changes = batch.Changes
                .Select(change =>
                {
                    var token = SqlServerCdcCheckpointToken.FromHexStrings(
                        change.StartLsn,
                        change.SequenceValue,
                        change.Operation);
                    var serializedCheckpoint = token.Serialize();
                    var changeId = string.IsNullOrWhiteSpace(change.ChangeId)
                        ? serializedCheckpoint.Replace("|", "-", StringComparison.Ordinal)
                        : change.ChangeId.Trim();
                    var operationName = change.Operation switch
                    {
                        1 => "delete",
                        2 => "insert",
                        3 => "update-before",
                        4 => "update-after",
                        _ => $"operation-{change.Operation}"
                    };

                    var headers = new Dictionary<string, string>(change.Headers, StringComparer.OrdinalIgnoreCase)
                    {
                        ["provider"] = SqlServerDataOptions.ProviderId,
                        ["cdcCaptureId"] = descriptor.Id,
                        ["databaseName"] = descriptor.Metadata.TryGetValue("databaseName", out var databaseName) ? databaseName : string.Empty,
                        ["schemaName"] = captureOptions.TableSchema.Trim(),
                        ["tableName"] = captureOptions.TableName.Trim(),
                        ["captureInstance"] = captureOptions.CaptureInstance.Trim(),
                        ["operation"] = operationName
                    };

                    var metadata = new Dictionary<string, string>(change.Metadata, StringComparer.OrdinalIgnoreCase)
                    {
                        ["sourceId"] = descriptor.SourceId,
                        ["eventFormat"] = descriptor.EventFormat,
                        ["checkpointToken"] = serializedCheckpoint
                    };

                    return new SqlServerCdcCapturedChange(
                        changeId,
                        operationName,
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

            return Task.FromResult(new SqlServerCdcReadBatch(
                changes,
                batch.HasMoreChanges,
                new Dictionary<string, string>(batch.Metadata, StringComparer.OrdinalIgnoreCase)));
        }

        public Task CommitCheckpointAsync(
            SqlServerCdcCaptureOptions captureOptions,
            CdcCaptureDescriptor descriptor,
            SqlServerCdcCheckpointToken checkpointToken,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            owner.RecordCommittedCheckpoint(checkpointToken.Serialize());
            return Task.CompletedTask;
        }
    }
}

public static class SqlServerCdcTestHarnessServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServerCdcTestHarness(
        this IServiceCollection services,
        SqlServerCdcTestHarness harness)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(harness);

        return harness.Register(services);
    }
}
