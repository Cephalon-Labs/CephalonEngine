using Cephalon.Abstractions.Data;
using Cephalon.Eventing.Configuration;
using System.Collections.ObjectModel;

namespace Cephalon.Eventing.Services;

internal sealed class EventDispatchRemediationRuntimeCatalog(
    EventingOptions options) : IEventDispatchRemediationRuntimeCatalog
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    private readonly Lock gate = new();
    private readonly Dictionary<string, EventDispatchRemediationRuntimeState> statesByCommandId = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<EventDispatchRemediationRuntimeState> states = [];
    private long totalRecordedCommandCount;
    private long droppedCommandCount;

    public EventDispatchRemediationRuntimeSummary Summary
    {
        get
        {
            lock (gate)
            {
                return CreateSummary(states);
            }
        }
    }

    public EventDispatchRemediationRuntimeRetention Retention
    {
        get
        {
            lock (gate)
            {
                return CreateRetention(
                    states,
                    options.RemediationCommandHistoryLimit,
                    totalRecordedCommandCount,
                    droppedCommandCount);
            }
        }
    }

    public EventDispatchRemediationRuntimeState? Latest
    {
        get
        {
            lock (gate)
            {
                return GetLatestState(states);
            }
        }
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> States
    {
        get
        {
            lock (gate)
            {
                return states
                    .OrderByDescending(static state => state.ObservedAtUtc)
                    .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }
    }

    private static EventDispatchRemediationRuntimeSummary CreateSummary(List<EventDispatchRemediationRuntimeState> recordedStates)
    {
        if (recordedStates.Count == 0)
        {
            return EventDispatchRemediationRuntimeSummary.Empty;
        }

        var lastState = GetLatestState(recordedStates)!;

        return new EventDispatchRemediationRuntimeSummary(
            totalCommandCount: recordedStates.Count,
            acceptedCount: recordedStates.Count(static state =>
                string.Equals(state.Outcome, EventDispatchRemediationOutcomes.Accepted, StringComparison.OrdinalIgnoreCase)),
            rejectedCount: recordedStates.Count(static state =>
                string.Equals(state.Outcome, EventDispatchRemediationOutcomes.Rejected, StringComparison.OrdinalIgnoreCase)),
            errorCount: recordedStates.Count(static state => !string.IsNullOrWhiteSpace(state.Error)),
            duplicateCommandCount: recordedStates.Count(static state =>
                state.Metadata.TryGetValue(EventDispatchRemediationMetadataKeys.DuplicateCommand, out var duplicateCommand) &&
                string.Equals(duplicateCommand, "true", StringComparison.OrdinalIgnoreCase)),
            lastCommandId: lastState.CommandId,
            lastOperationId: lastState.OperationId,
            lastOutcome: lastState.Outcome,
            lastDispatchOutcome: lastState.DispatchOutcome,
            lastObservedAtUtc: lastState.ObservedAtUtc);
    }

    private static EventDispatchRemediationRuntimeRetention CreateRetention(
        List<EventDispatchRemediationRuntimeState> recordedStates,
        int historyLimit,
        long totalRecordedCommandCount,
        long droppedCommandCount)
    {
        if (recordedStates.Count == 0)
        {
            return new EventDispatchRemediationRuntimeRetention(
                historyLimit: historyLimit,
                totalRecordedCommandCount: totalRecordedCommandCount,
                droppedCommandCount: droppedCommandCount,
                truncated: droppedCommandCount > 0);
        }

        var oldestState = GetOldestState(recordedStates)!;
        var latestState = GetLatestState(recordedStates)!;

        return new EventDispatchRemediationRuntimeRetention(
            historyLimit: historyLimit,
            retainedCommandCount: recordedStates.Count,
            totalRecordedCommandCount: totalRecordedCommandCount,
            droppedCommandCount: droppedCommandCount,
            truncated: droppedCommandCount > 0,
            oldestRetainedCommandId: oldestState.CommandId,
            oldestRetainedObservedAtUtc: oldestState.ObservedAtUtc,
            latestRetainedCommandId: latestState.CommandId,
            latestRetainedObservedAtUtc: latestState.ObservedAtUtc);
    }

    private static EventDispatchRemediationRuntimeState? GetLatestState(List<EventDispatchRemediationRuntimeState> recordedStates)
    {
        return recordedStates
            .OrderByDescending(static state => state.ObservedAtUtc)
            .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static EventDispatchRemediationRuntimeState? GetOldestState(List<EventDispatchRemediationRuntimeState> recordedStates)
    {
        return recordedStates
            .OrderBy(static state => state.ObservedAtUtc)
            .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    public EventDispatchRemediationRuntimeState? GetByCommandId(string commandId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        lock (gate)
        {
            return statesByCommandId.GetValueOrDefault(commandId.Trim());
        }
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByOutboxId(string outboxId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxId);
        var normalizedOutboxId = outboxId.Trim();

        lock (gate)
        {
            return states
                .Where(state => string.Equals(state.OutboxId, normalizedOutboxId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(static state => state.ObservedAtUtc)
                .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByMessageId(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        var normalizedMessageId = messageId.Trim();

        lock (gate)
        {
            return states
                .Where(state => string.Equals(state.MessageId, normalizedMessageId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(static state => state.ObservedAtUtc)
                .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByChannelId(string channelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        var normalizedChannelId = channelId.Trim();

        lock (gate)
        {
            return states
                .Where(state => string.Equals(state.ChannelId, normalizedChannelId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(static state => state.ObservedAtUtc)
                .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();

        lock (gate)
        {
            return states
                .Where(state => string.Equals(state.OperationId, normalizedOperationId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(static state => state.ObservedAtUtc)
                .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByActorId(string actorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        var normalizedActorId = actorId.Trim();

        lock (gate)
        {
            return states
                .Where(state =>
                    state.Metadata.TryGetValue(EventDispatchRemediationMetadataKeys.OperatorActorId, out var recordedActorId) &&
                    string.Equals(recordedActorId, normalizedActorId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(static state => state.ObservedAtUtc)
                .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByCorrelationId(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        var normalizedCorrelationId = correlationId.Trim();

        lock (gate)
        {
            return states
                .Where(state =>
                    state.Metadata.TryGetValue(EventDispatchRemediationMetadataKeys.OperatorCorrelationId, out var recordedCorrelationId) &&
                    string.Equals(recordedCorrelationId, normalizedCorrelationId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(static state => state.ObservedAtUtc)
                .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByReason(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var normalizedReason = reason.Trim();

        lock (gate)
        {
            return states
                .Where(state =>
                    state.Metadata.TryGetValue(EventDispatchRemediationMetadataKeys.OperatorCommandReason, out var recordedReason) &&
                    string.Equals(recordedReason, normalizedReason, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(static state => state.ObservedAtUtc)
                .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByOutcome(string outcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);
        var normalizedOutcome = outcome.Trim();

        lock (gate)
        {
            return states
                .Where(state => string.Equals(state.Outcome, normalizedOutcome, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(static state => state.ObservedAtUtc)
                .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByDispatchOutcome(string dispatchOutcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dispatchOutcome);
        var normalizedDispatchOutcome = dispatchOutcome.Trim();

        lock (gate)
        {
            return states
                .Where(state => string.Equals(state.DispatchOutcome, normalizedDispatchOutcome, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(static state => state.ObservedAtUtc)
                .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public void Record(EventDispatchRemediationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var limit = options.RemediationCommandHistoryLimit;
        if (limit == 0)
        {
            return;
        }

        var metadata = result.Metadata.Count == 0
            ? EmptyMetadata
            : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(result.Metadata, StringComparer.OrdinalIgnoreCase));
        var state = new EventDispatchRemediationRuntimeState(
            CommandId: result.CommandId,
            OutboxId: result.OutboxId,
            MessageId: result.MessageId,
            ChannelId: result.ChannelId,
            OperationId: result.OperationId,
            Outcome: result.Outcome,
            DispatchOutcome: result.DispatchOutcome,
            ObservedAtUtc: result.ObservedAtUtc,
            Error: result.Error,
            Metadata: metadata);

        lock (gate)
        {
            var replacedExisting = statesByCommandId.Remove(state.CommandId);
            if (replacedExisting)
            {
                states.RemoveAll(existing => string.Equals(existing.CommandId, state.CommandId, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                totalRecordedCommandCount++;
            }

            states.Add(state);
            statesByCommandId[state.CommandId] = state;

            while (states.Count > limit)
            {
                var oldest = GetOldestState(states)!;
                states.Remove(oldest);
                statesByCommandId.Remove(oldest.CommandId);
                droppedCommandCount++;
            }
        }
    }
}
