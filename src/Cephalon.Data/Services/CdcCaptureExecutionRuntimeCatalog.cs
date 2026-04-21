using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

internal sealed class CdcCaptureExecutionRuntimeCatalog : ICdcCaptureExecutionRuntimeCatalog
{
    private readonly Dictionary<string, CdcCaptureExecutionRuntimeDescriptor> index;
    private readonly ICdcCaptureCatalog captureCatalog;
    private readonly ICdcCaptureRuntimeStateCatalog? runtimeStateCatalog;

    public CdcCaptureExecutionRuntimeCatalog(
        CdcCaptureExecutionRuntimeDescriptorCatalog runtimeDescriptorCatalog,
        ICdcCaptureCatalog captureCatalog,
        ICdcCaptureRuntimeStateCatalog? runtimeStateCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(runtimeDescriptorCatalog);
        ArgumentNullException.ThrowIfNull(captureCatalog);

        this.captureCatalog = captureCatalog;
        this.runtimeStateCatalog = runtimeStateCatalog;
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

    private string[] ResolveCaptureIds(string executionRuntimeId)
    {
        return captureCatalog.GetByExecutionRuntimeId(executionRuntimeId)
            .Select(static cdcCapture => cdcCapture.Id)
            .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
