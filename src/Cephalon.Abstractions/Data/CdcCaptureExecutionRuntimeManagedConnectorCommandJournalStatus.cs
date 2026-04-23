namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector bounded command-journal posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus
{
    /// <summary>
    /// Creates a new managed-connector command-journal answer.
    /// </summary>
    /// <param name="state">
    /// The stable command-journal state, such as <c>not-applicable</c>, <c>empty</c>, <c>bounded</c>, <c>truncated</c>, <c>cooldown-active</c>, <c>duplicate-evidence-present</c>, or <c>insufficient-for-automation</c>.
    /// </param>
    /// <param name="description">An optional operator-facing command-journal summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector command-journal state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector command-journal state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing command-journal summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable command-journal categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable management-operation identifier currently associated with the command journal.
    /// </summary>
    public string OperationId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalOperationIds.None;

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the current runtime-level reporting-coverage state that informed the command journal.
    /// </summary>
    public string ReportingCoverageState { get; init; } = CdcCaptureExecutionRuntimeReportingCoverageStates.Unknown;

    /// <summary>
    /// Gets the current runtime-level remediation state that informed the command journal.
    /// </summary>
    public string RemediationState { get; init; } = CdcCaptureExecutionRuntimeRemediationStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector governance state that informed the command journal.
    /// </summary>
    public string GovernanceState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector drift state that informed the command journal.
    /// </summary>
    public string DriftState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector action-plan state that informed the command journal.
    /// </summary>
    public string ActionPlanState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector write-path readiness state that informed the command journal.
    /// </summary>
    public string WritePathReadinessState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector preflight state that informed the command journal.
    /// </summary>
    public string PreflightState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector dry-run state that informed the command journal.
    /// </summary>
    public string DryRunState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector execution-intent state that informed the command journal.
    /// </summary>
    public string ExecutionIntentState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector execution-approval state that informed the command journal.
    /// </summary>
    public string ExecutionApprovalState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-envelope state that informed the command journal.
    /// </summary>
    public string CommandEnvelopeState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-issuance state that informed the command journal.
    /// </summary>
    public string CommandIssuanceState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector execution-adapter state that informed the command journal.
    /// </summary>
    public string ExecutionAdapterState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.NotApplicable;

    /// <summary>
    /// Gets the latest recorded managed-connector command-execution state visible to the command journal.
    /// </summary>
    public string LatestCommandExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded;

    /// <summary>
    /// Gets the current managed-connector command-retry state that informed the command journal.
    /// </summary>
    public string CommandRetryState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector retry-execution policy state that informed the command journal.
    /// </summary>
    public string RetryExecutionPolicyState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable;

    /// <summary>
    /// Gets the primary action identifier currently associated with the runtime's managed-connector action plan.
    /// </summary>
    public string PrimaryActionId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.None;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive the command journal.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalSources.Unknown;

    /// <summary>
    /// Gets the primary source identifier already associated with the command-retry lane.
    /// </summary>
    public string CommandRetrySourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandRetrySources.Unknown;

    /// <summary>
    /// Gets the primary source identifier already associated with the retry-execution policy lane.
    /// </summary>
    public string RetryExecutionPolicySourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicySources.Unknown;

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with the command journal.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with the command journal.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the best available connector-cluster identifier currently associated with the command journal.
    /// </summary>
    public string? ConnectClusterId { get; init; }

    /// <summary>
    /// Gets the best available connector-class identifier currently associated with the command journal.
    /// </summary>
    public string? ConnectorClass { get; init; }

    /// <summary>
    /// Gets the best available source-provider identifier currently associated with the command journal.
    /// </summary>
    public string? SourceProviderId { get; init; }

    /// <summary>
    /// Gets the total number of command-execution outcomes Cephalon has recorded for the execution runtime.
    /// </summary>
    public int TotalRecordedEntryCount { get; init; }

    /// <summary>
    /// Gets the number of bounded journal entries Cephalon currently retains for the execution runtime.
    /// </summary>
    public int RetainedEntryCount { get; init; }

    /// <summary>
    /// Gets the maximum number of bounded journal entries Cephalon retains for one execution runtime.
    /// </summary>
    public int MaximumRetainedEntryCount { get; init; }

    /// <summary>
    /// Gets the number of visible potential shared write-path changes currently associated with the command journal.
    /// </summary>
    public int PotentialChangeCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current command journal still reflects one or more shared write-path changes.
    /// </summary>
    public bool WouldApplyChanges { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current command journal still requires an explicit approval gate.
    /// </summary>
    public bool RequiresExplicitApproval { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current command journal targets a destructive connector operation.
    /// </summary>
    public bool IsDestructiveOperation { get; init; }

    /// <summary>
    /// Gets a value indicating whether automatic background retry execution is enabled for the current journal answer.
    /// </summary>
    public bool IsAutomaticRetryEnabled { get; init; }

    /// <summary>
    /// Gets the deterministic command fingerprint currently associated with the command journal.
    /// </summary>
    public string CommandFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic retry fingerprint currently associated with the command journal.
    /// </summary>
    public string RetryFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic latest retained execution fingerprint currently visible to the command journal.
    /// </summary>
    public string LatestExecutionFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the stable latest retained command-execution attempt identifier when one exists.
    /// </summary>
    public string LatestAttemptId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the stable oldest retained command-execution attempt identifier when one exists.
    /// </summary>
    public string OldestRetainedAttemptId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when Cephalon recorded the latest retained command-execution outcome that informed the command journal.
    /// </summary>
    public DateTimeOffset? LatestRecordedAtUtc { get; init; }

    /// <summary>
    /// Gets the timestamp when Cephalon recorded the oldest retained command-execution outcome currently visible in the bounded journal.
    /// </summary>
    public DateTimeOffset? OldestRetainedRecordedAtUtc { get; init; }

    /// <summary>
    /// Gets the timestamp when the active retry cooldown window ends, when one applies.
    /// </summary>
    public DateTimeOffset? CooldownUntilUtc { get; init; }

    /// <summary>
    /// Gets a value indicating whether the latest retained command currently matches the derived retry fingerprint.
    /// </summary>
    public bool HasMatchingRetryFingerprint { get; init; }

    /// <summary>
    /// Gets a value indicating whether the latest retained command currently matches the derived command fingerprint.
    /// </summary>
    public bool HasMatchingCommandFingerprint { get; init; }

    /// <summary>
    /// Gets the number of active command-journal categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the bounded command journal currently has no recorded entries.
    /// </summary>
    public bool IsEmpty => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Empty, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the bounded command journal currently retains enough evidence for operator-facing automation answers.
    /// </summary>
    public bool IsBounded => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Bounded, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the bounded command journal has truncated older entries.
    /// </summary>
    public bool IsTruncated => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.Truncated, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the bounded command journal currently exposes an active cooldown window.
    /// </summary>
    public bool IsCooldownActive => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.CooldownActive, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the bounded command journal contains evidence that replaying the command would be duplicative.
    /// </summary>
    public bool HasDuplicateEvidence => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.DuplicateEvidencePresent, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the bounded command journal currently remains insufficient for automation.
    /// </summary>
    public bool IsInsufficientForAutomation => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.InsufficientForAutomation, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether Cephalon has recorded one or more managed-connector command outcomes for the journal.
    /// </summary>
    public bool HasRecordedCommandHistory => TotalRecordedEntryCount > 0;

    /// <summary>
    /// Gets a value indicating whether the bounded command journal currently retains one or more entries.
    /// </summary>
    public bool HasRetainedCommandHistory => RetainedEntryCount > 0;

    /// <summary>
    /// Gets a value indicating whether the bounded command journal currently exposes an active cooldown window.
    /// </summary>
    public bool HasCooldownWindow => CooldownUntilUtc.HasValue;

    /// <summary>
    /// Gets a value indicating whether the bounded command journal has truncated at least one older entry.
    /// </summary>
    public bool HasTruncatedHistory => TotalRecordedEntryCount > RetainedEntryCount;
}
