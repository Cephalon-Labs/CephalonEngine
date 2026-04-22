using Cephalon.Abstractions.Data;
using Cephalon.Data.Oracle.Configuration;
using Cephalon.Data.Oracle.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Support;

public sealed class OracleLogMinerTestChange
{
    public decimal CommitScn { get; set; } = 1100m;

    public decimal ChangeScn { get; set; } = 1095m;

    public string RecordSetId { get; set; } = "0x001";

    public long SqlSequenceNumber { get; set; } = 1L;

    public string? ChangeId { get; set; }

    public string OperationName { get; set; } = "INSERT";

    public int OperationCode { get; set; } = 1;

    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public string Payload { get; set; } = """{"id":"change-001"}""";

    public IDictionary<string, string> Headers { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IDictionary<string, string> Metadata { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class OracleLogMinerTestBatch
{
    public IList<OracleLogMinerTestChange> Changes { get; } = [];

    public bool HasMoreChanges { get; set; }

    public string? FailureKind { get; set; }

    public string? FailureMessage { get; set; }

    public IDictionary<string, string> Metadata { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class OracleDataCdcTestHarness
{
    private readonly Lock gate = new();
    private readonly Queue<OracleLogMinerTestBatch> batches = new();
    private readonly List<string> committedCheckpoints = [];
    private readonly FakeOracleLogMinerTransport transport;

    public OracleDataCdcTestHarness()
    {
        transport = new FakeOracleLogMinerTransport(this);
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

    public void EnqueueBatch(OracleLogMinerTestBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        lock (gate)
        {
            batches.Enqueue(batch);
        }
    }

    public void EnqueueIdleBatch(IDictionary<string, string>? metadata = null)
    {
        var batch = new OracleLogMinerTestBatch();
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

        services.AddSingleton<IOracleLogMinerTransport>(transport);
        return services;
    }

    private OracleLogMinerTestBatch DequeueBatch()
    {
        lock (gate)
        {
            return batches.Count > 0
                ? batches.Dequeue()
                : new OracleLogMinerTestBatch();
        }
    }

    private void RecordCommittedCheckpoint(string checkpoint)
    {
        lock (gate)
        {
            committedCheckpoints.Add(checkpoint);
        }
    }

    private sealed class FakeOracleLogMinerTransport(OracleDataCdcTestHarness owner) : IOracleLogMinerTransport
    {
        private const string ContentType = "application/vnd.cephalon.oracle.logminer+json";

        public Task<OracleLogMinerReadBatch> ReadBatchAsync(
            OracleLogMinerCaptureOptions captureOptions,
            CdcCaptureDescriptor descriptor,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = owner.DequeueBatch();
            if (!string.IsNullOrWhiteSpace(batch.FailureKind))
            {
                throw new OracleLogMinerCaptureException(
                    string.IsNullOrWhiteSpace(batch.FailureMessage)
                        ? $"The Oracle CDC test harness raised '{batch.FailureKind}'."
                        : batch.FailureMessage.Trim(),
                    batch.FailureKind.Trim(),
                    new Dictionary<string, string>(batch.Metadata, StringComparer.OrdinalIgnoreCase));
            }

            if (batch.Changes.Count == 0)
            {
                return Task.FromResult(OracleLogMinerReadBatch.Idle(
                    new Dictionary<string, string>(batch.Metadata, StringComparer.OrdinalIgnoreCase)));
            }

            var changes = batch.Changes
                .Select((change, index) =>
                {
                    var token = new OracleLogMinerCheckpointToken(
                        change.CommitScn,
                        change.ChangeScn,
                        change.RecordSetId,
                        change.SqlSequenceNumber);
                    var serializedCheckpoint = token.Serialize();
                    var changeId = string.IsNullOrWhiteSpace(change.ChangeId)
                        ? $"{change.CommitScn}|{change.ChangeScn}|{change.RecordSetId}|{change.SqlSequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture)}:{(index + 1).ToString("D4", System.Globalization.CultureInfo.InvariantCulture)}"
                        : change.ChangeId.Trim();

                    var headers = new Dictionary<string, string>(change.Headers, StringComparer.OrdinalIgnoreCase)
                    {
                        ["provider"] = OracleDataOptions.ProviderId,
                        ["cdcCaptureId"] = descriptor.Id,
                        ["databaseName"] = descriptor.Metadata.TryGetValue("databaseName", out var databaseName) ? databaseName : string.Empty,
                        ["schemaName"] = descriptor.Metadata.TryGetValue("tableSchema", out var schemaName) ? schemaName : string.Empty,
                        ["tableName"] = captureOptions.TableName.Trim(),
                        ["operation"] = change.OperationName.Trim(),
                        ["operationCode"] = change.OperationCode.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["commitScn"] = change.CommitScn.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["changeScn"] = change.ChangeScn.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["recordSetId"] = change.RecordSetId,
                        ["sqlSequenceNumber"] = change.SqlSequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    };
                    var metadata = new Dictionary<string, string>(change.Metadata, StringComparer.OrdinalIgnoreCase)
                    {
                        ["sourceId"] = descriptor.SourceId,
                        ["eventFormat"] = descriptor.EventFormat,
                        ["checkpointToken"] = serializedCheckpoint
                    };

                    return new OracleLogMinerCapturedChange(
                        changeId,
                        change.OperationName.Trim(),
                        change.OperationCode,
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

            return Task.FromResult(new OracleLogMinerReadBatch(
                changes,
                batch.HasMoreChanges,
                new Dictionary<string, string>(batch.Metadata, StringComparer.OrdinalIgnoreCase)));
        }

        public Task CommitCheckpointAsync(
            OracleLogMinerCaptureOptions captureOptions,
            CdcCaptureDescriptor descriptor,
            OracleLogMinerCheckpointToken checkpointToken,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            owner.RecordCommittedCheckpoint(checkpointToken.Serialize());
            return Task.CompletedTask;
        }
    }
}

public static class OracleDataCdcTestHarnessServiceCollectionExtensions
{
    public static IServiceCollection AddOracleDataCdcTestHarness(
        this IServiceCollection services,
        OracleDataCdcTestHarness harness)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(harness);

        return harness.Register(services);
    }
}
