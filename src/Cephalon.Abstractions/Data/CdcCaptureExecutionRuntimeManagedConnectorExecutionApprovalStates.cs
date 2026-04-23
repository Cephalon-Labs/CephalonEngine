namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable managed-connector execution-approval state identifiers used by CDC execution runtimes.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates
{
    /// <summary>
    /// The execution runtime does not currently require managed-connector execution approval on the shared surface.
    /// </summary>
    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// The managed connector is currently blocked by incomplete runtime truth or runtime remediation posture before approval can be considered.
    /// </summary>
    public const string AutoBlocked = "auto-blocked";

    /// <summary>
    /// The managed connector is currently blocked by governance or control-plane policy rather than runtime readiness alone.
    /// </summary>
    public const string PolicyBlocked = "policy-blocked";

    /// <summary>
    /// The managed connector would still require an elevated explicit approval gate because the intended follow-through remains safety-sensitive.
    /// </summary>
    public const string ApprovalRequired = "approval-required";

    /// <summary>
    /// The managed connector is currently ready to enter a future approval workflow before engine execution.
    /// </summary>
    public const string ApprovalReady = "approval-ready";

    /// <summary>
    /// The managed connector currently fits a future auto-execution lane without requiring an additional approval workflow.
    /// </summary>
    public const string AutoEligible = "auto-eligible";
}
