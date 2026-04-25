namespace Cephalon.Eventing.Services;

/// <summary>
/// Describes how a declared event subscription binds to a managed execution runtime.
/// </summary>
public sealed class EventSubscriptionExecutionBindingDescriptor
{
    /// <summary>
    /// Creates a new managed execution binding descriptor for a declared event subscription.
    /// </summary>
    /// <param name="subscriptionId">The stable declared subscription identifier.</param>
    /// <param name="executionRuntimeId">The operator-facing managed execution-runtime identifier.</param>
    /// <param name="executionOwnership">The operator-facing ownership mode for the execution runtime.</param>
    /// <param name="executionMode">The operator-facing execution mode for the binding.</param>
    /// <param name="metadata">Optional operator-facing metadata associated with the binding.</param>
    public EventSubscriptionExecutionBindingDescriptor(
        string subscriptionId,
        string executionRuntimeId,
        string executionOwnership = "runtime-managed",
        string executionMode = "message-handler",
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            throw new ArgumentException("Subscription id is required.", nameof(subscriptionId));
        }

        if (string.IsNullOrWhiteSpace(executionRuntimeId))
        {
            throw new ArgumentException("Execution runtime id is required.", nameof(executionRuntimeId));
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
        ExecutionRuntimeId = executionRuntimeId.Trim();
        ExecutionOwnership = executionOwnership.Trim();
        ExecutionMode = executionMode.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable declared subscription identifier.
    /// </summary>
    public string SubscriptionId { get; }

    /// <summary>
    /// Gets the operator-facing managed execution-runtime identifier.
    /// </summary>
    public string ExecutionRuntimeId { get; }

    /// <summary>
    /// Gets the operator-facing ownership mode for the managed execution runtime.
    /// </summary>
    public string ExecutionOwnership { get; }

    /// <summary>
    /// Gets the operator-facing execution mode for the binding.
    /// </summary>
    public string ExecutionMode { get; }

    /// <summary>
    /// Gets operator-facing metadata associated with the binding.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
