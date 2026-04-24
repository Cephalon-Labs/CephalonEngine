namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector scheduler recovery and execution-hardening answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningSources
{
    /// <summary>
    /// The scheduler recovery and execution-hardening answer was derived primarily from durable shared scheduler orchestration truth.
    /// </summary>
    public const string DurableSharedSchedulerOrchestration = "durable-shared-scheduler-orchestration";

    /// <summary>
    /// The scheduler recovery and execution-hardening answer was derived primarily from durable command-journal truth.
    /// </summary>
    public const string CommandJournalDurability = "command-journal-durability";

    /// <summary>
    /// The scheduler recovery and execution-hardening answer was derived primarily from broader multi-node lease-execution truth.
    /// </summary>
    public const string MultiNodeLeaseExecution = "multi-node-lease-execution";

    /// <summary>
    /// The scheduler recovery and execution-hardening answer was derived primarily from distributed retry orchestration truth.
    /// </summary>
    public const string DistributedRetryOrchestration = "distributed-retry-orchestration";

    /// <summary>
    /// The scheduler recovery and execution-hardening answer was derived primarily from the latest command-execution outcome.
    /// </summary>
    public const string CommandExecution = "command-execution";

    /// <summary>
    /// The scheduler recovery and execution-hardening answer does not currently resolve to one specific source.
    /// </summary>
    public const string Unknown = "unknown";
}
