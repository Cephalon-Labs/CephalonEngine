using System.Text.Json;
using Cephalon.Data.EntityFramework.Modeling;
using Cephalon.Eventing.Services;
using Microsoft.EntityFrameworkCore;

namespace Cephalon.Data.EntityFramework.Services;

internal sealed class EntityFrameworkEventDispatchStore(
    DbContext dbContext,
    IEntityFrameworkOutboxContext outboxContext) : IEventDispatchStore, IEventDispatchProviderContextPersistenceStore
{
    private static readonly string[] ProviderContextPersistenceMetadataKeys =
    [
        EventDispatchRuntimeMetadataKeys.DurableDispatchContextPropagation,
        EventDispatchRuntimeMetadataKeys.DispatchContextMetadata,
        EventDispatchRuntimeMetadataKeys.DispatchContextHeaderCount,
        EventDispatchRuntimeMetadataKeys.DispatchContextMetadataCount,
        EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaders,
        EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaderProjection,
        EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaderCount,
        EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaderNames,
        EventDispatchRuntimeMetadataKeys.ProviderSideContextPersistence,
        EventDispatchRuntimeMetadataKeys.ProviderSideContextPersistenceSource,
        EventDispatchRuntimeMetadataKeys.ProviderSideContextPersistenceHeaderCount,
        EventDispatchRuntimeMetadataKeys.ProviderSideContextPersistenceHeaderNames,
        EventDispatchRuntimeMetadataKeys.CrossNodeContextHandoff
    ];

    public IReadOnlyList<string> OutboxIds => [EntityFrameworkDataRuntimeIds.OutboxId];

    public async ValueTask<IReadOnlyList<EventDispatchItem>> ReadPendingAsync(
        int maximumCount,
        CancellationToken cancellationToken = default)
    {
        if (maximumCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount), maximumCount, "Maximum count must be greater than or equal to 1.");
        }

        var now = DateTimeOffset.UtcNow;
        var entries = await outboxContext.OutboxMessages
            .AsNoTracking()
            .Where(static entry => entry.DispatchedAtUtc == null)
            .Where(entry => entry.NextAttemptAtUtc == null || entry.NextAttemptAtUtc <= now)
            .OrderBy(entry => entry.NextAttemptAtUtc ?? entry.CreatedAtUtc)
            .ThenBy(entry => entry.CreatedAtUtc)
            .Take(maximumCount)
            .ToListAsync(cancellationToken);

        return entries.Select(CreateDispatchItem).ToArray();
    }

    public async ValueTask ApplyReportAsync(
        EventDispatchExecutionReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (!string.Equals(report.OutboxId, EntityFrameworkDataRuntimeIds.OutboxId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Outbox '{report.OutboxId}' is not owned by the active Entity Framework event dispatch store.");
        }

        if (string.IsNullOrWhiteSpace(report.MessageId))
        {
            throw new InvalidOperationException("Dispatch reports must include a message id when applied to the durable Entity Framework event dispatch store.");
        }

        var entry = await outboxContext.OutboxMessages
            .SingleOrDefaultAsync(
                candidate => candidate.Id == report.MessageId,
                cancellationToken);

        if (entry is null)
        {
            throw new InvalidOperationException(
                $"Outbox message '{report.MessageId}' is not staged in the active Entity Framework outbox.");
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
                entry.DispatchedAtUtc = observedAtUtc;
                entry.NextAttemptAtUtc = null;
                break;

            case EventDispatchExecutionOutcomes.Failed:
                entry.DispatchedAtUtc = EventDispatchRuntimeMetadataKeys.IsTerminalFailure(report.Metadata)
                    ? observedAtUtc
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

        PersistProviderContextMetadata(entry, report);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public EventDispatchExecutionReport CreatePersistedContextReport(EventDispatchExecutionReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return EventDispatchProviderContextPersistenceMetadata.CreateReport(report, EntityFrameworkDataRuntimeIds.OutboxId);
    }

    private static EventDispatchItem CreateDispatchItem(EntityFrameworkOutboxEntry entry)
    {
        return new EventDispatchItem(
            outboxId: EntityFrameworkDataRuntimeIds.OutboxId,
            messageId: entry.Id,
            channelId: entry.ChannelId,
            eventType: entry.MessageType,
            payload: entry.Payload,
            occurredAtUtc: entry.OccurredAtUtc,
            createdAtUtc: entry.CreatedAtUtc,
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
            _ => throw new InvalidOperationException(
                $"Dispatch outcome '{outcome}' is not supported by the Entity Framework event dispatch store.")
        };
    }

    private static DateTimeOffset? TryGetNextAttemptAtUtc(IReadOnlyDictionary<string, string> metadata)
    {
        if (!metadata.TryGetValue(EventDispatchRuntimeMetadataKeys.NextRetryAtUtc, out var rawValue) || string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        return DateTimeOffset.TryParse(rawValue, out var parsedValue)
            ? parsedValue
            : null;
    }

    private static void PersistProviderContextMetadata(EntityFrameworkOutboxEntry entry, EventDispatchExecutionReport report)
    {
        var persistedMetadata = EventDispatchProviderContextPersistenceMetadata.CreateMetadata(
            report.Metadata,
            EntityFrameworkDataRuntimeIds.OutboxId);

        if (!EventDispatchProviderContextPersistenceMetadata.IsPersisted(persistedMetadata))
        {
            return;
        }

        var entryMetadata = DeserializeDictionary(entry.MetadataJson);
        foreach (var key in ProviderContextPersistenceMetadataKeys)
        {
            if (persistedMetadata.TryGetValue(key, out var value))
            {
                entryMetadata[key] = value;
            }
        }

        entry.MetadataJson = JsonSerializer.Serialize(entryMetadata);
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
