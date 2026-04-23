namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector provider execution-adapter posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus
{
    /// <summary>
    /// Creates a new managed-connector execution-adapter answer.
    /// </summary>
    /// <param name="state">
    /// The stable execution-adapter state, such as <c>blocked</c>, <c>operator-only</c>, <c>unavailable</c>, <c>ready</c>, or <c>not-applicable</c>.
    /// </param>
    /// <param name="description">An optional operator-facing execution-adapter summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector execution-adapter state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector execution-adapter state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing execution-adapter summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable execution-adapter categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable management-operation identifier currently associated with the execution adapter.
    /// </summary>
    public string OperationId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None;

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the current runtime-level reporting-coverage state that informed the execution adapter.
    /// </summary>
    public string ReportingCoverageState { get; init; } = CdcCaptureExecutionRuntimeReportingCoverageStates.Unknown;

    /// <summary>
    /// Gets the current runtime-level remediation state that informed the execution adapter.
    /// </summary>
    public string RemediationState { get; init; } = CdcCaptureExecutionRuntimeRemediationStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector governance state that informed the execution adapter.
    /// </summary>
    public string GovernanceState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector drift state that informed the execution adapter.
    /// </summary>
    public string DriftState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector action-plan state that informed the execution adapter.
    /// </summary>
    public string ActionPlanState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector write-path readiness state that informed the execution adapter.
    /// </summary>
    public string WritePathReadinessState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector preflight state that informed the execution adapter.
    /// </summary>
    public string PreflightState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector dry-run state that informed the execution adapter.
    /// </summary>
    public string DryRunState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector execution-intent state that informed the execution adapter.
    /// </summary>
    public string ExecutionIntentState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector execution-approval state that informed the execution adapter.
    /// </summary>
    public string ExecutionApprovalState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-envelope state that informed the execution adapter.
    /// </summary>
    public string CommandEnvelopeState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-issuance state that informed the execution adapter.
    /// </summary>
    public string CommandIssuanceState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.NotApplicable;

    /// <summary>
    /// Gets the primary action identifier currently associated with the runtime's managed-connector action plan.
    /// </summary>
    public string PrimaryActionId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.None;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive the execution adapter.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources.Unknown;

    /// <summary>
    /// Gets the primary source identifier already associated with managed-connector command issuance.
    /// </summary>
    public string CommandIssuanceSourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources.Unknown;

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
    /// Gets the stable provider execution-adapter identifier currently associated with the execution runtime.
    /// </summary>
    public string AdapterId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.None;

    /// <summary>
    /// Gets the number of visible potential shared write-path changes currently associated with the execution adapter.
    /// </summary>
    public int PotentialChangeCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current execution adapter would still apply one or more shared write-path changes.
    /// </summary>
    public bool WouldApplyChanges { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current execution adapter still requires an explicit approval gate.
    /// </summary>
    public bool RequiresExplicitApproval { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current execution adapter targets a destructive connector operation.
    /// </summary>
    public bool IsDestructiveOperation { get; init; }

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with the execution adapter.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with the execution adapter.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the best available connector-cluster identifier Cephalon would target through the execution adapter.
    /// </summary>
    public string? ConnectClusterId { get; init; }

    /// <summary>
    /// Gets the best available connector-class identifier Cephalon would target through the execution adapter.
    /// </summary>
    public string? ConnectorClass { get; init; }

    /// <summary>
    /// Gets the best available source-provider identifier Cephalon would target through the execution adapter.
    /// </summary>
    public string? SourceProviderId { get; init; }

    /// <summary>
    /// Gets the deterministic command fingerprint already associated with the current managed connector.
    /// </summary>
    public string CommandFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic issuance fingerprint already associated with the current managed connector.
    /// </summary>
    public string IssuanceFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic execution-adapter fingerprint Cephalon currently derives for the managed connector.
    /// </summary>
    public string AdapterFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of active execution-adapter categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector currently remains blocked before Cephalon can trust the provider execution adapter.
    /// </summary>
    public bool IsBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Blocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector currently remains operator-owned.
    /// </summary>
    public bool IsOperatorOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.OperatorOnly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether no provider execution adapter is currently registered for the runtime.
    /// </summary>
    public bool IsUnavailable => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Unavailable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether a matching provider execution adapter is ready for the current managed connector.
    /// </summary>
    public bool IsReady => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.Ready, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the execution adapter currently targets a concrete managed-connector operation.
    /// </summary>
    public bool HasAdaptableCommand =>
        AppliesToManagedConnector &&
        !string.Equals(OperationId, CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the current provider execution-adapter lane can be used by a matching provider pack.
    /// </summary>
    public bool CanUseProviderExecutionAdapter => IsReady;

    /// <summary>
    /// Gets a value indicating whether the managed connector currently requires additional execution-adapter attention.
    /// </summary>
    public bool RequiresAttention => IsBlocked || IsOperatorOnly || IsUnavailable;
}
