namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the declared-versus-reported coverage currently visible for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeReportingCoverageStatus
{
    /// <summary>
    /// Creates a new execution-runtime reporting-coverage answer.
    /// </summary>
    /// <param name="state">
    /// The stable reporting-coverage state, such as <c>unreported</c>, <c>partially-reported</c>, or <c>fully-reported</c>.
    /// </param>
    /// <param name="description">An optional operator-facing reporting-coverage summary.</param>
    public CdcCaptureExecutionRuntimeReportingCoverageStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Reporting coverage state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable reporting-coverage state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing reporting-coverage summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the number of declared CDC captures currently owned by the execution runtime.
    /// </summary>
    public int DeclaredCaptureCount { get; init; }

    /// <summary>
    /// Gets the number of declared CDC captures that have reported runtime state.
    /// </summary>
    public int ReportedCaptureCount { get; init; }

    /// <summary>
    /// Gets the declared CDC capture identifiers that have not reported runtime state yet.
    /// </summary>
    public IReadOnlyList<string> UnreportedCdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the number of declared CDC captures that have not reported runtime state yet.
    /// </summary>
    public int UnreportedCaptureCount => UnreportedCdcCaptureIds.Count;

    /// <summary>
    /// Gets a value indicating whether any declared CDC captures still have not reported runtime state.
    /// </summary>
    public bool HasUnreportedCdcCaptures => UnreportedCaptureCount > 0;

    /// <summary>
    /// Gets a value indicating whether the execution runtime has reported every declared CDC capture.
    /// </summary>
    public bool HasFullCoverage =>
        DeclaredCaptureCount > 0 &&
        ReportedCaptureCount >= DeclaredCaptureCount &&
        UnreportedCaptureCount == 0;
}
