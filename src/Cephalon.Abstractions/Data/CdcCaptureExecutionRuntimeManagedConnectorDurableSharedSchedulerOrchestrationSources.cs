namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector durable shared scheduler-orchestration answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationSources
{
    /// <summary>
    /// The durable shared scheduler-orchestration answer was derived primarily from automatic-retry coordination truth.
    /// </summary>
    public const string AutomaticRetryCoordination = "automatic-retry-coordination";

    /// <summary>
    /// The durable shared scheduler-orchestration answer was derived primarily from durable command-journal truth.
    /// </summary>
    public const string CommandJournalDurability = "command-journal-durability";

    /// <summary>
    /// The durable shared scheduler-orchestration answer was derived primarily from distributed retry orchestration truth.
    /// </summary>
    public const string DistributedRetryOrchestration = "distributed-retry-orchestration";

    /// <summary>
    /// The durable shared scheduler-orchestration answer was derived primarily from broader multi-node lease-execution truth.
    /// </summary>
    public const string MultiNodeLeaseExecution = "multi-node-lease-execution";

    /// <summary>
    /// The durable shared scheduler-orchestration answer does not currently resolve to one specific source.
    /// </summary>
    public const string Unknown = "unknown";
}
