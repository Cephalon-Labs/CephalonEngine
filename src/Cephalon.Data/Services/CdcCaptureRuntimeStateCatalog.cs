using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

internal sealed class CdcCaptureRuntimeStateCatalog(
    ICdcCaptureCatalog descriptorCatalog,
    CdcCaptureExecutionRuntimeDescriptorCatalog runtimeDescriptorCatalog,
    IEventDispatchRuntimeCatalog? dispatchRuntimeCatalog = null,
    TimeProvider? timeProvider = null) : ICdcCaptureRuntimeStateCatalog, ICdcCaptureRuntimeReporter, ICdcCaptureExecutionRuntimeReportSink
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new Dictionary<string, string>(Comparer);

    private readonly Lock gate = new();
    private readonly IReadOnlyList<CdcCaptureDescriptor> descriptors = descriptorCatalog.CdcCaptures;
    private readonly Dictionary<string, CdcCaptureDescriptor> descriptorsById = descriptorCatalog.CdcCaptures
        .ToDictionary(static descriptor => descriptor.Id, Comparer);
    private readonly Dictionary<string, CdcCaptureExecutionRuntimeDescriptor> runtimeDescriptorsById = runtimeDescriptorCatalog.Runtimes
        .ToDictionary(static runtime => runtime.Id, Comparer);
    private readonly Dictionary<string, CdcCaptureRuntimeState> reportedStatesById = new(Comparer);
    private readonly Dictionary<string, RuntimeReporterCoordinationMemory> runtimeReporterCoordinationById = new(Comparer);
    private readonly TimeProvider timeProvider = timeProvider ?? TimeProvider.System;

    private static readonly CdcCaptureFreshnessStatus UnknownFreshness =
        new(CdcCaptureFreshnessStates.Unknown);
    private static readonly CdcCaptureFreshnessStatus UnknownObservationFreshness =
        new(CdcCaptureFreshnessStates.Unknown);
    private static readonly CdcCaptureLagStatus UnknownLag =
        new(CdcCaptureLagStates.Unknown);
    private static readonly CdcCapturePublicationStatus UnknownPublication =
        new(CdcCapturePublicationStates.Unknown);
    private static readonly CdcCaptureReporterCoordinationStatus UnknownReporterCoordination =
        new(CdcCaptureReporterCoordinationStates.Unknown);

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

    public IReadOnlyList<CdcCaptureRuntimeState> GetByExecutionRuntimeId(string executionRuntimeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionRuntimeId);
        var normalizedExecutionRuntimeId = executionRuntimeId.Trim();

        lock (gate)
        {
            return descriptorCatalog
                .GetByExecutionRuntimeId(normalizedExecutionRuntimeId)
                .Select(CreateState)
                .OrderBy(static state => state.SourceModuleId, Comparer)
                .ThenBy(static state => state.Provider, Comparer)
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

        ApplyReport(report, executionRuntime: null);

        return ValueTask.CompletedTask;
    }

    public ValueTask ReportAsync(
        string executionRuntimeId,
        IReadOnlyList<CdcCaptureRuntimeObservation> observations,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionRuntimeId);
        ArgumentNullException.ThrowIfNull(observations);
        cancellationToken.ThrowIfCancellationRequested();

        if (observations.Count == 0)
        {
            throw new ArgumentException("At least one CDC capture runtime observation is required.", nameof(observations));
        }

        var normalizedExecutionRuntimeId = executionRuntimeId.Trim();
        var runtimeDescriptor = ResolveExecutionRuntimeDescriptor(normalizedExecutionRuntimeId);

        foreach (var observation in observations)
        {
            ArgumentNullException.ThrowIfNull(observation);

            if (!descriptorsById.TryGetValue(observation.CdcCaptureId, out var descriptor))
            {
                throw new InvalidOperationException(
                    $"CDC capture '{observation.CdcCaptureId}' is not registered in the active runtime.");
            }

            var effectiveExecutionRuntimeId = descriptor.ExecutionBinding.EffectiveExecutionRuntimeId;
            if (!Comparer.Equals(effectiveExecutionRuntimeId, normalizedExecutionRuntimeId))
            {
                var currentOwner = string.IsNullOrWhiteSpace(effectiveExecutionRuntimeId)
                    ? "unbound"
                    : effectiveExecutionRuntimeId;
                throw new InvalidOperationException(
                    $"CDC capture '{observation.CdcCaptureId}' is currently owned by execution runtime '{currentOwner}' and cannot report through '{normalizedExecutionRuntimeId}'.");
            }

            var observationFreshness = CreateObservationFreshness(runtimeDescriptor, observation.ObservedAtUtc);
            var reporterLeaseExpiresAtUtc = CreateReporterLeaseExpiry(
                runtimeDescriptor,
                observation.ReporterId,
                observation.ObservedAtUtc);
            ApplyReport(new CdcCaptureExecutionReport(
                cdcCaptureId: observation.CdcCaptureId,
                outcome: observation.Outcome,
                observedAtUtc: observation.ObservedAtUtc,
                reportId: observation.ReportId,
                capturedChangeCount: observation.CapturedChangeCount,
                producedMessageCount: observation.ProducedMessageCount,
                changeId: observation.ChangeId,
                checkpoint: observation.Checkpoint,
                error: observation.Error,
                freshness: observation.Freshness,
                observationFreshness: observationFreshness,
                lag: observation.Lag,
                publication: observation.Publication,
                metadata: AddExecutionRuntimeMetadata(
                    normalizedExecutionRuntimeId,
                    observation.Metadata,
                    observation.ReportId,
                    observationFreshness,
                    runtimeDescriptor?.ObservationStaleAfterSeconds,
                    observation.ReporterId,
                    reporterLeaseExpiresAtUtc,
                    observation.EdgeNodeId),
                reporterId: observation.ReporterId,
                edgeNodeId: observation.EdgeNodeId),
                runtimeDescriptor);
        }

        return ValueTask.CompletedTask;
    }

    private CdcCaptureRuntimeState CreateState(CdcCaptureDescriptor descriptor)
    {
        if (reportedStatesById.TryGetValue(descriptor.Id, out var existing))
        {
            var dispatchState = dispatchRuntimeCatalog?.GetByOutboxId(descriptor.OutboxId);
            var reporterCoordination = ResolveReporterCoordination(descriptor, existing);
            return existing with
            {
                ObservationFreshness = ResolveObservationFreshness(existing.ObservationFreshness),
                Publication = ResolvePublicationStatus(
                    existing.Publication,
                    reportedPublication: null,
                    dispatchState,
                    existing.LastOutcome,
                    existing.LastError),
                OutboxDispatchState = dispatchState,
                ReporterCoordination = reporterCoordination
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
            LastReportId: null,
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
            ObservationFreshness: UnknownObservationFreshness,
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
            ExecutionBinding = descriptor.ExecutionBinding,
            ReporterCoordination = ResolveReporterCoordination(descriptor, current: null)
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

    private void ApplyReport(
        CdcCaptureExecutionReport report,
        CdcCaptureExecutionRuntimeDescriptor? executionRuntime)
    {
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
            executionRuntime ??= ResolveExecutionRuntimeDescriptor(
                descriptor.ExecutionBinding.EffectiveExecutionRuntimeId,
                report.Metadata);
            var normalizedReportId = string.IsNullOrWhiteSpace(report.ReportId)
                ? null
                : report.ReportId.Trim();
            var reporterSnapshot = executionRuntime is null
                ? RuntimeReporterLeaseSnapshot.Empty
                : SnapshotRuntimeReporterLease(executionRuntime.Id, timeProvider.GetUtcNow());

            if (normalizedReportId is not null &&
                Comparer.Equals(current.LastReportId, normalizedReportId))
            {
                if (IsIdempotentDuplicate(current, report))
                {
                    return;
                }

                throw new InvalidOperationException(
                    $"CDC capture '{report.CdcCaptureId}' already recorded report '{normalizedReportId}' with different payload.");
            }

            if (executionRuntime is not null)
            {
                ValidateExecutionRuntimeEdgeNode(report, executionRuntime);
                ValidateExecutionRuntimeReporterIdentity(report, executionRuntime, reporterSnapshot);
            }

            if (executionRuntime?.RejectOutOfOrderReports == true &&
                current.LastObservedAtUtc.HasValue &&
                report.ObservedAtUtc < current.LastObservedAtUtc.Value)
            {
                throw new InvalidOperationException(
                    $"CDC capture '{report.CdcCaptureId}' rejected out-of-order report '{normalizedReportId ?? "(no report id)"}' because the latest observation is already '{current.LastObservedAtUtc.Value:O}'.");
            }

            var totalCapturedChangeCount = current.TotalCapturedChangeCount + report.CapturedChangeCount;
            var totalProducedMessageCount = current.TotalProducedMessageCount + report.ProducedMessageCount;
            var dispatchState = dispatchRuntimeCatalog?.GetByOutboxId(descriptor.OutboxId);
            var freshness = report.Freshness ?? current.Freshness;
            var observationFreshness = report.ObservationFreshness ?? UnknownObservationFreshness;
            var lag = report.Lag ?? current.Lag;
            var reporterLeaseExpiresAtUtc = CreateReporterLeaseExpiry(
                executionRuntime,
                report.ReporterId,
                report.ObservedAtUtc);
            var reporterTakeover = executionRuntime is null
                ? null
                : DetectReporterTakeover(executionRuntime, report, reporterSnapshot);

            current = normalizedOutcome switch
            {
                CdcCaptureRuntimeOutcomes.Started => current with
                {
                    LastOutcome = normalizedOutcome,
                    LastObservedAtUtc = report.ObservedAtUtc,
                    LastReportId = normalizedReportId,
                    LastCapturedChangeCount = report.CapturedChangeCount,
                    LastProducedMessageCount = report.ProducedMessageCount,
                    StartedCount = current.StartedCount + 1,
                    TotalCapturedChangeCount = totalCapturedChangeCount,
                    TotalProducedMessageCount = totalProducedMessageCount,
                    LastChangeId = report.ChangeId,
                    LastCheckpoint = report.Checkpoint,
                    LastError = null,
                    LastReporterId = report.ReporterId,
                    ReporterLeaseExpiresAtUtc = reporterLeaseExpiresAtUtc,
                    LastEdgeNodeId = report.EdgeNodeId,
                    Freshness = freshness,
                    ObservationFreshness = observationFreshness,
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
                    LastReportId = normalizedReportId,
                    LastCapturedChangeCount = report.CapturedChangeCount,
                    LastProducedMessageCount = report.ProducedMessageCount,
                    CapturedCount = current.CapturedCount + 1,
                    TotalCapturedChangeCount = totalCapturedChangeCount,
                    TotalProducedMessageCount = totalProducedMessageCount,
                    LastChangeId = report.ChangeId,
                    LastCheckpoint = report.Checkpoint,
                    LastError = null,
                    LastReporterId = report.ReporterId,
                    ReporterLeaseExpiresAtUtc = reporterLeaseExpiresAtUtc,
                    LastEdgeNodeId = report.EdgeNodeId,
                    Freshness = freshness,
                    ObservationFreshness = observationFreshness,
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
                    LastReportId = normalizedReportId,
                    LastCapturedChangeCount = report.CapturedChangeCount,
                    LastProducedMessageCount = report.ProducedMessageCount,
                    IdleCount = current.IdleCount + 1,
                    TotalCapturedChangeCount = totalCapturedChangeCount,
                    TotalProducedMessageCount = totalProducedMessageCount,
                    LastChangeId = report.ChangeId,
                    LastCheckpoint = report.Checkpoint,
                    LastError = null,
                    LastReporterId = report.ReporterId,
                    ReporterLeaseExpiresAtUtc = reporterLeaseExpiresAtUtc,
                    LastEdgeNodeId = report.EdgeNodeId,
                    Freshness = freshness,
                    ObservationFreshness = observationFreshness,
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
                    LastReportId = normalizedReportId,
                    LastCapturedChangeCount = report.CapturedChangeCount,
                    LastProducedMessageCount = report.ProducedMessageCount,
                    FailedCount = current.FailedCount + 1,
                    TotalCapturedChangeCount = totalCapturedChangeCount,
                    TotalProducedMessageCount = totalProducedMessageCount,
                    LastChangeId = report.ChangeId,
                    LastCheckpoint = report.Checkpoint,
                    LastError = report.Error,
                    LastReporterId = report.ReporterId,
                    ReporterLeaseExpiresAtUtc = reporterLeaseExpiresAtUtc,
                    LastEdgeNodeId = report.EdgeNodeId,
                    Freshness = freshness,
                    ObservationFreshness = observationFreshness,
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

            if (executionRuntime is not null)
            {
                RecordAcceptedReporterObservation(
                    executionRuntime.Id,
                    report,
                    reporterSnapshot,
                    reporterTakeover);

                current = current with
                {
                    ReporterCoordination = ResolveReporterCoordination(descriptor, current)
                };
            }

            reportedStatesById[report.CdcCaptureId] = current;
        }
    }

    private static Dictionary<string, string> AddExecutionRuntimeMetadata(
        string executionRuntimeId,
        IReadOnlyDictionary<string, string> metadata,
        string? reportId,
        CdcCaptureFreshnessStatus? observationFreshness,
        int? observationStaleAfterSeconds,
        string? reporterId,
        DateTimeOffset? reporterLeaseExpiresAtUtc,
        string? edgeNodeId)
    {
        var merged = metadata.Count == 0
            ? new Dictionary<string, string>(Comparer)
            : new Dictionary<string, string>(metadata, Comparer);
        merged["cdcCaptureExecutionRuntimeId"] = executionRuntimeId;
        UpsertOptional(merged, "cdcCaptureReportId", reportId);
        UpsertOptional(merged, "observationFreshUntilUtc", observationFreshness?.FreshUntilUtc?.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        UpsertOptional(merged, "observationFreshnessState", observationFreshness?.State);
        UpsertOptional(merged, "observationStaleAfterSeconds", observationStaleAfterSeconds?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        UpsertOptional(merged, "cdcCaptureReporterId", reporterId);
        UpsertOptional(merged, "cdcCaptureReporterLeaseExpiresAtUtc", reporterLeaseExpiresAtUtc?.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        UpsertOptional(merged, "cdcCaptureEdgeNodeId", edgeNodeId);

        return merged;
    }

    private CdcCaptureExecutionRuntimeDescriptor? ResolveExecutionRuntimeDescriptor(
        string? executionRuntimeId,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        var normalizedExecutionRuntimeId = string.IsNullOrWhiteSpace(executionRuntimeId)
            ? null
            : executionRuntimeId.Trim();
        if (normalizedExecutionRuntimeId is null &&
            metadata is not null &&
            metadata.TryGetValue("cdcCaptureExecutionRuntimeId", out var runtimeIdFromMetadata) &&
            !string.IsNullOrWhiteSpace(runtimeIdFromMetadata))
        {
            normalizedExecutionRuntimeId = runtimeIdFromMetadata.Trim();
        }

        return normalizedExecutionRuntimeId is not null &&
               runtimeDescriptorsById.TryGetValue(normalizedExecutionRuntimeId, out var runtimeDescriptor)
            ? runtimeDescriptor
            : null;
    }

    private CdcCaptureFreshnessStatus ResolveObservationFreshness(CdcCaptureFreshnessStatus freshness)
    {
        if (!freshness.HasWindow ||
            freshness.FreshUntilUtc is null)
        {
            return freshness;
        }

        if (timeProvider.GetUtcNow() <= freshness.FreshUntilUtc.Value)
        {
            return freshness;
        }

        return new CdcCaptureFreshnessStatus(
            CdcCaptureFreshnessStates.Stale,
            freshness.FreshUntilUtc,
            "The latest CDC runtime observation is older than the configured freshness window.");
    }

    private static bool IsIdempotentDuplicate(
        CdcCaptureRuntimeState current,
        CdcCaptureExecutionReport report)
    {
        return Nullable.Equals(current.LastObservedAtUtc, report.ObservedAtUtc) &&
               string.Equals(current.LastOutcome, report.Outcome, StringComparison.OrdinalIgnoreCase) &&
               current.LastCapturedChangeCount == report.CapturedChangeCount &&
               current.LastProducedMessageCount == report.ProducedMessageCount &&
               string.Equals(current.LastChangeId, report.ChangeId, StringComparison.Ordinal) &&
               string.Equals(current.LastCheckpoint, report.Checkpoint, StringComparison.Ordinal) &&
               string.Equals(current.LastError, report.Error, StringComparison.Ordinal) &&
               string.Equals(current.LastReporterId, report.ReporterId, StringComparison.Ordinal) &&
               string.Equals(current.LastEdgeNodeId, report.EdgeNodeId, StringComparison.Ordinal) &&
               MatchesOptional(current.Freshness, report.Freshness) &&
               MatchesOptional(current.ObservationFreshness, report.ObservationFreshness) &&
               MatchesOptional(current.Lag, report.Lag) &&
               MatchesOptional(current.Publication, report.Publication) &&
               MetadataMatches(current.Metadata, report.Metadata);
    }

    private static bool MatchesOptional<T>(T current, T? incoming)
        where T : class
    {
        return incoming is null || EqualityComparer<T>.Default.Equals(current, incoming);
    }

    private static bool MetadataMatches(
        IReadOnlyDictionary<string, string> current,
        IReadOnlyDictionary<string, string> incoming)
    {
        if (current.Count != incoming.Count)
        {
            return false;
        }

        foreach (var pair in current)
        {
            if (!incoming.TryGetValue(pair.Key, out var incomingValue) ||
                !Comparer.Equals(pair.Value, incomingValue))
            {
                return false;
            }
        }

        return true;
    }

    private static CdcCaptureFreshnessStatus? CreateObservationFreshness(
        CdcCaptureExecutionRuntimeDescriptor? executionRuntime,
        DateTimeOffset observedAtUtc)
    {
        if (executionRuntime?.ObservationStaleAfterSeconds is not int staleAfterSeconds ||
            staleAfterSeconds <= 0)
        {
            return null;
        }

        return new CdcCaptureFreshnessStatus(
            CdcCaptureFreshnessStates.Fresh,
            observedAtUtc.AddSeconds(staleAfterSeconds),
            "The latest CDC runtime observation is still within the configured freshness window.");
    }

    private static DateTimeOffset? CreateReporterLeaseExpiry(
        CdcCaptureExecutionRuntimeDescriptor? executionRuntime,
        string? reporterId,
        DateTimeOffset observedAtUtc)
    {
        if (executionRuntime?.ReporterLeaseSeconds is not int leaseSeconds ||
            leaseSeconds <= 0 ||
            string.IsNullOrWhiteSpace(reporterId))
        {
            return null;
        }

        return observedAtUtc.AddSeconds(leaseSeconds);
    }

    private static void ValidateExecutionRuntimeEdgeNode(
        CdcCaptureExecutionReport report,
        CdcCaptureExecutionRuntimeDescriptor executionRuntime)
    {
        if (string.IsNullOrWhiteSpace(report.EdgeNodeId) ||
            executionRuntime.EdgeNodeIds.Count == 0)
        {
            return;
        }

        if (executionRuntime.EdgeNodeIds.Contains(report.EdgeNodeId, Comparer))
        {
            return;
        }

        throw new InvalidOperationException(
            $"CDC capture '{report.CdcCaptureId}' rejected edge node '{report.EdgeNodeId}' because execution runtime '{executionRuntime.Id}' only declares edge nodes '{string.Join("', '", executionRuntime.EdgeNodeIds)}'.");
    }

    private void ValidateExecutionRuntimeReporterIdentity(
        CdcCaptureExecutionReport report,
        CdcCaptureExecutionRuntimeDescriptor executionRuntime,
        RuntimeReporterLeaseSnapshot reporterSnapshot)
    {
        if (!executionRuntime.RejectConflictingReporterIds)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(report.ReporterId))
        {
            throw new InvalidOperationException(
                $"CDC capture '{report.CdcCaptureId}' requires reporter identity for execution runtime '{executionRuntime.Id}' because it rejects conflicting reporters.");
        }

        var activeReporterStates = reporterSnapshot.ActiveReporterStates;
        if (activeReporterStates.Length == 0)
        {
            return;
        }

        if (activeReporterStates.Length == 1 &&
            Comparer.Equals(activeReporterStates[0].ReporterId, report.ReporterId))
        {
            return;
        }

        RecordRejectedReporterConflict(executionRuntime.Id, report);

        var activeReporterSummary = string.Join(
            ", ",
            activeReporterStates.Select(static state =>
                state.ReporterLeaseExpiresAtUtc.HasValue
                    ? $"{state.ReporterId} (lease until {state.ReporterLeaseExpiresAtUtc.Value:O})"
                    : $"{state.ReporterId} (indefinite lease)"));
        throw new InvalidOperationException(
            $"CDC capture '{report.CdcCaptureId}' rejected reporter '{report.ReporterId}' because execution runtime '{executionRuntime.Id}' already has active reporter lease ownership for {activeReporterSummary}.");
    }

    private static bool IsReporterLeaseActive(
        DateTimeOffset? reporterLeaseExpiresAtUtc,
        DateTimeOffset now)
    {
        return !reporterLeaseExpiresAtUtc.HasValue ||
               reporterLeaseExpiresAtUtc.Value >= now;
    }

    private CdcCaptureReporterCoordinationStatus ResolveReporterCoordination(
        CdcCaptureDescriptor descriptor,
        CdcCaptureRuntimeState? current)
    {
        var executionRuntimeId = descriptor.ExecutionBinding.EffectiveExecutionRuntimeId;
        var executionRuntime = ResolveExecutionRuntimeDescriptor(executionRuntimeId, current?.Metadata);
        if (executionRuntime is null)
        {
            return UnknownReporterCoordination;
        }

        if (executionRuntime.ReporterLeaseSeconds is not int reporterLeaseSeconds ||
            reporterLeaseSeconds <= 0)
        {
            return new CdcCaptureReporterCoordinationStatus(
                CdcCaptureReporterCoordinationStates.NotConfigured,
                "The execution runtime does not currently declare reporter-lease coordination.")
            {
                ActiveReporterId = current?.LastReporterId,
                TakeoverState = CdcCaptureReporterTakeoverStates.NotApplicable,
                DegradedReason = CdcCaptureReporterCoordinationIssueReasons.None
            };
        }

        if (current is null || !current.HasReports)
        {
            return new CdcCaptureReporterCoordinationStatus(
                CdcCaptureReporterCoordinationStates.Unreported,
                "The execution runtime has not reported any external reporter observations yet.")
            {
                TakeoverState = CdcCaptureReporterTakeoverStates.NotApplicable,
                DegradedReason = CdcCaptureReporterCoordinationIssueReasons.None
            };
        }

        var now = timeProvider.GetUtcNow();
        var reporterSnapshot = SnapshotRuntimeReporterLease(executionRuntime.Id, now);
        var memory = ResolveRuntimeReporterCoordinationMemory(executionRuntime.Id);
        var participants = CreateReporterParticipants(reporterSnapshot, memory, now);
        var activeParticipants = participants
            .Where(static participant => string.Equals(participant.Role, CdcCaptureReporterParticipantRoles.Active, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var standbyParticipants = participants
            .Where(static participant => string.Equals(participant.Role, CdcCaptureReporterParticipantRoles.Standby, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var rejectedParticipants = participants
            .Where(static participant => string.Equals(participant.Role, CdcCaptureReporterParticipantRoles.Rejected, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var latestRejectedReporter = ResolveLatestRejectedReporter(memory);

        if (activeParticipants.Length > 1)
        {
            return new CdcCaptureReporterCoordinationStatus(
                CdcCaptureReporterCoordinationStates.Conflicted,
                "Multiple external reporters currently hold active leases for the same execution runtime.")
            {
                PreviousReporterId = memory.PreviousReporterId,
                LeaseExpiredAtUtc = memory.LeaseExpiredAtUtc,
                LastTakeoverObservedAtUtc = memory.LastTakeoverObservedAtUtc,
                LastConflictingReporterId = latestRejectedReporter?.ReporterId,
                LastConflictedAtUtc = latestRejectedReporter?.ObservedAtUtc,
                ReporterParticipants = participants,
                TakeoverState = CdcCaptureReporterTakeoverStates.NotApplicable,
                DegradedReason = CdcCaptureReporterCoordinationIssueReasons.MultipleActiveReporters
            };
        }

        if (activeParticipants.Length == 1)
        {
            var activeParticipant = activeParticipants[0];
            var takeoverState = !string.IsNullOrWhiteSpace(memory.PreviousReporterId) &&
                                memory.LastTakeoverObservedAtUtc.HasValue
                ? CdcCaptureReporterTakeoverStates.Completed
                : CdcCaptureReporterTakeoverStates.NotRequired;
            var description = rejectedParticipants.Length > 0
                ? $"Reporter '{activeParticipant.ReporterId}' currently holds the active lease while {rejectedParticipants.Length} rejected reporter(s) remain visible."
                : standbyParticipants.Length > 0
                    ? $"Reporter '{activeParticipant.ReporterId}' currently holds the active lease while {standbyParticipants.Length} standby reporter(s) remain visible from earlier observations."
                    : !string.IsNullOrWhiteSpace(memory.PreviousReporterId) && memory.LastTakeoverObservedAtUtc.HasValue
                        ? $"Reporter '{activeParticipant.ReporterId}' currently holds the active lease after taking over from '{memory.PreviousReporterId}'."
                        : $"Reporter '{activeParticipant.ReporterId}' currently holds the active lease for the execution runtime.";

            return new CdcCaptureReporterCoordinationStatus(
                rejectedParticipants.Length > 0
                    ? CdcCaptureReporterCoordinationStates.Conflicted
                    : CdcCaptureReporterCoordinationStates.Active,
                description)
            {
                ActiveReporterId = activeParticipant.ReporterId,
                ActiveReporterLeaseExpiresAtUtc = activeParticipant.LeaseExpiresAtUtc,
                PreviousReporterId = memory.PreviousReporterId,
                LeaseExpiredAtUtc = memory.LeaseExpiredAtUtc,
                LastTakeoverObservedAtUtc = memory.LastTakeoverObservedAtUtc,
                LastConflictingReporterId = latestRejectedReporter?.ReporterId,
                LastConflictedAtUtc = latestRejectedReporter?.ObservedAtUtc,
                ReporterParticipants = participants,
                TakeoverState = takeoverState,
                DegradedReason = rejectedParticipants.Length > 0
                    ? CdcCaptureReporterCoordinationIssueReasons.RejectedReporterConflict
                    : CdcCaptureReporterCoordinationIssueReasons.None
            };
        }

        if (standbyParticipants.Length > 0)
        {
            var latestStandbyParticipant = standbyParticipants
                .OrderByDescending(static participant => participant.LastObservedAtUtc ?? DateTimeOffset.MinValue)
                .ThenBy(static participant => participant.ReporterId, Comparer)
                .First();
            return new CdcCaptureReporterCoordinationStatus(
                CdcCaptureReporterCoordinationStates.LeaseExpired,
                rejectedParticipants.Length > 0
                    ? $"Reporter '{latestStandbyParticipant.ReporterId}' no longer holds an active lease; the execution runtime is awaiting failover or takeover while rejected reporter evidence remains visible."
                    : $"Reporter '{latestStandbyParticipant.ReporterId}' no longer holds an active lease; the execution runtime is awaiting failover or takeover.")
            {
                PreviousReporterId = latestStandbyParticipant.ReporterId,
                LeaseExpiredAtUtc = latestStandbyParticipant.LeaseExpiresAtUtc ?? memory.LeaseExpiredAtUtc,
                LastTakeoverObservedAtUtc = memory.LastTakeoverObservedAtUtc,
                LastConflictingReporterId = latestRejectedReporter?.ReporterId,
                LastConflictedAtUtc = latestRejectedReporter?.ObservedAtUtc,
                ReporterParticipants = participants,
                TakeoverState = CdcCaptureReporterTakeoverStates.AwaitingTakeover,
                DegradedReason = CdcCaptureReporterCoordinationIssueReasons.AwaitingTakeover
            };
        }

        if (rejectedParticipants.Length > 0)
        {
            var latestRejectedParticipant = rejectedParticipants
                .OrderByDescending(static participant => participant.LastObservedAtUtc ?? DateTimeOffset.MinValue)
                .ThenBy(static participant => participant.ReporterId, Comparer)
                .First();
            return new CdcCaptureReporterCoordinationStatus(
                CdcCaptureReporterCoordinationStates.Conflicted,
                $"Reporter '{latestRejectedParticipant.ReporterId}' most recently attempted to report while another reporter still held the active lease.")
            {
                LastConflictingReporterId = latestRejectedParticipant.ReporterId,
                LastConflictedAtUtc = latestRejectedParticipant.LastObservedAtUtc,
                ReporterParticipants = participants,
                TakeoverState = CdcCaptureReporterTakeoverStates.NotApplicable,
                DegradedReason = CdcCaptureReporterCoordinationIssueReasons.RejectedReporterConflict
            };
        }

        return new CdcCaptureReporterCoordinationStatus(
            CdcCaptureReporterCoordinationStates.Unknown,
            "The execution runtime reported external reporter identity, but the current lease posture cannot be determined.")
        {
            PreviousReporterId = memory.PreviousReporterId,
            LeaseExpiredAtUtc = memory.LeaseExpiredAtUtc,
            LastTakeoverObservedAtUtc = memory.LastTakeoverObservedAtUtc,
            LastConflictingReporterId = latestRejectedReporter?.ReporterId,
            LastConflictedAtUtc = latestRejectedReporter?.ObservedAtUtc,
            ReporterParticipants = participants,
            TakeoverState = CdcCaptureReporterTakeoverStates.NotApplicable,
            DegradedReason = CdcCaptureReporterCoordinationIssueReasons.None
        };
    }

    private RuntimeReporterCoordinationMemory ResolveRuntimeReporterCoordinationMemory(string executionRuntimeId)
    {
        return runtimeReporterCoordinationById.TryGetValue(executionRuntimeId, out var memory)
            ? memory
            : RuntimeReporterCoordinationMemory.Empty;
    }

    private RuntimeReporterLeaseSnapshot SnapshotRuntimeReporterLease(
        string executionRuntimeId,
        DateTimeOffset now)
    {
        var runtimeStates = reportedStatesById.Values
            .Where(state =>
                Comparer.Equals(state.ExecutionBinding.EffectiveExecutionRuntimeId, executionRuntimeId) &&
                !string.IsNullOrWhiteSpace(state.LastReporterId))
            .ToArray();
        if (runtimeStates.Length == 0)
        {
            return RuntimeReporterLeaseSnapshot.Empty;
        }

        var reporterStates = runtimeStates
            .GroupBy(static state => state.LastReporterId!, Comparer)
            .Select(group =>
            {
                var latestReporterState = group
                    .OrderByDescending(static state => state.LastObservedAtUtc ?? DateTimeOffset.MinValue)
                    .ThenBy(static state => state.CdcCaptureId, Comparer)
                    .First();
                var reporterLeaseExpiresAtUtc = group
                    .Where(static state => state.ReporterLeaseExpiresAtUtc.HasValue)
                    .Select(static state => state.ReporterLeaseExpiresAtUtc)
                    .Max();

                return new RuntimeReporterLeaseState(
                    ReporterId: group.Key,
                    ReporterLeaseExpiresAtUtc: reporterLeaseExpiresAtUtc,
                    LastObservedAtUtc: latestReporterState.LastObservedAtUtc,
                    LastCdcCaptureId: latestReporterState.CdcCaptureId,
                    ObservedEdgeNodeIds: group
                        .Select(static state => state.LastEdgeNodeId)
                        .Where(static edgeNodeId => !string.IsNullOrWhiteSpace(edgeNodeId))
                        .Select(static edgeNodeId => edgeNodeId!)
                        .Distinct(Comparer)
                        .OrderBy(static edgeNodeId => edgeNodeId, Comparer)
                        .ToArray(),
                    HasActiveLease: IsReporterLeaseActive(reporterLeaseExpiresAtUtc, now));
            })
            .OrderBy(static state => state.ReporterId, Comparer)
            .ToArray();
        var latestState = runtimeStates
            .OrderByDescending(static state => state.LastObservedAtUtc ?? DateTimeOffset.MinValue)
            .ThenBy(static state => state.CdcCaptureId, Comparer)
            .First();

        return new RuntimeReporterLeaseSnapshot(
            LatestReporterId: latestState.LastReporterId,
            LatestReporterLeaseExpiresAtUtc: latestState.ReporterLeaseExpiresAtUtc,
            ReporterStates: reporterStates);
    }

    private static ReporterTakeoverTransition? DetectReporterTakeover(
        CdcCaptureExecutionRuntimeDescriptor executionRuntime,
        CdcCaptureExecutionReport report,
        RuntimeReporterLeaseSnapshot reporterSnapshot)
    {
        if (string.IsNullOrWhiteSpace(report.ReporterId) ||
            executionRuntime.ReporterLeaseSeconds is not int reporterLeaseSeconds ||
            reporterLeaseSeconds <= 0 ||
            reporterSnapshot.ActiveReporterStates.Length > 1 ||
            !string.IsNullOrWhiteSpace(reporterSnapshot.ActiveReporterId) ||
            string.IsNullOrWhiteSpace(reporterSnapshot.LatestReporterId) ||
            !reporterSnapshot.LatestReporterLeaseExpiresAtUtc.HasValue ||
            Comparer.Equals(reporterSnapshot.LatestReporterId, report.ReporterId) ||
            reporterSnapshot.LatestReporterLeaseExpiresAtUtc.Value >= report.ObservedAtUtc)
        {
            return null;
        }

        var previousReporterState = reporterSnapshot.ReporterStates
            .FirstOrDefault(state => Comparer.Equals(state.ReporterId, reporterSnapshot.LatestReporterId));

        return new ReporterTakeoverTransition(
            reporterSnapshot.LatestReporterId,
            reporterSnapshot.LatestReporterLeaseExpiresAtUtc.Value,
            report.ObservedAtUtc,
            previousReporterState?.LastObservedAtUtc,
            previousReporterState?.LastCdcCaptureId,
            previousReporterState?.ObservedEdgeNodeIds ?? []);
    }

    private void RecordRejectedReporterConflict(
        string executionRuntimeId,
        CdcCaptureExecutionReport report)
    {
        var existing = ResolveRuntimeReporterCoordinationMemory(executionRuntimeId);
        runtimeReporterCoordinationById[executionRuntimeId] = existing with
        {
            RejectedReporters = UpsertRejectedReporter(
                existing.RejectedReporters,
                new RuntimeRejectedReporterMemory(
                    report.ReporterId!,
                    report.ObservedAtUtc,
                    report.CdcCaptureId,
                    report.EdgeNodeId))
        };
    }

    private void RecordAcceptedReporterObservation(
        string executionRuntimeId,
        CdcCaptureExecutionReport report,
        RuntimeReporterLeaseSnapshot reporterSnapshot,
        ReporterTakeoverTransition? reporterTakeover)
    {
        if (string.IsNullOrWhiteSpace(report.ReporterId))
        {
            return;
        }

        var existing = ResolveRuntimeReporterCoordinationMemory(executionRuntimeId);
        var updated = existing with
        {
            RejectedReporters = PruneRejectedReporters(
                existing.RejectedReporters,
                report.ObservedAtUtc,
                report.ReporterId)
        };

        if (reporterTakeover is not null)
        {
            updated = updated with
            {
                PreviousReporterId = reporterTakeover.PreviousReporterId,
                LeaseExpiredAtUtc = reporterTakeover.LeaseExpiredAtUtc,
                LastTakeoverObservedAtUtc = reporterTakeover.ObservedAtUtc,
                PreviousReporterLastObservedAtUtc = reporterTakeover.PreviousReporterLastObservedAtUtc,
                PreviousReporterLastCdcCaptureId = reporterTakeover.PreviousReporterLastCdcCaptureId,
                PreviousReporterObservedEdgeNodeIds = reporterTakeover.PreviousReporterObservedEdgeNodeIds
            };
        }

        if (!runtimeReporterCoordinationById.ContainsKey(executionRuntimeId) ||
            !EqualityComparer<RuntimeReporterCoordinationMemory>.Default.Equals(existing, updated))
        {
            runtimeReporterCoordinationById[executionRuntimeId] = updated;
        }
    }

    private static CdcCaptureReporterParticipantStatus[] CreateReporterParticipants(
        RuntimeReporterLeaseSnapshot reporterSnapshot,
        RuntimeReporterCoordinationMemory memory,
        DateTimeOffset now)
    {
        var participants = new List<CdcCaptureReporterParticipantStatus>();
        var rejectedReportersById = memory.RejectedReporters.ToDictionary(static item => item.ReporterId, Comparer);

        foreach (var reporterState in reporterSnapshot.ReporterStates)
        {
            rejectedReportersById.TryGetValue(reporterState.ReporterId, out var rejectedReporter);

            if (reporterState.HasActiveLease)
            {
                participants.Add(new CdcCaptureReporterParticipantStatus(
                    reporterState.ReporterId,
                    CdcCaptureReporterParticipantRoles.Active,
                    $"Reporter '{reporterState.ReporterId}' currently holds the active lease for the execution runtime.")
                {
                    LastObservedAtUtc = reporterState.LastObservedAtUtc,
                    LeaseExpiresAtUtc = reporterState.ReporterLeaseExpiresAtUtc,
                    LastCdcCaptureId = reporterState.LastCdcCaptureId,
                    ObservedEdgeNodeIds = reporterState.ObservedEdgeNodeIds
                });

                continue;
            }

            if (rejectedReporter is not null &&
                (!reporterState.LastObservedAtUtc.HasValue ||
                 rejectedReporter.ObservedAtUtc >= reporterState.LastObservedAtUtc.Value))
            {
                participants.Add(CreateRejectedReporterParticipant(rejectedReporter));
                rejectedReportersById.Remove(rejectedReporter.ReporterId);
                continue;
            }

            participants.Add(new CdcCaptureReporterParticipantStatus(
                reporterState.ReporterId,
                CdcCaptureReporterParticipantRoles.Standby,
                reporterState.ReporterLeaseExpiresAtUtc.HasValue && reporterState.ReporterLeaseExpiresAtUtc.Value < now
                    ? $"Reporter '{reporterState.ReporterId}' no longer holds an active lease but remains visible from accepted runtime observations."
                    : $"Reporter '{reporterState.ReporterId}' remains visible from accepted runtime observations without holding the single active lease.")
            {
                LastObservedAtUtc = reporterState.LastObservedAtUtc,
                LeaseExpiresAtUtc = reporterState.ReporterLeaseExpiresAtUtc,
                LastCdcCaptureId = reporterState.LastCdcCaptureId,
                ObservedEdgeNodeIds = reporterState.ObservedEdgeNodeIds
            });
        }

        if (!string.IsNullOrWhiteSpace(memory.PreviousReporterId) &&
            ShouldAddHistoricalStandbyParticipant(memory, participants))
        {
            participants.Add(new CdcCaptureReporterParticipantStatus(
                memory.PreviousReporterId,
                CdcCaptureReporterParticipantRoles.Standby,
                $"Reporter '{memory.PreviousReporterId}' previously held the active lease and is now visible as a standby reporter after takeover.")
            {
                LastObservedAtUtc = memory.PreviousReporterLastObservedAtUtc,
                LeaseExpiresAtUtc = memory.LeaseExpiredAtUtc,
                LastCdcCaptureId = memory.PreviousReporterLastCdcCaptureId,
                ObservedEdgeNodeIds = memory.PreviousReporterObservedEdgeNodeIds
            });
        }

        foreach (var rejectedReporter in rejectedReportersById.Values
                     .OrderByDescending(static item => item.ObservedAtUtc)
                     .ThenBy(static item => item.ReporterId, Comparer))
        {
            participants.Add(CreateRejectedReporterParticipant(rejectedReporter));
        }

        return participants
            .OrderBy(static participant => GetParticipantRoleOrder(participant.Role))
            .ThenBy(static participant => participant.ReporterId, Comparer)
            .ToArray();
    }

    private static CdcCaptureReporterParticipantStatus CreateRejectedReporterParticipant(
        RuntimeRejectedReporterMemory rejectedReporter)
    {
        var observedEdgeNodeIds = string.IsNullOrWhiteSpace(rejectedReporter.EdgeNodeId)
            ? Array.Empty<string>()
            : [rejectedReporter.EdgeNodeId];

        return new CdcCaptureReporterParticipantStatus(
            rejectedReporter.ReporterId,
            CdcCaptureReporterParticipantRoles.Rejected,
            $"Reporter '{rejectedReporter.ReporterId}' most recently attempted to report while another reporter still held the active lease.")
        {
            LastObservedAtUtc = rejectedReporter.ObservedAtUtc,
            LastCdcCaptureId = rejectedReporter.CdcCaptureId,
            ObservedEdgeNodeIds = observedEdgeNodeIds
        };
    }

    private static int GetParticipantRoleOrder(string role)
    {
        if (string.Equals(role, CdcCaptureReporterParticipantRoles.Active, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (string.Equals(role, CdcCaptureReporterParticipantRoles.Standby, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (string.Equals(role, CdcCaptureReporterParticipantRoles.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return 3;
    }

    private static RuntimeRejectedReporterMemory? ResolveLatestRejectedReporter(
        RuntimeReporterCoordinationMemory memory)
    {
        return memory.RejectedReporters
            .OrderByDescending(static item => item.ObservedAtUtc)
            .ThenBy(static item => item.ReporterId, Comparer)
            .FirstOrDefault();
    }

    private static bool ShouldAddHistoricalStandbyParticipant(
        RuntimeReporterCoordinationMemory memory,
        IReadOnlyList<CdcCaptureReporterParticipantStatus> participants)
    {
        if (string.IsNullOrWhiteSpace(memory.PreviousReporterId) ||
            participants.Any(participant => Comparer.Equals(participant.ReporterId, memory.PreviousReporterId)))
        {
            return false;
        }

        if (!memory.LastTakeoverObservedAtUtc.HasValue)
        {
            return true;
        }

        return !participants.Any(participant =>
            string.Equals(participant.Role, CdcCaptureReporterParticipantRoles.Active, StringComparison.OrdinalIgnoreCase) &&
            participant.LastObservedAtUtc.HasValue &&
            participant.LastObservedAtUtc.Value > memory.LastTakeoverObservedAtUtc.Value);
    }

    private static RuntimeRejectedReporterMemory[] UpsertRejectedReporter(
        RuntimeRejectedReporterMemory[] existing,
        RuntimeRejectedReporterMemory rejectedReporter)
    {
        return existing
            .Where(item => !Comparer.Equals(item.ReporterId, rejectedReporter.ReporterId))
            .Append(rejectedReporter)
            .OrderBy(static item => item.ReporterId, Comparer)
            .ToArray();
    }

    private static RuntimeRejectedReporterMemory[] PruneRejectedReporters(
        RuntimeRejectedReporterMemory[] existing,
        DateTimeOffset acceptedObservedAtUtc,
        string? reporterId)
    {
        if (existing.Length == 0)
        {
            return existing;
        }

        return existing
            .Where(item =>
                item.ObservedAtUtc > acceptedObservedAtUtc &&
                !Comparer.Equals(item.ReporterId, reporterId))
            .OrderBy(static item => item.ReporterId, Comparer)
            .ToArray();
    }

    private static void UpsertOptional(
        Dictionary<string, string> metadata,
        string key,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            metadata.Remove(key);
            return;
        }

        metadata[key] = value.Trim();
    }

    private sealed record RuntimeReporterCoordinationMemory(
        string? PreviousReporterId,
        DateTimeOffset? LeaseExpiredAtUtc,
        DateTimeOffset? LastTakeoverObservedAtUtc,
        DateTimeOffset? PreviousReporterLastObservedAtUtc,
        string? PreviousReporterLastCdcCaptureId,
        string[] PreviousReporterObservedEdgeNodeIds,
        RuntimeRejectedReporterMemory[] RejectedReporters)
    {
        public static RuntimeReporterCoordinationMemory Empty { get; } =
            new(null, null, null, null, null, [], []);
    }

    private sealed record RuntimeReporterLeaseSnapshot(
        string? LatestReporterId,
        DateTimeOffset? LatestReporterLeaseExpiresAtUtc,
        RuntimeReporterLeaseState[] ReporterStates)
    {
        public static RuntimeReporterLeaseSnapshot Empty { get; } =
            new(null, null, []);

        public RuntimeReporterLeaseState[] ActiveReporterStates => ReporterStates
            .Where(static state => state.HasActiveLease)
            .ToArray();

        public string? ActiveReporterId => ActiveReporterStates.Length == 1
            ? ActiveReporterStates[0].ReporterId
            : null;

        public DateTimeOffset? ActiveReporterLeaseExpiresAtUtc => ActiveReporterStates.Length == 1
            ? ActiveReporterStates[0].ReporterLeaseExpiresAtUtc
            : null;
    }

    private sealed record RuntimeReporterLeaseState(
        string ReporterId,
        DateTimeOffset? ReporterLeaseExpiresAtUtc,
        DateTimeOffset? LastObservedAtUtc,
        string? LastCdcCaptureId,
        string[] ObservedEdgeNodeIds,
        bool HasActiveLease);

    private sealed record RuntimeRejectedReporterMemory(
        string ReporterId,
        DateTimeOffset ObservedAtUtc,
        string CdcCaptureId,
        string? EdgeNodeId);

    private sealed record ReporterTakeoverTransition(
        string PreviousReporterId,
        DateTimeOffset LeaseExpiredAtUtc,
        DateTimeOffset ObservedAtUtc,
        DateTimeOffset? PreviousReporterLastObservedAtUtc,
        string? PreviousReporterLastCdcCaptureId,
        string[] PreviousReporterObservedEdgeNodeIds);
}
