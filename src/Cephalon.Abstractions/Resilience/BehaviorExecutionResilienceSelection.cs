using Cephalon.Abstractions.AppModel;

namespace Cephalon.Abstractions.Resilience;

/// <summary>
/// Describes the subset of resilience policy selections that apply to behavior execution pipelines.
/// </summary>
public sealed class BehaviorExecutionResilienceSelection
{
    /// <summary>
    /// Gets an empty behavior-execution resilience selection.
    /// </summary>
    public static BehaviorExecutionResilienceSelection Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorExecutionResilienceSelection" /> class.
    /// </summary>
    /// <param name="retry">The retry selection that applies to behavior execution.</param>
    /// <param name="timeout">The timeout selection that applies to behavior execution.</param>
    /// <param name="circuitBreaker">The circuit-breaker selection that applies to behavior execution.</param>
    /// <param name="bulkhead">The bulkhead selection that applies to behavior execution.</param>
    /// <param name="rateLimiting">The rate-limiting selection that applies to behavior execution.</param>
    public BehaviorExecutionResilienceSelection(
        RetrySelection? retry = null,
        TimeoutSelection? timeout = null,
        CircuitBreakerSelection? circuitBreaker = null,
        BulkheadSelection? bulkhead = null,
        RateLimitingSelection? rateLimiting = null)
    {
        Retry = retry ?? RetrySelection.Empty;
        Timeout = timeout ?? TimeoutSelection.Empty;
        CircuitBreaker = circuitBreaker ?? CircuitBreakerSelection.Empty;
        Bulkhead = bulkhead ?? BulkheadSelection.Empty;
        RateLimiting = rateLimiting ?? RateLimitingSelection.Empty;
    }

    /// <summary>
    /// Gets the retry selection that applies to behavior execution.
    /// </summary>
    public RetrySelection Retry { get; }

    /// <summary>
    /// Gets the timeout selection that applies to behavior execution.
    /// </summary>
    public TimeoutSelection Timeout { get; }

    /// <summary>
    /// Gets the circuit-breaker selection that applies to behavior execution.
    /// </summary>
    public CircuitBreakerSelection CircuitBreaker { get; }

    /// <summary>
    /// Gets the bulkhead selection that applies to behavior execution.
    /// </summary>
    public BulkheadSelection Bulkhead { get; }

    /// <summary>
    /// Gets the rate-limiting selection that applies to behavior execution.
    /// </summary>
    public RateLimitingSelection RateLimiting { get; }

    /// <summary>
    /// Gets a value indicating whether any behavior-execution resilience inputs were supplied.
    /// </summary>
    public bool HasValues =>
        Retry.HasValues ||
        Timeout.HasValues ||
        CircuitBreaker.HasValues ||
        Bulkhead.HasValues ||
        RateLimiting.HasValues;
}
