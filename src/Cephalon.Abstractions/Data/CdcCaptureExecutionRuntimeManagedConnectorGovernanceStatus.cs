namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector governance posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus
{
    /// <summary>
    /// Creates a new managed-connector governance answer.
    /// </summary>
    /// <param name="state">
    /// The stable governance state, such as <c>observe-only</c>, <c>future-control-plane</c>, or <c>out-of-policy</c>.
    /// </param>
    /// <param name="description">An optional operator-facing governance summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector governance state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector governance state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing governance summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable managed-connector governance categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the upstream connector-cluster identifier when one is known.
    /// </summary>
    public string? ConnectClusterId { get; init; }

    /// <summary>
    /// Gets the upstream connector-class identifier when one is known.
    /// </summary>
    public string? ConnectorClass { get; init; }

    /// <summary>
    /// Gets the upstream source-provider identifier when one is known.
    /// </summary>
    public string? SourceProviderId { get; init; }

    /// <summary>
    /// Gets the declared task count when the managed connector reports one.
    /// </summary>
    public int? ExpectedTaskCount { get; init; }

    /// <summary>
    /// Gets the latest reported task count when the managed connector reports one.
    /// </summary>
    public int? ReportedTaskCount { get; init; }

    /// <summary>
    /// Gets the declared managed-connector task identifiers when they are known.
    /// </summary>
    public IReadOnlyList<string> DeclaredTaskIds { get; init; } = [];

    /// <summary>
    /// Gets the latest reported managed-connector task identifiers when they are known.
    /// </summary>
    public IReadOnlyList<string> ReportedTaskIds { get; init; } = [];

    /// <summary>
    /// Gets the latest reported active managed-connector task identifiers when they are known.
    /// </summary>
    public IReadOnlyList<string> ActiveTaskIds { get; init; } = [];

    /// <summary>
    /// Gets the latest reported connector lifecycle state when one is known.
    /// </summary>
    public string? ConnectorLifecycleState { get; init; }

    /// <summary>
    /// Gets the latest reported task-reconciliation state when one is known.
    /// </summary>
    public string? TaskReconciliationState { get; init; }

    /// <summary>
    /// Gets the latest reported overall reconciliation state when one is known.
    /// </summary>
    public string? ReconciliationState { get; init; }

    /// <summary>
    /// Gets the latest reported reconciliation summary when one is known.
    /// </summary>
    public string? ReconciliationReason { get; init; }

    /// <summary>
    /// Gets the stable recommended action identifier for the current governance posture.
    /// </summary>
    public string RecommendedActionId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorGovernanceActionIds.None;

    /// <summary>
    /// Gets the number of active governance categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.NotApplicable, StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.Unknown, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the execution runtime is currently governed in observe-only mode.
    /// </summary>
    public bool IsObserveOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.ObserveOnly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently requires future control-plane support.
    /// </summary>
    public bool RequiresControlPlaneSupport => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.FutureControlPlane, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the execution runtime is currently out of policy.
    /// </summary>
    public bool IsOutOfPolicy => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.OutOfPolicy, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently requires operator governance attention.
    /// </summary>
    public bool RequiresAttention => RequiresControlPlaneSupport || IsOutOfPolicy;
}
