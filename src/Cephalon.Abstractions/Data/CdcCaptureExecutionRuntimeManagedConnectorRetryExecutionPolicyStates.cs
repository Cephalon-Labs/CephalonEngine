namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector retry-execution policy answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates
{
    /// <summary>
    /// The retry-execution policy does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// The current shared runtime truth indicates that no further retry action is currently needed.
    /// </summary>
    public const string NotNeeded = "not-needed";

    /// <summary>
    /// The current retry-execution policy remains operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// The current retry-execution policy is waiting for a cooldown window to elapse.
    /// </summary>
    public const string Cooldown = "cooldown";

    /// <summary>
    /// The current retry-execution policy remains blocked by shared runtime truth or safety guardrails.
    /// </summary>
    public const string PolicyBlocked = "policy-blocked";

    /// <summary>
    /// The current retry-execution policy still needs a human approval gate to clear first.
    /// </summary>
    public const string ManualApproval = "manual-approval";

    /// <summary>
    /// The current retry-execution policy allows Cephalon to execute one safe retry automatically.
    /// </summary>
    public const string RetryReady = "retry-ready";

    /// <summary>
    /// The current shared runtime truth allows one safe retry, but background retry execution is not enabled yet.
    /// </summary>
    public const string BackgroundRetryDisabled = "background-retry-disabled";
}
