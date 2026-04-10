using Microsoft.Extensions.Configuration;

namespace Cephalon.Engine.Configuration;

/// <summary>
/// Describes configuration-driven resilience settings for a Cephalon app.
/// </summary>
public sealed class ResilienceSettings
{
    /// <summary>
    /// Gets an empty resilience-settings instance.
    /// </summary>
    public static ResilienceSettings Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceSettings" /> class.
    /// </summary>
    /// <param name="retry">The retry settings resolved for the app.</param>
    /// <param name="timeout">The timeout settings resolved for the app.</param>
    /// <param name="circuitBreaker">The circuit-breaker settings resolved for the app.</param>
    /// <param name="bulkhead">The bulkhead settings resolved for the app.</param>
    /// <param name="rateLimiting">The rate-limiting settings resolved for the app.</param>
    public ResilienceSettings(
        RetrySettings? retry = null,
        TimeoutSettings? timeout = null,
        CircuitBreakerSettings? circuitBreaker = null,
        BulkheadSettings? bulkhead = null,
        RateLimitingSettings? rateLimiting = null)
    {
        Retry = retry ?? RetrySettings.Empty;
        Timeout = timeout ?? TimeoutSettings.Empty;
        CircuitBreaker = circuitBreaker ?? CircuitBreakerSettings.Empty;
        Bulkhead = bulkhead ?? BulkheadSettings.Empty;
        RateLimiting = rateLimiting ?? RateLimitingSettings.Empty;
    }

    /// <summary>
    /// Gets the retry settings resolved for the app.
    /// </summary>
    public RetrySettings Retry { get; }

    /// <summary>
    /// Gets the timeout settings resolved for the app.
    /// </summary>
    public TimeoutSettings Timeout { get; }

    /// <summary>
    /// Gets the circuit-breaker settings resolved for the app.
    /// </summary>
    public CircuitBreakerSettings CircuitBreaker { get; }

    /// <summary>
    /// Gets the bulkhead settings resolved for the app.
    /// </summary>
    public BulkheadSettings Bulkhead { get; }

    /// <summary>
    /// Gets the rate-limiting settings resolved for the app.
    /// </summary>
    public RateLimitingSettings RateLimiting { get; }

    /// <summary>
    /// Gets a value indicating whether any resilience settings were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        Retry.HasValues ||
        Timeout.HasValues ||
        CircuitBreaker.HasValues ||
        Bulkhead.HasValues ||
        RateLimiting.HasValues;

    /// <summary>
    /// Reads resilience settings from configuration.
    /// </summary>
    /// <param name="configuration">The configuration source that contains the engine section.</param>
    /// <param name="sectionPath">The root configuration section path to read from.</param>
    /// <returns>The parsed resilience settings.</returns>
    public static ResilienceSettings FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Resilience");

        return new ResilienceSettings(
            retry: RetrySettings.FromSection(section.GetSection("Retry")),
            timeout: TimeoutSettings.FromSection(section.GetSection("Timeout")),
            circuitBreaker: CircuitBreakerSettings.FromSection(section.GetSection("CircuitBreaker")),
            bulkhead: BulkheadSettings.FromSection(section.GetSection("Bulkhead")),
            rateLimiting: RateLimitingSettings.FromSection(section.GetSection("RateLimiting")));
    }
}
