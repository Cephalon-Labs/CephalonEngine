namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the latest operator-facing runtime state visible for one active CDC capture.
/// </summary>
/// <param name="CdcCaptureId">The stable CDC capture identifier.</param>
/// <param name="SourceModuleId">The identifier of the module that owns the CDC capture.</param>
/// <param name="Provider">The logical provider identifier that supplies the change feed.</param>
/// <param name="SourceId">The logical source stream, database, or feed identifier.</param>
/// <param name="OutboxId">The outbox identifier that receives captured publications.</param>
/// <param name="Mode">The capture mode such as <c>wal</c>, <c>change-stream</c>, or <c>table-tail</c>.</param>
/// <param name="EventFormat">The emitted change-event format such as <c>debezium-envelope</c>.</param>
/// <param name="ResourceIds">The resource identifiers observed by the capture.</param>
/// <param name="LastOutcome">The latest reported capture outcome identifier when one exists.</param>
/// <param name="LastObservedAtUtc">The UTC timestamp when the latest capture observation was reported.</param>
/// <param name="LastReportId">The latest stable report identifier when the active runtime supplied one.</param>
/// <param name="LastCapturedChangeCount">The number of source changes observed in the latest report.</param>
/// <param name="LastProducedMessageCount">The number of outbox messages produced by the latest report.</param>
/// <param name="StartedCount">The number of <c>started</c> observations reported so far.</param>
/// <param name="CapturedCount">The number of <c>captured</c> observations reported so far.</param>
/// <param name="IdleCount">The number of <c>idle</c> observations reported so far.</param>
/// <param name="FailedCount">The number of <c>failed</c> observations reported so far.</param>
/// <param name="TotalCapturedChangeCount">The total number of source changes reported so far.</param>
/// <param name="TotalProducedMessageCount">The total number of outbox messages produced so far.</param>
/// <param name="LastChangeId">The latest provider-facing change identifier when one was reported.</param>
/// <param name="LastCheckpoint">The latest provider-facing checkpoint or cursor when one was reported.</param>
/// <param name="LastError">The latest operator-facing error summary when one was reported.</param>
/// <param name="Freshness">The latest provider-facing freshness answer reported for the capture.</param>
/// <param name="ObservationFreshness">The latest report-freshness answer visible for the capture observation itself.</param>
/// <param name="Lag">The latest provider-facing lag answer reported for the capture.</param>
/// <param name="Publication">The latest publication posture answer reported for the capture.</param>
/// <param name="OutboxDispatchState">
/// The latest linked outbox dispatch state when the active runtime also reports publication posture for the capture's outbox.
/// </param>
/// <param name="Metadata">The operator-facing metadata captured by the latest report.</param>
public sealed record CdcCaptureRuntimeState(
    string CdcCaptureId,
    string SourceModuleId,
    string Provider,
    string SourceId,
    string OutboxId,
    string Mode,
    string EventFormat,
    IReadOnlyList<string> ResourceIds,
    string? LastOutcome,
    DateTimeOffset? LastObservedAtUtc,
    string? LastReportId,
    int LastCapturedChangeCount,
    int LastProducedMessageCount,
    int StartedCount,
    int CapturedCount,
    int IdleCount,
    int FailedCount,
    long TotalCapturedChangeCount,
    long TotalProducedMessageCount,
    string? LastChangeId,
    string? LastCheckpoint,
    string? LastError,
    CdcCaptureFreshnessStatus Freshness,
    CdcCaptureFreshnessStatus ObservationFreshness,
    CdcCaptureLagStatus Lag,
    CdcCapturePublicationStatus Publication,
    EventDispatchRuntimeState? OutboxDispatchState,
    IReadOnlyDictionary<string, string> Metadata)
{
    /// <summary>
    /// Gets the authored or effective execution-binding answer for the CDC capture.
    /// </summary>
    public CdcCaptureExecutionBindingDescriptor ExecutionBinding { get; init; } =
        CdcCaptureExecutionBindingDescriptor.Unbound(CdcCaptureId);

    /// <summary>
    /// Gets the latest reporter identity visible for the capture when one was reported.
    /// </summary>
    public string? LastReporterId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the latest reporter lease expires when one is known.
    /// </summary>
    public DateTimeOffset? ReporterLeaseExpiresAtUtc { get; init; }

    /// <summary>
    /// Gets the latest edge-node identifier visible for the capture when one was reported.
    /// </summary>
    public string? LastEdgeNodeId { get; init; }

    /// <summary>
    /// Gets the reporter-coordination posture currently visible for the capture's execution runtime.
    /// </summary>
    public CdcCaptureReporterCoordinationStatus ReporterCoordination { get; init; } =
        new(CdcCaptureReporterCoordinationStates.Unknown);

    /// <summary>
    /// Gets the total number of capture observations reported for the CDC capture.
    /// </summary>
    public int TotalReports => StartedCount + CapturedCount + IdleCount + FailedCount;

    /// <summary>
    /// Gets a value indicating whether the capture has reported any runtime observations yet.
    /// </summary>
    public bool HasReports => TotalReports > 0;

    /// <summary>
    /// Gets a value indicating whether the latest reported capture posture is failed.
    /// </summary>
    public bool IsFailed => string.Equals(LastOutcome, "failed", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the capture still has a provider-reported freshness window.
    /// </summary>
    public bool HasFreshnessWindow => Freshness.HasWindow;

    /// <summary>
    /// Gets a value indicating whether the capture observation still has a report-freshness window.
    /// </summary>
    public bool HasObservationFreshnessWindow => ObservationFreshness.HasWindow;

    /// <summary>
    /// Gets a value indicating whether the capture currently carries reporter-lease metadata.
    /// </summary>
    public bool HasReporterLease => ReporterLeaseExpiresAtUtc.HasValue;

    /// <summary>
    /// Gets a value indicating whether the capture's execution runtime currently has one active reporter owner.
    /// </summary>
    public bool HasActiveReporterOwner => ReporterCoordination.HasActiveReporter;

    /// <summary>
    /// Gets a value indicating whether the capture's execution runtime currently reports degraded reporter ownership.
    /// </summary>
    public bool HasReporterCoordinationIssue => ReporterCoordination.IsDegraded;

    /// <summary>
    /// Gets a value indicating whether the latest report is now stale according to the execution-runtime reporting policy.
    /// </summary>
    public bool IsObservationStale => string.Equals(ObservationFreshness.State, CdcCaptureFreshnessStates.Stale, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the capture still has provider-reported pending source changes.
    /// </summary>
    public bool HasPendingChanges => Lag.HasPendingChanges;

    /// <summary>
    /// Gets a value indicating whether the capture still has provider-reported pending publications.
    /// </summary>
    public bool HasPendingPublications => Publication.HasPendingPublications;

    /// <summary>
    /// Gets a value indicating whether the linked outbox dispatch path has reported runtime state.
    /// </summary>
    public bool HasDispatchReports => OutboxDispatchState?.TotalReports > 0;
}
