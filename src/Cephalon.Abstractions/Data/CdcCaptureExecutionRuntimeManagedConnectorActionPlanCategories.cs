namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable action-plan category identifiers used by managed-connector execution-runtime answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorActionPlanCategories
{
    /// <summary>
    /// The managed connector is currently blocked by active runtime remediation work.
    /// </summary>
    public const string BlockingRemediation = "blocking-remediation";

    /// <summary>
    /// The managed connector currently needs non-blocking runtime remediation work.
    /// </summary>
    public const string RuntimeRemediation = "runtime-remediation";

    /// <summary>
    /// The managed connector declaration is currently out of policy.
    /// </summary>
    public const string GovernanceOutOfPolicy = "governance-out-of-policy";

    /// <summary>
    /// The managed connector declares a future write-path management mode that Cephalon does not own yet.
    /// </summary>
    public const string FutureControlPlaneDeferred = "future-control-plane-deferred";

    /// <summary>
    /// The managed connector does not yet declare the task baseline required for drift evaluation.
    /// </summary>
    public const string DriftBaselineIncomplete = "drift-baseline-incomplete";

    /// <summary>
    /// The managed connector is waiting for additional runtime truth before drift can be evaluated.
    /// </summary>
    public const string WaitingForRuntimeTruth = "waiting-for-runtime-truth";

    /// <summary>
    /// The managed connector currently reports declared-versus-observed drift that needs investigation.
    /// </summary>
    public const string DriftDetected = "drift-detected";

    /// <summary>
    /// The managed connector can continue in steady-state observe-only posture.
    /// </summary>
    public const string ObserveOnlySteadyState = "observe-only-steady-state";
}
