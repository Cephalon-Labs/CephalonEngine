namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector command-retry answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories
{
    /// <summary>
    /// The command-retry posture is not currently applicable because the runtime remains observe-only.
    /// </summary>
    public const string ObserveOnlyMode = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ObserveOnlyMode;

    /// <summary>
    /// The command-retry posture remains blocked by remediation that still needs to clear first.
    /// </summary>
    public const string BlockingRemediation = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.BlockingRemediation;

    /// <summary>
    /// The command-retry posture is constrained by stale observation posture.
    /// </summary>
    public const string StaleObservation = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.StaleObservation;

    /// <summary>
    /// The command-retry posture is constrained by incomplete reporting coverage.
    /// </summary>
    public const string IncompleteReportingCoverage = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.IncompleteReportingCoverage;

    /// <summary>
    /// The command-retry posture is constrained by incomplete runtime truth.
    /// </summary>
    public const string RuntimeTruthIncomplete = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.RuntimeTruthIncomplete;

    /// <summary>
    /// The command-retry posture is constrained by governance that is currently out of policy.
    /// </summary>
    public const string GovernanceOutOfPolicy = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.GovernanceOutOfPolicy;

    /// <summary>
    /// The command-retry posture is constrained because control-plane ownership still remains outside Cephalon.
    /// </summary>
    public const string ControlPlaneOwnershipGap = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ControlPlaneOwnershipGap;

    /// <summary>
    /// The command-retry posture still reflects one or more shared write-path changes.
    /// </summary>
    public const string ChangePlanned = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ChangePlanned;

    /// <summary>
    /// The command-retry posture still reflects a lifecycle transition such as pause, resume, restart, or delete.
    /// </summary>
    public const string LifecycleChange = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.LifecycleChange;

    /// <summary>
    /// The command-retry posture still reflects a destructive write-path such as connector deletion.
    /// </summary>
    public const string DestructiveOperation = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.DestructiveOperation;

    /// <summary>
    /// The command-retry posture still reflects a higher-risk approval requirement.
    /// </summary>
    public const string ApprovalRequired = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ApprovalRequired;

    /// <summary>
    /// The command-retry posture is currently approval-ready but still requires an approval gate to clear.
    /// </summary>
    public const string ApprovalReady = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.ApprovalReady;

    /// <summary>
    /// The command-retry posture currently remains operator-owned.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.OperatorOnly;

    /// <summary>
    /// The current shared runtime truth indicates that no additional provider command is needed.
    /// </summary>
    public const string NoExecutionNeeded = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterCategories.NoExecutionNeeded;

    /// <summary>
    /// No managed-connector command-execution outcome has been recorded yet for the current retry posture.
    /// </summary>
    public const string NoRecordedCommand = "no-recorded-command";

    /// <summary>
    /// The current retry posture does not currently match any recorded command outcome.
    /// </summary>
    public const string NoMatchingCommandHistory = "no-matching-command-history";

    /// <summary>
    /// The current retry posture matches a recorded command fingerprint.
    /// </summary>
    public const string MatchingCommandFingerprint = "matching-command-fingerprint";

    /// <summary>
    /// The current retry posture matches a recorded issuance fingerprint.
    /// </summary>
    public const string MatchingIssuanceFingerprint = "matching-issuance-fingerprint";

    /// <summary>
    /// The current retry posture matches a recorded execution-adapter fingerprint.
    /// </summary>
    public const string MatchingAdapterFingerprint = "matching-adapter-fingerprint";

    /// <summary>
    /// The current retry posture is waiting for the active cooldown window to elapse.
    /// </summary>
    public const string CooldownActive = "cooldown-active";

    /// <summary>
    /// Replaying the current retry posture would duplicate a previously recorded command.
    /// </summary>
    public const string DuplicateCommand = "duplicate-command";

    /// <summary>
    /// The latest matching command-execution outcome failed while Cephalon was translating the command.
    /// </summary>
    public const string LatestExecutionFailed = "latest-execution-failed";

    /// <summary>
    /// The latest matching command-execution outcome remained blocked.
    /// </summary>
    public const string LatestExecutionBlocked = "latest-execution-blocked";

    /// <summary>
    /// The latest matching command-execution outcome could not resolve a provider execution adapter.
    /// </summary>
    public const string LatestExecutionUnavailable = "latest-execution-unavailable";

    /// <summary>
    /// The latest matching command-execution outcome already translated the provider command shape.
    /// </summary>
    public const string LatestExecutionAdapted = "latest-execution-adapted";

    /// <summary>
    /// The current retry posture is eligible for one safe retry.
    /// </summary>
    public const string RetryEligible = "retry-eligible";
}
