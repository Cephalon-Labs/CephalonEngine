namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector automatic background retry execution posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStatus
{
    /// <summary>
    /// Creates a new managed-connector automatic background retry execution answer.
    /// </summary>
    /// <param name="state">
    /// The stable automatic background retry execution state, such as <c>not-applicable</c>, <c>disabled</c>, <c>blocked</c>, <c>eligible</c>, or <c>completed</c>.
    /// </param>
    /// <param name="description">An optional operator-facing automatic background retry execution summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector automatic background retry execution state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector automatic background retry execution state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing automatic background retry execution summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable automatic background retry execution categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable management-operation identifier currently associated with automatic background retry execution.
    /// </summary>
    public string OperationId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionOperationIds.None;

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the current runtime-level reporting-coverage state that informed automatic background retry execution.
    /// </summary>
    public string ReportingCoverageState { get; init; } = CdcCaptureExecutionRuntimeReportingCoverageStates.Unknown;

    /// <summary>
    /// Gets the current runtime-level remediation state that informed automatic background retry execution.
    /// </summary>
    public string RemediationState { get; init; } = CdcCaptureExecutionRuntimeRemediationStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector governance state that informed automatic background retry execution.
    /// </summary>
    public string GovernanceState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector drift state that informed automatic background retry execution.
    /// </summary>
    public string DriftState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector action-plan state that informed automatic background retry execution.
    /// </summary>
    public string ActionPlanState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector write-path readiness state that informed automatic background retry execution.
    /// </summary>
    public string WritePathReadinessState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector preflight state that informed automatic background retry execution.
    /// </summary>
    public string PreflightState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector dry-run state that informed automatic background retry execution.
    /// </summary>
    public string DryRunState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector execution-intent state that informed automatic background retry execution.
    /// </summary>
    public string ExecutionIntentState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector execution-approval state that informed automatic background retry execution.
    /// </summary>
    public string ExecutionApprovalState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-envelope state that informed automatic background retry execution.
    /// </summary>
    public string CommandEnvelopeState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-issuance state that informed automatic background retry execution.
    /// </summary>
    public string CommandIssuanceState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector execution-adapter state that informed automatic background retry execution.
    /// </summary>
    public string ExecutionAdapterState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.NotApplicable;

    /// <summary>
    /// Gets the latest recorded managed-connector command-execution state visible to automatic background retry execution.
    /// </summary>
    public string LatestCommandExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded;

    /// <summary>
    /// Gets the current managed-connector command-retry state that informed automatic background retry execution.
    /// </summary>
    public string CommandRetryState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector retry-execution policy state that informed automatic background retry execution.
    /// </summary>
    public string RetryExecutionPolicyState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-journal state that informed automatic background retry execution.
    /// </summary>
    public string CommandJournalState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.NotApplicable;

    /// <summary>
    /// Gets the primary action identifier currently associated with the runtime's managed-connector action plan.
    /// </summary>
    public string PrimaryActionId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.None;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive automatic background retry execution.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionSources.Unknown;

    /// <summary>
    /// Gets the primary source identifier already associated with retry-execution policy.
    /// </summary>
    public string RetryExecutionPolicySourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicySources.Unknown;

    /// <summary>
    /// Gets the primary source identifier already associated with the command journal.
    /// </summary>
    public string CommandJournalSourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalSources.Unknown;

    /// <summary>
    /// Gets the invocation source identifier of the latest recorded command-execution outcome.
    /// </summary>
    public string LatestCommandExecutionInvocationSourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.None;

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with automatic background retry execution.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with automatic background retry execution.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the best available connector-cluster identifier currently associated with automatic background retry execution.
    /// </summary>
    public string? ConnectClusterId { get; init; }

    /// <summary>
    /// Gets the best available connector-class identifier currently associated with automatic background retry execution.
    /// </summary>
    public string? ConnectorClass { get; init; }

    /// <summary>
    /// Gets the best available source-provider identifier currently associated with automatic background retry execution.
    /// </summary>
    public string? SourceProviderId { get; init; }

    /// <summary>
    /// Gets the number of visible potential shared write-path changes currently associated with automatic background retry execution.
    /// </summary>
    public int PotentialChangeCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current automatic background retry execution answer would still apply one or more shared write-path changes.
    /// </summary>
    public bool WouldApplyChanges { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current automatic background retry execution answer still requires an explicit approval gate.
    /// </summary>
    public bool RequiresExplicitApproval { get; init; }

    /// <summary>
    /// Gets a value indicating whether matching command history already recorded an explicit approval Cephalon can reuse for automatic background retry execution.
    /// </summary>
    public bool LatestMatchingApprovalApplied { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current automatic background retry execution answer targets a destructive connector operation.
    /// </summary>
    public bool IsDestructiveOperation { get; init; }

    /// <summary>
    /// Gets a value indicating whether matching command history already recorded an explicit destructive-operation allowance Cephalon can reuse for automatic background retry execution.
    /// </summary>
    public bool LatestMatchingDestructiveAllowanceApplied { get; init; }

    /// <summary>
    /// Gets a value indicating whether Cephalon can reuse approval context from matching command history for automatic background retry execution.
    /// </summary>
    public bool CanReuseApprovalFromMatchingHistory { get; init; }

    /// <summary>
    /// Gets a value indicating whether Cephalon can reuse destructive-operation allowance from matching command history for automatic background retry execution.
    /// </summary>
    public bool CanReuseDestructiveAllowanceFromMatchingHistory { get; init; }

    /// <summary>
    /// Gets a value indicating whether automatic background retry execution is enabled for the current runtime.
    /// </summary>
    public bool IsAutomaticRetryEnabled { get; init; }

    /// <summary>
    /// Gets the deterministic command fingerprint currently associated with automatic background retry execution.
    /// </summary>
    public string CommandFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic retry fingerprint currently associated with automatic background retry execution.
    /// </summary>
    public string RetryFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic latest recorded execution fingerprint currently visible to automatic background retry execution.
    /// </summary>
    public string LatestExecutionFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the stable latest recorded command-execution attempt identifier when one exists.
    /// </summary>
    public string LatestAttemptId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when Cephalon recorded the latest command-execution outcome that informed automatic background retry execution.
    /// </summary>
    public DateTimeOffset? LatestRecordedAtUtc { get; init; }

    /// <summary>
    /// Gets the timestamp when the active retry cooldown window ends, when one applies.
    /// </summary>
    public DateTimeOffset? CooldownUntilUtc { get; init; }

    /// <summary>
    /// Gets a value indicating whether Cephalon has already recorded one automatic background retry attempt.
    /// </summary>
    public bool HasAutomaticRetryAttempt { get; init; }

    /// <summary>
    /// Gets a value indicating whether Cephalon has already recorded one automatic background retry attempt matching the current retry fingerprint.
    /// </summary>
    public bool HasMatchingAutomaticRetryAttempt { get; init; }

    /// <summary>
    /// Gets the state of the latest recorded automatic background retry attempt when one exists.
    /// </summary>
    public string LatestAutomaticRetryState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded;

    /// <summary>
    /// Gets the stable attempt identifier of the latest recorded automatic background retry attempt when one exists.
    /// </summary>
    public string LatestAutomaticRetryAttemptId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when Cephalon recorded the latest automatic background retry attempt when one exists.
    /// </summary>
    public DateTimeOffset? LatestAutomaticRetryRecordedAtUtc { get; init; }

    /// <summary>
    /// Gets the deterministic execution fingerprint of the latest recorded automatic background retry attempt when one exists.
    /// </summary>
    public string LatestAutomaticRetryExecutionFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of active automatic background retry execution categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether automatic background retry execution is currently disabled.
    /// </summary>
    public bool IsDisabled => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.Disabled, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether automatic background retry execution is currently blocked.
    /// </summary>
    public bool IsBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.Blocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether automatic background retry execution is currently eligible to run one shared retry attempt.
    /// </summary>
    public bool IsEligible => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.Eligible, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether automatic background retry execution already recorded one matching shared retry attempt.
    /// </summary>
    public bool IsCompleted => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.Completed, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether Cephalon can reuse the full matching safety context for automatic background retry execution.
    /// </summary>
    public bool CanReuseMatchingSafetyContext =>
        CanReuseApprovalFromMatchingHistory &&
        CanReuseDestructiveAllowanceFromMatchingHistory;
}
