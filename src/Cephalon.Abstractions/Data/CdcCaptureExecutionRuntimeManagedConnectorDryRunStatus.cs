namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector dry-run posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus
{
    /// <summary>
    /// Creates a new managed-connector dry-run answer.
    /// </summary>
    /// <param name="state">
    /// The stable dry-run state, such as <c>deferred</c>, <c>blocked</c>, <c>no-op</c>, <c>would-change</c>, or <c>not-applicable</c>.
    /// </param>
    /// <param name="description">An optional operator-facing dry-run summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector dry-run state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector dry-run state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing dry-run summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable dry-run categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable management-operation identifier Cephalon would currently preview.
    /// </summary>
    public string OperationId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDryRunOperationIds.None;

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the current runtime-level reporting-coverage state that informed the dry-run answer.
    /// </summary>
    public string ReportingCoverageState { get; init; } = CdcCaptureExecutionRuntimeReportingCoverageStates.Unknown;

    /// <summary>
    /// Gets the current runtime-level remediation state that informed the dry-run answer.
    /// </summary>
    public string RemediationState { get; init; } = CdcCaptureExecutionRuntimeRemediationStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector governance state that informed the dry-run answer.
    /// </summary>
    public string GovernanceState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector drift state that informed the dry-run answer.
    /// </summary>
    public string DriftState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector action-plan state that informed the dry-run answer.
    /// </summary>
    public string ActionPlanState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector write-path readiness state that informed the dry-run answer.
    /// </summary>
    public string WritePathReadinessState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector preflight state that informed the dry-run answer.
    /// </summary>
    public string PreflightState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotApplicable;

    /// <summary>
    /// Gets the primary action identifier currently associated with the runtime's managed-connector action plan.
    /// </summary>
    public string PrimaryActionId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.None;

    /// <summary>
    /// Gets the latest reported connector lifecycle state when one is known.
    /// </summary>
    public string? ConnectorLifecycleState { get; init; }

    /// <summary>
    /// Gets the latest reported overall reconciliation state when one is known.
    /// </summary>
    public string? ReconciliationState { get; init; }

    /// <summary>
    /// Gets the declared task ids that are currently missing from the latest reported task set.
    /// </summary>
    public IReadOnlyList<string> MissingDeclaredTaskIds { get; init; } = [];

    /// <summary>
    /// Gets the reported task ids that were not part of the declared task baseline.
    /// </summary>
    public IReadOnlyList<string> UnexpectedReportedTaskIds { get; init; } = [];

    /// <summary>
    /// Gets the number of visible potential shared write-path changes in the current dry-run answer.
    /// </summary>
    public int PotentialChangeCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current dry-run answer includes one or more shared write-path changes.
    /// </summary>
    public bool WouldApplyChanges { get; init; }

    /// <summary>
    /// Gets the number of active dry-run categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether dry-run follow-through is currently deferred because the runtime remains observe-only.
    /// </summary>
    public bool IsDeferred => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.Deferred, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector is currently blocked before a dry-run answer can be trusted.
    /// </summary>
    public bool IsBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.Blocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the intended management operation would currently produce no shared write-path changes.
    /// </summary>
    public bool IsNoOp => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.NoOp, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the intended management operation would currently produce one or more shared write-path changes.
    /// </summary>
    public bool IsWouldChange => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.WouldChange, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector currently requires additional dry-run attention.
    /// </summary>
    public bool RequiresAttention => IsBlocked || WouldApplyChanges;
}
