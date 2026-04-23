namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector execution-approval answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalSources
{
    /// <summary>
    /// The execution-approval answer does not currently have a more specific primary source.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// The execution-approval answer is primarily grounded in runtime remediation posture.
    /// </summary>
    public const string Remediation = "remediation";

    /// <summary>
    /// The execution-approval answer is primarily grounded in managed-connector governance posture.
    /// </summary>
    public const string Governance = "governance";

    /// <summary>
    /// The execution-approval answer is primarily grounded in shared managed-connector dry-run truth.
    /// </summary>
    public const string DryRun = "dry-run";

    /// <summary>
    /// The execution-approval answer is primarily grounded in shared managed-connector execution intent.
    /// </summary>
    public const string ExecutionIntent = "execution-intent";
}
