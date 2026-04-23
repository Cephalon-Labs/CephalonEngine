namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector command-retry and idempotency posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus
{
    /// <summary>
    /// Creates a new managed-connector command-retry answer.
    /// </summary>
    /// <param name="state">
    /// The stable command-retry state, such as <c>not-needed</c>, <c>duplicate</c>, <c>cooldown</c>, <c>retry-blocked</c>, <c>retry-eligible</c>, <c>operator-only</c>, or <c>not-applicable</c>.
    /// </param>
    /// <param name="description">An optional operator-facing command-retry summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector command-retry state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector command-retry state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing command-retry summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable command-retry categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable management-operation identifier currently associated with command retry.
    /// </summary>
    public string OperationId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryOperationIds.None;

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the current runtime-level reporting-coverage state that informed command retry.
    /// </summary>
    public string ReportingCoverageState { get; init; } = CdcCaptureExecutionRuntimeReportingCoverageStates.Unknown;

    /// <summary>
    /// Gets the current runtime-level remediation state that informed command retry.
    /// </summary>
    public string RemediationState { get; init; } = CdcCaptureExecutionRuntimeRemediationStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector governance state that informed command retry.
    /// </summary>
    public string GovernanceState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector drift state that informed command retry.
    /// </summary>
    public string DriftState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector action-plan state that informed command retry.
    /// </summary>
    public string ActionPlanState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector write-path readiness state that informed command retry.
    /// </summary>
    public string WritePathReadinessState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector preflight state that informed command retry.
    /// </summary>
    public string PreflightState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector dry-run state that informed command retry.
    /// </summary>
    public string DryRunState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector execution-intent state that informed command retry.
    /// </summary>
    public string ExecutionIntentState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector execution-approval state that informed command retry.
    /// </summary>
    public string ExecutionApprovalState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-envelope state that informed command retry.
    /// </summary>
    public string CommandEnvelopeState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-issuance state that informed command retry.
    /// </summary>
    public string CommandIssuanceState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector execution-adapter state that informed command retry.
    /// </summary>
    public string ExecutionAdapterState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.NotApplicable;

    /// <summary>
    /// Gets the latest recorded managed-connector command-execution state Cephalon considered for the retry posture.
    /// </summary>
    public string LatestCommandExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded;

    /// <summary>
    /// Gets the primary action identifier currently associated with the runtime's managed-connector action plan.
    /// </summary>
    public string PrimaryActionId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.None;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive command retry.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandRetrySources.Unknown;

    /// <summary>
    /// Gets the primary source identifier already associated with the execution-adapter lane.
    /// </summary>
    public string ExecutionAdapterSourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources.Unknown;

    /// <summary>
    /// Gets the primary source identifier already associated with the latest command-execution lane.
    /// </summary>
    public string LatestCommandExecutionSourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources.Unknown;

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with command retry.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with command retry.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the best available connector-cluster identifier Cephalon would target for the current retry posture.
    /// </summary>
    public string? ConnectClusterId { get; init; }

    /// <summary>
    /// Gets the best available connector-class identifier Cephalon would target for the current retry posture.
    /// </summary>
    public string? ConnectorClass { get; init; }

    /// <summary>
    /// Gets the best available source-provider identifier Cephalon would target for the current retry posture.
    /// </summary>
    public string? SourceProviderId { get; init; }

    /// <summary>
    /// Gets the number of visible potential shared write-path changes currently associated with command retry.
    /// </summary>
    public int PotentialChangeCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current retry posture would still apply one or more shared write-path changes.
    /// </summary>
    public bool WouldApplyChanges { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current retry posture still requires an explicit approval gate.
    /// </summary>
    public bool RequiresExplicitApproval { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current retry posture targets a destructive connector operation.
    /// </summary>
    public bool IsDestructiveOperation { get; init; }

    /// <summary>
    /// Gets the deterministic command fingerprint currently associated with the retry posture.
    /// </summary>
    public string CommandFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic issuance fingerprint currently associated with the retry posture.
    /// </summary>
    public string IssuanceFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic execution-adapter fingerprint currently associated with the retry posture.
    /// </summary>
    public string AdapterFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic retry fingerprint Cephalon currently derives for duplicate and retry checks.
    /// </summary>
    public string RetryFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the latest recorded command-execution fingerprint Cephalon considered for the retry posture.
    /// </summary>
    public string LatestExecutionFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the stable latest recorded command-execution attempt identifier when one exists.
    /// </summary>
    public string LatestAttemptId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when Cephalon recorded the latest command-execution outcome that informed the retry posture.
    /// </summary>
    public DateTimeOffset? LatestRecordedAtUtc { get; init; }

    /// <summary>
    /// Gets the timestamp when the active retry cooldown window ends, when one applies.
    /// </summary>
    public DateTimeOffset? CooldownUntilUtc { get; init; }

    /// <summary>
    /// Gets a value indicating whether the latest recorded command currently matches the derived retry fingerprint.
    /// </summary>
    public bool HasMatchingRetryFingerprint { get; init; }

    /// <summary>
    /// Gets a value indicating whether the latest recorded command currently matches the derived command fingerprint.
    /// </summary>
    public bool HasMatchingCommandFingerprint { get; init; }

    /// <summary>
    /// Gets a value indicating whether the latest recorded command currently matches the derived issuance fingerprint.
    /// </summary>
    public bool HasMatchingIssuanceFingerprint { get; init; }

    /// <summary>
    /// Gets a value indicating whether the latest recorded command currently matches the derived execution-adapter fingerprint.
    /// </summary>
    public bool HasMatchingAdapterFingerprint { get; init; }

    /// <summary>
    /// Gets the number of active command-retry categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether retry is not currently needed.
    /// </summary>
    public bool IsNotNeeded => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.NotNeeded, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether replaying the current command would be duplicative.
    /// </summary>
    public bool IsDuplicate => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.Duplicate, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the current retry posture is waiting for a cooldown window to elapse.
    /// </summary>
    public bool IsCooldown => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.Cooldown, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the current retry posture remains blocked.
    /// </summary>
    public bool IsRetryBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.RetryBlocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the current retry posture remains operator-owned.
    /// </summary>
    public bool IsOperatorOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.OperatorOnly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the current retry posture allows one safe retry.
    /// </summary>
    public bool IsRetryEligible => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.RetryEligible, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether Cephalon has recorded one concrete command-execution outcome for the retry posture.
    /// </summary>
    public bool HasRecordedCommandHistory => LatestRecordedAtUtc.HasValue;

    /// <summary>
    /// Gets a value indicating whether the retry posture currently exposes an active cooldown window.
    /// </summary>
    public bool HasCooldownWindow => CooldownUntilUtc.HasValue;

    /// <summary>
    /// Gets a value indicating whether the current retry posture can safely retry through the shared command lane.
    /// </summary>
    public bool CanRetry => IsRetryEligible;
}
