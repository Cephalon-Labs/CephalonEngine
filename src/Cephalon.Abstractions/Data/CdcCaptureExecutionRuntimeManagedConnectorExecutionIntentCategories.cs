namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable managed-connector execution-intent category identifiers used by CDC execution runtimes.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentCategories
{
    /// <summary>
    /// The managed connector is blocked by failed or otherwise blocking runtime remediation work.
    /// </summary>
    public const string BlockingRemediation = "blocking-remediation";

    /// <summary>
    /// The managed connector still needs runtime remediation attention before Cephalon can trust execution intent.
    /// </summary>
    public const string RuntimeRemediation = "runtime-remediation";

    /// <summary>
    /// The managed connector does not yet have full declared-versus-reported coverage on the shared runtime surface.
    /// </summary>
    public const string IncompleteReportingCoverage = "incomplete-reporting-coverage";

    /// <summary>
    /// The managed connector is currently out of policy for execution follow-through.
    /// </summary>
    public const string GovernanceOutOfPolicy = "governance-out-of-policy";

    /// <summary>
    /// The managed connector does not yet report enough runtime truth for Cephalon to trust execution intent.
    /// </summary>
    public const string RuntimeTruthIncomplete = "runtime-truth-incomplete";

    /// <summary>
    /// The managed connector currently stays in observe-only mode.
    /// </summary>
    public const string ObserveOnlyMode = "observe-only-mode";

    /// <summary>
    /// The managed connector currently declares a future control-plane mode that Cephalon does not yet own.
    /// </summary>
    public const string FutureControlPlane = "future-control-plane";

    /// <summary>
    /// The next intended follow-through currently remains operator-owned rather than engine-executable.
    /// </summary>
    public const string OperatorOnly = "operator-only";

    /// <summary>
    /// The next intended follow-through would still require an approval or safety gate before engine execution.
    /// </summary>
    public const string ApprovalRequired = "approval-required";

    /// <summary>
    /// The next intended follow-through currently falls inside the future engine-execution lane.
    /// </summary>
    public const string EngineExecutionCandidate = "engine-execution-candidate";

    /// <summary>
    /// The current intended operation would not require additional managed-connector changes.
    /// </summary>
    public const string NoExecutionNeeded = "no-execution-needed";

    /// <summary>
    /// The current intended operation would still change managed-connector posture.
    /// </summary>
    public const string ChangePlanned = "change-planned";

    /// <summary>
    /// The current intended operation would still require a lifecycle change such as pause, resume, restart, or delete.
    /// </summary>
    public const string LifecycleChange = "lifecycle-change";
}
