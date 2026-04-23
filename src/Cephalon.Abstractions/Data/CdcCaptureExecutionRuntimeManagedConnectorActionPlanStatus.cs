namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current operator-facing managed-connector action plan for one CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus
{
    /// <summary>
    /// Creates a new managed-connector action-plan answer.
    /// </summary>
    /// <param name="state">
    /// The stable action-plan state, such as <c>observe</c>, <c>waiting</c>, <c>action-required</c>, <c>blocked</c>, or <c>not-applicable</c>.
    /// </param>
    /// <param name="description">An optional operator-facing action-plan summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector action-plan state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector action-plan state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing action-plan summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable action-plan categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the ordered action identifiers currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> ActionIds { get; init; } = [];

    /// <summary>
    /// Gets the current runtime-level remediation state that informed the action plan.
    /// </summary>
    public string RemediationState { get; init; } = CdcCaptureExecutionRuntimeRemediationStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector governance state that informed the action plan.
    /// </summary>
    public string GovernanceState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.Unknown;

    /// <summary>
    /// Gets the current managed-connector drift state that informed the action plan.
    /// </summary>
    public string DriftState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown;

    /// <summary>
    /// Gets the primary action identifier for the current action plan.
    /// </summary>
    public string PrimaryActionId => ActionIds.Count == 0
        ? CdcCaptureExecutionRuntimeManagedConnectorActionPlanActionIds.None
        : ActionIds[0];

    /// <summary>
    /// Gets the number of active action-plan categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets the number of active action identifiers currently visible for the execution runtime.
    /// </summary>
    public int ActionCount => ActionIds.Count;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector can currently remain in observe mode.
    /// </summary>
    public bool IsObserve => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.Observe, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector is currently waiting for more runtime truth.
    /// </summary>
    public bool IsWaiting => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.Waiting, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector currently requires operator action.
    /// </summary>
    public bool RequiresAction =>
        string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.ActionRequired, StringComparison.OrdinalIgnoreCase) ||
        IsBlocked;

    /// <summary>
    /// Gets a value indicating whether the managed connector is currently blocked by runtime remediation work.
    /// </summary>
    public bool IsBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.Blocked, StringComparison.OrdinalIgnoreCase);
}
