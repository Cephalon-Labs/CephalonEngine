using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

internal sealed class CdcCaptureExecutionRuntimeCatalog : ICdcCaptureExecutionRuntimeCatalog
{
    private readonly Dictionary<string, CdcCaptureExecutionRuntimeDescriptor> index;
    private readonly ICdcCaptureCatalog captureCatalog;
    private readonly ICdcCaptureRuntimeStateCatalog? runtimeStateCatalog;
    private readonly TimeProvider timeProvider;

    public CdcCaptureExecutionRuntimeCatalog(
        CdcCaptureExecutionRuntimeDescriptorCatalog runtimeDescriptorCatalog,
        ICdcCaptureCatalog captureCatalog,
        ICdcCaptureRuntimeStateCatalog? runtimeStateCatalog = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(runtimeDescriptorCatalog);
        ArgumentNullException.ThrowIfNull(captureCatalog);

        this.captureCatalog = captureCatalog;
        this.runtimeStateCatalog = runtimeStateCatalog;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        var runtimes = runtimeDescriptorCatalog.Runtimes;
        index = runtimes.ToDictionary(static runtime => runtime.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> Runtimes => index.Values
        .Select(Enrich)
        .OrderBy(static runtime => runtime.DisplayName, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public CdcCaptureExecutionRuntimeDescriptor? GetById(string executionRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(executionRuntimeId))
        {
            return null;
        }

        return index.TryGetValue(executionRuntimeId.Trim(), out var runtime)
            ? Enrich(runtime)
            : null;
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByReporterId(string reporterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reporterId);
        var normalizedReporterId = reporterId.Trim();

        return FilterRuntimes(runtime => runtime.Summary.ReporterCoordination.ReporterParticipants.Any(
            participant => string.Equals(participant.ReporterId, normalizedReporterId, StringComparison.OrdinalIgnoreCase)));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByEdgeNodeId(string edgeNodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(edgeNodeId);
        var normalizedEdgeNodeId = edgeNodeId.Trim();

        return FilterRuntimes(runtime => runtime.Summary.ObservedEdgeNodeIds.Contains(normalizedEdgeNodeId, StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByReporterCoordinationState(string coordinationState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(coordinationState);
        var normalizedCoordinationState = coordinationState.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.Summary.ReporterCoordination.State,
            normalizedCoordinationState,
            StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> GetByReporterCoordinationIssueReason(string degradedReason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(degradedReason);
        var normalizedDegradedReason = degradedReason.Trim();

        return FilterRuntimes(runtime => string.Equals(
            runtime.Summary.ReporterCoordination.DegradedReason,
            normalizedDegradedReason,
            StringComparison.OrdinalIgnoreCase));
    }

    private CdcCaptureExecutionRuntimeDescriptor[] FilterRuntimes(
        Func<CdcCaptureExecutionRuntimeDescriptor, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return index.Values
            .Select(Enrich)
            .Where(predicate)
            .OrderBy(static runtime => runtime.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private CdcCaptureExecutionRuntimeDescriptor Enrich(CdcCaptureExecutionRuntimeDescriptor runtime)
    {
        var captureIds = ResolveCaptureIds(runtime.Id);
        var summary = runtimeStateCatalog is null
            ? CdcCaptureExecutionRuntimeSummary.Empty
            : CreateSummary(runtime);
        return new CdcCaptureExecutionRuntimeDescriptor(
            id: runtime.Id,
            displayName: runtime.DisplayName,
            description: runtime.Description,
            metadata: runtime.Metadata,
            cdcCaptureIds: captureIds,
            summary: summary);
    }

    private CdcCaptureExecutionRuntimeSummary CreateSummary(CdcCaptureExecutionRuntimeDescriptor runtime)
    {
        var matchingStates = runtimeStateCatalog!.GetByExecutionRuntimeId(runtime.Id);

        if (matchingStates.Count == 0)
        {
            return CdcCaptureExecutionRuntimeSummary.Empty with
            {
                ReporterCoordination = CreateReporterCoordination(runtime)
            };
        }

        var latestState = matchingStates
            .OrderByDescending(static state => state.LastObservedAtUtc ?? DateTimeOffset.MinValue)
            .ThenBy(static state => state.CdcCaptureId, StringComparer.OrdinalIgnoreCase)
            .First();
        latestState.Metadata.TryGetValue("acknowledgement", out var lastAcknowledgement);
        var activeReporterId = ResolveActiveReporterId(matchingStates, timeProvider.GetUtcNow());

        return new CdcCaptureExecutionRuntimeSummary(
            ReportedCdcCaptureIds: matchingStates.Select(static state => state.CdcCaptureId).ToArray(),
            LastCdcCaptureId: latestState.CdcCaptureId,
            LastOutcome: latestState.LastOutcome,
            LastObservedAtUtc: latestState.LastObservedAtUtc,
            LastReportId: latestState.LastReportId,
            LastChangeId: latestState.LastChangeId,
            LastCheckpoint: latestState.LastCheckpoint,
            StartedCount: matchingStates.Sum(static state => state.StartedCount),
            CapturedCount: matchingStates.Sum(static state => state.CapturedCount),
            IdleCount: matchingStates.Sum(static state => state.IdleCount),
            FailedCount: matchingStates.Sum(static state => state.FailedCount),
            TotalCapturedChangeCount: matchingStates.Sum(static state => state.TotalCapturedChangeCount),
            TotalProducedMessageCount: matchingStates.Sum(static state => state.TotalProducedMessageCount),
            LastAcknowledgement: string.IsNullOrWhiteSpace(lastAcknowledgement) ? null : lastAcknowledgement.Trim(),
            LastError: latestState.LastError,
            ObservationFreshness: AggregateObservationFreshness(matchingStates))
        {
            ReporterCoordination = latestState.ReporterCoordination,
            LastReporterId = latestState.LastReporterId,
            ActiveReporterId = activeReporterId,
            ReporterLeaseExpiresAtUtc = ResolveActiveReporterLeaseExpiry(matchingStates, activeReporterId),
            ObservedEdgeNodeIds = matchingStates
                .Select(static state => state.LastEdgeNodeId)
                .Where(static edgeNodeId => !string.IsNullOrWhiteSpace(edgeNodeId))
                .Select(static edgeNodeId => edgeNodeId!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static edgeNodeId => edgeNodeId, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            LastEdgeNodeId = latestState.LastEdgeNodeId
        };
    }

    private static CdcCaptureReporterCoordinationStatus CreateReporterCoordination(
        CdcCaptureExecutionRuntimeDescriptor runtime)
    {
        if (runtime.ReporterLeaseSeconds is not int reporterLeaseSeconds ||
            reporterLeaseSeconds <= 0)
        {
            return new CdcCaptureReporterCoordinationStatus(
                CdcCaptureReporterCoordinationStates.NotConfigured,
                "The execution runtime does not currently declare reporter-lease coordination.");
        }

        return new CdcCaptureReporterCoordinationStatus(
            CdcCaptureReporterCoordinationStates.Unreported,
            "The execution runtime has not reported any external reporter observations yet.");
    }

    private static CdcCaptureFreshnessStatus AggregateObservationFreshness(
        IReadOnlyList<CdcCaptureRuntimeState> matchingStates)
    {
        var reportedStates = matchingStates
            .Where(static state => state.HasReports)
            .ToArray();
        if (reportedStates.Length == 0)
        {
            return new CdcCaptureFreshnessStatus(CdcCaptureFreshnessStates.Unknown);
        }

        var knownStates = reportedStates
            .Select(static state => state.ObservationFreshness)
            .Where(static freshness => !string.Equals(freshness.State, CdcCaptureFreshnessStates.Unknown, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (knownStates.Length == 0)
        {
            return new CdcCaptureFreshnessStatus(CdcCaptureFreshnessStates.Unknown);
        }

        if (knownStates.Any(static freshness => string.Equals(freshness.State, CdcCaptureFreshnessStates.Stale, StringComparison.OrdinalIgnoreCase)))
        {
            var earliestKnownExpiry = knownStates
                .Where(static freshness => freshness.FreshUntilUtc.HasValue)
                .Select(static freshness => freshness.FreshUntilUtc)
                .Min();
            return new CdcCaptureFreshnessStatus(
                CdcCaptureFreshnessStates.Stale,
                freshUntilUtc: earliestKnownExpiry,
                description: "At least one CDC capture observation owned by the execution runtime is now stale.");
        }

        var freshStates = knownStates
            .Where(static freshness => string.Equals(freshness.State, CdcCaptureFreshnessStates.Fresh, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (freshStates.Length == knownStates.Length &&
            freshStates.All(static freshness => freshness.FreshUntilUtc.HasValue))
        {
            return new CdcCaptureFreshnessStatus(
                CdcCaptureFreshnessStates.Fresh,
                freshStates.Min(static freshness => freshness.FreshUntilUtc!.Value),
                "All CDC capture observations owned by the execution runtime remain within the configured freshness window.");
        }

        var earliestFreshExpiry = freshStates
            .Where(static freshness => freshness.FreshUntilUtc.HasValue)
            .Select(static freshness => freshness.FreshUntilUtc)
            .Min();
        return new CdcCaptureFreshnessStatus(
            CdcCaptureFreshnessStates.Mixed,
            freshUntilUtc: earliestFreshExpiry,
            description: "CDC capture observations owned by the execution runtime do not currently share the same freshness posture.");
    }

    private string[] ResolveCaptureIds(string executionRuntimeId)
    {
        return captureCatalog.GetByExecutionRuntimeId(executionRuntimeId)
            .Select(static cdcCapture => cdcCapture.Id)
            .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? ResolveActiveReporterId(
        IReadOnlyList<CdcCaptureRuntimeState> matchingStates,
        DateTimeOffset now)
    {
        var activeReporterIds = matchingStates
            .Where(state =>
                !string.IsNullOrWhiteSpace(state.LastReporterId) &&
                IsReporterLeaseActive(state.ReporterLeaseExpiresAtUtc, now))
            .Select(static state => state.LastReporterId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static reporterId => reporterId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return activeReporterIds.Length == 1
            ? activeReporterIds[0]
            : null;
    }

    private static DateTimeOffset? ResolveActiveReporterLeaseExpiry(
        IReadOnlyList<CdcCaptureRuntimeState> matchingStates,
        string? activeReporterId)
    {
        if (string.IsNullOrWhiteSpace(activeReporterId))
        {
            return null;
        }

        return matchingStates
            .Where(state => string.Equals(state.LastReporterId, activeReporterId, StringComparison.OrdinalIgnoreCase))
            .Where(static state => state.ReporterLeaseExpiresAtUtc.HasValue)
            .Select(static state => state.ReporterLeaseExpiresAtUtc)
            .Max();
    }

    private static bool IsReporterLeaseActive(
        DateTimeOffset? reporterLeaseExpiresAtUtc,
        DateTimeOffset now)
    {
        return !reporterLeaseExpiresAtUtc.HasValue ||
               reporterLeaseExpiresAtUtc.Value >= now;
    }
}
