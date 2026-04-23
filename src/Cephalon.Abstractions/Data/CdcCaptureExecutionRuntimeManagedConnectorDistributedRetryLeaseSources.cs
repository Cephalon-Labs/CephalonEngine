namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector distributed retry lease answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseSources
{
    /// <summary>
    /// The distributed retry lease answer was derived primarily from automatic-retry coordination truth.
    /// </summary>
    public const string AutomaticRetryCoordination = "automatic-retry-coordination";

    /// <summary>
    /// The distributed retry lease answer was derived primarily from durable command-journal truth.
    /// </summary>
    public const string CommandJournalDurability = "command-journal-durability";

    /// <summary>
    /// The distributed retry lease answer was derived primarily from retained command-execution history.
    /// </summary>
    public const string CommandExecutionHistory = "command-execution-history";

    /// <summary>
    /// The distributed retry lease answer was derived primarily from retry-execution policy truth.
    /// </summary>
    public const string RetryExecutionPolicy = "retry-execution-policy";

    /// <summary>
    /// The distributed retry lease answer does not currently resolve to one specific source.
    /// </summary>
    public const string Unknown = "unknown";
}
