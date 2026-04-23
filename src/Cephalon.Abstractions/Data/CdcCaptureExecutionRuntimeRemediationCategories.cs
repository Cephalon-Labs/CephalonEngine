namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable remediation-category identifiers used by CDC execution-runtime summaries.
/// </summary>
public static class CdcCaptureExecutionRuntimeRemediationCategories
{
    /// <summary>
    /// Declared CDC captures have not reported runtime state yet.
    /// </summary>
    public const string UnreportedCdcCaptures = "unreported-cdc-captures";

    /// <summary>
    /// Reported CDC captures currently publish stale runtime observations.
    /// </summary>
    public const string StaleObservations = "stale-observations";

    /// <summary>
    /// One or more reported CDC captures currently publish failed runtime outcomes.
    /// </summary>
    public const string FailedCdcCaptures = "failed-cdc-captures";

    /// <summary>
    /// One or more reported CDC captures currently publish degraded reporter-coordination posture.
    /// </summary>
    public const string ReporterCoordinationIssues = "reporter-coordination-issues";
}
