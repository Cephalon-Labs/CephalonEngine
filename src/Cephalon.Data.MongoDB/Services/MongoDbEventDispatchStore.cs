using System.Text.Json;
using Cephalon.Eventing.Services;
using MongoDB.Driver;

namespace Cephalon.Data.MongoDB.Services;

internal sealed class MongoDbEventDispatchStore(
    IMongoCollection<MongoDbOutboxEntry> collection) : IEventDispatchStore
{
    private const string OutboxId = "mongodb-outbox";

    public IReadOnlyList<string> OutboxIds => [OutboxId];

    public async ValueTask<IReadOnlyList<EventDispatchItem>> ReadPendingAsync(
        int maximumCount,
        CancellationToken cancellationToken = default)
    {
        if (maximumCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount), maximumCount, "Maximum count must be greater than or equal to 1.");
        }

        var now = DateTime.UtcNow;
        var filter = Builders<MongoDbOutboxEntry>.Filter.Eq(static entry => entry.DispatchedAtUtc, null) &
            (Builders<MongoDbOutboxEntry>.Filter.Eq(static entry => entry.NextAttemptAtUtc, null) |
             Builders<MongoDbOutboxEntry>.Filter.Lte(static entry => entry.NextAttemptAtUtc, now));

        var entries = await collection.Find(filter)
            .SortBy(static entry => entry.NextAttemptAtUtc)
            .ThenBy(static entry => entry.CreatedAtUtc)
            .Limit(maximumCount)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entries.Select(CreateDispatchItem).ToArray();
    }

    public async ValueTask ApplyReportAsync(
        EventDispatchExecutionReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (!string.Equals(report.OutboxId, OutboxId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Outbox '{report.OutboxId}' is not owned by the active MongoDB event dispatch store.");
        }

        if (string.IsNullOrWhiteSpace(report.MessageId))
        {
            throw new InvalidOperationException("Dispatch reports must include a message id when applied to the durable MongoDB event dispatch store.");
        }

        var entry = await collection.Find(candidate => candidate.MessageId == report.MessageId)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entry is null)
        {
            throw new InvalidOperationException($"Outbox message '{report.MessageId}' is not staged in the active MongoDB outbox.");
        }

        if (!string.Equals(entry.ChannelId, report.ChannelId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Outbox message '{report.MessageId}' belongs to channel '{entry.ChannelId}', but the dispatch report referenced channel '{report.ChannelId}'.");
        }

        var observedAtUtc = report.ObservedAtUtc == default
            ? DateTimeOffset.UtcNow
            : report.ObservedAtUtc;

        entry.DispatchAttemptCount = Math.Max(entry.DispatchAttemptCount, report.Attempt);

        switch (NormalizeOutcome(report.Outcome))
        {
            case EventDispatchExecutionOutcomes.Started:
                entry.NextAttemptAtUtc = null;
                break;

            case EventDispatchExecutionOutcomes.Succeeded:
            case EventDispatchExecutionOutcomes.Skipped:
                entry.DispatchedAtUtc = observedAtUtc.UtcDateTime;
                entry.NextAttemptAtUtc = null;
                break;

            case EventDispatchExecutionOutcomes.Failed:
                entry.DispatchedAtUtc = EventDispatchRuntimeMetadataKeys.IsTerminalFailure(report.Metadata)
                    ? observedAtUtc.UtcDateTime
                    : null;
                entry.NextAttemptAtUtc = EventDispatchRuntimeMetadataKeys.IsTerminalFailure(report.Metadata)
                    ? null
                    : TryGetNextAttemptAtUtc(report.Metadata);
                break;

            case EventDispatchExecutionOutcomes.RetryScheduled:
                entry.DispatchedAtUtc = null;
                entry.NextAttemptAtUtc = TryGetNextAttemptAtUtc(report.Metadata);
                break;
        }

        _ = await collection.ReplaceOneAsync(
                candidate => candidate.MessageId == report.MessageId,
                entry,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private static EventDispatchItem CreateDispatchItem(MongoDbOutboxEntry entry)
    {
        return new EventDispatchItem(
            outboxId: OutboxId,
            messageId: entry.MessageId,
            channelId: entry.ChannelId,
            eventType: entry.MessageType,
            payload: entry.Payload,
            occurredAtUtc: new DateTimeOffset(entry.OccurredAtUtc, TimeSpan.Zero),
            createdAtUtc: new DateTimeOffset(entry.CreatedAtUtc, TimeSpan.Zero),
            dispatchAttemptCount: entry.DispatchAttemptCount,
            contentType: entry.ContentType,
            correlationId: entry.CorrelationId,
            tenantId: entry.TenantId,
            headers: DeserializeDictionary(entry.HeadersJson),
            metadata: DeserializeDictionary(entry.MetadataJson));
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
            _ => throw new InvalidOperationException($"Dispatch outcome '{outcome}' is not supported by the MongoDB event dispatch store.")
        };
    }

    private static DateTime? TryGetNextAttemptAtUtc(IReadOnlyDictionary<string, string> metadata)
    {
        if (!metadata.TryGetValue(EventDispatchRuntimeMetadataKeys.NextRetryAtUtc, out var rawValue) || string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        return DateTimeOffset.TryParse(rawValue, out var parsedValue)
            ? parsedValue.UtcDateTime
            : null;
    }

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
