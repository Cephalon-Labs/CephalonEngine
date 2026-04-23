namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector write-path command envelope for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus
{
    /// <summary>
    /// Creates a new managed-connector command-envelope answer.
    /// </summary>
    /// <param name="state">
    /// The stable command-envelope state, such as <c>blocked</c>, <c>operator-only</c>, <c>approval-gated</c>, <c>engine-ready</c>, or <c>not-applicable</c>.
    /// </param>
    /// <param name="description">An optional operator-facing command-envelope summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector command-envelope state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector command-envelope state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing command-envelope summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable command-envelope categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable management-operation identifier currently associated with the command envelope.
    /// </summary>
    public string OperationId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.None;

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the current runtime-level reporting-coverage state that informed the command envelope.
    /// </summary>
    public string ReportingCoverageState { get; init; } = CdcCaptureExecutionRuntimeReportingCoverageStates.Unknown;

    /// <summary>
    /// Gets the current runtime-level remediation state that informed the command envelope.
    /// </summary>
    public string RemediationState { get; init; } = CdcCaptureExecutionRuntimeRemediationStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector governance state that informed the command envelope.
    /// </summary>
    public string GovernanceState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector drift state that informed the command envelope.
    /// </summary>
    public string DriftState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector action-plan state that informed the command envelope.
    /// </summary>
    public string ActionPlanState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector write-path readiness state that informed the command envelope.
    /// </summary>
    public string WritePathReadinessState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector preflight state that informed the command envelope.
    /// </summary>
    public string PreflightState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector dry-run state that informed the command envelope.
    /// </summary>
    public string DryRunState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector execution-intent state that informed the command envelope.
    /// </summary>
    public string ExecutionIntentState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector execution-approval state that informed the command envelope.
    /// </summary>
    public string ExecutionApprovalState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.NotApplicable;

    /// <summary>
    /// Gets the primary action identifier currently associated with the runtime's managed-connector action plan.
    /// </summary>
    public string PrimaryActionId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.None;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive the command envelope.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources.Unknown;

    /// <summary>
    /// Gets the primary confidence-source identifier already associated with managed-connector execution intent.
    /// </summary>
    public string ExecutionIntentConfidenceSourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentSources.Unknown;

    /// <summary>
    /// Gets the primary safety-gating source identifier already associated with managed-connector execution approval.
    /// </summary>
    public string ExecutionApprovalSourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources.Unknown;

    /// <summary>
    /// Gets the number of visible potential shared write-path changes currently associated with the command envelope.
    /// </summary>
    public int PotentialChangeCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current command envelope would still apply one or more shared write-path changes.
    /// </summary>
    public bool WouldApplyChanges { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current command envelope still requires an explicit approval gate.
    /// </summary>
    public bool RequiresExplicitApproval { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current command envelope targets a destructive connector operation.
    /// </summary>
    public bool IsDestructiveOperation { get; init; }

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with the command envelope.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with the command envelope.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the best available connector-cluster identifier Cephalon would target for the command envelope.
    /// </summary>
    public string? ConnectClusterId { get; init; }

    /// <summary>
    /// Gets the best available connector-class identifier Cephalon would target for the command envelope.
    /// </summary>
    public string? ConnectorClass { get; init; }

    /// <summary>
    /// Gets the best available source-provider identifier Cephalon would target for the command envelope.
    /// </summary>
    public string? SourceProviderId { get; init; }

    /// <summary>
    /// Gets the deterministic command fingerprint Cephalon currently derives for the managed connector.
    /// </summary>
    public string CommandFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of active command-envelope categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector currently remains blocked before Cephalon can trust the command envelope.
    /// </summary>
    public bool IsBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.Blocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector currently remains operator-owned.
    /// </summary>
    public bool IsOperatorOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.OperatorOnly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector currently sits behind an approval gate.
    /// </summary>
    public bool IsApprovalGated => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.ApprovalGated, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector currently sits on an engine-ready execution lane.
    /// </summary>
    public bool IsEngineReady => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.EngineReady, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the command envelope currently targets a concrete managed-connector operation.
    /// </summary>
    public bool HasCommandTarget =>
        AppliesToManagedConnector &&
        !string.Equals(OperationId, CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeOperationIds.None, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the command envelope currently fits a future engine-execution lane.
    /// </summary>
    public bool CanPrepareFutureExecutionLane => IsApprovalGated || IsEngineReady;

    /// <summary>
    /// Gets a value indicating whether the managed connector currently requires additional command-envelope attention.
    /// </summary>
    public bool RequiresAttention => IsBlocked || IsOperatorOnly || IsApprovalGated;
}
