namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector command-journal answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorCommandJournalSources
{
    /// <summary>
    /// The command journal was derived primarily from bounded shared command-execution history.
    /// </summary>
    public const string CommandExecutionHistory = "command-execution-history";

    /// <summary>
    /// The command journal was derived primarily from the shared command-retry lane.
    /// </summary>
    public const string CommandRetry = "command-retry";

    /// <summary>
    /// The command journal was derived primarily from the shared retry-execution policy lane.
    /// </summary>
    public const string RetryExecutionPolicy = "retry-execution-policy";

    /// <summary>
    /// The command journal does not currently resolve to one specific source.
    /// </summary>
    public const string Unknown = "unknown";
}
