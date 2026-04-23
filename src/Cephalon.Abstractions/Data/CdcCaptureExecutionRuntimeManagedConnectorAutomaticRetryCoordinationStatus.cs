namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector automatic background retry coordination posture for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStatus
{
    /// <summary>
    /// Creates a new managed-connector automatic background retry coordination answer.
    /// </summary>
    /// <param name="state">
    /// The stable automatic background retry coordination state, such as <c>not-applicable</c>, <c>single-node</c>, <c>uncoordinated</c>, <c>lease-held</c>, <c>lease-missing</c>, <c>conflicted</c>, or <c>operator-only</c>.
    /// </param>
    /// <param name="description">An optional operator-facing automatic background retry coordination summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector automatic background retry coordination state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector automatic background retry coordination state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing automatic background retry coordination summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable automatic background retry coordination categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with automatic background retry coordination.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the CDC capture identifiers currently associated with automatic background retry coordination.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; init; } = [];

    /// <summary>
    /// Gets the operator-facing execution-ownership mode that informed automatic background retry coordination.
    /// </summary>
    public string ExecutionOwnership { get; init; } = "runtime-managed";

    /// <summary>
    /// Gets the operator-facing execution-topology classification that informed automatic background retry coordination.
    /// </summary>
    public string ExecutionTopology { get; init; } = "not-configured";

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the host-owned coordination owner identifier when one was configured for automatic retry.
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
    /// Gets the current reporter-coordination state that informed automatic background retry coordination.
    /// </summary>
    public string ReporterCoordinationState { get; init; } = CdcCaptureReporterCoordinationStates.Unknown;

    /// <summary>
    /// Gets the current reporter-coordination degraded-reason identifier when one applies.
    /// </summary>
    public string ReporterCoordinationIssueReason { get; init; } = CdcCaptureReporterCoordinationIssueReasons.None;

    /// <summary>
    /// Gets the current reporter takeover state when one applies.
    /// </summary>
    public string ReporterTakeoverState { get; init; } = CdcCaptureReporterTakeoverStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector automatic background retry execution state that informed coordination.
    /// </summary>
    public string AutomaticRetryExecutionState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector retry-execution policy state that informed coordination.
    /// </summary>
    public string RetryExecutionPolicyState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable;

    /// <summary>
    /// Gets the current managed-connector command-journal state that informed coordination.
    /// </summary>
    public string CommandJournalState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.NotApplicable;

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive automatic background retry coordination.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationSources.Unknown;

    /// <summary>
    /// Gets the declared or observed edge-node identifiers currently visible for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> ObservedEdgeNodeIds { get; init; } = [];

    /// <summary>
    /// Gets the number of active automatic background retry coordination categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the current host declared a coordination owner identifier for automatic retry.
    /// </summary>
    public bool HasCoordinationOwner => !string.IsNullOrWhiteSpace(CoordinationOwnerId);

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently exposes one active reporter lease.
    /// </summary>
    public bool HasActiveReporterLease =>
        !string.IsNullOrWhiteSpace(ActiveReporterId) &&
        ActiveReporterLeaseExpiresAtUtc.HasValue;

    /// <summary>
    /// Gets a value indicating whether the current host coordination owner matches the active reporter identifier.
    /// </summary>
    public bool CoordinationOwnerMatchesActiveReporter =>
        HasCoordinationOwner &&
        !string.IsNullOrWhiteSpace(ActiveReporterId) &&
        string.Equals(CoordinationOwnerId, ActiveReporterId, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether automatic background retry can safely execute on the current node.
    /// </summary>
    public bool CanExecuteOnCurrentNode => IsSingleNode || IsLeaseHeld;

    /// <summary>
    /// Gets a value indicating whether automatic background retry can execute without reporter-lease coordination.
    /// </summary>
    public bool IsSingleNode => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.SingleNode, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether automatic background retry currently remains uncoordinated on the current node.
    /// </summary>
    public bool IsUncoordinated => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.Uncoordinated, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the current node currently holds the active reporter lease.
    /// </summary>
    public bool IsLeaseHeld => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.LeaseHeld, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether no active reporter lease is currently visible for automatic retry.
    /// </summary>
    public bool IsLeaseMissing => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.LeaseMissing, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether reporter coordination currently remains conflicted.
    /// </summary>
    public bool IsConflicted => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.Conflicted, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether automatic background retry still remains operator-owned outside Cephalon.
    /// </summary>
    public bool IsOperatorOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.OperatorOnly, StringComparison.OrdinalIgnoreCase);
}
