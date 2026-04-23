namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable managed-connector execution-approval and safety-gating category identifiers used by CDC execution runtimes.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalCategories
{
    /// <summary>
    /// The managed connector currently remains observe-only, so execution approval is not applicable on the shared surface.
    /// </summary>
    public const string ObserveOnlyMode = "observe-only-mode";

    /// <summary>
    /// The managed connector is blocked by failed or otherwise blocking runtime remediation work.
    /// </summary>
    public const string BlockingRemediation = "blocking-remediation";

    /// <summary>
    /// The managed connector currently reports stale observations that make safety-gating less trustworthy.
    /// </summary>
    public const string StaleObservation = "stale-observation";

    /// <summary>
    /// The managed connector does not yet have full declared-versus-reported coverage on the shared runtime surface.
    /// </summary>
    public const string IncompleteReportingCoverage = "incomplete-reporting-coverage";

    /// <summary>
    /// The managed connector does not yet report enough runtime truth for Cephalon to trust execution approval.
    /// </summary>
    public const string RuntimeTruthIncomplete = "runtime-truth-incomplete";

    /// <summary>
    /// The managed connector is currently out of policy for shared execution follow-through.
    /// </summary>
    public const string GovernanceOutOfPolicy = "governance-out-of-policy";

    /// <summary>
    /// The managed connector currently declares a control plane that Cephalon does not yet own for execution.
    /// </summary>
    public const string ControlPlaneOwnershipGap = "control-plane-ownership-gap";

    /// <summary>
    /// The managed connector currently reports drift that increases execution risk.
    /// </summary>
    public const string DriftRisk = "drift-risk";

    /// <summary>
    /// The current intended follow-through would still require a lifecycle change such as pause, resume, restart, or delete.
    /// </summary>
    public const string LifecycleChange = "lifecycle-change";

    /// <summary>
    /// The current intended follow-through would still perform a destructive operation such as delete.
    /// </summary>
    public const string DestructiveOperation = "destructive-operation";

    /// <summary>
    /// The current intended follow-through still requires an explicit approval gate.
    /// </summary>
    public const string ApprovalRequired = "approval-required";

    /// <summary>
    /// The current intended follow-through is ready to enter a future approval workflow.
    /// </summary>
    public const string ApprovalReady = "approval-ready";

    /// <summary>
    /// The current intended follow-through is currently eligible for a future auto-execution lane.
    /// </summary>
    public const string AutoEligible = "auto-eligible";

    /// <summary>
    /// The current intended follow-through would not require additional managed-connector changes.
    /// </summary>
    public const string NoExecutionNeeded = "no-execution-needed";
}
