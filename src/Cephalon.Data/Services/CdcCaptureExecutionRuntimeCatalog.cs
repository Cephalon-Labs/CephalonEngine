using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

internal sealed class CdcCaptureExecutionRuntimeCatalog : ICdcCaptureExecutionRuntimeCatalog
{
    private readonly Dictionary<string, CdcCaptureExecutionRuntimeDescriptor> index;
    private readonly ICdcCaptureRuntimeStateCatalog? runtimeStateCatalog;

    public CdcCaptureExecutionRuntimeCatalog(
        IEnumerable<ICdcCaptureExecutionRuntimeContributor> contributors,
        ICdcCaptureRuntimeStateCatalog? runtimeStateCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(contributors);

        var registry = new CdcCaptureExecutionRuntimeRegistry();
        foreach (var contributor in contributors)
        {
            contributor.RegisterExecutionRuntimes(registry);
        }

        this.runtimeStateCatalog = runtimeStateCatalog;
        var runtimes = registry.Build();
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

    private CdcCaptureExecutionRuntimeDescriptor Enrich(CdcCaptureExecutionRuntimeDescriptor runtime)
    {
        if (runtimeStateCatalog is null)
        {
            return runtime;
        }

        var summary = CreateSummary(runtime);
        return new CdcCaptureExecutionRuntimeDescriptor(
            id: runtime.Id,
            displayName: runtime.DisplayName,
            description: runtime.Description,
            metadata: runtime.Metadata,
            cdcCaptureIds: runtime.CdcCaptureIds,
            summary: summary);
    }

    private CdcCaptureExecutionRuntimeSummary CreateSummary(CdcCaptureExecutionRuntimeDescriptor runtime)
    {
        var matchingStates = runtimeStateCatalog!.States
            .Where(state => RuntimeStateBelongsToRuntime(state, runtime))
            .OrderBy(static state => state.CdcCaptureId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (matchingStates.Length == 0)
        {
            return CdcCaptureExecutionRuntimeSummary.Empty;
        }

        var latestState = matchingStates
            .OrderByDescending(static state => state.LastObservedAtUtc ?? DateTimeOffset.MinValue)
            .ThenBy(static state => state.CdcCaptureId, StringComparer.OrdinalIgnoreCase)
            .First();
        latestState.Metadata.TryGetValue("acknowledgement", out var lastAcknowledgement);

        return new CdcCaptureExecutionRuntimeSummary(
            ReportedCdcCaptureIds: matchingStates.Select(static state => state.CdcCaptureId).ToArray(),
            LastCdcCaptureId: latestState.CdcCaptureId,
            LastOutcome: latestState.LastOutcome,
            LastObservedAtUtc: latestState.LastObservedAtUtc,
            LastChangeId: latestState.LastChangeId,
            LastCheckpoint: latestState.LastCheckpoint,
            StartedCount: matchingStates.Sum(static state => state.StartedCount),
            CapturedCount: matchingStates.Sum(static state => state.CapturedCount),
            IdleCount: matchingStates.Sum(static state => state.IdleCount),
            FailedCount: matchingStates.Sum(static state => state.FailedCount),
            TotalCapturedChangeCount: matchingStates.Sum(static state => state.TotalCapturedChangeCount),
            TotalProducedMessageCount: matchingStates.Sum(static state => state.TotalProducedMessageCount),
            LastAcknowledgement: string.IsNullOrWhiteSpace(lastAcknowledgement) ? null : lastAcknowledgement.Trim(),
            LastError: latestState.LastError);
    }

    private static bool RuntimeStateBelongsToRuntime(
        CdcCaptureRuntimeState state,
        CdcCaptureExecutionRuntimeDescriptor runtime)
    {
        if (runtime.CdcCaptureIds.Contains(state.CdcCaptureId, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        if (state.Metadata.TryGetValue("cdcCaptureExecutionRuntimeId", out var reportedRuntimeId) &&
            string.Equals(reportedRuntimeId, runtime.Id, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return state.Metadata.TryGetValue("captureExecutionRuntimeId", out reportedRuntimeId) &&
            string.Equals(reportedRuntimeId, runtime.Id, StringComparison.OrdinalIgnoreCase);
    }
}
