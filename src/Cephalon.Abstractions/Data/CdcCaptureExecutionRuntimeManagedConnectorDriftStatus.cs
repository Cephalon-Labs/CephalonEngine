namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the current desired-versus-observed drift posture for one managed-connector CDC execution runtime.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorDriftStatus
{
    /// <summary>
    /// Creates a new managed-connector drift answer.
    /// </summary>
    /// <param name="state">
    /// The stable drift state, such as <c>in-sync</c>, <c>drifted</c>, <c>unknown</c>, or <c>not-applicable</c>.
    /// </param>
    /// <param name="description">An optional operator-facing drift summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorDriftStatus(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector drift state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector drift state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets an optional operator-facing drift summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable managed-connector drift categories currently active for the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CategoryIds { get; init; } = [];

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the declared connector-cluster identifier when one is known.
    /// </summary>
    public string? DeclaredConnectClusterId { get; init; }

    /// <summary>
    /// Gets the latest reported connector-cluster identifier when one is known.
    /// </summary>
    public string? ReportedConnectClusterId { get; init; }

    /// <summary>
    /// Gets the declared connector-class identifier when one is known.
    /// </summary>
    public string? DeclaredConnectorClass { get; init; }

    /// <summary>
    /// Gets the latest reported connector-class identifier when one is known.
    /// </summary>
    public string? ReportedConnectorClass { get; init; }

    /// <summary>
    /// Gets the declared source-provider identifier when one is known.
    /// </summary>
    public string? DeclaredSourceProviderId { get; init; }

    /// <summary>
    /// Gets the latest reported source-provider identifier when one is known.
    /// </summary>
    public string? ReportedSourceProviderId { get; init; }

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
    /// Gets the declared task ids that are missing from the latest reported task set.
    /// </summary>
    public IReadOnlyList<string> MissingDeclaredTaskIds { get; init; } = [];

    /// <summary>
    /// Gets the reported task ids that were not part of the declared task baseline.
    /// </summary>
    public IReadOnlyList<string> UnexpectedReportedTaskIds { get; init; } = [];

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
    /// Gets the stable recommended action identifier for the current drift posture.
    /// </summary>
    public string RecommendedActionId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorDriftActionIds.None;

    /// <summary>
    /// Gets the number of active drift categories currently visible for the execution runtime.
    /// </summary>
    public int CategoryCount => CategoryIds.Count;

    /// <summary>
    /// Gets a value indicating whether the execution runtime currently represents a managed connector.
    /// </summary>
    public bool AppliesToManagedConnector =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDriftStates.NotApplicable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the current drift posture can be evaluated from the available baseline and report data.
    /// </summary>
    public bool CanEvaluateDrift =>
        !string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown, StringComparison.OrdinalIgnoreCase) &&
        AppliesToManagedConnector;

    /// <summary>
    /// Gets a value indicating whether the managed connector currently reports no declared-versus-observed drift.
    /// </summary>
    public bool IsInSync => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDriftStates.InSync, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector currently reports declared-versus-observed drift.
    /// </summary>
    public bool IsDrifted => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Drifted, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the managed connector currently requires operator drift attention.
    /// </summary>
    public bool RequiresAttention => IsDrifted;
}
