namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector automatic background retry execution answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates
{
    /// <summary>
    /// Automatic background retry execution does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// Automatic background retry execution is currently disabled for the execution runtime.
    /// </summary>
    public const string Disabled = "disabled";

    /// <summary>
    /// Automatic background retry execution is currently blocked by shared runtime truth or safety guardrails.
    /// </summary>
    public const string Blocked = "blocked";

    /// <summary>
    /// Automatic background retry execution is currently eligible to run one shared retry attempt.
    /// </summary>
    public const string Eligible = "eligible";

    /// <summary>
    /// Automatic background retry execution has already recorded one matching shared retry attempt.
    /// </summary>
    public const string Completed = "completed";
}
