namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector command-issuance posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus
{
    /// <summary>
    /// Creates a new managed-connector command-issuance answer.
    /// </summary>
    /// <param name="state">
    /// The stable command-issuance state, such as <c>blocked</c>, <c>operator-only</c>, <c>accepted</c>, <c>rejected</c>, <c>issued</c>, or <c>not-applicable</c>.
    /// </param>
    /// <param name="description">An optional operator-facing command-issuance summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector command-issuance state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector command-issuance state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing command-issuance summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable command-issuance categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable management-operation identifier currently associated with command issuance.
    /// </summary>
    public string OperationId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.None;

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the current runtime-level reporting-coverage state that informed command issuance.
    /// </summary>
    public string ReportingCoverageState { get; init; } = CdcCaptureExecutionRuntimeReportingCoverageStates.Unknown;

    /// <summary>
    /// Gets the current runtime-level remediation state that informed command issuance.
    /// </summary>
    public string RemediationState { get; init; } = CdcCaptureExecutionRuntimeRemediationStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector governance state that informed command issuance.
    /// </summary>
    public string GovernanceState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector drift state that informed command issuance.
    /// </summary>
    public string DriftState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector action-plan state that informed command issuance.
    /// </summary>
    public string ActionPlanState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector write-path readiness state that informed command issuance.
    /// </summary>
    public string WritePathReadinessState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector preflight state that informed command issuance.
    /// </summary>
    public string PreflightState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector dry-run state that informed command issuance.
    /// </summary>
    public string DryRunState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector execution-intent state that informed command issuance.
    /// </summary>
    public string ExecutionIntentState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector execution-approval state that informed command issuance.
    /// </summary>
    public string ExecutionApprovalState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-envelope state that informed command issuance.
    /// </summary>
    public string CommandEnvelopeState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.NotApplicable;

    /// <summary>
    /// Gets the primary action identifier currently associated with the runtime's managed-connector action plan.
    /// </summary>
    public string PrimaryActionId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.None;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive command issuance.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources.Unknown;

    /// <summary>
    /// Gets the primary source identifier already associated with managed-connector command envelopes.
    /// </summary>
    public string CommandEnvelopeSourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources.Unknown;

    /// <summary>
    /// Gets the primary confidence-source identifier already associated with managed-connector execution intent.
    /// </summary>
    public string ExecutionIntentConfidenceSourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentSources.Unknown;

    /// <summary>
    /// Gets the primary safety-gating source identifier already associated with managed-connector execution approval.
    /// </summary>
    public string ExecutionApprovalSourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources.Unknown;

    /// <summary>
    /// Gets the number of visible potential shared write-path changes currently associated with command issuance.
    /// </summary>
    public int PotentialChangeCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current command issuance would still apply one or more shared write-path changes.
    /// </summary>
    public bool WouldApplyChanges { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current command issuance still requires an explicit approval gate.
    /// </summary>
    public bool RequiresExplicitApproval { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current command issuance targets a destructive connector operation.
    /// </summary>
    public bool IsDestructiveOperation { get; init; }

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with command issuance.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with command issuance.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the best available connector-cluster identifier Cephalon would target for command issuance.
    /// </summary>
    public string? ConnectClusterId { get; init; }

    /// <summary>
    /// Gets the best available connector-class identifier Cephalon would target for command issuance.
    /// </summary>
    public string? ConnectorClass { get; init; }

    /// <summary>
    /// Gets the best available source-provider identifier Cephalon would target for command issuance.
    /// </summary>
    public string? SourceProviderId { get; init; }

    /// <summary>
    /// Gets the deterministic command fingerprint already associated with the current managed connector.
    /// </summary>
    public string CommandFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic issuance fingerprint Cephalon currently derives for the shared issuance lane.
    /// </summary>
    public string IssuanceFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of active command-issuance categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector currently remains blocked before Cephalon can trust the shared issuance lane.
    /// </summary>
    public bool IsBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Blocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector currently remains operator-owned.
    /// </summary>
    public bool IsOperatorOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.OperatorOnly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector has been accepted onto a future shared issuance lane.
    /// </summary>
    public bool IsAccepted => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Accepted, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector has been rejected on the shared issuance lane.
    /// </summary>
    public bool IsRejected => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Rejected, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector has been marked as issued on the shared issuance lane.
    /// </summary>
    public bool IsIssued => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Issued, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether command issuance currently targets a concrete managed-connector operation.
    /// </summary>
    public bool HasIssuableCommand =>
        AppliesToManagedConnector &&
        !string.Equals(OperationId, CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceOperationIds.None, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the current command issuance can advance through a future shared issuance lane.
    /// </summary>
    public bool CanEnterFutureIssuanceLane => IsAccepted || IsIssued;

    /// <summary>
    /// Gets a value indicating whether the managed connector currently requires additional command-issuance attention.
    /// </summary>
    public bool RequiresAttention => IsBlocked || IsOperatorOnly || IsAccepted;
}
