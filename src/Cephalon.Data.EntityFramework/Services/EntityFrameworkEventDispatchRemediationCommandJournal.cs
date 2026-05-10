using System.Collections.ObjectModel;
using System.Text.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Data.EntityFramework.Modeling;
using Cephalon.Eventing.Services;
using Microsoft.EntityFrameworkCore;

namespace Cephalon.Data.EntityFramework.Services;

internal sealed class EntityFrameworkEventDispatchRemediationCommandJournal(
    DbContext dbContext,
    IEntityFrameworkEventDispatchRemediationCommandJournalContext journalContext) :
    IEventDispatchRemediationCommandJournal,
    IEventDispatchRemediationCommandReplayCursorCatalog
{
    private const string PendingDispatchOutcome = "pending";

    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    public EventDispatchRemediationCommandJournalDescriptor Descriptor { get; } = new(
        JournalId: "entity-framework.event-dispatch-remediation-command-journal",
        Provider: "Cephalon.Data.EntityFramework",
        Storage: "entity-framework-table",
        Durability: "durable",
        Scope: "cross-node",
        CrossNodeCommandAudit: true,
        DurableReplayCursor: true);

    public EventDispatchRemediationRuntimeSummary Summary => CreateSummary(States);

    public EventDispatchRemediationRuntimeRetention Retention
    {
        get
        {
            var states = States;
            if (states.Count == 0)
            {
                return EventDispatchRemediationRuntimeRetention.Empty;
            }

            var oldestState = GetOldestState(states)!;
            var latestState = states[0];
            return new EventDispatchRemediationRuntimeRetention(
                historyLimit: 0,
                retainedCommandCount: states.Count,
                totalRecordedCommandCount: states.Count,
                droppedCommandCount: 0,
                truncated: false,
                oldestRetainedCommandId: oldestState.CommandId,
                oldestRetainedObservedAtUtc: oldestState.ObservedAtUtc,
                latestRetainedCommandId: latestState.CommandId,
                latestRetainedObservedAtUtc: latestState.ObservedAtUtc);
        }
    }

    public EventDispatchRemediationRuntimeState? Latest
    {
        get
        {
            var states = States;
            return states.Count == 0 ? null : states[0];
        }
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> States => ReadStates(static entries => entries);

    public EventDispatchRemediationCommandReplayCursor? LatestReplayCursor
    {
        get
        {
            var entry = journalContext.EventDispatchRemediationCommandJournalEntries
                .AsNoTracking()
                .OrderByDescending(entry => entry.ObservedAtUtc)
                .ThenByDescending(entry => entry.CommandId)
                .FirstOrDefault();

            return entry is null ? null : CreateReplayCursor(entry);
        }
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetAfterReplayCursor(
        EventDispatchRemediationCommandReplayCursor? cursor,
        int maxCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCount);
        if (cursor is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cursor.CommandId);
        }

        var entries = journalContext.EventDispatchRemediationCommandJournalEntries
            .AsNoTracking()
            .Where(entry => cursor == null || entry.ObservedAtUtc >= cursor.ObservedAtUtc)
            .OrderBy(entry => entry.ObservedAtUtc)
            .ThenBy(entry => entry.CommandId)
            .AsEnumerable()
            .Where(entry => cursor is null || IsAfterReplayCursor(entry, cursor))
            .Take(maxCount)
            .ToArray();

        return entries.Select(CreateState).ToArray();
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetInDoubt()
    {
        return GetInDoubtBefore(null);
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetInDoubtBefore(DateTimeOffset? beforeObservedAtUtc)
    {
        return ReadStates(entries => entries
            .Where(entry => entry.Outcome == EventDispatchRemediationOutcomes.Reserved)
            .Where(entry => beforeObservedAtUtc == null || entry.ObservedAtUtc <= beforeObservedAtUtc.Value));
    }

    public EventDispatchRemediationRuntimeState? GetOldestInDoubtBefore(DateTimeOffset? beforeObservedAtUtc)
    {
        var entry = journalContext.EventDispatchRemediationCommandJournalEntries
            .AsNoTracking()
            .Where(entry => entry.Outcome == EventDispatchRemediationOutcomes.Reserved)
            .Where(entry => beforeObservedAtUtc == null || entry.ObservedAtUtc <= beforeObservedAtUtc.Value)
            .OrderBy(entry => entry.ObservedAtUtc)
            .ThenBy(entry => entry.CommandId)
            .FirstOrDefault();

        return entry is null ? null : CreateState(entry);
    }

    public EventDispatchRemediationRuntimeSummary GetInDoubtSummaryBefore(DateTimeOffset? beforeObservedAtUtc)
    {
        return CreateSummary(ReadStates(entries => entries
            .Where(entry => entry.Outcome == EventDispatchRemediationOutcomes.Reserved)
            .Where(entry => beforeObservedAtUtc == null || entry.ObservedAtUtc <= beforeObservedAtUtc.Value)));
    }

    public EventDispatchRemediationRuntimeState? GetByCommandId(string commandId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);
        var normalizedCommandId = commandId.Trim();
        var states = ReadStates(entries => entries.Where(entry => entry.CommandId == normalizedCommandId));
        return states.Length == 0 ? null : states[0];
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByObservedAt(
        DateTimeOffset? fromObservedAtUtc,
        DateTimeOffset? toObservedAtUtc)
    {
        ValidateObservationWindow(fromObservedAtUtc, toObservedAtUtc);

        return ReadStates(entries => entries
            .Where(entry => fromObservedAtUtc == null || entry.ObservedAtUtc >= fromObservedAtUtc.Value)
            .Where(entry => toObservedAtUtc == null || entry.ObservedAtUtc <= toObservedAtUtc.Value));
    }

    public EventDispatchRemediationRuntimeSummary GetSummaryByObservedAt(
        DateTimeOffset? fromObservedAtUtc,
        DateTimeOffset? toObservedAtUtc)
    {
        return CreateSummary(GetByObservedAt(fromObservedAtUtc, toObservedAtUtc));
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByOutboxId(string outboxId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxId);
        var normalizedOutboxId = outboxId.Trim();
        return ReadStates(entries => entries.Where(entry => entry.OutboxId == normalizedOutboxId));
    }

    public EventDispatchRemediationRuntimeSummary GetSummaryByOutboxId(string outboxId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxId);
        var normalizedOutboxId = outboxId.Trim();
        return CreateSummary(ReadStates(entries => entries.Where(entry => entry.OutboxId == normalizedOutboxId)));
    }

    public EventDispatchRemediationRuntimeRetention GetRetentionByOutboxId(string outboxId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxId);
        var normalizedOutboxId = outboxId.Trim();
        return ReadRetention(entries => entries.Where(entry => entry.OutboxId == normalizedOutboxId));
    }

    public EventDispatchRemediationRuntimeState? GetLatestByOutboxId(string outboxId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxId);
        var normalizedOutboxId = outboxId.Trim();
        return ReadLatestState(entries => entries.Where(entry => entry.OutboxId == normalizedOutboxId));
    }

    public EventDispatchRemediationRuntimeState? GetOldestByOutboxId(string outboxId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxId);
        var normalizedOutboxId = outboxId.Trim();
        return ReadOldestState(entries => entries.Where(entry => entry.OutboxId == normalizedOutboxId));
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByMessageId(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        var normalizedMessageId = messageId.Trim();
        return ReadStates(entries => entries.Where(entry => entry.MessageId == normalizedMessageId));
    }

    public EventDispatchRemediationRuntimeSummary GetSummaryByMessageId(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        var normalizedMessageId = messageId.Trim();
        return CreateSummary(ReadStates(entries => entries.Where(entry => entry.MessageId == normalizedMessageId)));
    }

    public EventDispatchRemediationRuntimeRetention GetRetentionByMessageId(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        var normalizedMessageId = messageId.Trim();
        return ReadRetention(entries => entries.Where(entry => entry.MessageId == normalizedMessageId));
    }

    public EventDispatchRemediationRuntimeState? GetLatestByMessageId(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        var normalizedMessageId = messageId.Trim();
        return ReadLatestState(entries => entries.Where(entry => entry.MessageId == normalizedMessageId));
    }

    public EventDispatchRemediationRuntimeState? GetOldestByMessageId(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        var normalizedMessageId = messageId.Trim();
        return ReadOldestState(entries => entries.Where(entry => entry.MessageId == normalizedMessageId));
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByChannelId(string channelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        var normalizedChannelId = channelId.Trim();
        return ReadStates(entries => entries.Where(entry => entry.ChannelId == normalizedChannelId));
    }

    public EventDispatchRemediationRuntimeSummary GetSummaryByChannelId(string channelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        var normalizedChannelId = channelId.Trim();
        return CreateSummary(ReadStates(entries => entries.Where(entry => entry.ChannelId == normalizedChannelId)));
    }

    public EventDispatchRemediationRuntimeRetention GetRetentionByChannelId(string channelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        var normalizedChannelId = channelId.Trim();
        return ReadRetention(entries => entries.Where(entry => entry.ChannelId == normalizedChannelId));
    }

    public EventDispatchRemediationRuntimeState? GetLatestByChannelId(string channelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        var normalizedChannelId = channelId.Trim();
        return ReadLatestState(entries => entries.Where(entry => entry.ChannelId == normalizedChannelId));
    }

    public EventDispatchRemediationRuntimeState? GetOldestByChannelId(string channelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        var normalizedChannelId = channelId.Trim();
        return ReadOldestState(entries => entries.Where(entry => entry.ChannelId == normalizedChannelId));
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();
        return ReadStates(entries => entries.Where(entry => entry.OperationId == normalizedOperationId));
    }

    public EventDispatchRemediationRuntimeSummary GetSummaryByOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();
        return CreateSummary(ReadStates(entries => entries.Where(entry => entry.OperationId == normalizedOperationId)));
    }

    public EventDispatchRemediationRuntimeRetention GetRetentionByOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();
        return ReadRetention(entries => entries.Where(entry => entry.OperationId == normalizedOperationId));
    }

    public EventDispatchRemediationRuntimeState? GetLatestByOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();
        return ReadLatestState(entries => entries.Where(entry => entry.OperationId == normalizedOperationId));
    }

    public EventDispatchRemediationRuntimeState? GetOldestByOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();
        return ReadOldestState(entries => entries.Where(entry => entry.OperationId == normalizedOperationId));
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByActorId(string actorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        var normalizedActorId = actorId.Trim();
        return ReadStates(entries => entries.Where(entry => entry.ActorId == normalizedActorId));
    }

    public EventDispatchRemediationRuntimeSummary GetSummaryByActorId(string actorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        var normalizedActorId = actorId.Trim();
        return CreateSummary(ReadStates(entries => entries.Where(entry => entry.ActorId == normalizedActorId)));
    }

    public EventDispatchRemediationRuntimeRetention GetRetentionByActorId(string actorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        var normalizedActorId = actorId.Trim();
        return ReadRetention(entries => entries.Where(entry => entry.ActorId == normalizedActorId));
    }

    public EventDispatchRemediationRuntimeState? GetLatestByActorId(string actorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        var normalizedActorId = actorId.Trim();
        return ReadLatestState(entries => entries.Where(entry => entry.ActorId == normalizedActorId));
    }

    public EventDispatchRemediationRuntimeState? GetOldestByActorId(string actorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        var normalizedActorId = actorId.Trim();
        return ReadOldestState(entries => entries.Where(entry => entry.ActorId == normalizedActorId));
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByCorrelationId(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        var normalizedCorrelationId = correlationId.Trim();
        return ReadStates(entries => entries.Where(entry => entry.CorrelationId == normalizedCorrelationId));
    }

    public EventDispatchRemediationRuntimeSummary GetSummaryByCorrelationId(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        var normalizedCorrelationId = correlationId.Trim();
        return CreateSummary(ReadStates(entries => entries.Where(entry => entry.CorrelationId == normalizedCorrelationId)));
    }

    public EventDispatchRemediationRuntimeRetention GetRetentionByCorrelationId(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        var normalizedCorrelationId = correlationId.Trim();
        return ReadRetention(entries => entries.Where(entry => entry.CorrelationId == normalizedCorrelationId));
    }

    public EventDispatchRemediationRuntimeState? GetLatestByCorrelationId(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        var normalizedCorrelationId = correlationId.Trim();
        return ReadLatestState(entries => entries.Where(entry => entry.CorrelationId == normalizedCorrelationId));
    }

    public EventDispatchRemediationRuntimeState? GetOldestByCorrelationId(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        var normalizedCorrelationId = correlationId.Trim();
        return ReadOldestState(entries => entries.Where(entry => entry.CorrelationId == normalizedCorrelationId));
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByReason(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var normalizedReason = reason.Trim();
        return ReadStates(entries => entries.Where(entry => entry.Reason == normalizedReason));
    }

    public EventDispatchRemediationRuntimeSummary GetSummaryByReason(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var normalizedReason = reason.Trim();
        return CreateSummary(ReadStates(entries => entries.Where(entry => entry.Reason == normalizedReason)));
    }

    public EventDispatchRemediationRuntimeRetention GetRetentionByReason(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var normalizedReason = reason.Trim();
        return ReadRetention(entries => entries.Where(entry => entry.Reason == normalizedReason));
    }

    public EventDispatchRemediationRuntimeState? GetLatestByReason(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var normalizedReason = reason.Trim();
        return ReadLatestState(entries => entries.Where(entry => entry.Reason == normalizedReason));
    }

    public EventDispatchRemediationRuntimeState? GetOldestByReason(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var normalizedReason = reason.Trim();
        return ReadOldestState(entries => entries.Where(entry => entry.Reason == normalizedReason));
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByOutcome(string outcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);
        var normalizedOutcome = outcome.Trim();
        return ReadStates(entries => entries.Where(entry => entry.Outcome == normalizedOutcome));
    }

    public EventDispatchRemediationRuntimeSummary GetSummaryByOutcome(string outcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);
        var normalizedOutcome = outcome.Trim();
        return CreateSummary(ReadStates(entries => entries.Where(entry => entry.Outcome == normalizedOutcome)));
    }

    public EventDispatchRemediationRuntimeRetention GetRetentionByOutcome(string outcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);
        var normalizedOutcome = outcome.Trim();
        return ReadRetention(entries => entries.Where(entry => entry.Outcome == normalizedOutcome));
    }

    public EventDispatchRemediationRuntimeState? GetLatestByOutcome(string outcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);
        var normalizedOutcome = outcome.Trim();
        return ReadLatestState(entries => entries.Where(entry => entry.Outcome == normalizedOutcome));
    }

    public EventDispatchRemediationRuntimeState? GetOldestByOutcome(string outcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);
        var normalizedOutcome = outcome.Trim();
        return ReadOldestState(entries => entries.Where(entry => entry.Outcome == normalizedOutcome));
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByDispatchOutcome(string dispatchOutcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dispatchOutcome);
        var normalizedDispatchOutcome = dispatchOutcome.Trim();
        return ReadStates(entries => entries.Where(entry => entry.DispatchOutcome == normalizedDispatchOutcome));
    }

    public EventDispatchRemediationRuntimeSummary GetSummaryByDispatchOutcome(string dispatchOutcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dispatchOutcome);
        var normalizedDispatchOutcome = dispatchOutcome.Trim();
        return CreateSummary(ReadStates(entries => entries.Where(entry => entry.DispatchOutcome == normalizedDispatchOutcome)));
    }

    public EventDispatchRemediationRuntimeRetention GetRetentionByDispatchOutcome(string dispatchOutcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dispatchOutcome);
        var normalizedDispatchOutcome = dispatchOutcome.Trim();
        return ReadRetention(entries => entries.Where(entry => entry.DispatchOutcome == normalizedDispatchOutcome));
    }

    public EventDispatchRemediationRuntimeState? GetLatestByDispatchOutcome(string dispatchOutcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dispatchOutcome);
        var normalizedDispatchOutcome = dispatchOutcome.Trim();
        return ReadLatestState(entries => entries.Where(entry => entry.DispatchOutcome == normalizedDispatchOutcome));
    }

    public EventDispatchRemediationRuntimeState? GetOldestByDispatchOutcome(string dispatchOutcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dispatchOutcome);
        var normalizedDispatchOutcome = dispatchOutcome.Trim();
        return ReadOldestState(entries => entries.Where(entry => entry.DispatchOutcome == normalizedDispatchOutcome));
    }

    public async ValueTask<EventDispatchRemediationCommandReservation> ReserveAsync(
        EventDispatchRemediationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedCommandId = request.CommandId.Trim();
        var existingState = await ReadStateByCommandIdAsync(normalizedCommandId, cancellationToken).ConfigureAwait(false);
        if (existingState is not null)
        {
            return CreateDuplicateReservation(normalizedCommandId, existingState);
        }

        var entry = CreateReservedEntry(request);
        journalContext.EventDispatchRemediationCommandJournalEntries.Add(entry);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(entry).State = EntityState.Detached;
            existingState = await ReadStateByCommandIdAsync(normalizedCommandId, cancellationToken).ConfigureAwait(false);
            if (existingState is not null)
            {
                return CreateDuplicateReservation(normalizedCommandId, existingState);
            }

            throw;
        }

        var reservedState = CreateState(entry);
        return new EventDispatchRemediationCommandReservation(
            normalizedCommandId,
            reserved: true,
            existingCommand: null,
            reservedState.Metadata);
    }

    public async ValueTask RecordAsync(
        EventDispatchRemediationResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedCommandId = result.CommandId.Trim();
        var existingEntry = await journalContext.EventDispatchRemediationCommandJournalEntries
            .SingleOrDefaultAsync(entry => entry.CommandId == normalizedCommandId, cancellationToken)
            .ConfigureAwait(false);

        if (existingEntry is null)
        {
            journalContext.EventDispatchRemediationCommandJournalEntries.Add(CreateEntry(result));
        }
        else
        {
            ApplyResult(existingEntry, result);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private EventDispatchRemediationRuntimeState[] ReadStates(
        Func<IQueryable<EntityFrameworkEventDispatchRemediationCommandEntry>, IQueryable<EntityFrameworkEventDispatchRemediationCommandEntry>> shape)
    {
        var entries = shape(journalContext.EventDispatchRemediationCommandJournalEntries.AsNoTracking())
            .OrderByDescending(entry => entry.ObservedAtUtc)
            .ThenBy(entry => entry.CommandId)
            .ToArray();

        return entries.Select(CreateState).ToArray();
    }

    private static EventDispatchRemediationCommandReplayCursor CreateReplayCursor(
        EntityFrameworkEventDispatchRemediationCommandEntry entry)
    {
        return new EventDispatchRemediationCommandReplayCursor(
            ObservedAtUtc: entry.ObservedAtUtc,
            CommandId: entry.CommandId);
    }

    private static bool IsAfterReplayCursor(
        EntityFrameworkEventDispatchRemediationCommandEntry entry,
        EventDispatchRemediationCommandReplayCursor cursor)
    {
        if (entry.ObservedAtUtc > cursor.ObservedAtUtc)
        {
            return true;
        }

        return entry.ObservedAtUtc == cursor.ObservedAtUtc &&
            string.CompareOrdinal(entry.CommandId, cursor.CommandId) > 0;
    }

    private EventDispatchRemediationRuntimeState? ReadLatestState(
        Func<IQueryable<EntityFrameworkEventDispatchRemediationCommandEntry>, IQueryable<EntityFrameworkEventDispatchRemediationCommandEntry>> shape)
    {
        var entry = shape(journalContext.EventDispatchRemediationCommandJournalEntries.AsNoTracking())
            .OrderByDescending(entry => entry.ObservedAtUtc)
            .ThenBy(entry => entry.CommandId)
            .FirstOrDefault();

        return entry is null ? null : CreateState(entry);
    }

    private EventDispatchRemediationRuntimeState? ReadOldestState(
        Func<IQueryable<EntityFrameworkEventDispatchRemediationCommandEntry>, IQueryable<EntityFrameworkEventDispatchRemediationCommandEntry>> shape)
    {
        var entry = shape(journalContext.EventDispatchRemediationCommandJournalEntries.AsNoTracking())
            .OrderBy(entry => entry.ObservedAtUtc)
            .ThenBy(entry => entry.CommandId)
            .FirstOrDefault();

        return entry is null ? null : CreateState(entry);
    }

    private EventDispatchRemediationRuntimeRetention ReadRetention(
        Func<IQueryable<EntityFrameworkEventDispatchRemediationCommandEntry>, IQueryable<EntityFrameworkEventDispatchRemediationCommandEntry>> shape)
    {
        var entries = shape(journalContext.EventDispatchRemediationCommandJournalEntries.AsNoTracking())
            .OrderByDescending(entry => entry.ObservedAtUtc)
            .ThenBy(entry => entry.CommandId)
            .ToArray();

        if (entries.Length == 0)
        {
            return EventDispatchRemediationRuntimeRetention.Empty;
        }

        var latestEntry = entries[0];
        var oldestEntry = entries
            .OrderBy(entry => entry.ObservedAtUtc)
            .ThenBy(entry => entry.CommandId)
            .First();

        return new EventDispatchRemediationRuntimeRetention(
            historyLimit: 0,
            retainedCommandCount: entries.Length,
            totalRecordedCommandCount: entries.Length,
            droppedCommandCount: 0,
            truncated: false,
            oldestRetainedCommandId: oldestEntry.CommandId,
            oldestRetainedObservedAtUtc: oldestEntry.ObservedAtUtc,
            latestRetainedCommandId: latestEntry.CommandId,
            latestRetainedObservedAtUtc: latestEntry.ObservedAtUtc);
    }

    private async Task<EventDispatchRemediationRuntimeState?> ReadStateByCommandIdAsync(
        string normalizedCommandId,
        CancellationToken cancellationToken)
    {
        var entry = await journalContext.EventDispatchRemediationCommandJournalEntries
            .AsNoTracking()
            .Where(entry => entry.CommandId == normalizedCommandId)
            .OrderByDescending(entry => entry.ObservedAtUtc)
            .ThenBy(entry => entry.CommandId)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return entry is null ? null : CreateState(entry);
    }

    private static EntityFrameworkEventDispatchRemediationCommandEntry CreateReservedEntry(EventDispatchRemediationRequest request)
    {
        var observedAtUtc = request.RequestedAtUtc == default
            ? DateTimeOffset.UtcNow
            : request.RequestedAtUtc;
        var metadata = CreateReservationMetadata(
            request.CommandId,
            request.OperationId,
            observedAtUtc,
            state: "reserved",
            request.Metadata);

        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            metadata[EventDispatchRemediationMetadataKeys.OperatorCommandReason] = request.Reason;
        }

        if (!string.IsNullOrWhiteSpace(request.ActorId))
        {
            metadata[EventDispatchRemediationMetadataKeys.OperatorActorId] = request.ActorId;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            metadata[EventDispatchRemediationMetadataKeys.OperatorCorrelationId] = request.CorrelationId;
        }

        return CreateEntry(new EventDispatchRemediationResult(
            CommandId: request.CommandId,
            OutboxId: request.OutboxId,
            MessageId: request.MessageId,
            ChannelId: request.ChannelId,
            OperationId: request.OperationId.Trim().ToLowerInvariant(),
            Outcome: EventDispatchRemediationOutcomes.Reserved,
            DispatchOutcome: PendingDispatchOutcome,
            ObservedAtUtc: observedAtUtc,
            Error: null,
            Metadata: metadata));
    }

    private static EntityFrameworkEventDispatchRemediationCommandEntry CreateEntry(EventDispatchRemediationResult result)
    {
        var entry = new EntityFrameworkEventDispatchRemediationCommandEntry();
        ApplyResult(entry, result);
        return entry;
    }

    private static void ApplyResult(
        EntityFrameworkEventDispatchRemediationCommandEntry entry,
        EventDispatchRemediationResult result)
    {
        result.Metadata.TryGetValue(EventDispatchRemediationMetadataKeys.OperatorActorId, out var actorId);
        result.Metadata.TryGetValue(EventDispatchRemediationMetadataKeys.OperatorCorrelationId, out var correlationId);
        result.Metadata.TryGetValue(EventDispatchRemediationMetadataKeys.OperatorCommandReason, out var reason);

        entry.CommandId = result.CommandId;
        entry.OutboxId = result.OutboxId;
        entry.MessageId = result.MessageId;
        entry.ChannelId = result.ChannelId;
        entry.OperationId = result.OperationId;
        entry.Outcome = result.Outcome;
        entry.DispatchOutcome = result.DispatchOutcome;
        entry.ObservedAtUtc = result.ObservedAtUtc;
        entry.Error = result.Error;
        entry.ActorId = string.IsNullOrWhiteSpace(actorId) ? null : actorId;
        entry.CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId;
        entry.Reason = string.IsNullOrWhiteSpace(reason) ? null : reason;
        entry.MetadataJson = JsonSerializer.Serialize(new Dictionary<string, string>(result.Metadata, StringComparer.OrdinalIgnoreCase));
    }

    private static EventDispatchRemediationRuntimeState CreateState(EntityFrameworkEventDispatchRemediationCommandEntry entry)
    {
        var metadata = DeserializeDictionary(entry.MetadataJson);
        return new EventDispatchRemediationRuntimeState(
            CommandId: entry.CommandId,
            OutboxId: entry.OutboxId,
            MessageId: entry.MessageId,
            ChannelId: entry.ChannelId,
            OperationId: entry.OperationId,
            Outcome: entry.Outcome,
            DispatchOutcome: entry.DispatchOutcome,
            ObservedAtUtc: entry.ObservedAtUtc,
            Error: entry.Error,
            Metadata: metadata.Count == 0
                ? EmptyMetadata
                : new ReadOnlyDictionary<string, string>(metadata));
    }

    private static EventDispatchRemediationRuntimeSummary CreateSummary(IReadOnlyList<EventDispatchRemediationRuntimeState> states)
    {
        if (states.Count == 0)
        {
            return EventDispatchRemediationRuntimeSummary.Empty;
        }

        var lastState = states[0];
        var oldestState = GetOldestState(states);
        var oldestReservedState = GetOldestReservedState(states);
        return new EventDispatchRemediationRuntimeSummary(
            totalCommandCount: states.Count,
            acceptedCount: states.Count(static state =>
                string.Equals(state.Outcome, EventDispatchRemediationOutcomes.Accepted, StringComparison.OrdinalIgnoreCase)),
            rejectedCount: states.Count(static state =>
                string.Equals(state.Outcome, EventDispatchRemediationOutcomes.Rejected, StringComparison.OrdinalIgnoreCase)),
            errorCount: states.Count(static state => !string.IsNullOrWhiteSpace(state.Error)),
            duplicateCommandCount: states.Count(static state =>
                state.Metadata.TryGetValue(EventDispatchRemediationMetadataKeys.DuplicateCommand, out var duplicateCommand) &&
                string.Equals(duplicateCommand, "true", StringComparison.OrdinalIgnoreCase)),
            reservedCount: states.Count(static state =>
                string.Equals(state.Outcome, EventDispatchRemediationOutcomes.Reserved, StringComparison.OrdinalIgnoreCase)),
            lastCommandId: lastState.CommandId,
            lastOperationId: lastState.OperationId,
            lastOutcome: lastState.Outcome,
            lastDispatchOutcome: lastState.DispatchOutcome,
            lastObservedAtUtc: lastState.ObservedAtUtc,
            droppedCommandCount: 0,
            retentionTruncated: false,
            summaryMayBeIncomplete: false,
            oldestRetainedCommandId: oldestState?.CommandId,
            oldestRetainedObservedAtUtc: oldestState?.ObservedAtUtc,
            oldestReservedCommandId: oldestReservedState?.CommandId,
            oldestReservedObservedAtUtc: oldestReservedState?.ObservedAtUtc);
    }

    private static EventDispatchRemediationRuntimeState? GetOldestState(IReadOnlyList<EventDispatchRemediationRuntimeState> states)
    {
        return states
            .OrderBy(static state => state.ObservedAtUtc)
            .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static EventDispatchRemediationRuntimeState? GetOldestReservedState(IReadOnlyList<EventDispatchRemediationRuntimeState> states)
    {
        return states
            .Where(static state =>
                string.Equals(state.Outcome, EventDispatchRemediationOutcomes.Reserved, StringComparison.OrdinalIgnoreCase))
            .OrderBy(static state => state.ObservedAtUtc)
            .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static void ValidateObservationWindow(
        DateTimeOffset? fromObservedAtUtc,
        DateTimeOffset? toObservedAtUtc)
    {
        if (fromObservedAtUtc is { } from && toObservedAtUtc is { } to && from > to)
        {
            throw new ArgumentException(
                "The observation window start must be less than or equal to the end.",
                nameof(fromObservedAtUtc));
        }
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

    private static EventDispatchRemediationCommandReservation CreateDuplicateReservation(
        string commandId,
        EventDispatchRemediationRuntimeState existingState)
    {
        var metadata = CreateReservationMetadata(
            commandId,
            existingState.OperationId,
            existingState.ObservedAtUtc,
            state: "duplicate");

        return new EventDispatchRemediationCommandReservation(
            commandId,
            reserved: false,
            existingState,
            metadata);
    }

    private static Dictionary<string, string> CreateReservationMetadata(
        string commandId,
        string operationId,
        DateTimeOffset observedAtUtc,
        string state,
        IReadOnlyDictionary<string, string>? sourceMetadata = null)
    {
        var metadata = sourceMetadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(sourceMetadata, StringComparer.OrdinalIgnoreCase);

        metadata["operatorCommandId"] = commandId;
        metadata["operatorCommand"] = operationId.Trim().ToLowerInvariant();
        metadata["operatorCommandRequestedAtUtc"] = observedAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        metadata[EventDispatchRemediationMetadataKeys.CommandIdempotencyPolicy] = "unique-command-id";
        metadata[EventDispatchRemediationMetadataKeys.DuplicateCommandPolicy] = "reject-without-mutation";
        metadata[EventDispatchRemediationMetadataKeys.CommandReservationPolicy] = "reserve-before-mutation";
        metadata[EventDispatchRemediationMetadataKeys.CommandReservationTiming] = "before-dispatch-store-mutation";
        metadata[EventDispatchRemediationMetadataKeys.CommandReservationDuplicatePolicy] = "duplicate-reservation-rejects-without-mutation";
        metadata[EventDispatchRemediationMetadataKeys.CommandReservationInDoubtOutcome] = EventDispatchRemediationOutcomes.Reserved;
        metadata[EventDispatchRemediationMetadataKeys.CommandReservationOwner] = "command-journal";
        metadata[EventDispatchRemediationMetadataKeys.CommandReservationState] = state;
        return metadata;
    }
}
