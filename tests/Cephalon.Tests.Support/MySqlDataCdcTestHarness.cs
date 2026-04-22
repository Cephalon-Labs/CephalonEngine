using Cephalon.Abstractions.Data;
using Cephalon.Data.MySql.Configuration;
using Cephalon.Data.MySql.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Support;

public sealed class MySqlBinlogTestChange
{
    public string BinlogFile { get; set; } = "mysql-bin.000001";

    public long BinlogPosition { get; set; } = 1260;

    public string? ChangeId { get; set; }

    public string OperationName { get; set; } = "insert";

    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public string Payload { get; set; } = """{"id":"change-001"}""";

    public IDictionary<string, string> Headers { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IDictionary<string, string> Metadata { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class MySqlBinlogTestBatch
{
    public IList<MySqlBinlogTestChange> Changes { get; } = [];

    public bool HasMoreChanges { get; set; }

    public string? FailureKind { get; set; }

    public string? FailureMessage { get; set; }

    public IDictionary<string, string> Metadata { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class MySqlDataCdcTestHarness
{
    private readonly Lock gate = new();
    private readonly Queue<MySqlBinlogTestBatch> batches = new();
    private readonly List<string> committedCheckpoints = [];
    private readonly FakeMySqlBinlogTransport transport;

    public MySqlDataCdcTestHarness()
    {
        transport = new FakeMySqlBinlogTransport(this);
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

    public void EnqueueBatch(MySqlBinlogTestBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        lock (gate)
        {
            batches.Enqueue(batch);
        }
    }

    public void EnqueueIdleBatch(IDictionary<string, string>? metadata = null)
    {
        var batch = new MySqlBinlogTestBatch();
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

        services.AddSingleton<IMySqlBinlogTransport>(transport);
        return services;
    }

    private MySqlBinlogTestBatch DequeueBatch()
    {
        lock (gate)
        {
            return batches.Count > 0
                ? batches.Dequeue()
                : new MySqlBinlogTestBatch();
        }
    }

    private void RecordCommittedCheckpoint(string checkpoint)
    {
        lock (gate)
        {
            committedCheckpoints.Add(checkpoint);
        }
    }

    private sealed class FakeMySqlBinlogTransport(MySqlDataCdcTestHarness owner) : IMySqlBinlogTransport
    {
        private const string ContentType = "application/vnd.cephalon.mysql.binlog+json";

        public Task<MySqlBinlogReadBatch> ReadBatchAsync(
            MySqlBinlogCaptureOptions captureOptions,
            CdcCaptureDescriptor descriptor,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = owner.DequeueBatch();
            if (!string.IsNullOrWhiteSpace(batch.FailureKind))
            {
                throw new MySqlBinlogCaptureException(
                    string.IsNullOrWhiteSpace(batch.FailureMessage)
                        ? $"The MySQL CDC test harness raised '{batch.FailureKind}'."
                        : batch.FailureMessage.Trim(),
                    batch.FailureKind.Trim(),
                    new Dictionary<string, string>(batch.Metadata, StringComparer.OrdinalIgnoreCase));
            }

            if (batch.Changes.Count == 0)
            {
                return Task.FromResult(MySqlBinlogReadBatch.Idle(
                    new Dictionary<string, string>(batch.Metadata, StringComparer.OrdinalIgnoreCase)));
            }

            var changes = batch.Changes
                .Select((change, index) =>
                {
                    var sourceServerUuid = ReadOptionalValue(change.Metadata, batch.Metadata, "sourceServerUuid");
                    var sourceServerId = ParseInt64(ReadOptionalValue(change.Metadata, batch.Metadata, "sourceServerId"));
                    var gtidExecutedSet = ReadOptionalValue(change.Metadata, batch.Metadata, "gtidExecutedSet");
                    var binlogFormat = ReadOptionalValue(change.Metadata, batch.Metadata, "binlogFormat");
                    var binlogRowImage = ReadOptionalValue(change.Metadata, batch.Metadata, "binlogRowImage");
                    var gtidMode = ReadOptionalValue(change.Metadata, batch.Metadata, "gtidMode");
                    var token = new MySqlBinlogCheckpointToken(
                        change.BinlogFile,
                        change.BinlogPosition,
                        sourceServerUuid,
                        sourceServerId,
                        gtidExecutedSet,
                        binlogFormat,
                        binlogRowImage);
                    var serializedCheckpoint = token.Serialize();
                    var changeId = string.IsNullOrWhiteSpace(change.ChangeId)
                        ? $"{change.BinlogFile}:{change.BinlogPosition}:{(index + 1).ToString("D4", System.Globalization.CultureInfo.InvariantCulture)}"
                        : change.ChangeId.Trim();

                    var headers = new Dictionary<string, string>(change.Headers, StringComparer.OrdinalIgnoreCase)
                    {
                        ["provider"] = MySqlDataOptions.ProviderId,
                        ["cdcCaptureId"] = descriptor.Id,
                        ["databaseName"] = descriptor.Metadata.TryGetValue("databaseName", out var databaseName) ? databaseName : string.Empty,
                        ["schemaName"] = descriptor.Metadata.TryGetValue("tableSchema", out var schemaName) ? schemaName : string.Empty,
                        ["tableName"] = captureOptions.TableName.Trim(),
                        ["serverId"] = captureOptions.ServerId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["operation"] = change.OperationName.Trim(),
                        ["binlogFile"] = change.BinlogFile
                    };
                    if (!string.IsNullOrWhiteSpace(sourceServerUuid))
                    {
                        headers["sourceServerUuid"] = sourceServerUuid;
                    }

                    var metadata = new Dictionary<string, string>(change.Metadata, StringComparer.OrdinalIgnoreCase)
                    {
                        ["sourceId"] = descriptor.SourceId,
                        ["eventFormat"] = descriptor.EventFormat,
                        ["checkpointToken"] = serializedCheckpoint
                    };
                    if (!string.IsNullOrWhiteSpace(gtidMode))
                    {
                        metadata["gtidMode"] = gtidMode;
                    }

                    if (!string.IsNullOrWhiteSpace(gtidExecutedSet))
                    {
                        metadata["gtidExecutedSet"] = gtidExecutedSet;
                    }

                    return new MySqlBinlogCapturedChange(
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

            return Task.FromResult(new MySqlBinlogReadBatch(
                changes,
                batch.HasMoreChanges,
                new Dictionary<string, string>(batch.Metadata, StringComparer.OrdinalIgnoreCase)));
        }

        public Task CommitCheckpointAsync(
            MySqlBinlogCaptureOptions captureOptions,
            CdcCaptureDescriptor descriptor,
            MySqlBinlogCheckpointToken checkpointToken,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            owner.RecordCommittedCheckpoint(checkpointToken.Serialize());
            return Task.CompletedTask;
        }
    }

    private static string? ReadOptionalValue(
        IDictionary<string, string> primary,
        IDictionary<string, string> secondary,
        string key)
    {
        if (primary.TryGetValue(key, out var primaryValue) && !string.IsNullOrWhiteSpace(primaryValue))
        {
            return primaryValue.Trim();
        }

        return secondary.TryGetValue(key, out var secondaryValue) && !string.IsNullOrWhiteSpace(secondaryValue)
            ? secondaryValue.Trim()
            : null;
    }

    private static long? ParseInt64(string? value)
    {
        return long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }
}

public static class MySqlDataCdcTestHarnessServiceCollectionExtensions
{
    public static IServiceCollection AddMySqlDataCdcTestHarness(
        this IServiceCollection services,
        MySqlDataCdcTestHarness harness)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(harness);

        return harness.Register(services);
    }
}
