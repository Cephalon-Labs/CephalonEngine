namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector execution-adapter answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources
{
    /// <summary>
    /// The execution adapter does not currently have a more specific primary source.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// The execution adapter is primarily grounded in shared command-issuance truth.
    /// </summary>
    public const string CommandIssuance = "command-issuance";

    /// <summary>
    /// The execution adapter is primarily grounded in shared command-envelope truth.
    /// </summary>
    public const string CommandEnvelope = "command-envelope";

    /// <summary>
    /// The execution adapter is primarily grounded in shared execution-approval truth.
    /// </summary>
    public const string ExecutionApproval = "execution-approval";

    /// <summary>
    /// The execution adapter is primarily grounded in shared execution-intent truth.
    /// </summary>
    public const string ExecutionIntent = "execution-intent";
}
