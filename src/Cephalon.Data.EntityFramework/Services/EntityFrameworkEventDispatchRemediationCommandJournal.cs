using System.Collections.ObjectModel;
using System.Text.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Data.EntityFramework.Modeling;
using Cephalon.Eventing.Services;
using Microsoft.EntityFrameworkCore;

namespace Cephalon.Data.EntityFramework.Services;

internal sealed class EntityFrameworkEventDispatchRemediationCommandJournal(
    DbContext dbContext,
    IEntityFrameworkEventDispatchRemediationCommandJournalContext journalContext) : IEventDispatchRemediationCommandJournal
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    public EventDispatchRemediationCommandJournalDescriptor Descriptor { get; } = new(
        JournalId: "entity-framework.event-dispatch-remediation-command-journal",
        Provider: "Cephalon.Data.EntityFramework",
        Storage: "entity-framework-table",
        Durability: "durable",
        Scope: "cross-node",
        CrossNodeCommandAudit: true,
        DurableReplayCursor: false);

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

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByMessageId(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        var normalizedMessageId = messageId.Trim();
        return ReadStates(entries => entries.Where(entry => entry.MessageId == normalizedMessageId));
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByChannelId(string channelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        var normalizedChannelId = channelId.Trim();
        return ReadStates(entries => entries.Where(entry => entry.ChannelId == normalizedChannelId));
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();
        return ReadStates(entries => entries.Where(entry => entry.OperationId == normalizedOperationId));
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByActorId(string actorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        var normalizedActorId = actorId.Trim();
        return ReadStates(entries => entries.Where(entry => entry.ActorId == normalizedActorId));
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByCorrelationId(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        var normalizedCorrelationId = correlationId.Trim();
        return ReadStates(entries => entries.Where(entry => entry.CorrelationId == normalizedCorrelationId));
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByReason(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var normalizedReason = reason.Trim();
        return ReadStates(entries => entries.Where(entry => entry.Reason == normalizedReason));
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByOutcome(string outcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);
        var normalizedOutcome = outcome.Trim();
        return ReadStates(entries => entries.Where(entry => entry.Outcome == normalizedOutcome));
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByDispatchOutcome(string dispatchOutcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dispatchOutcome);
        var normalizedDispatchOutcome = dispatchOutcome.Trim();
        return ReadStates(entries => entries.Where(entry => entry.DispatchOutcome == normalizedDispatchOutcome));
    }

    public async ValueTask RecordAsync(
        EventDispatchRemediationResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedCommandId = result.CommandId.Trim();
        var exists = await journalContext.EventDispatchRemediationCommandJournalEntries
            .AnyAsync(entry => entry.CommandId == normalizedCommandId, cancellationToken)
            .ConfigureAwait(false);
        if (exists)
        {
            return;
        }

        journalContext.EventDispatchRemediationCommandJournalEntries.Add(CreateEntry(result));
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

    private static EntityFrameworkEventDispatchRemediationCommandEntry CreateEntry(EventDispatchRemediationResult result)
    {
        result.Metadata.TryGetValue(EventDispatchRemediationMetadataKeys.OperatorActorId, out var actorId);
        result.Metadata.TryGetValue(EventDispatchRemediationMetadataKeys.OperatorCorrelationId, out var correlationId);
        result.Metadata.TryGetValue(EventDispatchRemediationMetadataKeys.OperatorCommandReason, out var reason);

        return new EntityFrameworkEventDispatchRemediationCommandEntry
        {
            CommandId = result.CommandId,
            OutboxId = result.OutboxId,
            MessageId = result.MessageId,
            ChannelId = result.ChannelId,
            OperationId = result.OperationId,
            Outcome = result.Outcome,
            DispatchOutcome = result.DispatchOutcome,
            ObservedAtUtc = result.ObservedAtUtc,
            Error = result.Error,
            ActorId = string.IsNullOrWhiteSpace(actorId) ? null : actorId,
            CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason,
            MetadataJson = JsonSerializer.Serialize(new Dictionary<string, string>(result.Metadata, StringComparer.OrdinalIgnoreCase))
        };
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
            lastCommandId: lastState.CommandId,
            lastOperationId: lastState.OperationId,
            lastOutcome: lastState.Outcome,
            lastDispatchOutcome: lastState.DispatchOutcome,
            lastObservedAtUtc: lastState.ObservedAtUtc,
            droppedCommandCount: 0,
            retentionTruncated: false,
            summaryMayBeIncomplete: false,
            oldestRetainedCommandId: GetOldestState(states)?.CommandId,
            oldestRetainedObservedAtUtc: GetOldestState(states)?.ObservedAtUtc);
    }

    private static EventDispatchRemediationRuntimeState? GetOldestState(IReadOnlyList<EventDispatchRemediationRuntimeState> states)
    {
        return states
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
}
