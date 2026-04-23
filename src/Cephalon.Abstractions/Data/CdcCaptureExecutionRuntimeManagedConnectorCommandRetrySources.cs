namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector command-retry answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandRetrySources
{
    /// <summary>
    /// The retry posture was derived from the shared execution-adapter lane.
    /// </summary>
    public const string ExecutionAdapter = "execution-adapter";

    /// <summary>
    /// The retry posture was derived from the latest shared command-execution outcome.
    /// </summary>
    public const string CommandExecution = "command-execution";

    /// <summary>
    /// The retry posture was derived from bounded shared command-execution history.
    /// </summary>
    public const string CommandExecutionHistory = "command-execution-history";

    /// <summary>
    /// The retry posture does not currently resolve to one specific source.
    /// </summary>
    public const string Unknown = "unknown";
}
