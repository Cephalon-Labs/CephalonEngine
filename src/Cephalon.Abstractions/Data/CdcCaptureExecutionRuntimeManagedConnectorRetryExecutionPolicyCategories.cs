namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector retry-execution policy answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories
{
    /// <summary>
    /// The retry-execution policy is not currently applicable because the runtime remains observe-only.
    /// </summary>
    public const string ObserveOnlyMode = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.ObserveOnlyMode;

    /// <summary>
    /// The retry-execution policy remains blocked by remediation that still needs to clear first.
    /// </summary>
    public const string BlockingRemediation = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.BlockingRemediation;

    /// <summary>
    /// The retry-execution policy is constrained by stale observation posture.
    /// </summary>
    public const string StaleObservation = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.StaleObservation;

    /// <summary>
    /// The retry-execution policy is constrained by incomplete reporting coverage.
    /// </summary>
    public const string IncompleteReportingCoverage = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.IncompleteReportingCoverage;

    /// <summary>
    /// The retry-execution policy is constrained by incomplete runtime truth.
    /// </summary>
    public const string RuntimeTruthIncomplete = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.RuntimeTruthIncomplete;

    /// <summary>
    /// The retry-execution policy is constrained by governance that remains out of policy.
    /// </summary>
    public const string GovernanceOutOfPolicy = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.GovernanceOutOfPolicy;

    /// <summary>
    /// The retry-execution policy is constrained because control-plane ownership still remains outside Cephalon.
    /// </summary>
    public const string ControlPlaneOwnershipGap = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.ControlPlaneOwnershipGap;

    /// <summary>
    /// The retry-execution policy still reflects one or more shared write-path changes.
    /// </summary>
    public const string ChangePlanned = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.ChangePlanned;

    /// <summary>
    /// The retry-execution policy still reflects a lifecycle transition such as pause, resume, restart, or delete.
    /// </summary>
    public const string LifecycleChange = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.LifecycleChange;

    /// <summary>
    /// The retry-execution policy still reflects a destructive write-path such as connector deletion.
    /// </summary>
    public const string DestructiveOperation = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.DestructiveOperation;

    /// <summary>
    /// The retry-execution policy still requires explicit manual approval.
    /// </summary>
    public const string ManualApprovalRequired = "manual-approval-required";

    /// <summary>
    /// The retry-execution policy is approval-ready but still waiting for a manual approval gate to clear.
    /// </summary>
    public const string ManualApprovalReady = "manual-approval-ready";

    /// <summary>
    /// The current shared runtime truth indicates that no additional provider command is needed.
    /// </summary>
    public const string NoExecutionNeeded = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.NoExecutionNeeded;

    /// <summary>
    /// The current retry-execution policy is waiting for the active cooldown window to elapse.
    /// </summary>
    public const string CooldownActive = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.CooldownActive;

    /// <summary>
    /// Replaying the current retry-execution policy would duplicate a previously recorded command.
    /// </summary>
    public const string DuplicateCommand = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.DuplicateCommand;

    /// <summary>
    /// The latest matching command-execution outcome remained blocked.
    /// </summary>
    public const string ProviderExecutionBlocked = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.LatestExecutionBlocked;

    /// <summary>
    /// The latest matching command-execution outcome could not resolve a provider execution adapter.
    /// </summary>
    public const string ProviderExecutionUnavailable = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.LatestExecutionUnavailable;

    /// <summary>
    /// The latest matching command-execution outcome failed while Cephalon was translating the command.
    /// </summary>
    public const string ProviderExecutionFailed = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.LatestExecutionFailed;

    /// <summary>
    /// The retry-execution policy currently remains operator-owned.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.OperatorOnly;

    /// <summary>
    /// The current shared runtime truth exposes one safe retry candidate.
    /// </summary>
    public const string RetryCandidate = "retry-candidate";

    /// <summary>
    /// Automatic background retry remains disabled for the current retry candidate.
    /// </summary>
    public const string AutomaticRetryDisabled = "automatic-retry-disabled";
}
