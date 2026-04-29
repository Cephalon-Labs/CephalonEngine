namespace Cephalon.Eventing.Services;

/// <summary>
/// Describes the current execution-readiness posture for one declared event subscription.
/// </summary>
public sealed class EventSubscriptionExecutionReadinessDescriptor
{
    /// <summary>
    /// Creates a new event-subscription execution-readiness descriptor.
    /// </summary>
    /// <param name="subscriptionId">The stable declared subscription identifier.</param>
    /// <param name="readinessState">The stable readiness-state identifier.</param>
    /// <param name="executionOwnership">The operator-facing execution ownership answer.</param>
    /// <param name="executionMode">The operator-facing execution mode answer.</param>
    /// <param name="executionRuntimeId">The managed runtime identifier when a runtime-bound path exists.</param>
    /// <param name="reasons">The ordered machine-readable reasons that explain the readiness state.</param>
    /// <param name="metadata">Optional operator-facing metadata associated with the readiness answer.</param>
    public EventSubscriptionExecutionReadinessDescriptor(
        string subscriptionId,
        string readinessState,
        string executionOwnership,
        string executionMode,
        string? executionRuntimeId = null,
        IReadOnlyList<string>? reasons = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            throw new ArgumentException("Subscription id is required.", nameof(subscriptionId));
        }

        if (string.IsNullOrWhiteSpace(readinessState))
        {
            throw new ArgumentException("Readiness state is required.", nameof(readinessState));
        }

        if (string.IsNullOrWhiteSpace(executionOwnership))
        {
            throw new ArgumentException("Execution ownership is required.", nameof(executionOwnership));
        }

        if (string.IsNullOrWhiteSpace(executionMode))
        {
            throw new ArgumentException("Execution mode is required.", nameof(executionMode));
        }

        SubscriptionId = subscriptionId.Trim();
        ReadinessState = readinessState.Trim();
        ExecutionOwnership = executionOwnership.Trim();
        ExecutionMode = executionMode.Trim();
        ExecutionRuntimeId = string.IsNullOrWhiteSpace(executionRuntimeId) ? null : executionRuntimeId.Trim();
        Reasons = reasons?
            .Where(static reason => !string.IsNullOrWhiteSpace(reason))
            .Select(static reason => reason.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static reason => reason, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable declared subscription identifier.
    /// </summary>
    public string SubscriptionId { get; }

    /// <summary>
    /// Gets the stable readiness-state identifier.
    /// </summary>
    public string ReadinessState { get; }

    /// <summary>
    /// Gets a value indicating whether Cephalon can currently observe or bind an execution path for the subscription.
    /// </summary>
    public bool HasExecutionPath => !string.Equals(
        ReadinessState,
        EventSubscriptionExecutionReadinessStates.DeclaredOnly,
        StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the operator-facing execution ownership answer.
    /// </summary>
    public string ExecutionOwnership { get; }

    /// <summary>
    /// Gets the operator-facing execution mode answer.
    /// </summary>
    public string ExecutionMode { get; }

    /// <summary>
    /// Gets the managed runtime identifier when a runtime-bound path exists.
    /// </summary>
    public string? ExecutionRuntimeId { get; }

    /// <summary>
    /// Gets the ordered machine-readable reasons that explain the readiness state.
    /// </summary>
    public IReadOnlyList<string> Reasons { get; }

    /// <summary>
    /// Gets operator-facing metadata associated with the readiness answer.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
