using Cephalon.Abstractions.Execution;

namespace Cephalon.Behaviors.Patterns.Runtime;

internal sealed class DurableExecutionRuntimeStateCatalog(
    IDurableExecutionRuntimeCatalog descriptorCatalog) : IDurableExecutionRuntimeStateCatalog, IDurableExecutionRuntimeReporter
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;
    private readonly Lock gate = new();
    private readonly Dictionary<string, DurableExecutionRuntimeDescriptor> descriptorsById = descriptorCatalog
        .DurableExecutions
        .ToDictionary(static descriptor => descriptor.Id, Comparer);
    private readonly Dictionary<string, DurableExecutionRuntimeState> statesByStreamId = new(Comparer);

    public IReadOnlyList<DurableExecutionRuntimeState> States
    {
        get
        {
            lock (gate)
            {
                return statesByStreamId.Values
                    .OrderBy(static state => state.SourceModuleId ?? string.Empty, Comparer)
                    .ThenBy(static state => state.BehaviorId, Comparer)
                    .ThenBy(static state => state.StreamId, Comparer)
                    .ToArray();
            }
        }
    }

    public DurableExecutionRuntimeState? GetByStreamId(string streamId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

        lock (gate)
        {
            return statesByStreamId.GetValueOrDefault(streamId.Trim());
        }
    }

    public IReadOnlyList<DurableExecutionRuntimeState> GetByBehaviorId(string behaviorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);

        lock (gate)
        {
            return statesByStreamId.Values
                .Where(state => Comparer.Equals(state.BehaviorId, behaviorId.Trim()))
                .OrderBy(static state => state.StreamId, Comparer)
                .ToArray();
        }
    }

    public IReadOnlyList<DurableExecutionRuntimeState> GetBySourceModule(string sourceModuleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceModuleId);

        lock (gate)
        {
            return statesByStreamId.Values
                .Where(state => Comparer.Equals(state.SourceModuleId, sourceModuleId.Trim()))
                .OrderBy(static state => state.BehaviorId, Comparer)
                .ThenBy(static state => state.StreamId, Comparer)
                .ToArray();
        }
    }

    public IReadOnlyList<DurableExecutionRuntimeState> GetByTransportId(string transportId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);

        lock (gate)
        {
            return statesByStreamId.Values
                .Where(state => state.TransportIds.Contains(transportId.Trim(), Comparer))
                .OrderBy(static state => state.BehaviorId, Comparer)
                .ThenBy(static state => state.StreamId, Comparer)
                .ToArray();
        }
    }

    public IReadOnlyList<DurableExecutionRuntimeState> GetWithPendingTimers()
    {
        lock (gate)
        {
            return statesByStreamId.Values
                .Where(static state => state.HasPendingTimers)
                .OrderBy(static state => state.NextTimerDueAtUtc)
                .ThenBy(static state => state.BehaviorId, Comparer)
                .ThenBy(static state => state.StreamId, Comparer)
                .ToArray();
        }
    }

    public IReadOnlyList<DurableExecutionRuntimeState> GetWithPendingSignals()
    {
        lock (gate)
        {
            return statesByStreamId.Values
                .Where(static state => state.HasPendingSignals)
                .OrderBy(static state => state.BehaviorId, Comparer)
                .ThenBy(static state => state.StreamId, Comparer)
                .ToArray();
        }
    }

    public IReadOnlyList<DurableExecutionRuntimeState> GetByPendingTimerId(string timerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(timerId);
        var normalizedTimerId = timerId.Trim();

        lock (gate)
        {
            return statesByStreamId.Values
                .Where(state => state.PendingTimers.Any(timer => Comparer.Equals(timer.Id, normalizedTimerId)))
                .OrderBy(static state => state.NextTimerDueAtUtc)
                .ThenBy(static state => state.BehaviorId, Comparer)
                .ThenBy(static state => state.StreamId, Comparer)
                .ToArray();
        }
    }

    public IReadOnlyList<DurableExecutionRuntimeState> GetByPendingSignalId(string signalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signalId);
        var normalizedSignalId = signalId.Trim();

        lock (gate)
        {
            return statesByStreamId.Values
                .Where(state => state.PendingSignals.Any(signal => Comparer.Equals(signal.Id, normalizedSignalId)))
                .OrderBy(static state => state.BehaviorId, Comparer)
                .ThenBy(static state => state.StreamId, Comparer)
                .ToArray();
        }
    }

    public bool TryGetByStreamId(string streamId, out DurableExecutionRuntimeState? state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

        lock (gate)
        {
            return statesByStreamId.TryGetValue(streamId.Trim(), out state);
        }
    }

    public ValueTask ReportAsync(
        DurableExecutionExecutionReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        cancellationToken.ThrowIfCancellationRequested();

        if (!descriptorsById.TryGetValue(report.BehaviorId, out var descriptor))
        {
            throw new InvalidOperationException(
                $"Durable execution behavior '{report.BehaviorId}' is not registered in the active runtime.");
        }

        var normalizedOutcome = NormalizeOutcome(report.Outcome);
        var normalizedStage = NormalizeStage(report.Stage);
        var metadata = report.Metadata.Count == 0
            ? EmptyMetadata
            : new Dictionary<string, string>(report.Metadata, Comparer);

        lock (gate)
        {
            var current = statesByStreamId.TryGetValue(report.StreamId, out var existing)
                ? existing
                : new DurableExecutionRuntimeState(
                    BehaviorId: descriptor.Id,
                    StreamId: report.StreamId,
                    SourceModuleId: descriptor.SourceModuleId,
                    TransportIds: descriptor.TransportIds,
                    LastOutcome: null,
                    LastStage: null,
                    LastObservedAtUtc: null,
                    LastReplayedVersion: null,
                    LastKnownVersion: null,
                    LastHttpStatusCode: null,
                    LastAppendedEventCount: 0,
                    LastStepProducedOutput: false,
                    LastStepCompleted: false,
                    StartedCount: 0,
                    SucceededCount: 0,
                    ContinuationCount: 0,
                    CompletedCount: 0,
                    FailedCount: 0,
                    PendingTimers: [],
                    PendingSignals: [],
                    LastError: null,
                    Metadata: EmptyMetadata);

            current = normalizedOutcome switch
            {
                DurableExecutionRuntimeOutcomes.Started => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastStage = normalizedStage,
                    LastObservedAtUtc = report.ObservedAtUtc,
                    LastReplayedVersion = report.ReplayedVersion,
                    LastKnownVersion = report.KnownVersion,
                    LastHttpStatusCode = report.HttpStatusCode,
                    LastAppendedEventCount = report.AppendedEventCount,
                    LastStepProducedOutput = report.ProducedOutput,
                    LastStepCompleted = report.IsCompleted,
                    StartedCount = current.StartedCount + 1,
                    LastError = null,
                    Metadata = metadata
                },
                DurableExecutionRuntimeOutcomes.Succeeded => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastStage = normalizedStage,
                    LastObservedAtUtc = report.ObservedAtUtc,
                    LastReplayedVersion = report.ReplayedVersion,
                    LastKnownVersion = report.KnownVersion,
                    LastHttpStatusCode = report.HttpStatusCode,
                    LastAppendedEventCount = report.AppendedEventCount,
                    LastStepProducedOutput = report.ProducedOutput,
                    LastStepCompleted = report.IsCompleted,
                    SucceededCount = current.SucceededCount + 1,
                    PendingTimers = report.PendingTimers,
                    PendingSignals = report.PendingSignals,
                    LastError = null,
                    Metadata = metadata
                },
                DurableExecutionRuntimeOutcomes.ContinuationStaged => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastStage = normalizedStage,
                    LastObservedAtUtc = report.ObservedAtUtc,
                    LastReplayedVersion = report.ReplayedVersion,
                    LastKnownVersion = report.KnownVersion,
                    LastHttpStatusCode = report.HttpStatusCode,
                    LastAppendedEventCount = report.AppendedEventCount,
                    LastStepProducedOutput = report.ProducedOutput,
                    LastStepCompleted = report.IsCompleted,
                    ContinuationCount = current.ContinuationCount + 1,
                    PendingTimers = report.PendingTimers,
                    PendingSignals = report.PendingSignals,
                    LastError = null,
                    Metadata = metadata
                },
                DurableExecutionRuntimeOutcomes.Waiting => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastStage = normalizedStage,
                    LastObservedAtUtc = report.ObservedAtUtc,
                    LastReplayedVersion = report.ReplayedVersion,
                    LastKnownVersion = report.KnownVersion,
                    LastHttpStatusCode = report.HttpStatusCode,
                    LastAppendedEventCount = report.AppendedEventCount,
                    LastStepProducedOutput = report.ProducedOutput,
                    LastStepCompleted = report.IsCompleted,
                    ContinuationCount = current.ContinuationCount + 1,
                    PendingTimers = report.PendingTimers,
                    PendingSignals = report.PendingSignals,
                    LastError = null,
                    Metadata = metadata
                },
                DurableExecutionRuntimeOutcomes.Completed => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastStage = normalizedStage,
                    LastObservedAtUtc = report.ObservedAtUtc,
                    LastReplayedVersion = report.ReplayedVersion,
                    LastKnownVersion = report.KnownVersion,
                    LastHttpStatusCode = report.HttpStatusCode,
                    LastAppendedEventCount = report.AppendedEventCount,
                    LastStepProducedOutput = report.ProducedOutput,
                    LastStepCompleted = report.IsCompleted,
                    CompletedCount = current.CompletedCount + 1,
                    PendingTimers = report.PendingTimers,
                    PendingSignals = report.PendingSignals,
                    LastError = null,
                    Metadata = metadata
                },
                DurableExecutionRuntimeOutcomes.Failed => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastStage = normalizedStage,
                    LastObservedAtUtc = report.ObservedAtUtc,
                    LastReplayedVersion = report.ReplayedVersion,
                    LastKnownVersion = report.KnownVersion,
                    LastHttpStatusCode = report.HttpStatusCode,
                    LastAppendedEventCount = report.AppendedEventCount,
                    LastStepProducedOutput = report.ProducedOutput,
                    LastStepCompleted = report.IsCompleted,
                    FailedCount = current.FailedCount + 1,
                    LastError = report.Error,
                    Metadata = metadata
                },
                _ => throw new InvalidOperationException(
                    $"Durable execution outcome '{report.Outcome}' is not supported by the active runtime.")
            };

            statesByStreamId[report.StreamId] = current;
        }

        return ValueTask.CompletedTask;
    }

    private static string NormalizeOutcome(string outcome)
    {
        var normalized = outcome.Trim().ToLowerInvariant();
        return normalized switch
        {
            DurableExecutionRuntimeOutcomes.Started => DurableExecutionRuntimeOutcomes.Started,
            DurableExecutionRuntimeOutcomes.Succeeded => DurableExecutionRuntimeOutcomes.Succeeded,
            DurableExecutionRuntimeOutcomes.ContinuationStaged => DurableExecutionRuntimeOutcomes.ContinuationStaged,
            DurableExecutionRuntimeOutcomes.Waiting => DurableExecutionRuntimeOutcomes.Waiting,
            DurableExecutionRuntimeOutcomes.Completed => DurableExecutionRuntimeOutcomes.Completed,
            DurableExecutionRuntimeOutcomes.Failed => DurableExecutionRuntimeOutcomes.Failed,
            _ => throw new InvalidOperationException(
                $"Durable execution outcome '{outcome}' is not supported by the active runtime.")
        };
    }

    private static string NormalizeStage(string stage)
    {
        var normalized = stage.Trim().ToLowerInvariant();
        return normalized switch
        {
            DurableExecutionRuntimeStages.Replay => DurableExecutionRuntimeStages.Replay,
            DurableExecutionRuntimeStages.Execute => DurableExecutionRuntimeStages.Execute,
            DurableExecutionRuntimeStages.Append => DurableExecutionRuntimeStages.Append,
            _ => throw new InvalidOperationException(
                $"Durable execution stage '{stage}' is not supported by the active runtime.")
        };
    }
}
