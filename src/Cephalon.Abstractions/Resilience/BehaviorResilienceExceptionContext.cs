using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Abstractions.Resilience;

/// <summary>
/// Describes one behavior-execution exception being evaluated by the resilience pipeline.
/// </summary>
public sealed class BehaviorResilienceExceptionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorResilienceExceptionContext" /> class.
    /// </summary>
    /// <param name="policyId">The stable resilience-policy identifier handling the exception.</param>
    /// <param name="behaviorId">The stable behavior identifier being executed.</param>
    /// <param name="transportId">The active transport identifier when one is known.</param>
    /// <param name="targetedBehaviorIds">The behavior identifiers targeted by the active policy.</param>
    /// <param name="targetedTransportIds">The transport identifiers targeted by the active policy.</param>
    /// <param name="exception">The exception being classified.</param>
    /// <param name="behaviorIdempotency">The declared behavior idempotency mode when one is known.</param>
    public BehaviorResilienceExceptionContext(
        string policyId,
        string behaviorId,
        string? transportId,
        IReadOnlyList<string>? targetedBehaviorIds,
        IReadOnlyList<string>? targetedTransportIds,
        Exception exception,
        BehaviorIdempotencyMode behaviorIdempotency = BehaviorIdempotencyMode.Unknown)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentNullException.ThrowIfNull(exception);

        PolicyId = policyId.Trim();
        BehaviorId = behaviorId.Trim();
        TransportId = string.IsNullOrWhiteSpace(transportId)
            ? null
            : transportId.Trim();
        TargetedBehaviorIds = targetedBehaviorIds?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .ToArray() ?? [];
        TargetedTransportIds = targetedTransportIds?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .ToArray() ?? [];
        Exception = exception;
        BehaviorIdempotency = behaviorIdempotency;
    }

    /// <summary>
    /// Gets the stable resilience-policy identifier handling the exception.
    /// </summary>
    public string PolicyId { get; }

    /// <summary>
    /// Gets the stable behavior identifier being executed.
    /// </summary>
    public string BehaviorId { get; }

    /// <summary>
    /// Gets the active transport identifier when one is known.
    /// </summary>
    public string? TransportId { get; }

    /// <summary>
    /// Gets the behavior identifiers targeted by the active policy.
    /// </summary>
    public IReadOnlyList<string> TargetedBehaviorIds { get; }

    /// <summary>
    /// Gets the transport identifiers targeted by the active policy.
    /// </summary>
    public IReadOnlyList<string> TargetedTransportIds { get; }

    /// <summary>
    /// Gets the exception being classified.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets the declared behavior idempotency mode when one is known.
    /// </summary>
    public BehaviorIdempotencyMode BehaviorIdempotency { get; }
}
