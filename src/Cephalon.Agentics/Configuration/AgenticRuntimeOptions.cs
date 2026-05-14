using Cephalon.Agentics.Services;

namespace Cephalon.Agentics.Configuration;

/// <summary>
/// Configures the built-in agentic runtime pack.
/// </summary>
/// <remarks>
/// These options seed the host-owned part of the agentic runtime. Installed modules can still
/// contribute additional tools through <see cref="Services.IAgentToolContributor" />.
/// </remarks>
public sealed class AgenticRuntimeOptions
{
    /// <summary>
    /// Creates agentic runtime options with the default host-owned features enabled.
    /// </summary>
    public AgenticRuntimeOptions()
    {
    }

    /// <summary>
    /// Gets the host-defined tool descriptors that should be available to the agentic runtime.
    /// </summary>
    public IList<AgentToolDescriptor> Tools { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether agent memory features are enabled.
    /// </summary>
    public bool EnableMemory { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether Cephalon-managed tool dispatch and run-state features are enabled.
    /// </summary>
    public bool EnableExecution { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of process-local attempts for one managed tool execution.
    /// </summary>
    /// <remarks>
    /// The default value preserves single-attempt execution. Values greater than <c>1</c> enable
    /// bounded in-process retry for executor failures without claiming durable retry queues or
    /// distributed coordination.
    /// </remarks>
    public int ExecutionMaxAttempts { get; set; } = 1;

    /// <summary>
    /// Gets or sets the optional delay, in milliseconds, before a process-local retry attempt.
    /// </summary>
    public int ExecutionRetryDelayMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether duplicate completed run ids should be skipped inside the current process.
    /// </summary>
    /// <remarks>
    /// This is a bounded, process-local idempotency posture. It suppresses duplicate completed
    /// tool runs observed by the in-memory run catalog without claiming durable inbox storage,
    /// cross-node deduplication, or distributed exactly-once execution.
    /// </remarks>
    public bool EnableExecutionIdempotency { get; set; }

    /// <summary>
    /// Gets or sets the idempotency durability mode used when completed run suppression is enabled.
    /// </summary>
    /// <remarks>
    /// The default <c>process-local</c> mode preserves the original in-memory run-catalog behavior.
    /// Set this value to <c>inbox</c> to require exactly one active <c>IInbox</c> provider and use it
    /// as the durable duplicate-completed run marker. The inbox mode does not claim distributed
    /// exactly-once execution; it only reuses a provider-backed processed-message store for
    /// <c>toolId + runId</c> idempotency.
    /// </remarks>
    public string ExecutionIdempotencyDurability { get; set; } = "process-local";

    /// <summary>
    /// Gets or sets the process-local retention window, in minutes, for completed run-id suppression.
    /// </summary>
    /// <remarks>
    /// Values less than <c>1</c> are normalized to one minute by the dispatcher.
    /// </remarks>
    public int ExecutionIdempotencyRetentionMinutes { get; set; } = 60;

    /// <summary>
    /// Gets arbitrary metadata that can be attached to the agentic runtime configuration.
    /// </summary>
    public IDictionary<string, string> Metadata { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
