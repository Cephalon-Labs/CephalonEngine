namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector automatic background retry execution answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionSources
{
    /// <summary>
    /// The automatic background retry execution answer was derived primarily from the shared retry-execution policy lane.
    /// </summary>
    public const string RetryExecutionPolicy = "retry-execution-policy";

    /// <summary>
    /// The automatic background retry execution answer was derived primarily from the bounded command journal.
    /// </summary>
    public const string CommandJournal = "command-journal";

    /// <summary>
    /// The automatic background retry execution answer was derived primarily from recorded automatic retry attempt history.
    /// </summary>
    public const string AutomaticRetryHistory = "automatic-retry-history";

    /// <summary>
    /// The automatic background retry execution answer does not currently resolve to one specific source.
    /// </summary>
    public const string Unknown = "unknown";
}
