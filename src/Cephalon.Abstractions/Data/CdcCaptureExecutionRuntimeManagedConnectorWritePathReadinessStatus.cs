namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector write-path readiness posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus
{
    /// <summary>
    /// Creates a new managed-connector write-path readiness answer.
    /// </summary>
    /// <param name="state">
    /// The stable readiness state, such as <c>deferred</c>, <c>not-ready</c>, <c>ready</c>, <c>blocked</c>, or <c>not-applicable</c>.
    /// </param>
    /// <param name="description">An optional operator-facing readiness summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector write-path readiness state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector write-path readiness state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing readiness summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable readiness categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the current runtime-level reporting-coverage state that informed write-path readiness.
    /// </summary>
    public string ReportingCoverageState { get; init; } = CdcCaptureExecutionRuntimeReportingCoverageStates.Unknown;

    /// <summary>
    /// Gets the current runtime-level remediation state that informed write-path readiness.
    /// </summary>
    public string RemediationState { get; init; } = CdcCaptureExecutionRuntimeRemediationStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector governance state that informed write-path readiness.
    /// </summary>
    public string GovernanceState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector drift state that informed write-path readiness.
    /// </summary>
    public string DriftState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector action-plan state that informed write-path readiness.
    /// </summary>
    public string ActionPlanState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.NotApplicable;

    /// <summary>
    /// Gets the primary action identifier currently associated with the runtime's managed-connector action plan.
    /// </summary>
    public string PrimaryActionId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.None;

    /// <summary>
    /// Gets the number of active readiness categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether write-path readiness is currently deferred because the runtime remains observe-only.
    /// </summary>
    public bool IsDeferred => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.Deferred, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector currently satisfies the shared write-path readiness baseline.
    /// </summary>
    public bool IsReady => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.Ready, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector is currently blocked before write-path readiness can be considered.
    /// </summary>
    public bool IsBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.Blocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector currently requires additional readiness follow-through.
    /// </summary>
    public bool RequiresAttention =>
        string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotReady, StringComparison.OrdinalIgnoreCase) ||
        IsBlocked;
}
