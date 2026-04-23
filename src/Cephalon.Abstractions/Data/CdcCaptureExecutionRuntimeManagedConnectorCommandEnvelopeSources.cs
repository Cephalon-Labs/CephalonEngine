namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector command-envelope answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeSources
{
    /// <summary>
    /// The command envelope does not currently have a more specific primary source.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// The command envelope is primarily grounded in shared execution-approval truth.
    /// </summary>
    public const string ExecutionApproval = "execution-approval";

    /// <summary>
    /// The command envelope is primarily grounded in shared execution-intent truth.
    /// </summary>
    public const string ExecutionIntent = "execution-intent";

    /// <summary>
    /// The command envelope is primarily grounded in shared dry-run truth.
    /// </summary>
    public const string DryRun = "dry-run";
}
