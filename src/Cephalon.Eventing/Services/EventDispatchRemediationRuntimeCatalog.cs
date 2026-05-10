using Cephalon.Abstractions.Data;
using Cephalon.Eventing.Configuration;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class EventDispatchRemediationRuntimeCatalog(
    EventingOptions options) : IEventDispatchRemediationCommandJournal
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    private readonly Lock gate = new();
    private readonly Dictionary<string, EventDispatchRemediationRuntimeState> statesByCommandId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, EventDispatchRemediationRuntimeState> reservationsByCommandId = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<EventDispatchRemediationRuntimeState> states = [];
    private long totalRecordedCommandCount;
    private long droppedCommandCount;

    public EventDispatchRemediationCommandJournalDescriptor Descriptor { get; } = new(
        JournalId: "eventing.process-local-remediation-command-journal",
        Provider: "Cephalon.Eventing",
        Storage: "memory",
        Durability: "process-local",
        Scope: "single-process",
        CrossNodeCommandAudit: false,
        DurableReplayCursor: false);

    public EventDispatchRemediationRuntimeSummary Summary
    {
        get
        {
            lock (gate)
            {
                return CreateSummary(
                    states,
                    droppedCommandCount,
                    summaryMayBeIncomplete: droppedCommandCount > 0);
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

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetInDoubt()
    {
        return GetInDoubtBefore(null);
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetInDoubtBefore(DateTimeOffset? beforeObservedAtUtc)
    {
        lock (gate)
        {
            return states
                .Where(state =>
                    IsReservedState(state) &&
                    (beforeObservedAtUtc is null || state.ObservedAtUtc <= beforeObservedAtUtc.Value))
                .OrderByDescending(static state => state.ObservedAtUtc)
                .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public EventDispatchRemediationRuntimeState? GetOldestInDoubtBefore(DateTimeOffset? beforeObservedAtUtc)
    {
        lock (gate)
        {
            return states
                .Where(state =>
                    IsReservedState(state) &&
                    (beforeObservedAtUtc is null || state.ObservedAtUtc <= beforeObservedAtUtc.Value))
                .OrderBy(static state => state.ObservedAtUtc)
                .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }
    }

    public EventDispatchRemediationRuntimeSummary GetInDoubtSummaryBefore(DateTimeOffset? beforeObservedAtUtc)
    {
        lock (gate)
        {
            return CreateFilteredSummary(
                state =>
                    IsReservedState(state) &&
                    (beforeObservedAtUtc is null || state.ObservedAtUtc <= beforeObservedAtUtc.Value));
        }
    }

    private static EventDispatchRemediationRuntimeSummary CreateSummary(
        List<EventDispatchRemediationRuntimeState> recordedStates,
        long droppedCommandCount = 0,
        bool summaryMayBeIncomplete = false,
        EventDispatchRemediationRuntimeState? oldestRetainedState = null)
    {
        var aggregate = AggregateStates(recordedStates);
        return CreateSummary(
            aggregate,
            droppedCommandCount,
            summaryMayBeIncomplete,
            oldestRetainedState ?? aggregate.OldestState);
    }

    private static EventDispatchRemediationRuntimeSummary CreateSummary(
        RemediationStateAggregate aggregate,
        long droppedCommandCount = 0,
        bool summaryMayBeIncomplete = false,
        EventDispatchRemediationRuntimeState? oldestRetainedState = null)
    {
        if (aggregate.Count == 0)
        {
            return droppedCommandCount == 0 && !summaryMayBeIncomplete && oldestRetainedState is null
                ? EventDispatchRemediationRuntimeSummary.Empty
                : new EventDispatchRemediationRuntimeSummary(
                    droppedCommandCount: droppedCommandCount,
                    retentionTruncated: droppedCommandCount > 0,
                    summaryMayBeIncomplete: summaryMayBeIncomplete,
                    oldestRetainedCommandId: oldestRetainedState?.CommandId,
                    oldestRetainedObservedAtUtc: oldestRetainedState?.ObservedAtUtc);
        }

        var lastState = aggregate.LatestState!;
        var oldestReservedState = aggregate.OldestReservedState;

        return new EventDispatchRemediationRuntimeSummary(
            totalCommandCount: aggregate.Count,
            acceptedCount: aggregate.AcceptedCount,
            rejectedCount: aggregate.RejectedCount,
            errorCount: aggregate.ErrorCount,
            duplicateCommandCount: aggregate.DuplicateCommandCount,
            reservedCount: aggregate.ReservedCount,
            lastCommandId: lastState.CommandId,
            lastOperationId: lastState.OperationId,
            lastOutcome: lastState.Outcome,
            lastDispatchOutcome: lastState.DispatchOutcome,
            lastObservedAtUtc: lastState.ObservedAtUtc,
            droppedCommandCount: droppedCommandCount,
            retentionTruncated: droppedCommandCount > 0,
            summaryMayBeIncomplete: summaryMayBeIncomplete,
            oldestRetainedCommandId: oldestRetainedState?.CommandId,
            oldestRetainedObservedAtUtc: oldestRetainedState?.ObservedAtUtc,
            oldestReservedCommandId: oldestReservedState?.CommandId,
            oldestReservedObservedAtUtc: oldestReservedState?.ObservedAtUtc);
    }

    private static EventDispatchRemediationRuntimeRetention CreateRetention(
        List<EventDispatchRemediationRuntimeState> recordedStates,
        int historyLimit,
        long totalRecordedCommandCount,
        long droppedCommandCount)
    {
        return CreateRetention(
            AggregateStates(recordedStates),
            historyLimit,
            totalRecordedCommandCount,
            droppedCommandCount);
    }

    private static EventDispatchRemediationRuntimeRetention CreateRetention(
        RemediationStateAggregate aggregate,
        int historyLimit,
        long totalRecordedCommandCount,
        long droppedCommandCount)
    {
        if (aggregate.Count == 0)
        {
            return new EventDispatchRemediationRuntimeRetention(
                historyLimit: historyLimit,
                totalRecordedCommandCount: totalRecordedCommandCount,
                droppedCommandCount: droppedCommandCount,
                truncated: droppedCommandCount > 0);
        }

        var oldestState = aggregate.OldestState!;
        var latestState = aggregate.LatestState!;

        return new EventDispatchRemediationRuntimeRetention(
            historyLimit: historyLimit,
            retainedCommandCount: aggregate.Count,
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
        EventDispatchRemediationRuntimeState? latestState = null;
        foreach (var state in recordedStates)
        {
            if (IsLatestCandidate(state, latestState))
            {
                latestState = state;
            }
        }

        return latestState;
    }

    private static EventDispatchRemediationRuntimeState? GetOldestState(List<EventDispatchRemediationRuntimeState> recordedStates)
    {
        EventDispatchRemediationRuntimeState? oldestState = null;
        foreach (var state in recordedStates)
        {
            if (IsOldestCandidate(state, oldestState))
            {
                oldestState = state;
            }
        }

        return oldestState;
    }

    private static EventDispatchRemediationRuntimeState? GetOldestReservedState(List<EventDispatchRemediationRuntimeState> recordedStates)
    {
        EventDispatchRemediationRuntimeState? oldestReservedState = null;
        foreach (var state in recordedStates)
        {
            if (IsReservedState(state) && IsOldestCandidate(state, oldestReservedState))
            {
                oldestReservedState = state;
            }
        }

        return oldestReservedState;
    }

    private static bool IsReservedState(EventDispatchRemediationRuntimeState state) =>
        string.Equals(state.Outcome, EventDispatchRemediationOutcomes.Reserved, StringComparison.OrdinalIgnoreCase);

    public EventDispatchRemediationRuntimeState? GetByCommandId(string commandId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        lock (gate)
        {
            var normalizedCommandId = commandId.Trim();
            return statesByCommandId.GetValueOrDefault(normalizedCommandId) ??
                reservationsByCommandId.GetValueOrDefault(normalizedCommandId);
        }
    }

    public IReadOnlyList<EventDispatchRemediationRuntimeState> GetByObservedAt(
        DateTimeOffset? fromObservedAtUtc,
        DateTimeOffset? toObservedAtUtc)
    {
        ValidateObservationWindow(fromObservedAtUtc, toObservedAtUtc);

        lock (gate)
        {
            return states
                .Where(state =>
                    (fromObservedAtUtc is null || state.ObservedAtUtc >= fromObservedAtUtc.Value) &&
                    (toObservedAtUtc is null || state.ObservedAtUtc <= toObservedAtUtc.Value))
                .OrderByDescending(static state => state.ObservedAtUtc)
                .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public EventDispatchRemediationRuntimeSummary GetSummaryByObservedAt(
        DateTimeOffset? fromObservedAtUtc,
        DateTimeOffset? toObservedAtUtc)
    {
        ValidateObservationWindow(fromObservedAtUtc, toObservedAtUtc);

        lock (gate)
        {
            var filteredStates = AggregateStatesWithOldestRetained(
                states,
                state =>
                    (fromObservedAtUtc is null || state.ObservedAtUtc >= fromObservedAtUtc.Value) &&
                    (toObservedAtUtc is null || state.ObservedAtUtc <= toObservedAtUtc.Value));

            return CreateFilteredSummary(
                filteredStates,
                summaryMayBeIncomplete: IsObservationWindowPotentiallyTruncated(
                    fromObservedAtUtc,
                    filteredStates.OldestRetainedState,
                    droppedCommandCount));
        }
    }

    private EventDispatchRemediationRuntimeSummary CreateFilteredSummary(
        (RemediationStateAggregate Aggregate, EventDispatchRemediationRuntimeState? OldestRetainedState) filteredStates,
        bool summaryMayBeIncomplete)
    {
        return CreateSummary(
            filteredStates.Aggregate,
            droppedCommandCount,
            summaryMayBeIncomplete: summaryMayBeIncomplete,
            oldestRetainedState: filteredStates.OldestRetainedState);
    }

    private EventDispatchRemediationRuntimeSummary CreateFilteredSummary(
        Func<EventDispatchRemediationRuntimeState, bool> predicate,
        bool? summaryMayBeIncomplete = null)
    {
        var filteredStates = AggregateStatesWithOldestRetained(states, predicate);

        return CreateSummary(
            filteredStates.Aggregate,
            droppedCommandCount,
            summaryMayBeIncomplete: summaryMayBeIncomplete ?? droppedCommandCount > 0,
            oldestRetainedState: filteredStates.OldestRetainedState);
    }

    private EventDispatchRemediationRuntimeSummary CreateFilteredSummary(
        RemediationStateFilter filter,
        string filterValue)
    {
        var filteredStates = AggregateStatesWithOldestRetained(states, filter, filterValue);

        return CreateSummary(
            filteredStates.Aggregate,
            droppedCommandCount,
            summaryMayBeIncomplete: droppedCommandCount > 0,
            oldestRetainedState: filteredStates.OldestRetainedState);
    }

    private EventDispatchRemediationRuntimeRetention CreateFilteredRetention(
        Func<EventDispatchRemediationRuntimeState, bool> predicate)
    {
        var aggregate = AggregateStates(states, predicate);

        return CreateRetention(
            aggregate,
            options.RemediationCommandHistoryLimit,
            aggregate.Count + droppedCommandCount,
            droppedCommandCount);
    }

    private EventDispatchRemediationRuntimeRetention CreateFilteredRetention(
        RemediationStateFilter filter,
        string filterValue)
    {
        var aggregate = AggregateStates(states, filter, filterValue);

        return CreateRetention(
            aggregate,
            options.RemediationCommandHistoryLimit,
            aggregate.Count + droppedCommandCount,
            droppedCommandCount);
    }

    private EventDispatchRemediationRuntimeState? GetLatestFilteredState(
        Func<EventDispatchRemediationRuntimeState, bool> predicate)
    {
        EventDispatchRemediationRuntimeState? latestState = null;
        foreach (var state in states)
        {
            if (predicate(state) && IsLatestCandidate(state, latestState))
            {
                latestState = state;
            }
        }

        return latestState;
    }

    private EventDispatchRemediationRuntimeState? GetLatestFilteredState(
        RemediationStateFilter filter,
        string filterValue)
    {
        EventDispatchRemediationRuntimeState? latestState = null;
        foreach (var state in states)
        {
            if (MatchesFilter(state, filter, filterValue) && IsLatestCandidate(state, latestState))
            {
                latestState = state;
            }
        }

        return latestState;
    }

    private EventDispatchRemediationRuntimeState? GetOldestFilteredState(
        Func<EventDispatchRemediationRuntimeState, bool> predicate)
    {
        EventDispatchRemediationRuntimeState? oldestState = null;
        foreach (var state in states)
        {
            if (predicate(state) && IsOldestCandidate(state, oldestState))
            {
                oldestState = state;
            }
        }

        return oldestState;
    }

    private EventDispatchRemediationRuntimeState? GetOldestFilteredState(
        RemediationStateFilter filter,
        string filterValue)
    {
        EventDispatchRemediationRuntimeState? oldestState = null;
        foreach (var state in states)
        {
            if (MatchesFilter(state, filter, filterValue) && IsOldestCandidate(state, oldestState))
            {
                oldestState = state;
            }
        }

        return oldestState;
    }

    private static RemediationStateAggregate AggregateStates(List<EventDispatchRemediationRuntimeState> recordedStates)
    {
        var aggregate = new RemediationStateAggregate();
        foreach (var state in recordedStates)
        {
            aggregate.Add(state);
        }

        return aggregate;
    }

    private static RemediationStateAggregate AggregateStates(
        List<EventDispatchRemediationRuntimeState> recordedStates,
        Func<EventDispatchRemediationRuntimeState, bool> predicate)
    {
        var aggregate = new RemediationStateAggregate();
        foreach (var state in recordedStates)
        {
            if (predicate(state))
            {
                aggregate.Add(state);
            }
        }

        return aggregate;
    }

    private static (RemediationStateAggregate Aggregate, EventDispatchRemediationRuntimeState? OldestRetainedState)
        AggregateStatesWithOldestRetained(
            List<EventDispatchRemediationRuntimeState> recordedStates,
            Func<EventDispatchRemediationRuntimeState, bool> predicate)
    {
        var aggregate = new RemediationStateAggregate();
        EventDispatchRemediationRuntimeState? oldestRetainedState = null;
        foreach (var state in recordedStates)
        {
            if (IsOldestCandidate(state, oldestRetainedState))
            {
                oldestRetainedState = state;
            }

            if (predicate(state))
            {
                aggregate.Add(state);
            }
        }

        return (aggregate, oldestRetainedState);
    }

    private static RemediationStateAggregate AggregateStates(
        List<EventDispatchRemediationRuntimeState> recordedStates,
        RemediationStateFilter filter,
        string filterValue)
    {
        var aggregate = new RemediationStateAggregate();
        foreach (var state in recordedStates)
        {
            if (MatchesFilter(state, filter, filterValue))
            {
                aggregate.Add(state);
            }
        }

        return aggregate;
    }

    private static (RemediationStateAggregate Aggregate, EventDispatchRemediationRuntimeState? OldestRetainedState)
        AggregateStatesWithOldestRetained(
            List<EventDispatchRemediationRuntimeState> recordedStates,
            RemediationStateFilter filter,
            string filterValue)
    {
        var aggregate = new RemediationStateAggregate();
        EventDispatchRemediationRuntimeState? oldestRetainedState = null;
        foreach (var state in recordedStates)
        {
            if (IsOldestCandidate(state, oldestRetainedState))
            {
                oldestRetainedState = state;
            }

            if (MatchesFilter(state, filter, filterValue))
            {
                aggregate.Add(state);
            }
        }

        return (aggregate, oldestRetainedState);
    }

    private static bool MatchesFilter(
        EventDispatchRemediationRuntimeState state,
        RemediationStateFilter filter,
        string filterValue)
    {
        return filter switch
        {
            RemediationStateFilter.OutboxId => string.Equals(state.OutboxId, filterValue, StringComparison.OrdinalIgnoreCase),
            RemediationStateFilter.MessageId => string.Equals(state.MessageId, filterValue, StringComparison.OrdinalIgnoreCase),
            RemediationStateFilter.ChannelId => string.Equals(state.ChannelId, filterValue, StringComparison.OrdinalIgnoreCase),
            RemediationStateFilter.OperationId => string.Equals(state.OperationId, filterValue, StringComparison.OrdinalIgnoreCase),
            RemediationStateFilter.ActorId => MetadataEquals(state, EventDispatchRemediationMetadataKeys.OperatorActorId, filterValue),
            RemediationStateFilter.CorrelationId => MetadataEquals(state, EventDispatchRemediationMetadataKeys.OperatorCorrelationId, filterValue),
            RemediationStateFilter.Reason => MetadataEquals(state, EventDispatchRemediationMetadataKeys.OperatorCommandReason, filterValue),
            RemediationStateFilter.Outcome => string.Equals(state.Outcome, filterValue, StringComparison.OrdinalIgnoreCase),
            RemediationStateFilter.DispatchOutcome => string.Equals(state.DispatchOutcome, filterValue, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static bool MetadataEquals(
        EventDispatchRemediationRuntimeState state,
        string key,
        string expectedValue)
    {
        return state.Metadata.TryGetValue(key, out var recordedValue) &&
            string.Equals(recordedValue, expectedValue, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLatestCandidate(
        EventDispatchRemediationRuntimeState state,
        EventDispatchRemediationRuntimeState? current)
    {
        if (current is null)
        {
            return true;
        }

        var observedAtComparison = state.ObservedAtUtc.CompareTo(current.ObservedAtUtc);
        return observedAtComparison > 0 ||
            observedAtComparison == 0 &&
            string.Compare(state.CommandId, current.CommandId, StringComparison.OrdinalIgnoreCase) < 0;
    }

    private static bool IsOldestCandidate(
        EventDispatchRemediationRuntimeState state,
        EventDispatchRemediationRuntimeState? current)
    {
        if (current is null)
        {
            return true;
        }

        var observedAtComparison = state.ObservedAtUtc.CompareTo(current.ObservedAtUtc);
        return observedAtComparison < 0 ||
            observedAtComparison == 0 &&
            string.Compare(state.CommandId, current.CommandId, StringComparison.OrdinalIgnoreCase) < 0;
    }

    private enum RemediationStateFilter
    {
        OutboxId,
        MessageId,
        ChannelId,
        OperationId,
        ActorId,
        CorrelationId,
        Reason,
        Outcome,
        DispatchOutcome
    }

    private struct RemediationStateAggregate
    {
        public int Count { get; private set; }
        public int AcceptedCount { get; private set; }
        public int RejectedCount { get; private set; }
        public int ErrorCount { get; private set; }
        public int DuplicateCommandCount { get; private set; }
        public int ReservedCount { get; private set; }
        public EventDispatchRemediationRuntimeState? LatestState { get; private set; }
        public EventDispatchRemediationRuntimeState? OldestState { get; private set; }
        public EventDispatchRemediationRuntimeState? OldestReservedState { get; private set; }

        public void Add(EventDispatchRemediationRuntimeState state)
        {
            Count++;
            if (string.Equals(state.Outcome, EventDispatchRemediationOutcomes.Accepted, StringComparison.OrdinalIgnoreCase))
            {
                AcceptedCount++;
            }

            if (string.Equals(state.Outcome, EventDispatchRemediationOutcomes.Rejected, StringComparison.OrdinalIgnoreCase))
            {
                RejectedCount++;
            }

            if (!string.IsNullOrWhiteSpace(state.Error))
            {
                ErrorCount++;
            }

            if (state.Metadata.TryGetValue(EventDispatchRemediationMetadataKeys.DuplicateCommand, out var duplicateCommand) &&
                string.Equals(duplicateCommand, "true", StringComparison.OrdinalIgnoreCase))
            {
                DuplicateCommandCount++;
            }

            if (IsReservedState(state))
            {
                ReservedCount++;
                if (IsOldestCandidate(state, OldestReservedState))
                {
                    OldestReservedState = state;
                }
            }

            if (IsLatestCandidate(state, LatestState))
            {
                LatestState = state;
            }

            if (IsOldestCandidate(state, OldestState))
            {
                OldestState = state;
            }
        }
    }

    private static bool IsObservationWindowPotentiallyTruncated(
        DateTimeOffset? fromObservedAtUtc,
        EventDispatchRemediationRuntimeState? oldestRetainedState,
        long droppedCommandCount)
    {
        if (droppedCommandCount == 0)
        {
            return false;
        }

        return oldestRetainedState is null ||
            fromObservedAtUtc is null ||
            fromObservedAtUtc.Value < oldestRetainedState.ObservedAtUtc;
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

    public EventDispatchRemediationRuntimeSummary GetSummaryByOutboxId(string outboxId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxId);
        var normalizedOutboxId = outboxId.Trim();

        lock (gate)
        {
            return CreateFilteredSummary(RemediationStateFilter.OutboxId, normalizedOutboxId);
        }
    }

    public EventDispatchRemediationRuntimeRetention GetRetentionByOutboxId(string outboxId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxId);
        var normalizedOutboxId = outboxId.Trim();

        lock (gate)
        {
            return CreateFilteredRetention(RemediationStateFilter.OutboxId, normalizedOutboxId);
        }
    }

    public EventDispatchRemediationRuntimeState? GetLatestByOutboxId(string outboxId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxId);
        var normalizedOutboxId = outboxId.Trim();

        lock (gate)
        {
            return GetLatestFilteredState(RemediationStateFilter.OutboxId, normalizedOutboxId);
        }
    }

    public EventDispatchRemediationRuntimeState? GetOldestByOutboxId(string outboxId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxId);
        var normalizedOutboxId = outboxId.Trim();

        lock (gate)
        {
            return GetOldestFilteredState(RemediationStateFilter.OutboxId, normalizedOutboxId);
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

    public EventDispatchRemediationRuntimeSummary GetSummaryByMessageId(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        var normalizedMessageId = messageId.Trim();

        lock (gate)
        {
            return CreateFilteredSummary(RemediationStateFilter.MessageId, normalizedMessageId);
        }
    }

    public EventDispatchRemediationRuntimeRetention GetRetentionByMessageId(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        var normalizedMessageId = messageId.Trim();

        lock (gate)
        {
            return CreateFilteredRetention(RemediationStateFilter.MessageId, normalizedMessageId);
        }
    }

    public EventDispatchRemediationRuntimeState? GetLatestByMessageId(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        var normalizedMessageId = messageId.Trim();

        lock (gate)
        {
            return GetLatestFilteredState(RemediationStateFilter.MessageId, normalizedMessageId);
        }
    }

    public EventDispatchRemediationRuntimeState? GetOldestByMessageId(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        var normalizedMessageId = messageId.Trim();

        lock (gate)
        {
            return GetOldestFilteredState(RemediationStateFilter.MessageId, normalizedMessageId);
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

    public EventDispatchRemediationRuntimeSummary GetSummaryByChannelId(string channelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        var normalizedChannelId = channelId.Trim();

        lock (gate)
        {
            return CreateFilteredSummary(RemediationStateFilter.ChannelId, normalizedChannelId);
        }
    }

    public EventDispatchRemediationRuntimeRetention GetRetentionByChannelId(string channelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        var normalizedChannelId = channelId.Trim();

        lock (gate)
        {
            return CreateFilteredRetention(RemediationStateFilter.ChannelId, normalizedChannelId);
        }
    }

    public EventDispatchRemediationRuntimeState? GetLatestByChannelId(string channelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        var normalizedChannelId = channelId.Trim();

        lock (gate)
        {
            return GetLatestFilteredState(RemediationStateFilter.ChannelId, normalizedChannelId);
        }
    }

    public EventDispatchRemediationRuntimeState? GetOldestByChannelId(string channelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        var normalizedChannelId = channelId.Trim();

        lock (gate)
        {
            return GetOldestFilteredState(RemediationStateFilter.ChannelId, normalizedChannelId);
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

    public EventDispatchRemediationRuntimeSummary GetSummaryByOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();

        lock (gate)
        {
            return CreateFilteredSummary(RemediationStateFilter.OperationId, normalizedOperationId);
        }
    }

    public EventDispatchRemediationRuntimeRetention GetRetentionByOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();

        lock (gate)
        {
            return CreateFilteredRetention(RemediationStateFilter.OperationId, normalizedOperationId);
        }
    }

    public EventDispatchRemediationRuntimeState? GetLatestByOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();

        lock (gate)
        {
            return GetLatestFilteredState(RemediationStateFilter.OperationId, normalizedOperationId);
        }
    }

    public EventDispatchRemediationRuntimeState? GetOldestByOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var normalizedOperationId = operationId.Trim();

        lock (gate)
        {
            return GetOldestFilteredState(RemediationStateFilter.OperationId, normalizedOperationId);
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

    public EventDispatchRemediationRuntimeSummary GetSummaryByActorId(string actorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        var normalizedActorId = actorId.Trim();

        lock (gate)
        {
            return CreateFilteredSummary(RemediationStateFilter.ActorId, normalizedActorId);
        }
    }

    public EventDispatchRemediationRuntimeRetention GetRetentionByActorId(string actorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        var normalizedActorId = actorId.Trim();

        lock (gate)
        {
            return CreateFilteredRetention(RemediationStateFilter.ActorId, normalizedActorId);
        }
    }

    public EventDispatchRemediationRuntimeState? GetLatestByActorId(string actorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        var normalizedActorId = actorId.Trim();

        lock (gate)
        {
            return GetLatestFilteredState(RemediationStateFilter.ActorId, normalizedActorId);
        }
    }

    public EventDispatchRemediationRuntimeState? GetOldestByActorId(string actorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        var normalizedActorId = actorId.Trim();

        lock (gate)
        {
            return GetOldestFilteredState(RemediationStateFilter.ActorId, normalizedActorId);
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

    public EventDispatchRemediationRuntimeSummary GetSummaryByCorrelationId(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        var normalizedCorrelationId = correlationId.Trim();

        lock (gate)
        {
            return CreateFilteredSummary(RemediationStateFilter.CorrelationId, normalizedCorrelationId);
        }
    }

    public EventDispatchRemediationRuntimeRetention GetRetentionByCorrelationId(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        var normalizedCorrelationId = correlationId.Trim();

        lock (gate)
        {
            return CreateFilteredRetention(RemediationStateFilter.CorrelationId, normalizedCorrelationId);
        }
    }

    public EventDispatchRemediationRuntimeState? GetLatestByCorrelationId(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        var normalizedCorrelationId = correlationId.Trim();

        lock (gate)
        {
            return GetLatestFilteredState(RemediationStateFilter.CorrelationId, normalizedCorrelationId);
        }
    }

    public EventDispatchRemediationRuntimeState? GetOldestByCorrelationId(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        var normalizedCorrelationId = correlationId.Trim();

        lock (gate)
        {
            return GetOldestFilteredState(RemediationStateFilter.CorrelationId, normalizedCorrelationId);
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

    public EventDispatchRemediationRuntimeSummary GetSummaryByReason(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var normalizedReason = reason.Trim();

        lock (gate)
        {
            return CreateFilteredSummary(RemediationStateFilter.Reason, normalizedReason);
        }
    }

    public EventDispatchRemediationRuntimeRetention GetRetentionByReason(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var normalizedReason = reason.Trim();

        lock (gate)
        {
            return CreateFilteredRetention(RemediationStateFilter.Reason, normalizedReason);
        }
    }

    public EventDispatchRemediationRuntimeState? GetLatestByReason(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var normalizedReason = reason.Trim();

        lock (gate)
        {
            return GetLatestFilteredState(RemediationStateFilter.Reason, normalizedReason);
        }
    }

    public EventDispatchRemediationRuntimeState? GetOldestByReason(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var normalizedReason = reason.Trim();

        lock (gate)
        {
            return GetOldestFilteredState(RemediationStateFilter.Reason, normalizedReason);
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

    public EventDispatchRemediationRuntimeSummary GetSummaryByOutcome(string outcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);
        var normalizedOutcome = outcome.Trim();

        lock (gate)
        {
            return CreateFilteredSummary(RemediationStateFilter.Outcome, normalizedOutcome);
        }
    }

    public EventDispatchRemediationRuntimeRetention GetRetentionByOutcome(string outcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);
        var normalizedOutcome = outcome.Trim();

        lock (gate)
        {
            return CreateFilteredRetention(RemediationStateFilter.Outcome, normalizedOutcome);
        }
    }

    public EventDispatchRemediationRuntimeState? GetLatestByOutcome(string outcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);
        var normalizedOutcome = outcome.Trim();

        lock (gate)
        {
            return GetLatestFilteredState(RemediationStateFilter.Outcome, normalizedOutcome);
        }
    }

    public EventDispatchRemediationRuntimeState? GetOldestByOutcome(string outcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);
        var normalizedOutcome = outcome.Trim();

        lock (gate)
        {
            return GetOldestFilteredState(RemediationStateFilter.Outcome, normalizedOutcome);
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

    public EventDispatchRemediationRuntimeSummary GetSummaryByDispatchOutcome(string dispatchOutcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dispatchOutcome);
        var normalizedDispatchOutcome = dispatchOutcome.Trim();

        lock (gate)
        {
            return CreateFilteredSummary(RemediationStateFilter.DispatchOutcome, normalizedDispatchOutcome);
        }
    }

    public EventDispatchRemediationRuntimeRetention GetRetentionByDispatchOutcome(string dispatchOutcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dispatchOutcome);
        var normalizedDispatchOutcome = dispatchOutcome.Trim();

        lock (gate)
        {
            return CreateFilteredRetention(RemediationStateFilter.DispatchOutcome, normalizedDispatchOutcome);
        }
    }

    public EventDispatchRemediationRuntimeState? GetLatestByDispatchOutcome(string dispatchOutcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dispatchOutcome);
        var normalizedDispatchOutcome = dispatchOutcome.Trim();

        lock (gate)
        {
            return GetLatestFilteredState(RemediationStateFilter.DispatchOutcome, normalizedDispatchOutcome);
        }
    }

    public EventDispatchRemediationRuntimeState? GetOldestByDispatchOutcome(string dispatchOutcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dispatchOutcome);
        var normalizedDispatchOutcome = dispatchOutcome.Trim();

        lock (gate)
        {
            return GetOldestFilteredState(RemediationStateFilter.DispatchOutcome, normalizedDispatchOutcome);
        }
    }

    public void Record(EventDispatchRemediationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        Record(new EventDispatchRemediationRuntimeState(
            CommandId: result.CommandId,
            OutboxId: result.OutboxId,
            MessageId: result.MessageId,
            ChannelId: result.ChannelId,
            OperationId: result.OperationId,
            Outcome: result.Outcome,
            DispatchOutcome: result.DispatchOutcome,
            ObservedAtUtc: result.ObservedAtUtc,
            Error: result.Error,
            Metadata: result.Metadata));
    }

    public ValueTask<EventDispatchRemediationCommandReservation> ReserveAsync(
        EventDispatchRemediationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Reserve(request));
    }

    public ValueTask RecordAsync(
        EventDispatchRemediationResult result,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Record(result);
        return ValueTask.CompletedTask;
    }

    private EventDispatchRemediationCommandReservation Reserve(EventDispatchRemediationRequest request)
    {
        var state = CreateReservedState(request);

        lock (gate)
        {
            if (statesByCommandId.TryGetValue(state.CommandId, out var existingState) ||
                reservationsByCommandId.TryGetValue(state.CommandId, out existingState))
            {
                return CreateDuplicateReservation(state.CommandId, existingState);
            }

            reservationsByCommandId[state.CommandId] = state;

            var limit = options.RemediationCommandHistoryLimit;
            if (limit > 0)
            {
                states.Add(state);
                statesByCommandId[state.CommandId] = state;
                totalRecordedCommandCount++;

                while (states.Count > limit)
                {
                    var oldest = GetOldestState(states)!;
                    states.Remove(oldest);
                    statesByCommandId.Remove(oldest.CommandId);
                    droppedCommandCount++;
                }
            }
        }

        return new EventDispatchRemediationCommandReservation(
            state.CommandId,
            reserved: true,
            existingCommand: null,
            state.Metadata);
    }

    private void Record(EventDispatchRemediationRuntimeState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var limit = options.RemediationCommandHistoryLimit;
        var metadata = state.Metadata.Count == 0
            ? EmptyMetadata
            : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(state.Metadata, StringComparer.OrdinalIgnoreCase));
        var recordedState = new EventDispatchRemediationRuntimeState(
            CommandId: state.CommandId,
            OutboxId: state.OutboxId,
            MessageId: state.MessageId,
            ChannelId: state.ChannelId,
            OperationId: state.OperationId,
            Outcome: state.Outcome,
            DispatchOutcome: state.DispatchOutcome,
            ObservedAtUtc: state.ObservedAtUtc,
            Error: state.Error,
            Metadata: metadata);

        lock (gate)
        {
            var replacedReservation = reservationsByCommandId.Remove(recordedState.CommandId);
            if (limit == 0)
            {
                return;
            }

            var replacedExisting = statesByCommandId.Remove(recordedState.CommandId);
            if (replacedExisting)
            {
                states.RemoveAll(existing => string.Equals(existing.CommandId, recordedState.CommandId, StringComparison.OrdinalIgnoreCase));
            }
            else if (!replacedReservation)
            {
                totalRecordedCommandCount++;
            }

            states.Add(recordedState);
            statesByCommandId[recordedState.CommandId] = recordedState;

            while (states.Count > limit)
            {
                var oldest = GetOldestState(states)!;
                states.Remove(oldest);
                statesByCommandId.Remove(oldest.CommandId);
                droppedCommandCount++;
            }
        }
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

    private static EventDispatchRemediationRuntimeState CreateReservedState(EventDispatchRemediationRequest request)
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

        return new EventDispatchRemediationRuntimeState(
            CommandId: request.CommandId,
            OutboxId: request.OutboxId,
            MessageId: request.MessageId,
            ChannelId: request.ChannelId,
            OperationId: request.OperationId.Trim().ToLowerInvariant(),
            Outcome: EventDispatchRemediationOutcomes.Reserved,
            DispatchOutcome: "pending",
            ObservedAtUtc: observedAtUtc,
            Error: null,
            Metadata: new ReadOnlyDictionary<string, string>(metadata));
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
        metadata["operatorCommandRequestedAtUtc"] = observedAtUtc.ToString("O", CultureInfo.InvariantCulture);
        EventDispatchRemediationCommandMetadata.AddIdempotencyMetadata(metadata);
        metadata[EventDispatchRemediationMetadataKeys.CommandReservationState] = state;
        return metadata;
    }
}
