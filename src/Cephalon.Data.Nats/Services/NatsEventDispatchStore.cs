using System.Globalization;
using System.Text.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Eventing.Services;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;

namespace Cephalon.Data.Nats.Services;

internal sealed class NatsEventDispatchStore(INatsConnection nats, string bucketName) : IEventDispatchStore
{
    private const string OutboxId = "nats-outbox";

    public IReadOnlyList<string> OutboxIds => [OutboxId];

    public async ValueTask<IReadOnlyList<EventDispatchItem>> ReadPendingAsync(
        int maximumCount,
        CancellationToken cancellationToken = default)
    {
        if (maximumCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount), maximumCount, "Maximum count must be greater than or equal to 1.");
        }

        var kv = await GetStoreAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;
        var candidates = new List<PendingDispatchCandidate>();

        await foreach (var key in kv.GetKeysAsync(cancellationToken: cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entry = await TryGetRecordAsync(kv, key, cancellationToken).ConfigureAwait(false);
            if (entry is null)
            {
                continue;
            }

            var snapshot = entry.Value;
            if (snapshot.Record.DispatchedAtUtc is not null)
            {
                continue;
            }

            if (snapshot.Record.NextAttemptAtUtc is not null && snapshot.Record.NextAttemptAtUtc > now)
            {
                continue;
            }

            candidates.Add(new PendingDispatchCandidate(
                new EventDispatchItem(
                    outboxId: OutboxId,
                    messageId: snapshot.Record.MessageId,
                    channelId: snapshot.Record.ChannelId,
                    eventType: snapshot.Record.MessageType,
                    payload: snapshot.Record.Payload,
                    occurredAtUtc: snapshot.Record.OccurredAtUtc,
                    createdAtUtc: snapshot.Record.CreatedAtUtc,
                    dispatchAttemptCount: snapshot.Record.DispatchAttemptCount,
                    contentType: snapshot.Record.ContentType,
                    correlationId: snapshot.Record.CorrelationId,
                    tenantId: snapshot.Record.TenantId,
                    headers: snapshot.Record.Headers,
                    metadata: snapshot.Record.Metadata),
                snapshot.Record.NextAttemptAtUtc ?? snapshot.Record.CreatedAtUtc));
        }

        return candidates
            .OrderBy(static candidate => candidate.SortAtUtc)
            .ThenBy(static candidate => candidate.Item.CreatedAtUtc)
            .Take(maximumCount)
            .Select(static candidate => candidate.Item)
            .ToArray();
    }

    public async ValueTask ApplyReportAsync(
        EventDispatchExecutionReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (!string.Equals(report.OutboxId, OutboxId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Outbox '{report.OutboxId}' is not owned by the active NATS event dispatch store.");
        }

        if (string.IsNullOrWhiteSpace(report.MessageId))
        {
            throw new InvalidOperationException("Dispatch reports must include a message id when applied to the durable NATS event dispatch store.");
        }

        var kv = await GetStoreAsync(cancellationToken).ConfigureAwait(false);
        var entry = await TryGetRecordAsync(kv, report.MessageId, cancellationToken).ConfigureAwait(false);
        if (entry is null)
        {
            throw new InvalidOperationException($"Outbox message '{report.MessageId}' is not staged in the active NATS outbox.");
        }

        var snapshot = entry.Value;
        var record = snapshot.Record;
        if (!string.Equals(record.ChannelId, report.ChannelId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Outbox message '{report.MessageId}' belongs to channel '{record.ChannelId}', but the dispatch report referenced channel '{report.ChannelId}'.");
        }

        var normalizedOutcome = NormalizeOutcome(report.Outcome);
        var observedAtUtc = report.ObservedAtUtc == default
            ? DateTimeOffset.UtcNow
            : report.ObservedAtUtc;

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
            case EventDispatchExecutionOutcomes.RetryScheduled:
                record.DispatchedAtUtc = null;
                record.NextAttemptAtUtc = TryGetNextAttemptAtUtc(report.Metadata);
                break;
        }

        var bytes = JsonSerializer.SerializeToUtf8Bytes(record);
        _ = await kv.UpdateAsync(record.MessageId, bytes, snapshot.Revision, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<INatsKVStore> GetStoreAsync(CancellationToken cancellationToken)
    {
        var js = new NatsJSContext(nats);
        var kvCtx = new NatsKVContext(js);
        return await kvCtx.CreateOrUpdateStoreAsync(new NatsKVConfig(bucketName), cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<NatsOutboxEntrySnapshot?> TryGetRecordAsync(
        INatsKVStore kv,
        string messageId,
        CancellationToken cancellationToken)
    {
        var result = await kv.TryGetEntryAsync<byte[]>(messageId, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!result.Success || result.Value.Error is not null || result.Value.Value is null)
        {
            return null;
        }

        return new NatsOutboxEntrySnapshot(
            result.Value.Revision,
            DeserializeRecord(result.Value.Value));
    }

    private static NatsOutboxRecord DeserializeRecord(byte[] bytes)
    {
        var record = JsonSerializer.Deserialize<NatsOutboxRecord>(bytes);
        if (record is not null && !string.IsNullOrWhiteSpace(record.MessageId))
        {
            NormalizeRecord(record);
            return record;
        }

        var legacyMessage = JsonSerializer.Deserialize<OutboxMessage>(bytes)
            ?? throw new InvalidOperationException("NATS outbox entry could not be deserialized into a supported dispatch-store shape.");

        return NatsOutboxRecord.Create(legacyMessage);
    }

    private static void NormalizeRecord(NatsOutboxRecord record)
    {
        record.Headers ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        record.Metadata ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        record.Headers = new Dictionary<string, string>(record.Headers, StringComparer.OrdinalIgnoreCase);
        record.Metadata = new Dictionary<string, string>(record.Metadata, StringComparer.OrdinalIgnoreCase);

        if (record.CreatedAtUtc == default)
        {
            record.CreatedAtUtc = record.OccurredAtUtc == default
                ? DateTimeOffset.UtcNow
                : record.OccurredAtUtc;
        }

        if (record.OccurredAtUtc == default)
        {
            record.OccurredAtUtc = record.CreatedAtUtc;
        }
    }

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
            _ => throw new InvalidOperationException($"Dispatch outcome '{outcome}' is not supported by the NATS event dispatch store.")
        };
    }

    private static DateTimeOffset? TryGetNextAttemptAtUtc(IReadOnlyDictionary<string, string> metadata)
    {
        if (!metadata.TryGetValue("nextRetryAtUtc", out var rawValue) || string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        return DateTimeOffset.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedValue)
            ? parsedValue
            : null;
    }

    private readonly record struct PendingDispatchCandidate(EventDispatchItem Item, DateTimeOffset SortAtUtc);

    private readonly record struct NatsOutboxEntrySnapshot(ulong Revision, NatsOutboxRecord Record);
}
