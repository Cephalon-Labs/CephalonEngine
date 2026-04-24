namespace Cephalon.Abstractions.Data;

/// <summary>
/// Defines the stable source identifiers used by managed-connector provider execution-orchestration answers.
/// </summary>
public static class CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationSources
{
    /// <summary>
    /// The provider execution-orchestration answer was derived primarily from provider-owned write-path execution truth.
    /// </summary>
    public const string ProviderOwnedWritePathExecution = "provider-owned-write-path-execution";

    /// <summary>
    /// The provider execution-orchestration answer was derived primarily from scheduler recovery and execution-hardening truth.
    /// </summary>
    public const string SchedulerRecoveryExecutionHardening = "scheduler-recovery-execution-hardening";

    /// <summary>
    /// The provider execution-orchestration answer was derived primarily from durable shared scheduler-orchestration truth.
    /// </summary>
    public const string DurableSharedSchedulerOrchestration = "durable-shared-scheduler-orchestration";

    /// <summary>
    /// The provider execution-orchestration answer was derived primarily from command-journal truth.
    /// </summary>
    public const string CommandJournal = "command-journal";

    /// <summary>
    /// The provider execution-orchestration answer was derived primarily from the latest command-execution outcome.
    /// </summary>
    public const string CommandExecution = "command-execution";

    /// <summary>
    /// The provider execution-orchestration answer was derived primarily from execution-adapter truth.
    /// </summary>
    public const string ExecutionAdapter = "execution-adapter";

    /// <summary>
    /// The provider execution-orchestration answer does not currently resolve to one specific source.
    /// </summary>
    public const string Unknown = "unknown";
}
