using System.Text.Json;
using Cephalon.Eventing.Services;

namespace Cephalon.Data.OpenSearch.Services;

internal sealed class OpenSearchEventDispatchStore(
    global::OpenSearch.Client.OpenSearchClient client,
    string indexName) : IEventDispatchStore
{
    private const string OutboxId = "opensearch-outbox";

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
        var response = await client.SearchAsync<OpenSearchOutboxEntry>(search => search
                .Index(indexName)
                .Size(Math.Min(maximumCount * 4, 256))
                .Query(query => query.Bool(boolean => boolean.MustNot(
                    mustNot => mustNot.Exists(exists => exists.Field("dispatched_at_utc")))))
                .Sort(sort => sort.Ascending("created_at_utc")),
            cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsValid)
        {
            throw new InvalidOperationException($"OpenSearch outbox dispatch-store read failed for index '{indexName}': {response.DebugInformation}");
        }

        return response.Hits
            .Select(static hit => hit.Source)
            .Where(static entry => entry is not null)
            .Where(entry => entry!.NextAttemptAtUtc is null || entry.NextAttemptAtUtc <= now)
            .Take(maximumCount)
            .Select(static entry => CreateDispatchItem(entry!))
            .ToArray();
    }

    public async ValueTask ApplyReportAsync(
        EventDispatchExecutionReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (!string.Equals(report.OutboxId, OutboxId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Outbox '{report.OutboxId}' is not owned by the active OpenSearch event dispatch store.");
        }

        if (string.IsNullOrWhiteSpace(report.MessageId))
        {
            throw new InvalidOperationException("Dispatch reports must include a message id when applied to the durable OpenSearch event dispatch store.");
        }

        var getResponse = await client.GetAsync<OpenSearchOutboxEntry>(
                new global::OpenSearch.Client.DocumentPath<OpenSearchOutboxEntry>(report.MessageId),
                descriptor => descriptor.Index(indexName),
                cancellationToken)
            .ConfigureAwait(false);

        if (!getResponse.IsValid || !getResponse.Found || getResponse.Source is null)
        {
            throw new InvalidOperationException($"Outbox message '{report.MessageId}' is not staged in the active OpenSearch outbox.");
        }

        var entry = getResponse.Source;
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

        var indexResponse = await client.IndexAsync(
                entry,
                descriptor => descriptor.Index(indexName).Id(report.MessageId),
                cancellationToken)
            .ConfigureAwait(false);

        if (!indexResponse.IsValid)
        {
            throw new InvalidOperationException(
                $"OpenSearch outbox dispatch-store update failed for message '{report.MessageId}': {indexResponse.DebugInformation}");
        }
    }

    private static EventDispatchItem CreateDispatchItem(OpenSearchOutboxEntry entry)
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
            _ => throw new InvalidOperationException($"Dispatch outcome '{outcome}' is not supported by the OpenSearch event dispatch store.")
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
