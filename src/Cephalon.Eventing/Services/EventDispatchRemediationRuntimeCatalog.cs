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
                    summaryMayBeIncomplete: droppedCommandCount > 0,
                    oldestRetainedState: GetOldestState(states));
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
            var oldestState = GetOldestState(states);
            var matchingStates = states
                .Where(state =>
                    IsReservedState(state) &&
                    (beforeObservedAtUtc is null || state.ObservedAtUtc <= beforeObservedAtUtc.Value))
                .ToList();

            return CreateSummary(
                matchingStates,
                droppedCommandCount,
                summaryMayBeIncomplete: droppedCommandCount > 0,
                oldestRetainedState: oldestState);
        }
    }

    private static EventDispatchRemediationRuntimeSummary CreateSummary(
        List<EventDispatchRemediationRuntimeState> recordedStates,
        long droppedCommandCount = 0,
        bool summaryMayBeIncomplete = false,
        EventDispatchRemediationRuntimeState? oldestRetainedState = null)
    {
        var oldestState = oldestRetainedState ?? GetOldestState(recordedStates);
        if (recordedStates.Count == 0)
        {
            return droppedCommandCount == 0 && !summaryMayBeIncomplete && oldestState is null
                ? EventDispatchRemediationRuntimeSummary.Empty
                : new EventDispatchRemediationRuntimeSummary(
                    droppedCommandCount: droppedCommandCount,
                    retentionTruncated: droppedCommandCount > 0,
                    summaryMayBeIncomplete: summaryMayBeIncomplete,
                    oldestRetainedCommandId: oldestState?.CommandId,
                    oldestRetainedObservedAtUtc: oldestState?.ObservedAtUtc);
        }

        var lastState = GetLatestState(recordedStates)!;
        var oldestReservedState = GetOldestReservedState(recordedStates);

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
            reservedCount: recordedStates.Count(IsReservedState),
            lastCommandId: lastState.CommandId,
            lastOperationId: lastState.OperationId,
            lastOutcome: lastState.Outcome,
            lastDispatchOutcome: lastState.DispatchOutcome,
            lastObservedAtUtc: lastState.ObservedAtUtc,
            droppedCommandCount: droppedCommandCount,
            retentionTruncated: droppedCommandCount > 0,
            summaryMayBeIncomplete: summaryMayBeIncomplete,
            oldestRetainedCommandId: oldestState?.CommandId,
            oldestRetainedObservedAtUtc: oldestState?.ObservedAtUtc,
            oldestReservedCommandId: oldestReservedState?.CommandId,
            oldestReservedObservedAtUtc: oldestReservedState?.ObservedAtUtc);
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

    private static EventDispatchRemediationRuntimeState? GetOldestReservedState(List<EventDispatchRemediationRuntimeState> recordedStates)
    {
        return recordedStates
            .Where(IsReservedState)
            .OrderBy(static state => state.ObservedAtUtc)
            .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
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
            var oldestState = GetOldestState(states);
            var matchingStates = states
                .Where(state =>
                    (fromObservedAtUtc is null || state.ObservedAtUtc >= fromObservedAtUtc.Value) &&
                    (toObservedAtUtc is null || state.ObservedAtUtc <= toObservedAtUtc.Value))
                .ToList();

            return CreateSummary(
                matchingStates,
                droppedCommandCount,
                summaryMayBeIncomplete: IsObservationWindowPotentiallyTruncated(
                    fromObservedAtUtc,
                    oldestState,
                    droppedCommandCount),
                oldestRetainedState: oldestState);
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
