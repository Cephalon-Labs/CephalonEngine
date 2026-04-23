namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector automatic background retry execution answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionCategories
{
    /// <summary>
    /// The runtime remains observe-only, so automatic background retry execution does not apply.
    /// </summary>
    public const string ObserveOnlyMode = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ObserveOnlyMode;

    /// <summary>
    /// Automatic background retry execution is disabled by the current host-owned data runtime options.
    /// </summary>
    public const string FeatureDisabled = "feature-disabled";

    /// <summary>
    /// Automatic background retry execution is disabled by the current shared retry-execution policy.
    /// </summary>
    public const string PolicyDisabled = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.AutomaticRetryDisabled;

    /// <summary>
    /// The runtime currently exposes one retry-ready candidate for automatic background retry execution.
    /// </summary>
    public const string RetryReady = "retry-ready";

    /// <summary>
    /// The runtime currently remains inside an active retry cooldown window.
    /// </summary>
    public const string CooldownActive = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.CooldownActive;

    /// <summary>
    /// The runtime still requires manual approval before automatic background retry execution should continue.
    /// </summary>
    public const string ManualApprovalRequired = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ManualApprovalRequired;

    /// <summary>
    /// Shared runtime truth currently blocks automatic background retry execution.
    /// </summary>
    public const string PolicyBlocked = "policy-blocked";

    /// <summary>
    /// Control-plane ownership still remains outside Cephalon.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.OperatorOnly;

    /// <summary>
    /// No additional provider execution is currently needed.
    /// </summary>
    public const string NoExecutionNeeded = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.NoExecutionNeeded;

    /// <summary>
    /// The bounded command journal already contains one automatic retry attempt.
    /// </summary>
    public const string AutomaticAttemptRecorded = "automatic-attempt-recorded";

    /// <summary>
    /// The bounded command journal already contains one automatic retry attempt that matches the current retry fingerprint.
    /// </summary>
    public const string MatchingAutomaticAttempt = "matching-automatic-attempt";

    /// <summary>
    /// The latest matching automatic retry attempt translated a provider command shape.
    /// </summary>
    public const string LatestExecutionAdapted = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.LatestExecutionAdapted;

    /// <summary>
    /// The latest matching automatic retry attempt determined that no provider command is required.
    /// </summary>
    public const string LatestExecutionNoOp = "latest-execution-no-op";

    /// <summary>
    /// The latest matching automatic retry attempt remained blocked.
    /// </summary>
    public const string LatestExecutionBlocked = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.LatestExecutionBlocked;

    /// <summary>
    /// The latest matching automatic retry attempt could not resolve a provider execution adapter.
    /// </summary>
    public const string LatestExecutionUnavailable = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.LatestExecutionUnavailable;

    /// <summary>
    /// The latest matching automatic retry attempt failed while Cephalon was translating the provider command.
    /// </summary>
    public const string LatestExecutionFailed = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories.LatestExecutionFailed;
}
