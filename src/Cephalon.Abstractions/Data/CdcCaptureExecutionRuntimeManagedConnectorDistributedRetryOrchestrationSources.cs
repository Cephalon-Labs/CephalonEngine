namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector distributed retry orchestration answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationSources
{
    /// <summary>
    /// The distributed retry orchestration answer was derived primarily from automatic background retry execution truth.
    /// </summary>
    public const string AutomaticRetryExecution = "automatic-retry-execution";

    /// <summary>
    /// The distributed retry orchestration answer was derived primarily from retry-execution policy truth.
    /// </summary>
    public const string RetryExecutionPolicy = "retry-execution-policy";

    /// <summary>
    /// The distributed retry orchestration answer was derived primarily from distributed retry lease truth.
    /// </summary>
    public const string DistributedRetryLease = "distributed-retry-lease";

    /// <summary>
    /// The distributed retry orchestration answer was derived primarily from durable command-journal truth.
    /// </summary>
    public const string CommandJournalDurability = "command-journal-durability";

    /// <summary>
    /// The distributed retry orchestration answer does not currently resolve to one specific source.
    /// </summary>
    public const string Unknown = "unknown";
}
