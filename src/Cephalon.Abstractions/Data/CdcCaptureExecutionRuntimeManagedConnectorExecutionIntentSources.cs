namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable confidence-source identifiers used by managed-connector execution-intent answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentSources
{
    /// <summary>
    /// The execution intent does not currently have a more specific confidence source.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// The execution intent is primarily grounded in the shared managed-connector action plan.
    /// </summary>
    public const string ActionPlan = "action-plan";

    /// <summary>
    /// The execution intent is primarily grounded in the shared managed-connector write-path readiness answer.
    /// </summary>
    public const string WritePathReadiness = "write-path-readiness";

    /// <summary>
    /// The execution intent is primarily grounded in the shared managed-connector preflight answer.
    /// </summary>
    public const string Preflight = "preflight";

    /// <summary>
    /// The execution intent is primarily grounded in the shared managed-connector dry-run answer.
    /// </summary>
    public const string DryRun = "dry-run";
}
