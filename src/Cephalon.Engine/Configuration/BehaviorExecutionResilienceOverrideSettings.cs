using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes one named behavior-execution resilience override configured for a subset of behaviors or transports.
/// </summary>
public sealed class BehaviorExecutionResilienceOverrideSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorExecutionResilienceOverrideSettings" /> class.
    /// </summary>
    /// <param name="id">The stable override identifier.</param>
    /// <param name="behaviorIds">The targeted behavior identifiers.</param>
    /// <param name="transportIds">The targeted transport identifiers.</param>
    /// <param name="retry">The retry override requested for the targeted surface.</param>
    /// <param name="timeout">The timeout override requested for the targeted surface.</param>
    /// <param name="circuitBreaker">The circuit-breaker override requested for the targeted surface.</param>
    /// <param name="bulkhead">The bulkhead override requested for the targeted surface.</param>
    /// <param name="rateLimiting">The rate-limiting override requested for the targeted surface.</param>
    public BehaviorExecutionResilienceOverrideSettings(
        string id,
        IReadOnlyList<string>? behaviorIds = null,
        IReadOnlyList<string>? transportIds = null,
        RetrySettings? retry = null,
        TimeoutSettings? timeout = null,
        CircuitBreakerSettings? circuitBreaker = null,
        BulkheadSettings? bulkhead = null,
        RateLimitingSettings? rateLimiting = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Id = id.Trim();
        BehaviorIds = NormalizeList(behaviorIds);
        TransportIds = NormalizeList(transportIds);
        Retry = retry ?? RetrySettings.Empty;
        Timeout = timeout ?? TimeoutSettings.Empty;
        CircuitBreaker = circuitBreaker ?? CircuitBreakerSettings.Empty;
        Bulkhead = bulkhead ?? BulkheadSettings.Empty;
        RateLimiting = rateLimiting ?? RateLimitingSettings.Empty;
    }

    /// <summary>
    /// Gets the stable override identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the targeted behavior identifiers.
    /// </summary>
    public IReadOnlyList<string> BehaviorIds { get; }

    /// <summary>
    /// Gets the targeted transport identifiers.
    /// </summary>
    public IReadOnlyList<string> TransportIds { get; }

    /// <summary>
    /// Gets the retry override requested for the targeted surface.
    /// </summary>
    public RetrySettings Retry { get; }

    /// <summary>
    /// Gets the timeout override requested for the targeted surface.
    /// </summary>
    public TimeoutSettings Timeout { get; }

    /// <summary>
    /// Gets the circuit-breaker override requested for the targeted surface.
    /// </summary>
    public CircuitBreakerSettings CircuitBreaker { get; }

    /// <summary>
    /// Gets the bulkhead override requested for the targeted surface.
    /// </summary>
    public BulkheadSettings Bulkhead { get; }

    /// <summary>
    /// Gets the rate-limiting override requested for the targeted surface.
    /// </summary>
    public RateLimitingSettings RateLimiting { get; }

    /// <summary>
    /// Gets a value indicating whether any override settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        BehaviorIds.Count > 0 ||
        TransportIds.Count > 0 ||
        HasStrategyValues;

    /// <summary>
    /// Gets a value indicating whether any strategy-level override settings were explicitly supplied.
    /// </summary>
    public bool HasStrategyValues =>
        Retry.HasValues ||
        Timeout.HasValues ||
        CircuitBreaker.HasValues ||
        Bulkhead.HasValues ||
        RateLimiting.HasValues;

    internal static IReadOnlyList<BehaviorExecutionResilienceOverrideSettings> FromSection(IConfiguration section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return section.GetChildren()
            .Select(static child => new BehaviorExecutionResilienceOverrideSettings(
                id: child.Key,
                behaviorIds: ReadStringArray(child.GetSection("Behaviors")),
                transportIds: ReadStringArray(child.GetSection("Transports")),
                retry: RetrySettings.FromSection(child.GetSection("Retry")),
                timeout: TimeoutSettings.FromSection(child.GetSection("Timeout")),
                circuitBreaker: CircuitBreakerSettings.FromSection(child.GetSection("CircuitBreaker")),
                bulkhead: BulkheadSettings.FromSection(child.GetSection("Bulkhead")),
                rateLimiting: RateLimitingSettings.FromSection(child.GetSection("RateLimiting"))))
            .ToArray();
    }

    private static string[] ReadStringArray(IConfiguration section)
    {
        return section.GetChildren()
            .Select(static child => child.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

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
