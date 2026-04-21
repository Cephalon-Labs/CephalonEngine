using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

internal sealed class CdcCaptureRuntimeStateCatalog(
    ICdcCaptureCatalog descriptorCatalog,
    IEventDispatchRuntimeCatalog? dispatchRuntimeCatalog = null) : ICdcCaptureRuntimeStateCatalog, ICdcCaptureRuntimeReporter
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new Dictionary<string, string>(Comparer);

    private readonly Lock gate = new();
    private readonly IReadOnlyList<CdcCaptureDescriptor> descriptors = descriptorCatalog.CdcCaptures;
    private readonly Dictionary<string, CdcCaptureDescriptor> descriptorsById = descriptorCatalog.CdcCaptures
        .ToDictionary(static descriptor => descriptor.Id, Comparer);
    private readonly Dictionary<string, CdcCaptureRuntimeState> reportedStatesById = new(Comparer);

    private static readonly CdcCaptureFreshnessStatus UnknownFreshness =
        new(CdcCaptureFreshnessStates.Unknown);
    private static readonly CdcCaptureLagStatus UnknownLag =
        new(CdcCaptureLagStates.Unknown);
    private static readonly CdcCapturePublicationStatus UnknownPublication =
        new(CdcCapturePublicationStates.Unknown);

    public IReadOnlyList<CdcCaptureRuntimeState> States
    {
        get
        {
            lock (gate)
            {
                return descriptors
                    .Select(CreateState)
                    .OrderBy(static state => state.SourceModuleId, Comparer)
                    .ThenBy(static state => state.Provider, Comparer)
                    .ThenBy(static state => state.CdcCaptureId, Comparer)
                    .ToArray();
            }
        }
    }

    public CdcCaptureRuntimeState? GetById(string cdcCaptureId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cdcCaptureId);

        lock (gate)
        {
            return descriptorsById.TryGetValue(cdcCaptureId.Trim(), out var descriptor)
                ? CreateState(descriptor)
                : null;
        }
    }

    public IReadOnlyList<CdcCaptureRuntimeState> GetBySourceModule(string sourceModuleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceModuleId);
        var normalizedModuleId = sourceModuleId.Trim();

        lock (gate)
        {
            return descriptors
                .Where(descriptor => Comparer.Equals(descriptor.SourceModuleId, normalizedModuleId))
                .Select(CreateState)
                .OrderBy(static state => state.Provider, Comparer)
                .ThenBy(static state => state.CdcCaptureId, Comparer)
                .ToArray();
        }
    }

    public IReadOnlyList<CdcCaptureRuntimeState> GetByProvider(string provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        var normalizedProvider = provider.Trim();

        lock (gate)
        {
            return descriptors
                .Where(descriptor => Comparer.Equals(descriptor.Provider, normalizedProvider))
                .Select(CreateState)
                .OrderBy(static state => state.SourceModuleId, Comparer)
                .ThenBy(static state => state.CdcCaptureId, Comparer)
                .ToArray();
        }
    }

    public IReadOnlyList<CdcCaptureRuntimeState> GetByOutboxId(string outboxId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxId);
        var normalizedOutboxId = outboxId.Trim();

        lock (gate)
        {
            return descriptors
                .Where(descriptor => Comparer.Equals(descriptor.OutboxId, normalizedOutboxId))
                .Select(CreateState)
                .OrderBy(static state => state.SourceModuleId, Comparer)
                .ThenBy(static state => state.CdcCaptureId, Comparer)
                .ToArray();
        }
    }

    public IReadOnlyList<CdcCaptureRuntimeState> GetBySourceId(string sourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        var normalizedSourceId = sourceId.Trim();

        lock (gate)
        {
            return descriptors
                .Where(descriptor => Comparer.Equals(descriptor.SourceId, normalizedSourceId))
                .Select(CreateState)
                .OrderBy(static state => state.SourceModuleId, Comparer)
                .ThenBy(static state => state.CdcCaptureId, Comparer)
                .ToArray();
        }
    }

    public IReadOnlyList<CdcCaptureRuntimeState> GetByResourceId(string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        var normalizedResourceId = resourceId.Trim();

        lock (gate)
        {
            return descriptors
                .Where(descriptor => descriptor.ResourceIds.Contains(normalizedResourceId, Comparer))
                .Select(CreateState)
                .OrderBy(static state => state.SourceModuleId, Comparer)
                .ThenBy(static state => state.CdcCaptureId, Comparer)
                .ToArray();
        }
    }

    public ValueTask ReportAsync(
        CdcCaptureExecutionReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        cancellationToken.ThrowIfCancellationRequested();

        if (!descriptorsById.TryGetValue(report.CdcCaptureId, out var descriptor))
        {
            throw new InvalidOperationException(
                $"CDC capture '{report.CdcCaptureId}' is not registered in the active runtime.");
        }

        var normalizedOutcome = NormalizeOutcome(report.Outcome);
        var metadata = report.Metadata.Count == 0
            ? EmptyMetadata
            : new Dictionary<string, string>(report.Metadata, Comparer);

        lock (gate)
        {
            var current = reportedStatesById.TryGetValue(report.CdcCaptureId, out var existing)
                ? existing
                : CreateDefaultState(descriptor);
            var totalCapturedChangeCount = current.TotalCapturedChangeCount + report.CapturedChangeCount;
            var totalProducedMessageCount = current.TotalProducedMessageCount + report.ProducedMessageCount;
            var dispatchState = dispatchRuntimeCatalog?.GetByOutboxId(descriptor.OutboxId);
            var freshness = report.Freshness ?? current.Freshness;
            var lag = report.Lag ?? current.Lag;

            current = normalizedOutcome switch
            {
                CdcCaptureRuntimeOutcomes.Started => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = report.ObservedAtUtc,
                    LastCapturedChangeCount = report.CapturedChangeCount,
                    LastProducedMessageCount = report.ProducedMessageCount,
                    StartedCount = current.StartedCount + 1,
                    TotalCapturedChangeCount = totalCapturedChangeCount,
                    TotalProducedMessageCount = totalProducedMessageCount,
                    LastChangeId = report.ChangeId,
                    LastCheckpoint = report.Checkpoint,
                    LastError = null,
                    Freshness = freshness,
                    Lag = lag,
                    Publication = ResolvePublicationStatus(
                        current.Publication,
                        report.Publication,
                        dispatchState,
                        normalizedOutcome,
                        error: null),
                    OutboxDispatchState = dispatchState,
                    Metadata = metadata
                },
                CdcCaptureRuntimeOutcomes.Captured => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = report.ObservedAtUtc,
                    LastCapturedChangeCount = report.CapturedChangeCount,
                    LastProducedMessageCount = report.ProducedMessageCount,
                    CapturedCount = current.CapturedCount + 1,
                    TotalCapturedChangeCount = totalCapturedChangeCount,
                    TotalProducedMessageCount = totalProducedMessageCount,
                    LastChangeId = report.ChangeId,
                    LastCheckpoint = report.Checkpoint,
                    LastError = null,
                    Freshness = freshness,
                    Lag = lag,
                    Publication = ResolvePublicationStatus(
                        current.Publication,
                        report.Publication,
                        dispatchState,
                        normalizedOutcome,
                        error: null),
                    OutboxDispatchState = dispatchState,
                    Metadata = metadata
                },
                CdcCaptureRuntimeOutcomes.Idle => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = report.ObservedAtUtc,
                    LastCapturedChangeCount = report.CapturedChangeCount,
                    LastProducedMessageCount = report.ProducedMessageCount,
                    IdleCount = current.IdleCount + 1,
                    TotalCapturedChangeCount = totalCapturedChangeCount,
                    TotalProducedMessageCount = totalProducedMessageCount,
                    LastChangeId = report.ChangeId,
                    LastCheckpoint = report.Checkpoint,
                    LastError = null,
                    Freshness = freshness,
                    Lag = lag,
                    Publication = ResolvePublicationStatus(
                        current.Publication,
                        report.Publication,
                        dispatchState,
                        normalizedOutcome,
                        error: null),
                    OutboxDispatchState = dispatchState,
                    Metadata = metadata
                },
                CdcCaptureRuntimeOutcomes.Failed => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = report.ObservedAtUtc,
                    LastCapturedChangeCount = report.CapturedChangeCount,
                    LastProducedMessageCount = report.ProducedMessageCount,
                    FailedCount = current.FailedCount + 1,
                    TotalCapturedChangeCount = totalCapturedChangeCount,
                    TotalProducedMessageCount = totalProducedMessageCount,
                    LastChangeId = report.ChangeId,
                    LastCheckpoint = report.Checkpoint,
                    LastError = report.Error,
                    Freshness = freshness,
                    Lag = lag,
                    Publication = ResolvePublicationStatus(
                        current.Publication,
                        report.Publication,
                        dispatchState,
                        normalizedOutcome,
                        report.Error),
                    OutboxDispatchState = dispatchState,
                    Metadata = metadata
                },
                _ => throw new InvalidOperationException(
                    $"CDC capture outcome '{report.Outcome}' is not supported by the active runtime.")
            };

            reportedStatesById[report.CdcCaptureId] = current;
        }

        return ValueTask.CompletedTask;
    }

    private CdcCaptureRuntimeState CreateState(CdcCaptureDescriptor descriptor)
    {
        if (reportedStatesById.TryGetValue(descriptor.Id, out var existing))
        {
            var dispatchState = dispatchRuntimeCatalog?.GetByOutboxId(descriptor.OutboxId);
            return existing with
            {
                Publication = ResolvePublicationStatus(
                    existing.Publication,
                    reportedPublication: null,
                    dispatchState,
                    existing.LastOutcome,
                    existing.LastError),
                OutboxDispatchState = dispatchState
            };
        }

        return CreateDefaultState(descriptor);
    }

    private CdcCaptureRuntimeState CreateDefaultState(CdcCaptureDescriptor descriptor)
    {
        return new CdcCaptureRuntimeState(
            CdcCaptureId: descriptor.Id,
            SourceModuleId: descriptor.SourceModuleId,
            Provider: descriptor.Provider,
            SourceId: descriptor.SourceId,
            OutboxId: descriptor.OutboxId,
            Mode: descriptor.Mode,
            EventFormat: descriptor.EventFormat,
            ResourceIds: descriptor.ResourceIds,
            LastOutcome: null,
            LastObservedAtUtc: null,
            LastCapturedChangeCount: 0,
            LastProducedMessageCount: 0,
            StartedCount: 0,
            CapturedCount: 0,
            IdleCount: 0,
            FailedCount: 0,
            TotalCapturedChangeCount: 0,
            TotalProducedMessageCount: 0,
            LastChangeId: null,
            LastCheckpoint: null,
            LastError: null,
            Freshness: UnknownFreshness,
            Lag: UnknownLag,
            Publication: ResolvePublicationStatus(
                UnknownPublication,
                reportedPublication: null,
                dispatchRuntimeCatalog?.GetByOutboxId(descriptor.OutboxId),
                lastOutcome: null,
                error: null),
            OutboxDispatchState: dispatchRuntimeCatalog?.GetByOutboxId(descriptor.OutboxId),
            Metadata: EmptyMetadata)
        {
            ExecutionBinding = descriptor.ExecutionBinding
        };
    }

    private static CdcCapturePublicationStatus ResolvePublicationStatus(
        CdcCapturePublicationStatus currentPublication,
        CdcCapturePublicationStatus? reportedPublication,
        EventDispatchRuntimeState? dispatchState,
        string? lastOutcome,
        string? error)
    {
        var publication = reportedPublication ?? currentPublication ?? UnknownPublication;

        if (Comparer.Equals(lastOutcome, CdcCaptureRuntimeOutcomes.Failed))
        {
            return new CdcCapturePublicationStatus(
                CdcCapturePublicationStates.CaptureFailed,
                publication.PendingPublicationCount,
                string.IsNullOrWhiteSpace(error)
                    ? "The CDC capture last reported a failure before publication completed."
                    : error);
        }

        if (dispatchState is null)
        {
            return publication;
        }

        return dispatchState.LastOutcome switch
        {
            "retry-scheduled" => new CdcCapturePublicationStatus(
                CdcCapturePublicationStates.DispatchRetryPending,
                publication.PendingPublicationCount,
                dispatchState.LastError ?? "The linked outbox dispatch runtime has a retry pending."),
            "failed" => new CdcCapturePublicationStatus(
                CdcCapturePublicationStates.DispatchFailed,
                publication.PendingPublicationCount,
                dispatchState.LastError ?? "The linked outbox dispatch runtime last reported a failure."),
            "started" => new CdcCapturePublicationStatus(
                CdcCapturePublicationStates.Dispatching,
                publication.PendingPublicationCount,
                "The linked outbox dispatch runtime is actively dispatching publications."),
            "succeeded" when publication.PendingPublicationCount is 0 => new CdcCapturePublicationStatus(
                CdcCapturePublicationStates.Current,
                0,
                "The capture does not report pending publications and the linked outbox dispatch runtime last succeeded."),
            "skipped" when publication.PendingPublicationCount is 0 => new CdcCapturePublicationStatus(
                CdcCapturePublicationStates.Current,
                0,
                "The capture does not report pending publications and the linked outbox dispatch runtime does not currently need to publish a message."),
            _ => publication
        };
    }

    private static string NormalizeOutcome(string outcome)
    {
        var normalized = outcome.Trim().ToLowerInvariant();
        return normalized switch
        {
            CdcCaptureRuntimeOutcomes.Started => CdcCaptureRuntimeOutcomes.Started,
            CdcCaptureRuntimeOutcomes.Captured => CdcCaptureRuntimeOutcomes.Captured,
            CdcCaptureRuntimeOutcomes.Idle => CdcCaptureRuntimeOutcomes.Idle,
            CdcCaptureRuntimeOutcomes.Failed => CdcCaptureRuntimeOutcomes.Failed,
            _ => throw new InvalidOperationException(
                $"CDC capture outcome '{outcome}' is not supported by the active runtime.")
        };
    }
}
