namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the grouped reporter-coordination story currently visible for one CDC capture execution runtime.
/// </summary>
/// <param name="CoordinationStateBreakdown">
/// The grouped reporter-coordination states currently visible across the execution runtime's reported CDC captures.
/// </param>
/// <param name="DegradedReasonBreakdown">
/// The grouped degraded-reason answers currently visible across the execution runtime's reported CDC captures.
/// </param>
public sealed record CdcCaptureExecutionRuntimeReporterCoordinationRollup(
    IReadOnlyList<CdcCaptureReporterCoordinationBreakdownEntry> CoordinationStateBreakdown,
    IReadOnlyList<CdcCaptureReporterCoordinationBreakdownEntry> DegradedReasonBreakdown)
{
    /// <summary>
    /// Gets an empty rollup for execution runtimes that have not reported state yet.
    /// </summary>
    public static CdcCaptureExecutionRuntimeReporterCoordinationRollup Empty { get; } = new(
        CoordinationStateBreakdown: [],
        DegradedReasonBreakdown: []);

    /// <summary>
    /// Gets the reporter identities currently visible as active owners on at least one CDC capture.
    /// </summary>
    public IReadOnlyList<string> ActiveReporterIds { get; init; } = [];

    /// <summary>
    /// Gets the reporter identities currently visible as standby participants on at least one CDC capture.
    /// </summary>
    public IReadOnlyList<string> StandbyReporterIds { get; init; } = [];

    /// <summary>
    /// Gets the reporter identities currently visible as rejected participants on at least one CDC capture.
    /// </summary>
    public IReadOnlyList<string> RejectedReporterIds { get; init; } = [];

    /// <summary>
    /// Gets the CDC capture identifiers whose current coordination posture is degraded.
    /// </summary>
    public IReadOnlyList<string> DegradedCdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the number of distinct active reporters currently visible across the execution runtime.
    /// </summary>
    public int ActiveReporterCount => ActiveReporterIds.Count;

    /// <summary>
    /// Gets the number of distinct standby reporters currently visible across the execution runtime.
    /// </summary>
    public int StandbyReporterCount => StandbyReporterIds.Count;

    /// <summary>
    /// Gets the number of distinct rejected reporters currently visible across the execution runtime.
    /// </summary>
    public int RejectedReporterCount => RejectedReporterIds.Count;

    /// <summary>
    /// Gets the number of CDC captures currently reporting degraded coordination posture.
    /// </summary>
    public int DegradedCdcCaptureCount => DegradedCdcCaptureIds.Count;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently exposes standby reporters anywhere in its coordination story.
    /// </summary>
    public bool HasStandbyReporters => StandbyReporterCount > 0;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently exposes rejected reporters anywhere in its coordination story.
    /// </summary>
    public bool HasRejectedReporters => RejectedReporterCount > 0;

    /// <summary>
    /// Gets a value indicating whether any CDC capture currently reports degraded coordination posture for the execution runtime.
    /// </summary>
    public bool HasDegradedCdcCaptures => DegradedCdcCaptureCount > 0;
}
