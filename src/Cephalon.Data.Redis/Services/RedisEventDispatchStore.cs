using System.Globalization;
using System.Text.Json;
using Cephalon.Data.Redis.Configuration;
using Cephalon.Eventing.Services;
using StackExchange.Redis;

namespace Cephalon.Data.Redis.Services;

internal sealed class RedisEventDispatchStore(
    IConnectionMultiplexer multiplexer,
    RedisDataOptions options) : IEventDispatchStore
{
    private const string OutboxId = "redis-outbox";

    public IReadOnlyList<string> OutboxIds => [OutboxId];

    public async ValueTask<IReadOnlyList<EventDispatchItem>> ReadPendingAsync(
        int maximumCount,
        CancellationToken cancellationToken = default)
    {
        if (maximumCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount), maximumCount, "Maximum count must be greater than or equal to 1.");
        }

        var db = multiplexer.GetDatabase();
        var nowScore = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var messageIds = await db.SortedSetRangeByScoreAsync(
                PendingSetKey(),
                stop: nowScore,
                order: Order.Ascending,
                take: maximumCount)
            .ConfigureAwait(false);

        if (messageIds.Length == 0)
        {
            return [];
        }

        var items = new List<EventDispatchItem>(messageIds.Length);
        foreach (var messageId in messageIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (messageId.IsNullOrEmpty)
            {
                continue;
            }

            var hashEntries = await db.HashGetAllAsync(OutboxHashKey(messageId!)).ConfigureAwait(false);
            if (hashEntries.Length == 0)
            {
                continue;
            }

            items.Add(CreateDispatchItem(hashEntries));
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
            throw new InvalidOperationException($"Outbox '{report.OutboxId}' is not owned by the active Redis event dispatch store.");
        }

        if (string.IsNullOrWhiteSpace(report.MessageId))
        {
            throw new InvalidOperationException("Dispatch reports must include a message id when applied to the durable Redis event dispatch store.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var db = multiplexer.GetDatabase();
        var hashKey = OutboxHashKey(report.MessageId);
        var hashEntries = await db.HashGetAllAsync(hashKey).ConfigureAwait(false);
        if (hashEntries.Length == 0)
        {
            throw new InvalidOperationException($"Outbox message '{report.MessageId}' is not staged in the active Redis outbox.");
        }

        var values = hashEntries.ToDictionary(static entry => entry.Name.ToString(), static entry => entry.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        var stagedChannelId = values.TryGetValue("ChannelId", out var channelId)
            ? channelId
            : string.Empty;

        if (!string.Equals(stagedChannelId, report.ChannelId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Outbox message '{report.MessageId}' belongs to channel '{stagedChannelId}', but the dispatch report referenced channel '{report.ChannelId}'.");
        }

        var observedAtUtc = report.ObservedAtUtc == default
            ? DateTimeOffset.UtcNow
            : report.ObservedAtUtc;

        var nextAttemptAtUtc = NormalizeOutcome(report.Outcome) switch
        {
            EventDispatchExecutionOutcomes.Failed => TryGetNextAttemptAtUtc(report.Metadata),
            EventDispatchExecutionOutcomes.RetryScheduled => TryGetNextAttemptAtUtc(report.Metadata),
            _ => null
        };

        var updates = new HashEntry[]
        {
            new("DispatchAttemptCount", Math.Max(ParseInt(values, "DispatchAttemptCount"), report.Attempt)),
            new("DispatchedAtUtc", ShouldMarkDispatched(report.Outcome) ? observedAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture) : string.Empty),
            new("NextAttemptAtUtc", nextAttemptAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty)
        };

        await db.HashSetAsync(hashKey, updates).ConfigureAwait(false);

        var pendingKey = PendingSetKey();
        switch (NormalizeOutcome(report.Outcome))
        {
            case EventDispatchExecutionOutcomes.Succeeded:
            case EventDispatchExecutionOutcomes.Skipped:
                _ = await db.SortedSetRemoveAsync(pendingKey, report.MessageId).ConfigureAwait(false);
                break;

            case EventDispatchExecutionOutcomes.Failed:
            case EventDispatchExecutionOutcomes.RetryScheduled:
                _ = await db.SortedSetAddAsync(
                        pendingKey,
                        report.MessageId,
                        (nextAttemptAtUtc ?? observedAtUtc).ToUnixTimeMilliseconds(),
                        SortedSetWhen.Always)
                    .ConfigureAwait(false);
                break;
        }
    }

    private static EventDispatchItem CreateDispatchItem(HashEntry[] entries)
    {
        var values = entries.ToDictionary(static entry => entry.Name.ToString(), static entry => entry.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        return new EventDispatchItem(
            outboxId: OutboxId,
            messageId: Required(values, "MessageId"),
            channelId: Required(values, "ChannelId"),
            eventType: Required(values, "MessageType"),
            payload: Required(values, "Payload"),
            occurredAtUtc: ParseDateTimeOffset(values, "OccurredAtUtc"),
            createdAtUtc: ParseDateTimeOffset(values, "CreatedAtUtc"),
            dispatchAttemptCount: ParseInt(values, "DispatchAttemptCount"),
            contentType: Optional(values, "ContentType"),
            correlationId: Optional(values, "CorrelationId"),
            tenantId: Optional(values, "TenantId"),
            headers: DeserializeDictionary(Optional(values, "HeadersJson")),
            metadata: DeserializeDictionary(Optional(values, "MetadataJson")));
    }

    private string OutboxHashKey(string messageId) =>
        $"{options.KeyPrefix}outbox:msg:{messageId}";

    private string PendingSetKey() =>
        $"{options.KeyPrefix}outbox:pending";

    private static bool ShouldMarkDispatched(string outcome) =>
        string.Equals(outcome, EventDispatchExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(outcome, EventDispatchExecutionOutcomes.Skipped, StringComparison.OrdinalIgnoreCase);

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
            _ => throw new InvalidOperationException($"Dispatch outcome '{outcome}' is not supported by the Redis event dispatch store.")
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

    private static DateTimeOffset ParseDateTimeOffset(Dictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var rawValue) ||
            !DateTimeOffset.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedValue))
        {
            throw new InvalidOperationException($"Redis outbox hash field '{key}' is required and must contain an ISO-8601 timestamp.");
        }

        return parsedValue;
    }

    private static int ParseInt(Dictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var rawValue) || !int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return 0;
        }

        return parsedValue;
    }

    private static string Required(Dictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var rawValue) || string.IsNullOrWhiteSpace(rawValue))
        {
            throw new InvalidOperationException($"Redis outbox hash field '{key}' is required.");
        }

        return rawValue;
    }

    private static string? Optional(Dictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var rawValue) && !string.IsNullOrWhiteSpace(rawValue)
            ? rawValue
            : null;

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
}
