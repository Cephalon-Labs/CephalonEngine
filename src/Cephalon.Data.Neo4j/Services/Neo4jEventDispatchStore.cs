using System.Globalization;
using System.Text.Json;
using Cephalon.Data.Neo4j.Configuration;
using Cephalon.Eventing.Services;
using Neo4j.Driver;

namespace Cephalon.Data.Neo4j.Services;

internal sealed class Neo4jEventDispatchStore : IEventDispatchStore
{
    private const string OutboxId = "neo4j-outbox";

    private readonly IDriver _driver;
    private readonly Neo4jDataOptions _options;
    private volatile bool _constraintCreated;

    public Neo4jEventDispatchStore(IDriver driver, Neo4jDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(options);
        _driver = driver;
        _options = options;
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

        await EnsureConstraintAsync().ConfigureAwait(false);

        var label = $"{_options.LabelPrefix}OutboxMessage";
        var nowUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);

        await using var session = _driver.AsyncSession();
        var records = await session.ExecuteReadAsync(async tx =>
        {
            var result = await tx.RunAsync($@"
                MATCH (m:{label})
                WHERE (m.dispatchedAtUtc IS NULL OR m.dispatchedAtUtc = '')
                  AND (m.nextAttemptAtUtc IS NULL OR m.nextAttemptAtUtc = '' OR m.nextAttemptAtUtc <= $nowUtc)
                RETURN
                    m.messageId AS messageId,
                    m.channelId AS channelId,
                    m.messageType AS messageType,
                    m.payload AS payload,
                    m.occurredAtUtc AS occurredAtUtc,
                    m.createdAtUtc AS createdAtUtc,
                    m.dispatchAttemptCount AS dispatchAttemptCount,
                    m.contentType AS contentType,
                    m.correlationId AS correlationId,
                    m.tenantId AS tenantId,
                    m.headersJson AS headersJson,
                    m.metadataJson AS metadataJson
                ORDER BY
                    CASE
                        WHEN m.nextAttemptAtUtc IS NULL OR m.nextAttemptAtUtc = '' THEN m.createdAtUtc
                        ELSE m.nextAttemptAtUtc
                    END ASC,
                    m.createdAtUtc ASC
                LIMIT $maximumCount",
                new
                {
                    nowUtc,
                    maximumCount
                }).ConfigureAwait(false);

            return await result.ToListAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);

        return records.Select(CreateDispatchItem).ToArray();
    }

    public async ValueTask ApplyReportAsync(
        EventDispatchExecutionReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (!string.Equals(report.OutboxId, OutboxId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Outbox '{report.OutboxId}' is not owned by the active Neo4j event dispatch store.");
        }

        if (string.IsNullOrWhiteSpace(report.MessageId))
        {
            throw new InvalidOperationException("Dispatch reports must include a message id when applied to the durable Neo4j event dispatch store.");
        }

        await EnsureConstraintAsync().ConfigureAwait(false);

        var label = $"{_options.LabelPrefix}OutboxMessage";
        var normalizedOutcome = NormalizeOutcome(report.Outcome);
        var observedAtUtc = report.ObservedAtUtc == default
            ? DateTimeOffset.UtcNow
            : report.ObservedAtUtc;

        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx =>
        {
            var getResult = await tx.RunAsync($@"
                MATCH (m:{label} {{messageId: $messageId}})
                RETURN
                    m.channelId AS channelId,
                    m.dispatchAttemptCount AS dispatchAttemptCount",
                new
                {
                    messageId = report.MessageId
                }).ConfigureAwait(false);

            var records = await getResult.ToListAsync().ConfigureAwait(false);
            if (records.Count == 0)
            {
                throw new InvalidOperationException($"Outbox message '{report.MessageId}' is not staged in the active Neo4j outbox.");
            }

            var record = records[0];
            var stagedChannelId = record["channelId"].As<string>();
            if (!string.Equals(stagedChannelId, report.ChannelId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Outbox message '{report.MessageId}' belongs to channel '{stagedChannelId}', but the dispatch report referenced channel '{report.ChannelId}'.");
            }

            var dispatchAttemptCount = Math.Max(record["dispatchAttemptCount"].As<int>(), report.Attempt);
            var dispatchedAtUtc = normalizedOutcome is EventDispatchExecutionOutcomes.Succeeded or EventDispatchExecutionOutcomes.Skipped
                ? observedAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)
                : null;
            var nextAttemptAtUtc = normalizedOutcome is EventDispatchExecutionOutcomes.Failed or EventDispatchExecutionOutcomes.RetryScheduled
                ? TryGetNextAttemptAtUtc(report.Metadata)?.ToString("O", CultureInfo.InvariantCulture)
                : null;

            var updateResult = await tx.RunAsync($@"
                MATCH (m:{label} {{messageId: $messageId}})
                SET
                    m.dispatchAttemptCount = $dispatchAttemptCount,
                    m.dispatchedAtUtc = $dispatchedAtUtc,
                    m.nextAttemptAtUtc = $nextAttemptAtUtc
                RETURN m.messageId AS id",
                new
                {
                    messageId = report.MessageId,
                    dispatchAttemptCount,
                    dispatchedAtUtc,
                    nextAttemptAtUtc
                }).ConfigureAwait(false);

            _ = await updateResult.ToListAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    private static EventDispatchItem CreateDispatchItem(IRecord record)
    {
        return new EventDispatchItem(
            outboxId: OutboxId,
            messageId: record["messageId"].As<string>(),
            channelId: record["channelId"].As<string>(),
            eventType: record["messageType"].As<string>(),
            payload: record["payload"].As<string>(),
            occurredAtUtc: ParseDateTimeOffset(record["occurredAtUtc"].As<string>(), "occurredAtUtc"),
            createdAtUtc: ParseDateTimeOffset(record["createdAtUtc"].As<string>(), "createdAtUtc"),
            dispatchAttemptCount: record["dispatchAttemptCount"].As<int>(),
            contentType: NullIfWhitespace(record["contentType"].As<string>()),
            correlationId: NullIfWhitespace(record["correlationId"].As<string>()),
            tenantId: NullIfWhitespace(record["tenantId"].As<string>()),
            headers: DeserializeDictionary(record["headersJson"].As<string>()),
            metadata: DeserializeDictionary(record["metadataJson"].As<string>()));
    }

    private async Task EnsureConstraintAsync()
    {
        if (_constraintCreated)
        {
            return;
        }

        var label = $"{_options.LabelPrefix}OutboxMessage";
        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx =>
        {
            var result = await tx.RunAsync($@"
                CREATE CONSTRAINT {label.ToLowerInvariant()}_message_id IF NOT EXISTS
                FOR (m:{label}) REQUIRE m.messageId IS UNIQUE").ConfigureAwait(false);
            _ = await result.ToListAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);

        _constraintCreated = true;
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
            _ => throw new InvalidOperationException($"Dispatch outcome '{outcome}' is not supported by the Neo4j event dispatch store.")
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

    private static DateTimeOffset ParseDateTimeOffset(string value, string fieldName)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedValue))
        {
            throw new InvalidOperationException($"Neo4j outbox property '{fieldName}' must contain an ISO-8601 timestamp.");
        }

        return parsedValue;
    }

    private static string? NullIfWhitespace(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value;

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
