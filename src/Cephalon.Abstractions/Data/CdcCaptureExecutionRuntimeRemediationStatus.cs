namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing remediation posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeRemediationStatus
{
    /// <summary>
    /// Creates a new execution-runtime remediation answer.
    /// </summary>
    /// <param name="state">
    /// The stable remediation state, such as <c>ready</c>, <c>attention</c>, or <c>blocked</c>.
    /// </param>
    /// <param name="description">An optional operator-facing remediation summary.</param>
    public CdcCaptureExecutionRuntimeRemediationStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Remediation state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable remediation state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing remediation summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable remediation categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the CDC capture identifiers currently affected by active remediation work.
    /// </summary>
    public IReadOnlyList<string> AffectedCdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the declared CDC capture identifiers that have not reported runtime state yet.
    /// </summary>
    public IReadOnlyList<string> UnreportedCdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the CDC capture identifiers whose latest reported runtime observation is stale.
    /// </summary>
    public IReadOnlyList<string> StaleCdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the CDC capture identifiers whose latest reported runtime outcome is failed.
    /// </summary>
    public IReadOnlyList<string> FailedCdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the CDC capture identifiers whose latest runtime story reports degraded reporter coordination.
    /// </summary>
    public IReadOnlyList<string> ReporterCoordinationIssueCdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the number of active remediation categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets the number of CDC captures currently affected by active remediation work.
    /// </summary>
    public int AffectedCaptureCount => AffectedCdcCaptureIds.Count;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently requires operator remediation.
    /// </summary>
    public bool RequiresRemediation =>
        string.Equals(State, CdcCaptureExecutionRuntimeRemediationStates.Attention, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(State, CdcCaptureExecutionRuntimeRemediationStates.Blocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the execution runtime is currently blocked by failed CDC captures.
    /// </summary>
    public bool IsBlocked => string.Equals(State, CdcCaptureExecutionRuntimeRemediationStates.Blocked, StringComparison.OrdinalIgnoreCase);
}
