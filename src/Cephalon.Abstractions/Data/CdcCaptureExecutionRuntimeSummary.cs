namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the latest aggregate operator-facing runtime summary for one CDC capture execution runtime.
/// </summary>
/// <param name="ReportedCdcCaptureIds">The CDC capture identifiers that have reported runtime state for the execution runtime.</param>
/// <param name="LastCdcCaptureId">The CDC capture identifier that produced the latest runtime observation.</param>
/// <param name="LastOutcome">The latest reported capture outcome visible for the execution runtime.</param>
/// <param name="LastObservedAtUtc">The UTC timestamp when the latest runtime observation was reported.</param>
/// <param name="LastReportId">The latest stable report identifier visible for the execution runtime when one was reported.</param>
/// <param name="LastChangeId">The latest provider-facing change identifier visible for the execution runtime.</param>
/// <param name="LastCheckpoint">The latest provider-facing checkpoint visible for the execution runtime.</param>
/// <param name="StartedCount">The total number of <c>started</c> observations visible for the execution runtime.</param>
/// <param name="CapturedCount">The total number of <c>captured</c> observations visible for the execution runtime.</param>
/// <param name="IdleCount">The total number of <c>idle</c> observations visible for the execution runtime.</param>
/// <param name="FailedCount">The total number of <c>failed</c> observations visible for the execution runtime.</param>
/// <param name="TotalCapturedChangeCount">The total number of source changes reported for the execution runtime.</param>
/// <param name="TotalProducedMessageCount">The total number of produced outbox messages reported for the execution runtime.</param>
/// <param name="LastAcknowledgement">The latest acknowledgement posture reported for the execution runtime when one is known.</param>
/// <param name="LastError">The latest operator-facing error summary visible for the execution runtime.</param>
/// <param name="ObservationFreshness">The latest aggregate report-freshness posture visible for the execution runtime.</param>
public sealed record CdcCaptureExecutionRuntimeSummary(
    IReadOnlyList<string> ReportedCdcCaptureIds,
    string? LastCdcCaptureId,
    string? LastOutcome,
    DateTimeOffset? LastObservedAtUtc,
    string? LastReportId,
    string? LastChangeId,
    string? LastCheckpoint,
    int StartedCount,
    int CapturedCount,
    int IdleCount,
    int FailedCount,
    long TotalCapturedChangeCount,
    long TotalProducedMessageCount,
    string? LastAcknowledgement,
    string? LastError,
    CdcCaptureFreshnessStatus ObservationFreshness)
{
    /// <summary>
    /// Gets an empty summary for execution runtimes that have not reported state yet.
    /// </summary>
    public static CdcCaptureExecutionRuntimeSummary Empty { get; } = new(
        ReportedCdcCaptureIds: [],
        LastCdcCaptureId: null,
        LastOutcome: null,
        LastObservedAtUtc: null,
        LastReportId: null,
        LastChangeId: null,
        LastCheckpoint: null,
        StartedCount: 0,
        CapturedCount: 0,
        IdleCount: 0,
        FailedCount: 0,
        TotalCapturedChangeCount: 0,
        TotalProducedMessageCount: 0,
        LastAcknowledgement: null,
        LastError: null,
        ObservationFreshness: new CdcCaptureFreshnessStatus(CdcCaptureFreshnessStates.Unknown));

    /// <summary>
    /// Gets the latest reporter identity visible for the execution runtime when one was reported.
    /// </summary>
    public string? LastReporterId { get; init; }

    /// <summary>
    /// Gets the currently active reporter identity when one reporter still holds an active lease for the execution runtime.
    /// </summary>
    public string? ActiveReporterId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the active reporter lease expires when one is known.
    /// </summary>
    public DateTimeOffset? ReporterLeaseExpiresAtUtc { get; init; }

    /// <summary>
    /// Gets the declared or observed edge-node identifiers that most recently reported runtime state for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> ObservedEdgeNodeIds { get; init; } = [];

    /// <summary>
    /// Gets the latest edge-node identifier visible for the execution runtime when one was reported.
    /// </summary>
    public string? LastEdgeNodeId { get; init; }

    /// <summary>
    /// Gets the number of distinct CDC captures that have reported runtime state for the execution runtime.
    /// </summary>
    public int ReportedCaptureCount => ReportedCdcCaptureIds.Count;

    /// <summary>
    /// Gets the total number of runtime observations visible for the execution runtime.
    /// </summary>
    public int TotalReports => StartedCount + CapturedCount + IdleCount + FailedCount;

    /// <summary>
    /// Gets a value indicating whether the execution runtime has reported any runtime observations yet.
    /// </summary>
    public bool HasReports => TotalReports > 0;

    /// <summary>
    /// Gets a value indicating whether at least one reported capture observation is now stale.
    /// </summary>
    public bool HasStaleObservations => string.Equals(ObservationFreshness.State, CdcCaptureFreshnessStates.Stale, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently has an active reporter lease.
    /// </summary>
    public bool HasActiveReporterLease => ReporterLeaseExpiresAtUtc.HasValue;
}
