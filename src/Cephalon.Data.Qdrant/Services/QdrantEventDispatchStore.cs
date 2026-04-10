using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cephalon.Eventing.Services;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Cephalon.Data.Qdrant.Services;

internal sealed class QdrantEventDispatchStore : IEventDispatchStore, IDisposable
{
    private const string OutboxId = "qdrant-outbox";

    private readonly QdrantClient _client;
    private readonly string _collectionName;
    private volatile bool _collectionEnsured;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public QdrantEventDispatchStore(QdrantClient client, string collectionName)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        _client = client;
        _collectionName = collectionName;
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

        await EnsureCollectionAsync(cancellationToken).ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        var candidates = new List<PendingDispatchCandidate>();
        PointId? offset = null;

        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            var scrollResult = await _client.ScrollAsync(
                    _collectionName,
                    limit: 256,
                    offset: offset,
                    payloadSelector: true,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            foreach (var point in scrollResult.Result)
            {
                if (TryCreateDispatchCandidate(point, now, out var candidate))
                {
                    candidates.Add(candidate);
                }
            }

            offset = scrollResult.NextPageOffset;
        }
        while (offset is not null);

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
            throw new InvalidOperationException($"Outbox '{report.OutboxId}' is not owned by the active Qdrant event dispatch store.");
        }

        if (string.IsNullOrWhiteSpace(report.MessageId))
        {
            throw new InvalidOperationException("Dispatch reports must include a message id when applied to the durable Qdrant event dispatch store.");
        }

        await EnsureCollectionAsync(cancellationToken).ConfigureAwait(false);

        var pointId = TryGetPointId(report.MessageId);
        var points = await _client.RetrieveAsync(_collectionName, [pointId], cancellationToken: cancellationToken).ConfigureAwait(false);
        if (points.Count == 0)
        {
            throw new InvalidOperationException($"Outbox message '{report.MessageId}' is not staged in the active Qdrant outbox.");
        }

        var point = points[0];
        var stagedChannelId = GetRequiredStringPayload(point, "channel_id");
        if (!string.Equals(stagedChannelId, report.ChannelId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Outbox message '{report.MessageId}' belongs to channel '{stagedChannelId}', but the dispatch report referenced channel '{report.ChannelId}'.");
        }

        var normalizedOutcome = NormalizeOutcome(report.Outcome);
        var observedAtUtc = report.ObservedAtUtc == default
            ? DateTimeOffset.UtcNow
            : report.ObservedAtUtc;
        var updatedPoint = ClonePoint(point);

        updatedPoint.Payload["dispatch_attempt_count"] = Math.Max(GetIntPayload(point, "dispatch_attempt_count"), report.Attempt);

        switch (normalizedOutcome)
        {
            case EventDispatchExecutionOutcomes.Started:
                updatedPoint.Payload.Remove("dispatched_at_utc");
                updatedPoint.Payload.Remove("next_attempt_at_utc");
                break;

            case EventDispatchExecutionOutcomes.Succeeded:
            case EventDispatchExecutionOutcomes.Skipped:
                updatedPoint.Payload["dispatched_at_utc"] = observedAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
                updatedPoint.Payload.Remove("next_attempt_at_utc");
                break;

            case EventDispatchExecutionOutcomes.Failed:
            case EventDispatchExecutionOutcomes.RetryScheduled:
                updatedPoint.Payload.Remove("dispatched_at_utc");
                var nextAttemptAtUtc = TryGetNextAttemptAtUtc(report.Metadata);
                if (nextAttemptAtUtc is null)
                {
                    updatedPoint.Payload.Remove("next_attempt_at_utc");
                }
                else
                {
                    updatedPoint.Payload["next_attempt_at_utc"] = nextAttemptAtUtc.Value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
                }

                break;
        }

        await _client.UpsertAsync(_collectionName, [updatedPoint], cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _initLock.Dispose();
    }

    private async Task EnsureCollectionAsync(CancellationToken cancellationToken)
    {
        if (_collectionEnsured)
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_collectionEnsured)
            {
                return;
            }

            var exists = await _client.CollectionExistsAsync(_collectionName, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                await _client.CreateCollectionAsync(
                    _collectionName,
                    new VectorParams { Size = 1, Distance = Distance.Cosine },
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            await _client.CreatePayloadIndexAsync(
                _collectionName,
                "message_id",
                PayloadSchemaType.Keyword,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            _collectionEnsured = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static bool TryCreateDispatchCandidate(
        RetrievedPoint point,
        DateTimeOffset now,
        out PendingDispatchCandidate candidate)
    {
        if (HasPayloadValue(point, "dispatched_at_utc"))
        {
            candidate = default;
            return false;
        }

        var nextAttemptAtUtc = TryGetDateTimePayload(point, "next_attempt_at_utc");
        if (nextAttemptAtUtc is not null && nextAttemptAtUtc > now)
        {
            candidate = default;
            return false;
        }

        var item = new EventDispatchItem(
            outboxId: OutboxId,
            messageId: GetRequiredStringPayload(point, "message_id"),
            channelId: GetRequiredStringPayload(point, "channel_id"),
            eventType: GetRequiredStringPayload(point, "message_type"),
            payload: GetRequiredStringPayload(point, "payload"),
            occurredAtUtc: GetRequiredDateTimePayload(point, "occurred_at_utc"),
            createdAtUtc: GetRequiredDateTimePayload(point, "created_at_utc"),
            dispatchAttemptCount: GetIntPayload(point, "dispatch_attempt_count"),
            contentType: GetOptionalStringPayload(point, "content_type"),
            correlationId: GetOptionalStringPayload(point, "correlation_id"),
            tenantId: GetOptionalStringPayload(point, "tenant_id"),
            headers: DeserializeDictionary(GetOptionalStringPayload(point, "headers_json")),
            metadata: DeserializeDictionary(GetOptionalStringPayload(point, "metadata_json")));

        candidate = new PendingDispatchCandidate(item, nextAttemptAtUtc ?? item.CreatedAtUtc);
        return true;
    }

    private static PointStruct ClonePoint(RetrievedPoint point)
    {
        var cloned = new PointStruct
        {
            Id = point.Id,
            Vectors = new float[] { 0.0f }
        };

        foreach (var payloadEntry in point.Payload)
        {
            cloned.Payload[payloadEntry.Key] = payloadEntry.Value;
        }

        return cloned;
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
            _ => throw new InvalidOperationException($"Dispatch outcome '{outcome}' is not supported by the Qdrant event dispatch store.")
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

    private static Guid TryGetPointId(string messageId) =>
        Guid.TryParse(messageId, out var parsed)
            ? parsed
            : DeriveGuid(messageId);

    private static string GetRequiredStringPayload(RetrievedPoint point, string key)
    {
        var value = GetOptionalStringPayload(point, key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Qdrant outbox payload field '{key}' is required.");
        }

        return value;
    }

    private static string? GetOptionalStringPayload(RetrievedPoint point, string key) =>
        HasPayloadValue(point, key) && point.Payload.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value.StringValue)
            ? value.StringValue
            : null;

    private static int GetIntPayload(RetrievedPoint point, string key) =>
        point.Payload.TryGetValue(key, out var value)
            ? Convert.ToInt32(value.IntegerValue, CultureInfo.InvariantCulture)
            : 0;

    private static DateTimeOffset GetRequiredDateTimePayload(RetrievedPoint point, string key)
    {
        var value = TryGetDateTimePayload(point, key);
        if (value is null)
        {
            throw new InvalidOperationException($"Qdrant outbox payload field '{key}' must contain an ISO-8601 timestamp.");
        }

        return value.Value;
    }

    private static DateTimeOffset? TryGetDateTimePayload(RetrievedPoint point, string key)
    {
        var value = GetOptionalStringPayload(point, key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedValue)
            ? parsedValue
            : null;
    }

    private static bool HasPayloadValue(RetrievedPoint point, string key) =>
        point.Payload.TryGetValue(key, out var value) &&
        (!string.IsNullOrWhiteSpace(value.StringValue) ||
         value.IntegerValue != 0 ||
         value.KindCase != Value.KindOneofCase.None);

    private static Dictionary<string, string> DeserializeDictionary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var result = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        return result is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(result, StringComparer.OrdinalIgnoreCase);
    }

    private static Guid DeriveGuid(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(hash[..16]);
    }

    private readonly record struct PendingDispatchCandidate(EventDispatchItem Item, DateTimeOffset SortAtUtc);
}
