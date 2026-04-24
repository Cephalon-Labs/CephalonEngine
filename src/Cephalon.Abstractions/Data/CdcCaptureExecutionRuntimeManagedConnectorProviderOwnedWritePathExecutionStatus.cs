namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector provider-owned write-path execution posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus
{
    /// <summary>
    /// Creates a new managed-connector provider-owned write-path execution answer.
    /// </summary>
    /// <param name="state">
    /// The stable provider-owned write-path execution state, such as <c>not-applicable</c>, <c>operator-only</c>, <c>provider-executable</c>, <c>provider-blocked</c>, <c>provider-owned-executing</c>, <c>provider-owned-completed</c>, or <c>provider-owned-risk</c>.
    /// </param>
    /// <param name="description">An optional operator-facing provider-owned write-path execution summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector provider-owned write-path execution state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector provider-owned write-path execution state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing provider-owned write-path execution summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable provider-owned write-path execution categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with provider-owned write-path execution.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with provider-owned write-path execution.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the operator-facing execution-ownership mode that informed provider-owned write-path execution.
    /// </summary>
    public string ExecutionOwnership { get; init; } = "runtime-managed";

    /// <summary>
    /// Gets the operator-facing execution-topology classification that informed provider-owned write-path execution.
    /// </summary>
    public string ExecutionTopology { get; init; } = "not-configured";

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the stable management-operation identifier currently associated with provider-owned write-path execution.
    /// </summary>
    public string OperationId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive provider-owned write-path execution.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionSources.Unknown;

    /// <summary>
    /// Gets the current managed-connector execution-adapter state that informed provider-owned write-path execution.
    /// </summary>
    public string ExecutionAdapterState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.NotApplicable;

    /// <summary>
    /// Gets the latest recorded managed-connector command-execution state visible to provider-owned write-path execution.
    /// </summary>
    public string LatestCommandExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded;

    /// <summary>
    /// Gets the invocation-source identifier of the latest recorded command-execution outcome.
    /// </summary>
    public string LatestCommandExecutionInvocationSourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.None;

    /// <summary>
    /// Gets the current managed-connector retry-execution policy state that informed provider-owned write-path execution.
    /// </summary>
    public string RetryExecutionPolicyState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector automatic background retry execution state that informed provider-owned write-path execution.
    /// </summary>
    public string AutomaticRetryExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector distributed retry lease state that informed provider-owned write-path execution.
    /// </summary>
    public string DistributedRetryLeaseState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDistributedRetryLeaseStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector durable shared scheduler-orchestration state that informed provider-owned write-path execution.
    /// </summary>
    public string DurableSharedSchedulerOrchestrationState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDurableSharedSchedulerOrchestrationStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector scheduler recovery and execution-hardening state that informed provider-owned write-path execution.
    /// </summary>
    public string SchedulerRecoveryExecutionHardeningState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorSchedulerRecoveryExecutionHardeningStates.NotApplicable;

    /// <summary>
    /// Gets the stable provider execution-adapter identifier currently associated with provider-owned write-path execution.
    /// </summary>
    public string AdapterId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.None;

    /// <summary>
    /// Gets the best available provider identifier currently associated with provider-owned write-path execution.
    /// </summary>
    public string? ProviderId { get; init; }

    /// <summary>
    /// Gets the best available connector-cluster identifier currently associated with provider-owned write-path execution.
    /// </summary>
    public string? ConnectClusterId { get; init; }

    /// <summary>
    /// Gets the best available connector-class identifier currently associated with provider-owned write-path execution.
    /// </summary>
    public string? ConnectorClass { get; init; }

    /// <summary>
    /// Gets the best available source-provider identifier currently associated with provider-owned write-path execution.
    /// </summary>
    public string? SourceProviderId { get; init; }

    /// <summary>
    /// Gets the deterministic command fingerprint currently associated with provider-owned write-path execution.
    /// </summary>
    public string CommandFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic execution-adapter fingerprint currently associated with provider-owned write-path execution.
    /// </summary>
    public string AdapterFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic latest recorded execution fingerprint currently visible to provider-owned write-path execution.
    /// </summary>
    public string LatestExecutionFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic retry fingerprint currently associated with provider-owned write-path execution.
    /// </summary>
    public string RetryFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of visible potential shared write-path changes currently associated with provider-owned write-path execution.
    /// </summary>
    public int PotentialChangeCount { get; init; }

    /// <summary>
    /// Gets the stable latest recorded command-execution attempt identifier when one exists.
    /// </summary>
    public string LatestAttemptId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when Cephalon recorded the latest command-execution outcome that informed provider-owned write-path execution.
    /// </summary>
    public DateTimeOffset? LatestRecordedAtUtc { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current provider-owned write-path would still apply one or more shared write-path changes.
    /// </summary>
    public bool WouldApplyChanges { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current provider-owned write-path still requires explicit approval.
    /// </summary>
    public bool RequiresExplicitApproval { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current provider-owned write-path targets a destructive connector operation.
    /// </summary>
    public bool IsDestructiveOperation { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current node can execute provider-owned write-path work safely.
    /// </summary>
    public bool CanExecuteProviderOwnedWritePathOnCurrentNode { get; init; }

    /// <summary>
    /// Gets the number of active provider-owned write-path execution categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-owned write-path execution still remains operator-owned outside Cephalon.
    /// </summary>
    public bool IsOperatorOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates.OperatorOnly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-owned write-path execution is currently ready.
    /// </summary>
    public bool IsProviderExecutable => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates.ProviderExecutable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-owned write-path execution currently remains blocked.
    /// </summary>
    public bool IsProviderBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates.ProviderBlocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-owned write-path execution already translated a provider-facing command shape.
    /// </summary>
    public bool IsProviderOwnedExecuting => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates.ProviderOwnedExecuting, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-owned write-path execution no longer needs another provider command.
    /// </summary>
    public bool IsProviderOwnedCompleted => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates.ProviderOwnedCompleted, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether provider-owned write-path execution currently remains risky.
    /// </summary>
    public bool IsProviderOwnedRisk => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorProviderOwnedWritePathExecutionStates.ProviderOwnedRisk, StringComparison.OrdinalIgnoreCase);
}
