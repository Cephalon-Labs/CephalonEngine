using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes the resilience-policy inputs resolved for a Cephalon app.
/// </summary>
public sealed class ResilienceSelection
{
    /// <summary>
    /// Gets an empty resilience-selection instance.
    /// </summary>
    public static ResilienceSelection Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceSelection" /> class.
    /// </summary>
    /// <param name="retry">The retry policy resolved for the app.</param>
    /// <param name="timeout">The timeout policy resolved for the app.</param>
    /// <param name="circuitBreaker">The circuit-breaker policy resolved for the app.</param>
    /// <param name="bulkhead">The bulkhead policy resolved for the app.</param>
    /// <param name="rateLimiting">The rate-limiting policy resolved for the app.</param>
    /// <param name="behaviorExecutionOverrides">The named behavior-execution override policies targeted at specific behaviors or transports.</param>
    [JsonConstructor]
    public ResilienceSelection(
        RetrySelection? retry = null,
        TimeoutSelection? timeout = null,
        CircuitBreakerSelection? circuitBreaker = null,
        BulkheadSelection? bulkhead = null,
        RateLimitingSelection? rateLimiting = null,
        IReadOnlyList<BehaviorExecutionResilienceOverrideSelection>? behaviorExecutionOverrides = null)
    {
        Retry = retry ?? RetrySelection.Empty;
        Timeout = timeout ?? TimeoutSelection.Empty;
        CircuitBreaker = circuitBreaker ?? CircuitBreakerSelection.Empty;
        Bulkhead = bulkhead ?? BulkheadSelection.Empty;
        RateLimiting = rateLimiting ?? RateLimitingSelection.Empty;
        BehaviorExecutionOverrides = behaviorExecutionOverrides?
            .Where(static entry => entry is not null)
            .ToArray() ?? [];
    }

    /// <summary>
    /// Gets the retry policy resolved for the app.
    /// </summary>
    public RetrySelection Retry { get; }

    /// <summary>
    /// Gets the timeout policy resolved for the app.
    /// </summary>
    public TimeoutSelection Timeout { get; }

    /// <summary>
    /// Gets the circuit-breaker policy resolved for the app.
    /// </summary>
    public CircuitBreakerSelection CircuitBreaker { get; }

    /// <summary>
    /// Gets the bulkhead policy resolved for the app.
    /// </summary>
    public BulkheadSelection Bulkhead { get; }

    /// <summary>
    /// Gets the rate-limiting policy resolved for the app.
    /// </summary>
    public RateLimitingSelection RateLimiting { get; }

    /// <summary>
    /// Gets the named behavior-execution override policies targeted at specific behaviors or transports.
    /// </summary>
    public IReadOnlyList<BehaviorExecutionResilienceOverrideSelection> BehaviorExecutionOverrides { get; }

    /// <summary>
    /// Gets a value indicating whether any resilience-selection inputs were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Retry.HasValues ||
        Timeout.HasValues ||
        CircuitBreaker.HasValues ||
        Bulkhead.HasValues ||
        RateLimiting.HasValues ||
        BehaviorExecutionOverrides.Count > 0;
}
