namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable invocation-source identifiers used by managed-connector command-execution results.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources
{
    /// <summary>
    /// No concrete invocation source has been recorded for the command-execution result.
    /// </summary>
    public const string None = "none";

    /// <summary>
    /// The command-execution result was recorded from an explicit operator-initiated request.
    /// </summary>
    public const string OperatorRequest = "operator-request";

    /// <summary>
    /// The command-execution result was recorded from the shared automatic background retry lane.
    /// </summary>
    public const string AutomaticRetry = "automatic-retry";
}
