namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector command-issuance answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceSources
{
    /// <summary>
    /// The command issuance does not currently have a more specific primary source.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// The command issuance is primarily grounded in shared command-envelope truth.
    /// </summary>
    public const string CommandEnvelope = "command-envelope";

    /// <summary>
    /// The command issuance is primarily grounded in shared execution-approval truth.
    /// </summary>
    public const string ExecutionApproval = "execution-approval";

    /// <summary>
    /// The command issuance is primarily grounded in shared execution-intent truth.
    /// </summary>
    public const string ExecutionIntent = "execution-intent";
}
