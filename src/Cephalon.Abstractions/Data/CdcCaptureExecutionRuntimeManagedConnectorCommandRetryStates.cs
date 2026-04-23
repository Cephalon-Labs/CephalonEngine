namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable state identifiers used by managed-connector command-retry answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates
{
    /// <summary>
    /// The command-retry posture does not currently apply to the execution runtime.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// The current shared runtime truth indicates that a retry is not currently needed.
    /// </summary>
    public const string NotNeeded = "not-needed";

    /// <summary>
    /// The current shared runtime truth matches an already-recorded command and replaying it would be duplicative.
    /// </summary>
    public const string Duplicate = "duplicate";

    /// <summary>
    /// The current shared runtime truth matches a recently-recorded command and should wait for a short cooldown window before retrying.
    /// </summary>
    public const string Cooldown = "cooldown";

    /// <summary>
    /// The current shared runtime truth still blocks a safe retry.
    /// </summary>
    public const string RetryBlocked = "retry-blocked";

    /// <summary>
    /// The current shared runtime truth allows a safe retry of a matching prior command.
    /// </summary>
    public const string RetryEligible = "retry-eligible";

    /// <summary>
    /// The current shared runtime truth still leaves the command operator-owned outside Cephalon.
    /// </summary>
    public const string OperatorOnly = "operator-only";
}
