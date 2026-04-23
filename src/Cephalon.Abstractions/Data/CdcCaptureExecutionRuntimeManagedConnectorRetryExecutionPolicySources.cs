namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector retry-execution policy answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicySources
{
    /// <summary>
    /// The retry-execution policy was derived primarily from the shared command-retry lane.
    /// </summary>
    public const string CommandRetry = "command-retry";

    /// <summary>
    /// The retry-execution policy was derived primarily from the shared execution-approval lane.
    /// </summary>
    public const string ExecutionApproval = "execution-approval";

    /// <summary>
    /// The retry-execution policy was derived primarily from the shared execution-adapter lane.
    /// </summary>
    public const string ExecutionAdapter = "execution-adapter";

    /// <summary>
    /// The retry-execution policy does not currently resolve to one specific source.
    /// </summary>
    public const string Unknown = "unknown";
}
