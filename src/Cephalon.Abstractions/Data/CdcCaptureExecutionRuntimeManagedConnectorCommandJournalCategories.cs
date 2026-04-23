namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable category identifiers used by managed-connector command-journal answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandJournalCategories
{
    /// <summary>
    /// The command journal is not currently applicable because the runtime remains observe-only.
    /// </summary>
    public const string ObserveOnlyMode = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ObserveOnlyMode;

    /// <summary>
    /// No managed-connector command-execution outcome has been recorded yet for the journal.
    /// </summary>
    public const string NoRecordedCommand = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.NoRecordedCommand;

    /// <summary>
    /// The command journal currently exposes bounded retention for recent command evidence.
    /// </summary>
    public const string BoundedRetention = "bounded-retention";

    /// <summary>
    /// The command journal has truncated older entries beyond the current bounded retention window.
    /// </summary>
    public const string HistoryTruncated = "history-truncated";

    /// <summary>
    /// The current command journal still reflects a cooldown window that has not elapsed.
    /// </summary>
    public const string CooldownActive = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.CooldownActive;

    /// <summary>
    /// The current command journal contains evidence that replaying the command would be duplicative.
    /// </summary>
    public const string DuplicateCommand = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.DuplicateCommand;

    /// <summary>
    /// The current command journal contains retained evidence matching the derived command fingerprint.
    /// </summary>
    public const string MatchingCommandFingerprint = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.MatchingCommandFingerprint;

    /// <summary>
    /// The current command journal remains constrained by incomplete shared runtime truth.
    /// </summary>
    public const string RuntimeTruthIncomplete = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.RuntimeTruthIncomplete;

    /// <summary>
    /// The current command journal remains constrained by governance that is out of policy.
    /// </summary>
    public const string GovernanceOutOfPolicy = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.GovernanceOutOfPolicy;

    /// <summary>
    /// The current command journal remains constrained because control-plane ownership still remains outside Cephalon.
    /// </summary>
    public const string ControlPlaneOwnershipGap = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ControlPlaneOwnershipGap;

    /// <summary>
    /// The current command journal remains operator-owned.
    /// </summary>
    public const string OperatorOnly = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.OperatorOnly;

    /// <summary>
    /// The current command journal remains blocked by shared runtime truth or policy guardrails.
    /// </summary>
    public const string PolicyBlocked = "policy-blocked";

    /// <summary>
    /// The current command journal still requires explicit manual approval before automation should continue.
    /// </summary>
    public const string ManualApprovalRequired = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ManualApprovalRequired;

    /// <summary>
    /// The current command journal exposes a safe retry candidate, but automatic background retry remains disabled.
    /// </summary>
    public const string AutomaticRetryDisabled = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.AutomaticRetryDisabled;

    /// <summary>
    /// The current command journal indicates that no additional provider command is needed.
    /// </summary>
    public const string NoExecutionNeeded = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.NoExecutionNeeded;

    /// <summary>
    /// The latest retained command-execution outcome remained blocked.
    /// </summary>
    public const string LatestExecutionBlocked = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ProviderExecutionBlocked;

    /// <summary>
    /// The latest retained command-execution outcome could not resolve a provider execution adapter.
    /// </summary>
    public const string LatestExecutionUnavailable = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ProviderExecutionUnavailable;

    /// <summary>
    /// The latest retained command-execution outcome failed while Cephalon was translating the provider command.
    /// </summary>
    public const string LatestExecutionFailed = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyCategories.ProviderExecutionFailed;

    /// <summary>
    /// The latest retained command-execution outcome already translated the provider command shape.
    /// </summary>
    public const string LatestExecutionAdapted = CdcCaptureExecutionRuntimeManagedConnectorCommandRetryCategories.LatestExecutionAdapted;
}
