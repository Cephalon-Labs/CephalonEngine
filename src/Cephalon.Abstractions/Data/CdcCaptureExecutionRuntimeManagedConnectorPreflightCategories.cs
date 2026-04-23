namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable managed-connector preflight category identifiers used by CDC execution runtimes.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorPreflightCategories
{
    /// <summary>
    /// The managed connector is blocked by failed or otherwise blocking runtime remediation work.
    /// </summary>
    public const string BlockingRemediation = "blocking-remediation";

    /// <summary>
    /// The managed connector still needs runtime remediation attention before Cephalon can preflight management operations.
    /// </summary>
    public const string RuntimeRemediation = "runtime-remediation";

    /// <summary>
    /// The managed connector does not yet have full declared-versus-reported coverage on the shared runtime surface.
    /// </summary>
    public const string IncompleteReportingCoverage = "incomplete-reporting-coverage";

    /// <summary>
    /// The managed connector is currently out of policy for future management follow-through.
    /// </summary>
    public const string GovernanceOutOfPolicy = "governance-out-of-policy";

    /// <summary>
    /// The managed connector does not yet report enough runtime truth to preflight the intended management operation.
    /// </summary>
    public const string RuntimeTruthIncomplete = "runtime-truth-incomplete";

    /// <summary>
    /// The managed connector still reports desired-versus-observed drift.
    /// </summary>
    public const string DriftDetected = "drift-detected";

    /// <summary>
    /// The managed connector currently stays in observe-only mode.
    /// </summary>
    public const string ObserveOnlyMode = "observe-only-mode";

    /// <summary>
    /// The managed connector currently expresses reconcile-style management intent.
    /// </summary>
    public const string ReconcileIntent = "reconcile-intent";

    /// <summary>
    /// The managed connector currently satisfies the shared baseline Cephalon would use to preflight the intended management operation.
    /// </summary>
    public const string PreflightReady = "preflight-ready";
}
