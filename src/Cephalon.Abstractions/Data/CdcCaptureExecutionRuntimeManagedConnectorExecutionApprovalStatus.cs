namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector execution-approval and safety-gating posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus
{
    /// <summary>
    /// Creates a new managed-connector execution-approval answer.
    /// </summary>
    /// <param name="state">
    /// The stable execution-approval state, such as <c>auto-blocked</c>, <c>policy-blocked</c>, <c>approval-required</c>, <c>approval-ready</c>, <c>auto-eligible</c>, or <c>not-applicable</c>.
    /// </param>
    /// <param name="description">An optional operator-facing execution-approval summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector execution-approval state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector execution-approval state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing execution-approval summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable execution-approval categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable management-operation identifier currently associated with execution approval.
    /// </summary>
    public string OperationId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalOperationIds.None;

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the current runtime-level reporting-coverage state that informed execution approval.
    /// </summary>
    public string ReportingCoverageState { get; init; } = CdcCaptureExecutionRuntimeReportingCoverageStates.Unknown;

    /// <summary>
    /// Gets the current runtime-level remediation state that informed execution approval.
    /// </summary>
    public string RemediationState { get; init; } = CdcCaptureExecutionRuntimeRemediationStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector governance state that informed execution approval.
    /// </summary>
    public string GovernanceState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector drift state that informed execution approval.
    /// </summary>
    public string DriftState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector action-plan state that informed execution approval.
    /// </summary>
    public string ActionPlanState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector write-path readiness state that informed execution approval.
    /// </summary>
    public string WritePathReadinessState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector preflight state that informed execution approval.
    /// </summary>
    public string PreflightState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector dry-run state that informed execution approval.
    /// </summary>
    public string DryRunState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector execution-intent state that informed execution approval.
    /// </summary>
    public string ExecutionIntentState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.NotApplicable;

    /// <summary>
    /// Gets the primary action identifier currently associated with the runtime's managed-connector action plan.
    /// </summary>
    public string PrimaryActionId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.None;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive execution approval.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources.Unknown;

    /// <summary>
    /// Gets the primary confidence-source identifier already associated with managed-connector execution intent.
    /// </summary>
    public string ExecutionIntentConfidenceSourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentSources.Unknown;

    /// <summary>
    /// Gets the number of visible potential shared write-path changes currently associated with execution approval.
    /// </summary>
    public int PotentialChangeCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current intended follow-through would still apply one or more shared write-path changes.
    /// </summary>
    public bool WouldApplyChanges { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current intended follow-through would require an explicit approval gate.
    /// </summary>
    public bool RequiresExplicitApproval { get; init; }

    /// <summary>
    /// Gets the number of active execution-approval categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector is currently blocked by runtime truth or remediation before approval can be considered.
    /// </summary>
    public bool IsAutoBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.AutoBlocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector is currently blocked by governance or control-plane policy.
    /// </summary>
    public bool IsPolicyBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.PolicyBlocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector currently requires an elevated explicit approval gate.
    /// </summary>
    public bool IsApprovalRequired => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.ApprovalRequired, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector is currently ready to enter a future approval workflow.
    /// </summary>
    public bool IsApprovalReady => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.ApprovalReady, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector currently fits a future auto-execution lane.
    /// </summary>
    public bool IsAutoEligible => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.AutoEligible, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector can currently enter a future approval workflow.
    /// </summary>
    public bool CanRequestApproval => IsApprovalReady;

    /// <summary>
    /// Gets a value indicating whether the managed connector currently satisfies the shared auto-execution gate.
    /// </summary>
    public bool CanAutoExecuteThroughEngine => IsAutoEligible;

    /// <summary>
    /// Gets a value indicating whether the managed connector currently satisfies the shared safety-gating baseline.
    /// </summary>
    public bool HasSafetyGateClearance => IsApprovalReady || IsAutoEligible;

    /// <summary>
    /// Gets a value indicating whether the managed connector currently requires additional execution-approval attention.
    /// </summary>
    public bool RequiresAttention => IsAutoBlocked || IsPolicyBlocked || IsApprovalRequired || IsApprovalReady;
}
