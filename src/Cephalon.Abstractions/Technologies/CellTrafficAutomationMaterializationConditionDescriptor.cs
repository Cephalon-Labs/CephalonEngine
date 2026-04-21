namespace Cephalon.Abstractions.Technologies;

/// <summary>
/// Describes one typed provider-managed or edge-managed materialization condition for a cell traffic automation answer.
/// </summary>
public sealed class CellTrafficAutomationMaterializationConditionDescriptor
{
    /// <summary>
    /// Creates a typed cell traffic automation materialization condition descriptor.
    /// </summary>
    /// <param name="dimension">
    /// The stable materialization dimension, such as <c>provider</c> or <c>edge</c>.
    /// </param>
    /// <param name="category">
    /// The stable condition category, such as <c>readiness</c>, <c>dependency</c>, or <c>ownership</c>.
    /// </param>
    /// <param name="conditionId">
    /// The stable provider-facing or edge-facing condition identifier inside the selected category, such as
    /// <c>gateway-accepted</c>, <c>ingress-route-present</c>, or <c>ownership</c>.
    /// </param>
    /// <param name="state">
    /// The stable condition state, such as <c>met</c>, <c>unmet</c>, <c>pending</c>, or <c>unknown</c>.
    /// </param>
    /// <param name="severity">
    /// The operator-facing severity for the current condition answer, such as <c>info</c>, <c>warning</c>, or
    /// <c>error</c>.
    /// </param>
    /// <param name="reason">The stable reason identifier that explains why the current condition state was selected.</param>
    /// <param name="description">The optional operator-facing condition summary.</param>
    /// <param name="metadata">Optional provider-facing or edge-facing condition metadata.</param>
    public CellTrafficAutomationMaterializationConditionDescriptor(
        string dimension,
        string category,
        string conditionId,
        string state,
        string severity,
        string? reason = null,
        string? description = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(dimension))
        {
            throw new ArgumentException("Condition dimension is required.", nameof(dimension));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Condition category is required.", nameof(category));
        }

        if (string.IsNullOrWhiteSpace(conditionId))
        {
            throw new ArgumentException("Condition id is required.", nameof(conditionId));
        }

        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Condition state is required.", nameof(state));
        }

        if (string.IsNullOrWhiteSpace(severity))
        {
            throw new ArgumentException("Condition severity is required.", nameof(severity));
        }

        Dimension = NormalizeDimension(dimension);
        Category = NormalizeCategory(category);
        ConditionId = conditionId.Trim().ToLowerInvariant();
        State = NormalizeState(state);
        Severity = NormalizeSeverity(severity);
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable materialization dimension that produced the condition.
    /// </summary>
    public string Dimension { get; }

    /// <summary>
    /// Gets the stable condition category.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Gets the stable provider-facing or edge-facing condition identifier.
    /// </summary>
    public string ConditionId { get; }

    /// <summary>
    /// Gets the stable condition state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets the operator-facing severity for the current condition answer.
    /// </summary>
    public string Severity { get; }

    /// <summary>
    /// Gets the stable reason identifier that explains why the current condition state was selected.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Gets the optional operator-facing condition summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets optional provider-facing or edge-facing condition metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string NormalizeDimension(string dimension)
    {
        var normalized = dimension.Trim().ToLowerInvariant();
        return normalized switch
        {
            CellTrafficAutomationMaterializationConditionDimensions.Provider => CellTrafficAutomationMaterializationConditionDimensions.Provider,
            CellTrafficAutomationMaterializationConditionDimensions.Edge => CellTrafficAutomationMaterializationConditionDimensions.Edge,
            _ => throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "Condition dimension is not supported.")
        };
    }

    private static string NormalizeCategory(string category)
    {
        var normalized = category.Trim().ToLowerInvariant();
        return normalized switch
        {
            CellTrafficAutomationMaterializationConditionCategories.Readiness => CellTrafficAutomationMaterializationConditionCategories.Readiness,
            CellTrafficAutomationMaterializationConditionCategories.Dependency => CellTrafficAutomationMaterializationConditionCategories.Dependency,
            CellTrafficAutomationMaterializationConditionCategories.Ownership => CellTrafficAutomationMaterializationConditionCategories.Ownership,
            CellTrafficAutomationMaterializationConditionCategories.Drift => CellTrafficAutomationMaterializationConditionCategories.Drift,
            CellTrafficAutomationMaterializationConditionCategories.Lifecycle => CellTrafficAutomationMaterializationConditionCategories.Lifecycle,
            CellTrafficAutomationMaterializationConditionCategories.Observation => CellTrafficAutomationMaterializationConditionCategories.Observation,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Condition category is not supported.")
        };
    }

    private static string NormalizeState(string state)
    {
        var normalized = state.Trim().ToLowerInvariant();
        return normalized switch
        {
            CellTrafficAutomationMaterializationConditionStates.Met => CellTrafficAutomationMaterializationConditionStates.Met,
            CellTrafficAutomationMaterializationConditionStates.Unmet => CellTrafficAutomationMaterializationConditionStates.Unmet,
            CellTrafficAutomationMaterializationConditionStates.Pending => CellTrafficAutomationMaterializationConditionStates.Pending,
            CellTrafficAutomationMaterializationConditionStates.Unknown => CellTrafficAutomationMaterializationConditionStates.Unknown,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Condition state is not supported.")
        };
    }

    private static string NormalizeSeverity(string severity)
    {
        var normalized = severity.Trim().ToLowerInvariant();
        return normalized switch
        {
            CellTrafficAutomationMaterializationConditionSeverities.Info => CellTrafficAutomationMaterializationConditionSeverities.Info,
            CellTrafficAutomationMaterializationConditionSeverities.Warning => CellTrafficAutomationMaterializationConditionSeverities.Warning,
            CellTrafficAutomationMaterializationConditionSeverities.Error => CellTrafficAutomationMaterializationConditionSeverities.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Condition severity is not supported.")
        };
    }
}
