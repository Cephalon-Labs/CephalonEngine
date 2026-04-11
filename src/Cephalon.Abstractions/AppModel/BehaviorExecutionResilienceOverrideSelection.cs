using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.AppModel;

/// <summary>
/// Describes one named behavior-execution resilience override requested for a subset of behaviors or transports.
/// </summary>
public sealed class BehaviorExecutionResilienceOverrideSelection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorExecutionResilienceOverrideSelection" /> class.
    /// </summary>
    /// <param name="id">The stable override identifier.</param>
    /// <param name="behaviorIds">The targeted behavior identifiers.</param>
    /// <param name="transportIds">The targeted transport identifiers.</param>
    /// <param name="retry">The retry override requested for the targeted surface.</param>
    /// <param name="timeout">The timeout override requested for the targeted surface.</param>
    /// <param name="circuitBreaker">The circuit-breaker override requested for the targeted surface.</param>
    /// <param name="bulkhead">The bulkhead override requested for the targeted surface.</param>
    [JsonConstructor]
    public BehaviorExecutionResilienceOverrideSelection(
        string id,
        IReadOnlyList<string>? behaviorIds = null,
        IReadOnlyList<string>? transportIds = null,
        RetrySelection? retry = null,
        TimeoutSelection? timeout = null,
        CircuitBreakerSelection? circuitBreaker = null,
        BulkheadSelection? bulkhead = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Id = id.Trim();
        BehaviorIds = NormalizeList(behaviorIds);
        TransportIds = NormalizeList(transportIds);
        Retry = retry ?? RetrySelection.Empty;
        Timeout = timeout ?? TimeoutSelection.Empty;
        CircuitBreaker = circuitBreaker ?? CircuitBreakerSelection.Empty;
        Bulkhead = bulkhead ?? BulkheadSelection.Empty;
    }

    /// <summary>
    /// Gets the stable override identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the behavior identifiers targeted by this override.
    /// </summary>
    public IReadOnlyList<string> BehaviorIds { get; }

    /// <summary>
    /// Gets the transport identifiers targeted by this override.
    /// </summary>
    public IReadOnlyList<string> TransportIds { get; }

    /// <summary>
    /// Gets the retry override requested for the targeted surface.
    /// </summary>
    public RetrySelection Retry { get; }

    /// <summary>
    /// Gets the timeout override requested for the targeted surface.
    /// </summary>
    public TimeoutSelection Timeout { get; }

    /// <summary>
    /// Gets the circuit-breaker override requested for the targeted surface.
    /// </summary>
    public CircuitBreakerSelection CircuitBreaker { get; }

    /// <summary>
    /// Gets the bulkhead override requested for the targeted surface.
    /// </summary>
    public BulkheadSelection Bulkhead { get; }

    /// <summary>
    /// Gets a value indicating whether any override values were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        BehaviorIds.Count > 0 ||
        TransportIds.Count > 0 ||
        HasStrategyValues;

    /// <summary>
    /// Gets a value indicating whether any strategy-level override values were explicitly supplied.
    /// </summary>
    public bool HasStrategyValues =>
        Retry.HasValues ||
        Timeout.HasValues ||
        CircuitBreaker.HasValues ||
        Bulkhead.HasValues;

    private static string[] NormalizeList(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
