namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector provider execution-orchestration posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus
{
    /// <summary>
    /// Creates a new managed-connector provider execution-orchestration answer.
    /// </summary>
    /// <param name="state">
    /// The stable provider execution-orchestration state, such as <c>not-applicable</c>, <c>operator-only</c>, <c>orchestration-ready</c>, <c>orchestration-blocked</c>, <c>orchestration-executing</c>, <c>orchestration-completed</c>, or <c>orchestration-risk</c>.
    /// </param>
    /// <param name="description">An optional operator-facing provider execution-orchestration summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector provider execution-orchestration state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector provider execution-orchestration state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing provider execution-orchestration summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable provider execution-orchestration categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with provider execution orchestration.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with provider execution orchestration.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the operator-facing execution-ownership mode that informed provider execution orchestration.
    /// </summary>
    public string ExecutionOwnership { get; init; } = "runtime-managed";

    /// <summary>
    /// Gets the operator-facing execution-topology classification that informed provider execution orchestration.
    /// </summary>
    public string ExecutionTopology { get; init; } = "not-configured";

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the stable management-operation identifier currently associated with provider execution orchestration.
    /// </summary>
    public string OperationId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive provider execution orchestration.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationSources.Unknown;

    /// <summary>
    /// Gets the current provider-owned write-path execution state that informed provider execution orchestration.
    /// </summary>
    public string ProviderOwnedWritePathExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector execution-adapter state that informed provider execution orchestration.
    /// </summary>
    public string ExecutionAdapterState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.NotApplicable;

    /// <summary>
    /// Gets the latest recorded managed-connector command-execution state visible to provider execution orchestration.
    /// </summary>
    public string LatestCommandExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded;

    /// <summary>
    /// Gets the invocation-source identifier of the latest recorded command-execution outcome.
    /// </summary>
    public string LatestCommandExecutionInvocationSourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.None;

    /// <summary>
    /// Gets the current managed-connector retry-execution policy state that informed provider execution orchestration.
    /// </summary>
    public string RetryExecutionPolicyState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-journal state that informed provider execution orchestration.
    /// </summary>
    public string CommandJournalState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.NotApplicable;

    /// <summary>
    /// Gets the current durable shared scheduler-orchestration state that informed provider execution orchestration.
    /// </summary>
    public string DurableSharedSchedulerOrchestrationState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates.NotApplicable;

    /// <summary>
    /// Gets the current scheduler recovery and execution-hardening state that informed provider execution orchestration.
    /// </summary>
    public string SchedulerRecoveryExecutionHardeningState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates.NotApplicable;

    /// <summary>
    /// Gets the stable provider execution-adapter identifier currently associated with provider execution orchestration.
    /// </summary>
    public string AdapterId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.None;

    /// <summary>
    /// Gets the best available provider identifier currently associated with provider execution orchestration.
    /// </summary>
    public string? ProviderId { get; init; }

    /// <summary>
    /// Gets the best available connector-cluster identifier currently associated with provider execution orchestration.
    /// </summary>
    public string? ConnectClusterId { get; init; }

    /// <summary>
    /// Gets the best available connector-class identifier currently associated with provider execution orchestration.
    /// </summary>
    public string? ConnectorClass { get; init; }

    /// <summary>
    /// Gets the best available source-provider identifier currently associated with provider execution orchestration.
    /// </summary>
    public string? SourceProviderId { get; init; }

    /// <summary>
    /// Gets the deterministic command fingerprint currently associated with provider execution orchestration.
    /// </summary>
    public string CommandFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic execution-adapter fingerprint currently associated with provider execution orchestration.
    /// </summary>
    public string AdapterFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic latest recorded execution fingerprint currently visible to provider execution orchestration.
    /// </summary>
    public string LatestExecutionFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic retry fingerprint currently associated with provider execution orchestration.
    /// </summary>
    public string RetryFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of visible potential shared provider-execution changes currently associated with provider execution orchestration.
    /// </summary>
    public int PotentialChangeCount { get; init; }

    /// <summary>
    /// Gets the stable latest recorded command-execution attempt identifier when one exists.
    /// </summary>
    public string LatestAttemptId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when Cephalon recorded the latest command-execution outcome that informed provider execution orchestration.
    /// </summary>
    public DateTimeOffset? LatestRecordedAtUtc { get; init; }

    /// <summary>
    /// Gets the host-owned coordination owner identifier when one is known.
    /// </summary>
    public string? CoordinationOwnerId { get; init; }

    /// <summary>
    /// Gets the active reporter identifier currently visible for the execution runtime when one exists.
    /// </summary>
    public string? ActiveReporterId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the active reporter lease expires when one is known.
    /// </summary>
    public DateTimeOffset? ActiveReporterLeaseExpiresAtUtc { get; init; }

    /// <summary>
    /// Gets the stable shared scheduler identifier currently associated with provider execution orchestration.
    /// </summary>
    public string SchedulerId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus.DefaultSchedulerId;

    /// <summary>
    /// Gets the stable shared scheduler kind currently associated with provider execution orchestration.
    /// </summary>
    public string SchedulerKind { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryOrchestrationStatus.DefaultSchedulerKind;

    /// <summary>
    /// Gets the bounded retry scheduler polling interval, in seconds, when one is configured.
    /// </summary>
    public int PollingIntervalSeconds { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current provider execution orchestration would still apply one or more shared write-path changes.
    /// </summary>
    public bool WouldApplyChanges { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current provider execution orchestration still requires explicit approval.
    /// </summary>
    public bool RequiresExplicitApproval { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current provider execution orchestration targets a destructive connector operation.
    /// </summary>
    public bool IsDestructiveOperation { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current command journal already exposes provider-execution evidence.
    /// </summary>
    public bool HasCommandJournalEvidence { get; init; }

    /// <summary>
    /// Gets a value indicating whether a durable command-journal store is currently configured.
    /// </summary>
    public bool HasDurableStoreConfigured { get; init; }

    /// <summary>
    /// Gets a value indicating whether the durable command-journal store currently exposes persisted recorded history.
    /// </summary>
    public bool HasPersistedRecordedHistory { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current process recovered persisted command history for this runtime.
    /// </summary>
    public bool HasRecoveredPersistedHistory { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current node can orchestrate provider execution safely.
    /// </summary>
    public bool CanOrchestrateProviderExecutionOnCurrentNode { get; init; }

    /// <summary>
    /// Gets the number of active provider execution-orchestration categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets a value indicating whether the runtime currently exposes one active reporter lease.
    /// </summary>
    public bool HasActiveReporterLease =>
        !string.IsNullOrWhiteSpace(ActiveReporterId) &&
        ActiveReporterLeaseExpiresAtUtc.HasValue;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider execution orchestration still remains operator-owned outside Cephalon.
    /// </summary>
    public bool IsOperatorOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates.OperatorOnly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider execution orchestration is currently ready.
    /// </summary>
    public bool IsOrchestrationReady => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates.OrchestrationReady, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider execution orchestration currently remains blocked.
    /// </summary>
    public bool IsOrchestrationBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates.OrchestrationBlocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider execution orchestration is currently executing one provider-facing orchestration step.
    /// </summary>
    public bool IsOrchestrationExecuting => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates.OrchestrationExecuting, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider execution orchestration no longer needs another provider-facing orchestration step.
    /// </summary>
    public bool IsOrchestrationCompleted => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates.OrchestrationCompleted, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider execution orchestration currently remains risky.
    /// </summary>
    public bool IsOrchestrationRisk => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderExecutionOrchestrationStates.OrchestrationRisk, StringComparison.OrdinalIgnoreCase);
}
