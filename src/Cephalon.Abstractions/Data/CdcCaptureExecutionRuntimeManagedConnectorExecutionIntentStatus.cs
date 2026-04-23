namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector execution intent for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus
{
    /// <summary>
    /// Creates a new managed-connector execution-intent answer.
    /// </summary>
    /// <param name="state">
    /// The stable execution-intent state, such as <c>deferred</c>, <c>blocked</c>, <c>operator-action</c>, <c>requires-approval</c>, <c>ready-to-execute</c>, or <c>not-applicable</c>.
    /// </param>
    /// <param name="description">An optional operator-facing execution-intent summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector execution-intent state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector execution-intent state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing execution-intent summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable execution-intent categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable management-operation identifier Cephalon currently intends to execute next.
    /// </summary>
    public string OperationId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentOperationIds.None;

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the current runtime-level reporting-coverage state that informed execution intent.
    /// </summary>
    public string ReportingCoverageState { get; init; } = CdcCaptureExecutionRuntimeReportingCoverageStates.Unknown;

    /// <summary>
    /// Gets the current runtime-level remediation state that informed execution intent.
    /// </summary>
    public string RemediationState { get; init; } = CdcCaptureExecutionRuntimeRemediationStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector governance state that informed execution intent.
    /// </summary>
    public string GovernanceState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector drift state that informed execution intent.
    /// </summary>
    public string DriftState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector action-plan state that informed execution intent.
    /// </summary>
    public string ActionPlanState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector write-path readiness state that informed execution intent.
    /// </summary>
    public string WritePathReadinessState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector preflight state that informed execution intent.
    /// </summary>
    public string PreflightState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector dry-run state that informed execution intent.
    /// </summary>
    public string DryRunState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.NotApplicable;

    /// <summary>
    /// Gets the primary action identifier currently associated with the runtime's managed-connector action plan.
    /// </summary>
    public string PrimaryActionId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.None;

    /// <summary>
    /// Gets the primary confidence-source identifier Cephalon used to derive execution intent.
    /// </summary>
    public string ConfidenceSourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentSources.Unknown;

    /// <summary>
    /// Gets the number of visible potential shared write-path changes in the current execution intent.
    /// </summary>
    public int PotentialChangeCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current execution intent would still apply one or more shared write-path changes.
    /// </summary>
    public bool WouldApplyChanges { get; init; }

    /// <summary>
    /// Gets the number of active execution-intent categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether execution intent is currently deferred because the runtime remains observe-only.
    /// </summary>
    public bool IsDeferred => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.Deferred, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector is currently blocked before Cephalon can trust execution intent.
    /// </summary>
    public bool IsBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.Blocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the next intended follow-through currently remains operator-owned.
    /// </summary>
    public bool IsOperatorAction => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.OperatorAction, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the next intended follow-through would still require an approval gate before engine execution.
    /// </summary>
    public bool IsApprovalRequired => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.RequiresApproval, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the next intended follow-through currently sits inside the future engine-execution lane.
    /// </summary>
    public bool CanExecuteThroughEngine => IsApprovalRequired || IsReadyToExecute;

    /// <summary>
    /// Gets a value indicating whether the current execution intent is ready for a future engine-execution lane.
    /// </summary>
    public bool IsReadyToExecute => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.ReadyToExecute, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the next intended follow-through still belongs to an operator-owned lane.
    /// </summary>
    public bool IsOperatorOnly => IsOperatorAction;

    /// <summary>
    /// Gets a value indicating whether the managed connector currently requires additional execution-intent attention.
    /// </summary>
    public bool RequiresAttention => IsBlocked || IsOperatorAction || IsApprovalRequired;
}
